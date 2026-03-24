using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// 체력 컴포넌트 (플레이어, 적 공용)
/// </summary>
public struct Health : IComponentData
{
    public float Current;
    public float Max;
}

/// <summary>
/// 이동 속도
/// </summary>
public struct MoveSpeed : IComponentData
{
    public float Value;
}

/// <summary>
/// 이동 방향 (정규화된 벡터)
/// </summary>
public struct MoveDirection : IComponentData
{
    public float2 Value;
}

/// <summary>
/// 데미지를 입은 대상에게 부여하는 컴포넌트
/// </summary>
public struct DamageEvent : IBufferElementData
{
    public float Value;
    public Entity Source;
}
