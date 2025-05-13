using Core.models.tokens_v1;
using Core.tokens.v1;

namespace Console.routine;

public static class ONNXTest
{
    public static void Run()
    {
        using var transformer = new GenericTransformer("onnx_transformer.onnx");
        
        transformer.SetTokens(V1_TokenMethods.V1_TokensFromString("Mf1564.M23453.12"));

        List<V1_Token> res = [];
        for (int i = 0; i < 10; i++)
        {
            res.Add(transformer.Generate());
        }

        System.Console.WriteLine(V1_TokenMethods.V1_TokensToString(res));
    }
}