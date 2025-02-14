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

var n = 400;

var tokens = TokenMethods.TokensFromString("2..F3p4.56S2M.1762f.453Ms5pp2.3F76p4M23");
var moreTokens = Enumerable.Repeat(tokens, 1000).SelectMany(it => it).ToList();

for (var i = 0; i < n; i++)
{
    var res = TokenMethods.ResolveMelody(moreTokens, leadSheet, 0);
    Console.WriteLine(tokens);
}

sw.Stop();
Console.WriteLine($"{(double)sw.ElapsedMilliseconds / n} ms elapsed");


// var song = MidiSong.FromNotes(TokenMethods.ResolveMelody(tokens, leadSheet, 0));

Console.WriteLine("asdf!");