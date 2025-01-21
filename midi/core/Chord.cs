using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;

namespace thesis.midi.core;

public class Chord
{
    public enum KeyEnum { C = 0, Db, D, Eb, E, F, Gb, G, Ab, A, Bb, B }
    
    public enum TypeEnum
    {
        Major = 0,
        Dominant,
        Minor,
        HalfDim7,
    }

    public KeyEnum Key;
    public TypeEnum Type;

    public Chord(KeyEnum key, TypeEnum type)
    {
        Key = key;
        Type = type;
    }

    public static Chord Deserialize(string input)
    {
        TypeEnum type;
        int count;
        
        if (input.EndsWith("m7b5"))
            (type, count) = (TypeEnum.HalfDim7, 4);
        else if (input.EndsWith("m7"))
            (type, count) = (TypeEnum.Minor, 2);
        else if (input.EndsWith("M7"))
            (type, count) = (TypeEnum.Major, 2);
        else if (input.EndsWith("7"))
            (type, count) = (TypeEnum.Dominant, 1);
        else throw new ArgumentException($"Invalid chord when deserializing ${input}");

        var keyInput = input[..^count];

        if (!Enum.TryParse<KeyEnum>(keyInput, out var key))
            throw new ArgumentException($"Invalid chord when deserializing ${input}");

        return new Chord(key, type);
    }

    public string Serialize()
    {
        return $"{Key}{Type switch
        {
            TypeEnum.Major => "M7",
            TypeEnum.Dominant => "7",
            TypeEnum.Minor => "m7",
            TypeEnum.HalfDim7 => "m7b5",
            _ => ""
        }}";
    }
    
    public static Dictionary<TypeEnum, List<int>> AbsoluteToRelative = new()
    {
        [TypeEnum.Major] =    [0, 0, 1, 1, 2, 3, 3, 4, 4, 5, 5, 6], // C D  E  F G  A  B
        [TypeEnum.Dominant] = [0, 0, 1, 1, 2, 3, 3, 4, 4, 5, 6, 6], // C D  E  F G  A  Bb
        [TypeEnum.Minor] =    [0, 0, 1, 2, 2, 3, 3, 4, 5, 5, 6, 6], // C D  Eb F G  Ab Bb
        [TypeEnum.HalfDim7] = [0, 1, 1, 2, 2, 3, 4, 4, 5, 5, 6, 6], // C Db Eb F Gb Ab Bb
    };

    public static Dictionary<TypeEnum, List<int>> RelativeToAbsolute = new()
    {
        [TypeEnum.Major] =    [0, 2, 4, 5, 7, 9, 11], // C D  E  F G  A  B
        [TypeEnum.Dominant] = [0, 2, 4, 5, 7, 9, 10], // C D  E  F G  A  Bb
        [TypeEnum.Minor] =    [0, 2, 3, 5, 7, 8, 10], // C D  Eb F G  Ab Bb
        [TypeEnum.HalfDim7] = [0, 1, 3, 5, 6, 8, 10], // C Db Eb F Gb Ab Bb
    };
    
    public int GetRelativeNote(int absoluteNote)
    {
        // Transpose to C
        absoluteNote -= (int)Key;
            
        // Limit to octave
        var octave = absoluteNote / 12;
        absoluteNote -= 12 * octave;

        // Get relative note
        var relativeNote = AbsoluteToRelative[Type][absoluteNote];

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
        var absoluteNote = RelativeToAbsolute[Type][relativeNote];

        // Add back octave
        absoluteNote += 12 * octave;
            
        // Transpose to key
        absoluteNote += (int)Key;

        return absoluteNote;
    }
}