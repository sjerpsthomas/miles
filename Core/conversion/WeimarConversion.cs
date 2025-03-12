using Core.midi;
using Microsoft.Data.Sqlite;

namespace Core.conversion;

public partial class Conversion
{
    private record WeimarNote(
        int Bar,
        int Beat,
        int Division,
        int Tatum,
        int Pitch,
        float Duration,
        float LoudCent,
        int Num
    );
    
    private static IEnumerable<WeimarNote> GetWeimarNotes(SqliteConnection connection, int melId)
    {
        var command = connection.CreateCommand();
        command.CommandText =
            $"select bar, beat, division, tatum, pitch, duration, loud_cent, num from melody where melid = {melId}";
        
        using var reader = command.ExecuteReader();
            
        while (reader.Read())
        {
            WeimarNote? res = null;
            try
            {
                res = new WeimarNote(
                    reader.GetInt32(0),
                    reader.GetInt32(1),
                    reader.GetInt32(2),
                    reader.GetInt32(3),
                    (int)reader.GetFloat(4),
                    reader.GetFloat(5),
                    reader.IsDBNull(6) ? 96 : reader.GetFloat(6),
                    reader.GetInt32(7)
                );
            }
            catch (Exception)
            {
                Console.WriteLine("Skipped note");
            }

            if (res != null)
                yield return res;
        }
    }
    
    public static IEnumerable<MidiNote> WeimarToNotes(SqliteConnection connection, int melId)
    {
        // Print
        Console.WriteLine($"Handling melody {melId}");
        
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
            Console.WriteLine($"Discarded mel_id {melId}: not in 4/4");
            return [];
        }
        
        // Get notes
        var notes = GetWeimarNotes(connection, melId);
        
        // Keep track of tokens
        return
            from note in notes
            where note.Bar >= 1
            let resTime = note.Bar + ((note.Tatum - 1) / (double)note.Division + note.Beat - 1) / note.Num
            let resLength = note.Duration * 0.25 * (avgTempo / 60)
            let resVelocity = (int)(note.LoudCent * 127)
            select new MidiNote(OutputName.Unknown, resTime, resLength, note.Pitch, resVelocity);
    }
}