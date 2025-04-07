using Core.midi;
using Core.midi.token;
using Core.midi.token.conversion.stage;
using Core.midi.token.conversion.stage.v2;

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
    
    public static List<Token> TokenizeV2(List<MidiNote> midiNotes, LeadSheet? leadSheet = null, int startMeasureNum = 0)
        {
            var relativeMelody = PitchStageV2.TokenizePitch(midiNotes, leadSheet, startMeasureNum);
            var tokenMelody = OctaveStageV2.TokenizeOctaves(relativeMelody);
            var timedTokenMelody = TimingStageV2.TokenizeTiming(tokenMelody, leadSheet);
            var tokens = VelocityStageV2.TokenizeVelocity(timedTokenMelody);
    
            return tokens;
        }
        
        public static List<MidiNote> ReconstructV2(List<Token> tokens, LeadSheet leadSheet, int startMeasureNum)
        {
            var timedTokenMelody = VelocityStageV2.ReconstructVelocity(tokens);
            var tokenMelody = TimingStageV2.ReconstructTiming(timedTokenMelody, leadSheet);
            var relativeMelody = OctaveStageV2.ReconstructOctaves(tokenMelody);
            var midiNotes = PitchStageV2.ReconstructPitch(relativeMelody, leadSheet, startMeasureNum);
    
            return midiNotes;
        }
}