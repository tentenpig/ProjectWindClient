using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// 플레이어 엔티티를 식별하는 태그 컴포넌트
/// </summary>
public struct PlayerTag : IComponentData { }

/// <summary>
/// 플레이어 입력 데이터
/// </summary>
public struct PlayerInput : IComponentData
{
    public float2 Move;
}

/// <summary>
/// 플레이어 레벨 및 경험치
/// </summary>
public struct PlayerLevel : IComponentData
{
    public int Level;
    public int CurrentExp;
    public int ExpToNextLevel;
}
