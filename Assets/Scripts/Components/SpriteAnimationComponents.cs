using Unity.Entities;

public enum FacingDirection : byte
{
    Down = 0,
    Up = 1,
    Left = 2,
    Right = 3,
}

public enum AnimAction : byte
{
    Idle = 0,
    Walk = 1,
    Attack = 2,
}

/// <summary>
/// 스프라이트 애니메이션 상태 (Player/Enemy 공용)
/// 방향 + 액션 조합으로 재생할 클립이 결정됨
/// </summary>
public struct SpriteAnimation : IComponentData
{
    public FacingDirection Direction;
    public AnimAction Action;
    public float Timer;
    public int FrameIndex;
    public FacingDirection PrevDirection;
    public AnimAction PrevAction;
}

/// <summary>
/// SpriteAnimationClipSet ScriptableObject 참조를 보관하는 매니지드 컴포넌트
/// </summary>
public class ManagedSpriteAnimation : IComponentData
{
    public SpriteAnimationClipSet ClipSet;
}
