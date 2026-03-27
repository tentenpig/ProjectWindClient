using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 스프라이트 시트에서 SpriteAnimationClipSet을 자동 생성하는 에디터 도구
///
/// 사용법:
///   1. Tools > Sprite Animation Setup 열기
///   2. 슬라이스된 스프라이트 시트 텍스처를 드래그
///   3. 행(Row)별 방향, 액션별 프레임 수 설정
///   4. Generate 클릭 → ClipSet SO 자동 생성
///
/// 스프라이트 시트 레이아웃 예시 (기본값):
///   Row 0: Down   [Idle 1f][Walk 4f][Attack 4f]
///   Row 1: Up     [Idle 1f][Walk 4f][Attack 4f]
///   Row 2: Left   [Idle 1f][Walk 4f][Attack 4f]
///   Row 3: Right  [Idle 1f][Walk 4f][Attack 4f]
/// </summary>
public class SpriteAnimationSetupWindow : EditorWindow
{
    private Texture2D _spriteSheet;
    private string _assetName = "NewSpriteAnimClipSet";

    // 행별 방향 매핑
    private FacingDirection _row0 = FacingDirection.Down;
    private FacingDirection _row1 = FacingDirection.Up;
    private FacingDirection _row2 = FacingDirection.Left;
    private FacingDirection _row3 = FacingDirection.Right;

    // 액션별 프레임 수
    private int _idleFrames = 1;
    private int _walkFrames = 4;
    private int _attackFrames = 4;

    // 액션별 프레임 간격 (초)
    private float _idleDuration = 0.4f;
    private float _walkDuration = 0.15f;
    private float _attackDuration = 0.1f;

    // 미리보기
    private Sprite[] _allSprites;
    private int _detectedColumns;
    private int _detectedRows;

    [MenuItem("Tools/Sprite Animation Setup")]
    public static void ShowWindow()
    {
        GetWindow<SpriteAnimationSetupWindow>("Sprite Anim Setup");
    }

