using UnityEngine;
using UnityEngine.UI;

public enum PowerUpType { Wide, Slow, Shield, Double, Tiny, Fast }

// Power-ups fall from the top every so often; catch one with the paddle to use it.
// Good: WIDE paddle, SLOW ball, SHIELD (saves the ball once), x2 points.
// Bad (they show up in harder modes): TINY paddle, FAST ball.
// Catching the opposite of an active power-up cancels it (WIDE cancels TINY, SLOW cancels FAST).
// Each one is a glossy orb with an icon: it pops in, sways and sparkles as it falls,
// bursts into a ring when caught, and fades away if missed.
public class PowerUps : MonoBehaviour
{
    private const int PickupCount = 3;
    private const float OrbSize = 0.95f;
    private const float PopInTime = 0.35f;
    private const float CatchTime = 0.3f;

    private struct Info
    {
        public string Label;
        public string Name;
        public bool Bad;
        public float Duration;
    }

    // In PowerUpType order. A duration of 0 means it lasts until used (the shield).
    private static readonly Info[] Infos =
    {
        new Info { Label = "WIDE", Name = "WIDE PADDLE", Duration = 10f },
        new Info { Label = "SLOW", Name = "SLOW BALL", Duration = 7f },
        new Info { Label = "SHIELD", Name = "SHIELD", Duration = 0f },
        new Info { Label = "x2", Name = "DOUBLE POINTS", Duration = 10f },
        new Info { Label = "TINY", Name = "TINY PADDLE", Bad = true, Duration = 7f },
        new Info { Label = "FAST", Name = "FAST BALL", Bad = true, Duration = 6f },
    };

    private enum PickupState { Hidden, Falling, Caught }

    private class Pickup
    {
        public Transform transform;
        public SpriteRenderer glow;
        public SpriteRenderer body;
        public SpriteRenderer rim;
        public SpriteRenderer shine;
        public SpriteRenderer icon;
        public Text label;
        public PowerUpType type;
        public PickupState state;
        public float age;
        public float startX;
        public float trailTimer;
    }

    public float WidthMultiplier => (IsActive(PowerUpType.Wide) ? 1.5f : 1f) * (IsActive(PowerUpType.Tiny) ? 0.6f : 1f);
    public float BallSpeedMultiplier => (IsActive(PowerUpType.Slow) ? 0.65f : 1f) * (IsActive(PowerUpType.Fast) ? 1.35f : 1f);
    public int PointsMultiplier => IsActive(PowerUpType.Double) ? 2 : 1;

    private GameManager game;
    private PlayerController paddle;
    private Ball ball;
    private Arena arena;
    private Effects effects;
    private GameUI ui;
    private Camera cam;

    private Pickup[] pickups;
    private SpriteRenderer shield;
    private readonly float[] timeLeft = new float[Infos.Length];
    private bool shieldUp;
    private float spawnTimer;

    public void Init(GameManager gameManager, PlayerController player, Ball gameBall, Arena gameArena, Effects gameEffects, GameUI gameUI, Camera camera)
    {
        game = gameManager;
        paddle = player;
        ball = gameBall;
        arena = gameArena;
        effects = gameEffects;
        ui = gameUI;
        cam = camera;

        SceneObjects.GetOrCreate(transform, "Power Ups", out GameObject root);
        pickups = new Pickup[PickupCount];
        for (int i = 0; i < PickupCount; i++)
        {
            pickups[i] = CreatePickup(root.transform, i);
        }

        // A glowing line near the bottom while the shield is up
        if (SceneObjects.GetOrCreate(root.transform, "Shield", out shield))
        {
            shield.sprite = SpriteFactory.Square;
            shield.color = Theme.PowerUpColors[(int)PowerUpType.Shield];
            shield.sortingLayerName = "Player";
            shield.sortingOrder = 3;
            shield.gameObject.SetActive(false);
        }
    }

