using NAudio.Dsp;
using static Core.midi.token.conversion.TokenMelody;
using static Core.midi.token.conversion.VelocityTokenMelody;
using static Core.midi.token.TokenMethods;

namespace Core.midi.token.conversion;

public static class TimingStage
{
    private record Phrase(List<TokenMelodyToken> Tokens, double Start, double End, TokenSpeed FinalSpeed);
    public static TokenMelody ResolveTiming(VelocityTokenMelody velocityTokenMelody)
    {
        var tokens = velocityTokenMelody.Tokens;

        var res = new TokenMelody();
        
        List<Phrase> currentMeasure = [];
        var currentPhrase = new Phrase([], 0.0, -1, TokenSpeed.Fast);
        var currentSpeed = TokenSpeed.Fast;
        var currentMeasureTime = 0.0;
        var currentMeasureNum = 0;
        foreach (var token in tokens)
        {
            // Handle ending of phrase
            if (token is VelocityTokenMelodyMeasure or VelocityTokenMelodySpeed)
            {
                currentMeasure.Add(currentPhrase with { End = currentMeasureTime });
                currentPhrase = new Phrase([], currentMeasureTime, -1, currentSpeed);
            }

            var tokenTime = currentSpeed.ToDouble();
            
            // Handle measure
            if (token is VelocityTokenMelodyMeasure)
            {
                // Handle empty measure
                if (currentMeasure is [{ Tokens: [] }])
                {
                    currentMeasureNum++;
                    currentMeasureTime = 0.0;
                    currentMeasure = [];
                    continue;
                }

                // Get length of all measures
                var measureLength = currentMeasure[^1].End;
                
                // Handle all phrases
                for (var index = 0; index < currentMeasure.Count; index++)
                {
                    var phrase = currentMeasure[index];

                    Console.WriteLine($"Start: {phrase.Start}, End: {phrase.End}");
                    
                    var finalSpeedDouble = phrase.FinalSpeed.ToDouble();

                    // Scale phrases such that measure is of length 1,
                    // Adjust start of phrase, snap end of phrase
                    phrase = phrase with
                    {
                        Start = index == 0 ? phrase.Start / measureLength : currentMeasure[index - 1].End,
                        End = phrase.End / measureLength
                        // End = (int)Math.Ceiling(phrase.End / measureLength / finalSpeedDouble) * finalSpeedDouble,
                    };

                    // Ignore notes when phrase does not snap correctly
                    if (phrase.End <= phrase.Start)
                    {
                        Console.WriteLine($"Phrase ignored ({phrase.Tokens.Count} notes)");
                        
                        phrase = phrase with { End = phrase.Start };
                        currentMeasure[index] = phrase;
                        continue;
                    }
                    
                    // Get time per token
                    var newTokenTime = (phrase.End - phrase.Start) / phrase.Tokens.Count;

                    // Add all time-shifted tokens
                    for (var i = 0; i < phrase.Tokens.Count; i++)
                    {
                        var phraseToken = phrase.Tokens[i];

                        // Ignore dummy notes, fix length
                        if (phraseToken is not TokenMelodyNote(_, _, _, 0))
                            res.Tokens.Add(
                                phraseToken with
                                {
                                    Time = currentMeasureNum + phrase.Start + i * newTokenTime,
                                    Length = newTokenTime
                                }
                            );
                    }

                    currentMeasure[index] = phrase;
                }

                // Increment time and measure
                currentMeasureNum++;
                currentMeasureTime = 0;
                currentMeasure = [];
                currentPhrase = new Phrase([], 0.0, -1, currentSpeed);
            }
            
            // Handle rest
            else if (token is VelocityTokenMelodyRest)
            {
                // Add dummy note
                var newToken = new TokenMelodyNote(0, currentMeasureTime, tokenTime, 0);
                currentPhrase.Tokens.Add(newToken);
                
                currentMeasureTime += tokenTime;
            }
            
            // Handle note
            else if (token is VelocityTokenMelodyNote(var scaleNote, var nVelocity))
            {
                // Add TokenMelody token to phrase
                var newToken = new TokenMelodyNote(scaleNote, currentMeasureTime, tokenTime, nVelocity);
                currentPhrase.Tokens.Add(newToken);

                currentMeasureTime += tokenTime;
            }
            
            // Handle passing tone
            else if (token is VelocityTokenMelodyPassingTone(var ptVelocity))
            {
                // Add TokenMelody token to phrase
                var newToken = new TokenMelodyPassingTone(currentMeasureTime, tokenTime, ptVelocity);
                currentPhrase.Tokens.Add(newToken);
                
                currentMeasureTime += tokenTime;
            }
            
            // Handle speed
            else if (token is VelocityTokenMelodySpeed(var speed))
            {
                currentSpeed = speed;
            }
        }
        
        // Return
        return res;
    }

    public static VelocityTokenMelody DeduceTiming(TokenMelody tokenMelody)
    {
        var tokens = tokenMelody.Tokens;

        var res = new VelocityTokenMelody { Tokens = new(tokens.Count) };
        
        // Get measures
        var measures = tokens.GroupBy(it => (int)Math.Truncate(it.Time + 0.03))
            .OrderBy(it => it.Key)
            .Select(it => it.ToList()).ToList();

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
                    Console.WriteLine("Removed small note");
                }

                if (noteRestTime < 0.03)
                {
                    // Give rest to token
                    measure[index] = token with { Length = token.Length + noteRestTime };
                    Console.WriteLine("Removed small rest");
                }
            }
            
            // Get the smallest note/rest
            var smallestSize = double.MaxValue;
            for (var index = 0; index < measure.Count; index++)
            {
                var token = measure[index];

                if (index == measure.Count - 1)
                    continue;
                
                // Get time of next token or end of measure
                var nextTime = (index == measure.Count - 1) ? (double)index + 1 : measure[index + 1].Time;
                
                smallestSize = Math.Min(smallestSize, token.Length);
                smallestSize = Math.Min(smallestSize, nextTime - token.Time);
            }

            // Scale measures so smallest note/rest is SuperFast (or slower)
            var scale = smallestSize > 0.0625 ? 1.0 : 0.0625 / smallestSize;
            
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
            
            // Go through every note in the measure
            var time = 0.0;
            
            // Get rest at start of measure
            {
                var targetTime = measure[0].Time * scale;
                var timeDiff = targetTime - time;
                if (timeDiff > 0.05)
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
                else Console.WriteLine("Skipped rest");
            }
            
            for (var index = 0; index < measure.Count; index++)
            {
                var token = measure[index];

                // Determine speed of note
                {
                    var targetTime = (token.Time + token.Length) * scale;
                    var timeDiff = targetTime - time;
                    if (timeDiff > 0.05)
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
                {
                    var nextTime = index == measure.Count - 1 ? (double)index + 1 : measure[index + 1].Time * scale;
                    
                    var targetTime = nextTime;
                    var timeDiff = targetTime - time;
                    if (timeDiff > 0.05)
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
                    else Console.WriteLine("Skipped rest");
                }
            }
            
            // Add measure token
            res.Tokens.Add(new VelocityTokenMelodyMeasure());
        }

        return res;
    }
}
