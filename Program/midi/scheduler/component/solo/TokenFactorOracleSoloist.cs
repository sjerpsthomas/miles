using System;
using System.Collections.Generic;
using System.Linq;
using Core.midi;
using Core.midi.token;
using Godot;

namespace Program.midi.scheduler.component.solo;

public class TokenFactorOracleSoloist : Soloist
{
    public TokenFactorOracle TokenFactorOracle;
    public LeadSheet LeadSheet;
    
    public override void Initialize(MidiSong solo, LeadSheet leadSheet)
    {
        TokenFactorOracle = new();
        
        IngestMeasures(solo.Measures, 0);
        
        LeadSheet = leadSheet;
    }

    public override void IngestMeasures(List<MidiMeasure> measures, int startMeasureNum)
    {
        // Get notes (shifted by measure number)
        List<MidiNote> notes = [];
        for (var index = 0; index < measures.Count; index++)
            foreach (var note in measures[index].Notes)
                notes.Add(note with { Time = note.Time + index });
        
        // Get tokens, add to factor oracle
        var tokens = TokenMethods.Tokenize(notes);
        TokenFactorOracle.AddTokens(tokens);
    }

    public override List<MidiMeasure> Generate(int generateMeasureCount, int startMeasureNum)
    {
        List<Token> res = [];
        
        // Traverse factor oracle
        var rng = new Random();
        var index = rng.Next(0, TokenFactorOracle.Nodes.Count - 1);
        
        var measureCount = 0;
        var tokenCount = 0;
        while (true)
        {
            // Traverse
            var (newToken, newIndex) = TokenFactorOracle.Nodes[index].Traverse(index, rng);

            if (newIndex == -1)
                (newToken, newIndex) = TokenFactorOracle.Nodes[0].Traverse(0, rng);
            
            // Generate token, add
            res.Add(newToken);
            tokenCount++;

            var measureTooLong = tokenCount > 10 && newToken != Token.Measure;
            
            if (measureTooLong)
            {
                // Force measure token
                res.Add(Token.Measure);
            }
            
            if (newToken == Token.Measure || measureTooLong)
            {
                // Advance measure
                measureCount++;
                tokenCount = 0;

                // Break if measure count reached
                if (measureCount == generateMeasureCount)
                    break;
            }
            
            // Iterate
            index = newIndex;
        }
        
        while (res is [Token.Measure, ..])
            res.RemoveAt(0);
        
        // Get notes from tokens, print
        var notes = TokenMethods.Reconstruct(res, LeadSheet, startMeasureNum);
        GD.Print(string.Join(',', notes.Select(it => it.Note.ToString())));

        // Return
        return MidiSong.FromNotes(notes).Measures;
    }
}