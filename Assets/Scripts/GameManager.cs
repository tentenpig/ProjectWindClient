using Unity.Entities;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 게임 상태를 관리하고 Title/GameOver UI를 제어하는 매니저
/// UI 프리팹은 Resources/UI/ 에서 로드 (Tools > Create UI Prefabs 로 생성)
/// </summary>
public class GameManager : MonoBehaviour
{
    private enum GameState { Title, Playing, GameOver }

    private GameState _state = GameState.Title;
    private EntityManager _entityManager;
    private EntityQuery _gamePlayingQuery;
    private bool _initialized;
    private static bool _skipTitle;

    // UI
    private Canvas _uiCanvas;
    private GameObject _titlePanel;
    private GameObject _gameOverPanel;

    private void Start()
    {
        CreateUICanvas();
        LoadUIPanels();

        if (_skipTitle)
        {
            _skipTitle = false;
            _titlePanel.SetActive(false);
            StartGame();
        }
        else
        {
            ShowTitle();
        }
    }

    private void Update()
    {
        switch (_state)
        {
            case GameState.Title:
                if (Input.GetMouseButtonDown(0) || Input.anyKeyDown)
                    StartGame();
                break;

            case GameState.Playing:
                CheckPlayerDeath();
                break;
        }
    }

    private void StartGame()
    {
        _state = GameState.Playing;
        _titlePanel.SetActive(false);

        _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        _entityManager.CreateEntity(typeof(GamePlaying));
        _gamePlayingQuery = _entityManager.CreateEntityQuery(typeof(GamePlaying));
        _initialized = true;
    }

    private void CheckPlayerDeath()
    {
        if (!_initialized) return;

        if (_gamePlayingQuery.IsEmpty)
        {
            ShowGameOver();
        }
    }

    private void ShowTitle()
    {
        _state = GameState.Title;
        _titlePanel.SetActive(true);
        _gameOverPanel.SetActive(false);
    }

    private void ShowGameOver()
    {
        _state = GameState.GameOver;
        _titlePanel.SetActive(false);
        _gameOverPanel.SetActive(true);
    }

    private void RestartGame()
    {
        _skipTitle = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void GoToTitle()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ─────────────────────────────────────
    // UI 초기화
    // ─────────────────────────────────────

    private void CreateUICanvas()
    {
        var canvasGO = new GameObject("GameUICanvas");
        canvasGO.transform.SetParent(transform);
        _uiCanvas = canvasGO.AddComponent<Canvas>();
        _uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _uiCanvas.sortingOrder = 200;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        if (FindAnyObjectByType<EventSystem>() == null)
        {
            var eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.transform.SetParent(transform);
            eventSystemGO.AddComponent<EventSystem>();
            eventSystemGO.AddComponent<StandaloneInputModule>();
        }
    }

    private void LoadUIPanels()
    {
        var settings = Resources.Load<GameUISettings>("GameUISettings");
        if (settings == null)
        {
            Debug.LogError("[GameManager] Resources/GameUISettings 을 찾을 수 없습니다.");
            _titlePanel = new GameObject("TitlePanel");
            _titlePanel.transform.SetParent(_uiCanvas.transform, false);
            _gameOverPanel = new GameObject("GameOverPanel");
            _gameOverPanel.transform.SetParent(_uiCanvas.transform, false);
            return;
        }

        _titlePanel = Instantiate(settings.titlePanelPrefab, _uiCanvas.transform, false);

        _gameOverPanel = Instantiate(settings.gameOverPanelPrefab, _uiCanvas.transform, false);
        BindButton(_gameOverPanel, "RestartButton", RestartGame);
        BindButton(_gameOverPanel, "TitleButton", GoToTitle);

        _gameOverPanel.SetActive(false);
    }

    private static void BindButton(GameObject panel, string buttonName, UnityEngine.Events.UnityAction action)
    {
        var buttonTransform = panel.transform.Find(buttonName);
        if (buttonTransform == null) return;

        var button = buttonTransform.GetComponent<Button>();
        if (button != null)
            button.onClick.AddListener(action);
    }
}
