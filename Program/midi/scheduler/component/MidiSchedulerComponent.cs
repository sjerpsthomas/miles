using MidiRecorder = Program.midi.recorder.MidiRecorder;

namespace Program.midi.scheduler.component;

public abstract class MidiSchedulerComponent
{
    public MidiScheduler Scheduler;
    public MidiRecorder Recorder;
    
    public abstract void HandleMeasure(int currentMeasure);
}
