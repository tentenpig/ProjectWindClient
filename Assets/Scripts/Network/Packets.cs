using System;

/// <summary>
/// 2D 정수 좌표 (서버 Shared.Models.Vec2Int와 동일)
/// </summary>
[Serializable]
public struct Vec2Int
{
    public int X;
    public int Y;

    public Vec2Int(int x, int y) { X = x; Y = y; }
}

// ─────────────────────────────────────
// 클라이언트 → 서버
// ─────────────────────────────────────

[Serializable]
public class C_Login
{
    public string AccountName = "";
}

[Serializable]
public class C_Move
{
    public Vec2Int Position;
    public Vec2Int Direction;
}

// ─────────────────────────────────────
// 서버 → 클라이언트
// ─────────────────────────────────────

[Serializable]
public class S_Login
{
    public bool Success;
    public int PlayerId;
    public string MapId = "";
    public Vec2Int Position;
}

[Serializable]
public class S_Move
{
    public int PlayerId;
    public Vec2Int Position;
    public Vec2Int Direction;
}

[Serializable]
public class S_Spawn
{
    public int PlayerId;
    public string Name = "";
    public string MapId = "";
    public Vec2Int Position;
}

[Serializable]
public class S_Despawn
{
    public int PlayerId;
}
