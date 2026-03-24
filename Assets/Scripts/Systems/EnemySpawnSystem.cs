using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// 플레이어 주변에 적을 주기적으로 스폰하는 시스템
/// </summary>
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct EnemySpawnSystem : ISystem
{
    private Random _random;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<EnemySpawner>();
        state.RequireForUpdate<PlayerTag>();
        _random = Random.CreateFromIndex(42);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float dt = SystemAPI.Time.DeltaTime;

        // 현재 적 수 카운트
        int enemyCount = 0;
        foreach (var _ in SystemAPI.Query<RefRO<EnemyTag>>())
        {
            enemyCount++;
        }

        // 플레이어 위치
        float3 playerPos = float3.zero;
        foreach (var transform in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<PlayerTag>())
        {
            playerPos = transform.ValueRO.Position;
            break;
        }

        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var spawner in SystemAPI.Query<RefRW<EnemySpawner>>())
        {
            if (spawner.ValueRO.Prefab == Entity.Null)
                continue;

            spawner.ValueRW.SpawnTimer -= dt;
            if (spawner.ValueRO.SpawnTimer > 0f)
                continue;

            spawner.ValueRW.SpawnTimer = spawner.ValueRO.SpawnInterval;

            float radius = spawner.ValueRO.SpawnRadius;
            int max = spawner.ValueRO.MaxEnemies;
            int count = spawner.ValueRO.SpawnPerTick;

            // SpawnPerTick만큼 한번에 스폰
            for (int i = 0; i < count && enemyCount < max; i++)
            {
                float angle = _random.NextFloat(0f, math.PI * 2f);
                float3 spawnPos = playerPos + new float3(
                    math.cos(angle) * radius,
                    math.sin(angle) * radius,
                    0f
                );

                Entity enemy = ecb.Instantiate(spawner.ValueRO.Prefab);
                ecb.SetComponent(enemy, LocalTransform.FromPosition(spawnPos));
                enemyCount++;
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
