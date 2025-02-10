using System.Diagnostics;

namespace Core.midi.token;

public static class TokenMethods
{
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

    public static bool HasDuration(this Token token)
    {
        return token.IsNote(out _) || token is Token.Rest or Token.PassingTone;
    }

    public static Token FromChar(char c) => c switch
    {
        '.' => Token.Rest,
        '1' => Token.Note1,
        '2' => Token.Note2,
        '3' => Token.Note3,
        '4' => Token.Note4,
        '5' => Token.Note5,
        '6' => Token.Note6,
        '7' => Token.Note7,

        'p' => Token.PassingTone,

        'F' => Token.Faster,
        'S' => Token.Slower,
        'L' => Token.Louder,
        'Q' => Token.Quieter,
        'U' => Token.OctaveUp,
        'D' => Token.OctaveDown,

        _ => throw new ArgumentOutOfRangeException(nameof(c), c, null)
    };
    
    public static char ToChar(Token token) => token switch
    {
        Token.Rest => '.',
        Token.Note1 => '1',
        Token.Note2 => '2',
        Token.Note3 => '3',
        Token.Note4 => '4',
        Token.Note5 => '5',
        Token.Note6 => '6',
        Token.Note7 => '7',

        Token.PassingTone => 'p',

        Token.Faster => 'F',
        Token.Slower => 'S',
        Token.Louder => 'L',
        Token.Quieter => 'Q',
        Token.OctaveUp => 'U',
        Token.OctaveDown => 'D',

        _ => throw new ArgumentOutOfRangeException(nameof(token), token, null)
    };
}
