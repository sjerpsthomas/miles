using System.Linq;
using Core.midi;

namespace Program.midi.scheduler.component;

public class SongMidiSchedulerComponent : MidiSchedulerComponent
{
    public MidiSong Song;

    public int Repetitions = 1;
    
    public override void HandleMeasure(int currentMeasure)
    {
        // Early return if not song start
        if (currentMeasure != 0) return;
        
        // Add all measures from file at start
        var songLength = Song.Measures.Count;
        for (var i = 0; i < Repetitions; i++)
        {
            Scheduler.AddSong(songLength * i, Song);
        }
        
        // Append first 8th of last measure
        var lastMeasure = new MidiMeasure(Song.Measures[0].Notes.Where(it => it.Time < 0.1));
        Scheduler.AddMeasure(songLength * Repetitions, lastMeasure);
    }
}