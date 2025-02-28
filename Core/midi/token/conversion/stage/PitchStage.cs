using System.Diagnostics.CodeAnalysis;
using static Core.midi.Chord;
using static Core.midi.token.conversion.RelativeMelody;

namespace Core.midi.token.conversion.stage;

public static class PitchStage
{
    public static RelativeMelody TokenizePitch(List<MidiNote> midiNotes, LeadSheet? leadSheet)
    {
        if (midiNotes is [])
            return new RelativeMelody();
        
        var resArr = new RelativeMelodyToken?[midiNotes.Count];

        // Get key of song
        var key = leadSheet?.Key ?? CMajor;
        
        var prevDelta = 999;
        for (var i = 0; i < midiNotes.Count - 1; i++)
        {
            var (_, time, length, note, velocity) = midiNotes[i];
            var delta = midiNotes[i + 1].Note - note;
            
            // Add passing tone if delta is same as previous
            if (prevDelta == delta)
            {
                resArr[i] = new RelativeMelodyPassingTone(time, length, velocity);
            }
            else
            {
                // note - 36 omitted; octaves are removed during DeduceOctaves anyway
                var octaveScaleNote = key.GetRelativeNote(note);
                resArr[i] = new RelativeMelodyNote(octaveScaleNote, time, length, velocity);
            }
            
            prevDelta = delta;
        }
        
        // Add last note
        {
            var (_, time, length, note, velocity) = midiNotes[^1];
            // note - 36 omitted; octaves are removed during DeduceOctaves anyway
            var octaveScaleNote = key.GetRelativeNote(note);
            resArr[^1] = new RelativeMelodyNote(octaveScaleNote, time, length, velocity);
        }

        return new RelativeMelody { Tokens = resArr.Select(it => it!).ToList() };
    }
    
    [SuppressMessage("ReSharper", "ConvertTypeCheckPatternToNullCheck")]
    public static List<MidiNote> ReconstructPitch(RelativeMelody relativeMelody, LeadSheet leadSheet, int startMeasureNum)
    {
        var outputName = OutputName.Algorithm;
        
        var tokens = relativeMelody.Tokens;
        var resArr = new MidiNote?[tokens.Count];

        // First convert all note tokens
        for (var index = 0; index < tokens.Count; index++)
        {
            var token = tokens[index];
            if (token is not RelativeMelodyNote(var octaveScaleNote, var time, var length, var velocity)) continue;

            var note = leadSheet.ChordAtTime(time + startMeasureNum).GetAbsoluteNote(octaveScaleNote) + 36;
            resArr[index] = new MidiNote(outputName, time, length, note, velocity);
        }
        
        // Then convert all passing note tokens
        for (var index = 0; index < tokens.Count; index++)
        {
            if (resArr[index] != null) continue;
            
            var firstIndex = index - 1;
            var firstMidiNote = firstIndex >= 0 && tokens[firstIndex] is RelativeMelodyNote
                ? resArr[firstIndex]
                : null;

            var secondIndex = index + 1;
            while (secondIndex < tokens.Count && tokens[secondIndex] is not RelativeMelodyNote)
                secondIndex++;
            
            var secondMidiNote = secondIndex < tokens.Count && tokens[secondIndex] is RelativeMelodyNote
                ? resArr[secondIndex]
                : null;

            var passingCount = secondIndex - firstIndex - 1;

            // Create runs
            int first, second;
            switch (firstMidiNote, secondMidiNote)
            {
                // Both notes are valid
                case var ((_, _, _, n1, _), (_, _, _, n2, _)):
                    first = n1;
                    second = n2;
                    break;
                
                // One note is valid
                case var ((_, _, _, n1, _), _):
                    first = n1;
                    second = n1 + passingCount + 1;
                    break;
                case var (_, (_, _, _, n2, _)):
                    first = n2 - passingCount - 1;
                    second = n2;
                    break;
                
                // No notes are valid
                default:
                    continue;
            }

            for (var k = 0; k < passingCount; k++)
            {
                var ptIndex = index + k;
                    
                if (tokens[ptIndex] is not RelativeMelodyPassingTone(var ptTime, var ptLength, var ptVelocity))
                    continue;
                    
                var f = (k + 1) / ((double)passingCount + 1);
                var newNote = (int)((1 - f) * first + f * second);
                    
                resArr[ptIndex] = new MidiNote(outputName, ptTime, ptLength, newNote, ptVelocity);
            }
        }

        var res = resArr
            .Where(it => it is not null)
            .Select(it => (MidiNote)it!)
            .ToList();
        return res;
    }
}
