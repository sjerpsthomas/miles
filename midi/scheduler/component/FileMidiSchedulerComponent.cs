using System;
using System.Collections.Generic;
using Godot;
using NAudio.Midi;
using thesis.midi.core;
using thesis.util;

namespace thesis.midi.scheduler.component;

public class FileMidiSchedulerComponent : MidiSchedulerComponent
{
    public string FileName;
    
    public override void HandleMeasure(int currentMeasure)
    {
        // Only execute at first measure
        if (currentMeasure != 0) return;
        
        // Add all measures from file
        var song = FileToSong();
        Scheduler.AddSong(0, song);
    }

    private MidiSong FileToSong()
    {
        var song = new MidiSong([]);
        
        var mf = new MidiFile(
            new FileAccessStream(FileName, FileAccess.ModeFlags.Read),
            false
        );
        
        for (var i = 0; i < mf.Tracks; i++)
        {
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
                var note = new MidiNote(time - measureNum, length, noteOnEvent.NoteNumber, noteOnEvent.Velocity);
                
                // Add note to measure
                var measure = song.Measures[measureNum];
                measure.Notes.Add(note);
            }
        }

        return song;
    }
}