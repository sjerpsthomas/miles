using System;
using System.Collections.Generic;
using Godot;
using NAudio.Midi;
using thesis.util;

namespace thesis.midi.core;

public class MidiSong
{
    public List<MidiMeasure> Measures = [];

    public static MidiSong FromFile(string fileName)
    {
        var song = new MidiSong();
        
        var mf = new MidiFile(
            new FileAccessStream(fileName, FileAccess.ModeFlags.Read),
            false
        );
        
        for (var i = 0; i < mf.Tracks; i++)
        {
            var outputName = i switch
            {
                0 => MidiServer.OutputName.Algorithm,
                1 => MidiServer.OutputName.Backing1Bass,
                2 => MidiServer.OutputName.Backing2Piano,
                3 => MidiServer.OutputName.Backing3Guitar,
                4 => MidiServer.OutputName.Backing4Drums,
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
                // TODO: implement multiple tracks
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
