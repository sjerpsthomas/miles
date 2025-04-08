using System;
using System.Collections.Generic;
using System.Linq;

namespace Program.midi.scheduler.component.solo.v2;

public class MarkovChain<T>(IEqualityComparer<T> equalityComparer)
{
    private class Node
    {
        public List<T> Neighbors = [];

        public T GetRandomNeighbor(Random rng)
        {
            if (Neighbors is [])
                return default;

            return Neighbors[rng.Next(Neighbors.Count)];
        }
    }

    private Dictionary<T, Node> _nodes2 = new(equalityComparer);
    
    private Random _rng = new ();
    
    private bool _hasLastValue = false;
    private T _lastValue;

    private Node Train(T value)
    {
        if (_nodes2.TryGetValue(value, out var node)) return node;
        
        node = new Node();
        _nodes2[value] = node;

        return node;
    }
    
    private void Train(T value, T nextValue)
    {
        var node = Train(value);
        
        if (nextValue != null)
            node.Neighbors.Add(nextValue);
    }

    public void Train(List<T> values)
    {
        if (values is []) return;
        
        // Train on last value if possible
        if (_hasLastValue)
            Train(_lastValue, values[0]);
        
        // Train on all-but-last value
        for (var index = 0; index < values.Count - 1; index++)
        {
            var value = values[index];
            var nextValue = values[index + 1];
            
            Train(value, nextValue);
        }
        
        // Train on last value
        Train(values[^1]);
        _lastValue = values[^1];
        _hasLastValue = true;
    }

    public T StartingValue() => _nodes2.Keys.ElementAt(_rng.Next(_nodes2.Count));
    public T Traverse(T value) => _nodes2[value].GetRandomNeighbor(_rng);
}
