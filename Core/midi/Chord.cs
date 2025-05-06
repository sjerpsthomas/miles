namespace Core.midi;

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
    
    public static Chord CMajor = new Chord(KeyEnum.C, TypeEnum.Major);

    public KeyEnum Key;
    public TypeEnum Type;

    public Chord(KeyEnum key, TypeEnum type)
    {
        Key = key;
        Type = type;
    }
}