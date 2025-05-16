using Core.midi;
using Core.tokens.v2;

namespace Core.algorithm.tokens_v2;

public class V2_TokenRepetitionAlgorithm: IAlgorithm
{
    public List<MidiNote> Notes;
    public LeadSheet LeadSheet;

    private void AddMeasures(List<MidiMeasure> measures)
    {
        for (var index = 0; index < measures.Count; index++)
        {
            var measure = measures[index];
            foreach (var note in measure.Notes)
                Notes.Add(note with { Time = note.Time + index });
        }
    }
    
    public void Initialize(MidiSong[] solos, LeadSheet leadSheet) => LeadSheet = leadSheet;

    public void IngestMeasures(List<MidiMeasure> measures, int startMeasureNum)
    {
        // Get all notes
        Notes = [];
        AddMeasures(measures);
    }

    public List<MidiMeasure> Generate(int generateMeasureCount, int startMeasureNum)
    {
        var tokens = V2_TokenMethods.V2_Tokenize(Notes, LeadSheet);
        Console.WriteLine(V2_TokenMethods.V2_TokensToString(tokens));
        
        var notes = V2_TokenMethods.V2_Reconstruct(tokens, LeadSheet, startMeasureNum);
        
        return MidiSong.FromNotes(notes).Measures;
    }
}