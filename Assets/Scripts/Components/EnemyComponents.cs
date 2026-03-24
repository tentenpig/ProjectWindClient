using Unity.Entities;

/// <summary>
/// 적 엔티티를 식별하는 태그 컴포넌트
/// </summary>
public struct EnemyTag : IComponentData { }

/// <summary>
/// 적 처치 시 드롭할 경험치량
/// </summary>
public struct ExperienceDrop : IComponentData
{
    public int Value;
}
