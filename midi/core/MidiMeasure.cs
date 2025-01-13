using System.Collections.Generic;

namespace thesis.midi.core;

public record struct MidiMeasure(List<MidiNote> Notes);
