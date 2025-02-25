using Core.midi.token.conversion;
using Core.midi.token.conversion.stage;
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
    
    public static List<Token> Tokenize(List<MidiNote> midiNotes)
    {
        var relativeMelody = PitchStage.TokenizePitch(midiNotes);
        var tokenMelody = OctaveStage.TokenizeOctaves(relativeMelody);
        var timedTokenMelody = TimingStage.TokenizeTiming(tokenMelody);
        var tokens = VelocityStage.TokenizeVelocity(timedTokenMelody);

        return tokens;
    }
    
    public static List<MidiNote> Reconstruct(List<Token> tokens, LeadSheet leadSheet, int startMeasureNum)
    {
        var timedTokenMelody = VelocityStage.ReconstructVelocity(tokens);
        var tokenMelody = TimingStage.ReconstructTiming(timedTokenMelody);
        var relativeMelody = OctaveStage.ReconstructOctaves(tokenMelody);
        var midiNotes = PitchStage.ReconstructPitch(relativeMelody, leadSheet, startMeasureNum);

        return midiNotes;
    }

    public static bool IsNote(this Token token)
    {
        var intToken = (int)token;
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
    
    public static double ToDouble(this TokenSpeed tokenSpeed) => tokenSpeed switch
    {
        TokenSpeed.SuperFast => 0.0625,
        TokenSpeed.Fast => 0.125,
        TokenSpeed.Slow => 0.25,
        TokenSpeed.SuperSlow => 0.5,
        _ => throw new ArgumentOutOfRangeException()
    };
    
    public static bool HasVelocity(this Token token, out TokenVelocity velocity)
    {
        velocity = TokenVelocity.Quiet;
        
        switch (token)
        {
            case Quiet:
                velocity = TokenVelocity.Quiet;
                break;
            case Loud:
                velocity = TokenVelocity.Loud;
                break;
            
            default:
                return false;
        }

        return true;
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
