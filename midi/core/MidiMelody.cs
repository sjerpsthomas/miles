using System;
using System.Collections.Generic;
using System.Linq;

namespace thesis.midi.core;

public class MidiMelody
{
    public record class MelodyNote(int Note, int Velocity, double Length, double RestLength);

    public List<MelodyNote> Melody;

    public MidiMelody(MidiSong song, LeadSheet leadSheet)
    {
        Melody = GetNotes(song.Measures, leadSheet, 0);
    }

    public List<MelodyNote> GetNotes(List<MidiMeasure> measures, LeadSheet leadSheet, int measureNum)
    {
        // Get notes from song info, sort by time
        var notes = measures
            .SelectMany((measure, i) =>
            {
                return measure.Notes.Select(note => (
                    note: leadSheet.ChordAtTime(note.Time + i).GetRelativeNote(note.Note),
                    velocity: note.Velocity,
                    time: note.Time + i,
                    length: note.Length));
            })
            .OrderBy(tuple => tuple.time).ToList();

        // Check for empty melody
        if (notes is []) return [];
        
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

        return melody;
    }
}