using static Core.midi.token.conversion.TokenMelody;
using static Core.midi.token.conversion.TimedTokenMelody;
using static Core.midi.token.TokenMethods;

namespace Core.midi.token.conversion.stage;

public static class TimingStage
{
    public static TimedTokenMelody TokenizeTiming(TokenMelody tokenMelody)
    {
        var tokens = tokenMelody.Tokens;

        // Early return
        if (tokens is [])
            return new TimedTokenMelody();
        
        // Move notes that are close to barlines
        for (var index = 0; index < tokens.Count; index++)
        {
            var token = tokens[index];
            var tokenTime = token.Time;
            var tokenSnapped = Math.Round(token.Time);
            
            if (tokenTime - Math.Truncate(tokenTime) is > 0.997 or < 0.003)
                tokens[index] = token with { Time = tokenSnapped };
        }

        // Delete small notes
        for (var index = 0; index < tokens.Count; index++)
        {
            if (!(tokens[index].Length < 0.03)) continue;

            // Console.WriteLine("Small note removed");
            tokens.RemoveAt(index);
            index -= 1;
        }
        
        // Delete small rests, make monophonic
        for (var index = 0; index < tokens.Count - 1; index++)
        {
            var token = tokens[index];
            var nextToken = tokens[index + 1];

            var restTime = nextToken.Time - (token.Time + token.Length);
            if (restTime < 0)
            {
                // Console.WriteLine("Note made monophonic");
                tokens[index] = token with { Length = nextToken.Time - token.Time };
            }
            else if (restTime < 0.8 * token.Time)
            {
                tokens[index] = token with { Length = nextToken.Time - token.Time };
            }
        }
        
        // Early return
        if (tokens is [])
            return new TimedTokenMelody();
        
        // Create velocity token melody
        var res = new TimedTokenMelody();
        
        // Get initial speed
        var currentSpeed = TokenSpeed.Fast;
        var startTime = tokens[0].Time;
        if (startTime > 0.03)
        {
            var f = (int)Math.Round(Math.Log2(startTime));
            currentSpeed = f switch
            {
                <= -4 => TokenSpeed.SuperFast,
                -3 => TokenSpeed.Fast,
                -2 => TokenSpeed.Slow,
                _ => TokenSpeed.SuperSlow,
            };
        }
        
        // (Creates a rest of specified length)
        void HandleRest(double length)
        {
            if (length <= 0.03) return;
            
            var amount = (int)Math.Round(length / currentSpeed.ToDouble());
            
            for (var i = 0; i < amount; i++)
                res.Tokens.Add(new TimedTokenMelodyRest());
        }
        
        // Create measures (horrible complexity)
        var measureCount = (int)Math.Truncate(tokens[^1].Time) + 1;
        var measureGroups = tokens.GroupBy(it => (int)Math.Truncate(it.Time)).ToList();
        var measures = Enumerable.Range(0, measureCount)
            .Select(it => 
                measureGroups.FirstOrDefault(group => group.Key == it)?.ToList() ?? []
            )
            .ToList();

        double measureStart = tokens[0].Time;
        
        for (var measureNum = 0; measureNum < measures.Count; measureNum++)
        {
            var measure = measures[measureNum];
            
            // Handle rest at start of measure
            if (measure is not [])
                HandleRest(measureStart);
            
            for (var index = 0; index < measure.Count; index++)
            {
                var token = measure[index];

                // Get speed of note length
                var f = (int)Math.Round(Math.Log2(token.Length));
                var newSpeed = f switch
                {
                    <= -4 => TokenSpeed.SuperFast,
                    -3 => TokenSpeed.Fast,
                    -2 => TokenSpeed.Slow,
                    _ => TokenSpeed.SuperSlow,
                };

                // Add speed tokens
                if (newSpeed != currentSpeed)
                {
                    res.Tokens.Add(new TimedTokenMelodySpeed(newSpeed));
                    currentSpeed = newSpeed;
                }

                // Add note and passing tone tokens
                if (token is TokenMelodyNote(var scaleNote, _, _, var nVelocity))
                    res.Tokens.Add(new TimedTokenMelodyNote(scaleNote, nVelocity));
                else if (token is TokenMelodyPassingTone(_, _, var ptVelocity))
                    res.Tokens.Add(new TimedTokenMelodyPassingTone(ptVelocity));

                // Add rest tokens
                var nextTime = index < measure.Count - 1 ? tokens[index + 1].Time : measureNum + 1;
                HandleRest(nextTime - (token.Time + token.Length));
            }

            if (measure is [])
                measureStart = 0.0;
            else
                measureStart = (measure[^1].Time + measure[^1].Length) - measureNum - 1;

            // Add measure token at the end
            res.Tokens.Add(new TimedTokenMelodyMeasure());
        }
        
        return res;
    }
    
    private record Phrase(List<TokenMelodyToken> Tokens, double Start, double End, TokenSpeed FinalSpeed);
    public static TokenMelody ReconstructTiming(TimedTokenMelody timedTokenMelody)
    {
        var tokens = timedTokenMelody.Tokens;

        var res = new TokenMelody();
        
        List<Phrase> currentMeasure = [];
        var currentPhrase = new Phrase([], 0.0, -1, TokenSpeed.Fast);
        var currentSpeed = TokenSpeed.Fast;
        var currentMeasureTime = 0.0;
        var currentMeasureNum = 0;
        foreach (var token in tokens)
        {
            // Handle ending of phrase
            if (token is TimedTokenMelodyMeasure or TimedTokenMelodySpeed)
            {
                currentMeasure.Add(currentPhrase with { End = currentMeasureTime });
                currentPhrase = new Phrase([], currentMeasureTime, -1, currentSpeed);
            }

            var tokenTime = currentSpeed.ToDouble();
            
            // Handle measure
            if (token is TimedTokenMelodyMeasure)
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

                    var finalSpeedDouble = phrase.FinalSpeed.ToDouble();

                    // Scale phrases such that measure is of length 1,
                    // Adjust start of phrase, snap end of phrase
                    phrase = phrase with
                    {
                        Start = index == 0 ? phrase.Start / measureLength : currentMeasure[index - 1].End,
                        // End = phrase.End / measureLength
                        End = (int)Math.Ceiling(phrase.End / measureLength / finalSpeedDouble) * finalSpeedDouble,
                    };

                    // Ignore notes when phrase does not snap correctly
                    if (phrase.End <= phrase.Start)
                    {
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
            else if (token is TimedTokenMelodyRest)
            {
                // Add dummy note
                var newToken = new TokenMelodyNote(0, currentMeasureTime, tokenTime, 0);
                currentPhrase.Tokens.Add(newToken);
                
                currentMeasureTime += tokenTime;
            }
            
            // Handle note
            else if (token is TimedTokenMelodyNote(var scaleNote, var nVelocity))
            {
                // Add TokenMelody token to phrase
                var newToken = new TokenMelodyNote(scaleNote, currentMeasureTime, tokenTime, nVelocity);
                currentPhrase.Tokens.Add(newToken);

                currentMeasureTime += tokenTime;
            }
            
            // Handle passing tone
            else if (token is TimedTokenMelodyPassingTone(var ptVelocity))
            {
                // Add TokenMelody token to phrase
                var newToken = new TokenMelodyPassingTone(currentMeasureTime, tokenTime, ptVelocity);
                currentPhrase.Tokens.Add(newToken);
                
                currentMeasureTime += tokenTime;
            }
            
            // Handle speed
            else if (token is TimedTokenMelodySpeed(var speed))
            {
                currentSpeed = speed;
            }
        }
        
        // Return
        return res;
    }
}
