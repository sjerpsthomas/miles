using System;
using System.Collections.Generic;
using System.Linq;
using Core.midi;
using Core.midi.token;

namespace Program.midi.scheduler.component.solo;

public class TokenFactorOracle
{
    public class Node
    {
        // Key: token, value: index of node
        private readonly Dictionary<Token, int> _transitions = new();

        public int Supply;
        
        public int this[Token index]
        {
            get => _transitions[index];
            set => _transitions[index] = value;
        }
        
        public bool Has(Token token) => _transitions.ContainsKey(token);

        public (Token, int) Traverse(int currentIndex, Random rng)
        {
            if (_transitions.Count == 0)
                return (0, -1);

            // Increase odds of continuing
            // if (rng.NextDouble() < 0.5)
            // {
            //     var (newKey, value) = _transitions.FirstOrDefault(tuple => tuple.Value == currentIndex + 1,
            //         new KeyValuePair<MidiMelody.MelodyNote, int>(null, -1));
            //     if (value != -1)
            //         return (newKey, value);
            // }
                
            // Get random token from transitions, return it with next index
            var key = _transitions.Keys.ElementAt(rng.Next(_transitions.Keys.Count));
            return (key, _transitions[key]);
        }
    }

    public List<Node> Nodes = [];
        
    public void AddToken(Token token)
    {
        // Create a new state
        var newNode = new Node();
            
        // Add the node if it is the first
        if (Nodes is not [.., var lastNode])
        {
            newNode.Supply = -1;
            Nodes.Add(newNode);
            return;
        }

        // Create a new transition from m to m + 1 labeled by note
        lastNode[token] = Nodes.Count;

        var k = lastNode.Supply;
        while (k > -1 && !Nodes[k].Has(token))
        {
            // Create a new transition from k to m + 1 by sigma
            Nodes[k][token] = Nodes.Count;
                
            k = Nodes[k].Supply;
        }

        newNode.Supply = k == -1 ? 0 : Nodes[k][token];
        Nodes.Add(newNode);
    }

    public void AddTokens(List<Token> tokens)
    {
        foreach (var token in tokens)
            AddToken(token);
    }
}