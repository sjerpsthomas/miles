using Core.midi;

namespace Console.routine;

public class MidiToNotes
{
    public void Run(string midiFilePath)
    {
        // Open song
        var song = MidiSong.FromMidiFileStream(new FileStream(midiFilePath, FileMode.Open));
        
        // Save to new file path
        var notesFilePath = Path.ChangeExtension(midiFilePath, ".notes");
        song.ToNotesFileStream(new FileStream(notesFilePath, FileMode.CreateNew));
    }
}