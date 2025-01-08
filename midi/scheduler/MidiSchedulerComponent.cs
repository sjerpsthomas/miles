namespace thesis.midi.scheduler;

public abstract class MidiSchedulerComponent(MidiScheduler scheduler)
{
    public MidiScheduler Scheduler = scheduler;
    
    public abstract void HandleMeasure(int currentMeasure);
}
