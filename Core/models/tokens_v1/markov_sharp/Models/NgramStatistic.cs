namespace Core.models.tokens_v1.markov_sharp.Models;

public class NgramStatistic<TNgram>
{
    public TNgram Value { get; set; }
    public double Count { get; set; }
    public double Probability { get; set; }
}