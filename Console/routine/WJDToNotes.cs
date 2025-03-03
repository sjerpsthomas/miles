using Core.midi;
using Microsoft.Data.Sqlite;

namespace Console.routine;

public class WJDToNotes
{
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
    
    private IEnumerable<WJDNote> GetWJDNotes(SqliteConnection connection, int melId)
    {
        var command = connection.CreateCommand();
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
    
    public IEnumerable<MidiNote> GetNotes(SqliteConnection connection, int melId)
    {
        // Print
        System.Console.WriteLine($"Handling melody {melId}");
        
        // Get average tempo and time signature
        float avgTempo;
        string signature;
        {
            // Get average tempo
            var soloInfoCommand = connection.CreateCommand();
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
            return [];
        }
        
        // Get notes
        var notes = GetWJDNotes(connection, melId);
        
        // Keep track of tokens
        return
            from note in notes
            where note.Bar >= 1
            let resTime = note.Bar + ((note.Tatum - 1) / note.Division + note.Beat - 1) / note.Num
            let resLength = note.Duration * (avgTempo / 60)
            let resVelocity = (int)(note.LoudCent * 127)
            select new MidiNote(OutputName.Unknown, resTime, resLength, note.Pitch, resVelocity);
    }
    

    public void Run(int melId, string path)
    {
        using var connection = new SqliteConnection("Filename=wjazzd.db");
        connection.Open();

        // Get notes from database
        var notes = GetNotes(connection, melId);

        // Get song from notes
        var song = MidiSong.FromNotes(notes.ToList());
        
        // Save to file
        song.ToNotesFileStream(new FileStream($@"{path}\{melId}.notes", FileMode.CreateNew));
    }

    public void RunAllStandards(string path)
    {
        // (Order: solo.notes, _extra_1.notes, ..., _extra_4.notes)
        List<int> melodies = [
            // Summertime (Sidney Bechet)
            377,  373, 374, 375, 376,
            
            // Long Ago and Far Away (Benny Carter)
            11,  7, 8, 10, 13,
            
            // My Little Suede Shoes (Charlie Parker)
            60,  52, 53, 54, 55,
            
            // Ornithology (Charlie Parker)
            61,  56, 57, 58, 59
        ];

        // Run on all melodies
        foreach (var melody in melodies)
            new WJDToNotes().Run(melody, path);
    }
}