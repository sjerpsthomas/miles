namespace Core.tokens.v2.conversion;

public class V2_RelativeMelody
{
    public abstract record V2_RelativeMelodyToken;
    public record V2_RelativeMelodyNote(int OctaveScaleNote, double Time, double Length, int Velocity) : V2_RelativeMelodyToken;
    public record V2_RelativeMelodyPassingTone(double Time, double Length, int Velocity) : V2_RelativeMelodyToken;

    public List<V2_RelativeMelodyToken> Tokens = [];
}
