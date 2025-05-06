using static Core.tokens.v2.V2_TokenMethods;

namespace Core.tokens.v2.conversion;

public class V2_TimedTokenMelody
{
    public abstract record V2_TimedTokenMelodyToken;
    public record V2_TimedTokenMelodyRest : V2_TimedTokenMelodyToken;
    public record V2_TimedTokenMelodyNote(int ScaleNote, int Velocity) : V2_TimedTokenMelodyToken;
    public record V2_TimedTokenMelodyPassingTone(int Velocity) : V2_TimedTokenMelodyToken;
    public record V2_TimedTokenMelodySpeed(V2_TokenSpeed Speed) : V2_TimedTokenMelodyToken;
    public record V2_TimedTokenMelodyMeasure : V2_TimedTokenMelodyToken;

    public List<V2_TimedTokenMelodyToken> Tokens = [];
}
