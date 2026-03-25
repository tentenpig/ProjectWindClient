using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

/// <summary>
/// 체력이 0 이하인 엔티티를 제거하고, 적 사망 시 경험치 처리
/// 플레이어 사망 시 GamePlaying 싱글톤을 제거하여 게임 오버 상태로 전환
/// </summary>
[UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
public partial struct HealthSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GamePlaying>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // 플레이어 사망 체크
        foreach (var (health, entity) in
            SystemAPI.Query<RefRO<Health>>()
                .WithAll<PlayerTag>()
                .WithEntityAccess())
        {
            if (health.ValueRO.Current <= 0f)
            {
                // GamePlaying 싱글톤 제거 → 모든 게임플레이 시스템 정지
                foreach (var (_, gamePlayingEntity) in
                    SystemAPI.Query<RefRO<GamePlaying>>().WithEntityAccess())
                {
                    ecb.DestroyEntity(gamePlayingEntity);
                }
            }
        }

        // 적 사망 처리: 경험치 부여
        int totalExp = 0;
        foreach (var (health, expDrop, entity) in
            SystemAPI.Query<RefRO<Health>, RefRO<ExperienceDrop>>()
                .WithAll<EnemyTag>()
                .WithEntityAccess())
        {
            if (health.ValueRO.Current <= 0f)
            {
                totalExp += expDrop.ValueRO.Value;
                ecb.DestroyEntity(entity);
            }
        }

        // 플레이어에게 경험치 부여
        if (totalExp > 0)
        {
            foreach (var level in SystemAPI.Query<RefRW<PlayerLevel>>().WithAll<PlayerTag>())
            {
                level.ValueRW.CurrentExp += totalExp;

                // 레벨업 체크
                while (level.ValueRO.CurrentExp >= level.ValueRO.ExpToNextLevel)
                {
                    level.ValueRW.CurrentExp -= level.ValueRO.ExpToNextLevel;
                    level.ValueRW.Level++;
                    level.ValueRW.ExpToNextLevel = CalculateExpToNext(level.ValueRO.Level);
                }
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    private static int CalculateExpToNext(int level)
    {
        // 레벨당 필요 경험치: 10 + (레벨 * 5)
        return 10 + level * 5;
    }
}
