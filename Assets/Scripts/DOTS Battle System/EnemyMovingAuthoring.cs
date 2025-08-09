using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class EnemyMovingAuthoring : MonoBehaviour
{
    public float MoveSpeed = 5f;

    class Baker : Baker<EnemyMovingAuthoring>
    {
        public override void Bake(EnemyMovingAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new EnemyMoving
            {
                MoveSpeed = authoring.MoveSpeed
            });
        }
    }
}

public struct EnemyMoving : IComponentData
{
    public float MoveSpeed;
}

[BurstCompile]
public partial struct EnemyMovingSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        float3 targetPosition = float3.zero;

        foreach (var(moving, transform) in SystemAPI.Query<RefRO<EnemyMoving>, RefRW<LocalTransform>>())
        {
            float3 direction = targetPosition - transform.ValueRO.Position;
            float distance = math.length(direction);

            if (distance > 0.01f)
            {
                float3 moveDir = math.normalize(direction);
                transform.ValueRW.Position += moveDir * moving.ValueRO.MoveSpeed * deltaTime;
            }
        }
    }
}
