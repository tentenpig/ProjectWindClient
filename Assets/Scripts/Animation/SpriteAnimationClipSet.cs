using System;
using UnityEngine;

/// <summary>
/// 캐릭터의 모든 스프라이트 애니메이션 클립을 정의하는 ScriptableObject
/// 4방향 × (Idle/Walk/Attack) = 12개 클립
/// Player와 Enemy가 각각 다른 에셋을 사용하되 같은 구조를 공유
/// </summary>
[CreateAssetMenu(fileName = "NewSpriteAnimClipSet", menuName = "Game/Sprite Animation Clip Set")]
public class SpriteAnimationClipSet : ScriptableObject
{
    public DirectionClips down;
    public DirectionClips up;
    public DirectionClips left;
    public DirectionClips right;

    [Serializable]
    public class DirectionClips
    {
        public AnimClip idle;
        public AnimClip walk;
        public AnimClip attack;
    }

    [Serializable]
    public class AnimClip
    {
        public Sprite[] frames = Array.Empty<Sprite>();
        public float frameDuration = 0.15f;
        public bool loop = true;
    }

    public AnimClip GetClip(FacingDirection direction, AnimAction action)
    {
        var dirClips = direction switch
        {
            FacingDirection.Up => up,
            FacingDirection.Left => left,
            FacingDirection.Right => right,
            _ => down,
        };

        return action switch
        {
            AnimAction.Walk => dirClips.walk,
            AnimAction.Attack => dirClips.attack,
            _ => dirClips.idle,
        };
    }
}