    private Pickup CreatePickup(Transform parent, int index)
    {
        Pickup pickup = new Pickup();

        if (SceneObjects.GetOrCreate(parent, "Pickup " + index, out GameObject go)) go.SetActive(false);
        pickup.transform = go.transform;

        pickup.glow = CreatePart(go.transform, "Glow", SpriteFactory.Glow, 12, OrbSize * 2.4f, Vector2.zero);
        pickup.body = CreatePart(go.transform, "Body", SpriteFactory.Circle, 13, OrbSize, Vector2.zero);
        pickup.rim = CreatePart(go.transform, "Rim", SpriteFactory.Ring, 14, OrbSize, Vector2.zero);
        pickup.shine = CreatePart(go.transform, "Shine", SpriteFactory.Circle, 15, OrbSize * 0.26f, new Vector2(-0.2f, 0.2f) * OrbSize);
        pickup.icon = CreatePart(go.transform, "Icon", SpriteFactory.Circle, 16, OrbSize * 0.8f, Vector2.zero);

        // The name is UI text that follows the orb on screen, drawn just above the world and under the menus
        Transform canvas = ui.HudCanvas.transform;
        if (SceneObjects.GetOrCreate(canvas, "Pickup Label " + index, out GameObject labelObject, typeof(RectTransform), typeof(Text)))
        {
            Text text = labelObject.GetComponent<Text>();
            text.font = ui.Font;
            text.fontSize = 30;
            text.fontStyle = ui.FontStyle;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            Shadow textShadow = labelObject.AddComponent<Shadow>();
            textShadow.effectColor = new Color(0.05f, 0.02f, 0.15f, 0.6f);
            textShadow.effectDistance = new Vector2(0f, -3f);
            Transform dim = canvas.Find("Dim");
            if (dim != null) labelObject.transform.SetSiblingIndex(dim.GetSiblingIndex() + 1);
        }
        pickup.label = labelObject.GetComponent<Text>();
        pickup.label.enabled = false;

        return pickup;
    }

    private static SpriteRenderer CreatePart(Transform parent, string name, Sprite sprite, int order, float size, Vector2 offset)
    {
        if (SceneObjects.GetOrCreate(parent, name, out SpriteRenderer sr))
        {
            sr.sprite = sprite;
            sr.sortingLayerName = "Player";
            sr.sortingOrder = order;
            sr.transform.localPosition = offset;
            sr.transform.localScale = Vector3.one * size / sprite.bounds.size.y;
        }
        return sr;
    }

    // ---------- Called by the GameManager ----------

    // Clears everything at the start of a round
    public void ResetAll()
    {
        HideAll();
        System.Array.Clear(timeLeft, 0, timeLeft.Length);
        shieldUp = false;
        ball.HasShield = false;
        spawnTimer = NextSpawnDelay();
        UpdateTimers(0f);
    }

    // Hides the orbs and the shield, e.g. on game over
    public void HideAll()
    {
        foreach (Pickup pickup in pickups) Hide(pickup);
        shield.gameObject.SetActive(false);
    }

    // The ball just bounced off the shield
    public void BreakShield()
    {
        shieldUp = false;
        shield.gameObject.SetActive(false);
    }

    // ---------- Every frame ----------

    void Update()
    {
        if (!game.IsPlaying) return;
        float dt = Time.deltaTime;

        UpdateTimers(dt);

        spawnTimer -= dt;
        if (spawnTimer <= 0f)
        {
            Spawn();
            spawnTimer = NextSpawnDelay();
        }

        foreach (Pickup pickup in pickups)
        {
            if (pickup.state == PickupState.Falling) UpdateFalling(pickup, dt);
            else if (pickup.state == PickupState.Caught) UpdateCaught(pickup, dt);
        }

        if (shieldUp)
        {
            // Pulse so it reads as an energy barrier
            Color c = shield.color;
            c.a = 0.55f + 0.35f * Mathf.Sin(Time.time * 6f);
            shield.color = c;
        }
    }

    // Counts down the active power-ups and shows them as timer rings in the HUD
    private void UpdateTimers(float dt)
    {
        bool expired = false;
        for (int i = 0; i < timeLeft.Length; i++)
        {
            if (timeLeft[i] > 0f)
            {
                timeLeft[i] -= dt;
                if (timeLeft[i] <= 0f)
                {
                    timeLeft[i] = 0f;
                    expired = true;
                }
            }

            PowerUpType type = (PowerUpType)i;
            if (type == PowerUpType.Shield) ui.SetPowerUpTimer(type, shieldUp ? 1f : 0f, float.MaxValue);
            else ui.SetPowerUpTimer(type, timeLeft[i] / Infos[i].Duration, timeLeft[i]);
        }

        if (expired)
        {
            SoundFX.Play(Sound.PowerUpEnd);
            game.RefreshPowerUpEffects();
        }
    }

