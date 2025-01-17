using thesis.midi.core;
namespace thesis.midi.scheduler.component;

public class FileMidiSchedulerComponent : MidiSchedulerComponent
{
    public string FileName;
    
    public override void HandleMeasure(int currentMeasure)
    {
        // Add all measures from file at start
        if (currentMeasure == 0)
            Scheduler.AddSong(0, MidiSong.FromFile(FileName));
    }
}