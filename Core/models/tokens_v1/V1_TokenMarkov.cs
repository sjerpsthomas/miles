using Core.models.tokens_v1.markov_sharp;
using Core.tokens.v1;

namespace Core.models.tokens_v1;

public class V1_TokenMarkov : GenericMarkov<List<V1_Token>, V1_Token>
{
    public Random Random = new();
    
    public V1_TokenMarkov(int level = 2) : base(level) { }

    public override IEnumerable<V1_Token> SplitTokens(List<V1_Token> input) => input ?? [];

    public override List<V1_Token> RebuildPhrase(IEnumerable<V1_Token> tokens) => tokens.ToList();

    public override V1_Token GetTerminatorUnigram() => V1_Token.Measure;

    public override V1_Token GetPrepadUnigram() => V1_Token.Fast;
}