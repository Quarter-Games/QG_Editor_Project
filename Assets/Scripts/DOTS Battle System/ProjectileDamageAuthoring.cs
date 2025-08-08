using Unity.Burst;
using Unity.Entities;
using UnityEngine;

public class ProjectileDamageAuthoring : MonoBehaviour
{
    public float Damage = 10f;
    class Baker : Baker<ProjectileDamageAuthoring>
    {
        public override void Bake(ProjectileDamageAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new ProjectileDamage
            {
                Damage = authoring.Damage
            });
        }
    }
}
public struct ProjectileDamage : IComponentData
{
    public float Damage;
}
[BurstCompile]
public partial struct ProjectileDamageSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (damage, entity) in SystemAPI.Query<RefRW<ProjectileDamage>>().WithEntityAccess())
        {

        }
    }
}