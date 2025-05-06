using Accord.Math.Optimization;
using Core.midi;
using MathNet.Numerics;
using static Core.tokens.v2.conversion.V2_TimedTokenMelody;
using static Core.tokens.v2.conversion.V2_TokenMelody;

namespace Core.tokens.v2.conversion.stage;

public static class V2_TimingStage
{
    public static V2_TimedTokenMelody TokenizeTiming(V2_TokenMelody tokenMelody, LeadSheet? leadSheet)
    {
        var tokens = tokenMelody.Tokens;

        // Delete small notes
        for (var index = 0; index < tokens.Count; index++)
        {
            if (tokens[index].Length > 0.02) continue;

            // Console.WriteLine("Small note removed");
            tokens.RemoveAt(index);
            index -= 1;
        }
        
        // Early return
        if (tokens is [])
            return new V2_TimedTokenMelody();
        
        // Remove swing
        var swing = (leadSheet?.Style ?? LeadSheet.StyleEnum.Straight) == LeadSheet.StyleEnum.Swing;
        for (var index = 0; index < tokens.Count; index++)
        {
            var token = tokens[index];

            var newOnset = RemoveSwing(token.Time, swing);
            var newRelease = RemoveSwing(token.Time + token.Length, swing);
            
            tokens[index] = token with { Time = newOnset, Length = newRelease - newOnset };
        }

        
        var measureCount = tokens.Max(it => (int)Math.Truncate(it.Time - 0.03)) + 1;
        
        // Keep track of units, current speed and measure
        List<V2_TimedTokenMelodyToken> res = [];
        var currentSpeed = V2_TokenMethods.V2_TokenSpeed.Fast;
        var currentMeasure = (int)Math.Truncate(tokens[0].Time);
        
        // Add initial rest if needed
        if (tokens[0].Time > 0.05)
        {
            // TODO: what to do if the rest is too long?
            
            res.Add(new V2_TimedTokenMelodyRest());
        }
        
        // Add dummy note to end of tokens
        tokens.Add(new V2_TokenMelodyNote(0, measureCount, 0, 0));
        
        // Generate tokens
        for (var index = 0; index < tokens.Count - 1; index++)
        {
            var token = tokens[index];
            var nextToken = tokens[index + 1];
            
            var restLength = nextToken.Time - (token.Time + token.Length);
            var addRest = true;

            if (restLength < 0.125)
            {
                token = token with { Length = nextToken.Time - token.Time };
                addRest = false;
            }
            
            // Handle speed
            var tokenSpeed = token.Length.V2_ToSpeed();
            if (currentSpeed != tokenSpeed)
            {
                currentSpeed = tokenSpeed;
                res.Add(new V2_TimedTokenMelodySpeed(tokenSpeed));
            }
            
            // Add token (and maybe rest) to measure
            if (token is V2_TokenMelodyNote(var nScaleTone, _, _, var nVelocity))
                res.Add(new V2_TimedTokenMelodyNote(nScaleTone, nVelocity));
            else if (token is V2_TokenMelodyPassingTone(_, _, var ptVelocity))
                res.Add(new V2_TimedTokenMelodyPassingTone(ptVelocity));
            
            // Potentially add rest
            if (addRest)
                res.Add(new V2_TimedTokenMelodyRest());
            
            // Handle measure
            var tokenMeasure = (int)Math.Truncate(token.Time);
            if (currentMeasure != tokenMeasure)
            {
                currentMeasure = tokenMeasure;
                res.Add(new V2_TimedTokenMelodyMeasure());
            }
        }

        return new V2_TimedTokenMelody { Tokens = res };
    }


    public static double RemoveSwing(double time, bool enable)
    {
        // Early return
        if (!enable) return time;

        time *= 4;
        
        // Truncate
        var trunc = (int)Math.Truncate(time);
        time -= trunc;

        if (time < 0.6666)
            time = time / 0.6666 * 0.5;
        else
            time = 1 - (1 - time) / 0.3333 * 0.5;

        time += trunc;

        time /= 4;

        return time;
    }

    public static double ApplySwing(double time, bool enable)
    {
        // Early return
        if (!enable) return time;

        time *= 4;
        
        // Truncate
        var trunc = (int)Math.Truncate(time);
        time -= trunc;

        if (time < 0.5)
            time = time / 0.5 * 0.6666;
        else
            time = 1 - ((1 - time) / 0.5 * 0.3333);

        time += trunc;

        time /= 4;

        return time;
    }
    
    private class ReconstructProblem
    {
        private const double HalfNoteWeight = 1.0;
        private const double LengthWeight = 0.1;
        private const double OvertimeWeight = 3.0;
        private const double ShortNoteWeight = 1.0;

        private readonly int MeasureCount;
        private readonly int VariableCount;
        private readonly double[] Speeds;
        private readonly List<bool> IsTone;
        private readonly int ToneCount;
        
