using Core.midi;
using static Core.midi.Chord.TypeEnum;

namespace Core.tokens.v2;

public static class V2_ChordMethods
{
    public static int Substitute(Chord.TypeEnum type, int note)
    {
        switch (type)
        {
            case Major:
                if (note == 3) return 4;
                if (note == 10) return 11;
                break;
            case Dominant:
                if (note == 3) return 4;
                if (note == 1) return 10;
                break;
            case Minor:
                if (note == 4) return 3;
                break;
            case HalfDim7:
                if (note == 4) return 3;
                if (note == 7) return 6;
                if (note == 11) return 10;
                break;
        }

        return note;
    }
    
    public const int OctaveSize = 12;
    
    public static int GetOctaveScaleNote(LeadSheet? leadSheet, double time, int absoluteNote)
    {
        return absoluteNote;
    }

    public static int GetAbsoluteNote(LeadSheet leadSheet, double time, int octaveScaleNote)
    {
        // Get current chord
        var currentChord = leadSheet.ChordAtTime(time);
        var (nextChord, nextChordTime) = leadSheet.NextChordAtTime(time);

        if (nextChordTime - time < 0.1)
            currentChord = nextChord;
        
        // Transpose by key and octave
        octaveScaleNote -= (int)currentChord.Key;
        var octave = octaveScaleNote / 12;
        octaveScaleNote -= octave * 12;

        // Make potential substitutions
        octaveScaleNote = Substitute(currentChord.Type, octaveScaleNote);

        // Transpose back
        octaveScaleNote += octave * 12;
        octaveScaleNote += (int)currentChord.Key;

        // Return
        return octaveScaleNote;
    }
}
