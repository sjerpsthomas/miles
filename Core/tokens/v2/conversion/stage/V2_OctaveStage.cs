using static Core.tokens.v2.conversion.V2_RelativeMelody;
using static Core.tokens.v2.conversion.V2_TokenMelody;

namespace Core.tokens.v2.conversion.stage;

public static class V2_OctaveStage
{
    public static V2_TokenMelody TokenizeOctaves(V2_RelativeMelody relativeMelody)
    {
        return new V2_TokenMelody
        {
            Tokens = relativeMelody.Tokens.Select(token => 
                new V2_TokenMelodyToken(token.OctaveScaleNote % V2_ChordMethods.OctaveSize, token.Time, token.Length, token.Velocity)
            ).ToList()
        };
    }
    
    private enum OctaveDirection { Up, Down };
    private record OctaveEvent(OctaveDirection Direction, int Index, double Priority);
    public static V2_RelativeMelody ReconstructOctaves(V2_TokenMelody tokenMelody)
    {
        var tokens = tokenMelody.Tokens;
        
        // Early return
        if (tokens is [])
            return new V2_RelativeMelody();

        var t = 0.0;
        var currentOctaveLength = 0.0;
        
        // Get all octave events
        List<OctaveEvent> octaveEvents = [];
        for (var index = 0; index < tokens.Count; index++)
        {
            // Get token, skip if not note
            var token = tokens[index];
            if (token is not var (scaleNote, _, length, _)) continue;

            t += length;
            currentOctaveLength += length;
            
            // Find second note
            if (index + 1 >= tokens.Count) continue;
            if (tokens[index + 1] is not var (scaleNote2, _, length2, _)) continue;

            // Skip when note not leading to octave break
            if (Math.Abs(scaleNote - scaleNote2) < 6) continue;

            var direction = scaleNote < scaleNote2 ? OctaveDirection.Down : OctaveDirection.Up;

            Console.WriteLine($"{scaleNote} - {scaleNote2}");
            
            // Create octave event
            octaveEvents.Add(
                new OctaveEvent(direction, index + 1, currentOctaveLength)
            );

            currentOctaveLength = 0.0;
        }

        // Limit octave events based on priority
        // var measureCount = (int)Math.Truncate(tokens.Last().Time) + 1;
        // octaveEvents = octaveEvents
        //     .OrderByDescending(it => it.Priority)
        //     .Take(measureCount / 2)
        //     .OrderBy(it => it.Index)
        //     .ToList();

        Console.WriteLine(string.Join(", ", octaveEvents.Select(it => it.ToString())));
        
        // Create tokens
        var res = new V2_RelativeMelody { Tokens = new(tokens.Count) };
        {
            var currentOctave = 3;
            var octaveEventIndex = 0;

            for (var index = 0; index < tokens.Count; index++)
            {
                // Apply octave event if possible
                if (octaveEventIndex < octaveEvents.Count)
                {
                    var octaveEvent = octaveEvents[octaveEventIndex];
                    if (octaveEvent.Index == index)
                    {
                        currentOctave += octaveEvent.Direction == OctaveDirection.Up ? 1 : -1;
                        currentOctave = Math.Clamp(currentOctave, 0, 5);
                        // Console.WriteLine($"{octaveEvent.Direction} --> {currentOctave}");
                        octaveEventIndex++;
                    }
                }

                res.Tokens.Add(tokens[index] switch {
                    var (scaleNote, time, length, velocity) =>
                        new V2_RelativeMelodyToken(scaleNote + V2_ChordMethods.OctaveSize * currentOctave, time, length, velocity),
                    _ => throw new ArgumentOutOfRangeException()
                });
            }
        }
        
        return res;
    }
}