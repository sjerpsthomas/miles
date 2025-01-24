using thesis.midi.core;
namespace thesis.midi.scheduler.component;

public class SongMidiSchedulerComponent : MidiSchedulerComponent
{
    public MidiSong Song;
    
    public override void HandleMeasure(int currentMeasure)
    {
        // Add all measures from file at start
        if (currentMeasure == 0)
            Scheduler.AddSong(0, Song);
    }
}