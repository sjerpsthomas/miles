using System.Diagnostics;
using SkiaSharp;

namespace Core.models.continuator.belief_propag;

public static class BeliefPropagation
{
    public static Dictionary<string, int> NameToAxisMapping(LabeledArray labeledArray) => new(
        labeledArray.AxesLabels
            .Zip(Enumerable.Range(0, labeledArray.AxesLabels.Count))
            .Select(it => new KeyValuePair<string, int>(it.First, it.Second)));

    public static List<int> OtherAxesFromLabeledAxes(LabeledArray labeledArray, string axisLabel) =>
        labeledArray.AxesLabels
            .Zip(Enumerable.Range(0, labeledArray.AxesLabels.Count))
            .Where(it => it.First != axisLabel)
            .Select(it => it.Second)
            .ToList();

    public record ParsedTerm(string term, string varName, List<string> given);

    public static (string, List<string>) ParseTerm(string term)
    {
        Debug.Assert(term[0] == '(' && term[^1] == ')');
        var termVariables = term[1..^1];
        
        // Handle conditionals
        if (!termVariables.Contains('|')) return (termVariables, []);
        
        var split = termVariables.Split('|');

        return (split[0], split[1].Split(',').ToList());
    }

    public static List<ParsedTerm> ParseModelStringIntoTerms(string modelString) =>
        modelString
            .Split('p')
            .Where(term => term != "")
            .Select(term =>
            {
                var (v, given) = ParseTerm(term);
                return new ParsedTerm($"p{term}", v, given);
            })
            .ToList();

    public static (List<Factor>, Dictionary<string, Variable>) ParseModelIntoVariablesAndFactors(string modelString)
    {
        // Takes in a model_string such as p(h1)p(h2∣h1)p(v1∣h1)p(v2∣h2) and returns a
        // dictionary of variable names to variables and a list of factors.
        
        // Split modelString into ParsedTerms
        var parsedTerms = ParseModelStringIntoTerms(modelString);
        
        // First, extract all of the variables from the model_string (h1, h2, v1, v2).
        // These each will be a new Variable that are referenced from Factors below.
        Dictionary<string, Variable> variables = [];
        foreach (var parsedTerm in parsedTerms)
        {
            // If the variable name wasn't seen yet, add it to the variables dict
            if (!variables.ContainsKey(parsedTerm.varName))
                variables[parsedTerm.varName] = new Variable(parsedTerm.varName);
        }
        
        // Now extract factors from the model. Each term (e.g. "p(v1|h1)") corresponds to
        // a factor.
        // Then find all variables in this term ("v1", "h1") and add the corresponding Variables
        // as neighbors to the new Factor, and this Factor to the Variables' neighbors.
        List<Factor> factors = [];
        foreach (var parsedTerm in parsedTerms)
        {
            // This factor will be neighbors with all "variables" (left-hand side variables) and given variables
            var newFactor = new Factor(parsedTerm.term);
            List<string> allVarNames =
            [
                parsedTerm.varName,
                ..parsedTerm.given
            ];

            foreach (var varName in allVarNames)
            {
                newFactor.AddNeighbor(variables[varName]);
                variables[varName].AddNeighbor(newFactor);
            }
            factors.Add(newFactor);
        }

        return (factors, variables);
    }
}