    private void Spawn()
    {
        Pickup pickup = System.Array.Find(pickups, p => p.state == PickupState.Hidden);
        if (pickup == null) return;

        pickup.type = ChooseType();
        Info info = Infos[(int)pickup.type];
        Color color = Theme.PowerUpColors[(int)pickup.type];

        pickup.body.color = color;
        pickup.icon.sprite = SpriteFactory.PowerUpIcon(pickup.type);
        pickup.icon.color = info.Bad ? Color.white : Theme.ButtonPrimaryText;
        pickup.label.text = info.Label;
        pickup.label.color = color;
        SetPartsVisible(pickup, true);

        pickup.startX = Random.Range(arena.Left + OrbSize, arena.Right - OrbSize);
        pickup.transform.position = new Vector3(pickup.startX, arena.Top - 0.8f, 0f);
        pickup.transform.localScale = Vector3.zero;
        pickup.transform.gameObject.SetActive(true);
        pickup.label.enabled = true;
        pickup.state = PickupState.Falling;
        pickup.age = 0f;
        pickup.trailTimer = 0f;
        ApplyFade(pickup, 1f);

        effects.Burst(pickup.transform.position, color, 10, 4f, Vector2.zero);
    }

    private PowerUpType ChooseType()
    {
        if (Random.value < Difficulty.Current.BadChance)
        {
            return Random.value < 0.5f ? PowerUpType.Tiny : PowerUpType.Fast;
        }

        // A second shield would do nothing, so pick another one while it's up
        PowerUpType type;
        do
        {
            type = (PowerUpType)Random.Range(0, (int)PowerUpType.Tiny);
        } while (type == PowerUpType.Shield && shieldUp);
        return type;
    }

    private void UpdateFalling(Pickup pickup, float dt)
    {
        pickup.age += dt;
        Info info = Infos[(int)pickup.type];
        Color color = Theme.PowerUpColors[(int)pickup.type];

        // Fall with a gentle side-to-side sway and tilt
        Vector3 p = pickup.transform.position;
        p.y -= Difficulty.Current.FallSpeed * dt;
        p.x = Mathf.Clamp(pickup.startX + Mathf.Sin(pickup.age * 2.4f) * 0.3f, arena.Left + OrbSize / 2f, arena.Right - OrbSize / 2f);
        pickup.transform.position = p;
        pickup.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(pickup.age * 3f) * 10f);

        // Pop in with a little overshoot, then breathe
        float pop = pickup.age < PopInTime ? EaseOutBack(pickup.age / PopInTime) : 1f + 0.04f * Mathf.Sin(pickup.age * 6f);
        pickup.transform.localScale = Vector3.one * pop;
        pickup.glow.transform.localScale = Vector3.one * (OrbSize * 2.4f / pickup.glow.sprite.bounds.size.y) * (1f + 0.15f * Mathf.Sin(pickup.age * 5f));

        // Bad ones flicker as a warning
        pickup.body.color = info.Bad ? Color.Lerp(color, Color.white, 0.35f * Mathf.Max(0f, Mathf.Sin(pickup.age * 14f))) : color;

        // A trail of sparkles
        pickup.trailTimer -= dt;
        if (pickup.trailTimer <= 0f)
        {
            effects.Burst((Vector2)p + Random.insideUnitCircle * 0.25f, color, 1, 0.8f, Vector2.up);
            pickup.trailTimer = 0.07f;
        }

        // Fade out once it has gone past the paddle
        float fade = p.y < paddle.Y ? Mathf.Clamp01((p.y - arena.Bottom) / (paddle.Y - arena.Bottom)) : 1f;
        ApplyFade(pickup, fade);
        PlaceLabel(pickup, fade);

