using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// 투사체 Authoring 컴포넌트
/// </summary>
public class ProjectileAuthoring : MonoBehaviour
{
    public float DefaultSpeed = 10f;
    public float DefaultDamage = 10f;
    public float DefaultLifetime = 2f;
    public float HitRadius = 0.5f;

    public class Baker : Baker<ProjectileAuthoring>
    {
        public override void Bake(ProjectileAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new Projectile
            {
                Speed = authoring.DefaultSpeed,
                Damage = authoring.DefaultDamage,
                Lifetime = authoring.DefaultLifetime,
                Direction = float2.zero,
                HitRadius = authoring.HitRadius
            });
            AddComponent(entity, new MoveSpeed { Value = authoring.DefaultSpeed });
            AddComponent(entity, new MoveDirection { Value = default });
        }
    }
}
