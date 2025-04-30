using System.Diagnostics;
using Console.routine;
using Core.models.continuator;


// new LakhTokenizer().Run(@"C:\Users\thoma\Desktop\lmd_full", @"C:\Users\thoma\Desktop\tokens_temp_2", false);




//
// List<Token> allTokens = [];
// var path = @"C:\Users\thoma\Desktop\tokens_temp";
// for (var melody = 1; melody < 457; melody++)
// {
//     var tokens = TokenMethods.TokensFromString(File.ReadAllText($"{path}/{melody}.tokens"));
//     allTokens.AddRange(tokens);
// }
//
// var neuralNet = new NeuralNet();
// neuralNet.LoadData(allTokens.ToArray());
// neuralNet.Train(@"C:\Users\thoma\Desktop\tokens_temp\neural_net");
//
// System.Console.WriteLine("Done!");

new WJDToNotes().RunAllStandards(@"C:\Users\thoma\Desktop\new_extra");


// var recherche = File.ReadAllText(@"C:\Users\thoma\Desktop\proust_debut.txt").TrimEnd();
// var items = recherche.ToCharArray().ToList();
//
// var vo = new VariableOrderMarkov<char>(it => (int)it, 3);
//
//
// var stopWatch = new Stopwatch();
// stopWatch.Start();
//
// for (var i = 0; i < 1000; i++)
// {
//     vo.LearnSequence(items);
//     // System.Console.WriteLine(vo);
//     var seq = vo.Generate(140);
//     var result = new string(seq.ToArray());
//     System.Console.WriteLine(result);
// }
//
// stopWatch.Stop();
// System.Console.WriteLine(stopWatch.ElapsedMilliseconds / 1000.0);


// with open('../data/proust_debut.txt', 'r') as file:
// recherche = file.read().rstrip()
// char_seq = list(recherche)
// vo = Variable_order_Markov(char_seq, None, 3)
// seq = vo.sample_sequence(140, constraints={0: vo.get_viewpoint('.'), 139: vo.get_viewpoint('.')})
// result = ''.join(seq)
// # Removes spaces before punctuation
// result = re.sub(r"\s([?.!,:;”])", r"\1", result)
// print(result)  # Removes spaces before punctuation