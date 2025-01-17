namespace thesis.midi.core;

public record struct MidiNote(MidiServer.OutputName OutputName, double Time, double Length, int Note, int Velocity);
