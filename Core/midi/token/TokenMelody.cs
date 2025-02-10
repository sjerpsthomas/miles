namespace Core.midi.token;

public class TokenMelody
{
    public List<Token> Tokens = [];

    public static TokenMelody FromMidiMelody(MidiMelody midiMelody)
    {
        var res = new TokenMelody();
        return res;
    }

    private class TokenSpan(List<Token> tokens, double startTime, double endTime)
    {
        public List<Token> Tokens = tokens;
        public double StartTime = startTime;
        public double EndTime = endTime;
    }

    public static TokenMelody FromString(string str) => new() { Tokens = str.Select(TokenMethods.FromChar).ToList() };
    public override string ToString() => string.Concat(Tokens.Select(TokenMethods.ToChar));

    private List<TokenSpan>? ToTokenSpans(int measureCount = 4)
    {
        List<TokenSpan> tokenSpans = [];
        TokenSpan? currentTokenSpan = null;
        
        // Create timespans
        var time = 0.0;
        var speed = 1.0;

        // Iterate through tokens
        foreach (var token in Tokens)
        {
            switch (token)
            {
                case Token.Faster:
                case Token.Slower:
                    speed *= token == Token.Faster ? 1.0 / 2.0 : 2.0;

                    if (currentTokenSpan != null)
                    {
                        currentTokenSpan.EndTime = time;
                        tokenSpans.Add(currentTokenSpan);
                        currentTokenSpan = null;
                    }
                    break;
                default:
                    currentTokenSpan ??= new TokenSpan([], time, 0.0);
                    
                    currentTokenSpan.Tokens.Add(token);

                    if (token.IsNote(out _))
                        time += speed;
                    
                    break;
            }
        }
        
        if (currentTokenSpan is { Tokens: not [] })
        {
            currentTokenSpan.EndTime = time;
            tokenSpans.Add(currentTokenSpan);
        }
        
        // Early return if no token spans made
        if (time == 0.0 || tokenSpans is [])
            return null;

        var totalTime = tokenSpans[^1].EndTime;
        if (totalTime == 0.0)
            return null;
        
        // Normalize and quantize token spans
        double NormAndQuant(double t) => (int)(t / totalTime * measureCount * 12.0) / 12.0;
        foreach (var tokenSpan in tokenSpans)
        {
            tokenSpan.StartTime = NormAndQuant(tokenSpan.StartTime);
            tokenSpan.EndTime = NormAndQuant(tokenSpan.EndTime);
        }
        
        // Return
        return tokenSpans;
    }

    public List<MidiMeasure> ToMeasures(int startOctave, LeadSheet leadSheet, int startMeasure, int measureCount = 4)
    {
        // Create measures
        List<MidiMeasure> res = [];
        for (var i = 0; i < measureCount; i++) res.Add(new MidiMeasure());
        
        // Get token spans
        var tokenSpans = ToTokenSpans(measureCount);

        if (tokenSpans == null)
            return res;
        
        // Create timed token list
        List<(Token, double, double)> tokens = [];

        {
            var time = 0.0;
            foreach (var tokenSpan in tokenSpans)
            {
                var length = (tokenSpan.EndTime - tokenSpan.StartTime) /
                                tokenSpan.Tokens.Count(it => it.HasDuration());

                foreach (var token in tokenSpan.Tokens)
                {
                    if (token != Token.Rest)
                        tokens.Add((token, time, length));

                    if (token.HasDuration()) time += length;
                }
            }
        }
        
        // Fill in notes
        var octave = startOctave;
        var velocity = 100;
        var lastAbsoluteNote = -1;

        for (var index = 0; index < tokens.Count; index++)
        {
            var (token, time, length) = tokens[index];

            var measureNum = (int)Math.Truncate(time);
            var measure = res[measureNum];
            var measureTime = time - measureNum;
            var chord = leadSheet.ChordAtTime(time - startMeasure);

            // Handle passing tone
            if (token == Token.PassingTone)
            {
                var startNote = lastAbsoluteNote;
                int endNote;

                // Find index of end note
                var passingCount = 0;
                do
                {
                    passingCount++;
                    
                    // TODO extract
                    var (passingToken, _, _) = tokens[index + passingCount];
                    // Handle state-altering tokens
                    if (passingToken == Token.Louder)
                        velocity = Math.Max(velocity + 10, 127);
                    else if (passingToken == Token.Quieter)
                        velocity = Math.Min(velocity - 10, 50);
                    else if (passingToken == Token.OctaveUp)
                        octave += 1;
                    else if (passingToken == Token.OctaveDown)
                        octave -= 1;
                }
                while (index + passingCount < tokens.Count - 1 && tokens[index + passingCount] is (Token.PassingTone, _, _));
                
                if (index + passingCount == tokens.Count)
                {
                    // If no second note to pass to is found, chromatically walk up
                    endNote = startNote + passingCount;
                }
                else
                {
                    // Get note to pass to
                    var (endToken, endTime, _) = tokens[index + passingCount];
                    var _ = endToken.IsNote(out var endRelativeNote);
                    endNote = leadSheet.ChordAtTime(endTime).GetAbsoluteNote(endRelativeNote) + 12 * octave;
                }

                // Create passing tones
                for (var j = 0; j < passingCount; j++)
                {
                    var f = (j + 1) / ((double)passingCount + 1);
                    var note = (int)((1 - f) * startNote + f * endNote);

                    var t = measureTime + j * length;
                    
                    var midiNote = new MidiNote(OutputName.Algorithm, t, length, note, velocity);
                    measure.Notes.Add(midiNote);
                }

                index = index + passingCount - 1;
                
                continue;
            }

            // Handle notes
            if (token.IsNote(out var relativeNote))
            {
                var note = chord.GetAbsoluteNote(relativeNote) + 12 * octave;
                lastAbsoluteNote = note;
                
                var midiNote = new MidiNote(OutputName.Algorithm, measureTime, length, note, velocity);

                measure.Notes.Add(midiNote);

                continue;
            }
            
            // Handle state-altering tokens
            if (token == Token.Louder)
                velocity = Math.Max(velocity + 10, 127);
            else if (token == Token.Quieter)
                velocity = Math.Min(velocity - 10, 50);
            else if (token == Token.OctaveUp)
                octave += 1;
            else if (token == Token.OctaveDown)
                octave -= 1;
        }

        return res;
    }
}