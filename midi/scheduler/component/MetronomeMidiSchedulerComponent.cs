namespace thesis.midi.scheduler.component;

using MeasureData = MidiScheduler.MeasureData;
using NoteData = MidiScheduler.NoteData;

public class MetronomeMidiSchedulerComponent : MidiSchedulerComponent
{
    public override void HandleMeasure(int currentMeasure)
    {
        Scheduler.AddMeasure(measure: currentMeasure, new MeasureData(
            new NoteData(Time: 0.00, Length: 0.125, Note: 79, Velocity: 70),
            new NoteData(Time: 0.25, Length: 0.125, Note: 79, Velocity: 30),
            new NoteData(Time: 0.50, Length: 0.125, Note: 79, Velocity: 30),
            new NoteData(Time: 0.75, Length: 0.125, Note: 79, Velocity: 30)
        ));
    }
}
