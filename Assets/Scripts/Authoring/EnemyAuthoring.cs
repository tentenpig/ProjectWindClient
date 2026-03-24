using Unity.Entities;
using UnityEngine;

/// <summary>
/// 적 엔티티 Authoring 컴포넌트
/// 값은 GameConfig.json에서 로드
/// </summary>
public class EnemyAuthoring : MonoBehaviour
{
    public string enemyId = "default";

    public class Baker : Baker<EnemyAuthoring>
    {
        public override void Bake(EnemyAuthoring authoring)
        {
            var cfg = GameConfigManager.GetEnemyConfig(authoring.enemyId);
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent<EnemyTag>(entity);
            AddComponent(entity, new Health
            {
                Current = cfg.maxHealth,
                Max = cfg.maxHealth
            });
            AddComponent(entity, new MoveSpeed { Value = cfg.moveSpeed });
            AddComponent(entity, new MoveDirection { Value = default });
            AddComponent(entity, new ExperienceDrop { Value = cfg.experienceValue });
        }
    }
}
