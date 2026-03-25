using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 씬 로드 시 GameManager를 자동으로 생성하는 부트스트래퍼
/// </summary>
public static class GameBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        CreateGameManager();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CreateGameManager();
    }

    private static void CreateGameManager()
    {
        if (Object.FindAnyObjectByType<GameManager>() == null)
        {
            var go = new GameObject("GameManager");
            go.AddComponent<GameManager>();
        }
    }
}
