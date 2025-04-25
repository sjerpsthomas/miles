using NumSharp;

namespace Core.models.continuator.belief_propag;

public record LabeledArray
{
    public LabeledArray(NDArray Array, List<string> AxesLabels)
    {
        this.Array = Array;
        this.AxesLabels = AxesLabels;
    }

    public NDArray Array { get; init; }
    public List<string> AxesLabels { get; init; }

    public void Deconstruct(out NDArray Array, out List<string> AxesLabels)
    {
        Array = this.Array;
        AxesLabels = this.AxesLabels;
    }
}
