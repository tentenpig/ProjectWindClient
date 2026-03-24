using System.IO;
using UnityEngine;

/// <summary>
/// GameConfig.json을 로드하고 전역에서 접근할 수 있게 하는 매니저
/// 씬 시작 시 자동 로드 (RuntimeInitializeOnLoadMethod)
/// </summary>
public static class GameConfigManager
{
    private static GameConfigData _config;
    private static bool _loaded;

    public static GameConfigData Config
    {
        get
        {
            if (!_loaded)
                Load();
            return _config;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Load()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "GameConfig.json");

        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            _config = JsonUtility.FromJson<GameConfigData>(json);
            Debug.Log($"[GameConfig] Loaded from {path}");
        }
        else
        {
            Debug.LogWarning($"[GameConfig] File not found: {path}, using defaults");
            _config = new GameConfigData
            {
                player = new PlayerConfig(),
                enemies = new[] { new EnemyConfig() },
                projectile = new ProjectileConfig(),
                spawner = new SpawnerConfig(),
                camera = new CameraConfig()
            };
        }

        _loaded = true;
    }

    /// <summary>
    /// id로 적 설정을 찾는 헬퍼
    /// </summary>
    public static EnemyConfig GetEnemyConfig(string id)
    {
        if (_config?.enemies == null) return new EnemyConfig();

        foreach (var enemy in _config.enemies)
        {
            if (enemy.id == id)
                return enemy;
        }

        return _config.enemies.Length > 0 ? _config.enemies[0] : new EnemyConfig();
    }
}
