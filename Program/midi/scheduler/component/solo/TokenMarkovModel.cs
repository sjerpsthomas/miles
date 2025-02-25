using System;
using System.Collections.Generic;
using System.Linq;
using Core.midi.token;
using Program.midi.scheduler.component.solo.markov_sharp;

namespace Program.midi.scheduler.component.solo;

public class TokenMarkovModel : GenericMarkov<List<Token>, Token>
{
    public Random Random = new();
    
    public TokenMarkovModel(int level = 2) : base(level) { }

    public override IEnumerable<Token> SplitTokens(List<Token> input) => input ?? [];

    public override List<Token> RebuildPhrase(IEnumerable<Token> tokens) => tokens.ToList();

    public override Token GetTerminatorUnigram() => Token.Measure;

    public override Token GetPrepadUnigram() => Token.Fast;
}