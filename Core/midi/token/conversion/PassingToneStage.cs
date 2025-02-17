using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using NAudio.Midi;
using static Core.midi.Chord;
using static Core.midi.token.conversion.OctaveMelody;

namespace Core.midi.token.conversion;

public static class PassingToneStage
{
    [SuppressMessage("ReSharper", "ConvertTypeCheckPatternToNullCheck")]
    public static List<MidiNote> ResolvePassingTones(OctaveMelody octaveMelody, LeadSheet leadSheet, int startMeasureNum)
    {
        var outputName = OutputName.Algorithm;
        
        var tokens = octaveMelody.Tokens;
        var resArr = new MidiNote?[tokens.Count];

        // First convert all note tokens
        for (var index = 0; index < tokens.Count; index++)
        {
            var token = tokens[index];
            if (token is not OctaveMelodyNote(var octaveScaleNote, var time, var length, var velocity)) continue;

            var note = leadSheet.ChordAtTime(time + startMeasureNum).GetAbsoluteNote(octaveScaleNote) + 36;
            resArr[index] = new MidiNote(outputName, time, length, note, velocity);
        }
        
        // Then convert all passing note tokens
        for (var index = 0; index < tokens.Count; index++)
        {
            if (resArr[index] != null) continue;
            
            var firstIndex = index - 1;
            var firstMidiNote = firstIndex >= 0 && tokens[firstIndex] is OctaveMelodyNote
                ? resArr[firstIndex]
                : null;

            var secondIndex = index + 1;
            while (secondIndex < tokens.Count && tokens[secondIndex] is not OctaveMelodyNote)
                secondIndex++;
            
            var secondMidiNote = secondIndex < tokens.Count && tokens[secondIndex] is OctaveMelodyNote
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
                    
                if (tokens[ptIndex] is not OctaveMelodyPassingTone(var ptTime, var ptLength, var ptVelocity))
                    continue;
                    
                var f = (k + 1) / ((double)passingCount + 1);
                var newNote = (int)((1 - f) * first + f * second);
                    
                resArr[ptIndex] = new MidiNote(outputName, ptTime, ptLength, newNote, ptVelocity / 3);
            }
        }

        var res = resArr
            .Where(it => it is not null)
            .Select(it => (MidiNote)it!)
            .ToList();
        return res;
    }

    public static OctaveMelody DeducePassingTones(List<MidiNote> midiNotes)
    {
        if (midiNotes is [])
            return new OctaveMelody();
        
        var resArr = new OctaveMelodyToken?[midiNotes.Count];

        var prevDelta = 999;
        for (var i = 0; i < midiNotes.Count - 1; i++)
        {
            var (_, time, length, note, velocity) = midiNotes[i];
            var delta = midiNotes[i + 1].Note - note;

            // Add passing tone if delta is same as previous
            if (prevDelta == delta)
            {
                resArr[i] = new OctaveMelodyPassingTone(time, length, velocity);
            }
            else
            {
                var octaveScaleNote = CMajor.GetRelativeNote(note - 36);
                resArr[i] = new OctaveMelodyNote(octaveScaleNote, time, length, velocity);
            }
            
            prevDelta = delta;
        }
        
        // Add last note
        {
            var (_, time, length, note, velocity) = midiNotes[^1];
            var octaveScaleNote = CMajor.GetRelativeNote(note - 36);
            resArr[^1] = new OctaveMelodyNote(octaveScaleNote, time, length, velocity);
        }

        return new OctaveMelody { Tokens = resArr.Select(it => it!).ToList() };
    }
}
