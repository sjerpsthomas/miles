using Core.tokens.v2;
using TorchSharp;
using static TorchSharp.torch;
using static TorchSharp.torch.nn;

namespace Core.models.tokens_v2;

public class V2_TokenNeuralNet
{
    private Module<Tensor, Tensor> _model;

    public const int ContextWindow = 30;
    
    private (Tensor, Tensor)[] _dataSetChunks;

    public Device Device;
    
    public V2_TokenNeuralNet()
    {
        Device = CUDA;
        
        _model = Sequential(
            ("emb", Embedding(16, 3)),
            ("f", Flatten(start_dim: 1)),
            ("lin1", Linear(ContextWindow * 3, 5)),
            ("r", ReLU()),
            ("lin3", Linear(5, 5)),
            ("d", Dropout(0.2)),
            ("lin4", Linear(5, 5)),
            ("lin2", Linear(5, 16))

        ).to(Device);
        
        // _model = new NeuralNetModule("module", Device);
    }

    public void LoadData(V2_Token[] tokens)
    {
        // Create dataset
        List<(Tensor input, Tensor target)> data = new(tokens.Length);
        Console.WriteLine("Iterating...");
        
        // Iterate
        for (var i = 0; i < tokens.Length - ContextWindow - 1; i += 1)
        {
            // Get chunks
            var input = tokens[i..(i + ContextWindow)];
            var target = tokens[i + ContextWindow];

            // Get input tensor
            var arr = input.Select(it => (long)it).ToArray();
            var inputTensor = from_array(arr, ScalarType.Int64).unsqueeze(0);
            
            // Get target tensor
            var targetTensor = from_array(new[] {(long)target}, ScalarType.Int64);

            // Set in chunks
            data.Add((
                inputTensor,
                targetTensor
            ));
        }
        
        // Shuffle
        var rng = new Random();
        var n = data.Count;
        Console.WriteLine("Shuffling...");
        
        while (n > 1) {  
            n--;  
            var k = rng.Next(n + 1);  
            (data[k], data[n]) = (data[n], data[k]);
        }  
        
        // Batch
        const int batchSize = 50;
        List<(Tensor, Tensor)> chunksList = [];
        Console.WriteLine("Batching...");
        
        for (var batchStart = 0; batchStart < data.Count; batchStart += batchSize)
        {
            var batch = data
                .Skip(batchStart)
                .Take(batchSize)
                .ToList();

            // Split into inputs and targets
            var inputs = batch.Select(b => b.input).ToArray();
            var targets = batch.Select(b => b.target).ToArray();
            
            // Stack tensors along the batch dimension
            var inputBatch = cat(inputs, dim: 0).to(Device); // Shape: [BatchSize, ContextWindow]
            var targetBatch = cat(targets, dim: 0).to(Device); // Shape: [BatchSize]
            
            chunksList.Add((inputBatch, targetBatch));
        }
        
        _dataSetChunks = chunksList.ToArray();

        Console.WriteLine("Done!");
    }
    
    public void Train(string path)
    {
        var optimizer = optim.Adam(_model.parameters());
        
        for (var i = 0; i < 50; i++)
        {
            Console.WriteLine($"Epoch {i}");

            for (var index = 0; index < _dataSetChunks.Length; index++)
            {
                var (inputBatch, targetBatch) = _dataSetChunks[index];

                using var eval = _model.forward(inputBatch);
                using var loss = functional.cross_entropy(eval, targetBatch);

                if (index % 200 == 0)
                {
                    var lossStr = $"{loss.item<float>():F2}";
                    var inputStr = V2_TokenMethods.V2_TokensToString(inputBatch[0].data<long>().Select(it => (V2_Token)it).ToList());
                    
                    var probs = softmax(eval[0], dim: 0);
                    var dist = distributions.Categorical(probs);
                    var sample = dist.sample();
                    
                    var outputStr = V2_TokenMethods.V2_TokensToString([(V2_Token)sample.item<long>()]);
                    var targetStr = V2_TokenMethods.V2_TokensToString([(V2_Token)targetBatch[0].item<long>()]);
                    
                    Console.WriteLine($"{inputStr} ==> {outputStr} ({targetStr}) [{lossStr}]");
                }

                optimizer.zero_grad();
                loss.backward();
                optimizer.step();
            }

            Console.WriteLine();
        }

        _model.save(path);
        Console.WriteLine($"Saved to {path}!");
    }

    public void Load(string path) => _model.load(path);

    public Tensor TokensToTensor(List<V2_Token> input)
    {
        var inputs = input.Select(it => (long)it).ToArray();
        return unsqueeze(inputs, 0).to(Device);
    }
    
    public IEnumerable<V2_Token> Infer(Tensor previous)
    {
        using var eval = _model.forward(previous);

        var probs = softmax(eval[0], dim: 0);
        var dist = distributions.Categorical(probs);
        var sample = dist.sample();
        
        yield return (V2_Token)sample.item<long>();
    }
}