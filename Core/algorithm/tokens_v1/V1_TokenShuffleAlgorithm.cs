using Core.midi;
using Core.models.tokens_v1;
using Core.tokens.v1;

namespace Core.algorithm.tokens_v1;

public class V1_TokenShuffleAlgorithm : IAlgorithm
{
    public LeadSheet LeadSheet;

    public List<MidiNote> Notes;

    public void Initialize(MidiSong[] solos, LeadSheet leadSheet) => LeadSheet = leadSheet;

    public void IngestMeasures(List<MidiMeasure> measures, int startMeasureNum)
    {
        // Set notes
        Notes = measures.SelectMany((measure, i) =>
            measure.Notes.Select(it => it with { Time = it.Time + i })
        ).ToList();
    }

    public List<MidiMeasure> Generate(int generateMeasureCount, int startMeasureNum)
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