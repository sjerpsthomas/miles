using System.Diagnostics;
using Accord.Math.Optimization;
using Google.OrTools.PDLP;
using Google.OrTools.Sat;
using Google.OrTools.Util;
using MathNet.Numerics;
using static Core.midi.LeadSheet;
using static Core.midi.token.conversion.TimedTokenMelody;
using static Core.midi.token.conversion.TokenMelody;
using static Core.midi.token.TokenMethods;

namespace Core.midi.token.conversion.stage.v2;

public static class TimingStageV2
{
    public static TimedTokenMelody TokenizeTiming(TokenMelody tokenMelody, LeadSheet? leadSheet)
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
            return new TimedTokenMelody();
        
        // Remove swing
        var swing = (leadSheet?.Style ?? StyleEnum.Straight) == StyleEnum.Swing;
        for (var index = 0; index < tokens.Count; index++)
        {
            var token = tokens[index];

            var newOnset = RemoveSwing(token.Time, swing);
            var newRelease = RemoveSwing(token.Time + token.Length, swing);
            
            tokens[index] = token with { Time = newOnset, Length = newRelease - newOnset };
        }

        
        var measureCount = tokens.Max(it => (int)Math.Truncate(it.Time - 0.03)) + 1;
        
        // Keep track of units, current speed and measure
        List<TimedTokenMelodyToken> res = [];
        var currentSpeed = TokenSpeed.Fast;
        var currentMeasure = (int)Math.Truncate(tokens[0].Time);
        
        // Add initial rest if needed
        if (tokens[0].Time > 0.05)
        {
            // TODO: what to do if the rest is too long?
            
            res.Add(new TimedTokenMelodyRest());
        }
        
        // Add dummy note to end of tokens
        tokens.Add(new TokenMelodyNote(0, measureCount, 0, 0));
        
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
            var tokenSpeed = token.Length.ToSpeed();
            if (currentSpeed != tokenSpeed)
            {
                currentSpeed = tokenSpeed;
                res.Add(new TimedTokenMelodySpeed(tokenSpeed));
            }
            
            // Add token (and maybe rest) to measure
            if (token is TokenMelodyNote(var nScaleTone, _, _, var nVelocity))
                res.Add(new TimedTokenMelodyNote(nScaleTone, nVelocity));
            else if (token is TokenMelodyPassingTone(_, _, var ptVelocity))
                res.Add(new TimedTokenMelodyPassingTone(ptVelocity));
            
            // Potentially add rest
            if (addRest)
                res.Add(new TimedTokenMelodyRest());
            
