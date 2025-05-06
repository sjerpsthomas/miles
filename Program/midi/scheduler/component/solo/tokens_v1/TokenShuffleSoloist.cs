using System;
using System.Collections.Generic;
using System.Linq;
using Core.midi;
using Core.models.tokens_v1;
using Core.tokens.v1;

namespace Program.midi.scheduler.component.solo.tokens_v1;

public class TokenShuffleSoloist : Soloist
{
    public LeadSheet LeadSheet;

    public List<MidiNote> Notes;

    public override void Initialize(MidiSong solo, LeadSheet leadSheet) => LeadSheet = leadSheet;

    public override void IngestMeasures(List<MidiMeasure> measures, int startMeasureNum)
    {
        // Set notes
        Notes = measures.SelectMany((measure, i) =>
            measure.Notes.Select(it => it with { Time = it.Time + i })
        ).ToList();
    }

    public override List<MidiMeasure> Generate(int generateMeasureCount, int startMeasureNum)
    {
        // Tokenize, permutate tokens
        var tokens = V1_TokenMethods.V1_Tokenize(Notes, LeadSheet);
        var permutatedTokens = new V1_TokenShuffleModel().Permutate(tokens);

        Console.WriteLine(V1_TokenMethods.V1_TokensToString(permutatedTokens));
        
        // Reconstruct, return notes
        var notes = V1_TokenMethods.V1_Reconstruct(permutatedTokens, LeadSheet, startMeasureNum);
        return MidiSong.FromNotes(notes).Measures;
    }
}