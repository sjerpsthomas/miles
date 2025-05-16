using System.Linq;
using Core.algorithm;
using Core.midi;

namespace Program.midi.scheduler.component;

public class AlgorithmMidiSchedulerComponent : MidiSchedulerComponent
{
    public LeadSheet LeadSheet;

    public IAlgorithm Algorithm;

    public int Repetitions;
    
    public AlgorithmMidiSchedulerComponent(MidiSong[] solos, LeadSheet leadSheet, IAlgorithm algorithm)
    {
        LeadSheet = leadSheet;
        Algorithm = algorithm;
        
        Algorithm.Initialize(solos, LeadSheet);
    }
    
    public override void HandleMeasure(int currentMeasure)
    {
        if (currentMeasure == 0) return;
        if ((currentMeasure + 4) % 8 != 0) return;
        if (currentMeasure >= LeadSheet.Chords.Count * Repetitions) return;

        const int recordMeasureCount = 4;
        const int generateMeasureCount = 4;
        
        // Flush recording
        Recorder.Flush(currentMeasure);
        
        // Get recorded measures, ingest
        var recordedMeasures = Recorder.GetUserMeasures(recordMeasureCount).ToList();
        Algorithm.IngestMeasures(recordedMeasures, currentMeasure - recordMeasureCount);

        var measures = Algorithm.Generate(generateMeasureCount, currentMeasure);
        
        // Schedule measures
        for (var i = 0; i < measures.Count; i++)
            Scheduler.AddMeasure(currentMeasure + i, measures[i]);
    }
}