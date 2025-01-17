using System.Collections.Generic;
using System.Linq;

namespace thesis.midi.core;

public class MidiMeasure(params MidiNote[] notes)
{
    public List<MidiNote> Notes = notes.ToList();
}
