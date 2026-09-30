using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Builds and runs all the screens: main menu, in-game HUD, pause menu, game over and the countdown.
// Layout is designed for a 1080 x 1920 portrait screen and scales to fit any phone.
public class GameUI : MonoBehaviour
{
    public Canvas HudCanvas { get; private set; }
    public Font Font { get; private set; }

    private GameManager game;
    private Camera cam;

    private RectTransform safeArea;
    private Rect lastSafeArea;
    private Image dim;
    private float dimTarget;

    private CanvasGroup menuPanel;
    private CanvasGroup hudPanel;
    private CanvasGroup pausePanel;
    private CanvasGroup gameOverPanel;
    private CanvasGroup countdownPanel;
    private CanvasGroup[] panels;

    // Menu
    private RectTransform menuTitle;
    private Text menuBest;
    private Text soundLabel;
    private Text vibrationLabel;

    // HUD
    private Text scoreText;
    private Text levelText;
    private Text levelUpText;
    private float levelUpTimer;
    private float scorePunch;

    // Pause
    private Text pauseScore;

    // Game over
    private Text gameOverScore;
    private Text gameOverBest;

    // Countdown
    private Text countdownText;
    private Text countdownHint;
    private float countdownPunch;

    private readonly Color faded = new Color(1f, 1f, 1f, 0.6f);

    public void Init(GameManager gameManager, Camera camera)
    {
        game = gameManager;
        cam = camera;
        Font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Buttons need an EventSystem to receive touches
        if (EventSystem.current == null)
        {
            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem));
            eventSystem.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        BuildScoreCanvas();

        HudCanvas = CreateCanvas("HUD Canvas", 10);
        dim = new GameObject("Dim", typeof(RectTransform)).AddComponent<Image>();
        dim.transform.SetParent(HudCanvas.transform, false);
        dim.color = new Color(0.02f, 0.01f, 0.08f, 0f);
        dim.raycastTarget = false;
        Stretch(dim.rectTransform);

        // Everything else goes inside the safe area so notches and rounded corners never cover it
        safeArea = new GameObject("Safe Area", typeof(RectTransform)).GetComponent<RectTransform>();
        safeArea.SetParent(HudCanvas.transform, false);
        Stretch(safeArea);

        BuildMenu();
        BuildHud();
        BuildPause();
        BuildGameOver();
        BuildCountdown();
        panels = new[] { menuPanel, hudPanel, pausePanel, gameOverPanel, countdownPanel };

