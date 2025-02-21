using System.Collections.Generic;
using System.Linq;
using Core.midi;
using Core.midi.token;
using Core.ml;
using Godot;

namespace Program.midi.scheduler.component.solo;

public class TransformerSoloist: Soloist
{
    public Transformer Transformer;
    public List<MidiNote> Notes = [];
    public LeadSheet LeadSheet;
    
    public override void Initialize(MidiSong solo, LeadSheet leadSheet)
    {
         LeadSheet = leadSheet;

         Transformer = new Transformer("onnx_transformer.onnx");
    }

    public override void IngestMeasures(List<MidiMeasure> measures, int startMeasureNum)
    {
        for (var index = 0; index < measures.Count; index++)
            foreach (var note in measures[index].Notes)
                Notes.Add(note with { Time = note.Time + index + startMeasureNum });
    }

    public override List<MidiMeasure> Generate(int generateMeasureCount, int startMeasureNum)
    {
        // Deduce and set tokens
        var tokens = TokenMethods.Tokenize(Notes);
        Transformer.SetTokens(tokens);
        
        List<Token> res = [];
        
        // Generate tokens
        var measureCount = 0;
        var tokenCount = 0;
        while (true)
        {
            // Generate token, add
            var newToken = Transformer.Generate();
            res.Add(newToken);
            tokenCount++;

            var measureTooLong = tokenCount > 10 && newToken != Token.Measure;
            
            if (measureTooLong)
            {
                // Force measure token
                res.Add(Token.Measure);
                Transformer.Append(Token.Measure);
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
        
        // Get notes from tokens, print
        var notes = TokenMethods.Reconstruct(res, LeadSheet, startMeasureNum);
        GD.Print(string.Join(',', notes.Select(it => it.Note.ToString())));

        // Return
        return MidiSong.FromNotes(notes).Measures;
    }
}