using Core.midi;
using Core.models.tokens_v2;
using Core.tokens.v2;

namespace Core.algorithm.tokens_v2;

public class V2_TokenMarkovAlgorithm : IAlgorithm
{
    public LeadSheet LeadSheet;

    public GenericContinuator<V2_Token> Model = new(it => (int)it, kMax: 6);

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
        var tokens = V2_TokenMethods.V2_Tokenize(notes, LeadSheet, startMeasureNum);
        Model.LearnSequence(tokens);
    }

    public List<MidiMeasure> Generate(int generateMeasureCount = 4, int startMeasureNum = 0)
    {
        // Generate tokens
        var res = Model.GenerateChunks((int)V2_Token.Measure, generateMeasureCount, 7);
        
        // Reconstruct, return notes
        var notes = V2_TokenMethods.V2_Reconstruct(res, LeadSheet, startMeasureNum);
        return MidiSong.FromNotes(notes).Measures;
    }
}