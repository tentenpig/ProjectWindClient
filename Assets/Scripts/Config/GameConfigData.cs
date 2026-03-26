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
    public CameraConfig camera;
    public TilemapConfig tilemap;
    public string startMapId = "town_01";
    public MapConfig[] maps;
}

[Serializable]
public class MapConfig
{
    public string id = "town_01";
    public string name = "마을";
    public int startX = 10;
    public int startY = 7;
}

[Serializable]
public class PlayerConfig
{
    public float maxHealth = 100f;
    public float moveSpeed = 3f;
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
public class CameraConfig
{
    public float orthographicSize = 8f;
}

[Serializable]
public class TilemapConfig
{
    public int tileSize = 16;
    public int pixelsPerUnit = 16;
}
