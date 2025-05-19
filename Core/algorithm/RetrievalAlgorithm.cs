using Core.midi;

namespace Core.algorithm;

public class RetrievalAlgorithm : IAlgorithm
{
    private MidiSong _solo;
    
    public void Initialize(MidiSong[] solos, LeadSheet leadSheet)
    {
        _solo = solos[0];
        
        // Change output name for all notes
        foreach (var measure in _solo.Measures)
            for (var index = 0; index < measure.Notes.Count; index++)
                measure.Notes[index] = measure.Notes[index] with { OutputName = OutputName.Algorithm };
    }

    public void Learn(List<MidiMeasure> measures, int startMeasureNum = 0) { }

    public List<MidiMeasure> Generate(int generateMeasureCount = 4, int startMeasureNum = 0)
    {
        // Schedule measures from solo
        return _solo.Measures.GetRange(startMeasureNum % _solo.Measures.Count, generateMeasureCount);
    }
}