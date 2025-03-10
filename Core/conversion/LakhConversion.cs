using System.Diagnostics;
using Core.midi;
using Core.midi.token;
using NAudio.Midi;

namespace Core.conversion;

public partial class Conversion
{
    public static IEnumerable<List<MidiNote>> LakhToMidiNotes(string fileName, bool printAllow = false)
    {
        MidiFile midiFile;
        try
        {
            midiFile = new MidiFile(fileName, true);
        }
        catch
        {
            if (printAllow)
                Console.WriteLine($"Skipped importing {Path.GetFileName(fileName)}: cannot open MIDI file");
            yield break;
        }

        // Get delta ticks per quarter note
        var ticksPerQuarterNote = midiFile.DeltaTicksPerQuarterNote;

        // Go over all tracks
        for (var i = 0; i < midiFile.Tracks; i++)
        {
            // Validate and tokenize track, yield return
            if (ValidateTrack(midiFile.Events[i], out var notes, ticksPerQuarterNote, printAllow))
                yield return notes;
        }
    }
    
    public static IEnumerable<List<Token>> LakhToTokens(string fileName, bool printAllow = false)
    {
        foreach (var track in LakhToMidiNotes(fileName, printAllow))
            yield return Tokenize(track);
    }

    private static bool ValidateTrack(IList<MidiEvent> events, out List<MidiNote> notes, int ticksPerQuarterNote, bool printAllow)
    {
        var currentPatch = -1;
        
        HashSet<int> uniqueNotes = [];
        notes = new(events.Count);

        foreach (var midiEvent in events)
        {
            // Change patch if patch change event
            if (midiEvent is PatchChangeEvent patchChangeEvent)
            {
                currentPatch = patchChangeEvent.Patch;
                continue;
            }

            // Check if current patch is defined
            if (currentPatch == -1)
            {
                if (printAllow)
                    Console.WriteLine("Event ignored: currentPatch == -1");
                continue;
            }

            // Check if current patch is melodic instrument
            //   (patch from 'Agogo' on is percussive to me)
            if (currentPatch >= 114)
            {
                if (printAllow)
                    Console.WriteLine("Event ignored: non-melodic instrument");
                continue;
            }

            // Check if event is note on event
            if (midiEvent is not NoteOnEvent noteOnEvent) continue;

            // Check if current channel is rhythm
            if (noteOnEvent.Channel == 10)
            {
                if (printAllow)
                    Console.WriteLine("Event ignored: rhythm channel");
                continue;
            }

            // Get note attributes
            int note;
            double time;
            double length;
            int velocity;

            try
            {
                note = noteOnEvent.NoteNumber;
                time = (double)noteOnEvent.AbsoluteTime / (ticksPerQuarterNote * 4);
                length = (double)noteOnEvent.NoteLength / (ticksPerQuarterNote * 4);
                velocity = noteOnEvent.Velocity;
            }
            catch
            {
                if (printAllow)
                    Console.WriteLine("Event ignored: unable to get attributes");
                continue;
            }

            // Add to notes
            notes.Add(new MidiNote(OutputName.Unknown, time, length, note, velocity));
            
            // Handle unique notes
            uniqueNotes.Add(note);
        }
        
        // Check number of unique notes
        if (uniqueNotes.Count < 10)
        {
            if (printAllow)
                Console.WriteLine("Track ignored: too few unique notes");
            return false;
        }

        var overlap = 0.0;
        var rest = 0.0;
        
        // Handle note overlaps and rests
        for (var index = 0; index < notes.Count - 1; index++)
        {
            var (_, time, length, _, _) = notes[index];
            var nextTime = notes[index + 1].Time;
            
            Debug.Assert(time <= nextTime, "Notes are out of order!");
            
            // Get distance
            var distance = nextTime - (time + length);

            if (distance > 0)
                rest += distance;
            else
                overlap += -distance;
        }

        // Get averages
        overlap /= notes.Count - 1;
        rest /= notes.Count - 1;

        // Check average rest
        if (rest > 0.5)
        {
            if (printAllow)
                Console.WriteLine("Track ignored: too large average rest");
            return false;
        }
        
        // Check average overlap
        if (overlap > 0.05)
        {
            if (printAllow)
                Console.WriteLine("Track ignored: too large average overlap");
            return false;
        }

        return true;
    }
}