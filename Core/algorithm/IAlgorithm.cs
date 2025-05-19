using Core.midi;

namespace Core.algorithm;

public interface IAlgorithm
{
    public void Initialize(MidiSong[] solos, LeadSheet leadSheet);

    public void Learn(List<MidiMeasure> measures, int startMeasureNum = 0);

    public List<MidiMeasure> Generate(int generateMeasureCount = 4, int startMeasureNum = 0);
}