        if (TouchesPaddle(p)) Collect(pickup);
        else if (fade <= 0f) Hide(pickup);
    }

    // After a catch, the rim expands into a fading ring
    private void UpdateCaught(Pickup pickup, float dt)
    {
        pickup.age += dt;
        float t = pickup.age / CatchTime;
        if (t >= 1f)
        {
            Hide(pickup);
            return;
        }

        pickup.transform.localScale = Vector3.one * Mathf.Lerp(1f, 3.2f, 1f - (1f - t) * (1f - t));
        Color c = Theme.PowerUpColors[(int)pickup.type];
        c.a = 0.9f * (1f - t);
        pickup.rim.color = c;
    }

    private bool TouchesPaddle(Vector2 p)
    {
        float closestX = Mathf.Clamp(p.x, paddle.X - paddle.HalfWidth, paddle.X + paddle.HalfWidth);
        float closestY = Mathf.Clamp(p.y, paddle.Y - paddle.HalfHeight, paddle.Y + paddle.HalfHeight);
        float radius = OrbSize / 2f;
        return (p - new Vector2(closestX, closestY)).sqrMagnitude <= radius * radius;
    }

    private void Collect(Pickup pickup)
    {
        Info info = Infos[(int)pickup.type];
        Color color = Theme.PowerUpColors[(int)pickup.type];
        Vector2 position = pickup.transform.position;

        effects.Burst(position, color, 24, 7f, Vector2.up);
        effects.ShowPopup(info.Name, position + Vector2.up * 1f, color);
        SoundFX.Play(info.Bad ? Sound.PowerDown : Sound.PowerUp);
        if (info.Bad) Haptics.Medium(); else Haptics.Light();
        paddle.Flash(color);

        // Keep only the rim for the ring burst
        SetPartsVisible(pickup, false);
        pickup.rim.enabled = true;
        pickup.label.enabled = false;
        pickup.transform.rotation = Quaternion.identity;
        pickup.state = PickupState.Caught;
        pickup.age = 0f;

        Activate(pickup.type);
    }

    private void Activate(PowerUpType type)
    {
        if (type == PowerUpType.Shield)
        {
            shieldUp = true;
            ball.HasShield = true;
            ball.ShieldY = arena.SafeBottom + 0.7f;
            shield.transform.position = new Vector3(arena.CenterX, ball.ShieldY, 0f);
            shield.transform.localScale = new Vector3((arena.Right - arena.Left) / shield.sprite.bounds.size.x, 0.1f / shield.sprite.bounds.size.y, 1f);
            shield.gameObject.SetActive(true);
        }
        else
        {
            timeLeft[(int)type] = Infos[(int)type].Duration;

            // Opposites cancel out
            if (type == PowerUpType.Wide) timeLeft[(int)PowerUpType.Tiny] = 0f;
            if (type == PowerUpType.Tiny) timeLeft[(int)PowerUpType.Wide] = 0f;
            if (type == PowerUpType.Slow) timeLeft[(int)PowerUpType.Fast] = 0f;
            if (type == PowerUpType.Fast) timeLeft[(int)PowerUpType.Slow] = 0f;
        }

        game.RefreshPowerUpEffects();
    }

    private void Hide(Pickup pickup)
    {
        pickup.state = PickupState.Hidden;
        pickup.transform.gameObject.SetActive(false);
        pickup.label.enabled = false;
    }

    private static void SetPartsVisible(Pickup pickup, bool visible)
    {
        pickup.glow.enabled = pickup.body.enabled = pickup.rim.enabled = pickup.shine.enabled = pickup.icon.enabled = visible;
    }

    private static void ApplyFade(Pickup pickup, float alpha)
    {
        SetAlpha(pickup.glow, 0.45f * alpha);
        SetAlpha(pickup.body, alpha);
        pickup.rim.color = new Color(1f, 1f, 1f, 0.55f * alpha);
        pickup.shine.color = new Color(1f, 1f, 1f, 0.4f * alpha);
        SetAlpha(pickup.icon, alpha);
    }

    private static void SetAlpha(SpriteRenderer sr, float alpha)
    {
        Color c = sr.color;
        c.a = alpha;
        sr.color = c;
    }

    private void PlaceLabel(Pickup pickup, float alpha)
    {
        pickup.label.transform.position = cam.WorldToScreenPoint(pickup.transform.position + Vector3.down * (OrbSize * 0.85f));
        Color c = pickup.label.color;
        c.a = alpha;
        pickup.label.color = c;
    }

    private static float EaseOutBack(float t)
    {
        const float overshoot = 1.70158f;
        float u = t - 1f;
        return 1f + (overshoot + 1f) * u * u * u + overshoot * u * u;
    }

    private bool IsActive(PowerUpType type) => timeLeft[(int)type] > 0f;

    private float NextSpawnDelay()
    {
        return Difficulty.Current.PowerUpInterval * Random.Range(0.7f, 1.3f);
    }
}
