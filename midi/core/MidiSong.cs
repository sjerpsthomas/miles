using System.Collections.Generic;
using System.Linq;

namespace thesis.midi.core;

public record struct MidiSong(List<MidiMeasure> Measures)
{
    public void Fill(int newMeasureCount)
    {
        while (Measures.Count < newMeasureCount) Measures.Add(new MidiMeasure([]));
    }
}
