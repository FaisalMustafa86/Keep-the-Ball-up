using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
#if UNITY_EDITOR
using UnityEngine.InputSystem.EnhancedTouch;
#endif

// Runs the game: builds the arena, paddle, ball and UI when the app starts, keeps score,
// makes the game harder as you go (and per difficulty mode), handles power-ups, and moves between the menu / countdown / playing / paused / game over screens.
// Its objects are saved in the scene by Tools > Keep the Ball Up > Build Scene, so they can be seen and edited in the Editor.
// Anything missing is created when the game starts, and if there is no GameManager at all, one is created automatically.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Difficulty")]
    // Points needed for each new level
    [SerializeField] private int pointsPerLevel = 10;
    // The paddle shrinks a bit every level, down to this width
    [SerializeField] private float minPaddleWidth = 1.4f;
    [SerializeField] private float paddleShrinkPerLevel = 0.15f;
    // Each perfect hit in a row is worth one more point, up to this many
    [SerializeField] private int maxComboPoints = 5;

    private enum State { Menu, Countdown, Playing, Paused, GameOver }

    // Bump this when the UI or power-up objects change design, so older copies saved in the scene get replaced
    private const int DesignVersion = 4;
    [SerializeField, HideInInspector] private int designVersion;

    private State state;
    private int score;
    private int level;
    private int perfectStreak;
    private Coroutine countdown;

    private Camera cam;
    private Arena arena;
    private PlayerController player;
    private Ball ball;
    private Effects effects;
    private CameraShake cameraShake;
    private GameUI ui;
    private Music music;
    private PowerUps powerUps;

    public bool IsPlaying => state == State.Playing;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateIfMissing()
    {
        if (FindFirstObjectByType<GameManager>() == null)
        {
            new GameObject("GameManager").AddComponent<GameManager>();
        }
    }

    void Awake()
    {
        Instance = this;

        // Phone settings: portrait only, smooth 60 FPS, and don't let the screen turn off mid-game
        Screen.orientation = ScreenOrientation.Portrait;
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

#if UNITY_EDITOR
        // Lets you test in the Editor: the mouse acts like a finger
        TouchSimulation.Enable();
#endif

        Build();
        GoToMenu();
    }

    // Finds the game's objects under this GameManager and creates any that are missing.
    // The editor's Build Scene also calls this outside Play mode, passing the Game view size
    // because Screen.width/height aren't reliable there.
    public void Build(Vector2Int? editorScreenSize = null)
    {
        if (designVersion < DesignVersion)
        {
            ReplaceOutdatedObjects();
            designVersion = DesignVersion;
        }

        SoundFX.Init(gameObject);
        music = SceneObjects.GetOrAdd<Music>(gameObject);
        music.Init();
        BuildWorld(editorScreenSize);

        ui = SceneObjects.GetOrAdd<GameUI>(gameObject);
        ui.Init(this, cam);
        effects = SceneObjects.GetOrAdd<Effects>(gameObject);
        effects.Init(cam, ui.HudCanvas, ui.Font);
        powerUps = SceneObjects.GetOrAdd<PowerUps>(gameObject);
        powerUps.Init(this, player, ball, arena, effects, ui, cam);

        // In the Editor, show the start of a round: ball on the paddle, level 1
        if (!Application.isPlaying) ResetRound();
    }

    // The menus, HUD and power-ups saved in the scene are from an older design: remove them so they're rebuilt.
    // (Only these get replaced; the arena, paddle and ball keep any changes made in the Inspector.)
    private void ReplaceOutdatedObjects()
    {
        foreach (string name in new[] { "Score Canvas", "HUD Canvas", "Power Ups" })
        {
            Transform old = transform.Find(name);
            if (old == null) continue;

            // Detach first so the rebuild below doesn't find it before it's destroyed
            old.SetParent(null);
            if (Application.isPlaying) Destroy(old.gameObject);
            else DestroyImmediate(old.gameObject);
        }
    }

    private void BuildWorld(Vector2Int? editorScreenSize)
    {
        // Old stand-ins from before the game built its own objects. Build Scene deletes them from the scene;
        // if it hasn't been run, remove them for this play session so they don't cover the game.
        if (Application.isPlaying)
        {
            foreach (GameObject root in gameObject.scene.GetRootGameObjects())
            {
                if (root.name == "Background" || root.name == "player") Destroy(root);
            }
        }

        cam = Camera.main;
        cam.backgroundColor = Theme.BackgroundTop;
        cameraShake = SceneObjects.GetOrAdd<CameraShake>(cam.gameObject);

        SceneObjects.GetOrCreate(transform, "Arena", out arena);
        arena.Init(cam, editorScreenSize);

        SceneObjects.GetOrCreate(transform, "Paddle", out player);
        player.Init();

        SceneObjects.GetOrCreate(transform, "Ball", out ball);
        ball.Init(player);
    }

    void Update()
    {
        // The Android back button arrives as the Escape key
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) OnBackButton();
    }

    private void OnBackButton()
    {
        switch (state)
        {
            case State.Menu: Application.Quit(); break;
            case State.Countdown:
            case State.Playing: Pause(); break;
            case State.Paused: Resume(); break;
            case State.GameOver: GoToMenu(); break;
        }
    }

    // Pause automatically when the player leaves the app or gets a phone call
    void OnApplicationPause(bool paused)
    {
        if (paused) Pause();
    }

    // ---------- Game flow (also called by the UI buttons) ----------

    public void GoToMenu()
    {
        StopAllCoroutines();
        countdown = null;
        state = State.Menu;
        ResetRound();
        player.InputEnabled = false;
        ui.SetScoreVisible(false);
        ui.ShowMenu(Settings.BestScore);
        music.SetMood(Music.Mood.Menu);
    }

    public void Play()
    {
        StopAllCoroutines();
        ResetRound();
        countdown = StartCoroutine(Countdown(true));
    }

    public void Restart()
    {
        Play();
    }

    public void Pause()
    {
        if (state != State.Playing && state != State.Countdown) return;

        StopCountdown();
        state = State.Paused;
        Time.timeScale = 0f;
        player.InputEnabled = false;
        ui.ShowPause(score);
        music.SetMood(Music.Mood.Muffled);
    }

    public void Resume()
    {
        if (state != State.Paused) return;

        // Short countdown so the player has time to put their finger back on the screen
        ball.Held = ball.IsLaunched;
        countdown = StartCoroutine(Countdown(!ball.IsLaunched));
    }

    private void ResetRound()
    {
        Time.timeScale = 1f;
        score = 0;
        level = 1;
        perfectStreak = 0;

        Difficulty difficulty = Difficulty.Current;
        ball.SetDifficulty(difficulty.BallSpeed, difficulty.SpeedGain);
        powerUps.ResetAll();
        player.SetWidth(BasePaddleWidth());
        player.ResetPosition();
        ball.SpeedMultiplier = 1f;
        ball.ResetBall();
        ui.SetBest(Settings.BestScore);
        ui.SetScore(0, false);
        ui.SetLevel(level, LevelColor(level), false);
        music.SetLevel(level);
    }

    private IEnumerator Countdown(bool launchBall)
    {
        state = State.Countdown;
        Time.timeScale = 1f;
        player.InputEnabled = true;
        ui.SetScoreVisible(true);
        music.SetMood(Music.Mood.Playing);

        for (int i = 3; i > 0; i--)
        {
            ui.ShowCountdown(i.ToString(), launchBall);
            SoundFX.Play(Sound.Tick);
            yield return new WaitForSecondsRealtime(0.6f);
        }

        state = State.Playing;
        ui.ShowHud();
        if (launchBall)
        {
            ball.Launch();
            SoundFX.Play(Sound.Launch);
        }
        else
        {
            ball.Held = false;
            SoundFX.Play(Sound.Go);
        }
        countdown = null;
    }

    private void StopCountdown()
    {
        if (countdown != null) StopCoroutine(countdown);
        countdown = null;
    }

    // ---------- Scoring and difficulty ----------

    public void OnPaddleHit(Vector2 position, bool perfect)
    {
        if (state != State.Playing) return;

        // Perfect hits in a row are worth more: +2, +3, +4... and the x2 power-up doubles it all
        perfectStreak = perfect ? perfectStreak + 1 : 0;
        int points = (perfect ? Mathf.Min(1 + perfectStreak, maxComboPoints) : 1) * powerUps.PointsMultiplier;
        score += points;
        ui.SetScore(score, true);

        Color color = perfect ? Theme.Highlight : Theme.Paddle;
        string message = !perfect ? "+" + points : perfectStreak > 1 ? "PERFECT x" + perfectStreak + "  +" + points : "PERFECT  +" + points;
        effects.Burst(position + Vector2.down * ball.Radius, color, perfect ? 22 : 12, 6f, Vector2.up);
        effects.ShowPopup(message, position + Vector2.up * 0.8f, perfect ? Theme.Highlight : Theme.Text);

        // Pitch climbs as the score goes up
        SoundFX.Play(perfect ? Sound.Perfect : Sound.Paddle, 1f + Mathf.Min(score * 0.01f, 0.6f));
        cameraShake.Shake(perfect ? 0.12f : 0.06f, 0.12f);
        if (perfect) Haptics.Medium(); else Haptics.Light();

        int newLevel = 1 + score / pointsPerLevel;
        if (newLevel > level) LevelUp(newLevel);
    }

    private void LevelUp(int newLevel)
    {
        level = newLevel;
        Color color = LevelColor(level);

        player.ResizeTo(BasePaddleWidth() * powerUps.WidthMultiplier);
        ui.SetLevel(level, color, true);

        effects.Burst(new Vector2(arena.CenterX, arena.Top - 1f), color, 30, 8f, Vector2.down);
        SoundFX.Play(Sound.LevelUp);
        Haptics.Medium();
        music.SetLevel(level);
    }

    // Paddle width for the current level and difficulty mode, before power-ups.
    // It shrinks a bit every level, down to a minimum.
    private float BasePaddleWidth()
    {
        float scale = Difficulty.Current.PaddleWidth;
        return Mathf.Max(minPaddleWidth * scale, player.StartWidth * scale - paddleShrinkPerLevel * (level - 1));
    }

    // Called by PowerUps when one starts or runs out
    public void RefreshPowerUpEffects()
    {
        player.ResizeTo(BasePaddleWidth() * powerUps.WidthMultiplier);
        ball.SpeedMultiplier = powerUps.BallSpeedMultiplier;
    }

    // The ball bounced off the shield power-up instead of falling out
    public void OnShieldUsed(Vector2 point)
    {
        powerUps.BreakShield();
        Color color = Theme.PowerUpColors[(int)PowerUpType.Shield];
        effects.Burst(point, color, 26, 8f, Vector2.up);
        effects.ShowPopup("SAVED!", point + Vector2.up * 0.9f, color);
        SoundFX.Play(Sound.Shield);
        cameraShake.Shake(0.2f, 0.2f);
        Haptics.Medium();
    }

    private static Color LevelColor(int level)
    {
        return Theme.LevelColors[(level - 1) % Theme.LevelColors.Length];
    }

    public void OnWallHit(Vector2 point)
    {
        if (state != State.Playing) return;

        effects.Burst(point, LevelColor(level), 5, 3f, (new Vector2(arena.CenterX, 0f) - point).normalized);
        SoundFX.Play(Sound.Wall, Random.Range(0.95f, 1.05f));
    }

    public void GameOver()
    {
        if (state != State.Playing) return;

        state = State.GameOver;
        player.InputEnabled = false;

        Vector2 fallPoint = new Vector2(ball.transform.position.x, arena.Bottom + 0.1f);
        ball.Freeze();
        powerUps.HideAll();
        effects.Burst(fallPoint, Theme.Danger, 30, 9f, Vector2.up);
        cameraShake.Shake(0.35f, 0.4f);
        SoundFX.Play(Sound.GameOver);
        Haptics.Heavy();
        music.SetMood(Music.Mood.Muffled);

        bool newBest = score > Settings.BestScore;
        if (newBest) Settings.BestScore = score;

        ui.SetScoreVisible(false);
        StartCoroutine(ShowGameOverAfterDelay(newBest));
    }

    // Wait a moment so the player sees the ball fall before the game over screen appears
    private IEnumerator ShowGameOverAfterDelay(bool newBest)
    {
        yield return new WaitForSecondsRealtime(0.7f);
        ui.ShowGameOver(score, Settings.BestScore, newBest, level);

        // Celebrate a new best score with confetti
        if (newBest && score > 0)
        {
            SoundFX.Play(Sound.LevelUp, 1.25f);
            for (int i = 0; i < Theme.LevelColors.Length; i++)
            {
                Vector2 point = new Vector2(Random.Range(arena.Left + 1f, arena.Right - 1f), Random.Range(arena.Top - 5f, arena.Top - 2f));
                effects.Burst(point, Theme.LevelColors[i], 18, 7f, Vector2.up);
                yield return new WaitForSecondsRealtime(0.08f);
            }
        }
    }
}
