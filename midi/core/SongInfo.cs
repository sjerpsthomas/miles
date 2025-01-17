using System;
using System.Collections.Generic;

namespace thesis.midi.core;

public record class SongInfo(List<SongInfo.MeasureInfo> Info)
{
    public enum Key { C = 0, Db, D, Eb, E, F, Gb, G, Ab, A, Bb, B }
    
    public enum ChordType
    {
        Major,
        Dominant,
        Minor,
        HalfDim7,
    }

    public static Dictionary<ChordType, List<int>> AbsoluteToRelative = new()
    {
        [ChordType.Major] =    [0, 0, 1, 1, 2, 3, 3, 4, 4, 5, 5, 6], // C D  E  F G  A  B
        [ChordType.Dominant] = [0, 0, 1, 1, 2, 3, 3, 4, 4, 5, 6, 6], // C D  E  F G  A  Bb
        [ChordType.Minor] =    [0, 0, 1, 2, 2, 3, 3, 4, 5, 5, 6, 6], // C D  Eb F G  Ab Bb
        [ChordType.HalfDim7] = [0, 1, 1, 2, 2, 3, 4, 4, 5, 5, 6, 6], // C Db Eb F Gb Ab Bb
    };

    public static Dictionary<ChordType, List<int>> RelativeToAbsolute = new()
    {
        [ChordType.Major] =    [0, 2, 4, 5, 7, 9, 11], // C D  E  F G  A  B
        [ChordType.Dominant] = [0, 2, 4, 5, 7, 9, 10], // C D  E  F G  A  Bb
        [ChordType.Minor] =    [0, 2, 3, 5, 7, 8, 10], // C D  Eb F G  Ab Bb
        [ChordType.HalfDim7] = [0, 1, 3, 5, 6, 8, 10], // C Db Eb F Gb Ab Bb
    };
    
    public enum SoloType { Learner, Algorithm }

    public record class MeasureInfo(Key Key, ChordType ChordType, SoloType SoloType)
    {
        public int GetRelativeNote(int absoluteNote)
        {
            // Transpose to C
            absoluteNote -= (int)Key;
            
            // Limit to octave
            var octave = absoluteNote / 12;
            absoluteNote -= 12 * octave;

            // Get relative note
            var relativeNote = AbsoluteToRelative[ChordType][absoluteNote];

            // Add back 'octave'
            relativeNote += 7 * octave;
            return relativeNote;
        }

        public int GetAbsoluteNote(int relativeNote)
        {
            // Limit to 'octave'
            var octave = relativeNote / 7;
            relativeNote -= 7 * octave;
            
            // Get absolute note
            var absoluteNote = RelativeToAbsolute[ChordType][relativeNote];

            // Add back octave
            absoluteNote += 12 * octave;
            
            // Transpose to key
            absoluteNote += (int)Key;

            return absoluteNote;
        }
    }
}