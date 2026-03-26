using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class SetupTilemapScene
{
    public static string Execute()
    {
        // 1. Grid 생성
        var gridGO = new GameObject("Grid");
        var grid = gridGO.AddComponent<Grid>();
        grid.cellSize = new Vector3(1, 1, 1);

        // 2. Ground Tilemap
        var groundGO = new GameObject("Ground");
        groundGO.transform.SetParent(gridGO.transform);
        var groundTilemap = groundGO.AddComponent<Tilemap>();
        var groundRenderer = groundGO.AddComponent<TilemapRenderer>();
        groundRenderer.sortingOrder = 0;

        // 3. Collision Tilemap
        var collisionGO = new GameObject("Collision");
        collisionGO.transform.SetParent(gridGO.transform);
        var collisionTilemap = collisionGO.AddComponent<Tilemap>();
        var collisionRenderer = collisionGO.AddComponent<TilemapRenderer>();
        collisionRenderer.sortingOrder = 1;

        // 4. 타일 에셋 생성 (프로젝트에 저장)
        if (!AssetDatabase.IsValidFolder("Assets/Tiles"))
            AssetDatabase.CreateFolder("Assets", "Tiles");

        var groundTile = CreateTile("Assets/Tiles/GroundTile.asset", new Color(0.4f, 0.6f, 0.3f));
        var wallTile = CreateTile("Assets/Tiles/WallTile.asset", new Color(0.3f, 0.25f, 0.2f));

        // 5. 테스트 맵 페인팅 (20x15 방, 외벽 + 내부 장애물)
        int mapW = 20, mapH = 15;

        for (int x = 0; x < mapW; x++)
        {
            for (int y = 0; y < mapH; y++)
            {
                var pos = new Vector3Int(x, y, 0);

                // 바닥 전체 칠하기
                groundTilemap.SetTile(pos, groundTile);

                // 외벽
                bool isWall = (x == 0 || x == mapW - 1 || y == 0 || y == mapH - 1);

                // 내부 장애물 (작은 벽 몇 개)
                if (x >= 5 && x <= 7 && y == 5) isWall = true;
                if (x == 12 && y >= 3 && y <= 8) isWall = true;
                if (x >= 14 && x <= 16 && y == 11) isWall = true;

                if (isWall)
                {
                    collisionTilemap.SetTile(pos, wallTile);
                }
            }
        }

        // 6. TilemapCollisionBridge 오브젝트 생성
        var bridgeGO = new GameObject("TilemapCollisionBridge");
        var bridge = bridgeGO.AddComponent<TilemapCollisionBridge>();
        // 리플렉션으로 private serialized 필드에 Collision Tilemap 할당
        var field = typeof(TilemapCollisionBridge).GetField("collisionTilemap",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
            field.SetValue(bridge, collisionTilemap);

        // 7. Main Camera에 CameraFollowSystem 추가
        var cam = Camera.main;
        if (cam != null && cam.GetComponent<CameraFollowSystem>() == null)
        {
            cam.gameObject.AddComponent<CameraFollowSystem>();
        }

        // 8. 카메라를 맵 중앙으로 이동
        if (cam != null)
        {
            cam.transform.position = new Vector3(mapW / 2f, mapH / 2f, -10f);
        }

        EditorUtility.SetDirty(gridGO);
        EditorUtility.SetDirty(bridgeGO);
        if (cam != null) EditorUtility.SetDirty(cam.gameObject);

        return $"타일맵 설정 완료: {mapW}x{mapH} 맵, Ground/Collision 레이어, Bridge 연결, 카메라 설정";
    }

    private static Tile CreateTile(string path, Color color)
    {
        // 이미 존재하면 로드
        var existing = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (existing != null) return existing;

        var tile = ScriptableObject.CreateInstance<Tile>();
        tile.color = color;

        // 기본 흰색 스프라이트 사용
        tile.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        AssetDatabase.CreateAsset(tile, path);
        AssetDatabase.SaveAssets();
        return tile;
    }
}
