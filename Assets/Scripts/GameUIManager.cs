using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using System.Collections;

public class GameUIManager : MonoBehaviour
{
    [Header("Referencias")]
    public BallController ballController;
    public LevelGenerator levelGenerator;
    public CameraFollow cameraFollow;
    public TowerController towerController;

    [Header("UI")]
    public string loseTitle = "Perdiste";
    public float loseMenuDelay = 2f;

    [Header("Audio")]

    private const string BestScoreKey = "BestPlatformsScore";

    private int currentScore;
    private int bestScore;
    private Vector3 ballStartPosition;

    private Canvas canvas;
    private GameObject hudPanel;
    private Text scoreText;
    private Text bestText;

    private GameObject loseOverlay;
    private GameObject loseCard;
    private Text loseScoreText;
    private Text loseBestText;
    private AudioSource uiAudioSource;
    private bool restartInProgress;
    private float ignoreDeathUntil;
    private Coroutine loseMenuRoutine;

    void Awake()
    {
        if (ballController == null)
            ballController = FindFirstObjectByType<BallController>();

        if (levelGenerator == null)
            levelGenerator = FindFirstObjectByType<LevelGenerator>();

        if (cameraFollow == null)
            cameraFollow = FindFirstObjectByType<CameraFollow>();

        if (towerController == null)
            towerController = FindFirstObjectByType<TowerController>();

        if (ballController != null)
            ballStartPosition = ballController.transform.position;

        uiAudioSource = GetComponent<AudioSource>();
        if (uiAudioSource == null)
            uiAudioSource = gameObject.AddComponent<AudioSource>();

        uiAudioSource.playOnAwake = false;
        uiAudioSource.spatialBlend = 0f;

        bestScore = PlayerPrefs.GetInt(BestScoreKey, 0);

        EnsureEventSystem();
        BuildUi();
        RefreshHud();
        ShowLoseMenu(false);
    }

    void OnEnable()
    {
        RingTrigger.OnRingPassed += HandleRingPassed;
        BallController.OnPlayerDied += HandlePlayerDied;
    }

    void OnDisable()
    {
        RingTrigger.OnRingPassed -= HandleRingPassed;
        BallController.OnPlayerDied -= HandlePlayerDied;

        if (loseMenuRoutine != null)
        {
            StopCoroutine(loseMenuRoutine);
            loseMenuRoutine = null;
        }
    }

    void EnsureEventSystem()
    {
        EventSystem existing = FindFirstObjectByType<EventSystem>();
        if (existing != null)
        {
            StandaloneInputModule legacyModule = existing.GetComponent<StandaloneInputModule>();
            if (legacyModule != null)
                Destroy(legacyModule);

            if (existing.GetComponent<InputSystemUIInputModule>() == null)
                existing.gameObject.AddComponent<InputSystemUIInputModule>();

            return;
        }

        GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        DontDestroyOnLoad(es);
    }

