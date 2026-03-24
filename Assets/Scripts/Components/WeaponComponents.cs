using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// 자동 공격 무기 (뱀서류 기본 무기)
/// </summary>
public struct AutoAttack : IComponentData
{
    public float Timer;
    public float Interval;
    public float Damage;
    public float ProjectileSpeed;
    public float Range;
    public Entity ProjectilePrefab;
}

/// <summary>
/// 투사체 컴포넌트
/// </summary>
public struct Projectile : IComponentData
{
    public float Speed;
    public float Damage;
    public float Lifetime;
    public float2 Direction;
    public float HitRadius;
}

/// <summary>
/// 투사체가 이미 데미지를 준 대상 추적 (관통 방지용)
/// </summary>
public struct ProjectileHitBuffer : IBufferElementData
{
    public Entity HitEntity;
}
