using System.Diagnostics.CodeAnalysis;
using System.IO.IsolatedStorage;
using System.Net.Mail;
using MathNet.Numerics.LinearAlgebra.Double;
using NRandom.Collections;
using NRandom.Linq;

namespace Core.ml;

using VpType = dynamic;
using Stuff = List<dynamic>;

public static class RandomChoices
{
    public static IEnumerable<T> Choices<T>(IEnumerable<T> seq, IEnumerable<double> weights, int n)
    {
        var weightedList = new WeightedList<T>();
        foreach (var (item, weight) in seq.Zip(weights))
            weightedList.Add(item, weight);

        for (var i = 0; i < n; i++)
            yield return weightedList.GetRandom();
    }

    public static T Choice<T>(IEnumerable<T> seq, IEnumerable<double> weights) => Choices(seq, weights, 1).First();
}

public class Counter<T>
{
    public Dictionary<T, int> Counts;
    
    public Counter(IEnumerable<T> elements)
    {
        Counts = new();

        foreach (var element in elements)
            Counts[element] = Counts.GetValueOrDefault(element, 0) + 1;
    }
}

[SuppressMessage("ReSharper", "UseSymbolAlias")]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
[SuppressMessage("Performance", "CA1822:Mark members as static")]
public class VariableOrderMarkov<T>
{
    public class Continuation() { }

    public class StartVp() { }
    public class EndVp() { }
    
    public Func<VpType, dynamic>? ViewpointLambda;
    public dynamic StartPadding;
    public dynamic EndPadding;
    public int KMax;
    public List<List<T>> InputSequences;
    public List<dynamic> AllUniqueViewpoints;
    public Dictionary<VpType, List<(int, int)>> ViewpointsRealizations;
    public List<Dictionary<dynamic, List<dynamic>>> PrefixesToContinuations;

    public Random Rng = new();
    
    public VariableOrderMarkov(List<T> sequenceOfStuff, Func<dynamic, dynamic> vpLambda, int kMax = 5)
    {
        ViewpointLambda = vpLambda;
        StartPadding = new StartVp();
        EndPadding = new EndVp();
        KMax = kMax;
        
        ClearMemory();
        
        LearnSequence(sequenceOfStuff);
    }

    public void ClearMemory()
    {
        InputSequences = [];
        AllUniqueViewpoints = [];
        ViewpointsRealizations = [];

        PrefixesToContinuations = [];
        for (var k = 0; k < KMax; k++)
            PrefixesToContinuations.Add(new Continuation());
    }

    public void ClearFirstNPhrases(int n)
    {
        if (InputSequences is [])
        {
            Console.WriteLine("nothing to remove, memory is empty");
            return;
        }

        if (InputSequences.Count < n)
        {
            Console.WriteLine($"nothing to remove, memory is less than {n}");
            return;
        }

        var sequencesToLearn = InputSequences.ToArray()[n..];
        ClearMemory();
        foreach (var seq in sequencesToLearn)
            LearnSequence(seq);
    }

    public void ClearLastPhrase()
    {
        if (InputSequences is [])
        {
            Console.WriteLine("nothing to remove, memory is empty");
            return;
        }
        
        var sequencesToLearn = InputSequences.ToArray()[..^1];
        ClearMemory();
        foreach (var seq in sequencesToLearn)
            LearnSequence(seq);
    }

    public void LearnSequence(List<T> sequenceOfStuff)
    {
        InputSequences.Add(sequenceOfStuff);
        BuildVoMarkovModel(sequenceOfStuff);
    }

    public T GetInputObject((int, int) objAddress)
    {
        return InputSequences[objAddress.Item1][objAddress.Item2];
    }

    public static bool IsStartingAddress((int, int) noteAdress)
    {
        return noteAdress.Item2 == 1;
    }

    public bool IsEndingAddress((int, int) noteAddress)
    {
        return noteAddress.Item1 == InputSequences[noteAddress.Item2].Count - 2;
    }

