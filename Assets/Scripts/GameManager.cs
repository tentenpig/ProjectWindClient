using Shared.Protocol;
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
    private const string PrefKeyAccountName = "AccountName";
    private const string PrefKeyPassword = "Password";

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

    // 로그인 UI
    private TMP_InputField _idInput;
    private TMP_InputField _pwInput;
    private Button _loginButton;

    private void Start()
    {
        CreateUICanvas();
        LoadUIPanels();
        CreateLoginUI();
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
                if (_idInput != null && !_idInput.isFocused && !_pwInput.isFocused
                    && Input.GetKeyDown(KeyCode.Return))
                {
                    OnLoginButtonClicked();
                }
                break;

            case GameState.Playing:
                CheckPlayerDeath();
                break;
        }
    }

    private void OnLoginButtonClicked()
    {
        var id = _idInput.text.Trim();
        var pw = _pwInput.text;

        if (string.IsNullOrEmpty(id))
        {
            _idInput.Select();
            return;
        }

        PlayerPrefs.SetString(PrefKeyAccountName, id);
        PlayerPrefs.SetString(PrefKeyPassword, pw);
        PlayerPrefs.Save();

        StartGame();
    }

    private void StartGame()
    {
        var id = PlayerPrefs.GetString(PrefKeyAccountName, "");
        var pw = PlayerPrefs.GetString(PrefKeyPassword, "");

        _state = GameState.Connecting;
        _titlePanel.SetActive(false);
        ShowConnecting("서버에 연결하는 중...");

        NetworkManager.Instance.Connect(success =>
        {
            if (success)
            {
                SetConnectingText("로그인 중...");
                NetworkManager.Instance.OnLoginResponse += OnLoginResponse;
                NetworkManager.Instance.SendLogin(id, pw);
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
            Debug.LogError($"[GameManager] 로그인 실패: {response.Message}");
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

        if (_idInput != null)
            _idInput.Select();
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
    // 로그인 UI
    // ─────────────────────────────────────

    private void CreateLoginUI()
    {
        // 기존 PressStartText 숨기기
        var pressStart = _titlePanel.transform.Find("PressStartText");
        if (pressStart != null)
            pressStart.gameObject.SetActive(false);

        // 로그인 폼 컨테이너
        var formGO = new GameObject("LoginForm");
        formGO.transform.SetParent(_titlePanel.transform, false);
        var formRect = formGO.AddComponent<RectTransform>();
        formRect.anchorMin = new Vector2(0.5f, 0.3f);
        formRect.anchorMax = new Vector2(0.5f, 0.3f);
        formRect.sizeDelta = new Vector2(400f, 200f);
        formRect.anchoredPosition = Vector2.zero;

        // ID 입력
        _idInput = CreateInputField(formGO.transform, "IdInput", "ID", false, new Vector2(0f, 60f));
        _idInput.text = PlayerPrefs.GetString(PrefKeyAccountName, "");

        // PW 입력
        _pwInput = CreateInputField(formGO.transform, "PwInput", "Password", true, new Vector2(0f, 0f));
        _pwInput.text = PlayerPrefs.GetString(PrefKeyPassword, "");

        // Tab 키로 ID→PW 이동
        _idInput.onSubmit.AddListener(_ => _pwInput.Select());
        _pwInput.onSubmit.AddListener(_ => OnLoginButtonClicked());

        // 로그인 버튼
        _loginButton = CreateButton(formGO.transform, "LoginButton", "Login", new Vector2(0f, -60f));
        _loginButton.onClick.AddListener(OnLoginButtonClicked);
    }

    private static TMP_InputField CreateInputField(Transform parent, string name, string placeholder,
        bool isPassword, Vector2 position)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(360f, 50f);
        rect.anchoredPosition = position;

        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);

        // 텍스트 영역
        var textArea = new GameObject("Text Area");
        textArea.transform.SetParent(go.transform, false);
        var taRect = textArea.AddComponent<RectTransform>();
        taRect.anchorMin = Vector2.zero;
        taRect.anchorMax = Vector2.one;
        taRect.offsetMin = new Vector2(10f, 5f);
        taRect.offsetMax = new Vector2(-10f, -5f);
        textArea.AddComponent<RectMask2D>();

        // 입력 텍스트
        var textGO = new GameObject("Text");
        textGO.transform.SetParent(textArea.transform, false);
        var textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = 28;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Overflow;

        // 플레이스홀더
        var phGO = new GameObject("Placeholder");
        phGO.transform.SetParent(textArea.transform, false);
        var phRect = phGO.AddComponent<RectTransform>();
        phRect.anchorMin = Vector2.zero;
        phRect.anchorMax = Vector2.one;
        phRect.offsetMin = Vector2.zero;
        phRect.offsetMax = Vector2.zero;

        var phTmp = phGO.AddComponent<TextMeshProUGUI>();
        phTmp.text = placeholder;
        phTmp.fontSize = 28;
        phTmp.fontStyle = FontStyles.Italic;
        phTmp.color = new Color(0.6f, 0.6f, 0.6f, 0.6f);
        phTmp.alignment = TextAlignmentOptions.MidlineLeft;
        phTmp.enableWordWrapping = false;
        phTmp.overflowMode = TextOverflowModes.Overflow;

        var inputField = go.AddComponent<TMP_InputField>();
        inputField.textViewport = taRect;
        inputField.textComponent = tmp;
        inputField.placeholder = phTmp;
        inputField.fontAsset = tmp.font;
        inputField.pointSize = 28;
        inputField.caretColor = Color.white;
        inputField.selectionColor = new Color(0.3f, 0.5f, 0.8f, 0.5f);

        if (isPassword)
            inputField.contentType = TMP_InputField.ContentType.Password;

        return inputField;
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 position)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(360f, 50f);
        rect.anchoredPosition = position;

        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.4f, 0.7f, 1f);

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(go.transform, false);
        var textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 30;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;

        return go.AddComponent<Button>();
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
