using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using Core.midi;
using Core.midi.token;
using Godot;
using Microsoft.VisualBasic.CompilerServices;

namespace Program.midi.scheduler.component.solo;

public class TokenRandomSoloist: Soloist
{
    public LeadSheet LeadSheet;

    public Random Random = new();
    
    public override void Initialize(MidiSong solo, LeadSheet leadSheet) => LeadSheet = leadSheet;

    // Empty; Token Random does not use user content
    public override void IngestMeasures(List<MidiMeasure> measures, int startMeasureNum) { }

    public override List<MidiMeasure> Generate(int generateMeasureCount, int startMeasureNum)
    {
        List<Token> tokens = [];
        for (var i = 0; i < generateMeasureCount; i++)
        {
            var measureTokenAmount = Random.Next(4, 8);
            tokens.AddRange(Enumerable.Range(0, measureTokenAmount).Select(_ => GetToken()));
            tokens.Add(Token.Measure);
        }
        Console.WriteLine(TokenMethods.TokensToString(tokens));
        
        var notes = TokenMethods.Reconstruct(tokens, LeadSheet, startMeasureNum);
        
        return MidiSong.FromNotes(notes).Measures;
    }

    private Token GetToken()
    {
        return Random.NextSingle() switch
        {
            < 0.15f => Token.Rest,
            < 0.40f => (Token)Random.Next(0, 8),
            < 0.60f => Token.PassingTone,
            < 0.65f => Token.SuperFast,
            < 0.72f => Token.Fast,
            < 0.79f => Token.Slow,
            < 0.86f => Token.SuperSlow,
            < 0.93f => Token.Loud,
            _ =>       Token.Quiet
        };
    }
}