using Core.midi;
using Core.models.tokens_v1;
using Core.tokens.v1;

namespace Core.algorithm.tokens_v1;

public class V1_TokenFactorOracleAlgorithm : IAlgorithm
{
    public V1_TokenFactorOracle TokenFactorOracle = new();
    public LeadSheet LeadSheet;

    public int HumanFourStart;
    
    public void Initialize(MidiSong[] solos, LeadSheet leadSheet)
    {
        Learn(solos[0].Measures, 0);
        
        LeadSheet = leadSheet;
    }

    public void Learn(List<MidiMeasure> measures, int startMeasureNum = 0)
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

    public List<MidiMeasure> Generate(int generateMeasureCount = 4, int startMeasureNum = 0)
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