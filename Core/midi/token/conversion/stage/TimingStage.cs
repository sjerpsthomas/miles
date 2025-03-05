using static Core.midi.LeadSheet;
using static Core.midi.token.conversion.TokenMelody;
using static Core.midi.token.conversion.TimedTokenMelody;
using static Core.midi.token.TokenMethods;

namespace Core.midi.token.conversion.stage;

public static class TimingStage
{
    private abstract record TimingTemp(double Time, double Length);
    private record TimingTempToken(TokenMelodyToken Token) : TimingTemp(Token.Time, Token.Length);
    private record TimingTempRest(double Time, double Length) : TimingTemp(Time, Length);

    public static TimedTokenMelody TokenizeTiming(TokenMelody tokenMelody, LeadSheet? leadSheet)
    {
        var tokens = tokenMelody.Tokens;
        
        // Lengthen sequence per measure
        List<List<TimingTemp>> units = GetUnits(tokens, leadSheet);

        if (units is [])
            return new TimedTokenMelody();
        
        // Fit measure to result
        return Fit(units);
    }

    private static List<List<TimingTemp>> GetUnits(List<TokenMelodyToken> tokens, LeadSheet? leadSheet)
    {
        // Early return
        if (tokens is [])
            return [];
        
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
        
        // Early return
        if (tokens is [])
            return [];
        
        // Delete small rests, make monophonic
        var measureCount = (int)Math.Truncate(tokens.Max(it => it.Time)) + 1;
        var measures = Enumerable.Range(0, measureCount)
            .Select(it => tokens.Where(t => (int)Math.Truncate(t.Time) == it).ToList());

        List<List<TimingTemp>> res = [];
        
        // var mesaure = tokens.GroupBy(it => (int)Math.Truncate(it.Time)).Select(it => it.ToList());
        foreach (var measure in measures)
        {
            List<TimingTemp> resMeasure = [];
            
            // Add dummy note to end of measure
            measure.Add(new TokenMelodyNote(0, 1.0, 0.0, 0));
            
            // Handle all but dummy note
            for (var index = 0; index < measure.Count - 1; index++)
            {
                var token = measure[index];
                var nextToken = measure[index + 1];

                var addRest = true;
                
                var restLength = nextToken.Time - (token.Time + token.Length);
                if (restLength < 0.6 * token.Time)
                {
                    measure[index] = token with { Length = nextToken.Time - token.Time };
                    addRest = false;
                }
            
                // Remove swing
                var swing = (leadSheet?.Style ?? StyleEnum.Straight) == StyleEnum.Swing;
                var newTime = RemoveSwing(token.Time, swing);
                var newLength = token.Length - (newTime - token.Time);
                
                // Add token (and maybe rest) to measure
                resMeasure.Add(new TimingTempToken(token with { Time = newTime, Length = newLength }));
                if (addRest) resMeasure.Add(new TimingTempRest(token.Time + token.Length, restLength));
            }
            
            res.Add(resMeasure);
        }

        return res;
    }
    
