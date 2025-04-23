
namespace Core.models.continuator.belief_propag;

public abstract class Node(string name)
{
    public string Name = name;
    public List<Node> Neighbors = [];

    public abstract bool IsValidNeighbor(Node neighbor);

    public void AddNeighbor(dynamic neighbor) => Neighbors.Add(neighbor);
}

public class Variable(string name) : Node(name)
{
    public override bool IsValidNeighbor(Node neighbor) => neighbor is Factor;
}

public class Factor(string name) : Node(name)
{
    public LabeledArray? Data = null;

    public override bool IsValidNeighbor(Node neighbor) => neighbor is Variable;
}