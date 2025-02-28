using System;
using System.Collections.Generic;
using System.Linq;
using Core.midi;
using Core.midi.token;
using Program.util;
using static Godot.FileAccess.ModeFlags;

namespace Program.midi.scheduler.component.solo;

public class TokenMarkovSoloist : Soloist
{
    public LeadSheet LeadSheet;

    public TokenMarkovModel Model = new(3);

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
        var tokens = TokenMethods.Tokenize(notes, LeadSheet);
        Model.Learn([tokens]);
    }
    
    public override void Initialize(MidiSong solo, LeadSheet leadSheet)
    {
        LeadSheet = leadSheet;
        
        // Learn the solo's measures
        Learn(solo.Measures);
        
        // Learn measures from extra songs
        for (var i = 1; i <= 4; i++)
        {
            var extraSong = MidiSong.FromStream(new FileAccessStream(StandardPath + "back.mid", Read));
            Learn(extraSong.Measures);
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
        var tokens = Model.Walk(4).Select(it => it.Take(10).Append(Token.Measure)).SelectMany(it => it).ToList();
        
        // Take no more than 4 measures
        var measureCount = 0;
        tokens = tokens.TakeWhile(it =>
        {
            if (it == Token.Measure)
                measureCount++;
            return measureCount < 4;
        }).ToList();
        tokens.Add(Token.Measure);

        // Print
        Console.WriteLine(TokenMethods.TokensToString(tokens));
        
        // Reconstruct notes        
        var notes = TokenMethods.Reconstruct(tokens, LeadSheet, startMeasureNum);
        
        // Create song
        return MidiSong.FromNotes(notes).Measures;
    }
}