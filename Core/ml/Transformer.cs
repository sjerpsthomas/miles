using System.Diagnostics;
using Core.midi.token;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace Core.ml;

public class Transformer: IDisposable
{
    public const int SequenceLength = 10;
    public const int VocabSize = 16;

    private readonly InferenceSession _session;
    private List<long> _inputSequence = [];
    
    public Transformer(string inputFile)
    {
        _session = new InferenceSession(inputFile);
    }

    public void SetTokens(List<Token> inputs)
    {
        Debug.Assert(inputs.Count >= SequenceLength);

        _inputSequence = inputs
            .TakeLast(SequenceLength)
            .Select(it => (long)it)
            .ToList();
    }

    public Token Generate()
    {
        // Create inputs
        var inputTensor = new DenseTensor<long>(_inputSequence.ToArray(), new[] { 1, SequenceLength });
        var onnxInputs = new NamedOnnxValue[] { NamedOnnxValue.CreateFromTensor("input", inputTensor) };
        
        // Run model
        using var results = _session.Run(onnxInputs);

        // Get model output (logits)
        var logits = results[0].AsTensor<float>();

        // Get next token
        // TODO: LINQ
        var lastLogits = new float[VocabSize];
        for (var i = 0; i < VocabSize; i++)
            lastLogits[i] = logits[0, SequenceLength - 1, i];
        
        var predictedToken = (Token)Array.IndexOf(lastLogits, lastLogits.Max());
        
        // Append to input sequence, return
        Append(predictedToken);
        return predictedToken;
    }

    public void Append(Token token)
    {
        // Append new token to end
        _inputSequence.Add((long)token);

        // Remove first
        _inputSequence.RemoveAt(0);
    }

    public void Dispose()
    {
        _session.Dispose();
    }
}