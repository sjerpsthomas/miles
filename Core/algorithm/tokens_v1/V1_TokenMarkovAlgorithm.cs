using Core.midi;
using Core.models.tokens_v1;
using Core.tokens.v1;

namespace Core.algorithm.tokens_v1;

public class V1_TokenMarkovAlgorithm : IAlgorithm
{
    public LeadSheet LeadSheet;

    public V1_TokenMarkov Model = new(3);
    
    public void Initialize(MidiSong[] solos, LeadSheet leadSheet)
    {
        LeadSheet = leadSheet;
        
        // Learn from all solos
        foreach (var solo in solos)
            Learn(solo.Measures);
    }

    public void Learn(List<MidiMeasure> measures, int startMeasureNum = 0)
    {
        // Create notes
        List<MidiNote> notes = [];
        for (var index = 0; index < measures.Count; index++)
        {
            var measure = measures[index];
            notes.AddRange(
                measure.Notes.Select(note => note with { Time = note.Time + index })
            );
        }

        // Create tokens, learn from them
        var tokens = V1_TokenMethods.V1_Tokenize(notes, LeadSheet);
        List<List<V1_Token>> tokensList = [tokens];
        Console.WriteLine($"learning from {V1_TokenMethods.V1_TokensToString(tokens)}");
        Model.Learn(tokensList);
    }

    public List<MidiMeasure> Generate(int generateMeasureCount = 4, int startMeasureNum = 0)
    {
        // Generate measures
        var tokens = Model.Walk(4)
            .Select(it =>
                it.Where(t => t != V1_Token.Measure)
                    .Take(10)
                    .Append(V1_Token.Measure)
            )
            .SelectMany(it => it).ToList();
        
        // Take no more than 4 measures
        var measureCount = 0;
        tokens = tokens.TakeWhile(it =>
        {
            if (it == V1_Token.Measure)
                measureCount++;
            return measureCount < 4;
        }).ToList();
        tokens.Add(V1_Token.Measure);

        // Print
        Console.WriteLine(V1_TokenMethods.V1_TokensToString(tokens));
        
        // Reconstruct notes        
        var notes = V1_TokenMethods.V1_Reconstruct(tokens, LeadSheet, startMeasureNum);
        
        // Create song
        return MidiSong.FromNotes(notes).Measures;
    }
}