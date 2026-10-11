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

    // The game font only has one weight, so faux bold is only used with Unity's fallback font
    public FontStyle FontStyle => Font != null && Font.name.StartsWith("LegacyRuntime") ? FontStyle.Bold : FontStyle.Normal;

    private static readonly Color ShadowColor = new Color(0.05f, 0.02f, 0.15f, 1f);

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
    private Text musicLabel;
    private Text vibrationLabel;
    private Text modeLabel;
    private Image[] modeArrows;

    // HUD
    private Text scoreText;
    private Text levelText;
    private Text hudScore;
    private Text hudBest;
    private int bestScore;
    private Text levelUpText;
    private GameObject[] powerUpChips;
    private Image[] powerUpRings;
    private CanvasGroup[] powerUpChipGroups;
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
        Font = Resources.Load<Font>("Fonts/LilitaOne-Regular");
        if (Font == null) Font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Buttons need an EventSystem to receive touches
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            SceneObjects.GetOrCreate(transform, "EventSystem", out GameObject eventSystem, typeof(EventSystem));
            SceneObjects.GetOrAdd<InputSystemUIInputModule>(eventSystem);
        }
        InputSystemUIInputModule inputModule = FindFirstObjectByType<InputSystemUIInputModule>();
        if (Application.isPlaying && inputModule != null && inputModule.actionsAsset == null) inputModule.AssignDefaultActions();

        BuildScoreCanvas();

        HudCanvas = CreateCanvas("HUD Canvas", 10);
        if (SceneObjects.GetOrCreate(HudCanvas.transform, "Dim", out GameObject dimObject, typeof(RectTransform), typeof(Image)))
        {
            Image newDim = dimObject.GetComponent<Image>();
            newDim.color = new Color(0.02f, 0.01f, 0.08f, 0f);
            newDim.raycastTarget = false;
            Stretch(newDim.rectTransform);
        }
        dim = dimObject.GetComponent<Image>();

        // Everything else goes inside the safe area so notches and rounded corners never cover it
        if (SceneObjects.GetOrCreate(HudCanvas.transform, "Safe Area", out GameObject safeAreaObject, typeof(RectTransform)))
        {
            Stretch((RectTransform)safeAreaObject.transform);
        }
        safeArea = (RectTransform)safeAreaObject.transform;

        BuildMenu();
        BuildHud();
        BuildPause();
        BuildGameOver();
        BuildCountdown();
        panels = new[] { menuPanel, hudPanel, pausePanel, gameOverPanel, countdownPanel };

        RefreshSettings();
    }

    // ---------- Showing screens ----------

    public void ShowMenu(int best)
    {
        SetMenuBest(best);
        Show(menuPanel, 0.55f);
    }

    private void SetMenuBest(int best)
    {
        menuBest.text = best > 0 ? Difficulty.Current.Name + " BEST  " + best : "";
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
        string mode = Difficulty.Current.Name;
        gameOverBest.text = newBest ? "NEW " + mode + " BEST!" : mode + "    BEST  " + best + "    LEVEL  " + level;
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
        hudScore.text = score.ToString();
        if (punch) scorePunch = 1f;

        // Turns gold once you beat your best
        bool beatingBest = bestScore > 0 && score > bestScore;
        hudScore.color = beatingBest ? Theme.Highlight : Theme.Text;
        hudBest.text = beatingBest ? "NEW BEST!" : bestScore > 0 ? "BEST  " + bestScore : "";
        hudBest.color = beatingBest ? Theme.Highlight : faded;
    }

    // The best score to compare against during this round
    public void SetBest(int best)
    {
        bestScore = best;
    }

    public void SetScoreVisible(bool visible)
    {
        scoreText.enabled = visible;
    }

    public void SetLevel(int level, Color color, bool announce)
    {
        levelText.text = "LEVEL  " + level;
        levelText.color = new Color(color.r, color.g, color.b, 0.9f);
        if (!announce) return;

        levelUpText.text = "LEVEL " + level;
        levelUpText.color = color;
        levelUpTimer = 1.6f;
    }

    // Shows a power-up's timer ring in the HUD. 'fraction' is how much time is left (0 hides it);
    // the ring blinks during the last couple of seconds.
    public void SetPowerUpTimer(PowerUpType type, float fraction, float secondsLeft)
    {
        int i = (int)type;
        bool visible = fraction > 0f;
        if (powerUpChips[i].activeSelf != visible) powerUpChips[i].SetActive(visible);
        if (!visible) return;

        powerUpRings[i].fillAmount = fraction;
        powerUpChipGroups[i].alpha = secondsLeft < 2f ? 0.45f + 0.55f * Mathf.Abs(Mathf.Cos(Time.time * 9f)) : 1f;
    }

    public void RefreshSettings()
    {
        Difficulty mode = Difficulty.Current;
        modeLabel.text = mode.Name + "\n<size=28>" + mode.Description + "</size>";
        modeLabel.color = mode.Color;
        foreach (Image arrow in modeArrows) arrow.color = new Color(mode.Color.r, mode.Color.g, mode.Color.b, 0.75f);
        SetOutline(modeLabel.transform.parent, new Color(mode.Color.r, mode.Color.g, mode.Color.b, 0.7f));

        soundLabel.text = "SOUND " + OnOff(Settings.SoundOn);
        musicLabel.text = "MUSIC " + OnOff(Settings.MusicOn);
        vibrationLabel.text = "VIBRATE " + OnOff(Settings.VibrationOn);
        SetToggleLook(soundLabel, Settings.SoundOn);
        SetToggleLook(musicLabel, Settings.MusicOn);
        SetToggleLook(vibrationLabel, Settings.VibrationOn);
    }

    private static string OnOff(bool on) => on ? "<color=#FFD23F>ON</color>" : "OFF";

    // Toggles that are on get a gold outline; off ones look dimmer
    private static void SetToggleLook(Text label, bool on)
    {
        Transform button = label.transform.parent;
        button.GetComponent<Image>().color = on ? Theme.ButtonSecondary : Theme.ButtonSecondaryOff;
        SetOutline(button, on ? new Color(Theme.Highlight.r, Theme.Highlight.g, Theme.Highlight.b, 0.6f) : new Color(1f, 1f, 1f, 0.06f));
        label.color = on ? Theme.Text : new Color(1f, 1f, 1f, 0.4f);
        Transform icon = button.Find("Icon");
        if (icon != null) icon.GetComponent<Image>().color = on ? Theme.Text : new Color(1f, 1f, 1f, 0.3f);
    }

    private static void SetOutline(Transform button, Color color)
    {
        Transform outline = button.Find("Outline");
        if (outline != null) outline.GetComponent<Image>().color = color;
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

        // Title gently bobs up and down, and the mode arrows nudge outwards to invite a tap
        menuTitle.anchoredPosition = new Vector2(0f, 470f + Mathf.Sin(Time.unscaledTime * 2f) * 12f);
        float nudge = Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3f)) * 8f;
        modeArrows[0].rectTransform.anchoredPosition = new Vector2(-255f - nudge, 0f);
        modeArrows[1].rectTransform.anchoredPosition = new Vector2(255f + nudge, 0f);

        scorePunch = Mathf.MoveTowards(scorePunch, 0f, dt * 4f);
        scoreText.transform.localScale = Vector3.one * (1f + 0.25f * scorePunch);
        hudScore.transform.localScale = Vector3.one * (1f + 0.3f * scorePunch);

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
        Canvas canvas = CreateCanvas("Score Canvas", 0, c =>
        {
            c.renderMode = RenderMode.ScreenSpaceCamera;
            c.planeDistance = 5f;
            c.sortingLayerName = "Background";
            c.sortingOrder = 3;
        });
        canvas.worldCamera = cam;
        scoreText = CreateText(canvas.transform, "Score", 420, new Vector2(0f, 250f), new Color(1f, 1f, 1f, 0.1f), t => t.enabled = false, false);
    }

    private void BuildMenu()
    {
        // The menu is the one screen left showing in the scene, since it's what the game starts on
        menuPanel = CreatePanel("Menu", true);
        RectTransform root = (RectTransform)menuPanel.transform;

        menuTitle = CreateText(root, "Title", 230, new Vector2(0f, 470f), Theme.Highlight, t =>
        {
            t.text = "KEEP\nIT UP";
            t.lineSpacing = 0.8f;
            t.rectTransform.sizeDelta = new Vector2(1000f, 540f);
            AddShadow(t, 16f, 0.55f);
        }, false).rectTransform;

        menuBest = CreateText(root, "Best", 54, new Vector2(0f, 180f), faded);

        // Tap to cycle through the difficulty modes
        modeLabel = CreateButton(root, "MODE", new Vector2(0f, 55f), new Vector2(620f, 130f), false, CycleMode, fontSize: 48);
        Transform modeButton = modeLabel.transform.parent;
        modeArrows = new[]
        {
            CreateIcon(modeButton, "Arrow Left", SpriteFactory.Shape.PlayIcon, new Vector2(-255f, 0f), 56f, Color.white, i => i.rectTransform.localEulerAngles = new Vector3(0f, 0f, 180f)),
            CreateIcon(modeButton, "Arrow Right", SpriteFactory.Shape.PlayIcon, new Vector2(255f, 0f), 56f, Color.white),
        };

        CreateButton(root, "PLAY", new Vector2(0f, -130f), new Vector2(620f, 190f), true, game.Play, SpriteFactory.Shape.PlayIcon);
        Vector2 toggleSize = new Vector2(300f, 140f);
        soundLabel = CreateToggle(root, "SOUND", new Vector2(-325f, -360f), toggleSize, ToggleSound, SpriteFactory.Shape.SoundIcon);
        musicLabel = CreateToggle(root, "MUSIC", new Vector2(0f, -360f), toggleSize, ToggleMusic, SpriteFactory.Shape.MusicIcon);
        vibrationLabel = CreateToggle(root, "VIBRATE", new Vector2(325f, -360f), toggleSize, ToggleVibration, SpriteFactory.Shape.VibrateIcon);

        CreateText(root, "Hint", 40, new Vector2(0f, -525f), new Color(1f, 1f, 1f, 0.45f), t => t.text = "Drag anywhere to move the paddle");

        Text version = CreateText(root, "Version", 30, Vector2.zero, new Color(1f, 1f, 1f, 0.25f), t =>
        {
            RectTransform versionRect = t.rectTransform;
            versionRect.anchorMin = versionRect.anchorMax = versionRect.pivot = new Vector2(0.5f, 0f);
            versionRect.anchoredPosition = new Vector2(0f, 30f);
            versionRect.sizeDelta = new Vector2(400f, 60f);
        }, false);
        version.text = "v" + Application.version;
    }

    private void BuildHud()
    {
        hudPanel = CreatePanel("HUD");
        RectTransform root = (RectTransform)hudPanel.transform;

        levelText = CreateText(root, "Level", 48, Vector2.zero, faded, t =>
        {
            t.alignment = TextAnchor.MiddleLeft;
            RectTransform levelRect = t.rectTransform;
            levelRect.anchorMin = levelRect.anchorMax = levelRect.pivot = new Vector2(0f, 1f);
            levelRect.anchoredPosition = new Vector2(50f, -50f);
            levelRect.sizeDelta = new Vector2(500f, 120f);
        });

        // Score counter at the top centre, with the best score under it
        hudScore = CreateText(root, "Score", 110, Vector2.zero, Theme.Text, t =>
        {
            RectTransform scoreRect = t.rectTransform;
            scoreRect.anchorMin = scoreRect.anchorMax = new Vector2(0.5f, 1f);
            scoreRect.anchoredPosition = new Vector2(0f, -105f);
            scoreRect.sizeDelta = new Vector2(400f, 140f);
            AddShadow(t, 8f, 0.5f);
        }, false);
        hudBest = CreateText(root, "Best", 34, Vector2.zero, faded, t =>
        {
            RectTransform bestRect = t.rectTransform;
            bestRect.anchorMin = bestRect.anchorMax = new Vector2(0.5f, 1f);
            bestRect.anchoredPosition = new Vector2(0f, -190f);
            bestRect.sizeDelta = new Vector2(400f, 50f);
        });

        BuildPowerUpTimers(root);

        // Pause button in the top-right corner
        bool created = SceneObjects.GetOrCreate(root, "Pause Button", out GameObject pause, typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform pauseRect = (RectTransform)pause.transform;
        if (created)
        {
            // Invisible, but still catches taps around the icon
            Image pauseImage = pause.GetComponent<Image>();
            pauseImage.sprite = SpriteFactory.Circle;
            pauseImage.color = Color.clear;
            pauseRect.anchorMin = pauseRect.anchorMax = pauseRect.pivot = new Vector2(1f, 1f);
            pauseRect.anchoredPosition = new Vector2(-40f, -40f);
            pauseRect.sizeDelta = new Vector2(130f, 130f);
            StyleButton(pause.GetComponent<Button>());
        }
        ListenToButton(pause.GetComponent<Button>(), game.Pause);
        CreateIcon(pauseRect, "Icon", SpriteFactory.Shape.PauseIcon, Vector2.zero, 110f, new Color(1f, 1f, 1f, 0.9f), i => AddShadow(i, 5f, 0.5f));

        levelUpText = CreateText(root, "Level Up", 140, new Vector2(0f, 500f), Theme.Highlight, t =>
        {
            t.enabled = false;
            AddShadow(t, 10f, 0.5f);
        }, false);
    }

    // A row of round timers under the level, one per active power-up: its icon inside a ring that drains
    private void BuildPowerUpTimers(RectTransform root)
    {
        if (SceneObjects.GetOrCreate(root, "Power Up Timers", out GameObject row, typeof(RectTransform), typeof(HorizontalLayoutGroup)))
        {
            RectTransform rowRect = (RectTransform)row.transform;
            rowRect.anchorMin = rowRect.anchorMax = rowRect.pivot = new Vector2(0f, 1f);
            rowRect.anchoredPosition = new Vector2(44f, -235f);
            rowRect.sizeDelta = new Vector2(800f, 100f);
            HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 18f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = layout.childControlHeight = false;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
        }

        int count = System.Enum.GetValues(typeof(PowerUpType)).Length;
        powerUpChips = new GameObject[count];
        powerUpRings = new Image[count];
        powerUpChipGroups = new CanvasGroup[count];
        for (int i = 0; i < count; i++)
        {
            PowerUpType type = (PowerUpType)i;
            Color color = Theme.PowerUpColors[i];
            if (SceneObjects.GetOrCreate(row.transform, type + " Timer", out GameObject chip, typeof(RectTransform), typeof(Image), typeof(CanvasGroup)))
            {
                Image background = chip.GetComponent<Image>();
                background.sprite = SpriteFactory.Circle;
                background.color = new Color(0.05f, 0.02f, 0.15f, 0.6f);
                background.raycastTarget = false;
                ((RectTransform)chip.transform).sizeDelta = new Vector2(96f, 96f);
                chip.SetActive(false);
            }

            Image ring = CreateIcon(chip.transform, "Ring", SpriteFactory.Shape.Ring, Vector2.zero, 96f, color, r =>
            {
                r.type = Image.Type.Filled;
                r.fillMethod = Image.FillMethod.Radial360;
                r.fillOrigin = (int)Image.Origin360.Top;
                r.fillClockwise = false;
            });
            CreateIcon(chip.transform, "Icon", SpriteFactory.PowerUpIconShape(type), Vector2.zero, 76f, color);

            powerUpChips[i] = chip;
            powerUpRings[i] = ring;
            powerUpChipGroups[i] = chip.GetComponent<CanvasGroup>();
        }
    }

    private void BuildPause()
    {
        pausePanel = CreatePanel("Pause");
        RectTransform root = (RectTransform)pausePanel.transform;

        CreateText(root, "Title", 160, new Vector2(0f, 420f), Theme.Text, t =>
        {
            t.text = "PAUSED";
            AddShadow(t, 12f, 0.5f);
        }, false);
        pauseScore = CreateText(root, "Score", 56, new Vector2(0f, 260f), faded);

        CreateButton(root, "RESUME", new Vector2(0f, 20f), new Vector2(620f, 180f), true, game.Resume, SpriteFactory.Shape.PlayIcon);
        CreateButton(root, "RESTART", new Vector2(0f, -190f), new Vector2(620f, 150f), false, game.Restart, SpriteFactory.Shape.RestartIcon);
        CreateButton(root, "MENU", new Vector2(0f, -370f), new Vector2(620f, 150f), false, game.GoToMenu, SpriteFactory.Shape.HomeIcon);
    }

    private void BuildGameOver()
    {
        gameOverPanel = CreatePanel("Game Over");
        RectTransform root = (RectTransform)gameOverPanel.transform;

        CreateText(root, "Title", 150, new Vector2(0f, 500f), Theme.Danger, t =>
        {
            t.text = "GAME OVER";
            AddShadow(t, 12f, 0.55f);
        }, false);
        gameOverScore = CreateText(root, "Score", 300, new Vector2(0f, 250f), Theme.Text, t =>
        {
            t.rectTransform.sizeDelta = new Vector2(1000f, 340f);
            AddShadow(t, 14f, 0.5f);
        }, false);
        gameOverBest = CreateText(root, "Best", 50, new Vector2(0f, 50f), faded);

        CreateButton(root, "PLAY AGAIN", new Vector2(0f, -170f), new Vector2(620f, 180f), true, game.Restart, SpriteFactory.Shape.RestartIcon);
        CreateButton(root, "MENU", new Vector2(0f, -370f), new Vector2(620f, 150f), false, game.GoToMenu, SpriteFactory.Shape.HomeIcon);
    }

    private void BuildCountdown()
    {
        countdownPanel = CreatePanel("Countdown");
        RectTransform root = (RectTransform)countdownPanel.transform;

        countdownText = CreateText(root, "Number", 340, new Vector2(0f, 200f), Theme.Highlight, t =>
        {
            t.rectTransform.sizeDelta = new Vector2(1000f, 420f);
            AddShadow(t, 16f, 0.5f);
        }, false);
        countdownHint = CreateText(root, "Hint", 46, new Vector2(0f, -60f), faded, t => t.text = "Drag anywhere to move");
    }

    private void CycleMode()
    {
        Settings.Mode = (GameMode)(((int)Settings.Mode + 1) % Difficulty.All.Length);
        RefreshSettings();
        SetMenuBest(Settings.BestScore);
    }

    private void ToggleSound()
    {
        Settings.SoundOn = !Settings.SoundOn;
        RefreshSettings();
    }

    private void ToggleMusic()
    {
        Settings.MusicOn = !Settings.MusicOn;
        RefreshSettings();
    }

    private void ToggleVibration()
    {
        Settings.VibrationOn = !Settings.VibrationOn;
        RefreshSettings();
        Haptics.Medium();
    }

    // ---------- Helpers ----------
    // The Create helpers below reuse an existing object with that name, or create and set it up.
    // 'setUp' only runs on a newly created object, so changes made in the Inspector are kept.

    private CanvasGroup CreatePanel(string name, bool visible = false)
    {
        if (SceneObjects.GetOrCreate(safeArea, name, out GameObject go, typeof(RectTransform), typeof(CanvasGroup)))
        {
            Stretch((RectTransform)go.transform);
            go.SetActive(visible);
        }
        return go.GetComponent<CanvasGroup>();
    }

    // A rounded button with a label, and optionally an icon to the left of it
    private Text CreateButton(RectTransform parent, string label, Vector2 position, Vector2 size, bool primary, System.Action onClick,
                              SpriteFactory.Shape? icon = null, int fontSize = 0)
    {
        RectTransform rect = CreateButtonBase(parent, label + " Button", position, size, primary, onClick);

        if (fontSize == 0) fontSize = primary ? 80 : 54;
        Color textColor = primary ? Theme.ButtonPrimaryText : Theme.Text;
        Text text = CreateText(rect, "Label", fontSize, Vector2.zero, textColor, t =>
        {
            t.text = label;
            Stretch(t.rectTransform);
        }, !primary);

        if (icon.HasValue)
        {
            // Centre the icon and label together as one group
            float iconSize = fontSize * 1.05f;
            const float gap = 18f;
            float labelWidth = text.preferredWidth;
            float left = -(iconSize + gap + labelWidth) / 2f;
            CreateIcon(rect, "Icon", icon.Value, new Vector2(left + iconSize / 2f, 0f), iconSize, textColor, i =>
            {
                text.rectTransform.anchorMin = text.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                text.rectTransform.sizeDelta = new Vector2(labelWidth + 20f, size.y);
                text.rectTransform.anchoredPosition = new Vector2(left + iconSize + gap + labelWidth / 2f, 0f);
            });
        }
        return text;
    }

    // A settings toggle: icon on top, "NAME ON/OFF" underneath
    private Text CreateToggle(RectTransform parent, string label, Vector2 position, Vector2 size, System.Action onClick, SpriteFactory.Shape icon)
    {
        RectTransform rect = CreateButtonBase(parent, label + " Button", position, size, false, onClick);
        CreateIcon(rect, "Icon", icon, new Vector2(0f, 22f), 74f, Theme.Text);
        return CreateText(rect, "Label", 30, new Vector2(0f, -40f), Theme.Text, t => t.rectTransform.sizeDelta = new Vector2(size.x, 50f));
    }

    private RectTransform CreateButtonBase(RectTransform parent, string name, Vector2 position, Vector2 size, bool primary, System.Action onClick)
    {
        bool created = SceneObjects.GetOrCreate(parent, name, out GameObject go, typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform rect = (RectTransform)go.transform;
        if (created)
        {
            Image image = go.GetComponent<Image>();
            image.sprite = SpriteFactory.RoundedBox;
            image.type = Image.Type.Sliced;
            // Rounder corners on the big buttons, almost a pill
            image.pixelsPerUnitMultiplier = primary ? 0.55f : 0.8f;
            image.color = primary ? Theme.ButtonPrimary : Theme.ButtonSecondary;
            AddShadow(image, primary ? 12f : 9f, primary ? Theme.ButtonPrimaryEdge : Theme.ButtonSecondaryEdge);
            AddShadow(image, 6f, new Color(ShadowColor.r, ShadowColor.g, ShadowColor.b, 0.35f));

            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            StyleButton(go.GetComponent<Button>());

            // Glossy highlight across the top half
            CreateSliced(rect, "Gloss", SpriteFactory.RoundedBox, new Color(1f, 1f, 1f, primary ? 0.3f : 0.07f), image.pixelsPerUnitMultiplier,
                         new Vector2(0f, 0.5f), Vector2.one, new Vector2(12f, 0f), new Vector2(-12f, -8f));
            if (!primary)
            {
                CreateSliced(rect, "Outline", SpriteFactory.RoundedOutline, Theme.ButtonOutline, image.pixelsPerUnitMultiplier,
                             Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }
        }
        ListenToButton(go.GetComponent<Button>(), onClick);
        return rect;
    }

    private static void StyleButton(Button button)
    {
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.selectedColor = Color.white;
        colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
        colors.fadeDuration = 0.05f;
        button.colors = colors;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        SceneObjects.GetOrAdd<ButtonPress>(button.gameObject);
    }

    // Click handlers aren't saved in the scene, so they're connected every time the game starts
    private static void ListenToButton(Button button, System.Action onClick)
    {
        button.onClick.AddListener(() =>
        {
            SoundFX.Play(Sound.Click);
            onClick();
        });
    }

    private Canvas CreateCanvas(string name, int order, System.Action<Canvas> setUp = null)
    {
        if (SceneObjects.GetOrCreate(transform, name, out GameObject go))
        {
            Canvas newCanvas = go.AddComponent<Canvas>();
            newCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            newCanvas.sortingOrder = order;
            CanvasScaler scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0f;
            go.AddComponent<GraphicRaycaster>();
            setUp?.Invoke(newCanvas);
        }
        return SceneObjects.GetOrAdd<Canvas>(go);
    }

    private Text CreateText(Transform parent, string name, int size, Vector2 position, Color color, System.Action<Text> setUp = null, bool shadow = true)
    {
        if (SceneObjects.GetOrCreate(parent, name, out GameObject go, typeof(RectTransform)))
        {
            Text newText = go.AddComponent<Text>();
            newText.font = Font;
            newText.fontSize = size;
            newText.fontStyle = FontStyle;
            newText.color = color;
            newText.alignment = TextAnchor.MiddleCenter;
            newText.horizontalOverflow = HorizontalWrapMode.Overflow;
            newText.verticalOverflow = VerticalWrapMode.Overflow;
            newText.raycastTarget = false;
            newText.rectTransform.anchoredPosition = position;
            newText.rectTransform.sizeDelta = new Vector2(1000f, 200f);
            if (shadow) AddShadow(newText, Mathf.Max(3f, size * 0.06f), 0.4f);
            setUp?.Invoke(newText);
        }
        return SceneObjects.GetOrAdd<Text>(go);
    }

    private static Image CreateIcon(Transform parent, string name, SpriteFactory.Shape shape, Vector2 position, float size, Color color, System.Action<Image> setUp = null)
    {
        if (SceneObjects.GetOrCreate(parent, name, out GameObject go, typeof(RectTransform), typeof(Image)))
        {
            Image image = go.GetComponent<Image>();
            image.sprite = SpriteFactory.Icon(shape);
            image.color = color;
            image.raycastTarget = false;
            image.rectTransform.anchoredPosition = position;
            image.rectTransform.sizeDelta = new Vector2(size, size);
            setUp?.Invoke(image);
        }
        return go.GetComponent<Image>();
    }

    // A soft drop shadow under text or a button, for a bit of depth
    private static void AddShadow(Graphic graphic, float distance, float alpha)
    {
        AddShadow(graphic, distance, new Color(ShadowColor.r, ShadowColor.g, ShadowColor.b, alpha));
    }

    private static void AddShadow(Graphic graphic, float distance, Color color)
    {
        Shadow shadow = graphic.gameObject.AddComponent<Shadow>();
        shadow.effectColor = color;
        shadow.effectDistance = new Vector2(0f, -distance);
    }

    // A 9-sliced image stretched inside its parent between the given anchors and offsets
    private static void CreateSliced(RectTransform parent, string name, Sprite sprite, Color color, float cornerScale,
                                     Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        if (!SceneObjects.GetOrCreate(parent, name, out GameObject go, typeof(RectTransform), typeof(Image))) return;

        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = cornerScale;
        image.color = color;
        image.raycastTarget = false;
        RectTransform rect = image.rectTransform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
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
