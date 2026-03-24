using Unity.Entities;

/// <summary>
/// 적 스포너 싱글톤 컴포넌트
/// </summary>
public struct EnemySpawner : IComponentData
{
    public Entity Prefab;
    public float SpawnInterval;
    public float SpawnTimer;
    public float SpawnRadius;
    public int MaxEnemies;
    public int CurrentWave;
    public int SpawnPerTick;
}
