using System.Diagnostics;
using Core.midi;
using Core.tokens.v1;
using static System.Console;
using static Core.midi.Chord.KeyEnum;
using static Core.midi.Chord.TypeEnum;

namespace Console.routine;

public static class TokenPerformanceTest
{
    public static void Run()
    {
        var leadSheet = new LeadSheet { Chords = [[new Chord(C, Major)]] };

        var sw = new Stopwatch();
        sw.Start();

        var n = 450;

        var tokens = V1_TokenMethods.V1_TokensFromString("2..F3p4.56S2M.1762f.453Ms5pp2.3F76p4M23");
        var moreTokens = Enumerable.Repeat(tokens, 1000).SelectMany(it => it).ToList();

        var notes = V1_TokenMethods.V1_Reconstruct(moreTokens, leadSheet, 0);

        for (var i = 0; i < n; i++)
        {
            _ = V1_TokenMethods.V1_Tokenize(notes);
        }

        sw.Stop();
        WriteLine($"{(double)sw.ElapsedMilliseconds / n} ms elapsed");

        WriteLine("Done!");
    }
}
