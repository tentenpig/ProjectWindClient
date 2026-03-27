using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// SpriteAnimation 컴포넌트를 가진 엔티티의 스프라이트를 매 프레임 갱신
/// - MoveDirection으로부터 FacingDirection 자동 결정
/// - TilePosition.IsMoving으로부터 Idle/Walk 자동 전환
/// - Attack은 외부에서 Action을 직접 설정하면 재생 후 Idle로 복귀
/// - SpriteRenderer는 시스템이 동적 생성·관리 (HealthBarUISystem과 동일 패턴)
/// </summary>
[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class SpriteAnimationSystem : SystemBase
{
    private readonly Dictionary<Entity, SpriteRenderer> _renderers = new();
    private readonly List<Entity> _toRemove = new();

    protected override void OnCreate()
    {
        RequireForUpdate<SpriteAnimation>();
    }

    protected override void OnUpdate()
    {
        float dt = SystemAPI.Time.DeltaTime;

        foreach (var (anim, moveDir, managed, localTransform, entity) in
            SystemAPI.Query<RefRW<SpriteAnimation>, RefRO<MoveDirection>,
                           ManagedSpriteAnimation, RefRO<LocalTransform>>()
                .WithEntityAccess())
        {
            UpdateState(ref anim.ValueRW, moveDir.ValueRO.Value, entity);
            AdvanceFrame(ref anim.ValueRW, managed.ClipSet, dt);
            ApplySprite(anim.ValueRO, managed.ClipSet, localTransform.ValueRO, entity);
        }

        CleanupDestroyedEntities();
    }

    private void UpdateState(ref SpriteAnimation anim, float2 moveDir, Entity entity)
    {
        // 방향 결정: MoveDirection이 0이 아니면 갱신
        if (math.lengthsq(moveDir) > 0.001f)
        {
            if (math.abs(moveDir.y) >= math.abs(moveDir.x))
                anim.Direction = moveDir.y > 0 ? FacingDirection.Up : FacingDirection.Down;
            else
                anim.Direction = moveDir.x > 0 ? FacingDirection.Right : FacingDirection.Left;
        }

        // 액션 결정: Attack 중에는 변경하지 않음
        if (anim.Action != AnimAction.Attack)
        {
            bool isMoving;
            if (SystemAPI.HasComponent<TilePosition>(entity))
                isMoving = SystemAPI.GetComponent<TilePosition>(entity).IsMoving;
            else
                isMoving = math.lengthsq(moveDir) > 0.001f;

            anim.Action = isMoving ? AnimAction.Walk : AnimAction.Idle;
        }

        // 클립 변경 감지 → 프레임 리셋
        if (anim.Direction != anim.PrevDirection || anim.Action != anim.PrevAction)
        {
            anim.FrameIndex = 0;
            anim.Timer = 0f;
            anim.PrevDirection = anim.Direction;
            anim.PrevAction = anim.Action;
        }
    }

    private static void AdvanceFrame(ref SpriteAnimation anim, SpriteAnimationClipSet clipSet, float dt)
    {
        if (clipSet == null) return;

        var clip = clipSet.GetClip(anim.Direction, anim.Action);
        if (clip == null || clip.frames.Length == 0) return;

        // 프레임 인덱스가 클립 범위를 벗어나면 리셋
        if (anim.FrameIndex >= clip.frames.Length)
        {
            anim.FrameIndex = 0;
            anim.Timer = 0f;
        }

        anim.Timer += dt;
        if (anim.Timer >= clip.frameDuration)
        {
            anim.Timer -= clip.frameDuration;
            int next = anim.FrameIndex + 1;

            if (next >= clip.frames.Length)
            {
                if (clip.loop)
                {
                    next = 0;
                }
                else
                {
                    next = clip.frames.Length - 1;
                    // 비루프 애니메이션(Attack 등) 종료 → Idle 복귀
                    if (anim.Action == AnimAction.Attack)
                        anim.Action = AnimAction.Idle;
                }
            }

            anim.FrameIndex = next;
        }
    }

    private void ApplySprite(SpriteAnimation anim, SpriteAnimationClipSet clipSet,
        LocalTransform localTransform, Entity entity)
    {
        if (clipSet == null) return;

        var clip = clipSet.GetClip(anim.Direction, anim.Action);
        if (clip == null || clip.frames.Length == 0) return;

        // SpriteRenderer 확보
        if (!_renderers.TryGetValue(entity, out var sr) || sr == null)
        {
            var go = new GameObject($"Sprite_{entity.Index}_{entity.Version}");
            sr = go.AddComponent<SpriteRenderer>();
            _renderers[entity] = sr;
        }

        // 스프라이트 적용
        int frameIdx = math.clamp(anim.FrameIndex, 0, clip.frames.Length - 1);
        sr.sprite = clip.frames[frameIdx];

        // 위치 동기화
        var pos = localTransform.Position;
        sr.transform.position = new Vector3(pos.x, pos.y, pos.z);

        // Y 기준 정렬 (위에 있는 객체가 뒤에 그려짐)
        sr.sortingOrder = -(int)(pos.y * 100);
    }

    private void CleanupDestroyedEntities()
    {
        _toRemove.Clear();
        foreach (var kvp in _renderers)
        {
            if (!EntityManager.Exists(kvp.Key))
            {
                if (kvp.Value != null)
                    Object.Destroy(kvp.Value.gameObject);
                _toRemove.Add(kvp.Key);
            }
        }
        foreach (var e in _toRemove)
            _renderers.Remove(e);
    }

    protected override void OnStopRunning()
    {
        foreach (var kvp in _renderers)
        {
            if (kvp.Value != null)
                Object.Destroy(kvp.Value.gameObject);
        }
        _renderers.Clear();
    }
}
