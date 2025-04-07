using System;
using System.Collections.Generic;
using System.Linq;
using Core.conversion;
using Core.midi;
using Core.midi.token;

namespace Program.midi.scheduler.component.solo.v2;

public class TokenFactorOracleSoloistV2 : Soloist
{
    private class TokenListComparer : IEqualityComparer<List<Token>>
    {
        public bool Equals(List<Token> x, List<Token> y) => x!.SequenceEqual(y!);

        public int GetHashCode(List<Token> list) =>
            list.Aggregate(17, (hash, token) => hash * 31 + token.GetHashCode());
    }
    
    // ReSharper disable once InconsistentNaming
    public FactorOracle<List<Token>> FO;
    public LeadSheet LeadSheet;

    public int HumanFourStart;
    
    public override void Initialize(MidiSong solo, LeadSheet leadSheet)
    {
        FO = new FactorOracle<List<Token>>(new TokenListComparer());
        
        IngestMeasures(solo.Measures, 0);
        
        LeadSheet = leadSheet;
    }

    public override void IngestMeasures(List<MidiMeasure> measures, int startMeasureNum)
    {
        HumanFourStart = FO.Nodes.Count;
        
        // Get notes (shifted by measure number)
        List<MidiNote> notes = [];
        for (var index = 0; index < measures.Count; index++)
            foreach (var note in measures[index].Notes)
                notes.Add(note with { Time = note.Time + index });
        
        // Get tokens, add to factor oracle
        var tokens = Conversion.TokenizeV2(notes, LeadSheet, startMeasureNum);

        const int n = 10;
        var chunks = tokens.TakeLast((tokens.Count / n) * n).Chunk(n).Select(it => it.ToList());
        FO.AddValues(chunks);
    }

    public override List<MidiMeasure> Generate(int generateMeasureCount, int startMeasureNum)
    {
        List<Token> res = [];
        
        // Traverse factor oracle
        var rng = new Random();
        var index = rng.Next(HumanFourStart, FO.Nodes.Count - 1);
        
        var measureCount = 0;
        var tokenCount = 0;
        while (true)
        {
            // Traverse
            var (newTokens, newIndex) = FO.Nodes[index].Traverse(index, rng);

            if (newIndex == -1)
                (newTokens, newIndex) = FO.Nodes[0].Traverse(0, rng);

            foreach (var newToken in newTokens)
            {
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
            }
            
            // Break if measure count reached
            if (measureCount == generateMeasureCount)
                break;
            
            // Iterate
            index = newIndex;
        }
        
        while (res is [Token.Measure, ..])
            res.RemoveAt(0);
        
        // Reconstruct, return notes
        var notes = Conversion.ReconstructV2(res, LeadSheet, startMeasureNum);
        return MidiSong.FromNotes(notes).Measures;
    }
}