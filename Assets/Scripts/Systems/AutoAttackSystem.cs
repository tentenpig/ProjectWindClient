using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// 플레이어의 자동 공격 시스템: 주기적으로 가장 가까운 적을 향해 투사체 발사
/// </summary>
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct AutoAttackSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PlayerTag>();
        state.RequireForUpdate<GamePlaying>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float dt = SystemAPI.Time.DeltaTime;
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (attack, playerTransform) in
            SystemAPI.Query<RefRW<AutoAttack>, RefRO<LocalTransform>>().WithAll<PlayerTag>())
        {
            attack.ValueRW.Timer -= dt;
            if (attack.ValueRO.Timer > 0f)
                continue;

            attack.ValueRW.Timer = attack.ValueRO.Interval;

            // 가장 가까운 적 찾기
            float3 playerPos = playerTransform.ValueRO.Position;
            float closestDistSq = attack.ValueRO.Range * attack.ValueRO.Range;
            float3 closestEnemyPos = float3.zero;
            bool foundEnemy = false;

            foreach (var enemyTransform in
                SystemAPI.Query<RefRO<LocalTransform>>().WithAll<EnemyTag>())
            {
                float distSq = math.distancesq(playerPos, enemyTransform.ValueRO.Position);
                if (distSq < closestDistSq)
                {
                    closestDistSq = distSq;
                    closestEnemyPos = enemyTransform.ValueRO.Position;
                    foundEnemy = true;
                }
            }

            if (!foundEnemy)
                continue;

            // 투사체 생성
            if (attack.ValueRO.ProjectilePrefab == Entity.Null)
                continue;

            float3 toEnemy = closestEnemyPos - playerPos;
            float2 dir = math.normalize(new float2(toEnemy.x, toEnemy.y));

            Entity projectile = ecb.Instantiate(attack.ValueRO.ProjectilePrefab);
            ecb.SetComponent(projectile, LocalTransform.FromPosition(playerPos));
            ecb.SetComponent(projectile, new Projectile
            {
                Speed = attack.ValueRO.ProjectileSpeed,
                Damage = attack.ValueRO.Damage,
                Lifetime = attack.ValueRO.Range / attack.ValueRO.ProjectileSpeed,
                Direction = dir,
                HitRadius = 0.5f
            });
            ecb.SetComponent(projectile, new MoveDirection { Value = dir });
            ecb.SetComponent(projectile, new MoveSpeed { Value = attack.ValueRO.ProjectileSpeed });
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
