using UnityEngine;

public abstract class QuestSO : ScriptableObject
{
    public string questId;
    public string title;
    public string description;
    public QuestGoalType goalType;
    public int rewardXP;
    public string rewardItem;
}