using System;
using System.Collections.Generic;
using Godot;
using NAudio.Midi;
using thesis.util;

using MeasureData = thesis.midi.scheduler.MidiScheduler.MeasureData;
using NoteData = thesis.midi.scheduler.MidiScheduler.NoteData;

namespace thesis.midi.scheduler.component;

public class FileMidiSchedulerComponent : MidiSchedulerComponent
{
    public string FileName;
    
    public override void HandleMeasure(int currentMeasure)
    {
        // Only execute at first measure
        if (currentMeasure != 0) return;
        
        // Add all measures from file
        var measures = FileToMeasures();
        for (var index = 0; index < measures.Count; index++)
            Scheduler.AddMeasure(index, measures[index]);
    }

    private List<MeasureData> FileToMeasures()
    {
        List<MeasureData> measures = [];
        
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
                var time = ConvertTime(noteOnEvent.AbsoluteTime, mf.DeltaTicksPerQuarterNote);
                var length = ConvertTime(noteOnEvent.NoteLength, mf.DeltaTicksPerQuarterNote);

                // Add necessary empty measures
                var measureNum = (int)Math.Truncate(time);
                while (measures.Count <= measureNum)
                    measures.Add(new MeasureData());

                // Create new note
                var note = new NoteData(time - measureNum, length, noteOnEvent.NoteNumber, noteOnEvent.Velocity);
                
                // Add note to measure
                var measure = measures[measureNum];
                measure.Notes.Add(note);
            }
        }

        return measures;
    }

    private double ConvertTime(long eventTime, int ticksPerQuarterNote) =>
        (double)eventTime / (ticksPerQuarterNote * 4);
}