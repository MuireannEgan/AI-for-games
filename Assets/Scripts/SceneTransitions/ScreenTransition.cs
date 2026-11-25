using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public class ScreenTransition : MonoBehaviour
{
    public static ScreenTransition Instance { get; private set; }
    public bool IsBusy { get; private set; }

    private const float SceneFadeDuration = 0.8f;
    private const float DefeatFadeDuration = 1f;
    private const float EscapeFadeDuration = 1.5f;

    private CanvasGroup overlay;
    private Image background;
    private GameObject endingPanel;
    private Text heading;
    private Text message;
    private Text buttonLabel;
    private EventSystem createdEventSystem;
    private bool ending;
    private Font font;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance == null)
            new GameObject("ScreenTransition").AddComponent<ScreenTransition>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildUI();
        overlay.alpha = 0f;
        overlay.blocksRaycasts = false;
    }

    private void LateUpdate()
    {
        if (ending)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public bool TryLoadScene(string sceneName)
    {
        if (IsBusy || !Application.CanStreamedLevelBeLoaded(sceneName))
            return false;

        IsBusy = true;
        StartCoroutine(LoadScene(sceneName));
        return true;
    }

    public bool TryShowEnding(bool escaped)
    {
        if (IsBusy)
            return false;

        IsBusy = true;
        ending = true;
        Time.timeScale = 0f;
        StartCoroutine(ShowEnding(escaped));
        return true;
    }

    private IEnumerator LoadScene(string sceneName)
    {
        float previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        background.color = Color.black;
        overlay.blocksRaycasts = true;
        yield return Fade(1f, SceneFadeDuration);
        yield return SceneManager.LoadSceneAsync(sceneName);
        yield return null;
        Time.timeScale = previousTimeScale;
        yield return Fade(0f, SceneFadeDuration);
        overlay.blocksRaycasts = false;
        IsBusy = false;
    }

    private IEnumerator ShowEnding(bool escaped)
    {
        background.color = escaped ? Color.white : Color.black;
        overlay.blocksRaycasts = true;
        yield return Fade(1f, escaped ? EscapeFadeDuration : DefeatFadeDuration);

        heading.text = escaped ? "YOU ESCAPED" : "GAME OVER";
        heading.color = escaped ? Color.black : new Color(0.75f, 0.22f, 0.22f);
        message.text = escaped ? "You survived the darkness." : "You were caught.";
        message.color = escaped ? new Color(0.25f, 0.25f, 0.25f) : new Color(0.75f, 0.75f, 0.75f);
        buttonLabel.text = escaped ? "Play Again" : "Try Again";
        EnsureEventSystem();
        endingPanel.SetActive(true);
    }

    private void Restart()
    {
        if (!ending || !endingPanel.activeSelf)
            return;

        if (!Application.CanStreamedLevelBeLoaded("Forest_Level"))
        {
            Debug.LogError("ScreenTransition: Forest_Level is not in the build scene list.");
            return;
        }

        endingPanel.SetActive(false);
        StartCoroutine(RestartGame());
    }

    private IEnumerator RestartGame()
    {
        ending = false;
        background.color = Color.black;
        overlay.alpha = 1f;

        ForestGenerator.ResetSession();

        if (ProgressManager.Instance != null &&
            (GameDirector.Instance == null ||
             ProgressManager.Instance.gameObject != GameDirector.Instance.gameObject))
            Destroy(ProgressManager.Instance.gameObject);

        if (GameDirector.Instance != null)
            Destroy(GameDirector.Instance.gameObject);

        if (createdEventSystem != null)
            Destroy(createdEventSystem.gameObject);

        yield return null;
        yield return SceneManager.LoadSceneAsync("Forest_Level");
        yield return null;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Time.timeScale = 1f;
        yield return Fade(0f, SceneFadeDuration);
        overlay.blocksRaycasts = false;
        IsBusy = false;
    }

    private IEnumerator Fade(float target, float duration)
    {
        float start = overlay.alpha;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            overlay.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        overlay.alpha = target;
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null)
            return;

        GameObject eventObject = new GameObject("TransitionEventSystem");
        eventObject.transform.SetParent(transform, false);
        createdEventSystem = eventObject.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
        InputSystemUIInputModule module = eventObject.AddComponent<InputSystemUIInputModule>();
        module.AssignDefaultActions();
#else
        eventObject.AddComponent<StandaloneInputModule>();
#endif
    }

    private void BuildUI()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject canvasObject = new GameObject("TransitionCanvas", typeof(RectTransform));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();
        overlay = canvasObject.AddComponent<CanvasGroup>();

        RectTransform backgroundRect = CreateRect("Background", canvasObject.transform);
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;
        background = backgroundRect.gameObject.AddComponent<Image>();
        background.color = Color.black;

        RectTransform panel = CreateRect("EndingPanel", canvasObject.transform);
        panel.anchorMin = Vector2.zero;
        panel.anchorMax = Vector2.one;
        panel.offsetMin = Vector2.zero;
        panel.offsetMax = Vector2.zero;
        endingPanel = panel.gameObject;

        heading = CreateText("Heading", panel, new Vector2(0, 110), new Vector2(1000, 100), 64);
        heading.fontStyle = FontStyle.Bold;
        message = CreateText("Message", panel, new Vector2(0, 15), new Vector2(1000, 60), 30);

        RectTransform buttonRect = CreateRect("RestartButton", panel);
        buttonRect.sizeDelta = new Vector2(300, 76);
        buttonRect.anchoredPosition = new Vector2(0, -110);
        Image buttonImage = buttonRect.gameObject.AddComponent<Image>();
        buttonImage.color = new Color(0.17f, 0.17f, 0.17f);
        Button button = buttonRect.gameObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(Restart);
        buttonLabel = CreateText("Label", buttonRect, Vector2.zero, new Vector2(300, 76), 30);
        buttonLabel.color = Color.white;
        endingPanel.SetActive(false);
    }

    private static RectTransform CreateRect(string objectName, Transform parent)
    {
        GameObject obj = new GameObject(objectName, typeof(RectTransform));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        return rect;
    }

    private Text CreateText(string objectName, Transform parent, Vector2 position,
        Vector2 size, int fontSize)
    {
        RectTransform rect = CreateRect(objectName, parent);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Text text = rect.gameObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        return text;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}