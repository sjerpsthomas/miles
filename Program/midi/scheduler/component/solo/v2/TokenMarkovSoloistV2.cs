using System;
using System.Collections.Generic;
using System.Linq;
using Core.conversion;
using Core.midi;
using Core.midi.token;
using Godot;
using Program.util;
using static Godot.FileAccess.ModeFlags;

namespace Program.midi.scheduler.component.solo.v2;

public class TokenMarkovSoloistV2 : Soloist
{
    public LeadSheet LeadSheet;

    public MarkovChain<List<Token>> MarkovChain = new(new TokenListComparer());

    public string StandardPath;
    
    public TokenMarkovSoloistV2(string standardPath)
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
        var tokens = Conversion.Tokenize(notes, LeadSheet);
        Learn(tokens);
    }

    private void Learn(List<Token> tokens)
    {
        var n = 5;
        var chunks = tokens.TakeLast((tokens.Count / n) * n).Chunk(n).Select(it => it.ToList()).ToList();
        MarkovChain.Train(chunks);
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
            var fileAccessStream = new FileAccessStream(StandardPath + $"_extra_{i}.tokens", Read);
            var extraSongTokens = TokenMethods.FromTokensFileStream(fileAccessStream);
            
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
        List<Token> res = [];
        
        // Traverse factor oracle
        var chunk = MarkovChain.StartingValue();
        
        var measureCount = 0;
        var tokenCount = 0;
        while (true)
        {
            // Traverse
            chunk = MarkovChain.Traverse(chunk) ?? MarkovChain.StartingValue();

            foreach (var token in chunk)
            {
                // Generate token, add
                res.Add(token);
                tokenCount++;

                var measureTooLong = tokenCount > 10 && token != Token.Measure;
            
                if (measureTooLong)
                {
                    // Force measure token
                    res.Add(Token.Measure);
                }
            
                if (token == Token.Measure || measureTooLong)
                {
                    // Advance measure
                    measureCount++;
                    tokenCount = 0;

                    // Break if measure count reached
                    if (measureCount == generateMeasureCount)
                        break;
                }
            }
            
            // Break if measure count reached
            if (measureCount == generateMeasureCount)
                break;
        }
        
        // Reconstruct, return notes
        var notes = Conversion.ReconstructV2(res, LeadSheet, startMeasureNum);
        return MidiSong.FromNotes(notes).Measures;
    }
}