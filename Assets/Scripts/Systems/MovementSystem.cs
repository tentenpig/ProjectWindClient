using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// MoveDirection과 MoveSpeed를 기반으로 엔티티를 이동시키는 시스템
/// 플레이어: PlayerInput → MoveDirection 변환 후 이동
/// 적: EnemyAISystem이 MoveDirection을 설정한 뒤 이동
/// </summary>
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(PlayerInputSystem))]
public partial struct MovementSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GamePlaying>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float dt = SystemAPI.Time.DeltaTime;

        // 플레이어: 입력을 이동 방향으로 변환
        foreach (var (input, moveDir) in
            SystemAPI.Query<RefRO<PlayerInput>, RefRW<MoveDirection>>().WithAll<PlayerTag>())
        {
            moveDir.ValueRW.Value = input.ValueRO.Move;
        }

        // 모든 이동 가능한 엔티티 이동
        foreach (var (transform, moveDir, speed) in
            SystemAPI.Query<RefRW<LocalTransform>, RefRO<MoveDirection>, RefRO<MoveSpeed>>())
        {
            float2 dir = moveDir.ValueRO.Value;
            if (math.lengthsq(dir) > 0f)
            {
                float3 delta = new float3(dir.x, dir.y, 0f) * speed.ValueRO.Value * dt;
                transform.ValueRW.Position += delta;
            }
        }
    }
}
