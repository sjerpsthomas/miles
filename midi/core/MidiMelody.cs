using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using OutputName = thesis.midi.MidiServer.OutputName;

using TempNoteList = System.Collections.Generic.List<(int note, int velocity, double time, double length)>;

namespace thesis.midi.core;

public class MidiMelody
{
    public record class MelodyNote(int Note, int Velocity, double Length, double RestLength);

    public List<MelodyNote> Melody = [];

    public MidiMelody(MidiSong song, SongInfo songInfo)
    {
        Melody = GetNotes(song.Measures, songInfo, 0);
    }

    public List<MelodyNote> GetNotes(List<MidiMeasure> measures, SongInfo songInfo, int measureNum)
    {
        // Get notes from song info, sort by time
        var notes = measures.Zip(songInfo.Info.Skip(measureNum))
            .SelectMany((tuple, i) =>
            {
                var (measure, info) = tuple;
                return measure.Notes.Select(note =>
                    (note: info.GetRelativeNote(note.Note), velocity: note.Velocity, time: note.Time + i,
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
            
            var newLength = Math.Max(note.length, nextNote.time - note.time);
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