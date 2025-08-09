using UnityEngine;

// Represents something that can be modified by grammar rules (like a Room or Graph Node)
public interface IGrammarTarget
{
    string TargetType { get; }  // Could be "Room", "GraphNode", etc.
}

// Represents a single grammar rule
public interface IGrammarRule
{
    string Name { get; }
    bool CanApply(IGrammarTarget target);        // Check if rule is valid for the current target
    void Apply(IGrammarTarget target);           // Perform the replacement/transformation
}

// Handles applying rules
public interface IGrammarProcessor
{
    void AddRule(IGrammarRule rule);
    void ApplyRules(IGrammarTarget target, int iterations = 1);
}