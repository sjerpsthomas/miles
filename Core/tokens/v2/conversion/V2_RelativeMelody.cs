namespace Core.tokens.v2.conversion;

public class V2_RelativeMelody
{
    public record V2_RelativeMelodyToken(int OctaveScaleNote, double Time, double Length, int Velocity);

    public List<V2_RelativeMelodyToken> Tokens = [];
}
