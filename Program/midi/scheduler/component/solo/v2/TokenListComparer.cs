using System.Collections.Generic;
using System.Linq;
using Core.midi.token;

namespace Program.midi.scheduler.component.solo.v2;

public class TokenListComparer : IEqualityComparer<List<Token>>
{
    public bool Equals(List<Token> x, List<Token> y) => x!.SequenceEqual(y!);

    public int GetHashCode(List<Token> list) =>
        list.Aggregate(17, (hash, token) => hash * 31 + token.GetHashCode());
}