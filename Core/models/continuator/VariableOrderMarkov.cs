using System.Diagnostics.CodeAnalysis;
using Core.models.continuator.belief_propag;
using MathNet.Numerics.LinearAlgebra.Double;
using NRandom.Collections;
using NRandom.Linq;

namespace Core.models.continuator;

using VpType = int;

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

public class Counter<T> where T : notnull
{
    public Dictionary<T, int> Counts;
    
    public Counter(IEnumerable<T> elements)
    {
        Counts = new();

        foreach (var element in elements)
            Counts[element] = Counts.GetValueOrDefault(element, 0) + 1;
    }
}


public class VariableOrderMarkov<T> where T : notnull
{
    public class VpListEqualityComparer : IEqualityComparer<List<VpType>>
    {
        public bool Equals(List<VpType>? x, List<VpType>? y) => x!.SequenceEqual(y!);

        public int GetHashCode(List<VpType> list) =>
            list.Aggregate(17, (hash, vp) => hash * 31 + vp.GetHashCode());
    }
    
    public Func<T, VpType>? ViewpointLambda;

    public const VpType StartPadding = -1000;
    public const VpType EndPadding = -1001;

    public int KMax;
    public List<List<T>> InputSequences;
    public List<VpType> AllUniqueViewpoints;
    public Dictionary<VpType, List<(int, int)>> ViewpointsRealizations;
    public List<Dictionary<List<VpType>, List<VpType>>> PrefixesToContinuations;

    public Random Rng = new();
    
    public VariableOrderMarkov(List<T> sequenceOfStuff, Func<T, VpType>? vpLambda, int kMax = 5)
    {
        ViewpointLambda = vpLambda;
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
            PrefixesToContinuations.Add(new(new VpListEqualityComparer()));
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
        // the number of unique viewpoints, including Start and End viewpoints
        return AllUniqueViewpoints.Count;
    }

    public VpType RandomInitialVp()
    {
        // returns a random initial vp, which are continuations of start paddings
        var allInitialVps = PrefixesToContinuations[0][[StartPadding]];
        return allInitialVps.RandomElement();
    }

    public VpType RandomVpWithProbs(double[] probs)
    {
        var idx = RandomChoices.Choice(Enumerable.Range(0, probs.Length), probs);
        return GetAllUniqueViewpoints()[idx];

    }

    public List<VpType> GetAllUniqueViewpoints()
    {
        return AllUniqueViewpoints;
    }

    public List<VpType> GetAllUniqueViewpointsExceptPaddings()
    {
        var vps = AllUniqueViewpoints.Select(it => it).ToList();
        vps.RemoveAt(vps.IndexOf(StartPadding));
        vps.RemoveAt(vps.IndexOf(EndPadding));

        return vps.ToList();
    }

    public int IndexOfVp(VpType vp)
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
            if (!ViewpointsRealizations.ContainsKey(vp))
                ViewpointsRealizations[vp] = [];
            
