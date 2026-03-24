using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// 적이 플레이어를 추적하도록 MoveDirection을 설정하는 시스템
/// </summary>
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(MovementSystem))]
public partial struct EnemyAISystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PlayerTag>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // 플레이어 위치 가져오기
        float3 playerPos = float3.zero;
        foreach (var transform in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<PlayerTag>())
        {
            playerPos = transform.ValueRO.Position;
            break;
        }

        // 모든 적의 이동 방향을 플레이어 쪽으로 설정
        foreach (var (transform, moveDir) in
            SystemAPI.Query<RefRO<LocalTransform>, RefRW<MoveDirection>>().WithAll<EnemyTag>())
        {
            float3 toPlayer = playerPos - transform.ValueRO.Position;
            float2 dir2D = new float2(toPlayer.x, toPlayer.y);

            if (math.lengthsq(dir2D) > 0.01f)
                dir2D = math.normalize(dir2D);

            moveDir.ValueRW.Value = dir2D;
        }
    }
}
