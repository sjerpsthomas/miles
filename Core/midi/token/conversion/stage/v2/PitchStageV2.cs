using static Core.midi.token.conversion.RelativeMelody;

namespace Core.midi.token.conversion.stage.v2;

public static class PitchStageV2
{
    public static RelativeMelody TokenizePitch(List<MidiNote> midiNotes, LeadSheet? leadSheet, int startMeasureNum)
    {
        if (midiNotes is [])
            return new RelativeMelody();
        
        var resArr = new RelativeMelodyToken?[midiNotes.Count];

        // Get key of song
        for (var i = 0; i < midiNotes.Count; i++)
        {
            var (_, time, length, note, velocity) = midiNotes[i];

            var currentChord = leadSheet?.ChordAtTime(time + startMeasureNum) ?? Chord.CMajor;
            
            // note - 36 omitted; octaves are removed during DeduceOctaves anyway
            var octaveScaleNote = currentChord.GetRelativeNote(note);
            resArr[i] = new RelativeMelodyNote(octaveScaleNote, time, length, velocity);
        }

        return new RelativeMelody { Tokens = resArr.Select(it => it!).ToList() };
    }

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
        
        var res = resArr
            .Where(it => it is not null)
            .Select(it => (MidiNote)it!)
            .ToList();
        return res;
    }
}