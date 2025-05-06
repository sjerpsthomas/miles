using System;
using System.Collections.Generic;
using System.Linq;
using Core.midi;
using Core.models.tokens_v1;
using Core.tokens.v1;
using Program.util;
using static Godot.FileAccess.ModeFlags;

namespace Program.midi.scheduler.component.solo.tokens_v1;

public class TokenMarkovSoloist : Soloist
{
    public LeadSheet LeadSheet;

    public V1_TokenMarkov Model = new(3);

    public string StandardPath;
    
    public TokenMarkovSoloist(string standardPath)
    {
        StandardPath = standardPath;
    }
    
    private void Learn(List<MidiMeasure> measures)
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
        Learn(tokens);
    }

    private void Learn(List<V1_Token> tokens)
    {
        List<List<V1_Token>> tokensList = [tokens];
        Console.WriteLine($"learning from {V1_TokenMethods.V1_TokensToString(tokens)}");
        Model.Learn(tokensList);
    }
    
    public override void Initialize(MidiSong solo, LeadSheet leadSheet)
    {
        LeadSheet = leadSheet;
        
        // Learn the solo's measures
        Learn(solo.Measures);
        
        // Learn tokens from extra songs
        for (var i = 1; i <= 4; i++)
        {
            // Get from file
            var fileAccessStream = new FileAccessStream(StandardPath + $"_extra_{i}.notes", Read);
            var extraSong = MidiSong.FromNotesFileStream(fileAccessStream);
            var extraSongTokens = V1_TokenMethods.V1_Tokenize(extraSong.ToNotes(), LeadSheet);
            
            Learn(extraSongTokens);
        }
    }

    public override void IngestMeasures(List<MidiMeasure> measures, int startMeasureNum)
    {
        // Learn the given measures
        Learn(measures);
    }

    public override List<MidiMeasure> Generate(int generateMeasureCount, int startMeasureNum)
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