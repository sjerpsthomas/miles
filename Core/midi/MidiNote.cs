namespace Core.midi;

public record struct MidiNote(OutputName OutputName, double Time, double Length, int Note, int Velocity);
