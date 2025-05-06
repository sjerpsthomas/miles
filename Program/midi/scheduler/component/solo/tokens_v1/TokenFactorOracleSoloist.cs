using System;
using System.Collections.Generic;
using Core.midi;
using Core.models.tokens_v1;
using Core.tokens.v1;

namespace Program.midi.scheduler.component.solo.tokens_v1;

public class TokenFactorOracleSoloist : Soloist
{
    public V1_TokenFactorOracle TokenFactorOracle;
    public LeadSheet LeadSheet;

    public int HumanFourStart;
    
    public override void Initialize(MidiSong solo, LeadSheet leadSheet)
    {
        TokenFactorOracle = new();
        
        IngestMeasures(solo.Measures, 0);
        
        LeadSheet = leadSheet;
    }

    public override void IngestMeasures(List<MidiMeasure> measures, int startMeasureNum)
    {
        HumanFourStart = TokenFactorOracle.Nodes.Count;
        
        // Get notes (shifted by measure number)
        List<MidiNote> notes = [];
        for (var index = 0; index < measures.Count; index++)
            foreach (var note in measures[index].Notes)
                notes.Add(note with { Time = note.Time + index });
        
        // Get tokens, add to factor oracle
        var tokens = V1_TokenMethods.V1_Tokenize(notes, LeadSheet);
        TokenFactorOracle.AddTokens(tokens);
    }

    public override List<MidiMeasure> Generate(int generateMeasureCount, int startMeasureNum)
    {
        List<V1_Token> res = [];
        
        // Traverse factor oracle
        var rng = new Random();
        var index = rng.Next(HumanFourStart, TokenFactorOracle.Nodes.Count - 1);
        
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

            var measureTooLong = tokenCount > 10 && newToken != V1_Token.Measure;
            
            if (measureTooLong)
            {
                // Force measure token
                res.Add(V1_Token.Measure);
            }
            
            if (newToken == V1_Token.Measure || measureTooLong)
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
        
        // ReSharper disable once LoopVariableIsNeverChangedInsideLoop
        while (res is [V1_Token.Measure, ..])
            res.RemoveAt(0);
        
        // Reconstruct, return notes
        var notes = V1_TokenMethods.V1_Reconstruct(res, LeadSheet, startMeasureNum);
        return MidiSong.FromNotes(notes).Measures;
    }
}