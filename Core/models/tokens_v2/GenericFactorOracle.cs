namespace Core.models.tokens_v2;


public class GenericFactorOracle<T>(IEqualityComparer<T> equalityComparer)
{
    public class Node(IEqualityComparer<T> equalityComparer)
    {
        // Transition logic
        private readonly Dictionary<T, int> _transitions = new(equalityComparer);
        public int GetTransition(T value) => _transitions[value];
        public void SetTransition(T value, int index) => _transitions[value] = index;
        public bool HasValue(T value) => _transitions.ContainsKey(value);
        
        public int Supply;

        public (T, int) Traverse(int currentIndex, Random rng)
        {
            if (_transitions.Count == 0)
                return (default, -1)!;
            
            // Return random key-value pair from transitions
            var (value, index) = _transitions.ElementAt(rng.Next(_transitions.Count));
            return (value, index);
        }
    }

    public List<Node> Nodes = [];

    public void AddValue(T value)
    {
        // Create a new node
        var newNode = new Node(equalityComparer);
        
        // Add the node if it is the first
        if (Nodes is not [.., var lastNode])
        {
            newNode.Supply = -1;
            Nodes.Add(newNode);
            return;
        }
        
        // Create a new transition form m to m + 1,
        //   with the value as the value
        lastNode.SetTransition(value, Nodes.Count);

        var k = lastNode.Supply;
        while (k > -1 && !Nodes[k].HasValue(value))
        {
            // Create a new transition from k to m + 1
            Nodes[k].SetTransition(value, Nodes.Count);

            k = Nodes[k].Supply;
        }

        newNode.Supply = k == -1 ? 0 : Nodes[k].GetTransition(value);
        Nodes.Add(newNode);
    }
    
    public void AddValues(IEnumerable<T> values)
    {
        foreach (var value in values) AddValue(value);
    }
}