using System;
using System.Collections.Generic;

namespace thesis.midi.core;

public class LeadSheet
{
    public enum SoloType { Learner, Algorithm }
    
    public List<List<Chord>> Chords;
    public List<SoloType> SoloDivision;
    
    public Chord ChordAtTime(double time)
    {
        if (time > Chords.Count)
            return null;
        
        var measure = (int)Math.Truncate(time);
        var measureTime = time - measure;
        var measureChords = Chords[measure];

        if (measureChords is [])
            return null;
        
        return measureChords[(int)Math.Truncate(measureTime * measureChords.Count)];
    }
}