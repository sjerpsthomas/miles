using System.Diagnostics;
using NumSharp;
using NumSharp.Generic;
using SkiaSharp;

namespace Core.models.continuator.belief_propag;

public class Messages
{
    public Dictionary<(string, string), NDArray> MyMessages;
    
    public Messages()
    {
        MyMessages = [];
    }

    public static NDArray Prod(NDArray arr)
    {
        var newArr = (double[][])arr.ToJaggedArray<double>();
        
        return np.array(
            Enumerable.Range(0, newArr[0].Length)
                .Select(i => 
                    newArr.Select(it => it[i]).Sum()
                )
        );
    }

    private NDArray _VariableToFactorMessages(Variable variable, Factor factor)
    {
        var incomingMessages = np.stack(variable.Neighbors.OfType<Factor>()
            .Where(neighborFactor => neighborFactor.Name != factor.Name)
            .Select(neighborFactor => FactorToVariableMessage(neighborFactor, variable))
            .ToArray());

        var newIncomingMessages = (double[][])incomingMessages.ToJaggedArray<double>();
        
        // If there are no incoming messages, this is 1
        // return np.prod(incomingMessages, axis: 0);
        return Prod(incomingMessages);
    }

    private NDArray _FactorToVariableMessages(Factor factor, Variable variable)
    {
        // Compute the product
        var factorDist = np.copy(factor.Data!.Array);
        foreach (var neighborVariable in factor.Neighbors.OfType<Variable>())
        {
            if (neighborVariable.Name == variable.Name)
                continue;
            var incomingMessage = VariableToFactorMessages(neighborVariable, factor);

            factorDist *= TileAlong(
                new LabeledArray(incomingMessage, [neighborVariable.Name]),
                factor.Data
            );
        }
        
        // Sum over the axes that aren't `variable`
        var otherAxes = BeliefPropagation.OtherAxesFromLabeledAxes(factor.Data, variable.Name);
        // TODO: should I take [0] from otherAxes? Should I reshape to double?
        // Debug.Assert(otherAxes.Count == 1);
        // var ax = otherAxes.FirstOrDefault(0);

        if (otherAxes is [var ax])
        {
            var newFactorDist = (double[][])factorDist.ToJaggedArray<double>();

            if (ax == 1)
                factorDist = np.array(newFactorDist.Select(it => it.Sum()));
            else if (ax == 0)
                factorDist = np.array(
                    Enumerable.Range(0, newFactorDist[0].Length)
                        .Select(i => 
                            newFactorDist.Select(it => it[i]).Sum()
                        )
                );
        }
        
        return np.squeeze(factorDist);
    }
    
    public List<double> Marginal(Variable variable)
    {
        var unnormP = Prod(
            np.stack(variable.Neighbors.OfType<Factor>()
                .Select(neighborFactor => FactorToVariableMessage(neighborFactor, variable))
                .ToArray()
            )
        );
        
        // At this point, we can normalize this distribution
        var somme = unnormP.ToArray<double>().Sum();
        if (somme == 0)
            throw new Exception("NoSolutionError: marginals are NaN");

        var asdf = unnormP / somme;

        return asdf.Data<double>().ToList();
    }

    public NDArray VariableToFactorMessages(Variable variable, Factor factor)
    {
        var messageName = (variable.Name, factor.Name);
        if (!MyMessages.ContainsKey(messageName))
            MyMessages[messageName] = _VariableToFactorMessages(variable, factor);

        return MyMessages[messageName];
    }

    public NDArray FactorToVariableMessage(Factor factor, Variable variable)
    {
        var messageName = (factor.Name, variable.Name);
        if (!MyMessages.ContainsKey(messageName))
            MyMessages[messageName] = _FactorToVariableMessages(factor, variable);

        return MyMessages[messageName];
    }

    public static NDArray TileAlong(LabeledArray tilingLabeledArray, LabeledArray targetArray)
    {
        var tiled = np.stack(
            Enumerable.Range(0, targetArray.Array.shape[0])
                .Select(_ => tilingLabeledArray.Array)
                .ToArray()
        );

        if (targetArray.AxesLabels[0] == tilingLabeledArray.AxesLabels[0])
            tiled = np.transpose(tiled);

        return tiled;
    }
}