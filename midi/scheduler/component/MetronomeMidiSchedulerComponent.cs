using thesis.midi.core;

namespace thesis.midi.scheduler.component;

public class MetronomeMidiSchedulerComponent : MidiSchedulerComponent
{
    public override void HandleMeasure(int currentMeasure)
    {
        Scheduler.AddMeasure(measureNum: currentMeasure, new MidiMeasure([
            new MidiNote(Time: 0.00, Length: 0.125, Note: 79, Velocity: 70),
            new MidiNote(Time: 0.25, Length: 0.125, Note: 79, Velocity: 30),
            new MidiNote(Time: 0.50, Length: 0.125, Note: 79, Velocity: 30),
            new MidiNote(Time: 0.75, Length: 0.125, Note: 79, Velocity: 30)
        ]));
    }
}
