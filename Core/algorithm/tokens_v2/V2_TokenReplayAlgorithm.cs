using Core.midi;
using Core.tokens.v2;

namespace Core.algorithm.tokens_v2;

public class V2_TokenReplayAlgorithm : IAlgorithm
{
    public LeadSheet LeadSheet;

    public int HumanFourStart;

    public List<V2_Token> PreviousFour;
    
    public void Initialize(MidiSong[] solos, LeadSheet leadSheet) => LeadSheet = leadSheet;

    public void Learn(List<MidiMeasure> measures, int startMeasureNum)
    {
        // Get notes (shifted by measure number)
        List<MidiNote> notes = [];
        for (var index = 0; index < measures.Count; index++)
            foreach (var note in measures[index].Notes)
                notes.Add(note with { Time = note.Time + index });
        
        // Get tokens, set previous four
        PreviousFour = V2_TokenMethods.V2_Tokenize(notes, LeadSheet, startMeasureNum);
    }

    public List<MidiMeasure> Generate(int generateMeasureCount, int startMeasureNum)
    {
        // Reconstruct, return notes
        var notes = V2_TokenMethods.V2_Reconstruct(PreviousFour, LeadSheet, startMeasureNum);
        return MidiSong.FromNotes(notes).Measures;
    }
}