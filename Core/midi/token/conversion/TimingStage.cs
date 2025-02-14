using static Core.midi.token.conversion.TokenMelody;
using static Core.midi.token.conversion.VelocityTokenMelody;
using static Core.midi.token.TokenMethods;

namespace Core.midi.token.conversion;

public static class TimingStage
{
    public static TokenMelody ResolveTiming(VelocityTokenMelody velocityTokenMelody)
    {
        var tokens = velocityTokenMelody.Tokens;
        
        List<(List<VelocityTokenMelodyToken>, double)> measures = [];
        #region Get measures
        {
            List<VelocityTokenMelodyToken> currentMeasure = [];
            var currentLength = 0.0;
            var currentSpeed = TokenSpeed.Fast;

            tokens.Add(new VelocityTokenMelodyMeasure());
            foreach (var token in tokens)
            {
                // Early return when handling measure token
                if (token is VelocityTokenMelodyMeasure)
                {
                    measures.Add((currentMeasure, currentLength));

                    currentLength = 0.0;
                    currentMeasure = [];
                    continue;
                }

                // Add token
                currentMeasure.Add(token);

                // Handle speed
                if (token is VelocityTokenMelodyRest or VelocityTokenMelodyNote or VelocityTokenMelodyPassingTone)
                    currentLength += currentSpeed switch
                    {
                        TokenSpeed.SuperFast => 0.0625,
                        TokenSpeed.Fast => 0.125,
                        TokenSpeed.Slow => 0.25,
                        TokenSpeed.SuperSlow => 0.5,
                        _ => throw new ArgumentOutOfRangeException()
                    };
                
                else if (token is VelocityTokenMelodySpeed(var speed))
                    currentSpeed = speed;
            }
        }
        #endregion

        TokenMelody res = new();
        
        // Go over every measure
        foreach (var (measure, measureLength) in measures)
        {
            // Put length in range of [0.6,1.2]
            var speedFactor = 1.0;
            while (speedFactor * measureLength < 1.2)
                speedFactor *= 2;
            while (speedFactor * measureLength > 1.2)
                speedFactor /= 2;
            
            // Turn into TokenMelodyTokens
            List<TokenMelodyToken> tokenMelodyTokens = [];
            var time = 0.0;
            var currentSpeed = TokenSpeed.Fast;
            
            foreach (var token in measure)
            {
                var tokenLength = currentSpeed switch
                {
                    TokenSpeed.SuperFast => 0.0625,
                    TokenSpeed.Fast => 0.125,
                    TokenSpeed.Slow => 0.25,
                    TokenSpeed.SuperSlow => 0.5,
                    _ => throw new ArgumentOutOfRangeException()
                } * speedFactor;
                
                switch (token)
                {
                    case VelocityTokenMelodyRest:
                        time += tokenLength;
                        break;

                    case VelocityTokenMelodyNote(var scaleNote, var velocity):
                        tokenMelodyTokens.Add(new TokenMelodyNote(scaleNote, time, tokenLength, velocity));
                        time += tokenLength;
                        break;

                    case VelocityTokenMelodyPassingTone(var velocity):
                        tokenMelodyTokens.Add(new TokenMelodyPassingTone(time, tokenLength, velocity));
                        time += tokenLength;
                        break;

                    case VelocityTokenMelodySpeed(var speed):
                        currentSpeed = speed;
                        break;
                    
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            
            // TODO: fit to length
            
            res.Tokens.AddRange(tokenMelodyTokens);
        }

        return res;
    }

    public static VelocityTokenMelody DeduceTiming(TokenMelody tokenMelody)
    {
        var tokens = tokenMelody.Tokens;

        var res = new VelocityTokenMelody();
        
        // Get measures
        var measures = tokens.GroupBy(it => it switch
        {
            TokenMelodyNote(_, var time, _, _) => time,
            TokenMelodyPassingTone(var time, _, _) => time,
            _ => 0.0
        })
            .OrderBy(it => it.Key)
            .Select(it => it.ToList());

        // Keep track of speed later on
        var currentSpeed = TokenSpeed.Fast;
        
        // Remove small notes, small rests
        foreach (var measure in measures)
        {
            for (var index = 0; index < measure.Count - 1; index++)
            {
                var token = measure[index];

                var previousToken = index == 0 ? null : measure[index - 1];
                var nextToken = index == measure.Count - 1 ? null : measure[index + 1];
                
                var noteLength = token.Length;
                var noteRestTime = (nextToken ?? token).Time - token.Time;

                if (noteLength < 0.03 && previousToken != null)
                {
                    // Remove note and give its length to previous token
                    previousToken = previousToken with { Length = previousToken.Length + noteLength };
                    measure[index - 1] = previousToken;
                    measure.RemoveAt(index);
                    index--;
                }

                if (noteRestTime < 0.03)
                {
                    // Give rest to token
                    measure[index] = token with { Length = token.Length + noteRestTime };
                }
            }
            
            // Get the smallest note/rest
            var smallestSize = double.MaxValue;
            for (var index = 0; index < measure.Count - 1; index++)
            {
                var token = measure[index];

                if (index == measure.Count - 1)
                    continue;
                
                smallestSize = Math.Min(smallestSize, token.Length);
                smallestSize = Math.Min(smallestSize, measure[index + 1].Time - token.Time);
            }

            // Scale measures so smallest note/rest is SuperFast (or slower)
            var scale = smallestSize > 0.0625 ? 1.0 : 0.0625 / smallestSize;
            
            // Go through every note in the measure
            var time = 0.0;
            for (var index = 0; index < measure.Count; index++)
            {
                var token = measure[index];

                void AddSpeed(double timeDiff)
                {
                    var f = (int)Math.Round(Math.Log2(timeDiff));

                    var speed = f switch
                    {
                        <= -4 => TokenSpeed.SuperFast,
                        -3 => TokenSpeed.Fast,
                        -2 => TokenSpeed.Slow,
                        _ => TokenSpeed.SuperSlow,
                    };

                    if (currentSpeed == speed) return;
                    
                    res.Tokens.Add(new VelocityTokenMelodySpeed(speed));
                    currentSpeed = speed;
                }
                
                // Determine speed of note
                {
                    var targetTime = (token.Time + token.Length) * scale;
                    var timeDiff = targetTime - time;
                    if (timeDiff > 0)
                    {
                        AddSpeed(timeDiff);
                    
                        // Add note token
                        res.Tokens.Add(token switch {
                            TokenMelodyNote(var scaleNote, _, _, var nVelocity) =>
                                new VelocityTokenMelodyNote(scaleNote, nVelocity),
                            TokenMelodyPassingTone(_, _, var ptVelocity) =>
                                new VelocityTokenMelodyPassingTone(ptVelocity),
                            _ => throw new ArgumentOutOfRangeException()
                        });
                    
                        // Increment time
                        time += currentSpeed switch
                        {
                            TokenSpeed.SuperFast => 0.6125,
                            TokenSpeed.Fast => 0.125,
                            TokenSpeed.Slow => 0.25,
                            TokenSpeed.SuperSlow => 0.5,
                            _ => throw new ArgumentOutOfRangeException()
                        };
                    }
                    else Console.WriteLine("Skipped note");
                }
                
                // Determine speed of rest
                if (index != measure.Count - 1)
                {
                    var nextToken = measure[index + 1];
                    
                    var targetTime = (nextToken.Time) * scale;
                    var timeDiff = targetTime - time;
                    if (timeDiff > 0)
                    {
                        AddSpeed(timeDiff);
                    
                        // Add rest token
                        res.Tokens.Add(new VelocityTokenMelodyRest());
                    
                        // Increment time
                        time += currentSpeed switch
                        {
                            TokenSpeed.SuperFast => 0.6125,
                            TokenSpeed.Fast => 0.125,
                            TokenSpeed.Slow => 0.25,
                            TokenSpeed.SuperSlow => 0.5,
                            _ => throw new ArgumentOutOfRangeException()
                        };
                    }
                    else Console.WriteLine("Skipped note");
                }
            }
        }

        return res;
    }
}