            // Handle measure
            var tokenMeasure = (int)Math.Truncate(token.Time);
            if (currentMeasure != tokenMeasure)
            {
                currentMeasure = tokenMeasure;
                res.Add(new TimedTokenMelodyMeasure());
            }
        }

        return new TimedTokenMelody { Tokens = res };
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
        private const double OvertimeWeight = 100.0;
        private const double ShortNoteWeight = 10.0;

        public int MeasureCount;
        public int VariableCount;
        public double[] Speeds;
        public List<bool> IsTone;
        public int ToneCount;
        
        public ReconstructProblem(int measureCount, int variableCount, List<TokenSpeed> speeds, List<bool> isTone)
        {
            MeasureCount = measureCount;
            VariableCount = variableCount;
            Speeds = speeds.Select(it => it.ToDouble()).ToArray();
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
            if (onset > MeasureCount)
                overtimeScore += 100;

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
                .Select(it => it / speedsSum * MeasureCount)
                .ToArray();
        }
    }
    
    public static TokenMelody ReconstructTiming(TimedTokenMelody timedTokenMelody, LeadSheet leadSheet)
    {
        var tokens = timedTokenMelody.Tokens;

        // Early return, add measure if needed
        if (tokens is []) return new TokenMelody();
        if (tokens[^1] is not TimedTokenMelodyMeasure) tokens.Add(new TimedTokenMelodyMeasure());
        
        // Count measures, tokens, measure per token
        var measureCount = 0;
        var variableCount = 0;
        var currentSpeed = TokenSpeed.Fast;
        List<TokenSpeed> tokenSpeeds = new(tokens.Count);
        List<bool> isTone = new(tokens.Count);
        
        foreach (var token in tokens)
        {
            switch (token)
            {
                case TimedTokenMelodyNote or TimedTokenMelodyPassingTone:
                    isTone.Add(true);
                    goto cont;
                    
                case TimedTokenMelodyRest:
                    isTone.Add(false);
                    
                    cont:
                    tokenSpeeds.Add(currentSpeed);
                    variableCount++;
                    break;
                
                case TimedTokenMelodySpeed(var speed):
                    currentSpeed = speed;
                    break;
                
                case TimedTokenMelodyMeasure:
                    measureCount++;
                    break;
            }
        }
        
        var problem = new ReconstructProblem(measureCount, variableCount, tokenSpeeds, isTone);
        
        var cobyla = new Cobyla(variableCount, problem.Value)
        {
            MaxIterations = 100
        };

        cobyla.Minimize(problem.InitialPosition());

        var solution = cobyla.Solution.Select(Math.Abs).ToArray();
        solution = solution.Select(it => it.RoundToPower(2)).ToArray();
        
        var i = 0;
        var t = 0.0;
        var res = new TokenMelody();
        foreach (var token in timedTokenMelody.Tokens)
        {
            if (token is TimedTokenMelodyMeasure or TimedTokenMelodySpeed)
                continue;
            
            // Apply swing
            var swing = leadSheet.Style == StyleEnum.Swing;
            
            var tokenTime = ApplySwing(t, swing);
            var tokenLength = ApplySwing(t + solution[i], swing) - tokenTime;
            
            switch (token)
            {
                case TimedTokenMelodyNote(var scaleNote, var velocity):
                    res.Tokens.Add(new TokenMelodyNote(scaleNote, tokenTime, tokenLength, velocity));
                    break;
                case TimedTokenMelodyPassingTone(var velocity):
                    res.Tokens.Add(new TokenMelodyPassingTone(tokenTime, tokenLength, velocity));
                    break;
            }
            
            t += solution[i++];
        }

        return res;
    }
    
    
    
    
    private class UnusedReconstructProblem
    {
        public int[] PhraseAmounts;
        public int[] PhraseSpeeds;
        public int MeasureCount;

        public UnusedReconstructProblem(int[] phraseAmounts, TokenSpeed[] phraseSpeeds, int measureCount)
        {
            PhraseAmounts = phraseAmounts;
            PhraseSpeeds = phraseSpeeds.Select(it => it switch
            {
                TokenSpeed.SuperSlow => 24,
                TokenSpeed.Slow => 12,
                TokenSpeed.Fast => 6,
                TokenSpeed.SuperFast => 3,
                _ => throw new Exception("asdf")
            }).ToArray();
            MeasureCount = measureCount;
        }

        public double[] Solve()
        {
            var model = new CpModel();

            var n = PhraseAmounts.Length;
            Debug.Assert(PhraseSpeeds.Length == n);
            
            // Variables
            var xs = new IntVar[n];
            var domain = Domain.FromValues([
                24, // whole
                12, // half
                8,  // quarter
                6,  // triplet
                4   // eighth
            ]);

            for (var i = 0; i < n; i++)
                xs[i] = model.NewIntVarFromDomain(domain, $"x{i}");

            // (determines the onset of the jth note)
            LinearExpr Onset(int j)
            {
                return LinearExpr.Sum(
                    Enumerable.Range(0, j).Select(i => xs[i] * PhraseAmounts[i])
                );
            }
            
            // CONSTRAINT: notes should not go beyond the measure count
            var finalOnset = Onset(n);
            model.Add(finalOnset < (MeasureCount * 24));
            
            // OBJECTIVE: speeds should be close to preferred speeds
            var speedDiffs = new IntVar[n];
            for (var i = 0; i < n; i++)
            {
                speedDiffs[i] = model.NewIntVar(0, 24, $"speedDiff_{i}");
                model.AddAbsEquality(speedDiffs[i], xs[i] - PhraseSpeeds[i]);
            }
            // Define the sum of all diffs
            var speedDiffSum = LinearExpr.Sum(speedDiffs);
            
            // OBJECTIVE: onsets should be close to their nearest multiple of 12
            var halfNoteDiffs = new IntVar[n - 1];
            for (var i = 1; i < n; i++)
            {
                var onset = model.NewIntVar(0, MeasureCount * 24, $"onset_{i}");
                model.Add(onset == Onset(i));

                var quant = model.NewIntVar(0, MeasureCount * 24, $"quant_{i}");

                // quant = 12 * round(onset / 12)
                var div = model.NewIntVar(0, MeasureCount * 2, $"div_{i}");
                model.AddDivisionEquality(div, onset, 12);
                model.AddMultiplicationEquality(quant, new[] { div, model.NewConstant(12) });

                // diff_i = |onset - quant|
                halfNoteDiffs[i - 1] = model.NewIntVar(0, 12, $"halfNoteDiff_{i}");
                model.AddAbsEquality(halfNoteDiffs[i - 1], onset - quant);
            }
            // Define the sum of all diffs
            var halfNoteDiffSum = LinearExpr.Sum(halfNoteDiffs);
            
            // Minimize the sum of half note diffs and speed diffs
            var obj = speedDiffSum + halfNoteDiffSum;
            model.Minimize(obj);
            
            // Solve
            var solver = new CpSolver();
            solver.Solve(model);

            // Turn into lengths
            return xs.Select(solver.Value).Select(it => it switch
            {
                24 => 1.0,      // whole
                12 => 0.5,      // half
                8 => 0.3333,    // quarter
                6 => 0.25,      // triplet
                4 => 0.1666,    // eighth
                _ => 0.0
            }).ToArray();
        }
    }

    public static TokenMelody UnusedReconstructTiming(TimedTokenMelody timedTokenMelody, LeadSheet leadSheet)
    {
        var tokens = timedTokenMelody.Tokens;

        // Early return, add measure if needed
        if (tokens is []) return new TokenMelody();
        if (tokens[^1] is not TimedTokenMelodyMeasure) tokens.Add(new TimedTokenMelodyMeasure());


        // Count phrase amounts, phrase speeds, measure count
        List<int> phraseAmounts = [];
        List<TokenSpeed> phraseSpeeds = [];

        var currentAmount = 0;
        var currentSpeed = TokenSpeed.Fast;
        var measureCount = 0;
        
        foreach (var token in tokens)
        {
            switch (token)
            {
                case TimedTokenMelodyRest or TimedTokenMelodyNote or TimedTokenMelodyPassingTone:
                    currentAmount++;
                    break;
                
                case TimedTokenMelodyMeasure:
                    measureCount++;
                    break;
                
                case TimedTokenMelodySpeed(var speed):
                    if (speed == currentSpeed) continue;

                    phraseSpeeds.Add(currentSpeed);
                    phraseAmounts.Add(currentAmount);
                    
                    currentSpeed = speed;
                    currentAmount = 0;
                    break;
            }
        }

        // Handle possible last phrase
        if (currentAmount != 0)
        {
            phraseSpeeds.Add(currentSpeed);
            phraseAmounts.Add(currentAmount);
        }

        // Create problem, solve
        var problem2 = new UnusedReconstructProblem(
            phraseAmounts.ToArray(),
            phraseSpeeds.ToArray(),
            measureCount
        );
        
        var solution = problem2.Solve();
        
        // Get tokens
        List<TokenMelodyToken> res = [];
        var solutionIndex = 0;
        var t = 0.0;
        currentSpeed = TokenSpeed.Fast;
        
        foreach (var token in tokens)
        {
            switch (token)
            {
                case TimedTokenMelodyNote(var scaleNote, var velocity):
                {
                    var length = solution[solutionIndex];
                    res.Add(new TokenMelodyNote(scaleNote, t, length, velocity));

                    t += length;
                    break;
                }
                case TimedTokenMelodyPassingTone(var velocity):
                {
                    var length = solution[solutionIndex];
                    res.Add(new TokenMelodyPassingTone(t, length, velocity));

                    t += length;
                    break;
                }
                case TimedTokenMelodySpeed(var speed):
                    if (currentSpeed != speed)
                    {
                        currentSpeed = speed;
                        solutionIndex++;
                    }
                    break;
            }
        }


        return new TokenMelody { Tokens = res };
    }
}