    void BuildUi()
    {
        GameObject canvasGO = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
        {
            Debug.LogError("GameUIManager: No se pudo cargar LegacyRuntime.ttf. Asigna una fuente manualmente en el script si persiste.");
            return;
        }

        hudPanel = CreatePanel("HUDPanel", canvas.transform, new Color(0f, 0f, 0f, 0.28f));
        RectTransform hudRT = hudPanel.GetComponent<RectTransform>();
        hudRT.anchorMin = new Vector2(0f, 1f);
        hudRT.anchorMax = new Vector2(0f, 1f);
        hudRT.pivot = new Vector2(0f, 1f);
        hudRT.anchoredPosition = new Vector2(18f, -18f);
        hudRT.sizeDelta = new Vector2(340f, 96f);

        scoreText = CreateText("ScoreText", hudPanel.transform, font, 30, FontStyle.Bold, Color.white, TextAnchor.UpperLeft);
        RectTransform scoreRT = scoreText.GetComponent<RectTransform>();
        scoreRT.anchorMin = new Vector2(0f, 1f);
        scoreRT.anchorMax = new Vector2(0f, 1f);
        scoreRT.pivot = new Vector2(0f, 1f);
        scoreRT.anchoredPosition = new Vector2(16f, -12f);
        scoreRT.sizeDelta = new Vector2(300f, 40f);

        bestText = CreateText("BestText", hudPanel.transform, font, 23, FontStyle.Normal, new Color(0.86f, 0.94f, 1f, 1f), TextAnchor.UpperLeft);
        RectTransform bestRT = bestText.GetComponent<RectTransform>();
        bestRT.anchorMin = new Vector2(0f, 1f);
        bestRT.anchorMax = new Vector2(0f, 1f);
        bestRT.pivot = new Vector2(0f, 1f);
        bestRT.anchoredPosition = new Vector2(16f, -50f);
        bestRT.sizeDelta = new Vector2(300f, 30f);

        loseOverlay = CreatePanel("LoseOverlay", canvas.transform, new Color(0.02f, 0.03f, 0.05f, 0.6f));
        RectTransform overlayRT = loseOverlay.GetComponent<RectTransform>();
        overlayRT.anchorMin = Vector2.zero;
        overlayRT.anchorMax = Vector2.one;
        overlayRT.offsetMin = Vector2.zero;
        overlayRT.offsetMax = Vector2.zero;

        loseCard = CreatePanel("LoseCard", loseOverlay.transform, new Color(0.09f, 0.11f, 0.15f, 0.96f));
        RectTransform cardRT = loseCard.GetComponent<RectTransform>();
        cardRT.anchorMin = new Vector2(0.5f, 0.5f);
        cardRT.anchorMax = new Vector2(0.5f, 0.5f);
        cardRT.pivot = new Vector2(0.5f, 0.5f);
        cardRT.sizeDelta = new Vector2(560f, 340f);

        Text title = CreateText("LoseTitle", loseCard.transform, font, 54, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        title.text = loseTitle;
        RectTransform titleRT = title.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.5f, 1f);
        titleRT.anchorMax = new Vector2(0.5f, 1f);
        titleRT.pivot = new Vector2(0.5f, 1f);
        titleRT.anchoredPosition = new Vector2(0f, -32f);
        titleRT.sizeDelta = new Vector2(500f, 68f);

        loseScoreText = CreateText("LoseScoreText", loseCard.transform, font, 30, FontStyle.Bold, new Color(1f, 0.93f, 0.52f, 1f), TextAnchor.MiddleCenter);
        RectTransform loseScoreRT = loseScoreText.GetComponent<RectTransform>();
        loseScoreRT.anchorMin = new Vector2(0.5f, 1f);
        loseScoreRT.anchorMax = new Vector2(0.5f, 1f);
        loseScoreRT.pivot = new Vector2(0.5f, 1f);
        loseScoreRT.anchoredPosition = new Vector2(0f, -122f);
        loseScoreRT.sizeDelta = new Vector2(520f, 48f);

        loseBestText = CreateText("LoseBestText", loseCard.transform, font, 25, FontStyle.Normal, new Color(0.86f, 0.94f, 1f, 1f), TextAnchor.MiddleCenter);
        RectTransform loseBestRT = loseBestText.GetComponent<RectTransform>();
        loseBestRT.anchorMin = new Vector2(0.5f, 1f);
        loseBestRT.anchorMax = new Vector2(0.5f, 1f);
        loseBestRT.pivot = new Vector2(0.5f, 1f);
        loseBestRT.anchoredPosition = new Vector2(0f, -170f);
        loseBestRT.sizeDelta = new Vector2(520f, 40f);

        CreateButton("Reiniciar", loseCard.transform, font, new Vector2(0f, -230f), new Color(0.11f, 0.58f, 0.9f, 1f), RestartGame);
        CreateButton("Salir", loseCard.transform, font, new Vector2(0f, -286f), new Color(0.8f, 0.25f, 0.3f, 1f), QuitGame);
    }

