using System.Linq;
using Core.midi;

namespace Program.midi.scheduler.component;

public class SongMidiSchedulerComponent : MidiSchedulerComponent
{
    public MidiSong Song;

    public int Repetitions = 1;
    
    public override void HandleMeasure(int currentMeasure)
    {
        var songLength = Song.Measures.Count;
        
        if (currentMeasure < 0) return;
        if (currentMeasure > songLength * Repetitions) return;

        // Schedule first 8th of last measure at the end
        if (currentMeasure == songLength * Repetitions)
        {
            var lastMeasure = new MidiMeasure(Song.Measures[0].Notes.Where(it => it.Time < 0.1));
            Scheduler.AddMeasure(songLength * Repetitions, lastMeasure);
            return;
        }
        
        Scheduler.AddMeasure(currentMeasure, Song.Measures[currentMeasure % songLength]);
    }
}