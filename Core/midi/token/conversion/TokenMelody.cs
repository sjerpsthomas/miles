namespace Core.midi.token.conversion;

public class TokenMelody
{
    public abstract record TokenMelodyToken(double Time, double Length);
    public record TokenMelodyNote(int ScaleNote, double Time, double Length, int Velocity) : TokenMelodyToken(Time, Length);
    public record TokenMelodyPassingTone(double Time, double Length, int Velocity) : TokenMelodyToken(Time, Length);

    public List<TokenMelodyToken> Tokens = [];
}
