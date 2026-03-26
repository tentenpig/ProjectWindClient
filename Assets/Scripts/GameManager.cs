using Unity.Entities;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 게임 상태를 관리하고 Title/GameOver UI를 제어하는 매니저
/// UI 프리팹은 Resources/UI/ 에서 로드 (Tools > Create UI Prefabs 로 생성)
/// </summary>
public class GameManager : MonoBehaviour
{
    private enum GameState { Title, Connecting, Playing, GameOver }

    private GameState _state = GameState.Title;
    private EntityManager _entityManager;
    private EntityQuery _gamePlayingQuery;
    private bool _initialized;
    private static bool _skipTitle;

    // UI
    private Canvas _uiCanvas;
    private GameObject _titlePanel;
    private GameObject _gameOverPanel;
    private GameObject _connectingPanel;
    private TextMeshProUGUI _connectingText;

    private void Start()
    {
        CreateUICanvas();
        LoadUIPanels();
        EnsureNetworkManager();

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
        _state = GameState.Connecting;
        _titlePanel.SetActive(false);
        ShowConnecting("서버에 연결하는 중...");

        NetworkManager.Instance.Connect(success =>
        {
            if (success)
            {
                SetConnectingText("로그인 중...");
                NetworkManager.Instance.OnLoginResponse += OnLoginResponse;
                NetworkManager.Instance.SendLogin("Player_" + UnityEngine.Random.Range(1000, 9999));
            }
            else
            {
                Debug.LogError("[GameManager] 서버 연결 실패");
                HideConnecting();
                ShowTitle();
            }
        });
    }

    private void OnLoginResponse(S_Login response)
    {
        NetworkManager.Instance.OnLoginResponse -= OnLoginResponse;
        HideConnecting();

        if (!response.Success)
        {
            Debug.LogError("[GameManager] 로그인 실패");
            ShowTitle();
            return;
        }

        Debug.Log($"[GameManager] 로그인 성공: PlayerId={response.PlayerId}, Map={response.MapId}, Pos=({response.Position.X},{response.Position.Y})");

        _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        _entityManager.CreateEntity(typeof(GamePlaying));
        _gamePlayingQuery = _entityManager.CreateEntityQuery(typeof(GamePlaying));
        _initialized = true;
        _state = GameState.Playing;
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

    // ─────────────────────────────────────
    // 연결 상태 UI
    // ─────────────────────────────────────

    private void CreateConnectingPanel()
    {
        _connectingPanel = new GameObject("ConnectingPanel");
        _connectingPanel.transform.SetParent(_uiCanvas.transform, false);

        var rect = _connectingPanel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        // 반투명 배경
        var bg = _connectingPanel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.7f);

        // 상태 텍스트
        var textGO = new GameObject("StatusText");
        textGO.transform.SetParent(_connectingPanel.transform, false);
        var textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.sizeDelta = new Vector2(600f, 100f);

        _connectingText = textGO.AddComponent<TextMeshProUGUI>();
        _connectingText.text = "";
        _connectingText.fontSize = 36;
        _connectingText.alignment = TextAlignmentOptions.Center;
        _connectingText.color = Color.white;

        _connectingPanel.SetActive(false);
    }

    private void ShowConnecting(string message)
    {
        if (_connectingPanel == null)
            CreateConnectingPanel();

        _connectingText.text = message;
        _connectingPanel.SetActive(true);
    }

    private void SetConnectingText(string message)
    {
        if (_connectingText != null)
            _connectingText.text = message;
    }

    private void HideConnecting()
    {
        if (_connectingPanel != null)
            _connectingPanel.SetActive(false);
    }

    private static void EnsureNetworkManager()
    {
        if (NetworkManager.Instance == null)
        {
            var go = new GameObject("NetworkManager");
            go.AddComponent<NetworkManager>();
        }
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
