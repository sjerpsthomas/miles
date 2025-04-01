using Core.midi;
using Core.midi.token;
using Core.midi.token.conversion.stage;

namespace Core.conversion;

public static partial class Conversion
{
    public static List<Token> Tokenize(List<MidiNote> midiNotes, LeadSheet? leadSheet = null)
    {
        var relativeMelody = PitchStage.TokenizePitch(midiNotes, leadSheet);
        var tokenMelody = OctaveStage.TokenizeOctaves(relativeMelody);
        var timedTokenMelody = TimingStage.TokenizeTiming(tokenMelody, leadSheet);
        var tokens = VelocityStage.TokenizeVelocity(timedTokenMelody);

        return tokens;
    }
    
    public static List<MidiNote> Reconstruct(List<Token> tokens, LeadSheet leadSheet, int startMeasureNum)
    {
        var timedTokenMelody = VelocityStage.ReconstructVelocity(tokens);
        var tokenMelody = TimingStage.ReconstructTiming(timedTokenMelody, leadSheet);
        var relativeMelody = OctaveStage.ReconstructOctaves(tokenMelody);
        var midiNotes = PitchStage.ReconstructPitch(relativeMelody, leadSheet, startMeasureNum);

        return midiNotes;
    }
}