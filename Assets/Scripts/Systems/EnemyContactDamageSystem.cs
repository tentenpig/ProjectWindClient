using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// 적이 플레이어에게 접촉하면 데미지를 주는 시스템
/// </summary>
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(MovementSystem))]
public partial struct EnemyContactDamageSystem : ISystem
{
    private float _damageTimer;
    private const float DamageInterval = 0.5f;
    private const float ContactRadius = 0.5f;
    private const float ContactDamage = 1f;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PlayerTag>();
        _damageTimer = 0f;
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float dt = SystemAPI.Time.DeltaTime;
        _damageTimer -= dt;
        if (_damageTimer > 0f)
            return;

        _damageTimer = DamageInterval;

        // 플레이어 위치 및 체력
        float3 playerPos = float3.zero;
        RefRW<Health> playerHealth = default;
        bool hasPlayer = false;

        foreach (var (transform, health) in
            SystemAPI.Query<RefRO<LocalTransform>, RefRW<Health>>().WithAll<PlayerTag>())
        {
            playerPos = transform.ValueRO.Position;
            playerHealth = health;
            hasPlayer = true;
            break;
        }

        if (!hasPlayer)
            return;

        float contactRadiusSq = ContactRadius * ContactRadius;

        foreach (var enemyTransform in
            SystemAPI.Query<RefRO<LocalTransform>>().WithAll<EnemyTag>())
        {
            float distSq = math.distancesq(playerPos, enemyTransform.ValueRO.Position);
            if (distSq <= contactRadiusSq)
            {
                playerHealth.ValueRW.Current -= ContactDamage;
            }
        }
    }
}
