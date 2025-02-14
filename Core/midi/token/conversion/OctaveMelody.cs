using static Core.midi.token.TokenMethods;

namespace Core.midi.token.conversion;

public class OctaveMelody
{
    public abstract record OctaveMelodyToken;
    public record OctaveMelodyNote(int OctaveScaleNote, double Time, double Length, int Velocity) : OctaveMelodyToken;
    public record OctaveMelodyPassingTone(double Time, double Length, int Velocity) : OctaveMelodyToken;

    public List<OctaveMelodyToken> Tokens = [];
}
