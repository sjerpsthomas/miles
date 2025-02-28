using System;
using System.Collections.Generic;
using System.Linq;
using Core.midi;
using Core.midi.token;

namespace Program.midi.scheduler.component.solo;

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
        var tokens = TokenMethods.Tokenize(Notes, LeadSheet);
        var permutatedTokens = new TokenShuffleModel().Permutate(tokens);

        Console.WriteLine(TokenMethods.TokensToString(permutatedTokens));
        
        // Reconstruct, return notes
        var notes = TokenMethods.Reconstruct(permutatedTokens, LeadSheet, startMeasureNum);
        return MidiSong.FromNotes(notes).Measures;
    }
}