            AddViewpointRealization(i, sequenceIndex, vp);
        }
        
        // Populate the prefixes-to-continuations with vp contexts to vps
        for (var k = 0; k < KMax; k++)
        {
            var prefixesToContK = PrefixesToContinuations[k];
            for (var i = 0; i < vpSequence.Length - k; i++)
            {
                if (i < k + 1) continue;

                var currentCtx = vpSequence[(i - k - 1)..i].ToList();
                // TODO: deep equals (you could pass an equality class to a dictionary?)
                if (!prefixesToContK.ContainsKey(currentCtx))
                    prefixesToContK[currentCtx] = [];
                prefixesToContK[currentCtx].Add(vpSequence[i]);
            }

            PrefixesToContinuations[k] = prefixesToContK;
        }
        
        // Special case for the endVp, which has no continuation, but should be in the list for consistency
        List<VpType> endTuple = [EndPadding];
        // TODO: deep equals
        if (PrefixesToContinuations[0].ContainsKey(endTuple))
            // Ends goes to end
            PrefixesToContinuations[0][endTuple] = [EndPadding];
    }

    public List<double> GetPriors()
    {
        // There is no start and end vps in this list
        Dictionary<VpType, int> keyCounts = [];
        foreach (var (key, continuations) in ViewpointsRealizations)
            keyCounts[key] = continuations.Count;
        
        var totalCount = keyCounts.Values.Sum();
        
        Dictionary<VpType, double> priors = [];
        foreach (var (key, count) in keyCounts)
            priors[key] = (double)count / totalCount;
        
        // Step 4: Convert to a sorted vector (optional)
        var sortedKeys = GetAllUniqueViewpointsExceptPaddings();
        
        // Ensure consistent ordering
        var probabilityVector = sortedKeys.Select(key => priors[key]).ToList();

        return probabilityVector;
    }

    public List<VpType> SampleZeroOrder(int k)
    {
        var priors = GetPriors();
        return RandomChoices.Choices(GetAllUniqueViewpointsExceptPaddings(), priors, k).ToList();
    }

    public void AddViewpointRealisationOld(int i, int sequenceIndex, VpType vp)
    {
        // Attention! VP sequence has extra start_vp, so i should be decreased by 1!
        var newAddress = (sequenceIndex, i);
        ViewpointsRealizations[vp].Add(newAddress);
    }

    public void AddViewpointRealisationNew(int i, int sequenceIndex, VpType vp)
    {
        // Adds only if different from existing ones, to avoid inflation in case of monotonous pieces
        var newAddress = (sequenceIndex, i);
        
        if (IsStartingAddress(newAddress) || IsEndingAddress(newAddress))
        {
            // Starting address are added, cause useful at rendering time
            ViewpointsRealizations[vp].Add(newAddress);
            return;
        }

        var newNote = GetInputObject(newAddress);
        foreach (var real in ViewpointsRealizations[vp])
        {
            var realNote = GetInputObject(real);
            // TODO: IsSimilarRealization is defined in Note
            // if (realNote.IsSimilarRealization(newNote))
            //     return;
        }
        
        ViewpointsRealizations[vp].Add(newAddress);
    }

    public void AddViewpointRealization(int i, int sequenceIndex, VpType vp) =>
        AddViewpointRealisationOld(i, sequenceIndex, vp);

    public DenseMatrix GetFirstOrderMatrix()
    {
        // Returns the matrix for first order Markov transitions
        // all states. This includes start and end padding states
        var keys = GetAllUniqueViewpoints();
        var result = new DenseMatrix(keys.Count, keys.Count);
        var k0 = PrefixesToContinuations[0];
        for (var iVp = 0; iVp < keys.Count; iVp++)
        {
            var vp = keys[iVp];
            // TODO this was changed
            if (!k0.TryGetValue([vp], out var conts))
                continue;
            var occurrences = new Counter<VpType>(conts);
            foreach (var (vp2, v) in occurrences.Counts)
            {
                var iVp2 = keys.IndexOf(vp2);
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
            return (VpType)realObject;
        return ViewpointLambda((T)realObject);
    }

    public List<(int, int)> GetRealisationsForVp(VpType vp)
    {
        return ViewpointsRealizations[vp];
    }

    // Removed RandomStartingNote: not used
    // public dynamic RandomStartingNote()
    // {
    //     var startingVp = (-1, 0);
    //     var startingConts = GetRealisationsForVp(startingVp);
    //     var start = startingConts.RandomElement();
    //     return start;
    // }

    public dynamic? SampleSequenceThatEnds(VpType startVp, int length = 50)
    {
        // If length is negative, stops when reaching the provided end_viewpoint
        // If nb_sequence is positive, stops after nb_sequences occurrences of the end_vp

        var pgm = BuildBpGraph(length);
        // Sets constraints on start and end
        pgm.SetValue("x1", IndexOfVp(startVp));
        pgm.SetValue($"x{length + 2}", IndexOfVp(EndPadding));
        // with BP
        try
        {
            var vpSeq = SampleVpSequenceWithBp(startVp, length, pgm);
            return vpSeq;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public List<int>? SampleSequence(int length, Dictionary<int, VpType>? constraints = null)
    {
        // If length is negative, stops when reaching the provided end_viewpoint
        // If nb_sequence is positive, stops after nb_sequences occurrences of the end_vp
        if (InputSequences is [])
            return null;

        var pgm = BuildBpGraph(length);
        VpType? startVp = null;
        if (constraints != null)
        {
            foreach (var (ctPos, ctVp) in constraints)
            {
                var varName = $"x{ctPos + 1}";
                pgm.SetValue(varName, IndexOfVp(ctVp));
            }

            if (constraints.ContainsKey(0))
                startVp = constraints[0];
        }

        // TODO: this used to be in a try-block
        var vpSeq = SampleVpSequenceWithBp(length, startVp, pgm);
        return vpSeq;
    }

    public PGM BuildBpGraph(int length)
    {
        var str = "";
        for (var i = 0; i < length; i++)
            str = str + $"p(x{i + 1})";
        for (var i = 2; i < length + 1; i++)
            str = str + $"p(x{i}|x{i - 1})";

        var mat = GetFirstOrderMatrix().Transpose().ToArray();

        var pgm = PGM.FromString(str);
        
        // assert is_conditional_prob(mat, "x2")
        var m = VocSize();
        Dictionary<string, LabeledArray> dataDict = [];
        for (var i = 0; i < length; i++)
        {
            // BUG: np.random.uniform(1 / m, 1 / m, m) gives the same value m times
            var variableDist = Enumerable.Repeat(1.0 / m, m).ToArray();

            variableDist[IndexOfVp(StartPadding)] = 0;
            variableDist[IndexOfVp(EndPadding)] = 0;

            var sum = variableDist.Sum();
            for (var j = 0; j < m; j++)
                variableDist[j] /= sum;

            // TODO: 1D and 2D arrays are interspersed
            dataDict[$"p(x{i + 1})"] = new LabeledArray(variableDist, [$"x{i + 1}"]); // 1D
            dataDict[$"p(x{i + 2}|x{i + 1})"] = new LabeledArray(mat, [$"x{i + 2}", $"x{i + 1}"]); // 2D
        }

        pgm.SetData(dataDict);
        return pgm;
    }

    public static bool IsOk(List<double> marginal)
    {
        return marginal.All(x => !double.IsNaN(x));
    }

    public List<VpType>? SampleVpSequenceWithBp(int length, VpType? startVp, PGM pgm)
    {
        // Generates a new sequence of vps from the Markov model.
        if (length < 0)
            Console.WriteLine("impossible");

        List<VpType> currentSeq;
        if (startVp is { } startVpNotNull)
            currentSeq = [startVpNotNull];
        else
        {
            try
            {
                var marginal1 = new Messages().Marginal(pgm.VariableFromName("x1"));
                var vp = RandomVpWithProbs(marginal1);
                currentSeq = [vp];
                // TODO: currentSeq[0] --> so just vp then?
                pgm.SetValue("x1", IndexOfVp(currentSeq[0]));
            }
            catch (Exception)
            {
                return null;
            }
        }
        
        // Generate the rest of the sequence
        var firstOrderMatrix = GetFirstOrderMatrix();
        for (var i = 0; i < length - 1; i++)
        {
            var pgmVariable = pgm.VariableFromName($"x{i + 2}");
            List<double> marginalI;
            
            // TODO: this was previously in a try-block, returning null if throwing
            marginalI = new Messages().Marginal(pgmVariable);
            
            // Compare with the markov transition matrix
            // TODO: I assume that indexing the matrix with an int returns its ith row?
            List<double> markovProba = firstOrderMatrix.Row(IndexOfVp(currentSeq[^1])).ToList();
            var productProba = marginalI.Zip(markovProba).Select(item => item.First * item.Second).ToList();
            var cont = GetContinuationWithBp(currentSeq, productProba);
            if (cont == -1)
            {
                Console.WriteLine("should not be here, there is always a continuation with BP");
                cont = RandomInitialVp();
            }
            currentSeq.Add(cont);
            pgm.SetValue($"x{i + 2}", IndexOfVp(cont));
        }

        return currentSeq;
    }

    public List<VpType> SampleVpSequence(VpType startVp, int length, VpType endVp)
    {
        // Generates a new sequence of vps from the Markov model
        List<VpType> currentSeq = [startVp];

        if (length >= 0)
        {
            for (var i = 0; i < length; i++)
            {
                var cont = GetContinuation(currentSeq);
                if (cont == -1)
                {
                    Console.WriteLine("restarting from scratch");
                    cont = RandomInitialVp();
                }
                currentSeq.Add(cont);
            }

            return currentSeq;
        }

        while (true)
        {
            var cont = GetContinuation(currentSeq);
            if (cont == -1)
            {
                Console.WriteLine("restarting from scratch");
                cont = RandomInitialVp();
            }

            if (cont == endVp)
            {
                Console.WriteLine("found the end");
                if (cont != EndPadding)
                    currentSeq.Add(cont);
                return currentSeq;
            }
            
            currentSeq.Add(cont);
        }
    }

    public VpType GetContinuation(List<VpType> currentSeq)
    {
        VpType? vpToSkip = null;
        for (var k = KMax; k > 0; k--)
        {
            if (k > currentSeq.Count)
                continue;

            var continuationsDict = PrefixesToContinuations[k - 1];
            var viewpointCtx = currentSeq.ToArray()[^k..].ToList();
            if (continuationsDict.ContainsKey(viewpointCtx))
            {
                var allContVps = continuationsDict[viewpointCtx];
                // considers the number of different viewpoints, not the number of continuations as they are repeated
                if (allContVps.Distinct().Count() == 1 && k > 1)
                {
                    // proba to skip is proportional to order
                    if (Rng.NextDouble() > 1.0 / (k + 1))
                    {
                        // print(f"skipping continuation for {k=}")
                        vpToSkip = allContVps[0];
                        continue;
                    }
                    else
                    {
                        vpToSkip = null;
                    }
                }

                List<VpType> contsToUse;
                if (vpToSkip is { } vpToSkipNotNull && k > 1)
                    contsToUse = allContVps.Where(c => c != vpToSkipNotNull).ToList();
                else
                    contsToUse = allContVps;

                var nextContinuation = contsToUse.RandomElement();
                return nextContinuation;
            }
        }

        Console.WriteLine("no continuation found");
        return -1;
    }

    public VpType GetContinuationWithBp(List<VpType> currentSeq, List<double> probs)
    {
        VpType? vpToSkip = null;
        for (var k = KMax; k > 0; k--)
        {
            if (k > currentSeq.Count)
                continue;

            var continuationsDict = PrefixesToContinuations[k - 1];
            var viewpointsCtx = currentSeq.ToArray()[^k..].ToList();
            if (continuationsDict.ContainsKey(viewpointsCtx))
            {
                // filters out the continuations with low probabilities
                // BUG: we're filtering for >0, is that not useless?
                var allContVps = continuationsDict[viewpointsCtx]
                    .Where(vp => probs[IndexOfVp(vp)] > 0)
                    .ToList();

                if (allContVps is [])
                    continue;
                
                // considers the number of different viewpoints, not the number of continuations as they are repeated
                if (allContVps.Distinct().Count() == 1 && k > 1)
                {
                    if (Rng.NextDouble() > 1.0 / (k + 1))
                    {
                        vpToSkip = allContVps[0];
                        continue;
                    }
                    else
                    {
                        vpToSkip = null;
                    }
                }

                List<VpType> contsToUse;
                if (vpToSkip is { } vpToSkipNotNull && k > 1)
                    contsToUse = allContVps.Where(c => c != vpToSkipNotNull).ToList();
                else
                    contsToUse = allContVps;
                
                var nextContinuation = contsToUse.RandomElement();
                return nextContinuation;
            }
        }

        Console.WriteLine("no continuation found");
        return -1;
    }

    public void ShowCentsStructure()
    {
        for (var k = 0; k < KMax; k++)
        {
            var len = PrefixesToContinuations[k].Count;
            Console.WriteLine($"size of context of size {k+1}: {len}");
        }
        
        // looks at the sparsity of the matrix
        var order1 = PrefixesToContinuations[0];
        var vocSize = VocSize();
        var minSize = vocSize;
        var maxSize = 0;

        foreach (var voc in order1.Keys)
        {
            var contsSize = order1[voc].Distinct().Count();
            if (contsSize > maxSize)
                maxSize = contsSize;
            if (contsSize < minSize)
                minSize = contsSize;
        }

        Console.WriteLine($"voc size: {vocSize}");
        Console.WriteLine($"min order 1 size: {minSize}, max: {maxSize}");

        var total = 0;
        foreach (var k in ViewpointsRealizations.Keys)
        {
            total += ViewpointsRealizations[k].Count;
        }
        Console.WriteLine($"Average nb of vp realisations: {total / vocSize}");
    }
}