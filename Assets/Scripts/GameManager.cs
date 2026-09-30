using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
#if UNITY_EDITOR
using UnityEngine.InputSystem.EnhancedTouch;
#endif

// Runs the game: builds the arena, paddle, ball and UI when the app starts, keeps score,
// makes the game harder as you go, and moves between the menu / countdown / playing / paused / game over screens.
// There is nothing to set up in the scene: if no GameManager exists, one is created automatically.
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

        SoundFX.Init(gameObject);
        BuildWorld();

        ui = gameObject.AddComponent<GameUI>();
        ui.Init(this, cam);
        effects = gameObject.AddComponent<Effects>();
        effects.Init(cam, ui.HudCanvas, ui.Font);

        GoToMenu();
    }

    private void BuildWorld()
    {
        cam = Camera.main;
        cam.backgroundColor = Theme.BackgroundTop;
        cameraShake = cam.GetComponent<CameraShake>();
        if (cameraShake == null) cameraShake = cam.gameObject.AddComponent<CameraShake>();

        GameObject backgroundObject = GameObject.Find("Background");
        SpriteRenderer background = backgroundObject != null ? backgroundObject.GetComponent<SpriteRenderer>() : null;

        arena = new GameObject("Arena").AddComponent<Arena>();
        arena.Init(cam, background);

        GameObject playerObject = GameObject.Find("player");
        if (playerObject == null) playerObject = new GameObject("player");
        player = playerObject.GetComponent<PlayerController>();
        if (player == null) player = playerObject.AddComponent<PlayerController>();
        player.Init();

        ball = new GameObject("Ball").AddComponent<Ball>();
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
        player.SetWidth(player.StartWidth);
        player.ResetPosition();
        ball.ResetBall();
        arena.SetWallColor(LevelColor(level));
        ui.SetScore(0, false);
        ui.SetLevel(level, LevelColor(level), false);
    }

    private IEnumerator Countdown(bool launchBall)
    {
        state = State.Countdown;
        Time.timeScale = 1f;
        player.InputEnabled = true;
        ui.SetScoreVisible(true);

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

        // Perfect hits in a row are worth more: +2, +3, +4...
        perfectStreak = perfect ? perfectStreak + 1 : 0;
        int points = perfect ? Mathf.Min(1 + perfectStreak, maxComboPoints) : 1;
        score += points;
        ui.SetScore(score, true);

        Color color = perfect ? Theme.Highlight : Theme.Paddle;
        string message = !perfect ? "+1" : perfectStreak > 1 ? "PERFECT x" + perfectStreak + "  +" + points : "PERFECT  +" + points;
        effects.Burst(position + Vector2.down * ball.Radius, color, perfect ? 22 : 12, 6f, Vector2.up);
        effects.ShowPopup(message, position + Vector2.up * 0.8f, perfect ? Theme.Highlight : Theme.Text);

        // Pitch climbs as the score goes up
        SoundFX.Play(perfect ? Sound.Perfect : Sound.Paddle, 1f + Mathf.Min(score * 0.01f, 0.6f));
        cameraShake.Shake(perfect ? 0.12f : 0.06f, 0.12f);

        int newLevel = 1 + score / pointsPerLevel;
        if (newLevel > level) LevelUp(newLevel);
    }

    private void LevelUp(int newLevel)
    {
        level = newLevel;
        Color color = LevelColor(level);

        float width = Mathf.Max(minPaddleWidth, player.StartWidth - paddleShrinkPerLevel * (level - 1));
        player.SetWidth(width);
        arena.SetWallColor(color);
        ui.SetLevel(level, color, true);

        effects.Burst(new Vector2(arena.CenterX, arena.Top - 1f), color, 30, 8f, Vector2.down);
        SoundFX.Play(Sound.LevelUp);
    }

    private static Color LevelColor(int level)
    {
        return Theme.LevelColors[(level - 1) % Theme.LevelColors.Length];
    }

    public void OnWallHit(Vector2 point)
    {
        if (state != State.Playing) return;

        arena.FlashWall(point);
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
        effects.Burst(fallPoint, Theme.Danger, 30, 9f, Vector2.up);
        cameraShake.Shake(0.35f, 0.4f);
        SoundFX.Play(Sound.GameOver);
        Settings.Vibrate();

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
    }
}
