using System;
using System.Collections.Generic;
using System.Linq;
using Core.midi;
using Core.midi.token;
using static Core.midi.token.Token;
using static Core.midi.token.TokenMethods;

namespace Program.midi.scheduler.component.solo;

public class TokenShuffleModel
{
    public record MirrorToken(Token Token, TokenSpeed Speed, TokenVelocity Velocity);
    
    private MirrorToken[] ToMirrorTokens(IEnumerable<Token> tokens)
    {
        var currentSpeed = TokenSpeed.Fast;
        var currentVelocity = TokenVelocity.Quiet;

        List<MirrorToken> mirrorTokens = [];
        
        foreach (var token in tokens)
        {
            switch (token)
            {
                case SuperFast:
                case Fast:
                case Slow:
                case SuperSlow:
                    token.HasSpeed(out currentSpeed);
                    break;
                
                case Loud:
                case Quiet:
                case Measure:
                    token.HasVelocity(out currentVelocity);
                    break;
                
                default:
                    mirrorTokens.Add(new MirrorToken(token, currentSpeed, currentVelocity));
                    break;
            }
        }

        return mirrorTokens.ToArray();
    }

    private Token[] ToTokens(IEnumerable<MirrorToken> mirrorTokens)
    {
        var currentSpeed = TokenSpeed.Fast;
        var currentVelocity = TokenVelocity.Quiet;

        List<Token> tokens = [];
        
        foreach (var (token, speed, velocity) in mirrorTokens)
        {
            if (speed != currentSpeed)
            {
                tokens.Add(speed switch
                {
                    TokenSpeed.SuperFast => SuperFast,
                    TokenSpeed.Fast => Fast,
                    TokenSpeed.Slow => Slow,
                    TokenSpeed.SuperSlow => SuperSlow,
                    _ => throw new ArgumentOutOfRangeException()
                });

                currentSpeed = speed;
            }

            if (velocity != currentVelocity)
            {
                tokens.Add(velocity switch
                {
                    TokenVelocity.Loud => Loud,
                    TokenVelocity.Quiet => Quiet,
                    _ => throw new ArgumentOutOfRangeException()
                });

                currentVelocity = velocity;
            }
            
            tokens.Add(token);
        }

        return tokens.ToArray();
    }
    
    public Token[] Mirror(IEnumerable<Token> tokens)
    {
        // Convert to mirror tokens, reverse, convert back
        var mirrorTokens = ToMirrorTokens(tokens);
        var reversedTokens = mirrorTokens.Reverse();
        return ToTokens(reversedTokens);
    }

    public Token[] Flip(IEnumerable<Token> tokens)
    {
        return tokens.Select(token =>
            token is Note1 or Note2 or Note3 or Note4 or Note5 or Note6 or Note7
                ? (Token)(8 - (int)token)
                : token
        ).ToArray();
    }

    public Token[] Transpose(IEnumerable<Token> tokens, int by)
    {
        return tokens.Select(token =>
            token is Note1 or Note2 or Note3 or Note4 or Note5 or Note6 or Note7
                ? (Token)(Chord.PosMod((int)token + by - 1, 7) + 1)
                : token
        ).ToArray();
    }

    public List<Token> Permutate(List<Token> tokens, int numPermutations = 3)
    {
        Console.WriteLine($"First token: {tokens[0]}");
        
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
            IEnumerable<Token> res = rng.NextDouble() switch
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