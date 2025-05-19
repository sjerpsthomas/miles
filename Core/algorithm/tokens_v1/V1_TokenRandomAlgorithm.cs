using Core.midi;
using Core.tokens.v1;

namespace Core.algorithm.tokens_v1;

public class V1_TokenRandomAlgorithm: IAlgorithm
{
    public LeadSheet LeadSheet;

    public Random Random = new();
    
    public void Initialize(MidiSong[] solos, LeadSheet leadSheet) => LeadSheet = leadSheet;

    // Empty; Token Random does not use user content
    public void Learn(List<MidiMeasure> measures, int startMeasureNum = 0) { }

    public List<MidiMeasure> Generate(int generateMeasureCount = 4, int startMeasureNum = 0)
    {
        List<V1_Token> tokens = [];
        for (var i = 0; i < generateMeasureCount; i++)
        {
            var measureTokenAmount = Random.Next(4, 8);
            tokens.AddRange(Enumerable.Range(0, measureTokenAmount).Select(_ => GetToken()));
            tokens.Add(V1_Token.Measure);
        }
        Console.WriteLine(V1_TokenMethods.V1_TokensToString(tokens));
        
        var notes = V1_TokenMethods.V1_Reconstruct(tokens, LeadSheet, startMeasureNum);
        
        return MidiSong.FromNotes(notes).Measures;
    }

    private V1_Token GetToken() =>
        Random.NextSingle() switch
        {
            < 0.15f => V1_Token.Rest,
            < 0.40f => (V1_Token)Random.Next(0, 8),
            < 0.60f => V1_Token.PassingTone,
            < 0.65f => V1_Token.SuperFast,
            < 0.72f => V1_Token.Fast,
            < 0.79f => V1_Token.Slow,
            < 0.86f => V1_Token.SuperSlow,
            < 0.93f => V1_Token.Loud,
            _ =>       V1_Token.Quiet
        };
}