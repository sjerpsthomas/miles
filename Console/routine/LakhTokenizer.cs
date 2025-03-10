using System.Diagnostics;
using Core.conversion;
using Core.midi.token;
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
            var numberOfUsableTracks = 0;

            var tracks = Conversion.LakhToTokens(fileName, printAllow);

            foreach (var track in tracks)
            {
                var tokensStr = TokenMethods.TokensToString(track);
                
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
