using System.Text.Json;

namespace Core.midi;

public class LeadSheet
{
    public enum SoloType { Learner, Algorithm }

    public enum StyleEnum { Straight, Swing }

    public int BPM;

    public Chord Key;
    
    public StyleEnum Style;
    
    public List<List<Chord>> Chords;
    
    public List<string> SectionLabels;
    
    public Chord ChordAtTime(double time)
    {
        time %= Chords.Count;
        
        var measure = (int)Math.Truncate(time);
        var measureTime = time - measure;
        var measureChords = Chords[measure];

        if (measureChords is [])
            throw new ArgumentException("No chords found!");
        
        return measureChords[(int)Math.Truncate(measureTime * measureChords.Count)];
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