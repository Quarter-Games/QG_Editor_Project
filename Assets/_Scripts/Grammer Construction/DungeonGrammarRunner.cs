using UnityEngine;

public class DungeonGrammarRunner : MonoBehaviour
{
    public ShapeConnector PatternShape;
    public ShapeConnector ReplacementPrefab;
    public IGrammarTarget TargetObject;

    private GrammarProcessor processor;

    void Start()
    {
        processor = new GrammarProcessor();

        // Add the ProBuilder shape replacement rule
        var rule = new ShapeReplacementRule(PatternShape, ReplacementPrefab);
        processor.AddRule(rule);

        // Run grammar once (or call this later when layout is built)
        processor.ApplyRules(TargetObject,1);
    }
}