using Core.conversion;
using Core.midi.token;
using Microsoft.Data.Sqlite;

namespace Console.routine;

public class WJDToTokens
{
    const int NumSolos = 456;
    
    void HandleMelody(SqliteConnection connection, int melId, string exportFolderName)
    {
        // Get notes
        var midiNotes = Conversion.WeimarToNotes(connection, melId).ToList();
        
        // Convert velocity token melody to tokens, get string
        var tokens = Conversion.Tokenize(midiNotes);
        var tokensStr = TokenMethods.TokensToString(tokens);
                
        // Trim measure tokens
        tokensStr = tokensStr.Trim('M');
        tokensStr += 'M';
                
        // Get export file name, write to disk
        var exportFileName = Path.Join(exportFolderName, $"{melId % 10000}.tokens");
        File.WriteAllText(exportFileName, tokensStr);
    }
    
    public void Run()
    {
        using var connection = new SqliteConnection("Filename=wjazzd.db");
        connection.Open();
        
        for (var melId = 1; melId <= NumSolos; melId++)
            HandleMelody(connection, melId, @"C:\Users\thoma\Desktop\wjd_tokens");
    }
}