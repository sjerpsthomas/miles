namespace Core.midi.token.conversion.stage.v2;

public static class OctaveStageV2
{
    public static TokenMelody TokenizeOctaves(RelativeMelody relativeMelody) =>
        OctaveStage.TokenizeOctaves(relativeMelody);

    public static RelativeMelody ReconstructOctaves(TokenMelody tokenMelody) =>
        OctaveStage.ReconstructOctaves(tokenMelody);
}