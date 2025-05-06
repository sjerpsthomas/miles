using static Core.tokens.v1.conversion.V1_TimedTokenMelody;
using static Core.tokens.v1.V1_Token;

namespace Core.tokens.v1.conversion.stage;

public static class V1_VelocityStage
{
    public static List<V1_Token> TokenizeVelocity(V1_TimedTokenMelody timedTokenMelody)
    {
        var tokens = timedTokenMelody.Tokens;
        List<V1_Token> res = new(tokens.Count);

        var currentVelocity = V1_TokenMethods.V1_TokenVelocity.Quiet;
        void HandleVelocity(int velocity)
        {
            var newVelocity = velocity <= 96 ? V1_TokenMethods.V1_TokenVelocity.Quiet : V1_TokenMethods.V1_TokenVelocity.Loud;
            if (newVelocity != currentVelocity)
            {
                res.Add(newVelocity == V1_TokenMethods.V1_TokenVelocity.Quiet ? Quiet : Loud);
                currentVelocity = newVelocity;
            }
        }
        
        foreach (var token in tokens)
        {
            switch (token)
            {
                case V1_TimedTokenMelodyRest:
                    res.Add(Rest);
                    break;
                case V1_TimedTokenMelodyNote(var scaleNote, var velocity):
                    HandleVelocity(velocity);
                    res.Add((V1_Token)scaleNote + 1);
                    break;

                case V1_TimedTokenMelodyPassingTone(var velocity):
                    HandleVelocity(velocity);
                    res.Add(PassingTone);
                    break;

                case V1_TimedTokenMelodySpeed(var speed):
                    res.Add(speed switch
                    {
                        V1_TokenMethods.V1_TokenSpeed.SuperFast => SuperFast,
                        V1_TokenMethods.V1_TokenSpeed.Fast => Fast,
                        V1_TokenMethods.V1_TokenSpeed.Slow => Slow,
                        V1_TokenMethods.V1_TokenSpeed.SuperSlow => SuperSlow,
                        _ => throw new ArgumentOutOfRangeException()
                    });
                    break;

                case V1_TimedTokenMelodyMeasure:
                    res.Add(Measure);
                    break;
            }
        }

        return res;
    }
    
    public static V1_TimedTokenMelody ReconstructVelocity(List<V1_Token> tokens)
    {
        List<V1_TimedTokenMelodyToken> resTokens = new(tokens.Count);

        var currentVelocity = V1_TokenMethods.V1_TokenVelocity.Quiet;
        
        foreach (var token in tokens)
        {
            switch (token)
            {
                case Rest:
                    resTokens.Add(new V1_TimedTokenMelodyRest());
                    break;

                case Note1:
                case Note2:
                case Note3:
                case Note4:
                case Note5:
                case Note6:
                case Note7:
                    resTokens.Add(new V1_TimedTokenMelodyNote((int)token - 1, currentVelocity == V1_TokenMethods.V1_TokenVelocity.Quiet ? 96 : 127));
                    break;

                case PassingTone:
                    resTokens.Add(new V1_TimedTokenMelodyPassingTone(currentVelocity == V1_TokenMethods.V1_TokenVelocity.Quiet ? 96 : 127));
                    break;

                case SuperFast:
                case Fast:
                case Slow:
                case SuperSlow:
                    token.V1_HasSpeed(out var speed);
                    resTokens.Add(new V1_TimedTokenMelodySpeed(speed));
                    break;
    
                case Loud:
                    currentVelocity = V1_TokenMethods.V1_TokenVelocity.Loud;
                    break;
                case Quiet:
                    currentVelocity = V1_TokenMethods.V1_TokenVelocity.Quiet;
                    break;
    
                case Measure:
                    resTokens.Add(new V1_TimedTokenMelodyMeasure());
                    break;
            }
        }

        return new V1_TimedTokenMelody { Tokens = resTokens };
    }
}