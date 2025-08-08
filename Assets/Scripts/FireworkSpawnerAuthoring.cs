using Unity.Entities;
using UnityEngine;

public class FireworkSpawnerAuthoring : MonoBehaviour
{
    public GameObject FireworkPrefab;
    
}

public class FireworkSpawnerBaker : Baker<FireworkSpawnerAuthoring>
{
    public override void Bake(FireworkSpawnerAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.None);
        AddComponentObject(entity, new FireworkSpawnerComponent
        {
            Prefab = GetEntity(authoring.FireworkPrefab, TransformUsageFlags.Dynamic)
        });
    }
}

public class FireworkSpawnerComponent : IComponentData
{
    public Entity Prefab;
}
