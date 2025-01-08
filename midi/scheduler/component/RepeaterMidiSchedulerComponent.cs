using Godot;

namespace thesis.midi.scheduler.component;

public class RepeaterMidiSchedulerComponent : MidiSchedulerComponent
{
    public override void HandleMeasure(int currentMeasure)
    {
        if ((currentMeasure + 2) % 4 == 0)
        {
            GD.Print(Scheduler);
            GD.Print(Recorder);
            
            Scheduler.AddMeasure(measure: currentMeasure, Recorder.Measures[^2]);
            Scheduler.AddMeasure(measure: currentMeasure + 1, Recorder.Measures[^1]);
        }
    }
}