namespace thesis.midi.scheduler.component;

public class RepeaterMidiSchedulerComponent : MidiSchedulerComponent
{
    public override void HandleMeasure(int currentMeasure)
    {
        if ((currentMeasure + 2) % 4 != 0) return;
        
        Recorder.Flush(currentMeasure);
            
        Scheduler.AddMeasure(measureNum: currentMeasure, Recorder.Song.Measures[^2]);
        Scheduler.AddMeasure(measureNum: currentMeasure + 1, Recorder.Song.Measures[^1]);
    }
}