    public bool IsEndPadding(dynamic vp)
    {
        return vp == EndPadding;
    }

    public int VocSize()
    {
        return AllUniqueViewpoints.Count;
    }

    public dynamic RandomInitialVp()
    {
        // TODO: ptc[i] takes a list, not a tuple
        var asdf = new Tuple<dynamic>(StartPadding);
        List<dynamic> allInitialVps = PrefixesToContinuations[0][asdf];

        return allInitialVps.RandomElement();
    }

    public dynamic RandomVpWithProbs(double[] probs)
    {
        var idx = RandomChoices.Choice(Enumerable.Range(0, probs.Length), probs);
        return GetAllUniqueViewpoints()[idx];

    }

    public List<dynamic> GetAllUniqueViewpoints()
    {
        return AllUniqueViewpoints;
    }

    public List<dynamic> GetAllUniqueViewpointsExceptPaddings()
    {
        var vps = AllUniqueViewpoints.Select(it => it).ToList();
        vps.RemoveAt(vps.IndexOf(StartPadding));
        vps.RemoveAt(vps.IndexOf(EndPadding));

        return vps.ToList();
    }

    public int IndexOfVp(dynamic vp)
    {
        return AllUniqueViewpoints.IndexOf(vp);
    }

    public void BuildVoMarkovModel(List<T> realSequence)
    {
        // Builds a variable-order Markov model for max K order
        // accumulates with existing model
        
        // builds the vp sequence with extra start and end padding vps
        VpType[] vpSequence = [
            StartPadding,
            ..realSequence.Select(obj => GetViewpoint(obj)),
            EndPadding
        ];
        
        // Adds unique viewpoints if any
        foreach (var vp in vpSequence)
            if (!AllUniqueViewpoints.Contains(vp))
                AllUniqueViewpoints.Add(vp);
        
        // Add the realization to the viewpoint's realizations
        var sequenceIndex = InputSequences.Count - 1;
        for (var i = 1; i < vpSequence.Length - 1; i++)
        {
            var vp = vpSequence[i];
            if (ViewpointsRealizations.ContainsKey(vp))
                ViewpointsRealizations[vp] = new List<(int, int)>();
            
            AddViewpointRealization(i, sequenceIndex, vp);
        }
        
        // Populate the prefixes-to-continuations with vp contexts to vps
        for (var k = 0; k < KMax; k++)
        {
            var prefixesToContK = PrefixesToContinuations[k];
            for (var i = 0; i < vpSequence.Length - k; i++)
            {
                if (i < k + 1) continue;

                var currentCtx = vpSequence[(i - k - 1)..i];
                // TODO: deep equals (you could pass an equality class to a dictionary?)
                if (!prefixesToContK.ContainsKey(currentCtx))
                    prefixesToContK[currentCtx] = [];
                prefixesToContK[currentCtx].Add(vpSequence[i]);
            }

            PrefixesToContinuations[k] = prefixesToContK;
        }
        
        // Special case for the endVp, which has no continuation, but should be in the list for consistency
        List<dynamic> endTuple = [EndPadding];
        // TODO: deep equals
        if (PrefixesToContinuations[0].ContainsKey(endTuple))
            // Ends goes to end
            PrefixesToContinuations[0][endTuple] = [EndPadding];
    }

    public dynamic GetPriors()
    {
        // There is no start and end vps in this list
        Dictionary<dynamic, int> keyCounts = [];
        foreach (var (key, continuations) in ViewpointsRealizations)
            keyCounts[key] = continuations.Count;
        
        var totalCount = keyCounts.Values.Sum();
        
        Dictionary<dynamic, double> priors = [];
        foreach (var (key, count) in keyCounts)
            priors[key] = (double)count / totalCount;
        
        // Step 4: Convert to a sorted vector (optional)
        var sortedKeys = GetAllUniqueViewpointsExceptPaddings();
        
        // Ensure consistent ordering
        var probabilityVector = sortedKeys.Select(key => (double)priors[key]).ToList();

        return probabilityVector;
    }

