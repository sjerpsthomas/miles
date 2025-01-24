using System;
using System.Collections.Generic;
using System.Linq;
using thesis.midi.core;

namespace thesis.midi.scheduler.component.solo;

public class SoloMidiSchedulerComponent : MidiSchedulerComponent
{
    public LeadSheet LeadSheet;

    public Soloist Soloist;
    
    public SoloMidiSchedulerComponent(MidiSong solo, LeadSheet leadSheet, Soloist soloist)
    {
        LeadSheet = leadSheet;
        Soloist = soloist;
        
        Soloist.Initialize(solo, LeadSheet);
    }
    
    public override void HandleMeasure(int currentMeasure)
    {
        if (currentMeasure == 0) return;
        if ((currentMeasure + 4) % 8 != 0) return;
        if (currentMeasure >= LeadSheet.Chords.Count) return;

        var recordMeasureCount = 4;
        var generateMeasureCount = 4;
        
        // Flush recording
        Recorder.Flush(currentMeasure);
        
        // Get recorded measures, ingest
        var recordedMeasures = Recorder.Song.Measures.TakeLast(recordMeasureCount).ToList();
        Soloist.IngestMeasures(recordedMeasures, currentMeasure - recordMeasureCount);

        var measures = Soloist.Generate(generateMeasureCount, currentMeasure);
        
        // Schedule measures
        for (var i = 0; i < generateMeasureCount; i++)
            Scheduler.AddMeasure(currentMeasure + i, measures[i]);
    }
}