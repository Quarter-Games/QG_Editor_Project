
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

internal class EnemyHealthAuthoring : MonoBehaviour
{
    public int maxHealth = 100;
    class Baker : Baker<EnemyHealthAuthoring>
    {
        public override void Bake(EnemyHealthAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new EnemyHealth { MaxHealth = authoring.maxHealth, CurrentHealth = authoring.maxHealth });
        }
    }
}
public struct EnemyHealth : IComponentData
{
    public int MaxHealth;
    public int CurrentHealth;
    public void TakeDamage(int damage)
    {
        CurrentHealth -= damage;
        if (CurrentHealth < 0)
        {
            CurrentHealth = 0; // Ensure health doesn't go below zero
        }
    }

}
[BurstCompile]
public partial struct EnemyHealthSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        // Initialization logic if needed
    }
    public void OnUpdate(ref SystemState state)
    {
        // Update logic for enemy health, if needed
        // This could include checking for damage, healing, etc.
    }
}