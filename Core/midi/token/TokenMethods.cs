using System.Diagnostics;
using Core.midi.token.conversion;
using NAudio.Wave;
using static Core.midi.token.Token;

namespace Core.midi.token;

public static class TokenMethods
{
    public enum TokenVelocity
    {
        Loud,
        Quiet,
    }
    public enum TokenSpeed
    {
        SuperFast,
        Fast,
        Slow,
        SuperSlow,
    }
    
    public static List<Token> TokensFromString(string str) => str.Select(FromChar).ToList();
    public static string TokensToString(List<Token> tokens) => string.Concat(tokens.Select(ToChar));
    
    public static List<MidiNote> ResolveMelody(List<Token> tokens, LeadSheet leadSheet, int startMeasureNum)
    {
        // Console.WriteLine("Resolving velocity...");
        var velocityTokenMelody = VelocityStage.ResolveVelocity(tokens);

        // Console.WriteLine("Resolving timing...");
        var tokenMelody = TimingStage.ResolveTiming(velocityTokenMelody);

        // Console.WriteLine("Resolving octaves...");
        var octaveMelody = OctaveStage.ResolveOctaves(tokenMelody);

        // Console.WriteLine("Resolving passing tones...");
        var midiNotes = PassingToneStage.ResolvePassingTones(octaveMelody, leadSheet, startMeasureNum);

        return midiNotes;
    }

    public static List<Token> DeduceMelody(List<MidiNote> midiNotes)
    {
        // Console.WriteLine("Deducing passing tones...");
        var octaveMelody = PassingToneStage.DeducePassingTones(midiNotes);

        // Console.WriteLine("Deducing octaves...");
        var tokenMelody = OctaveStage.DeduceOctaves(octaveMelody);
        
        // Console.WriteLine("Deducing timing...");
        var velocityTokenMelody = TimingStage.DeduceTiming(tokenMelody);

        // Console.WriteLine("Deducing velocity...");
        var tokens = VelocityStage.DeduceVelocity(velocityTokenMelody);

        return tokens;
    }
    
    public static int ToNote(this Token token, Chord currentChord)
    {
        if (!token.IsNote(out var intToken))
            Debug.Fail("Token is not a note");

        return currentChord.GetAbsoluteNote(intToken);
    }

    public static bool IsNote(this Token token, out int intToken)
    {
        intToken = (int)token;
        
        return intToken is >= 1 and <= 7;
    }

    public static bool HasSpeed(this Token token, out TokenSpeed speed)
    {
        speed = TokenSpeed.SuperSlow;
        
        switch (token)
        {
            case SuperFast:
                speed = TokenSpeed.SuperFast;
                break;
            case Fast:
                speed = TokenSpeed.Fast;
                break;
            case Slow:
                speed = TokenSpeed.Slow;
                break;
            case SuperSlow:
                speed = TokenSpeed.SuperSlow;
                break;
            
            default:
                return false;
        }

        return true;
    }

    public static bool HasDuration(this Token token)
    {
        return token.IsNote(out _) || token is Rest or PassingTone;
    }

    public static Token FromChar(char c) => c switch
    {
        '.' => Rest,
        
        '1' => Note1,
        '2' => Note2,
        '3' => Note3,
        '4' => Note4,
        '5' => Note5,
        '6' => Note6,
        '7' => Note7,

        'p' => PassingTone,

        'F' => SuperFast,
        'f' => Fast,
        's' => Slow,
        'S' => SuperSlow,
        
        'L' => Loud,
        'Q' => Quiet,
        
        'M' => Measure,
        
        _ => throw new ArgumentOutOfRangeException(nameof(c), c, null)
    };
    
    public static char ToChar(Token token) => token switch
    {
        Rest => '.',
        Note1 => '1',
        Note2 => '2',
        Note3 => '3',
        Note4 => '4',
        Note5 => '5',
        Note6 => '6',
        Note7 => '7',

        PassingTone => 'p',

        SuperFast => 'F',
        Fast => 'f',
        Slow => 's',
        SuperSlow => 'S',
        
        Loud => 'L',
        Quiet => 'Q',
        
        Measure => 'M',

        _ => throw new ArgumentOutOfRangeException(nameof(token), token, null)
    };
}
