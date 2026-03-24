using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class CreateSubScene
{
    public static void Execute()
    {
        var scene = SceneManager.GetActiveScene();

        var player = GameObject.Find("Player");
        var spawner = GameObject.Find("EnemySpawner");

        if (player == null || spawner == null)
        {
            Debug.LogError("Player or EnemySpawner not found in scene!");
            return;
        }

        // Create sub scene
        string subScenePath = "Assets/Scenes/GameplaySubScene.unity";
        var subScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

        SceneManager.MoveGameObjectToScene(player, subScene);
        SceneManager.MoveGameObjectToScene(spawner, subScene);

        EditorSceneManager.SaveScene(subScene, subScenePath);
        SceneManager.SetActiveScene(scene);
        EditorSceneManager.CloseScene(subScene, true);

        // Create SubScene component using reflection to avoid assembly reference issues
        var subSceneGO = new GameObject("GameplaySubScene");
        var subSceneType = Type.GetType("Unity.Scenes.SubScene, Unity.Scenes, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
        if (subSceneType == null)
        {
            // Try alternative assembly name
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                subSceneType = asm.GetType("Unity.Scenes.SubScene");
                if (subSceneType != null) break;
            }
        }

        if (subSceneType != null)
        {
            var component = subSceneGO.AddComponent(subSceneType);
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(subScenePath);
            var so = new SerializedObject(component);
            var prop = so.FindProperty("_SceneAsset");
            if (prop != null)
            {
                prop.objectReferenceValue = sceneAsset;
                so.ApplyModifiedProperties();
            }
        }
        else
        {
            Debug.LogError("SubScene type not found!");
        }

        EditorSceneManager.SaveScene(scene);
        Debug.Log("SubScene created! Player and EnemySpawner moved into GameplaySubScene.");
    }
}
