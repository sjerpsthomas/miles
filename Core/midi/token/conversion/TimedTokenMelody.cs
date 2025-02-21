namespace Core.midi.token.conversion;

public class TimedTokenMelody
{
    public abstract record TimedTokenMelodyToken;
    public record TimedTokenMelodyRest : TimedTokenMelodyToken;
    public record TimedTokenMelodyNote(int ScaleNote, int Velocity) : TimedTokenMelodyToken;
    public record TimedTokenMelodyPassingTone(int Velocity) : TimedTokenMelodyToken;
    public record TimedTokenMelodySpeed(TokenMethods.TokenSpeed Speed) : TimedTokenMelodyToken;
    public record TimedTokenMelodyMeasure : TimedTokenMelodyToken;

    public List<TimedTokenMelodyToken> Tokens = [];
}
