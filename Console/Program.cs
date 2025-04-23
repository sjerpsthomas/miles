using Console.routine;
using Core.conversion;
using Core.midi.token;
using Core.ml;
using Fastenshtein;


// new LakhTokenizer().Run(@"C:\Users\thoma\Desktop\lmd_full", @"C:\Users\thoma\Desktop\tokens_temp_2", false);




//
List<Token> allTokens = [];
var path = @"C:\Users\thoma\Desktop\tokens_temp";
for (var melody = 1; melody < 457; melody++)
{
    var tokens = TokenMethods.TokensFromString(File.ReadAllText($"{path}/{melody}.tokens"));
    allTokens.AddRange(tokens);
}

var neuralNet = new NeuralNet();
neuralNet.LoadData(allTokens.ToArray());
neuralNet.Train(@"C:\Users\thoma\Desktop\tokens_temp\neural_net");

System.Console.WriteLine("Done!");