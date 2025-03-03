using System.Text;
using NAudio.Midi;

namespace Core.midi;

public class MidiSong
{
    public List<MidiMeasure> Measures = [];

    public static MidiSong FromMidiFileStream(Stream stream)
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

    public static MidiSong FromNotesFileStream(Stream stream)
    {
        using var reader = new BinaryReader(stream, Encoding.UTF8, false);

        // Read count
        var count = reader.ReadInt32();
        
        // Read notes
        var notes = Enumerable.Range(0, count).Select(_ =>
            new MidiNote(
                OutputName: (OutputName)reader.ReadByte(),
                Time: reader.ReadDouble(),
                Length: reader.ReadDouble(),
                Note: reader.ReadInt32(),
                Velocity: reader.ReadInt32()
            )
        ).ToList();

        // Convert to Song
        return FromNotes(notes);
    }

    public void ToNotesFileStream(Stream stream)
    {
        using var writer = new BinaryWriter(stream, Encoding.UTF8, false);
        
        // Get notes
        var notes = ToNotes();
        
        // Write count
        writer.Write(notes.Count);
        
        // Write notes
        foreach (var note in notes)
        {
            writer.Write((byte)note.OutputName);
            writer.Write(note.Time);
            writer.Write(note.Length);
            writer.Write(note.Note);
            writer.Write(note.Velocity);
        }
    }
    
    public void Fill(int newMeasureCount)
    {
        while (Measures.Count < newMeasureCount) Measures.Add(new MidiMeasure([]));
    }
    
    public static MidiSong FromNotes(List<MidiNote> notes)
    {
        if (notes.Count == 0)
        {
            Console.WriteLine("No notes");
            return new MidiSong();
        }
        
        var measureCount = (int)Math.Truncate(notes[^1].Time) + 1;
        var measures = Enumerable.Range(0, measureCount)
            .Select(i => new MidiMeasure())
            .ToList();

        foreach (var note in notes)
        {
            var measureNum = (int)Math.Truncate(note.Time);
            measures[measureNum].Notes.Add(note with { Time = note.Time - measureNum });
        }

        return new MidiSong { Measures = measures };
    }

    public List<MidiNote> ToNotes()
    {
        return Measures.SelectMany((measure, measureNum) =>
            measure.Notes.Select(note => note with { Time = note.Time + measureNum })).ToList();
    }
}
