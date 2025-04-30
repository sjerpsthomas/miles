using System.Collections.Generic;
using Core.conversion;
using Core.midi;
using Core.midi.token;

namespace Program.midi.scheduler.component.solo.v2;

public class TokenReplaySoloistV2 : Soloist
{
    public LeadSheet LeadSheet;

    public int HumanFourStart;

    public List<Token> PreviousFour;
    
    public override void Initialize(MidiSong solo, LeadSheet leadSheet)
    {
        LeadSheet = leadSheet;
    }

    public override void IngestMeasures(List<MidiMeasure> measures, int startMeasureNum)
    {
        // Get notes (shifted by measure number)
        List<MidiNote> notes = [];
        for (var index = 0; index < measures.Count; index++)
            foreach (var note in measures[index].Notes)
                notes.Add(note with { Time = note.Time + index });
        
        // Get tokens, set previous four
        PreviousFour = Conversion.TokenizeV2(notes, LeadSheet, startMeasureNum);
    }

    public override List<MidiMeasure> Generate(int generateMeasureCount, int startMeasureNum)
    {
        // Reconstruct, return notes
        var notes = Conversion.ReconstructV2(PreviousFour, LeadSheet, startMeasureNum);
        return MidiSong.FromNotes(notes).Measures;
    }
}