        public ReconstructProblem(int measureCount, int variableCount, List<V2_TokenMethods.V2_TokenSpeed> speeds, List<bool> isTone)
        {
            MeasureCount = measureCount;
            VariableCount = variableCount;
            Speeds = speeds.Select(it => it.V2_ToDouble()).ToArray();
            IsTone = isTone;
            ToneCount = IsTone.Count(it => it);
        }

        private double Abs(double x) => Math.Abs(x);
        
        public double Value(double[] x)
        {
            var halfNoteScore = 0.0;
            var lengthScore = 0.0;
            var shortNoteScore = 0.0;
            
            var onset = 0.0;
            for (var j = 0; j < VariableCount; j++)
            {
                if (IsTone[j])
                {
                    // Notes may not be very short
                    if (x[j] < 0.0625)
                        shortNoteScore += 1.0;

                    // Notes should prefer to start on half-notes
                    halfNoteScore += Abs(onset - Math.Round(onset * 8.0) / 8);
                }
                
                // As many notes/rests should have their specified length
                lengthScore += Abs(x[j] - Speeds[j]);
                
                onset += x[j];
            }

            // The phrase should fit in the measures
            var overtimeScore = Abs(onset - MeasureCount);

            // Console.WriteLine(string.Join("; ", x.Select(it => $"{it:F2}")));
            
            // Multiply scores by weights, normalize, return
            return
                HalfNoteWeight * halfNoteScore / ToneCount +
                LengthWeight * lengthScore / VariableCount +
                ShortNoteWeight * shortNoteScore / ToneCount +
                OvertimeWeight * overtimeScore;
        }

        public double[] InitialPosition()
        {
            var speedsSum = Speeds.Sum();

            return Speeds
                .Select(it => it / speedsSum * MeasureCount * 0.75)
                .ToArray();
        }
    }
    
    public static V2_TokenMelody ReconstructTiming(V2_TimedTokenMelody timedTokenMelody, LeadSheet leadSheet)
    {
        var tokens = timedTokenMelody.Tokens;

        // Trim, early return
        while (tokens is not [] && tokens[^1] is V2_TimedTokenMelodyMeasure or V2_TimedTokenMelodySpeed)
            tokens.RemoveAt(tokens.Count - 1);
        if (tokens is []) return new V2_TokenMelody();
        
        // Count measures, tokens, measure per token
        var variableCount = 0;
        var currentSpeed = V2_TokenMethods.V2_TokenSpeed.Fast;
        List<V2_TokenMethods.V2_TokenSpeed> tokenSpeeds = new(tokens.Count);
        List<bool> isTone = new(tokens.Count);
        
        foreach (var token in tokens)
        {
            switch (token)
            {
                case V2_TimedTokenMelodyNote or V2_TimedTokenMelodyPassingTone:
                    isTone.Add(true);
                    goto cont;
                    
                case V2_TimedTokenMelodyRest:
                    isTone.Add(false);
                    
                    cont:
                    tokenSpeeds.Add(currentSpeed);
                    variableCount++;
                    break;
                
                case V2_TimedTokenMelodySpeed(var speed):
                    currentSpeed = speed;
                    break;
                
                case V2_TimedTokenMelodyMeasure:
                    break;
            }
        }
        
        // TODO: measure count is not used
        var problem = new ReconstructProblem(4, variableCount, tokenSpeeds, isTone);
        
        var cobyla = new Cobyla(variableCount, problem.Value)
        {
            MaxIterations = 100
        };

        cobyla.Minimize(problem.InitialPosition());

        var solution = cobyla.Solution.Select(Math.Abs).ToArray();
        solution = solution.Select(it => it.RoundToPower(2)).ToArray();
        
        var i = 0;
        var t = 0.0;
        var res = new V2_TokenMelody();
        foreach (var token in timedTokenMelody.Tokens)
        {
            if (token is V2_TimedTokenMelodyMeasure or V2_TimedTokenMelodySpeed)
                continue;
            
            // Apply swing
            var swing = leadSheet.Style == LeadSheet.StyleEnum.Swing;
            
            var tokenTime = ApplySwing(t, swing);
            var tokenLength = ApplySwing(t + solution[i], swing) - tokenTime;
            
            switch (token)
            {
                case V2_TimedTokenMelodyNote(var scaleNote, var velocity):
                    res.Tokens.Add(new V2_TokenMelodyNote(scaleNote, tokenTime, tokenLength, velocity));
                    break;
                case V2_TimedTokenMelodyPassingTone(var velocity):
                    res.Tokens.Add(new V2_TokenMelodyPassingTone(tokenTime, tokenLength, velocity));
                    break;
            }
            
            t += solution[i++];
        }

        return res;
    }
}