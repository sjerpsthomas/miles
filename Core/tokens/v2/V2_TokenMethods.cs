using Core.midi;
using Core.tokens.v2.conversion.stage;
using static Core.tokens.v2.V2_Token;

namespace Core.tokens.v2;

public static class V2_TokenMethods
{
    public static List<V2_Token> V2_Tokenize(List<MidiNote> midiNotes, LeadSheet? leadSheet = null, int startMeasureNum = 0)
    {
        var relativeMelody = V2_PitchStage.TokenizePitch(midiNotes, leadSheet, startMeasureNum);
        var tokenMelody = V2_OctaveStage.TokenizeOctaves(relativeMelody);
        var timedTokenMelody = V2_TimingStage.TokenizeTiming(tokenMelody, leadSheet);
        var tokens = V2_VelocityStage.TokenizeVelocity(timedTokenMelody);

        return tokens;
    }
    
    public static List<MidiNote> V2_Reconstruct(List<V2_Token> tokens, LeadSheet leadSheet, int startMeasureNum)
    {
        var timedTokenMelody = V2_VelocityStage.ReconstructVelocity(tokens);
        var tokenMelody = V2_TimingStage.ReconstructTiming(timedTokenMelody, leadSheet);
        var relativeMelody = V2_OctaveStage.ReconstructOctaves(tokenMelody);
        var midiNotes = V2_PitchStage.ReconstructPitch(relativeMelody, leadSheet, startMeasureNum);

        return midiNotes;
    }   
    
    
    public enum V2_TokenVelocity
    {
        Loud,
        Quiet,
    }
    public enum V2_TokenSpeed
    {
        SuperFast,
        Fast,
        Slow,
        SuperSlow,
    }

    public static List<V2_Token> V2_TokensFromString(string str) => str.Select(V2_FromChar).ToList();
    public static string V2_TokensToString(List<V2_Token> tokens) => string.Concat(tokens.Select(V2_ToChar));


    public static List<V2_Token> V2_FromTokensFileStream(Stream stream)
    {
        using var reader = new StreamReader(stream);

        var content = reader.ReadToEnd();

        return V2_TokensFromString(content);
    }

    public static bool V2_IsNote(this V2_Token token)
    {
        var intToken = (int)token;
        return intToken is >= 1 and <= 7;
    }

    public static bool V2_HasSpeed(this V2_Token token, out V2_TokenSpeed speed)
    {
        speed = V2_TokenSpeed.SuperSlow;
        
        switch (token)
        {
            case SuperFast:
                speed = V2_TokenSpeed.SuperFast;
                break;
            case Fast:
                speed = V2_TokenSpeed.Fast;
                break;
            case Slow:
                speed = V2_TokenSpeed.Slow;
                break;
            case SuperSlow:
                speed = V2_TokenSpeed.SuperSlow;
                break;
            
            default:
                return false;
        }

        return true;
    }
    
    public static double V2_ToDouble(this V2_TokenSpeed tokenSpeed) => tokenSpeed switch
    {
        V2_TokenSpeed.SuperFast => 0.0625,
        V2_TokenSpeed.Fast => 0.125,
        V2_TokenSpeed.Slow => 0.25,
        V2_TokenSpeed.SuperSlow => 0.5,
        _ => throw new ArgumentOutOfRangeException()
    };

    public static V2_TokenSpeed V2_ToSpeed(this double speed) =>
        (int)Math.Round(Math.Log2(speed)) switch
        {
            <= -4 => V2_TokenSpeed.SuperFast,
            -3 => V2_TokenSpeed.Fast,
            -2 => V2_TokenSpeed.Slow,
            _ => V2_TokenSpeed.SuperSlow,
        };

    public static bool V2_HasVelocity(this V2_Token token, out V2_TokenVelocity velocity)
    {
        velocity = V2_TokenVelocity.Quiet;
        
        switch (token)
        {
            case Quiet:
                velocity = V2_TokenVelocity.Quiet;
                break;
            case Loud:
                velocity = V2_TokenVelocity.Loud;
                break;
            
            default:
                return false;
        }

        return true;
    }

    public static V2_Token V2_FromChar(char c) => c switch
    {
        '.' => Rest,
        
        '1' => Note1,
        '2' => Note2,
        '3' => Note3,
        '4' => Note4,
        '5' => Note5,
        '6' => Note6,
        '7' => Note7,
        '8' => Note8,
        '9' => Note9,
        '!' => Note10,
        '@' => Note11,
        '#' => Note12,

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
    
    public static char V2_ToChar(V2_Token token) => token switch
    {
        Rest => '.',
        Note1 => '1',
        Note2 => '2',
        Note3 => '3',
        Note4 => '4',
        Note5 => '5',
        Note6 => '6',
        Note7 => '7',
        Note8 => '8',
        Note9 => '9',
        Note10 => '!',
        Note11 => '@',
        Note12 => '#',

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