using static Core.midi.token.conversion.TimedTokenMelody;
using static Core.midi.token.Token;
using static Core.midi.token.TokenMethods;

namespace Core.midi.token.conversion.stage;

public static class VelocityStage
{
    public static List<Token> TokenizeVelocity(TimedTokenMelody timedTokenMelody)
    {
        var tokens = timedTokenMelody.Tokens;
        List<Token> res = new(tokens.Count);

        var currentVelocity = TokenVelocity.Quiet;
        void HandleVelocity(int velocity)
        {
            var newVelocity = velocity <= 96 ? TokenVelocity.Quiet : TokenVelocity.Loud;
            if (newVelocity != currentVelocity)
            {
                res.Add(newVelocity == TokenVelocity.Quiet ? Quiet : Loud);
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
                        TokenSpeed.SuperFast => SuperFast,
                        TokenSpeed.Fast => Fast,
                        TokenSpeed.Slow => Slow,
                        TokenSpeed.SuperSlow => SuperSlow,
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

        var currentVelocity = TokenVelocity.Quiet;
        
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
                    resTokens.Add(new TimedTokenMelodyNote((int)token - 1, currentVelocity == TokenVelocity.Quiet ? 96 : 127));
                    break;

                case PassingTone:
                    resTokens.Add(new TimedTokenMelodyPassingTone(currentVelocity == TokenVelocity.Quiet ? 96 : 127));
                    break;

                case SuperFast:
                case Fast:
                case Slow:
                case SuperSlow:
                    token.HasSpeed(out var speed);
                    resTokens.Add(new TimedTokenMelodySpeed(speed));
                    break;
    
                case Loud:
                    currentVelocity = TokenVelocity.Loud;
                    break;
                case Quiet:
                    currentVelocity = TokenVelocity.Quiet;
                    break;
    
                case Measure:
                    resTokens.Add(new TimedTokenMelodyMeasure());
                    break;
            }
        }

        return new TimedTokenMelody { Tokens = resTokens };
    }
}