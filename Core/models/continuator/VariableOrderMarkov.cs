using NRandom.Linq;

namespace Core.models.continuator;

using VpType = int;

public class VariableOrderMarkov<T>
{
    public class VpArrEqualityComparer : IEqualityComparer<VpType[]>
    {
        public bool Equals(VpType[]? x, VpType[]? y) => x!.SequenceEqual(y!);

        public int GetHashCode(VpType[] list) =>
            list.Aggregate(17, (hash, vp) => hash * 31 + vp.GetHashCode());
    }
    
    private const VpType StartPadding = -1000;
    private const VpType EndPadding = -1001;

    public Func<T, VpType> Map;
    public Dictionary<VpType, T> Mapping;
    
    public int KMax;
    private List<Dictionary<VpType[], List<VpType>>> PrefixesToContinuations;

    private Random Rng = new();
    
    public VariableOrderMarkov(Func<T, VpType> map, int kMax = 5)
    {
        Map = map;
        Mapping = [];
        
        // Initialize KMax
        KMax = kMax;

        PrefixesToContinuations = [];
        for (var k = 0; k < KMax; k++)
            PrefixesToContinuations.Add(new(new VpArrEqualityComparer()));
    }

    private VpType GetRandomVp()
    {
        // returns a random initial vp, which are continuations of start paddings
        var allInitialVps = PrefixesToContinuations[0][[StartPadding]];
        return allInitialVps.RandomElement();
    }

    public void LearnSequence(List<T> items)
    {
        var vps = items
            .Select(it => Map(it))
            .ToList();

        foreach (var (item, vp) in items.Zip(vps))
            Mapping[vp] = item;
        
        // Builds a variable-order Markov model for max K order
        // accumulates with existing model
        
        // builds the vp sequence with extra start and end padding vps
        VpType[] vpSequence = [
            StartPadding,
            ..vps,
            EndPadding
        ];
        
        // Populate the prefixes-to-continuations with vp contexts to vps
        for (var k = 0; k < KMax; k++)
        {
            var prefixesToContK = PrefixesToContinuations[k];
            for (var i = 0; i < vpSequence.Length - k; i++)
            {
                if (i <= k) continue;

                var currentCtx = vpSequence[(i - k - 1)..i];
                if (prefixesToContK.TryGetValue(currentCtx, out var value))
                    value.Add(vpSequence[i]);
                else
                    prefixesToContK[currentCtx] = [vpSequence[i]];
            }
        }
    }

    public List<T> Generate(int maxLength)
    {
        // Generates a new sequence of vps from the Markov model
        List<VpType> currentSeq = [GetRandomVp()];

        for (var i = 0; i < maxLength; i++)
        {
            // Get continuation, restart from scratch if needed
            var cont = GetContinuation(currentSeq);
            if (cont == -1) cont = GetRandomVp();

            // Break if end padding found
            if (cont == EndPadding)
                break;
            
            currentSeq.Add(cont);
        }

        return currentSeq.Select(it => Mapping[it]).ToList();
    }

    public List<T> GenerateChunks(int delimVp, int chunkCount, int maxChunkSize)
    {
        // Generates a new sequence of vps from the Markov model
        List<VpType> currentSeq = [GetRandomVp()];

        var currentChunk = 0;
        var currentChunkSize = 0;
        
        while (true)
        {
            // Get continuation, restart from scratch if needed
            var cont = GetContinuation(currentSeq);
            if (cont is -1 or EndPadding) cont = GetRandomVp();
            
            currentSeq.Add(cont);
            currentChunkSize++;

            if (cont == delimVp)
                Console.WriteLine("Measure!");
            
            if (currentChunkSize == maxChunkSize)
            {
                currentSeq.Add(delimVp);
                cont = delimVp;
            }
            
            if (cont == delimVp)
            {
                currentChunk++;
                if (currentChunk == chunkCount)
                    break;
            }
        }

        return currentSeq.Select(it => Mapping[it]).ToList();
    }

    private VpType GetContinuation(List<VpType> currentSeq)
    {
        VpType? vpToSkip = null;
        for (var k = KMax; k > 0; k--)
        {
            if (k > currentSeq.Count) continue;

            var continuationsDict = PrefixesToContinuations[k - 1];
            var viewpointCtx = currentSeq.ToArray()[^k..];
            
            if (!continuationsDict.TryGetValue(viewpointCtx, out var allContVps)) continue;

            // considers the number of different viewpoints, not the number of continuations as they are repeated
            if (allContVps.Distinct().Count() == 1 && k > 1)
            {
                // proba to skip is proportional to order
                if (Rng.NextDouble() > 1.0 / (k + 1))
                {
                    vpToSkip = allContVps[0];
                    continue;
                }
                    
                vpToSkip = null;
            }

            List<VpType> contsToUse;
            if (vpToSkip is { } vpToSkipNotNull && k > 1)
                contsToUse = allContVps.Where(c => c != vpToSkipNotNull).ToList();
            else
                contsToUse = allContVps;

            var nextContinuation = contsToUse.RandomElement();
            return nextContinuation;
        }

        Console.WriteLine("no continuation found");
        return -1;
    }
}