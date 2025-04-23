namespace Core.models.continuator.belief_propag;

public class PGM
{
    public List<Factor> Factors;
    public Dictionary<string, Variable> Variables;
    
    public PGM(List<Factor> factors, Dictionary<string, Variable> variables)
    {
        Factors = factors;
        Variables = variables;
    }
    
    public static PGM FromString(string modelString)
    {
        var (factors, variables) = BeliefPropagation.ParseModelIntoVariablesAndFactors(modelString);
        return new PGM(factors, variables);
    }

    public void SetData(Dictionary<string, LabeledArray> data)
    {
        // Keep track of variable dimensions to check for shape mistakes
        Dictionary<string, int> varDims = [];
        
        foreach (var factor in Factors)
        {
            var factorData = data[factor.Name];

            if (factorData.AxesLabels.Distinct().Count() != factor.Neighbors.Select(it => it.Name).Distinct().Count())
            {
                // TODO: add set difference (not really necessary)

                throw new Exception("ValueError: data is missing axes");
            }

            foreach (var (varName, dim) in factorData.AxesLabels.Zip(factorData.Shape))
            {
                if (!varDims.ContainsKey(varName))
                    varDims[varName] = dim;

                if (varDims[varName] != dim)
                {
                    throw new Exception("ValueError: data axes is wrong size.");
                }
            }

            factor.Data = data[factor.Name];
        }
    }

    public dynamic? VariableFromName(string varName)
    {
        return Variables[varName];
    }

    public Factor? FactorFromName(string facName)
    {
        foreach (var f in Factors)
        {
            if (f.Name == facName)
                return f;
        }

        Console.WriteLine($"Factor not found: {facName}");
        return null;
    }

    public void PrintMarginals()
    {
        foreach (var variable in Variables.Values)
        {
            Console.WriteLine($"Marginal: {variable.Name}: {new Messages().Marginal(variable)}");
        }
    }

    public void SetValue(string varName, int valueIdx)
    {
        var factor = FactorFromName($"p({varName})");

        var data = Enumerable.Range(0, factor?.Data?.Shape[0] ?? throw new Exception("Unknown labeled array")).Select(_ => 0).ToArray();

        data[valueIdx] = 1;
        factor.Data = new LabeledArray1D<int>(data, [varName]);
    }
}

