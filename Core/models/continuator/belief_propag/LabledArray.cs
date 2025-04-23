namespace Core.models.continuator.belief_propag;

public abstract record LabeledArray(List<string> AxesLabels)
{
    public abstract List<int> Shape { get; }
};
public record LabeledArray1D<T>(T[] Array, List<string> AxesLabels) : LabeledArray(AxesLabels)
{
    public override List<int> Shape => [Array.Length];
}

public record LabeledArray2D<T>(T[,] Array, List<string> AxesLabels) : LabeledArray(AxesLabels)
{
    public override List<int> Shape => [Array.GetLength(0), Array.GetLength(1)];
}
