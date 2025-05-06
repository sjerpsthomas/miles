using static Core.tokens.v2.conversion.V2_TimedTokenMelody;
using static Core.tokens.v2.V2_Token;

namespace Core.tokens.v2.conversion.stage;

public class V2_VelocityStage
{
    public static List<V2_Token> TokenizeVelocity(V2_TimedTokenMelody timedTokenMelody)
    {
        var tokens = timedTokenMelody.Tokens;
        List<V2_Token> res = new(tokens.Count);

        var currentVelocity = V2_TokenMethods.V2_TokenVelocity.Quiet;
        void HandleVelocity(int velocity)
        {
            var newVelocity = velocity <= 96 ? V2_TokenMethods.V2_TokenVelocity.Quiet : V2_TokenMethods.V2_TokenVelocity.Loud;
            if (newVelocity != currentVelocity)
            {
                res.Add(newVelocity == V2_TokenMethods.V2_TokenVelocity.Quiet ? Quiet : Loud);
                currentVelocity = newVelocity;
            }
        }
        
        foreach (var token in tokens)
        {
            switch (token)
            {
                case V2_TimedTokenMelodyRest:
                    res.Add(Rest);
                    break;
                case V2_TimedTokenMelodyNote(var scaleNote, var velocity):
                    HandleVelocity(velocity);
                    res.Add((V2_Token)scaleNote + 1);
                    break;

                case V2_TimedTokenMelodyPassingTone(var velocity):
                    HandleVelocity(velocity);
                    res.Add(PassingTone);
                    break;

                case V2_TimedTokenMelodySpeed(var speed):
                    res.Add(speed switch
                    {
                        V2_TokenMethods.V2_TokenSpeed.SuperFast => SuperFast,
                        V2_TokenMethods.V2_TokenSpeed.Fast => Fast,
                        V2_TokenMethods.V2_TokenSpeed.Slow => Slow,
                        V2_TokenMethods.V2_TokenSpeed.SuperSlow => SuperSlow,
                        _ => throw new ArgumentOutOfRangeException()
                    });
                    break;

                case V2_TimedTokenMelodyMeasure:
                    res.Add(Measure);
                    break;
            }
        }

        return res;
    }
    
    public static V2_TimedTokenMelody ReconstructVelocity(List<V2_Token> tokens)
    {
        List<V2_TimedTokenMelodyToken> resTokens = new(tokens.Count);

        var currentVelocity = V2_TokenMethods.V2_TokenVelocity.Quiet;
        
        foreach (var token in tokens)
        {
            switch (token)
            {
                case Rest:
                    resTokens.Add(new V2_TimedTokenMelodyRest());
                    break;

                case Note1:
                case Note2:
                case Note3:
                case Note4:
                case Note5:
                case Note6:
                case Note7:
                    resTokens.Add(new V2_TimedTokenMelodyNote((int)token - 1, currentVelocity == V2_TokenMethods.V2_TokenVelocity.Quiet ? 96 : 127));
                    break;

                case PassingTone:
                    resTokens.Add(new V2_TimedTokenMelodyPassingTone(currentVelocity == V2_TokenMethods.V2_TokenVelocity.Quiet ? 96 : 127));
                    break;

                case SuperFast:
                case Fast:
                case Slow:
                case SuperSlow:
                    token.V2_HasSpeed(out var speed);
                    resTokens.Add(new V2_TimedTokenMelodySpeed(speed));
                    break;
    
                case Loud:
                    currentVelocity = V2_TokenMethods.V2_TokenVelocity.Loud;
                    break;
                case Quiet:
                    currentVelocity = V2_TokenMethods.V2_TokenVelocity.Quiet;
                    break;
    
                case Measure:
                    resTokens.Add(new V2_TimedTokenMelodyMeasure());
                    break;
            }
        }

        return new V2_TimedTokenMelody { Tokens = resTokens };
    }
}