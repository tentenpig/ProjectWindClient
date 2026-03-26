using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class AddSubScene
{
    public static string Execute()
    {
        // SubScene 타입을 리플렉션으로 가져오기 (Unity.Scenes 어셈블리)
        var subSceneType = System.Type.GetType("Unity.Scenes.SubScene, Unity.Scenes");
        if (subSceneType == null)
            return "ERROR: SubScene 타입을 찾을 수 없습니다.";

        // 이미 존재하는지 확인
        var existing = Object.FindAnyObjectByType(subSceneType);
        if (existing != null)
            return $"SubScene이 이미 존재합니다: {((Component)existing).gameObject.name}";

        // GameplaySubScene 에셋 찾기
        var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/GameplaySubScene.unity");
        if (sceneAsset == null)
            sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/GameplaySubScene.unity");
        if (sceneAsset == null)
            return "ERROR: GameplaySubScene.unity를 찾을 수 없습니다.";

        // SubScene GameObject 생성
        var subSceneGO = new GameObject("GameplaySubScene");
        var subScene = subSceneGO.AddComponent(subSceneType);

        // SceneAsset 프로퍼티 설정
        var prop = subSceneType.GetProperty("SceneAsset");
        if (prop != null && prop.CanWrite)
        {
            prop.SetValue(subScene, sceneAsset);
        }
        else
        {
            // 필드 직접 접근
            var field = subSceneType.GetField("_SceneAsset",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
                field.SetValue(subScene, sceneAsset);
        }

        EditorUtility.SetDirty(subSceneGO);
        EditorSceneManager.MarkSceneDirty(subSceneGO.scene);

        return $"SubScene 추가 완료: {sceneAsset.name}";
    }
}
