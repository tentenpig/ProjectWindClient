using Unity.Entities;
using UnityEngine;

/// <summary>
/// 적 스포너 Authoring 컴포넌트
/// 빈 GameObject에 추가하여 사용
/// </summary>
public class EnemySpawnerAuthoring : MonoBehaviour
{
    public GameObject EnemyPrefab;
    public float SpawnInterval = 2f;
    public float SpawnRadius = 10f;
    public int MaxEnemies = 50;

    public class Baker : Baker<EnemySpawnerAuthoring>
    {
        public override void Bake(EnemySpawnerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);

            AddComponent(entity, new EnemySpawner
            {
                Prefab = authoring.EnemyPrefab != null
                    ? GetEntity(authoring.EnemyPrefab, TransformUsageFlags.Dynamic)
                    : Entity.Null,
                SpawnInterval = authoring.SpawnInterval,
                SpawnTimer = 0f,
                SpawnRadius = authoring.SpawnRadius,
                MaxEnemies = authoring.MaxEnemies,
                CurrentWave = 1
            });
        }
    }
}
