
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
            CurrentHealth = 0;
        }
    }

}