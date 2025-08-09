using UnityEngine;

public class ObjectPlacementRule : IGrammarRule
{
    private GameObject treasurePrefab;
    private RoomInfo[] allRooms;

    public ObjectPlacementRule(GameObject treasurePrefab, RoomInfo[] rooms)
    {
        this.treasurePrefab = treasurePrefab;
        this.allRooms = rooms;
    }

    public bool TryApplyRule()
    {
        bool applied = false;

        foreach (var room in allRooms)
        {
            // Only decorate TreasureRooms in this example
            if (room.Type != RoomType.Treasure || room.HasObjects)
                continue;

            // Example: Place treasure near room center with some random offset
            Vector3 placementPos = room.Center + new Vector3(
                Random.Range(-1f, 1f),
                0,
                Random.Range(-1f, 1f)
            );

            GameObject.Instantiate(treasurePrefab, placementPos, Quaternion.identity);
            room.HasObjects = true;
            applied = true;
        }

        return applied;
    }

    public string Name { get; }
    public bool CanApply(IGrammarTarget target)
    {
        throw new System.NotImplementedException();
    }

    public void Apply(IGrammarTarget target)
    {
        throw new System.NotImplementedException();
    }
    
}