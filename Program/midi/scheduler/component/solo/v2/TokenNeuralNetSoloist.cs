using System;
using System.Collections.Generic;
using System.Linq;
using Core.conversion;
using Core.midi;
using Core.midi.token;
using Core.ml;

namespace Program.midi.scheduler.component.solo.v2;

public class TokenNeuralNetSoloist: Soloist
{
    public NeuralNet NeuralNet;
    public LeadSheet LeadSheet;

    public List<Token> Tokens = [];
    
    public override void Initialize(MidiSong solo, LeadSheet leadSheet)
    {
         LeadSheet = leadSheet;

         NeuralNet = new NeuralNet();
         
         IngestMeasures(solo.Measures, 0);
         
         // TODO: Load from user://
         NeuralNet.Load(@"C:\Users\thoma\Desktop\tokens_temp\neural_net");
    }

    public override void IngestMeasures(List<MidiMeasure> measures, int startMeasureNum)
    {
        List<MidiNote> notes = [];
        
        for (var index = 0; index < measures.Count; index++)
            foreach (var note in measures[index].Notes)
                notes.Add(note with { Time = note.Time + index + startMeasureNum });
        
        Tokens.AddRange(Conversion.TokenizeV2(notes, LeadSheet, startMeasureNum));
    }


    public override List<MidiMeasure> Generate(int generateMeasureCount, int startMeasureNum)
    {
        List<Token> res = [];
        
        // Generate tokens
        var measureCount = 0;

        while (true)
        {
            var lastTokens = Tokens.TakeLast(NeuralNet.ContextWindow).ToList();
            var lastTokensTensor = NeuralNet.TokensToTensor(lastTokens);

            // Console.WriteLine(TokenMethods.TokensToString(lastTokens));
            
            foreach (var token in NeuralNet.Infer(lastTokensTensor))
            {
                // Increase token count
                res.Add(token);
                Tokens.Add(token);

                // Advance measure
                if (token == Token.Measure) measureCount++;
                
                // Break if measure count reached
                if (measureCount == generateMeasureCount) break;
            }
            
            // Break if measure count reached
            if (measureCount == generateMeasureCount) break;
        }

        // Get notes from tokens, print
        var notes = Conversion.Reconstruct(res, LeadSheet, startMeasureNum);

        // Return
        return MidiSong.FromNotes(notes).Measures;
    }
}