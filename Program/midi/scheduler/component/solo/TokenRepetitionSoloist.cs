using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using Core.midi;
using Core.midi.token;
using Godot;

namespace Program.midi.scheduler.component.solo;

public class TokenRepetitionSoloist: Soloist
{
    public List<MidiNote> Notes;
    public LeadSheet LeadSheet;

    private void AddMeasures(List<MidiMeasure> measures)
    {
        for (var index = 0; index < measures.Count; index++)
        {
            var measure = measures[index];
            foreach (var note in measure.Notes)
                Notes.Add(note with { Time = note.Time + index });
        }
    }
    
    public override void Initialize(MidiSong solo, LeadSheet leadSheet)
    {
        // Nothing
        LeadSheet = leadSheet;
    }

    public override void IngestMeasures(List<MidiMeasure> measures, int startMeasureNum)
    {
        // Get all notes
        Notes = [];
        AddMeasures(measures);
    }

    public override List<MidiMeasure> Generate(int generateMeasureCount, int startMeasureNum)
    {
        var tokens = TokenMethods.Tokenize(Notes, LeadSheet);
        Console.WriteLine(TokenMethods.TokensToString(tokens));
        
        var notes = TokenMethods.Reconstruct(tokens, LeadSheet, startMeasureNum);
        
        return MidiSong.FromNotes(notes).Measures;
    }
}