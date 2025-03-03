using System.Linq;

namespace Program.midi.scheduler.component;

public class RepeaterMidiSchedulerComponent : MidiSchedulerComponent
{
    public override void HandleMeasure(int currentMeasure)
    {
        if ((currentMeasure + 2) % 4 != 0) return;
        
        Recorder.Flush(currentMeasure);

        // Repeat user measures
        var userMeasures = Recorder.GetUserMeasures(2).ToArray();
        Scheduler.AddMeasure(measureNum: currentMeasure, userMeasures[0]);
        Scheduler.AddMeasure(measureNum: currentMeasure + 1, userMeasures[1]);
    }
}