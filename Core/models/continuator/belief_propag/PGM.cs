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
        foreach (var factor in Factors)
            factor.Data = data[factor.Name];
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

        var data = Enumerable.Range(0, (int)factor?.Data!.Array.shape[0]!).Select(_ => 0).ToArray();

        data[valueIdx] = 1;
        factor.Data = new LabeledArray(data, [varName]); // 2D
    }
}

