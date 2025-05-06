using Core.tokens.v1;

namespace Core.models.tokens_v1;

public class V1_TokenFactorOracle
{
    public class Node
    {
        // Key: token, value: index of node
        private readonly Dictionary<V1_Token, int> _transitions = new();

        public int Supply;
        
        public int this[V1_Token index]
        {
            get => _transitions[index];
            set => _transitions[index] = value;
        }
        
        public bool Has(V1_Token token) => _transitions.ContainsKey(token);

        public (V1_Token, int) Traverse(int currentIndex, Random rng)
        {
            if (_transitions.Count == 0)
                return (0, -1);
                
            // Get random token from transitions, return it with next index
            var key = _transitions.Keys.ElementAt(rng.Next(_transitions.Keys.Count));
            return (key, _transitions[key]);
        }
    }

    public List<Node> Nodes = [];
        
    public void AddToken(V1_Token token)
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

    public void AddTokens(List<V1_Token> tokens)
    {
        foreach (var token in tokens)
            AddToken(token);
    }
}