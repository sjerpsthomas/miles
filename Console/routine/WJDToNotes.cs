using Core.conversion;
using Core.midi;
using Microsoft.Data.Sqlite;

namespace Console.routine;

public class WJDToNotes
{
    public void Run(int melId, string path)
    {
        using var connection = new SqliteConnection("Filename=wjazzd.db");
        connection.Open();

        // Get notes from database
        var notes = Conversion.WeimarToNotes(connection, melId);

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