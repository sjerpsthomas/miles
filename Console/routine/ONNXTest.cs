using Core.midi.token;
using Core.ml;

namespace Console.routine;

public static class ONNXTest
{
    public static void Run()
    {
        using var transformer = new Transformer("onnx_transformer.onnx");
        
        transformer.SetTokens(TokenMethods.TokensFromString("Mf1564.M23453.12"));

        List<Token> res = [];
        for (int i = 0; i < 10; i++)
        {
            res.Add(transformer.Generate());
        }

        System.Console.WriteLine(TokenMethods.TokensToString(res));
    }
}