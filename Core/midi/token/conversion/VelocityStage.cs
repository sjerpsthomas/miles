using System.Reflection.Metadata;
using static Core.midi.token.conversion.VelocityTokenMelody;
using static Core.midi.token.Token;

namespace Core.midi.token.conversion;

public static class VelocityStage
{
    public static VelocityTokenMelody ResolveVelocity(List<Token> tokens)
    {
        List<VelocityTokenMelodyToken> resTokens = new(tokens.Count);

        int currentVelocity = 96;
        
        foreach (var token in tokens)
        {
            switch (token)
            {
                case Rest:
                    resTokens.Add(new VelocityTokenMelodyRest());
                    break;

                case Note1:
                case Note2:
                case Note3:
                case Note4:
                case Note5:
                case Note6:
                case Note7:
                    token.IsNote(out var intToken);
                    resTokens.Add(new VelocityTokenMelodyNote(intToken, currentVelocity));
                    break;

                case PassingTone:
                    resTokens.Add(new VelocityTokenMelodyPassingTone(currentVelocity));
                    break;

                case SuperFast:
                case Fast:
                case Slow:
                case SuperSlow:
                    token.HasSpeed(out var speed);
                    resTokens.Add(new VelocityTokenMelodySpeed(speed));
                    break;
    
                case Loud:
                    currentVelocity = 96;
                    break;
                case Quiet:
                    currentVelocity = 127;
                    break;
    
                case Measure:
                    resTokens.Add(new VelocityTokenMelodyMeasure());
                    break;
            }
        }

        return new VelocityTokenMelody { Tokens = resTokens };
    }

    public static List<Token> DeduceVelocity(VelocityTokenMelody velocityTokenMelody)
    {
        var tokens = velocityTokenMelody.Tokens;
        List<Token> res = new(tokens.Count);

        var currentVelocity = 96;
        void HandleVelocity(int velocity)
        {
            var newVelocity = velocity <= 96 ? 96 : 127;
            if (newVelocity != currentVelocity)
            {
                res.Add(newVelocity == 96 ? Quiet : Loud);
                currentVelocity = newVelocity;
            }
        }
        
        foreach (var token in tokens)
        {
            switch (token)
            {
                case VelocityTokenMelodyRest:
                    res.Add(Rest);
                    break;
                case VelocityTokenMelodyNote(var scaleNote, var velocity):
                    HandleVelocity(velocity);
                    res.Add((Token)scaleNote);
                    break;

                case VelocityTokenMelodyPassingTone(var velocity):
                    HandleVelocity(velocity);
                    res.Add(PassingTone);
                    break;

                case VelocityTokenMelodySpeed(var speed):
                    res.Add(speed switch
                    {
                        TokenMethods.TokenSpeed.SuperFast => SuperFast,
                        TokenMethods.TokenSpeed.Fast => Fast,
                        TokenMethods.TokenSpeed.Slow => Slow,
                        TokenMethods.TokenSpeed.SuperSlow => SuperSlow,
                        _ => throw new ArgumentOutOfRangeException()
                    });
                    break;

                case VelocityTokenMelodyMeasure:
                    res.Add(Measure);
                    break;
            }
        }

        return res;
    }
}