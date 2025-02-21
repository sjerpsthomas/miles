using System.Diagnostics;
using Core.midi;
using Core.midi.token;
using NAudio.Midi;
using static System.Console;

namespace Console.routine;

public class LakhTokenizer
{
    private class IdLock { public int Value; };

    private IdLock Lock = new() { Value = 0 };
    
    public void Run(string directory, string targetDirectory, bool printAllow = false)
    {
        var stopWatch = new Stopwatch();
        stopWatch.Start();
        
        var fileNames = Directory.GetFiles(directory, "*.mid", SearchOption.AllDirectories);

        Parallel.ForEach(fileNames, fileName =>
        {
            MidiFile midiFile;
            try
            {
                midiFile = new MidiFile(fileName, true);
            }
            catch
            {
                if (printAllow)
                    WriteLine($"Skipped importing {Path.GetFileName(fileName)}: cannot open MIDI file");
                return;
            }

            var deltaTicksPerQuarterNote = midiFile.DeltaTicksPerQuarterNote;

            var numberOfUsableTracks = 0;
            
            // Go over all tracks
            for (var i = 0; i < midiFile.Tracks; i++)
            {
                var currentPatch = -1;
                
                HashSet<int> uniqueNotes = [];
                List<MidiNote> notes = new(midiFile.Events[i].Count);
                
                foreach (var midiEvent in midiFile.Events[i])
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
                            WriteLine("Event ignored: currentPatch == -1");
                        continue;
                    }

                    // Check if current patch is melodic instrument
                    //   (patch from 'Agogo' on is percussive to me)
                    if (currentPatch >= 114)
                    {
                        if (printAllow)
                            WriteLine("Event ignored: non-melodic instrument");
                        continue;
                    }

                    // Check if event is note on event
                    if (midiEvent is not NoteOnEvent noteOnEvent) continue;

                    // Check if current channel is rhythm
                    if (noteOnEvent.Channel == 10)
                    {
                        if (printAllow)
                            WriteLine("Event ignored: rhythm channel");
                        continue;
                    }

                    int note;
                    double time;
                    double length;
                    int velocity;

                    try
                    {
                        // Get note attributes
                        note = noteOnEvent.NoteNumber;
                        time = (double)noteOnEvent.AbsoluteTime / (deltaTicksPerQuarterNote * 4);
                        length = (double)noteOnEvent.NoteLength / (deltaTicksPerQuarterNote * 4);
                        velocity = noteOnEvent.Velocity;
                    }
                    catch
                    {
                        if (printAllow)
                            WriteLine("Event ignored: unable to parse channel");
                        break;
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
                        WriteLine("Track ignored: too few unique notes");
                    continue;
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
                        WriteLine("Track ignored: too large average rest");
                    continue;
                }
                
                // Check average overlap
                if (overlap > 0.05)
                {
                    if (printAllow)
                        WriteLine("Track ignored: too large average overlap");
                    continue;
                }

                // Deduce melody, add to result
                var tokens = TokenMethods.Tokenize(notes);
                var tokensStr = TokenMethods.TokensToString(tokens);
                
                // Trim measure tokens
                tokensStr = tokensStr.Trim('M');
                tokensStr += 'M';
                
                // Get export folder name
                int exportId;
                lock (Lock) exportId = Lock.Value++;
                
                var exportFolderName = Path.Join(targetDirectory, $"{exportId / 10000}/");
                Directory.CreateDirectory(exportFolderName);

                // Get export file name, write to disk
                var exportFileName = Path.Join(exportFolderName, $"{exportId % 10000}.tokens");
                File.WriteAllText(exportFileName, tokensStr);

                numberOfUsableTracks++;
            }

            WriteLine($"DONE {fileName} ({numberOfUsableTracks} usable tracks)");
        });

        stopWatch.Stop();
        
        WriteLine($"Completed in {stopWatch.ElapsedMilliseconds / 1000} seconds!");
    }
}
