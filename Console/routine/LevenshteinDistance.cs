using Core.midi;
using Core.tokens.v1;
using Fastenshtein;

namespace Console.routine;

public static class LevenshteinDistance
{
    public static List<V1_Token>[] GetFoursTokens(MidiSong song, OutputName outputName)
    {
        // Get notes of fours
        var fours = song.ToNotes()
            .Where(it => it.OutputName == outputName)
            .OrderBy(it => it.Time)
            .GroupBy(it => (int)(it.Time / 4))
            .ToList();
                    
        // Tokenize
        var foursTokens = Enumerable.Range(0, 64 / 4)
            .Select(i =>
            {
                return (fours.Find(x => x.Key == i)?.ToList() ?? [])
                    .Select(note => note with { Time = note.Time - i * 4 });
            })
            .Select(it => V1_TokenMethods.V1_Tokenize(it.ToList()))
            .ToArray();
        
        // Return
        return foursTokens;
    }
    
    public static void Run(string recordingPath)
    {
        const int numPupils = 5;
        const int numSessions = 4;
        const int numPerformances = 3;

        for (var pupil = 0; pupil < numPupils; pupil++)
        {
            for (var session = 0; session < numSessions; session++)
            {
                for (var performance = 0; performance < numPerformances; performance++)
                {
                    // Open song
                    var filePath = $"{recordingPath}/{pupil + 1}/{session + 1}/{performance + 1}.notes";
                    var song = MidiSong.FromNotesFileStream(new FileStream(filePath, FileMode.Open));

                    // Get tokens of fours of human and algorithm
                    var humanFoursTokens = GetFoursTokens(song, OutputName.Loopback);
                    var algorithmFoursTokens = GetFoursTokens(song, OutputName.Algorithm);
                    
                    // Get distances
                    var distances = GetDistances(humanFoursTokens, algorithmFoursTokens);
                    
                    // Save distances to file
                    var directory = $@"{recordingPath}\edit_distance\{pupil + 1}\{session + 1}";
                    Directory.CreateDirectory(directory);
                    
                    var outputPath = $@"{directory}\{performance + 1}.txt";
                    File.WriteAllLines(outputPath, distances.Select(it => it.ToString()));
                }
            }
        }
    }

    public static List<int> GetDistances(List<V1_Token>[] humanFours, List<V1_Token>[] algorithmFours, int measureCount = 64)
    {
        // Get consecutive fours
        List<List<V1_Token>> fours = [];
        for (var fourIndex = 0; fourIndex < measureCount / 8; fourIndex++)
        {
            // Add human four
            var humanFour = humanFours[fourIndex * 2];
            fours.Add(humanFour);

            // Add algorithm four
            var algorithmFour = algorithmFours[fourIndex * 2 + 1];
            fours.Add(algorithmFour);
        }
        
        // Compare fours, keep track of result
        List<int> res = [];
        for (var i = 0; i < fours.Count - 1; i++)
        {
            // Get fours
            var four = fours[i];
            var nextFour = fours[i + 1];
            
            // Get strings
            var fourString = V1_TokenMethods.V1_TokensToString(four);
            var nextFourString = V1_TokenMethods.V1_TokensToString(nextFour);
            
            // Get Levenshtein distance
            var levenshteinDistance = Levenshtein.Distance(fourString, nextFourString);
            res.Add(levenshteinDistance);
        }

        // Return
        return res;
    }
}