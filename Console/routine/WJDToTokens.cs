using Core.conversion;
using Core.midi;
using Core.tokens.v1;
using Microsoft.Data.Sqlite;
using static Core.midi.LeadSheet;

namespace Console.routine;

public class WJDToTokens
{
    const int NumSolos = 456;

    public void Run(int melId, string exportFolderName)
    {
        using var connection = new SqliteConnection("Filename=wjazzd.db");
        connection.Open();
        
        // Get notes
        var midiNotes = Conversion.WeimarToNotes(connection, melId).ToList();
        
        // Get key and rhythm feel
        string keyStr;
        string rhythmFeel;
        {
            // Get average tempo
            var soloInfoCommand = connection.CreateCommand();
            soloInfoCommand.CommandText = $"select key, rhythmfeel from solo_info where melid = {melId}";
            using var reader = soloInfoCommand.ExecuteReader();
            reader.Read();
            keyStr = reader.GetString(0);
            rhythmFeel = reader.GetString(1);
        }
        
        // Parse key
        keyStr = keyStr
            .Replace("-maj", "M7")
            .Replace("-min", "m7");
        Chord key;
        try
        {
            key = Chord.Deserialize(keyStr);
        }
        catch (Exception e)
        {
            System.Console.WriteLine($"Skipped key: {e.Message}");
            key = Chord.CMajor;
        }
        
        // Parse rhythm feel
        var style = rhythmFeel.Contains("SWING") ? StyleEnum.Swing : StyleEnum.Straight;

        // Create lead sheet
        var leadSheet = new LeadSheet()
        {
            Chords = Enumerable.Range(0, midiNotes.Count).Select(x => new List<Chord>() { key }).ToList(),
            Style = style
        };
        
        // Convert velocity token melody to tokens, get string
        var tokens = V1_TokenMethods.V1_Tokenize(midiNotes, leadSheet);
        var tokensStr = V1_TokenMethods.V1_TokensToString(tokens);
                
        // Trim measure tokens
        tokensStr = tokensStr.Trim('M');
        tokensStr += 'M';
                
        // Get export file name, write to disk
        var exportFileName = Path.Join(exportFolderName, $"{melId % 10000}.tokens");
        File.WriteAllText(exportFileName, tokensStr);
    }
    
    public void RunAllStandards(string path)
    {
        // Run on all melodies
        foreach (var melody in WJDToNotes.Melodies)
            new WJDToTokens().Run(melody, path);
    }
}