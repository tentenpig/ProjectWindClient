using Unity.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// GameObject Tilemap과 ECS 시스템을 연결하는 브릿지
/// Collision Tilemap의 타일 데이터를 NativeArray로 변환하여
/// ECS 시스템에서 충돌 판정에 사용할 수 있게 함
///
/// 메인 씬(SubScene 아님)에 배치하고, Inspector에서 Collision Tilemap을 연결
/// </summary>
public class TilemapCollisionBridge : MonoBehaviour
{
    [SerializeField] private Tilemap collisionTilemap;

    // ECS 시스템에서 접근하는 정적 데이터
    public static NativeArray<bool> CollisionGrid;
    public static int GridWidth;
    public static int GridHeight;
    public static int OriginX;
    public static int OriginY;
    public static bool IsReady;

    private void Awake()
    {
        if (CollisionGrid.IsCreated)
            CollisionGrid.Dispose();

        ExtractCollisionData();
    }

    private void ExtractCollisionData()
    {
        if (collisionTilemap == null)
        {
            Debug.LogError("[TilemapCollisionBridge] Collision Tilemap이 연결되지 않았습니다.");
            return;
        }

        collisionTilemap.CompressBounds();
        var bounds = collisionTilemap.cellBounds;

        GridWidth = bounds.size.x;
        GridHeight = bounds.size.y;
        OriginX = bounds.xMin;
        OriginY = bounds.yMin;

        if (GridWidth <= 0 || GridHeight <= 0)
        {
            Debug.LogWarning("[TilemapCollisionBridge] Collision Tilemap이 비어 있습니다.");
            IsReady = false;
            return;
        }

        CollisionGrid = new NativeArray<bool>(GridWidth * GridHeight, Allocator.Persistent);

        for (int y = bounds.yMin; y < bounds.yMax; y++)
        {
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                var tile = collisionTilemap.GetTile(new Vector3Int(x, y, 0));
                int index = (y - OriginY) * GridWidth + (x - OriginX);
                CollisionGrid[index] = (tile != null); // true = 이동 불가
            }
        }

        IsReady = true;
        Debug.Log($"[TilemapCollisionBridge] 충돌 맵 추출 완료: {GridWidth}x{GridHeight}, origin=({OriginX},{OriginY})");
    }

    /// <summary>
    /// 타일 좌표가 충돌 타일인지 확인
    /// </summary>
    public static bool IsBlocked(int tileX, int tileY)
    {
        if (!IsReady) return false;

        int localX = tileX - OriginX;
        int localY = tileY - OriginY;

        if (localX < 0 || localX >= GridWidth || localY < 0 || localY >= GridHeight)
            return true;

        return CollisionGrid[localY * GridWidth + localX];
    }

    /// <summary>
    /// 월드 좌표가 충돌 타일인지 확인
    /// </summary>
    public static bool IsBlockedWorld(float worldX, float worldY)
    {
        return IsBlocked(Mathf.FloorToInt(worldX), Mathf.FloorToInt(worldY));
    }

    private void OnDestroy()
    {
        if (CollisionGrid.IsCreated)
            CollisionGrid.Dispose();
        IsReady = false;
    }
}
