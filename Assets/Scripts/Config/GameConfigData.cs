using System;

/// <summary>
/// JSON 설정 파일의 루트 구조
/// StreamingAssets/GameConfig.json과 1:1 매핑
/// </summary>
[Serializable]
public class GameConfigData
{
    public PlayerConfig player;
    public EnemyConfig[] enemies;
    public ProjectileConfig projectile;
    public SpawnerConfig spawner;
    public CameraConfig camera;
}

[Serializable]
public class PlayerConfig
{
    public float maxHealth = 100f;
    public float moveSpeed = 5f;
    public float attackInterval = 0.01f;
    public float attackDamage = 10f;
    public float projectileSpeed = 10f;
    public float attackRange = 32f;
}

[Serializable]
public class EnemyConfig
{
    public string id = "default";
    public float maxHealth = 20f;
    public float moveSpeed = 2f;
    public int experienceValue = 5;
}

[Serializable]
public class ProjectileConfig
{
    public float speed = 10f;
    public float damage = 10f;
    public float lifetime = 2f;
    public float hitRadius = 0.5f;
}

[Serializable]
public class SpawnerConfig
{
    public float spawnInterval = 0.1f;
    public float spawnRadius = 15f;
    public int maxEnemies = 5000;
    public int spawnPerTick = 50;
}

[Serializable]
public class CameraConfig
{
    public float orthographicSize = 16.7f;
}