    private void OnGUI()
    {
        GUILayout.Label("Sprite Animation Clip Set Generator", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // 스프라이트 시트
        EditorGUI.BeginChangeCheck();
        _spriteSheet = (Texture2D)EditorGUILayout.ObjectField(
            "Sprite Sheet", _spriteSheet, typeof(Texture2D), false);
        if (EditorGUI.EndChangeCheck() && _spriteSheet != null)
            AnalyzeSpriteSheet();

        if (_spriteSheet == null)
        {
            EditorGUILayout.HelpBox(
                "슬라이스된 스프라이트 시트 텍스처를 지정하세요.\n" +
                "(Sprite Mode: Multiple, Sprite Editor에서 슬라이스 완료된 상태)",
                MessageType.Info);
            return;
        }

        // 감지 결과
        if (_allSprites != null && _allSprites.Length > 0)
        {
            EditorGUILayout.HelpBox(
                $"감지: {_allSprites.Length}개 스프라이트, {_detectedColumns}열 × {_detectedRows}행",
                MessageType.None);
        }
        else
        {
            EditorGUILayout.HelpBox(
                "스프라이트를 찾을 수 없습니다. Sprite Mode를 Multiple로 설정하고 슬라이스하세요.",
                MessageType.Warning);
            return;
        }

        EditorGUILayout.Space();
        _assetName = EditorGUILayout.TextField("Asset Name", _assetName);

        // 행별 방향
        EditorGUILayout.Space();
        GUILayout.Label("Row → Direction", EditorStyles.boldLabel);
        _row0 = (FacingDirection)EditorGUILayout.EnumPopup("Row 0", _row0);
        _row1 = (FacingDirection)EditorGUILayout.EnumPopup("Row 1", _row1);
        _row2 = (FacingDirection)EditorGUILayout.EnumPopup("Row 2", _row2);
        _row3 = (FacingDirection)EditorGUILayout.EnumPopup("Row 3", _row3);

        // 액션별 프레임 수
        EditorGUILayout.Space();
        GUILayout.Label("Frames per Action (한 행 내 좌→우 순서)", EditorStyles.boldLabel);

        _idleFrames = EditorGUILayout.IntField("Idle Frames", _idleFrames);
        _walkFrames = EditorGUILayout.IntField("Walk Frames", _walkFrames);
        _attackFrames = EditorGUILayout.IntField("Attack Frames", _attackFrames);

        int totalPerRow = _idleFrames + _walkFrames + _attackFrames;
        if (totalPerRow != _detectedColumns)
        {
            EditorGUILayout.HelpBox(
                $"프레임 합계({totalPerRow})가 열 수({_detectedColumns})와 다릅니다.",
                MessageType.Warning);
        }

        // 프레임 간격
        EditorGUILayout.Space();
        GUILayout.Label("Frame Duration (sec)", EditorStyles.boldLabel);
        _idleDuration = EditorGUILayout.FloatField("Idle", _idleDuration);
        _walkDuration = EditorGUILayout.FloatField("Walk", _walkDuration);
        _attackDuration = EditorGUILayout.FloatField("Attack", _attackDuration);

        // 생성
        EditorGUILayout.Space();
        GUI.enabled = _allSprites != null && _allSprites.Length > 0;
        if (GUILayout.Button("Generate ClipSet", GUILayout.Height(30)))
            Generate();
        GUI.enabled = true;
    }

    private void AnalyzeSpriteSheet()
    {
        var path = AssetDatabase.GetAssetPath(_spriteSheet);
        _allSprites = AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<Sprite>()
            .OrderBy(s => s.name, new NaturalStringComparer())
            .ToArray();

        if (_allSprites.Length == 0) return;

        // Y 좌표 기준으로 행 수 감지 (스프라이트 시트는 위→아래)
        var distinctYs = _allSprites.Select(s => s.rect.y).Distinct().Count();
        _detectedRows = distinctYs;
        _detectedColumns = _allSprites.Length / Mathf.Max(1, _detectedRows);
    }

    private void Generate()
    {
        var clipSet = ScriptableObject.CreateInstance<SpriteAnimationClipSet>();

        clipSet.down = new SpriteAnimationClipSet.DirectionClips();
        clipSet.up = new SpriteAnimationClipSet.DirectionClips();
        clipSet.left = new SpriteAnimationClipSet.DirectionClips();
        clipSet.right = new SpriteAnimationClipSet.DirectionClips();

        // 행별로 스프라이트 분류 (Y좌표 내림차순 = 시트 위→아래 = Row 0, 1, 2, ...)
        var rows = _allSprites
            .GroupBy(s => s.rect.y)
            .OrderByDescending(g => g.Key) // 위쪽(Y 큰 값)이 Row 0
            .Select(g => g.OrderBy(s => s.rect.x).ToArray())
            .ToArray();

        var directionMap = new[]
        {
            _row0, _row1, _row2, _row3
        };

        for (int r = 0; r < Mathf.Min(rows.Length, 4); r++)
        {
            var rowSprites = rows[r];
            var dir = directionMap[r];
            var dirClips = GetDirectionClips(clipSet, dir);

            int col = 0;
            dirClips.idle = ExtractClip(rowSprites, ref col, _idleFrames, _idleDuration, true);
            dirClips.walk = ExtractClip(rowSprites, ref col, _walkFrames, _walkDuration, true);
            dirClips.attack = ExtractClip(rowSprites, ref col, _attackFrames, _attackDuration, false);
        }

        // 에셋 저장
        var sheetPath = AssetDatabase.GetAssetPath(_spriteSheet);
        var dir2 = Path.GetDirectoryName(sheetPath);
        var savePath = Path.Combine(dir2, _assetName + ".asset").Replace("\\", "/");
        savePath = AssetDatabase.GenerateUniqueAssetPath(savePath);

        AssetDatabase.CreateAsset(clipSet, savePath);
        AssetDatabase.SaveAssets();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = clipSet;
        Debug.Log($"[SpriteAnimSetup] 생성 완료: {savePath}");
    }

    private static SpriteAnimationClipSet.AnimClip ExtractClip(
        Sprite[] rowSprites, ref int col, int frameCount, float duration, bool loop)
    {
        var clip = new SpriteAnimationClipSet.AnimClip
        {
            frameDuration = duration,
            loop = loop,
        };

        var frames = new List<Sprite>();
        for (int i = 0; i < frameCount && col < rowSprites.Length; i++, col++)
            frames.Add(rowSprites[col]);

        clip.frames = frames.ToArray();
        return clip;
    }

    private static SpriteAnimationClipSet.DirectionClips GetDirectionClips(
        SpriteAnimationClipSet set, FacingDirection dir)
    {
        return dir switch
        {
            FacingDirection.Up => set.up,
            FacingDirection.Left => set.left,
            FacingDirection.Right => set.right,
            _ => set.down,
        };
    }

    /// <summary>
    /// 자연 정렬: sprite_0, sprite_1, ... sprite_9, sprite_10 순서 보장
    /// </summary>
    private class NaturalStringComparer : IComparer<string>
    {
        public int Compare(string a, string b)
        {
            if (a == b) return 0;
            if (a == null) return -1;
            if (b == null) return 1;

            int ia = 0, ib = 0;
            while (ia < a.Length && ib < b.Length)
            {
                if (char.IsDigit(a[ia]) && char.IsDigit(b[ib]))
                {
                    long na = 0, nb = 0;
                    while (ia < a.Length && char.IsDigit(a[ia]))
                        na = na * 10 + (a[ia++] - '0');
                    while (ib < b.Length && char.IsDigit(b[ib]))
                        nb = nb * 10 + (b[ib++] - '0');
                    if (na != nb) return na.CompareTo(nb);
                }
                else
                {
                    if (a[ia] != b[ib]) return a[ia].CompareTo(b[ib]);
                    ia++;
                    ib++;
                }
            }
            return a.Length.CompareTo(b.Length);
        }
    }
}