    private static TimedTokenMelody Fit(List<List<TimingTemp>> units)
    {
        // Initialize result
        var res = new List<TimedTokenMelodyToken>(units.Count);
        
        // Fit to sections
        foreach (var measure in units)
        {
            var best = double.PositiveInfinity;
            var startIndex = 0;
            var phraseStart = 0.0;
            var previousPhraseEnd = 0.0;
            var previousAverageSpeed = TokenSpeed.Fast;

            for (var index = 0; index < measure.Count; index++)
            {
                var unit = measure[index];

                var currentPhrase = measure.Skip(startIndex).Take(index - startIndex + 1).ToArray();
                
                // Get average speed of phrase
                var averageSpeed = currentPhrase.Average(it => it.Time).ToSpeed();
                var averageSpeedDouble = averageSpeed.ToDouble();
                
                // Quantize end of unit (based on average speed)
                var phraseEnd = (int)Math.Ceiling((unit.Time + unit.Length) / averageSpeedDouble) * averageSpeedDouble;
                var unitTime = (phraseEnd - phraseStart) / currentPhrase.Length;
                
                // Get average deviation of end of units
                var averageError = currentPhrase.Select(
                    (phraseUnit, i) => Math.Abs(phraseUnit.Time + phraseUnit.Length - (phraseStart + i * unitTime))
                ).Average();
            
                // Check if unit does not improve average error
                if (averageError > best || index == measure.Count - 1)
                {
                    if (index == measure.Count - 1)
                    {
                        index += 1;
                        previousAverageSpeed = averageSpeed;
                        previousPhraseEnd = phraseEnd;
                    }
                    
                    // Add previous average speed
                    res.Add(new TimedTokenMelodySpeed(previousAverageSpeed));

                    // Add all in [startIndex..(index-1)] to result
                    for (var j = startIndex; j < index; j++)
                    {
                        var addUnit = measure[j];
                        if (addUnit is TimingTempToken(TokenMelodyNote(var nScaleTone, _, _, var nVelocity)))
                            res.Add(new TimedTokenMelodyNote(nScaleTone, nVelocity));
                        else if (addUnit is TimingTempToken(TokenMelodyPassingTone(_, _, var ptVelocity)))
                            res.Add(new TimedTokenMelodyPassingTone(ptVelocity));
                        else if (addUnit is TimingTempRest)
                            res.Add(new TimedTokenMelodyRest());
                    }

                    // Reset variables
                    best = double.PositiveInfinity;
                    phraseStart = previousPhraseEnd;
                    previousPhraseEnd = 0.0;
                    previousAverageSpeed = TokenSpeed.Fast;

                    // Restart at current index
                    index -= 1;
                }
                else
                {
                    best = averageError;
                    previousPhraseEnd = phraseStart;
                    previousAverageSpeed = averageSpeed;
                }
            }
            
            res.Add(new TimedTokenMelodyMeasure());
        }

        return new TimedTokenMelody { Tokens = res };
    }
    
    public static TimedTokenMelody TokenizeTiming2(TokenMelody tokenMelody, LeadSheet? leadSheet)
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
        
        // Remove swing
        var swing = (leadSheet?.Style ?? StyleEnum.Straight) == StyleEnum.Swing;
        for (var index = 0; index < tokens.Count - 1; index++)
        {
            var token = tokens[index];
        
            var newTime = RemoveSwing(token.Time, swing);
            var newLength = RemoveSwing(token.Time + token.Length, swing) - newTime;

            tokens[index] = token with { Time = newTime, Length = newLength };
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
            currentSpeed = startTime.ToSpeed();
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
                var newSpeed = token.Length.ToSpeed();

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

    public static double RemoveSwing(double time, bool enable)
    {
        // Early return
        if (!enable) return time;
        
        // Truncate
        var trunc = (int)Math.Truncate(time);
        time -= trunc;

        if (time < 0.666)
            time = time / 0.666 * 0.5;
        else
            time = 1 - (1 - time) / 0.333 * 0.5;

        time += trunc;

        return time;
    }

    public static double ApplySwing(double time, bool enable)
    {
        // Early return
        if (!enable) return time;
        
        // Truncate
        var trunc = (int)Math.Truncate(time);
        time -= trunc;

        if (time < 0.0 || time > 1.0)
            throw new Exception("asdfhiuoerhtuoi");

        if (time < 0.5)
            time = time / 0.5 * 0.66;
        else
            time = 1 - ((1 - time) / 0.5 * 0.33);

        time += trunc;

        return time;
    }
    
    private record Phrase(List<TokenMelodyToken> Tokens, double Start, double End, TokenSpeed FinalSpeed);
    public static TokenMelody ReconstructTiming(TimedTokenMelody timedTokenMelody, LeadSheet leadSheet)
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
                        {
                            var newTime = phrase.Start + i * newTokenTime;
                            var newLength = newTokenTime;

                            // Apply swing
                            var swing = leadSheet.Style == StyleEnum.Swing;
                            var oldTime = newTime;
                            newTime = ApplySwing(newTime, swing);
                            newLength = ApplySwing(oldTime + newLength, swing) - newTime;
                            
                            res.Tokens.Add(
                                phraseToken with
                                {
                                    Time = currentMeasureNum + newTime,
                                    Length = newLength
                                }
                            );
                        }
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
