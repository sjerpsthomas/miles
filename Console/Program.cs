// See https://aka.ms/new-console-template for more information


using System.Diagnostics;
using Core.midi;
using Core.midi.token;



var leadSheet = new LeadSheet
{
    Chords =
    [
        [new Chord(Chord.KeyEnum.C, Chord.TypeEnum.Major)],
        [new Chord(Chord.KeyEnum.C, Chord.TypeEnum.Major)],
        [new Chord(Chord.KeyEnum.C, Chord.TypeEnum.Major)],
        [new Chord(Chord.KeyEnum.C, Chord.TypeEnum.Major)],
        [new Chord(Chord.KeyEnum.C, Chord.TypeEnum.Major)],
        [new Chord(Chord.KeyEnum.C, Chord.TypeEnum.Major)],
        [new Chord(Chord.KeyEnum.C, Chord.TypeEnum.Major)],
        [new Chord(Chord.KeyEnum.C, Chord.TypeEnum.Major)],
        [new Chord(Chord.KeyEnum.C, Chord.TypeEnum.Major)],
        [new Chord(Chord.KeyEnum.C, Chord.TypeEnum.Major)],
    ]
};

var sw = new Stopwatch();
sw.Start();

var n = 450;

var tokens = TokenMethods.TokensFromString("556fSppLp6");
var moreTokens = Enumerable.Repeat(tokens, 1000).SelectMany(it => it).ToList();

for (var i = 0; i < n; i++)
{
    _ = TokenMethods.ResolveMelody(moreTokens, leadSheet, 0);
}

sw.Stop();
Console.WriteLine($"{(double)sw.ElapsedMilliseconds / n} ms elapsed");


// var song = MidiSong.FromNotes(TokenMethods.ResolveMelody(tokens, leadSheet, 0));

Console.WriteLine("asdf!");