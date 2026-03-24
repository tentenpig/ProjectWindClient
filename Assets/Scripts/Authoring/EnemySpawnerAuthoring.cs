using Unity.Entities;
using UnityEngine;

/// <summary>
/// 적 스포너 Authoring 컴포넌트
/// 값은 GameConfig.json에서 로드
/// </summary>
public class EnemySpawnerAuthoring : MonoBehaviour
{
    public GameObject EnemyPrefab;

    public class Baker : Baker<EnemySpawnerAuthoring>
    {
        public override void Bake(EnemySpawnerAuthoring authoring)
        {
            var cfg = GameConfigManager.Config.spawner;
            var entity = GetEntity(TransformUsageFlags.None);

            AddComponent(entity, new EnemySpawner
            {
                Prefab = authoring.EnemyPrefab != null
                    ? GetEntity(authoring.EnemyPrefab, TransformUsageFlags.Dynamic)
                    : Entity.Null,
                SpawnInterval = cfg.spawnInterval,
                SpawnTimer = 0f,
                SpawnRadius = cfg.spawnRadius,
                MaxEnemies = cfg.maxEnemies,
                CurrentWave = 1,
                SpawnPerTick = cfg.spawnPerTick
            });
        }
    }
}
