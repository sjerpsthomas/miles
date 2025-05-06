using Core.tokens.v2;

namespace Core.models.tokens_v2;

public class V2_TokenListComparer : IEqualityComparer<List<V2_Token>>
{
    public bool Equals(List<V2_Token> x, List<V2_Token> y) => x!.SequenceEqual(y!);

    public int GetHashCode(List<V2_Token> list) =>
        list.Aggregate(17, (hash, token) => hash * 31 + token.GetHashCode());
}
