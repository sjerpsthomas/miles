using static Core.midi.token.conversion.TimedTokenMelody;
using static Core.midi.token.Token;

namespace Core.midi.token.conversion.stage;

public static class VelocityStage
{
    public static List<Token> TokenizeVelocity(TimedTokenMelody timedTokenMelody)
    {
        var tokens = timedTokenMelody.Tokens;
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
                case TimedTokenMelodyRest:
                    res.Add(Rest);
                    break;
                case TimedTokenMelodyNote(var scaleNote, var velocity):
                    HandleVelocity(velocity);
                    res.Add((Token)scaleNote + 1);
                    break;

                case TimedTokenMelodyPassingTone(var velocity):
                    HandleVelocity(velocity);
                    res.Add(PassingTone);
                    break;

                case TimedTokenMelodySpeed(var speed):
                    res.Add(speed switch
                    {
                        TokenMethods.TokenSpeed.SuperFast => SuperFast,
                        TokenMethods.TokenSpeed.Fast => Fast,
                        TokenMethods.TokenSpeed.Slow => Slow,
                        TokenMethods.TokenSpeed.SuperSlow => SuperSlow,
                        _ => throw new ArgumentOutOfRangeException()
                    });
                    break;

                case TimedTokenMelodyMeasure:
                    res.Add(Measure);
                    break;
            }
        }

        return res;
    }
    
    public static TimedTokenMelody ReconstructVelocity(List<Token> tokens)
    {
        List<TimedTokenMelodyToken> resTokens = new(tokens.Count);

        int currentVelocity = 96;
        
        foreach (var token in tokens)
        {
            switch (token)
            {
                case Rest:
                    resTokens.Add(new TimedTokenMelodyRest());
                    break;

                case Note1:
                case Note2:
                case Note3:
                case Note4:
                case Note5:
                case Note6:
                case Note7:
                    resTokens.Add(new TimedTokenMelodyNote((int)token - 1, currentVelocity));
                    break;

                case PassingTone:
                    resTokens.Add(new TimedTokenMelodyPassingTone(currentVelocity));
                    break;

                case SuperFast:
                case Fast:
                case Slow:
                case SuperSlow:
                    token.HasSpeed(out var speed);
                    resTokens.Add(new TimedTokenMelodySpeed(speed));
                    break;
    
                case Loud:
                    currentVelocity = 96;
                    break;
                case Quiet:
                    currentVelocity = 127;
                    break;
    
                case Measure:
                    resTokens.Add(new TimedTokenMelodyMeasure());
                    break;
            }
        }

        return new TimedTokenMelody { Tokens = resTokens };
    }
}