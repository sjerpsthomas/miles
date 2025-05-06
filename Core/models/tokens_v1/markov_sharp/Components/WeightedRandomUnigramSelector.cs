namespace Core.models.tokens_v1.markov_sharp.Components
{
    public class WeightedRandomUnigramSelector<T> : IUnigramSelector<T>
    {
        public T SelectUnigram(IEnumerable<T> ngrams)
        {
            return ngrams.OrderBy(a => Guid.NewGuid()).FirstOrDefault();
        }
    }
}
