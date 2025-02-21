using System.Xml.XPath;
using Core.midi;
using Core.midi.token;
using Core.midi.token.conversion;
using Microsoft.Data.Sqlite;

namespace Console.routine;

public class WJDTokenizer
{
    public SqliteConnection Connection;
    
    const int NumSolos = 456;
    
    private record WJDNote(
        int Bar,
        int Beat,
        int Division,
        int Tatum,
        int Pitch,
        float Duration,
        float LoudCent,
        int Num,
        int Denom
    );
    
    IEnumerable<WJDNote> GetNotes(int melId)
    {
        var command = Connection.CreateCommand();
        command.CommandText =
            $"select bar, beat, division, tatum, pitch, duration, loud_cent, num, denom from melody where melid = {melId}";
        
        using var reader = command.ExecuteReader();
            
        while (reader.Read())
        {
            WJDNote? res = null;
            try
            {
                res = new WJDNote(
                    reader.GetInt32(0),
                    reader.GetInt32(1),
                    reader.GetInt32(2),
                    reader.GetInt32(3),
                    (int)reader.GetFloat(4),
                    reader.GetFloat(5),
                    reader.GetFloat(6),
                    reader.GetInt32(7),
                    reader.GetInt32(8)
                );
            }
            catch (Exception)
            {
                System.Console.WriteLine("Skipped note");
            }

            if (res != null)
                yield return res;
        }
    }
    
    void HandleMelody(int melId, int transpose, string exportFolderName)
    {
        // Print
        System.Console.WriteLine($"Handling melody {melId}");
        
        // Get average tempo and time signature
        float avgTempo;
        string signature;
        {
            // Get average tempo
            var soloInfoCommand = Connection.CreateCommand();
            soloInfoCommand.CommandText = $"select avgtempo, signature from solo_info where melid = {melId}";
            using var reader = soloInfoCommand.ExecuteReader();
            reader.Read();
            avgTempo = reader.GetFloat(0);
            signature = reader.GetString(1);
        }

        // Only consider melodies in 4/4
        if (signature != "4/4")
        {
            System.Console.WriteLine($"Discarded mel_id {melId}: not in 4/4");
            return;
        }

        // Keep track of tokens
        List<MidiNote> midiNotes = [];
        
        // Get notes
        var notes = GetNotes(melId);
        
        foreach (var note in notes)
        {
            // Ignore pickup measures
            if (note.Bar < 1) continue;

            // Get time
            var resTime = note.Bar + (note.Beat - 1 + (note.Tatum - 1) / note.Division) / note.Num;
            
            // Get length
            var resLength = note.Duration * (avgTempo / 60);
            
            // Get velocity
            var resVelocity = (int)(note.LoudCent * 127);
            
            // Add note
            midiNotes.Add(new MidiNote(OutputName.Unknown, resTime, resLength, note.Pitch, resVelocity));
        }
        
        // Convert velocity token melody to tokens, get string
        var tokens = TokenMethods.Tokenize(midiNotes);
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
        Connection = new("Filename=wjazzd.db");
        Connection.Open();
        
        for (var melId = 1; melId <= NumSolos; melId++)
            HandleMelody(melId, 0, "C:\\Users\\thoma\\Desktop\\wjd_tokens");
        
        Connection.Dispose();
    }
}