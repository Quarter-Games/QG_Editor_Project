using Unity.Burst;
using Unity.Entities;
using UnityEngine;

public class AutomatedTowerAuthoring : MonoBehaviour
{
    public float CoolDownSeconds = 1f;
    public ProjectileAuthoring ProjectileAuthoringPrefab;
    class Baker : Baker<AutomatedTowerAuthoring>
    {
        public override void Bake(AutomatedTowerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new AutomatedTower
            {
                CoolDownSeconds = authoring.CoolDownSeconds,
                CoolDownTimer = authoring.CoolDownSeconds,
                ProjectilePrefab = GetEntity(authoring.ProjectileAuthoringPrefab, TransformUsageFlags.Dynamic)
            });
        }

    }
}
public struct AutomatedTower : IComponentData
{
    public float CoolDownSeconds;
    public float CoolDownTimer;
    public Entity ProjectilePrefab;
    public void Shoot()
    {

        Debug.Log("Shoot!");
    }
}
[BurstCompile]
public partial struct AutomatedTowerSystem : ISystem
{

    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;
        foreach (var (tower, entity) in SystemAPI.Query<RefRW<AutomatedTower>>().WithEntityAccess())
        {
            tower.ValueRW.CoolDownTimer -= deltaTime;
            if (tower.ValueRW.CoolDownTimer <= 0f)
            {
                tower.ValueRW.Shoot();
                tower.ValueRW.CoolDownTimer = tower.ValueRW.CoolDownSeconds; // Reset cooldown
            }
        }
    }
}