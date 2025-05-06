using static Core.tokens.v1.V1_TokenMethods;

namespace Core.tokens.v1.conversion;

public class V1_TimedTokenMelody
{
    public abstract record V1_TimedTokenMelodyToken;
    public record V1_TimedTokenMelodyRest : V1_TimedTokenMelodyToken;
    public record V1_TimedTokenMelodyNote(int ScaleNote, int Velocity) : V1_TimedTokenMelodyToken;
    public record V1_TimedTokenMelodyPassingTone(int Velocity) : V1_TimedTokenMelodyToken;
    public record V1_TimedTokenMelodySpeed(V1_TokenSpeed Speed) : V1_TimedTokenMelodyToken;
    public record V1_TimedTokenMelodyMeasure : V1_TimedTokenMelodyToken;

    public List<V1_TimedTokenMelodyToken> Tokens = [];
}
