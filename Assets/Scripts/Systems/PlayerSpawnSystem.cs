using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// 게임 시작 시 플레이어를 배치하는 시스템
/// 서버 연결 시 S_Login에서 받은 위치를, 오프라인 시 GameConfig의 시작 위치를 사용
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

        int2 startTile;
        // 서버에서 받은 위치가 있으면 사용, 없으면 로컬 설정 사용
        if (NetworkManager.Instance != null && NetworkManager.Instance.IsConnected)
        {
            startTile = NetworkManager.Instance.SpawnPosition;
        }
        else
        {
            var map = GameConfigManager.GetStartMap();
            startTile = new int2(map.startX, map.startY);
        }

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
        _spawned = false;
    }
}
