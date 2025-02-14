using System.Diagnostics.CodeAnalysis;
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
        
        for (var index = 0; index < tokens.Count; index++)
        {
            // Ignore already handled passing tones
            if (resArr[index] != null)
                continue;
            
            var token = tokens[index];
            
            switch (token)
            {
                // Handle passing tones
                case OctaveMelodyPassingTone:
                {
                    // First note is previous token
                    var firstIndex = index - 1;
                    
                    // Get second note index
                    var secondIndex = index;
                    do secondIndex++;
                    while (secondIndex < tokens.Count - 1 && tokens[secondIndex] is not OctaveMelodyNote);

                    var passingCount = secondIndex - firstIndex - 1;
                    
                    // Get notes from lead sheet
                    int? firstMaybe = index > 0 && tokens[firstIndex] is OctaveMelodyNote(var octaveScaleNote1, var t1, _, _)
                        ? leadSheet.ChordAtTime(t1 + startMeasureNum).GetAbsoluteNote(octaveScaleNote1) + 36
                        : null;
                    int? secondMaybe = secondIndex < tokens.Count && tokens[secondIndex] is OctaveMelodyNote(var octaveScaleNote2, var t2, _, _)
                        ? leadSheet.ChordAtTime(t2 + startMeasureNum).GetAbsoluteNote(octaveScaleNote2) + 36
                        : null;

                    // Create runs if not known
                    var (first, second) = (firstMaybe, secondMaybe) switch
                    {
                        (int v1, int v2) => (v1, v2),
                        (int v1, null) => (v1, v1 + passingCount + 1),
                        (null, int v2) => (v2 - passingCount - 1, v2),
                        _ => (-1, -1)
                    };

                    // Continue if no notes found
                    if (first == -1)
                        continue;
                    
                    // Set all passing tones
                    for (var k = index; k < secondIndex; k++)
                    {
                        if (tokens[k] is not OctaveMelodyPassingTone(var ptTime, var ptLength, var ptVelocity))
                            continue;
                        
                        var f = (k + 1) / ((double)passingCount + 1);
                        var newNote = (int)((1 - f) * first + f * second);

                        resArr[k] = new MidiNote(outputName, ptTime, ptLength, newNote, ptVelocity);
                    }
                    break;
                }
                
                // Simply convert notes
                case OctaveMelodyNote(var octaveScaleNote, var time, var length, var velocity):
                    var note = leadSheet.ChordAtTime(time + startMeasureNum).GetAbsoluteNote(octaveScaleNote) + 36;
                    resArr[index] = new MidiNote(outputName, time, length, note, velocity);
                    break;
            }
        }

        var res = resArr.Select(it => (MidiNote)it!).ToList();
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
