using Core.midi;

namespace Core.algorithm;

public interface IAlgorithm
{
    public void Initialize(MidiSong[] solos, LeadSheet leadSheet);

    public void IngestMeasures(List<MidiMeasure> measures, int startMeasureNum);

    public List<MidiMeasure> Generate(int generateMeasureCount, int startMeasureNum);
}