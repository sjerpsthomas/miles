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
    
    public static Chord CMajor = new Chord(KeyEnum.C, TypeEnum.Major);

    public KeyEnum Key;
    public TypeEnum Type;

    public Chord(KeyEnum key, TypeEnum type)
    {
        Key = key;
        Type = type;
    }
}