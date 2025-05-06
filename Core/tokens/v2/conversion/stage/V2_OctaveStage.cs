namespace Core.tokens.v2.conversion.stage;

public static class V2_OctaveStage
{
    public static V2_TokenMelody TokenizeOctaves(V2_RelativeMelody relativeMelody)
    {
        throw new Exception("TODO: copy-paste!");
    }
    
    private enum OctaveDirection { Up, Down };
    private record OctaveEvent(OctaveDirection Direction, int Index, double Priority);
    public static V2_RelativeMelody ReconstructOctaves(V2_TokenMelody tokenMelody)
    {
        var tokens = tokenMelody.Tokens;
        
        // Early return
        if (tokens is [])
            return new V2_RelativeMelody();
        
        // Get all octave events
        List<OctaveEvent> octaveEvents = [];
        for (var index = 0; index < tokens.Count; index++)
        {
            // Get token, skip if not note
            var token = tokens[index];
            if (token is not V2_TokenMelody.V2_TokenMelodyNote(var scaleNote, _, var length, _)) continue;
                
            // Skip when note not leading to octave break
            if (scaleNote is > 2 and < 6) continue;
            
            // Find second note
            var secondIndex = index;
            do secondIndex++;
            while (secondIndex < tokens.Count - 1 && tokens[secondIndex] is not V2_TokenMelody.V2_TokenMelodyNote);

            if (secondIndex >= tokens.Count)
                continue;
            if (tokens[secondIndex] is not V2_TokenMelody.V2_TokenMelodyNote(var scaleNote2, _, var length2, _))
                continue;

            // Skip when note not leading to octave break
            if (scaleNote2 is > 2 and < 6) continue;
            if (scaleNote2 <= 2 && scaleNote <= 2) continue;
            if (scaleNote2 >= 6 && scaleNote >= 6) continue;

            var direction = scaleNote < scaleNote2 ? OctaveDirection.Down : OctaveDirection.Up;
            // Console.WriteLine($"scaleNote: {scaleNote}, scaleNote2: {scaleNote2}, direction: {direction}");

            var priority =
                Math.Min(length / 0.25, 1.0) +
                Math.Min(length2 / 0.25, 1.0) +
                (6 - (Math.Max(scaleNote, scaleNote2) - Math.Min(scaleNote, scaleNote2))) / 2.0;
            
            // Create octave event
            octaveEvents.Add(
                new OctaveEvent(direction, index + 1, priority)
            );
        }

        // Limit octave events based on priority
        var measureCount = (int)Math.Truncate(tokens.Last().Time) + 1;
        octaveEvents = octaveEvents
            .OrderBy(it => it.Priority)
            .Take(measureCount)
            .OrderBy(it => it.Index)
            .ToList();

        // Create tokens
        var res = new V2_RelativeMelody { Tokens = new(tokens.Count) };
        {
            var currentOctave = 2;
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
                        currentOctave = Math.Clamp(currentOctave, 0, 3);
                        octaveEventIndex++;
                    }
                }

                res.Tokens.Add(tokens[index] switch {
                    V2_TokenMelody.V2_TokenMelodyNote(var scaleNote, var time, var length, var velocity) =>
                        new V2_RelativeMelody.V2_RelativeMelodyNote(scaleNote + 7 * currentOctave, time, length, velocity),
                    V2_TokenMelody.V2_TokenMelodyPassingTone(var time, var length, var velocity) =>
                        new V2_RelativeMelody.V2_RelativeMelodyPassingTone(time, length, velocity),
                    _ => throw new ArgumentOutOfRangeException()
                });
            }
        }
        
        return res;
    }
}