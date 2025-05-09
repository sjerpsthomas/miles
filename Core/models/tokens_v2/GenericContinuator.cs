using NRandom.Linq;

namespace Core.models.tokens_v2;

using VpType = int;

public class GenericContinuator<T>
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

    public Func<T, T, double>? Distance;
    
    public int KMax;
    private List<Dictionary<VpType[], List<VpType>>> _prefixesToContinuations;

    private Random Rng = new();
    
    public GenericContinuator(Func<T, VpType> map, Func<T, T, double>? distance = null, int kMax = 5)
    {
        // Initialize map and mapping
        Map = map;
        Mapping = [];
    
        // Initialize distance
        Distance = distance;
        
        // Initialize KMax
        KMax = kMax;

        // Initialize prefixes to continuations
        _prefixesToContinuations = [];
        for (var k = 0; k < KMax; k++)
            _prefixesToContinuations.Add(new(new VpArrEqualityComparer()));
    }

    private VpType GetRandomVp(VpType? previousVp = null)
    {
        // Return random element
        if (previousVp is not { } previousVpNotNull || Distance is not { } distanceNotNull)
            return Mapping.Keys.RandomElement();
        
        // Get item from vp
        var previousItem = Mapping[previousVpNotNull];

        // Get random item, weighted by distance
        var randomItem = Mapping
            .Select(it => it.Value)
            .ToWeightedList(it => distanceNotNull(it, previousItem))
            .RandomElement().Value;

        // Get vp from item
        return Map(randomItem);
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
            var prefixesToContK = _prefixesToContinuations[k];
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
            
            if (currentChunkSize == maxChunkSize)
            {
                currentSeq.Add(delimVp);
                cont = delimVp;
                currentChunkSize = 0;
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

            var continuationsDict = _prefixesToContinuations[k - 1];
            var viewpointCtx = currentSeq.TakeLast(k).ToArray();
            
            if (!continuationsDict.TryGetValue(viewpointCtx, out var allContVps)) continue;

            // considers the number of different viewpoints, not the number of continuations as they are repeated
            if (k > 1)
            {
                var first = allContVps[0];
                var allTheSame = allContVps.All(it => it == first);
            
                if (allTheSame)
                {
                    // proba to skip is proportional to order
                    if (Rng.NextDouble() > 1.0 / (k + 1))
                    {
                        vpToSkip = allContVps[0];
                        continue;
                    }
                    
                    vpToSkip = null;
                }
                
                if (vpToSkip is { } vpToSkipNotNull )
                    return allContVps.Where(c => c != vpToSkipNotNull).RandomElement();
            }
            
            return allContVps.RandomElement();
        }

        return -1;
    }
}