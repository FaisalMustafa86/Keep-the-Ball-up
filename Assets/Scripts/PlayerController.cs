using UnityEngine;
using UnityEngine.InputSystem;

// The paddle. Drag anywhere on the screen to move it: it follows your finger's sideways movement,
// so your finger never has to cover the paddle or the ball.
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float width = 2.4f;
    [SerializeField] private float thickness = 0.32f;

    // How far the paddle moves compared to your finger (1 = exactly the same distance)
    [SerializeField] private float dragSensitivity = 1.3f;

    // How far above the bottom of the safe area the paddle sits
    [SerializeField] private float heightAboveBottom = 2.2f;

    // Only the game turns this on, so the paddle doesn't move while tapping menu buttons
    public bool InputEnabled { get; set; }

    public float StartWidth { get; private set; }

    public float X => transform.position.x;
    public float Y => transform.position.y;
    public float HalfWidth => width / 2f;
    public float HalfHeight => thickness / 2f;
    public float TopY => Y + HalfHeight;

    private SpriteRenderer sr;
    private SpriteRenderer glow;
    private Vector3 baseScale;
    private Vector2 spriteSize;
    private bool dragging;
    private float lastTouchX;
    private float targetX;
    private float velocityX;
    private float flash;
    private float squash;

    public void Init()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteFactory.Pill;
        sr.color = Theme.Paddle;
        sr.sortingLayerName = "Player";
        sr.sortingOrder = 5;

        // The old paddle was a rotated capsule; this one is a flat bar
        transform.rotation = Quaternion.identity;
        spriteSize = sr.sprite.bounds.size;
        StartWidth = width;
        SetWidth(width);

        GameObject glowObject = new GameObject("Glow");
        glowObject.transform.SetParent(transform, false);
        glowObject.transform.localScale = new Vector3(1.4f * spriteSize.x / spriteSize.y, 5f, 1f);
        glow = glowObject.AddComponent<SpriteRenderer>();
        glow.sprite = SpriteFactory.Glow;
        glow.color = new Color(Theme.Paddle.r, Theme.Paddle.g, Theme.Paddle.b, 0.25f);
        glow.sortingLayerName = "Player";
        glow.sortingOrder = 4;

        ResetPosition();
    }

    void Update()
    {
        Arena arena = Arena.Instance;
        float minX = arena.Left + HalfWidth;
        float maxX = arena.Right - HalfWidth;

        // Don't move while paused
        if (InputEnabled && Time.timeScale > 0f) ReadTouch(minX, maxX);
        else dragging = false;

        float oldX = X;
        float x = Mathf.Clamp(targetX, minX, maxX);
        transform.position = new Vector3(x, arena.SafeBottom + heightAboveBottom, 0f);

        velocityX = Time.deltaTime > 0f ? (x - oldX) / Time.deltaTime : 0f;

        // Juice: tilt a little while moving, squash and flash when the ball hits
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Clamp(-velocityX * 0.4f, -6f, 6f));
        squash = Mathf.MoveTowards(squash, 0f, Time.deltaTime * 4f);
        transform.localScale = new Vector3(baseScale.x * (1f + squash * 0.15f), baseScale.y * (1f - squash * 0.4f), 1f);
        flash = Mathf.MoveTowards(flash, 0f, Time.deltaTime * 5f);
        sr.color = Color.Lerp(Theme.Paddle, Color.white, flash);
    }

    private void ReadTouch(float minX, float maxX)
    {
        Touchscreen touch = Touchscreen.current;
        if (touch == null || !touch.primaryTouch.press.isPressed)
        {
            dragging = false;
            return;
        }

        float touchX = Arena.Instance.ScreenToWorldX(touch.primaryTouch.position.ReadValue().x);
        if (dragging)
        {
            targetX = Mathf.Clamp(targetX + (touchX - lastTouchX) * dragSensitivity, minX, maxX);
        }
        dragging = true;
        lastTouchX = touchX;
    }

    // Does a ball at this position touch the top of the paddle?
    public bool IsHitBy(Vector2 ballPosition, float radius)
    {
        if (ballPosition.y < Y) return false;

        float closestX = Mathf.Clamp(ballPosition.x, X - HalfWidth, X + HalfWidth);
        float closestY = Mathf.Clamp(ballPosition.y, Y - HalfHeight, Y + HalfHeight);
        return (ballPosition - new Vector2(closestX, closestY)).sqrMagnitude <= radius * radius;
    }

    // The paddle gets narrower as the levels go up
    public void SetWidth(float newWidth)
    {
        width = newWidth;
        baseScale = new Vector3(width / spriteSize.x, thickness / spriteSize.y, 1f);
        transform.localScale = baseScale;
    }

    public void OnHit()
    {
        flash = 1f;
        squash = 1f;
    }

    public void ResetPosition()
    {
        Arena arena = Arena.Instance;
        transform.position = new Vector3(arena.CenterX, arena.SafeBottom + heightAboveBottom, 0f);
        targetX = arena.CenterX;
        dragging = false;
    }
}
