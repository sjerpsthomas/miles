namespace Core.models.continuator.belief_propag;

public abstract record LabeledArray(List<string> AxesLabels);
public record LabeledArray1D<T>(T[] Array, List<string> AxesLabels) : LabeledArray(AxesLabels);
public record LabeledArray2D<T>(T[,] Array, List<string> AxesLabels) : LabeledArray(AxesLabels);
