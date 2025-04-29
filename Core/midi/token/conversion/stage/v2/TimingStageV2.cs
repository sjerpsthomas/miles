using NAudio.Midi;
using Quipu;
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

            if (restLength < 0.8 * token.Length)
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

        if (time < 0.666)
            time = time / 0.666 * 0.5;
        else
            time = 1 - (1 - time) / 0.333 * 0.5;

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
            time = time / 0.5 * 0.66;
        else
            time = 1 - ((1 - time) / 0.5 * 0.33);

        time += trunc;

        time /= 4;

        return time;
    }
    
    
    
    
    private class ReconstructProblem: IVectorFunction, IStartingPoint
    {
        const double HalfNoteWeight = 1.0;
        const double LengthWeight = 5.0;
        const double OvertimeWeight = 0.3;
        const double NeighborWeight = 0.5;

        public int M;
        public int N;
        public double[] S;

        public ReconstructProblem(int m, int n, List<TokenSpeed> s)
        {
            M = m;
            N = n;
            S = s.Select(it => it.ToDouble()).ToArray();
        }

        public double Value(double[] x)
        {
            var halfNoteScore = 0.0;
            var lengthScore = 0.0;
            var neighborScore = 0.0;
            
            var onset = 0.0;
            for (var j = 0; j < N; j++)
            {
                // As many onsets need to fall close to a half-note
                halfNoteScore += onset - (int)Math.Round(onset * 8) / 8.0;

                // Neighbors should have the same length
                if (j != N - 1)
                    neighborScore += x[j] - x[j + 1];
                
                // As many notes should have their specified length
                lengthScore += x[j] - S[j];
                
                onset += x[j];
            }

            // The phrase should fit in the measures
            var overtimeScore = onset - M;

            // Multiply scores by weights, normalize, return
            return
                HalfNoteWeight * halfNoteScore / N +
                LengthWeight * lengthScore / N +
                neighborScore * NeighborWeight / N +
                OvertimeWeight * overtimeScore;
        }

        public int Dimension => N;
        
        public double[][] create(int obj0) => [Enumerable.Range(0, N).Select(_ => (double)M / N).ToArray()];
    }
    public static TokenMelody ReconstructTiming(TimedTokenMelody timedTokenMelody, LeadSheet leadSheet)
    {
        var tokens = timedTokenMelody.Tokens;

        // Early return, add measure if needed
        if (tokens is []) return new TokenMelody();
        if (tokens[^1] is not TimedTokenMelodyMeasure) tokens.Add(new TimedTokenMelodyMeasure());
        
        // Count measures, tokens, measure per token
        var m = 0;
        var n = 0;
        var currentSpeed = TokenSpeed.Fast;
        List<int> M = new(tokens.Count);
        List<TokenSpeed> S = new(tokens.Count);
        
        foreach (var token in tokens)
        {
            switch (token)
            {
                case TimedTokenMelodyNote or TimedTokenMelodyPassingTone or TimedTokenMelodyRest:
                    M.Add(m);
                    S.Add(currentSpeed);
                    n++;
                    break;
                case TimedTokenMelodySpeed(var speed):
                    currentSpeed = speed;
                    break;
                case TimedTokenMelodyMeasure:
                    m++;
                    break;
            }
        }

        var problem = new ReconstructProblem(m, n, S);
        var result = Quipu.CSharp.NelderMead
            .Objective(problem)
            .WithMaximumIterations(50)
            .StartFrom(problem)
            .Minimize();

        var solution = (result as SolverResult.Abnormal)!.Item[0];
        
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
            
            if (token is TimedTokenMelodyNote(var scaleNote, var velocity1))
                res.Tokens.Add(new TokenMelodyNote(scaleNote, tokenTime, tokenLength, velocity1));
            else if (token is TimedTokenMelodyPassingTone(var velocity2))
                res.Tokens.Add(new TokenMelodyPassingTone(tokenTime, tokenLength, velocity2));
            
            t += solution[i++];
        }

        return res;
    }
}