using System.Collections.Generic;
using System.Linq;
using Core.conversion;
using Core.midi;
using Core.midi.token;
using Core.models.continuator;
using Program.util;
using static Godot.FileAccess.ModeFlags;

namespace Program.midi.scheduler.component.solo.v2;

public class TokenMarkovSoloistV2 : Soloist
{
    public LeadSheet LeadSheet;

    public VariableOrderMarkov<Token> Model = new(it => (int)it, kMax: 6);

    public string StandardPath;
    
    public TokenMarkovSoloistV2(string standardPath)
    {
        StandardPath = standardPath;
    }
    
    private void Learn(List<MidiMeasure> measures, int startMeasureNum)
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
        var tokens = Conversion.TokenizeV2(notes, LeadSheet, startMeasureNum);
        Learn(tokens);
    }

    private void Learn(List<Token> tokens)
    {
        Model.LearnSequence(tokens);
    }
    
    public override void Initialize(MidiSong solo, LeadSheet leadSheet)
    {
        LeadSheet = leadSheet;
        
        // Learn the solo's measures
        Learn(solo.Measures, 0);
        
        // Learn tokens from extra songs
        for (var i = 1; i <= 4; i++)
        {
            // Get from file
            var fileAccessStream = new FileAccessStream(StandardPath + $"_extra_{i}.notes", Read);
            var extraSong = MidiSong.FromNotesFileStream(fileAccessStream);
            var extraSongTokens = Conversion.TokenizeV2(extraSong.ToNotes(), LeadSheet, 0);
            
            Learn(extraSongTokens);
        }
    }

    public override void IngestMeasures(List<MidiMeasure> measures, int startMeasureNum)
    {
        // Learn the given measures
        Learn(measures, startMeasureNum);
    }

    public override List<MidiMeasure> Generate(int generateMeasureCount, int startMeasureNum)
    {
        // Generate tokens
        var res = Model.GenerateChunks((int)Token.Measure, generateMeasureCount, 10);
        
        // Reconstruct, return notes
        var notes = Conversion.ReconstructV2(res, LeadSheet, startMeasureNum);
        return MidiSong.FromNotes(notes).Measures;
    }
}