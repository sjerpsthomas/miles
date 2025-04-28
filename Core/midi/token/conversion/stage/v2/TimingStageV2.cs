using Accord.Math.Optimization;
using static Core.midi.token.conversion.TimedTokenMelody;

namespace Core.midi.token.conversion.stage.v2;

public static class TimingStageV2
{
    public static TimedTokenMelody TokenizeTiming(TokenMelody tokenMelody, LeadSheet? leadSheet) =>
        TimingStage.TokenizeTiming(tokenMelody, leadSheet);

    private static double Sqr(double x) => x * x;
    
    public static TokenMelody ReconstructTiming(TimedTokenMelody timedTokenMelody, LeadSheet leadSheet)
    {
        var tokens = timedTokenMelody.Tokens;

        // Early return, add measure if needed
        if (tokens is []) return new TokenMelody();
        if (tokens[^1] is not TimedTokenMelodyMeasure) tokens.Add(new TimedTokenMelodyMeasure());
        
        // Count measures, tokens, measure per token
        var m = 0;
        var n = 0;
        var currentSpeed = TokenMethods.TokenSpeed.Fast;
        List<int> M = new(tokens.Count);
        List<TokenMethods.TokenSpeed> S = new(tokens.Count);
        
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

        const double halfNoteWeight = 5.0;
        const double lengthWeight = 1.0;
        const double overtimeWeight = 0.3;
        const double neighborWeight = 0.5;
        
        var objective = new NonlinearObjectiveFunction(n, x =>
        {
            var halfNoteScore = 0.0;
            var lengthScore = 0.0;
            var neighborScore = 0.0;
            
            var onset = 0.0;
            for (var j = 0; j < n; j++)
            {
                // As many onsets need to fall close to a half-note
                halfNoteScore += Sqr(onset - (int)Math.Round(onset * 8) / 8.0);

                // Neighbors should have the same length
                if (j != n - 1)
                    neighborScore += Sqr(x[j] - x[j + 1]);
                
                // As many notes should have their specified length
                lengthScore += Sqr(x[j] - S[j].ToDouble());
                
                onset += x[j];
            }

            // The phrase should fit in the measures
            var overtimeScore = Sqr(onset - m);

            // Multiply scores by weights, normalize, return
            return
                halfNoteWeight * halfNoteScore / n +
                lengthWeight * lengthScore / n +
                neighborScore * neighborWeight / n +
                overtimeWeight * overtimeScore;
        });

        var cobyla = new Cobyla(objective);
        
        var initialGuess = Enumerable.Range(0, n).Select(_ => (double)m / n).ToArray();

        cobyla.MaxIterations = 150;
        cobyla.Minimize(initialGuess);

        Console.WriteLine($"Took {cobyla.Iterations} iterations");
        
        var solution = cobyla.Solution;
        
        var i = 0;
        var t = 0.0;
        var res = new TokenMelody();
        foreach (var token in timedTokenMelody.Tokens)
        {
            if (token is TimedTokenMelodyMeasure or TimedTokenMelodySpeed)
                continue;
            
            // Apply swing
            var swing = leadSheet.Style == LeadSheet.StyleEnum.Swing;
            var tokenTime = TimingStage.ApplySwing(t, swing);
            var tokenLength = TimingStage.ApplySwing(t + solution[i], swing) - tokenTime;
            
            if (token is TimedTokenMelodyNote(var scaleNote, var velocity1))
                res.Tokens.Add(new TokenMelody.TokenMelodyNote(scaleNote, tokenTime, tokenLength, velocity1));
            else if (token is TimedTokenMelodyPassingTone(var velocity2))
                res.Tokens.Add(new TokenMelody.TokenMelodyPassingTone(tokenTime, tokenLength, velocity2));
            
            t += solution[i++];
        }

        return res;
    }
}