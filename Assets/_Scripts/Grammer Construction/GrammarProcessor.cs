using UnityEngine;
using System.Collections.Generic;

// Base for any grammar processor
public class GrammarProcessor : IGrammarProcessor
{
    private List<IGrammarRule> rules = new List<IGrammarRule>();

    public void AddRule(IGrammarRule rule) => rules.Add(rule);

    
    public void ApplyRules(IGrammarTarget target, int iterations = 1)
    {
        for (int i = 0; i < iterations; i++)
        {
            foreach (var rule in rules)
            {
                if (rule.CanApply(target))
                {
                    rule.Apply(target);
                }
            }
        }
    }
}