using Core.midi;
using Core.models.tokens_v2;
using Core.tokens.v2;

namespace Core.algorithm.tokens_v2;

public class V2_TokenFactorOracleAlgorithm : IAlgorithm
{
    // ReSharper disable once InconsistentNaming
    public GenericFactorOracle<List<V2_Token>> FO;
    public LeadSheet LeadSheet;

    public int HumanFourStart;
    
    public void Initialize(MidiSong[] solos, LeadSheet leadSheet)
    {
        FO = new GenericFactorOracle<List<V2_Token>>(new V2_TokenListComparer());
        
        IngestMeasures(solos[0].Measures, 0);
        
        LeadSheet = leadSheet;
    }

    public void IngestMeasures(List<MidiMeasure> measures, int startMeasureNum)
    {
        HumanFourStart = FO.Nodes.Count;
        
        // Get notes (shifted by measure number)
        List<MidiNote> notes = [];
        for (var index = 0; index < measures.Count; index++)
            foreach (var note in measures[index].Notes)
                notes.Add(note with { Time = note.Time + index });
        
        // Get tokens, add to factor oracle
        var tokens = V2_TokenMethods.V2_Tokenize(notes, LeadSheet, startMeasureNum);

        const int n = 8;
        var chunks = tokens.TakeLast((tokens.Count / n) * n).Chunk(n).Select(it => it.ToList());
        FO.AddValues(chunks);
    }

    public List<MidiMeasure> Generate(int generateMeasureCount, int startMeasureNum)
    {
        List<V2_Token> res = [];
        
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

                var measureTooLong = tokenCount > 10 && newToken != V2_Token.Measure;
            
                if (measureTooLong)
                {
                    // Force measure token
                    res.Add(V2_Token.Measure);
                }
            
                if (newToken == V2_Token.Measure || measureTooLong)
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
        
        // ReSharper disable once LoopVariableIsNeverChangedInsideLoop
        while (res is [V2_Token.Measure, ..])
            res.RemoveAt(0);
        
        // Reconstruct, return notes
        var notes = V2_TokenMethods.V2_Reconstruct(res, LeadSheet, startMeasureNum);
        return MidiSong.FromNotes(notes).Measures;
    }
}