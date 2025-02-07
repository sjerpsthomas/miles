namespace Core.midi;

public class MidiMelody
{
    public record MelodyNote(int Note, int Velocity, double Length, double RestLength);

    public List<MelodyNote> Melody = [];

    public static MidiMelody FromMeasures(List<MidiMeasure> measures, LeadSheet leadSheet, int measureNum = 0)
    {
        // Get notes from song info, sort by time
        var notes = measures
            .SelectMany((measure, i) =>
            {
                return measure.Notes.Select(note => (
                    note: leadSheet.ChordAtTime(note.Time + i + measureNum).GetRelativeNote(note.Note),
                    velocity: note.Velocity,
                    time: note.Time + i,
                    length: note.Length));
            })
            .OrderBy(tuple => tuple.time).ToList();

        // Check for empty melody
        if (notes is []) return new MidiMelody();
        
        List<MelodyNote> melody = [];
        
        // Create melody
        for (var index = 0; index < notes.Count - 1; index++)
        {
            var note = notes[index];
            var nextNote = notes[index + 1];
            
            var newLength = Math.Min(note.length, nextNote.time - note.time);
            var restTime = nextNote.time - (note.time + newLength);
            
            melody.Add(new MelodyNote(note.note, note.velocity, newLength, restTime));
        }
        
        // Add last note
        // TODO: what rest length should we take?
        var lastNote = notes[^1];
        melody.Add(new MelodyNote(lastNote.note, lastNote.velocity, lastNote.length, 0));

        return new MidiMelody() { Melody = melody };
    }

    public static MidiMelody operator +(MidiMelody a, MidiMelody b)
    {
        var newMelody = new List<MelodyNote>();
        newMelody.AddRange(a.Melody);
        newMelody.AddRange(b.Melody);
        
        return new MidiMelody { Melody = newMelody };
    }
}