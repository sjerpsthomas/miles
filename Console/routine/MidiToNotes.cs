using Core.midi;

namespace Console.routine;

public class MidiToNotes
{
    public void Run(string midiFilePath)
    {
        // Open song
        var fileStream = new FileStream(midiFilePath, FileMode.Open);
        var song = MidiSong.FromMidiFileStream(fileStream, GetOutputName);
        
        // Save to new file path
        var notesFilePath = Path.ChangeExtension(midiFilePath, ".notes");
        song.ToNotesFileStream(new FileStream(notesFilePath, FileMode.CreateNew));


        System.Console.WriteLine("What BPM would you like to give the song?");
        string read;
        do
        {
            read = System.Console.ReadLine()!;
        } while (!int.TryParse(read, out song.Bpm));
        
        
        // System.Console.WriteLine("What song ID would you like to give the song?");
        // do
        // {
        //     read = System.Console.ReadLine()!;
        // } while (!int.TryParse(read, out song.));
        
        System.Console.WriteLine($"MIDI track saved to {notesFilePath}!");
    }

    private OutputName GetOutputName(int i)
    {
        System.Console.Clear();
        System.Console.WriteLine($"Which OutputName would you like to give to track {i}?");
        System.Console.WriteLine($"({string.Join(", ", Enum.GetValues<OutputName>().Select(it => it.ToString()))})");
        
        OutputName res;
        string read;
        do
        {
            read = System.Console.ReadLine()!;
        } while (!Enum.TryParse(read, true, out res));
        
        return res;
    }
}