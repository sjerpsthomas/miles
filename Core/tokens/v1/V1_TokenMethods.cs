using Core.midi;
using Core.tokens.v1.conversion.stage;
using static Core.tokens.v1.V1_Token;

namespace Core.tokens.v1;

public static class V1_TokenMethods
{
    public static List<V1_Token> V1_Tokenize(List<MidiNote> midiNotes, LeadSheet? leadSheet = null)
    {
        var relativeMelody = V1_PitchStage.TokenizePitch(midiNotes, leadSheet);
        var tokenMelody = V1_OctaveStage.TokenizeOctaves(relativeMelody);
        var timedTokenMelody = V1_TimingStage.TokenizeTiming(tokenMelody, leadSheet);
        var tokens = V1_VelocityStage.TokenizeVelocity(timedTokenMelody);

        return tokens;
    }
    
    public static List<MidiNote> V1_Reconstruct(List<V1_Token> tokens, LeadSheet leadSheet, int startMeasureNum)
    {
        var timedTokenMelody = V1_VelocityStage.ReconstructVelocity(tokens);
        var tokenMelody = V1_TimingStage.ReconstructTiming(timedTokenMelody, leadSheet);
        var relativeMelody = V1_OctaveStage.ReconstructOctaves(tokenMelody);
        var midiNotes = V1_PitchStage.ReconstructPitch(relativeMelody, leadSheet, startMeasureNum);

        return midiNotes;
    }
    
    public enum V1_TokenVelocity
    {
        Loud,
        Quiet,
    }
    public enum V1_TokenSpeed
    {
        SuperFast,
        Fast,
        Slow,
        SuperSlow,
    }

    public static List<V1_Token> V1_TokensFromString(string str) => str.Select(V1_FromChar).ToList();
    public static string V1_TokensToString(List<V1_Token> tokens) => string.Concat(tokens.Select(V1_ToChar));


    public static List<V1_Token> V1_FromTokensFileStream(Stream stream)
    {
        using var reader = new StreamReader(stream);

        var content = reader.ReadToEnd();

        return V1_TokensFromString(content);
    }

    public static bool V1_IsNote(this V1_Token token)
    {
        var intToken = (int)token;
        return intToken is >= 1 and <= V1_ChordMethods.OctaveSize;
    }

    public static bool V1_HasSpeed(this V1_Token token, out V1_TokenSpeed speed)
    {
        speed = V1_TokenSpeed.SuperSlow;
        
        switch (token)
        {
            case SuperFast:
                speed = V1_TokenSpeed.SuperFast;
                break;
            case Fast:
                speed = V1_TokenSpeed.Fast;
                break;
            case Slow:
                speed = V1_TokenSpeed.Slow;
                break;
            case SuperSlow:
                speed = V1_TokenSpeed.SuperSlow;
                break;
            
            default:
                return false;
        }

        return true;
    }
    
    public static double V1_ToDouble(this V1_TokenSpeed tokenSpeed) => tokenSpeed switch
    {
        V1_TokenSpeed.SuperFast => 0.0625,
        V1_TokenSpeed.Fast => 0.125,
        V1_TokenSpeed.Slow => 0.25,
        V1_TokenSpeed.SuperSlow => 0.5,
        _ => throw new ArgumentOutOfRangeException()
    };

    public static V1_TokenSpeed V1_ToSpeed(this double speed) =>
        (int)Math.Round(Math.Log2(speed)) switch
        {
            <= -4 => V1_TokenSpeed.SuperFast,
            -3 => V1_TokenSpeed.Fast,
            -2 => V1_TokenSpeed.Slow,
            _ => V1_TokenSpeed.SuperSlow,
        };

    public static bool V1_HasVelocity(this V1_Token token, out V1_TokenVelocity velocity)
    {
        velocity = V1_TokenVelocity.Quiet;
        
        switch (token)
        {
            case Quiet:
                velocity = V1_TokenVelocity.Quiet;
                break;
            case Loud:
                velocity = V1_TokenVelocity.Loud;
                break;
            
            default:
                return false;
        }

        return true;
    }

    public static V1_Token V1_FromChar(char c) => c switch
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
    
    public static char V1_ToChar(V1_Token token) => token switch
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
