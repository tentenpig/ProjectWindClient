using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// 타일 단위 이동 시스템
/// - 입력 → 목표 타일 설정 (충돌 체크)
/// - 현재 위치에서 목표 타일까지 보간 이동
/// - 이동 중에는 새 입력 무시
/// - 도착하면 키가 눌려있으면 연속 이동
/// </summary>
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(PlayerInputSystem))]
public partial struct MovementSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GamePlaying>();
    }

    // TilemapCollisionBridge 정적 접근을 위해 BurstCompile 미사용
    public void OnUpdate(ref SystemState state)
    {
        float dt = SystemAPI.Time.DeltaTime;

        foreach (var (transform, tilePos, input, speed) in
            SystemAPI.Query<RefRW<LocalTransform>, RefRW<TilePosition>, RefRO<PlayerInput>, RefRO<MoveSpeed>>()
                .WithAll<PlayerTag>())
        {
            if (tilePos.ValueRO.IsMoving)
            {
                // 이동 중: 보간 진행
                float moveSpeed = speed.ValueRO.Value;
                tilePos.ValueRW.Progress += moveSpeed * dt;

                if (tilePos.ValueRO.Progress >= 1f)
                {
                    // 도착
                    tilePos.ValueRW.Progress = 1f;
                    tilePos.ValueRW.Current = tilePos.ValueRO.Target;
                    tilePos.ValueRW.IsMoving = false;

                    // 타일 중앙에 정확히 위치
                    float3 finalPos = TileToWorld(tilePos.ValueRO.Current);
                    transform.ValueRW.Position = finalPos;

                    // 키가 계속 눌려 있으면 연속 이동 시도
                    TryStartMove(ref tilePos.ValueRW, input.ValueRO.Move);
                }
                else
                {
                    // 보간 중
                    float3 from = TileToWorld(tilePos.ValueRO.Current);
                    float3 to = TileToWorld(tilePos.ValueRO.Target);
                    transform.ValueRW.Position = math.lerp(from, to, tilePos.ValueRO.Progress);
                }
            }
            else
            {
                // 정지 상태: 입력이 있으면 이동 시작
                TryStartMove(ref tilePos.ValueRW, input.ValueRO.Move);
            }
        }
    }

    private static void TryStartMove(ref TilePosition tilePos, float2 input)
    {
        if (input.x == 0f && input.y == 0f)
            return;

        int2 dir = new int2((int)input.x, (int)input.y);
        int2 target = tilePos.Current + dir;

        // 충돌 체크
        if (TilemapCollisionBridge.IsBlocked(target.x, target.y))
            return;

        tilePos.Target = target;
        tilePos.Progress = 0f;
        tilePos.IsMoving = true;
    }

    /// <summary>
    /// 타일 좌표를 월드 좌표(타일 중앙)로 변환
    /// </summary>
    private static float3 TileToWorld(int2 tile)
    {
        return new float3(tile.x + 0.5f, tile.y + 0.5f, 0f);
    }
}
