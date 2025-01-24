using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;


namespace thesis.midi.core;

public class LeadSheet
{
    public enum SoloType { Learner, Algorithm }

    public enum StyleEnum { Straight, Swing }
    
    public StyleEnum Style;
    
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

    private static JsonSerializerOptions _jsonOptions = new() { IncludeFields = true, WriteIndented = true };
    public string Serialize() => JsonSerializer.Serialize(this, _jsonOptions);

    public static LeadSheet FromFile(string path)
    {
        var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        var text = file.GetAsText();
        file.Close();
        
        return JsonSerializer.Deserialize<LeadSheet>(text, _jsonOptions);
    }
}