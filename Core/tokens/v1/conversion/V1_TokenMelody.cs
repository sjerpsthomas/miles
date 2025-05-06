namespace Core.tokens.v1.conversion;

public class V1_TokenMelody
{
    public abstract record V1_TokenMelodyToken(double Time, double Length);
    public record V1_TokenMelodyNote(int ScaleNote, double Time, double Length, int Velocity) : V1_TokenMelodyToken(Time, Length);
    public record V1_TokenMelodyPassingTone(double Time, double Length, int Velocity) : V1_TokenMelodyToken(Time, Length);

    public List<V1_TokenMelodyToken> Tokens = [];
}
