using System.Collections.Generic;
using Core.midi;

namespace Program.midi.scheduler.component.solo;

public abstract class Soloist
{
    public abstract void Initialize(MidiSong solo, LeadSheet leadSheet);

    public abstract void IngestMeasures(List<MidiMeasure> measures, int startMeasureNum);

    public abstract List<MidiMeasure> Generate(int generateMeasureCount, int startMeasureNum);
}