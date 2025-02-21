using static Core.midi.token.TokenMethods;

namespace Core.midi.token.conversion;

public class RelativeMelody
{
    public abstract record RelativeMelodyToken;
    public record RelativeMelodyNote(int OctaveScaleNote, double Time, double Length, int Velocity) : RelativeMelodyToken;
    public record RelativeMelodyPassingTone(double Time, double Length, int Velocity) : RelativeMelodyToken;

    public List<RelativeMelodyToken> Tokens = [];
}
