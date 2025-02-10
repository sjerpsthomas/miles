// See https://aka.ms/new-console-template for more information


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

var tokenMelody = TokenMelody.FromString("2..FF3p4.56US2.1D762F.U453DSS5pp2.3FD76p65U23");
var song = new MidiSong { Measures = tokenMelody.ToMeasures(4, leadSheet, 0, 4) };

Console.WriteLine("asdf!");