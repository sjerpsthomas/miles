namespace Core.models.tokens_v1.markov_sharp.Components
{
    public interface IUnigramSelector <TUnigram>
    {
        TUnigram SelectUnigram(IEnumerable<TUnigram> ngrams);
    }
}
