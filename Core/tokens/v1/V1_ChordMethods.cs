using Core.midi;
using static Core.midi.Chord;

namespace Core.tokens.v1;

public static class V1_ChordMethods
{
    public const int OctaveSize = 7;
    
    public static Dictionary<TypeEnum, List<int>> AbsoluteToRelative = new()
    {
        [TypeEnum.Major] =    [0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6], // C D  E  F G  A  B
        [TypeEnum.Dominant] = [0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 6, 6], // C D  E  F G  A  Bb
        [TypeEnum.Minor] =    [0, 1, 1, 2, 2, 3, 3, 4, 5, 5, 6, 6], // C D  Eb F G  Ab Bb
        [TypeEnum.HalfDim7] = [0, 1, 1, 2, 2, 3, 4, 4, 5, 5, 6, 6], // C Db Eb F Gb Ab Bb
    };

    public static Dictionary<TypeEnum, List<int>> RelativeToAbsolute = new()
    {
        [TypeEnum.Major] =    [0, 2, 4, 5, 7, 9, 11], // C D  E  F G  A  B
        [TypeEnum.Dominant] = [0, 2, 4, 5, 7, 9, 10], // C D  E  F G  A  Bb
        [TypeEnum.Minor] =    [0, 2, 3, 5, 7, 8, 10], // C D  Eb F G  Ab Bb
        [TypeEnum.HalfDim7] = [0, 1, 3, 5, 6, 8, 10], // C Db Eb F Gb Ab Bb
    };
    
    public static int V1_GetOctaveScaleNote(this Chord chord, int absoluteNote)
    {
        // Limit to octave
        var octave = absoluteNote / 12;
        absoluteNote -= 12 * octave;

        // Get relative note
        // (act as if note is in C major)
        var relativeNote = AbsoluteToRelative[TypeEnum.Major][absoluteNote];

        // Add back 'octave'
        relativeNote += OctaveSize * octave;
        return relativeNote;
    }

    // From Godot source
    public static int PosMod(int a, int b)
    {
        var c = a % b;
        if ((c < 0 && b > 0) || (c > 0 && b < 0))
        {
            c += b;
        }
        return c;
    }
    
    public static int V1_GetAbsoluteNote(this Chord chord, int octaveScaleNote)
    {
        // Limit to 'octave'
        var octave = octaveScaleNote / OctaveSize;
        octaveScaleNote -= OctaveSize * octave;
            
        // Get absolute note
        var relativeIndex = octaveScaleNote - AbsoluteToRelative[TypeEnum.Major][(int)chord.Key];
        var absoluteNote = PosMod(RelativeToAbsolute[chord.Type][PosMod(relativeIndex, 7)] + (int)chord.Key, 12);
        
        // Add back octave
        absoluteNote += 12 * octave;
            
        return absoluteNote;
    }
}
