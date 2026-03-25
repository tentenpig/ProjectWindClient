using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// 투사체 수명 관리 및 적과의 충돌 감지
/// </summary>
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(MovementSystem))]
public partial struct ProjectileSystem : ISystem
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
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (projectile, transform, entity) in
            SystemAPI.Query<RefRW<Projectile>, RefRO<LocalTransform>>()
                .WithEntityAccess())
        {
            // 수명 감소
            projectile.ValueRW.Lifetime -= dt;
            if (projectile.ValueRO.Lifetime <= 0f)
            {
                ecb.DestroyEntity(entity);
                continue;
            }

            // 적과 충돌 감지 (간단한 거리 기반)
            float3 projPos = transform.ValueRO.Position;
            float hitRadiusSq = projectile.ValueRO.HitRadius * projectile.ValueRO.HitRadius;

            foreach (var (enemyTransform, health, enemyEntity) in
                SystemAPI.Query<RefRO<LocalTransform>, RefRW<Health>>()
                    .WithAll<EnemyTag>()
                    .WithEntityAccess())
            {
                float distSq = math.distancesq(projPos, enemyTransform.ValueRO.Position);
                if (distSq <= hitRadiusSq)
                {
                    health.ValueRW.Current -= projectile.ValueRO.Damage;
                    ecb.DestroyEntity(entity);
                    break;
                }
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
