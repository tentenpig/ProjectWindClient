using Unity.Entities;
using UnityEngine;

/// <summary>
/// 플레이어 엔티티 Authoring 컴포넌트
/// 값은 GameConfig.json에서 로드
/// </summary>
public class PlayerAuthoring : MonoBehaviour
{
    public GameObject ProjectilePrefab;

    public class Baker : Baker<PlayerAuthoring>
    {
        public override void Bake(PlayerAuthoring authoring)
        {
            var cfg = GameConfigManager.Config.player;
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent<PlayerTag>(entity);
            AddComponent(entity, new PlayerInput { Move = default });
            AddComponent(entity, new Health
            {
                Current = cfg.maxHealth,
                Max = cfg.maxHealth
            });
            AddComponent(entity, new MoveSpeed { Value = cfg.moveSpeed });
            AddComponent(entity, new MoveDirection { Value = default });
            AddComponent(entity, new PlayerLevel
            {
                Level = 1,
                CurrentExp = 0,
                ExpToNextLevel = 15
            });
            AddComponent(entity, new AutoAttack
            {
                Timer = 0f,
                Interval = cfg.attackInterval,
                Damage = cfg.attackDamage,
                ProjectileSpeed = cfg.projectileSpeed,
                Range = cfg.attackRange,
                ProjectilePrefab = authoring.ProjectilePrefab != null
                    ? GetEntity(authoring.ProjectilePrefab, TransformUsageFlags.Dynamic)
                    : Entity.Null
            });
        }
    }
}
