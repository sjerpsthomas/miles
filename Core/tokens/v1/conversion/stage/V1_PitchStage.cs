using System.Diagnostics.CodeAnalysis;
using Core.midi;
using static Core.tokens.v1.conversion.V1_RelativeMelody;

namespace Core.tokens.v1.conversion.stage;

public static class V1_PitchStage
{
    public static V1_RelativeMelody TokenizePitch(List<MidiNote> midiNotes, LeadSheet? leadSheet)
    {
        if (midiNotes is [])
            return new V1_RelativeMelody();
        
        var resArr = new V1_RelativeMelodyToken?[midiNotes.Count];

        // Get key of song
        var key = leadSheet?.Key ?? Chord.CMajor;
        
        var prevDelta = 999;
        for (var i = 0; i < midiNotes.Count - 1; i++)
        {
            var (_, time, length, note, velocity) = midiNotes[i];
            var delta = midiNotes[i + 1].Note - note;
            
            // Add passing tone if delta is same as previous
            if (prevDelta == delta)
            {
                resArr[i] = new V1_RelativeMelodyPassingTone(time, length, velocity);
            }
            else
            {
                // note - 36 omitted; octaves are removed during DeduceOctaves anyway
                var octaveScaleNote = key.GetRelativeNote(note);
                resArr[i] = new V1_RelativeMelodyNote(octaveScaleNote, time, length, velocity);
            }
            
            prevDelta = delta;
        }
        
        // Add last note
        {
            var (_, time, length, note, velocity) = midiNotes[^1];
            // note - 36 omitted; octaves are removed during DeduceOctaves anyway
            var octaveScaleNote = key.GetRelativeNote(note);
            resArr[^1] = new V1_RelativeMelodyNote(octaveScaleNote, time, length, velocity);
        }

        return new V1_RelativeMelody { Tokens = resArr.Select(it => it!).ToList() };
    }
    
    [SuppressMessage("ReSharper", "ConvertTypeCheckPatternToNullCheck")]
    public static List<MidiNote> ReconstructPitch(V1_RelativeMelody relativeMelody, LeadSheet leadSheet, int startMeasureNum)
    {
        var outputName = OutputName.Algorithm;
        
        var tokens = relativeMelody.Tokens;
        var resArr = new MidiNote?[tokens.Count];

        // First convert all note tokens
        for (var index = 0; index < tokens.Count; index++)
        {
            var token = tokens[index];
            if (token is not V1_RelativeMelodyNote(var octaveScaleNote, var time, var length, var velocity)) continue;

            var note = leadSheet.ChordAtTime(time + startMeasureNum).GetAbsoluteNote(octaveScaleNote) + 36;
            resArr[index] = new MidiNote(outputName, time, length, note, velocity);
        }
        
        // Then convert all passing note tokens
        for (var index = 0; index < tokens.Count; index++)
        {
            if (resArr[index] != null) continue;
            
            var firstIndex = index - 1;
            var firstMidiNote = firstIndex >= 0 && tokens[firstIndex] is V1_RelativeMelodyNote
                ? resArr[firstIndex]
                : null;

            var secondIndex = index + 1;
            while (secondIndex < tokens.Count && tokens[secondIndex] is not V1_RelativeMelodyNote)
                secondIndex++;
            
            var secondMidiNote = secondIndex < tokens.Count && tokens[secondIndex] is V1_RelativeMelodyNote
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
                    
                if (tokens[ptIndex] is not V1_RelativeMelodyPassingTone(var ptTime, var ptLength, var ptVelocity))
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