        FitSafeArea();
        RefreshSettings();
    }

    // ---------- Showing screens ----------

    public void ShowMenu(int best)
    {
        menuBest.text = best > 0 ? "BEST  " + best : "";
        Show(menuPanel, 0.55f);
    }

    public void ShowHud()
    {
        Show(hudPanel, 0f);
    }

    public void ShowPause(int score)
    {
        pauseScore.text = "SCORE  " + score;
        Show(pausePanel, 0.7f);
    }

    public void ShowGameOver(int score, int best, bool newBest, int level)
    {
        gameOverScore.text = score.ToString();
        gameOverBest.text = newBest ? "NEW BEST!" : "BEST  " + best + "      LEVEL  " + level;
        gameOverBest.color = newBest ? Theme.Highlight : faded;
        Show(gameOverPanel, 0.7f);
    }

    public void ShowCountdown(string text, bool showHint)
    {
        if (!countdownPanel.gameObject.activeSelf) Show(countdownPanel, 0.25f);
        countdownText.text = text;
        countdownHint.enabled = showHint;
        countdownPunch = 1f;
    }

    public void SetScore(int score, bool punch)
    {
        scoreText.text = score.ToString();
        if (punch) scorePunch = 1f;
    }

    public void SetScoreVisible(bool visible)
    {
        scoreText.enabled = visible;
    }

    public void SetLevel(int level, Color color, bool announce)
    {
        levelText.text = "LEVEL  " + level;
        levelText.color = new Color(color.r, color.g, color.b, 0.8f);
        if (!announce) return;

        levelUpText.text = "LEVEL " + level;
        levelUpText.color = color;
        levelUpTimer = 1.6f;
    }

    public void RefreshSettings()
    {
        soundLabel.text = Settings.SoundOn ? "SOUND  ON" : "SOUND  OFF";
        vibrationLabel.text = Settings.VibrationOn ? "VIBRATE  ON" : "VIBRATE  OFF";
    }

    private void Show(CanvasGroup panel, float dimAmount)
    {
        foreach (CanvasGroup p in panels) p.gameObject.SetActive(p == panel);
        panel.alpha = 0f;
        panel.transform.localScale = Vector3.one * 0.94f;
        dimTarget = dimAmount;
        levelUpTimer = 0f;
        levelUpText.enabled = false;
    }

    // ---------- Animation ----------

    void Update()
    {
        FitSafeArea();
        float dt = Time.unscaledDeltaTime;

        // Fade and grow the active screen in
        foreach (CanvasGroup p in panels)
        {
            if (!p.gameObject.activeSelf) continue;
            p.alpha = Mathf.MoveTowards(p.alpha, 1f, dt * 5f);
            p.transform.localScale = Vector3.MoveTowards(p.transform.localScale, Vector3.one, dt * 0.5f);
        }

        Color d = dim.color;
        d.a = Mathf.MoveTowards(d.a, dimTarget, dt * 3f);
        dim.color = d;

        // Title gently bobs up and down
        menuTitle.anchoredPosition = new Vector2(0f, 470f + Mathf.Sin(Time.unscaledTime * 2f) * 12f);

        scorePunch = Mathf.MoveTowards(scorePunch, 0f, dt * 4f);
        scoreText.transform.localScale = Vector3.one * (1f + 0.25f * scorePunch);

        countdownPunch = Mathf.MoveTowards(countdownPunch, 0f, dt * 3f);
        countdownText.transform.localScale = Vector3.one * (1f + 0.5f * countdownPunch);

        // "LEVEL X" banner pops in, holds, then fades out
        if (levelUpTimer > 0f)
        {
            levelUpTimer -= dt;
            levelUpText.enabled = levelUpTimer > 0f;
            float age = 1.6f - levelUpTimer;
            levelUpText.transform.localScale = Vector3.one * (age < 0.15f ? Mathf.Lerp(0.4f, 1.15f, age / 0.15f) : Mathf.Lerp(1.15f, 1f, (age - 0.15f) * 4f));
            Color c = levelUpText.color;
            c.a = Mathf.Clamp01(levelUpTimer * 2f);
            levelUpText.color = c;
        }
    }

    // ---------- Building screens ----------

    private void BuildScoreCanvas()
    {
        // Big faded score in the middle of the screen, drawn behind the ball and paddle
        Canvas canvas = CreateCanvas("Score Canvas", 0);
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 5f;
        canvas.sortingLayerName = "Background";
        canvas.sortingOrder = 3;
        scoreText = CreateText(canvas.transform, "Score", 420, new Vector2(0f, 250f), new Color(1f, 1f, 1f, 0.1f));
        scoreText.enabled = false;
    }

    private void BuildMenu()
    {
        menuPanel = CreatePanel("Menu");
        RectTransform root = (RectTransform)menuPanel.transform;

        Text title = CreateText(root, "Title", 210, new Vector2(0f, 470f), Theme.Highlight);
        title.text = "KEEP\nIT UP";
        title.lineSpacing = 0.85f;
        title.rectTransform.sizeDelta = new Vector2(1000f, 520f);
        menuTitle = title.rectTransform;

        menuBest = CreateText(root, "Best", 56, new Vector2(0f, 150f), faded);

        CreateButton(root, "PLAY", new Vector2(0f, -110f), new Vector2(620f, 190f), true, game.Play);
        soundLabel = CreateButton(root, "SOUND", new Vector2(-170f, -340f), new Vector2(310f, 120f), false, ToggleSound);
        vibrationLabel = CreateButton(root, "VIBRATE", new Vector2(170f, -340f), new Vector2(310f, 120f), false, ToggleVibration);
        soundLabel.fontSize = vibrationLabel.fontSize = 36;

        Text hint = CreateText(root, "Hint", 40, new Vector2(0f, -520f), new Color(1f, 1f, 1f, 0.45f));
        hint.fontStyle = FontStyle.Normal;
        hint.text = "Drag anywhere to move the paddle";
    }

    private void BuildHud()
    {
        hudPanel = CreatePanel("HUD");
        RectTransform root = (RectTransform)hudPanel.transform;

        levelText = CreateText(root, "Level", 44, Vector2.zero, faded);
        levelText.alignment = TextAnchor.MiddleLeft;
        RectTransform levelRect = levelText.rectTransform;
        levelRect.anchorMin = levelRect.anchorMax = levelRect.pivot = new Vector2(0f, 1f);
        levelRect.anchoredPosition = new Vector2(50f, -50f);
        levelRect.sizeDelta = new Vector2(500f, 120f);

        // Pause button in the top-right corner
        GameObject pause = new GameObject("Pause Button", typeof(RectTransform), typeof(Image), typeof(Button));
        pause.transform.SetParent(root, false);
        Image pauseImage = pause.GetComponent<Image>();
        pauseImage.sprite = SpriteFactory.Circle;
        pauseImage.color = new Color(1f, 1f, 1f, 0.14f);
        RectTransform pauseRect = (RectTransform)pause.transform;
        pauseRect.anchorMin = pauseRect.anchorMax = pauseRect.pivot = new Vector2(1f, 1f);
        pauseRect.anchoredPosition = new Vector2(-40f, -40f);
        pauseRect.sizeDelta = new Vector2(130f, 130f);
        SetUpButton(pause.GetComponent<Button>(), game.Pause);
        Text icon = CreateText(pauseRect, "Icon", 52, Vector2.zero, new Color(1f, 1f, 1f, 0.85f));
        icon.text = "II";
        Stretch(icon.rectTransform);

        levelUpText = CreateText(root, "Level Up", 130, new Vector2(0f, 500f), Theme.Highlight);
        levelUpText.enabled = false;
    }

    private void BuildPause()
    {
        pausePanel = CreatePanel("Pause");
        RectTransform root = (RectTransform)pausePanel.transform;

        CreateText(root, "Title", 150, new Vector2(0f, 420f), Theme.Text).text = "PAUSED";
        pauseScore = CreateText(root, "Score", 56, new Vector2(0f, 260f), faded);

        CreateButton(root, "RESUME", new Vector2(0f, 20f), new Vector2(620f, 180f), true, game.Resume);
        CreateButton(root, "RESTART", new Vector2(0f, -190f), new Vector2(620f, 150f), false, game.Restart);
        CreateButton(root, "MENU", new Vector2(0f, -370f), new Vector2(620f, 150f), false, game.GoToMenu);
    }

    private void BuildGameOver()
    {
        gameOverPanel = CreatePanel("Game Over");
        RectTransform root = (RectTransform)gameOverPanel.transform;

        CreateText(root, "Title", 140, new Vector2(0f, 500f), Theme.Danger).text = "GAME OVER";
        gameOverScore = CreateText(root, "Score", 280, new Vector2(0f, 250f), Theme.Text);
        gameOverScore.rectTransform.sizeDelta = new Vector2(1000f, 320f);
        gameOverBest = CreateText(root, "Best", 52, new Vector2(0f, 50f), faded);

        CreateButton(root, "PLAY AGAIN", new Vector2(0f, -170f), new Vector2(620f, 180f), true, game.Restart);
        CreateButton(root, "MENU", new Vector2(0f, -370f), new Vector2(620f, 150f), false, game.GoToMenu);
    }

    private void BuildCountdown()
    {
        countdownPanel = CreatePanel("Countdown");
        RectTransform root = (RectTransform)countdownPanel.transform;

        countdownText = CreateText(root, "Number", 320, new Vector2(0f, 200f), Theme.Highlight);
        countdownText.rectTransform.sizeDelta = new Vector2(1000f, 400f);
        countdownHint = CreateText(root, "Hint", 46, new Vector2(0f, -60f), faded);
        countdownHint.fontStyle = FontStyle.Normal;
        countdownHint.text = "Drag anywhere to move";
    }

    private void ToggleSound()
    {
        Settings.SoundOn = !Settings.SoundOn;
        RefreshSettings();
    }

    private void ToggleVibration()
    {
        Settings.VibrationOn = !Settings.VibrationOn;
        RefreshSettings();
        Settings.Vibrate();
    }

    // ---------- Helpers ----------

    private CanvasGroup CreatePanel(string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
        go.transform.SetParent(safeArea, false);
        Stretch((RectTransform)go.transform);
        go.SetActive(false);
        return go.GetComponent<CanvasGroup>();
    }

    private Text CreateButton(RectTransform parent, string label, Vector2 position, Vector2 size, bool primary, System.Action onClick)
    {
        GameObject go = new GameObject(label + " Button", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        Image image = go.GetComponent<Image>();
        image.sprite = SpriteFactory.RoundedBox;
        image.type = Image.Type.Sliced;
        image.color = primary ? Theme.ButtonPrimary : Theme.ButtonSecondary;

        RectTransform rect = (RectTransform)go.transform;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        SetUpButton(go.GetComponent<Button>(), onClick);

        Text text = CreateText(rect, "Label", primary ? 76 : 50, Vector2.zero, primary ? Theme.ButtonPrimaryText : Theme.Text);
        text.text = label;
        Stretch(text.rectTransform);
        return text;
    }

    private static void SetUpButton(Button button, System.Action onClick)
    {
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.selectedColor = Color.white;
        colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
        colors.fadeDuration = 0.05f;
        button.colors = colors;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.gameObject.AddComponent<ButtonPress>();
        button.onClick.AddListener(() =>
        {
            SoundFX.Play(Sound.Click);
            onClick();
        });
    }

    private Canvas CreateCanvas(string name, int order)
    {
        GameObject go = new GameObject(name);
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = order;
        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0f;
        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private Text CreateText(Transform parent, string name, int size, Vector2 position, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Text text = go.AddComponent<Text>();
        text.font = Font;
        text.fontSize = size;
        text.fontStyle = FontStyle.Bold;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        text.rectTransform.anchoredPosition = position;
        text.rectTransform.sizeDelta = new Vector2(1000f, 200f);
        return text;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    // Keep the Safe Area object matched to the phone's safe area (notches, cutouts, home bar)
    private void FitSafeArea()
    {
        Rect safe = Screen.safeArea;
        if (safe == lastSafeArea || Screen.width == 0 || Screen.height == 0) return;
        lastSafeArea = safe;

        safeArea.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
        safeArea.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
        safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
    }
}

// Makes buttons shrink a little while pressed, so taps feel responsive
public class ButtonPress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    private bool pressed;

    public void OnPointerDown(PointerEventData eventData) => pressed = true;
    public void OnPointerUp(PointerEventData eventData) => pressed = false;
    public void OnPointerExit(PointerEventData eventData) => pressed = false;

    void Update()
    {
        float target = pressed ? 0.93f : 1f;
        transform.localScale = Vector3.one * Mathf.MoveTowards(transform.localScale.x, target, Time.unscaledDeltaTime * 3f);
    }

    void OnDisable()
    {
        pressed = false;
        transform.localScale = Vector3.one;
    }
}
