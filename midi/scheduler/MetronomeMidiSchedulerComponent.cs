namespace thesis.midi.scheduler;

using MeasureData = MidiScheduler.MeasureData;
using NoteData = MidiScheduler.NoteData;

public class MetronomeMidiSchedulerComponent(MidiScheduler scheduler) : MidiSchedulerComponent(scheduler)
{
    public override void HandleMeasure(int currentMeasure)
    {
        Scheduler.AddMeasure(measure: currentMeasure, new MeasureData(
            new NoteData(RelativeTime: 0.00, Length: 0.125, Note: 70, Velocity: 100),
            new NoteData(RelativeTime: 0.25, Length: 0.125, Note: 70, Velocity: 100),
            new NoteData(RelativeTime: 0.50, Length: 0.125, Note: 70, Velocity: 100),
            new NoteData(RelativeTime: 0.75, Length: 0.125, Note: 70, Velocity: 100)
        ));
    }
}
