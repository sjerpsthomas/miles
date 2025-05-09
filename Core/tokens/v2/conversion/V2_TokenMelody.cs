namespace Core.tokens.v2.conversion;

public class V2_TokenMelody
{
    public record V2_TokenMelodyToken(int ScaleNote, double Time, double Length, int Velocity);

    public List<V2_TokenMelodyToken> Tokens = [];
}
