using UnityEngine;

[CreateAssetMenu(fileName = "NewKillQuest", menuName = "Quests/Kill Quest")]
public class KillQuestSO : QuestSO
{
    public string enemyType;
    public int amount;
}