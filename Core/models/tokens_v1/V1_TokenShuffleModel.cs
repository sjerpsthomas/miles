using Core.midi;
using Core.tokens.v1;
using static Core.tokens.v1.V1_Token;
using static Core.tokens.v1.V1_TokenMethods;

namespace Core.models.tokens_v1;

public class V1_TokenShuffleModel
{
    public record MirrorToken(V1_Token Token, V1_TokenSpeed Speed, V1_TokenVelocity Velocity);
    
    private MirrorToken[] ToMirrorTokens(IEnumerable<V1_Token> tokens)
    {
        var currentSpeed = V1_TokenSpeed.Fast;
        var currentVelocity = V1_TokenVelocity.Quiet;

        List<MirrorToken> mirrorTokens = [];
        
        foreach (var token in tokens)
        {
            switch (token)
            {
                case SuperFast:
                case Fast:
                case Slow:
                case SuperSlow:
                    token.V1_HasSpeed(out currentSpeed);
                    break;
                
                case Loud:
                case Quiet:
                case Measure:
                    token.V1_HasVelocity(out currentVelocity);
                    break;
                
                default:
                    mirrorTokens.Add(new MirrorToken(token, currentSpeed, currentVelocity));
                    break;
            }
        }

        return mirrorTokens.ToArray();
    }

    private V1_Token[] ToTokens(IEnumerable<MirrorToken> mirrorTokens)
    {
        var currentSpeed = V1_TokenSpeed.Fast;
        var currentVelocity = V1_TokenVelocity.Quiet;

        List<V1_Token> tokens = [];
        
        foreach (var (token, speed, velocity) in mirrorTokens)
        {
            if (speed != currentSpeed)
            {
                tokens.Add(speed switch
                {
                    V1_TokenSpeed.SuperFast => SuperFast,
                    V1_TokenSpeed.Fast => Fast,
                    V1_TokenSpeed.Slow => Slow,
                    V1_TokenSpeed.SuperSlow => SuperSlow,
                    _ => throw new ArgumentOutOfRangeException()
                });

                currentSpeed = speed;
            }

            if (velocity != currentVelocity)
            {
                tokens.Add(velocity switch
                {
                    V1_TokenVelocity.Loud => Loud,
                    V1_TokenVelocity.Quiet => Quiet,
                    _ => throw new ArgumentOutOfRangeException()
                });

                currentVelocity = velocity;
            }
            
            tokens.Add(token);
        }

        return tokens.ToArray();
    }
    
    public V1_Token[] Mirror(IEnumerable<V1_Token> tokens)
    {
        // Convert to mirror tokens, reverse, convert back
        var mirrorTokens = ToMirrorTokens(tokens);
        var reversedTokens = mirrorTokens.ToList();
        reversedTokens.Reverse();
        return ToTokens(reversedTokens);
    }

    public V1_Token[] Flip(IEnumerable<V1_Token> tokens)
    {
        return tokens.Select(token =>
            token is Note1 or Note2 or Note3 or Note4 or Note5 or Note6 or Note7
                ? (V1_Token)(8 - (int)token)
                : token
        ).ToArray();
    }

    public V1_Token[] Transpose(IEnumerable<V1_Token> tokens, int by)
    {
        return tokens.Select(token =>
            token is Note1 or Note2 or Note3 or Note4 or Note5 or Note6 or Note7
                ? (V1_Token)(Chord.PosMod((int)token + by - 1, 7) + 1)
                : token
        ).ToArray();
    }

    public List<V1_Token> Permutate(List<V1_Token> tokens, int numPermutations = 3)
    {
        // Initialize RNG
        var rng = new Random();

        // Repeat a certain number of times
        for (var i = 0; i < numPermutations; i++)
        {
            // Get start and end of slice
            var start = rng.Next(0, tokens.Count);
            var end = rng.Next(0, tokens.Count);
            if (start == end) continue;
            if (start > end)
                (start, end) = (end, start);

            // Get slice
            var listPart = tokens.GetRange(start, end - start);

            // Permutate (mirror, flip or transpose)
            IEnumerable<V1_Token> res = rng.NextDouble() switch
            {
                <= 0.333 => Mirror(listPart),
                <= 0.666 => Flip(listPart),
                _ => Transpose(listPart, rng.Next(-4, 3))
            };

            // Assign back to array
            var newTokens = tokens.Take(start).ToList();
            newTokens.AddRange(res);
            newTokens.AddRange(tokens.Skip(end));

            tokens = newTokens;
        }

        // Return
        return tokens;
    }
}