namespace Core.midi.token.conversion.stage.v2;

public static class VelocityStageV2
{
    public static List<Token> TokenizeVelocity(TimedTokenMelody timedTokenMelody) =>
        VelocityStage.TokenizeVelocity(timedTokenMelody);

    public static TimedTokenMelody ReconstructVelocity(List<Token> tokens) =>
        VelocityStage.ReconstructVelocity(tokens);
}