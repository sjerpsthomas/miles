namespace Core.midi.token.conversion;

public class VelocityTokenMelody
{
    public abstract record VelocityTokenMelodyToken;
    public record VelocityTokenMelodyRest : VelocityTokenMelodyToken;
    public record VelocityTokenMelodyNote(int ScaleNote, int Velocity) : VelocityTokenMelodyToken;
    public record VelocityTokenMelodyPassingTone(int Velocity) : VelocityTokenMelodyToken;
    public record VelocityTokenMelodySpeed(TokenMethods.TokenSpeed Speed) : VelocityTokenMelodyToken;
    public record VelocityTokenMelodyMeasure : VelocityTokenMelodyToken;

    public List<VelocityTokenMelodyToken> Tokens = [];
}
