using System.Collections.Generic;
using System.Linq;

namespace thesis.midi.core;

public class MidiMeasure
{
    public List<MidiNote> Notes;

    public MidiMeasure(params MidiNote[] notes)
    {
        Notes = notes.ToList();
    }

    public MidiMeasure(IEnumerable<MidiNote> notes)
    {
        Notes = notes.ToList();
    }
}
