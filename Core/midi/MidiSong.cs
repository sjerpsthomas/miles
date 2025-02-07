using NAudio.Midi;

namespace Core.midi;

public class MidiSong
{
    public List<MidiMeasure> Measures = [];

    public static MidiSong FromFile(Stream stream)
    {
        var song = new MidiSong();
        
        var mf = new MidiFile(
            stream,
            false
        );
        
        for (var i = 0; i < mf.Tracks; i++)
        {
            var outputName = i switch
            {
                0 => OutputName.Algorithm,
                1 => OutputName.Backing1Bass,
                2 => OutputName.Backing2Piano,
                3 => OutputName.Backing3Keyboard,
                4 => OutputName.Backing4Drums,
                _ => throw new ArgumentOutOfRangeException()
            };
            
            foreach (var midiEvent in mf.Events[i])
            {
                // Check if event is note on event
                if (midiEvent is not NoteOnEvent noteOnEvent) continue;
                
                // Compute time and length
                var time = (double)noteOnEvent.AbsoluteTime / (mf.DeltaTicksPerQuarterNote * 4);
                var length = (double)noteOnEvent.NoteLength / (mf.DeltaTicksPerQuarterNote * 4);

                // Add necessary empty measures
                var measureNum = (int)Math.Truncate(time);
                song.Fill(measureNum + 1);

                // Create new note
                var note = new MidiNote(outputName, time - measureNum, length, noteOnEvent.NoteNumber, noteOnEvent.Velocity);
                
                // Add note to measure
                var measure = song.Measures[measureNum];
                measure.Notes.Add(note);
            }
        }

        return song;
    }
    
    public void Fill(int newMeasureCount)
    {
        while (Measures.Count < newMeasureCount) Measures.Add(new MidiMeasure([]));
    }
    
    
}
