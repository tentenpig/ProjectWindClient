using Unity.Entities;
using UnityEngine;

/// <summary>
/// 적 엔티티 Authoring 컴포넌트
/// </summary>
public class EnemyAuthoring : MonoBehaviour
{
    [Header("Stats")]
    public float MaxHealth = 20f;
    public float MoveSpeed = 2f;
    public int ExperienceValue = 5;

    public class Baker : Baker<EnemyAuthoring>
    {
        public override void Bake(EnemyAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent<EnemyTag>(entity);
            AddComponent(entity, new Health
            {
                Current = authoring.MaxHealth,
                Max = authoring.MaxHealth
            });
            AddComponent(entity, new MoveSpeed { Value = authoring.MoveSpeed });
            AddComponent(entity, new MoveDirection { Value = default });
            AddComponent(entity, new ExperienceDrop { Value = authoring.ExperienceValue });
        }
    }
}
