namespace Core.tokens.v2.conversion;

public class V2_TokenMelody
{
    public abstract record V2_TokenMelodyToken(double Time, double Length);
    public record V2_TokenMelodyNote(int ScaleNote, double Time, double Length, int Velocity) : V2_TokenMelodyToken(Time, Length);
    public record V2_TokenMelodyPassingTone(double Time, double Length, int Velocity) : V2_TokenMelodyToken(Time, Length);

    public List<V2_TokenMelodyToken> Tokens = [];
}