    public dynamic SampleZeroOrder(int k)
    {
        var priors = GetPriors();
        return RandomChoices.Choices(GetAllUniqueViewpointsExceptPaddings(), priors, k);
    }

    public void AddViewpointRealisationOld(int i, int sequenceIndex, dynamic vp)
    {
        // Attention! VP sequence has extra start_vp, so i should be decreased by 1!
        var newAddress = (sequenceIndex, i);
        ((List<(int, int)>)ViewpointsRealizations[vp]).Add(newAddress);
    }

    public void AddViewpointRealisationNew(int i, int sequenceIndex, dynamic vp)
    {
        // Adds only if different from existing ones, to avoid inflation in case of monotonous pieces
        var newAddress = (sequenceIndex, i);
        
        if (IsStartingAddress(newAddress) || IsEndingAddress(newAddress))
        {
            // Starting address are added, cause useful at rendering time
            ((List<(int, int)>)ViewpointsRealizations[vp]).Add(newAddress);
            return;
        }

        var newNote = GetInputObject(newAddress);
        foreach (var real in ViewpointsRealizations[vp])
        {
            var realNote = GetInputObject(real);
            // TODO: IsSimilarRealization is defined in Note
            if (realNote.IsSimilarRealization(newNote))
                return;
        }
        
        ((List<(int, int)>)ViewpointsRealizations[vp]).Add(newAddress);
    }

    public void AddViewpointRealization(int i, int sequenceIndex, dynamic vp) =>
        AddViewpointRealisationOld(i, sequenceIndex, vp);

    public dynamic GetFirstOrderMatrix()
    {
        // Returns the matrix for first order Markov transitions
        // all states. This includes start and end padding states
        var keys = GetAllUniqueViewpoints();
        var result = new DenseMatrix(keys.Count, keys.Count);
        var k0 = PrefixesToContinuations[0];
        for (var iVp = 0; iVp < keys.Count; iVp++)
        {
            var vp = keys[iVp];
            List<dynamic> conts = k0[vp];
            var occurrences = new Counter<dynamic>(conts);
            foreach (var (vp2, v) in occurrences.Counts)
            {
                int iVp2 = keys.IndexOf(vp2);
                result[iVp, iVp2] = v;
            }

            var sum = Enumerable.Range(0, keys.Count).Select(it => result[iVp, it]).Sum();
            for (var jVp = 0; jVp < keys.Count; jVp++)
                result[iVp, jVp] /= sum;
        }

        return result;
    }

    public VpType GetViewpoint(dynamic realObject)
    {
        if (ViewpointLambda is null)
            return realObject;
        return ViewpointLambda(realObject);
    }

    public List<(int, int)> GetRealisationsForVp(dynamic vp)
    {
        return ViewpointsRealizations[vp];
    }

    public dynamic RandomStartingNote()
    {
        var startingVp = (-1, 0);
        var startingConts = GetRealisationsForVp(startingVp);
        var start = startingConts.RandomElement();
        return start;
    }

    public dynamic SampleSequenceThatEnds(dynamic startVp, int length = 50)
    {
        
    }

    public dynamic SampleSequence(int length, dynamic constraints = null)
    {
        
    }

    public dynamic BuildBpGraph(int length)
    {
        
    }

    public static bool IsOk(dynamic marginal)
    {
        
    }

    public dynamic SampleVpSequenceWithBp(int length, dynamic startVp, dynamic pgm)
    {
        
    }

    public dynamic SampleVpSequence(dynamic startVp, int length, dynamic endVp)
    {
        
    }

    public int GetContinuation(dynamic currentSeq)
    {
        
    }

    public int GetContinuationWithBp(dynamic currentSeq, dynamic probs)
    {
        
    }

    public void ShowCentsStructure()
    {
        
    }
}