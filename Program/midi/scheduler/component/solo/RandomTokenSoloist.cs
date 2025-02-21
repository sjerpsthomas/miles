using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using Core.midi;
using Core.midi.token;
using Godot;
using Microsoft.VisualBasic.CompilerServices;

namespace Program.midi.scheduler.component.solo;

public class RandomTokenSoloist: Soloist
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
        var rng = new Random();
        
        Token GetToken()
        {
            return rng.NextSingle() switch
            {
                < 0.15f => Token.Rest,
                < 0.40f => (Token)rng.Next(0, 8),
                < 0.60f => Token.PassingTone,
                < 0.65f => Token.SuperFast,
                < 0.72f => Token.Fast,
                < 0.79f => Token.Slow,
                < 0.86f => Token.SuperSlow,
                < 0.93f => Token.Loud,
                _ =>       Token.Quiet
            };
        }

        List<Token> tokens = [];
        for (var i = 0; i < generateMeasureCount; i++)
        {
            var measureTokenAmount = rng.Next(4, 8);
            tokens.AddRange(Enumerable.Range(0, measureTokenAmount).Select(_ => GetToken()));
            tokens.Add(Token.Measure);
        }
        GD.Print(TokenMethods.TokensToString(tokens));
        
        var notes = TokenMethods.Reconstruct(tokens, LeadSheet, startMeasureNum);
        GD.Print(string.Join(',', notes.Select(it => it.Note.ToString())));
        
        return MidiSong.FromNotes(notes).Measures;
    }
}