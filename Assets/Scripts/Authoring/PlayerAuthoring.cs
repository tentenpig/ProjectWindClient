using Unity.Entities;
using UnityEngine;

/// <summary>
/// 플레이어 엔티티 Authoring 컴포넌트
/// Unity Editor에서 플레이어 프리팹에 이 컴포넌트를 추가하여 설정
/// </summary>
public class PlayerAuthoring : MonoBehaviour
{
    [Header("Stats")]
    public float MaxHealth = 100f;
    public float MoveSpeed = 5f;

    [Header("Auto Attack")]
    public float AttackInterval = 1f;
    public float AttackDamage = 10f;
    public float ProjectileSpeed = 10f;
    public float AttackRange = 8f;
    public GameObject ProjectilePrefab;

    public class Baker : Baker<PlayerAuthoring>
    {
        public override void Bake(PlayerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent<PlayerTag>(entity);
            AddComponent(entity, new PlayerInput { Move = default });
            AddComponent(entity, new Health
            {
                Current = authoring.MaxHealth,
                Max = authoring.MaxHealth
            });
            AddComponent(entity, new MoveSpeed { Value = authoring.MoveSpeed });
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
                Interval = authoring.AttackInterval,
                Damage = authoring.AttackDamage,
                ProjectileSpeed = authoring.ProjectileSpeed,
                Range = authoring.AttackRange,
                ProjectilePrefab = authoring.ProjectilePrefab != null
                    ? GetEntity(authoring.ProjectilePrefab, TransformUsageFlags.Dynamic)
                    : Entity.Null
            });
        }
    }
}
