using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// 투사체 Authoring 컴포넌트
/// 값은 GameConfig.json에서 로드
/// </summary>
public class ProjectileAuthoring : MonoBehaviour
{
    public class Baker : Baker<ProjectileAuthoring>
    {
        public override void Bake(ProjectileAuthoring authoring)
        {
            var cfg = GameConfigManager.Config.projectile;
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new Projectile
            {
                Speed = cfg.speed,
                Damage = cfg.damage,
                Lifetime = cfg.lifetime,
                Direction = float2.zero,
                HitRadius = cfg.hitRadius
            });
            AddComponent(entity, new MoveSpeed { Value = cfg.speed });
            AddComponent(entity, new MoveDirection { Value = default });
        }
    }
}
