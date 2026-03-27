using Unity.Entities;
using UnityEngine;

/// <summary>
/// Player/Enemy authoring GameObject에 추가하여 스프라이트 애니메이션을 부여
/// ClipSet만 지정하면 SpriteAnimationSystem이 자동으로 렌더링 처리
/// </summary>
public class SpriteAnimationAuthoring : MonoBehaviour
{
    public SpriteAnimationClipSet clipSet;
    public FacingDirection initialDirection = FacingDirection.Down;

    class Baker : Baker<SpriteAnimationAuthoring>
    {
        public override void Bake(SpriteAnimationAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new SpriteAnimation
            {
                Direction = authoring.initialDirection,
                Action = AnimAction.Idle,
                PrevDirection = authoring.initialDirection,
                PrevAction = AnimAction.Idle,
            });

            AddComponentObject(entity, new ManagedSpriteAnimation
            {
                ClipSet = authoring.clipSet,
            });
        }
    }
}
