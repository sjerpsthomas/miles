using Core.midi;
using static Core.tokens.v2.conversion.V2_RelativeMelody;

namespace Core.tokens.v2.conversion.stage;

public static class V2_PitchStage
{
    public static V2_RelativeMelody TokenizePitch(List<MidiNote> midiNotes, LeadSheet? leadSheet, int startMeasureNum)
    {
        if (midiNotes is [])
            return new V2_RelativeMelody();
        
        var resArr = new V2_RelativeMelodyToken?[midiNotes.Count];

        // Get key of song
        for (var i = 0; i < midiNotes.Count; i++)
        {
            var (_, time, length, note, velocity) = midiNotes[i];

            var octaveScaleNote = V2_ChordMethods.GetOctaveScaleNote(leadSheet, time + startMeasureNum, note);
            resArr[i] = new V2_RelativeMelodyNote(octaveScaleNote, time, length, velocity);
        }

        return new V2_RelativeMelody { Tokens = resArr.Select(it => it!).ToList() };
    }

    public static List<MidiNote> ReconstructPitch(V2_RelativeMelody relativeMelody, LeadSheet leadSheet, int startMeasureNum)
    {
        var outputName = OutputName.Algorithm;
        
        var tokens = relativeMelody.Tokens;
        var resArr = new MidiNote?[tokens.Count];

        // First convert all note tokens
        for (var index = 0; index < tokens.Count; index++)
        {
            var token = tokens[index];
            if (token is not V2_RelativeMelodyNote(var octaveScaleNote, var time, var length, var velocity)) continue;

            var absoluteNote = V2_ChordMethods.GetAbsoluteNote(leadSheet, time + startMeasureNum, octaveScaleNote) + 36;
            resArr[index] = new MidiNote(outputName, time, length, absoluteNote, velocity);
        }
        
        var res = resArr
            .Where(it => it is not null)
            .Select(it => (MidiNote)it!)
            .ToList();
        return res;
    }
}