    GameObject CreatePanel(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        return go;
    }

    Text CreateText(string name, Transform parent, Font font, int size, FontStyle style, Color color, TextAnchor align)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        Text text = go.GetComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = align;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    void CreateButton(string label, Transform parent, Font font, Vector2 anchoredPos, Color bgColor, UnityEngine.Events.UnityAction onClick)
    {
        GameObject go = new GameObject(label + "Button", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        Image image = go.GetComponent<Image>();
        image.color = bgColor;

        Button button = go.GetComponent<Button>();
        ColorBlock block = button.colors;
        block.normalColor = bgColor;
        block.highlightedColor = Color.Lerp(bgColor, Color.white, 0.14f);
        block.pressedColor = Color.Lerp(bgColor, Color.black, 0.2f);
        block.selectedColor = block.highlightedColor;
        button.colors = block;
        button.onClick.AddListener(onClick);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(340f, 44f);

        Text text = CreateText(label + "Text", go.transform, font, 24, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        text.text = label;
        RectTransform textRT = text.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;
    }

    void HandleRingPassed()
    {
        if (restartInProgress)
            return;

        currentScore++;
        if (currentScore > bestScore)
        {
            bestScore = currentScore;
            PlayerPrefs.SetInt(BestScoreKey, bestScore);
            PlayerPrefs.Save();
        }


        RefreshHud();
    }

    void HandlePlayerDied()
    {
        if (restartInProgress || Time.time < ignoreDeathUntil)
            return;

        if (towerController != null)
            towerController.enabled = false;

        if (loseMenuRoutine != null)
            StopCoroutine(loseMenuRoutine);

        loseMenuRoutine = StartCoroutine(ShowLoseMenuDelayed());
    }

    IEnumerator ShowLoseMenuDelayed()
    {
        float delay = Mathf.Max(0f, loseMenuDelay);
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        ShowLoseMenu(true);
        loseMenuRoutine = null;
    }

    void RefreshHud()
    {
        if (scoreText != null)
            scoreText.text = "Puntaje: " + currentScore;

        if (bestText != null)
            bestText.text = "Mejor: " + bestScore;

        if (loseScoreText != null)
            loseScoreText.text = "Tu puntaje: " + currentScore;

        if (loseBestText != null)
            loseBestText.text = "Mejor puntaje: " + bestScore;
    }

    void ShowLoseMenu(bool show)
    {
        if (loseOverlay != null)
            loseOverlay.SetActive(show);
    }

    public void RestartGame()
    {
        if (restartInProgress)
            return;

        StartCoroutine(RestartRoutine());
    }

    IEnumerator RestartRoutine()
    {
        restartInProgress = true;

        if (levelGenerator == null)
            levelGenerator = FindFirstObjectByType<LevelGenerator>();

        if (ballController == null)
            ballController = FindFirstObjectByType<BallController>();

        if (cameraFollow == null)
            cameraFollow = FindFirstObjectByType<CameraFollow>();

        if (towerController == null)
            towerController = FindFirstObjectByType<TowerController>();

        if (loseMenuRoutine != null)
        {
            StopCoroutine(loseMenuRoutine);
            loseMenuRoutine = null;
        }

        ShowLoseMenu(false);

        // Bloquear eventos de muerte durante la reconstrucción.
        ignoreDeathUntil = Time.time + 0.5f;

        if (ballController != null)
            ballController.ResetBall(ballStartPosition);

        if (cameraFollow != null)
            cameraFollow.ResetCamera();

        if (levelGenerator != null)
            levelGenerator.ResetTower();

        // Espera un frame para completar destrucciones pendientes de Unity.
        yield return null;

        currentScore = 0;
        RefreshHud();

        if (towerController != null)
            towerController.enabled = true;

        restartInProgress = false;
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void PlayPlatformPassedSfx()
    {
        // Removed platform passed sound effect functionality
    }
}
