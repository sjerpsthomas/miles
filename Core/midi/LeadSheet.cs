using System.Text.Json;

namespace Core.midi;

public class LeadSheet
{
    public enum StyleEnum { Straight, Swing }

    public int Bpm;

    public Chord Key;
    
    public StyleEnum Style;
    
    public List<List<Chord>> Chords;
    
    public List<string> SectionLabels;
    
    public Chord ChordAtTime(double time)
    {
        time %= Chords.Count;
        
        var measure = (int)Math.Truncate(time);
        var measureChords = Chords[measure];

        if (measureChords is [])
            throw new ArgumentException("No chords found!");
        
        var measureTime = time - measure;
        return measureChords[(int)Math.Truncate(measureTime * measureChords.Count)];
    }

    public (Chord, double) NextChordAtTime(double time)
    {
        var measure = (int)Math.Truncate(time);
        var measureChords = Chords[measure % Chords.Count];

        // Return next chord of measure
        var measureTime = time - measure;
        var measureChordIndex = (int)Math.Truncate(measureTime * measureChords.Count);
        if (measureChords.Count >= measureChordIndex + 2)
            return (measureChords[measureChordIndex + 1], measure + 1.0 * measureChordIndex / measureChords.Count);

        // Return first chord of next measure
        do
        {
            measure++;
            measureChords = Chords[measure % Chords.Count];
        } while (measureChords is []);

        return (measureChords[0], measure);
    }
    
    private static JsonSerializerOptions _jsonOptions = new() { IncludeFields = true, WriteIndented = true };
    public string Serialize() => JsonSerializer.Serialize(this, _jsonOptions);

    public static LeadSheet FromStream(Stream stream)
    {
        string text;
        using (var reader = new StreamReader(stream)) text = reader.ReadToEnd();
        
        return JsonSerializer.Deserialize<LeadSheet>(text, _jsonOptions)!;
    }
}