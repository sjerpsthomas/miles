using Core.midi;
using Core.models.tokens_v2;
using Core.tokens.v2;

namespace Core.algorithm.tokens_v2;

public class V2_TokenNeuralNetAlgorithm: IAlgorithm
{
    public V2_TokenNeuralNet NeuralNet;
    public LeadSheet LeadSheet;

    public List<V2_Token> Tokens = [];
    
    public void Initialize(MidiSong[] solos, LeadSheet leadSheet)
    {
         LeadSheet = leadSheet;

         NeuralNet = new V2_TokenNeuralNet();
         
         Learn(solos[0].Measures);
         
         // TODO: Load from user://
         NeuralNet.Load(@"C:\Users\thoma\Desktop\tokens_temp\neural_net");
    }

    public void Learn(List<MidiMeasure> measures, int startMeasureNum = 0)
    {
        List<MidiNote> notes = [];
        
        for (var index = 0; index < measures.Count; index++)
            foreach (var note in measures[index].Notes)
                notes.Add(note with { Time = note.Time + index + startMeasureNum });
        
        Tokens.AddRange(V2_TokenMethods.V2_Tokenize(notes, LeadSheet, startMeasureNum));
    }


    public List<MidiMeasure> Generate(int generateMeasureCount = 4, int startMeasureNum = 0)
    {
        List<V2_Token> res = [];
        
        // Generate tokens
        var measureCount = 0;

        while (true)
        {
            var lastTokens = Tokens.TakeLast(V2_TokenNeuralNet.ContextWindow).ToList();
            var lastTokensTensor = NeuralNet.TokensToTensor(lastTokens);

            // Console.WriteLine(TokenMethods.TokensToString(lastTokens));
            
            foreach (var token in NeuralNet.Infer(lastTokensTensor))
            {
                // Increase token count
                res.Add(token);
                Tokens.Add(token);

                // Advance measure
                if (token == V2_Token.Measure) measureCount++;
                
                // Break if measure count reached
                if (measureCount == generateMeasureCount) break;
            }
            
            // Break if measure count reached
            if (measureCount == generateMeasureCount) break;
        }

        // Get notes from tokens, print
        var notes = V2_TokenMethods.V2_Reconstruct(res, LeadSheet, startMeasureNum);

        // Return
        return MidiSong.FromNotes(notes).Measures;
    }
}