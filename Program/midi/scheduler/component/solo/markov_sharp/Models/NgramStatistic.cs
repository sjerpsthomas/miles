namespace Program.midi.scheduler.component.solo.markov_sharp.Models;

public class NgramStatistic<TNgram>
{
    public TNgram Value { get; set; }
    public double Count { get; set; }
    public double Probability { get; set; }
}