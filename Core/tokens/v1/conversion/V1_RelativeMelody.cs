namespace Core.tokens.v1.conversion;

public class V1_RelativeMelody
{
    public abstract record V1_RelativeMelodyToken;
    public record V1_RelativeMelodyNote(int OctaveScaleNote, double Time, double Length, int Velocity) : V1_RelativeMelodyToken;
    public record V1_RelativeMelodyPassingTone(double Time, double Length, int Velocity) : V1_RelativeMelodyToken;

    public List<V1_RelativeMelodyToken> Tokens = [];
}
