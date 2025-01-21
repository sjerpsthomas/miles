using System;
using System.Collections.Generic;
using System.Linq;
using thesis.midi.core;

namespace thesis.midi.scheduler.component.solo;

public class SoloMidiSchedulerComponent : MidiSchedulerComponent
{
    public LeadSheet LeadSheet;

    public FactorOracle FactorOracle = new();

    private MidiMelody _melody;
    
    public SoloMidiSchedulerComponent(LeadSheet leadSheet, MidiSong solo)
    {
        LeadSheet = leadSheet;
        
        _melody = new MidiMelody(solo, leadSheet);

        foreach (var note in _melody.Melody)
            FactorOracle.AddNote(note);
    }
    
    public override void HandleMeasure(int currentMeasure)
    {
        if (currentMeasure == 0) return;
        if ((currentMeasure + 4) % 8 != 0) return;
        if (currentMeasure >= LeadSheet.Chords.Count) return;
        
        // Flush recording
        Recorder.Flush(currentMeasure);
        
        // Add notes to melody
        var recordedMeasures = Recorder.Song.Measures.TakeLast(4).ToList();
        var newNotes = _melody.GetNotes(recordedMeasures, LeadSheet, currentMeasure);
        _melody.Melody.AddRange(newNotes);
        
        // Train factor oracle
        foreach (var note in newNotes)
            FactorOracle.AddNote(note);
        
        // Create 4 new measures
        var measures = new List<MidiMeasure>();
        for (var i = 0; i < 4; i++)
            measures.Add(new MidiMeasure());
        
        // Traverse factor oracle until time runs out
        var rng = new Random();
        var time = 0.0;
        var index = FactorOracle.Nodes.Count - 10;
        
        while (time < 4.0)
        {
            // Traverse
            var (note, newIndex) = FactorOracle.Nodes[index].Traverse(index, rng);

            // Go back to start if finished
            if (note == null || newIndex >= FactorOracle.Nodes.Count)
                (note, newIndex) = FactorOracle.Nodes[0].Traverse(0, rng);
            
            // Add note to measure
            var measureNum = (int)Math.Truncate(time);
            var measure = measures[measureNum];

            var absoluteNote = LeadSheet.ChordAtTime(currentMeasure + time).GetAbsoluteNote(note.Note);
            var newNote = new MidiNote(MidiServer.OutputName.Algorithm, time - measureNum, note.Length, absoluteNote, note.Velocity);
            measure.Notes.Add(newNote);
            
            // Iterate
            index = newIndex;
            time += note.Length + note.RestLength;
        }
        
        // Schedule measures
        for (var i = 0; i < 4; i++)
            Scheduler.AddMeasure(currentMeasure + i, measures[i]);
    }
}