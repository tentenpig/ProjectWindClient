using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// 게임 시작 시 플레이어를 맵 시작 위치(타일 중앙)에 배치하는 시스템
/// GamePlaying 엔티티가 생성된 첫 프레임에만 실행
/// </summary>
[UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
[UpdateBefore(typeof(PlayerInputSystem))]
public partial struct PlayerSpawnSystem : ISystem
{
    private bool _spawned;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GamePlaying>();
        state.RequireForUpdate<PlayerTag>();
        _spawned = false;
    }

    public void OnUpdate(ref SystemState state)
    {
        if (_spawned) return;
        _spawned = true;

        var map = GameConfigManager.GetStartMap();
        int2 startTile = new int2(map.startX, map.startY);
        float3 worldPos = new float3(startTile.x + 0.5f, startTile.y + 0.5f, 0f);

        foreach (var (transform, tilePos) in
            SystemAPI.Query<RefRW<LocalTransform>, RefRW<TilePosition>>()
                .WithAll<PlayerTag>())
        {
            tilePos.ValueRW.Current = startTile;
            tilePos.ValueRW.Target = startTile;
            tilePos.ValueRW.Progress = 1f;
            tilePos.ValueRW.IsMoving = false;
            transform.ValueRW.Position = worldPos;
        }
    }

    public void OnStopRunning(ref SystemState state)
    {
        // GamePlaying이 사라지면 (게임오버/타이틀) 다음 게임에서 다시 스폰하도록 리셋
        _spawned = false;
    }
}
