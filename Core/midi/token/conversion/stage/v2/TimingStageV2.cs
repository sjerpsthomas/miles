namespace Core.midi.token.conversion.stage.v2;

public static class TimingStageV2
{
    public static TimedTokenMelody TokenizeTiming(TokenMelody tokenMelody, LeadSheet? leadSheet) =>
        TimingStage.TokenizeTiming(tokenMelody, leadSheet);

    public static TokenMelody ReconstructTiming(TimedTokenMelody timedTokenMelody, LeadSheet leadSheet) =>
        TimingStage.ReconstructTiming(timedTokenMelody, leadSheet);
}