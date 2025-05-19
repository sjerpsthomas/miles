using Core.midi;
using Core.models.tokens_v1;
using Core.tokens.v1;

namespace Core.algorithm.tokens_v1;

public class V1_TokenTransformerAlgorithm: IAlgorithm
{
    public GenericTransformer Transformer;
    public List<MidiNote> Notes = [];
    public LeadSheet LeadSheet;
    
    public void Initialize(MidiSong[] solos, LeadSheet leadSheet)
    {
         LeadSheet = leadSheet;

         Transformer = new GenericTransformer("250225_transformer.onnx");
    }

    public void Learn(List<MidiMeasure> measures, int startMeasureNum = 0)
    {
        for (var index = 0; index < measures.Count; index++)
            foreach (var note in measures[index].Notes)
                Notes.Add(note with { Time = note.Time + index + startMeasureNum });
    }

    public List<MidiMeasure> Generate(int generateMeasureCount = 4, int startMeasureNum = 0)
    {
        // Deduce and set tokens
        var tokens = V1_TokenMethods.V1_Tokenize(Notes, LeadSheet);
        Transformer.SetTokens(tokens);
        
        List<V1_Token> res = [];
        
        // Generate tokens
        var measureCount = 0;
        var tokenCount = 0;
        while (true)
        {
            // Generate token, add
            var newToken = Transformer.Generate();
            res.Add(newToken);
            tokenCount++;

            var measureTooLong = tokenCount > 10 && newToken != V1_Token.Measure;
            
            if (measureTooLong)
            {
                // Force measure token
                res.Add(V1_Token.Measure);
                Transformer.Append(V1_Token.Measure);
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
        }

        Console.WriteLine(V1_TokenMethods.V1_TokensToString(res));
        
        // Get notes from tokens, print
        var notes = V1_TokenMethods.V1_Reconstruct(res, LeadSheet, startMeasureNum);

        // Return
        return MidiSong.FromNotes(notes).Measures;
    }
}