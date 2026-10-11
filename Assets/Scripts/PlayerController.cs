using UnityEngine;
using UnityEngine.InputSystem;

// The paddle. Drag anywhere on the screen to move it: it follows your finger's sideways movement,
// so your finger never has to cover the paddle or the ball.
// On PC it follows the mouse, or moves with A/D or the arrow keys. All of these work at the same time.
public class PlayerController : MonoBehaviour
{
    // Starting width; the game changes the current width with levels, difficulty and power-ups
    [SerializeField] private float width = 2.4f;
    [SerializeField] private float thickness = 0.32f;

    // How far the paddle moves compared to your finger (1 = exactly the same distance)
    [SerializeField] private float dragSensitivity = 1.3f;

    // Paddle speed when using the keyboard, in world units per second
    [SerializeField] private float keyboardSpeed = 14f;

    // How far above the bottom of the safe area the paddle sits
    [SerializeField] private float heightAboveBottom = 2.2f;

    // Only the game turns this on, so the paddle doesn't move while tapping menu buttons
    public bool InputEnabled { get; set; }

    public float StartWidth => width;

    public float X => transform.position.x;
    public float Y => transform.position.y;
    public float HalfWidth => currentWidth / 2f;
    public float HalfHeight => thickness / 2f;
    public float TopY => Y + HalfHeight;

    private SpriteRenderer sr;
    private SpriteRenderer glow;
    private Vector3 baseScale;
    private Vector2 spriteSize;
    private float currentWidth;
    private float targetWidth;
    private bool dragging;
    private float lastTouchX;
    private float targetX;
    private float velocityX;
    private float flash;
    private Color flashColor = Color.white;
    private float squash;

    public void Init()
    {
        if (!TryGetComponent(out sr))
        {
            sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.Pill;
            sr.color = Theme.Paddle;
            sr.sortingLayerName = "Player";
            sr.sortingOrder = 5;
        }

        // The old paddle was a rotated capsule; this one is a flat bar
        transform.rotation = Quaternion.identity;
        spriteSize = sr.sprite.bounds.size;
        SetWidth(width);

        if (SceneObjects.GetOrCreate(transform, "Glow", out glow))
        {
            glow.transform.localScale = new Vector3(1.4f * spriteSize.x / spriteSize.y, 5f, 1f);
            glow.sprite = SpriteFactory.Glow;
            glow.color = new Color(Theme.Paddle.r, Theme.Paddle.g, Theme.Paddle.b, 0.25f);
            glow.sortingLayerName = "Player";
            glow.sortingOrder = 4;
        }

        ResetPosition();
    }

    void Update()
    {
        // Grow or shrink smoothly after a power-up or level up
        if (currentWidth != targetWidth) ApplyWidth(Mathf.MoveTowards(currentWidth, targetWidth, Time.deltaTime * 4f));

        Arena arena = Arena.Instance;
        float minX = arena.Left + HalfWidth;
        float maxX = arena.Right - HalfWidth;

        // Don't move while paused
        if (InputEnabled && Time.timeScale > 0f)
        {
            // Touch first; the mouse is skipped while a finger is down so they don't fight
            if (!ReadTouch(minX, maxX)) ReadMouse(minX, maxX);
            ReadKeyboard(minX, maxX);
        }
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
        sr.color = Color.Lerp(Theme.Paddle, flashColor, flash);
    }

    // Returns true while a finger is on the screen
    private bool ReadTouch(float minX, float maxX)
    {
        Touchscreen touch = Touchscreen.current;
        if (touch == null || !touch.primaryTouch.press.isPressed)
        {
            dragging = false;
            return false;
        }

        float touchX = Arena.Instance.ScreenToWorldX(touch.primaryTouch.position.ReadValue().x);
        if (dragging)
        {
            targetX = Mathf.Clamp(targetX + (touchX - lastTouchX) * dragSensitivity, minX, maxX);
        }
        dragging = true;
        lastTouchX = touchX;
        return true;
    }

    // The paddle jumps under the cursor, but only when the mouse actually moves,
    // so a resting mouse doesn't block the keyboard
    private void ReadMouse(float minX, float maxX)
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || mouse.delta.ReadValue().x == 0f) return;

        float mouseX = Arena.Instance.ScreenToWorldX(mouse.position.ReadValue().x);
        targetX = Mathf.Clamp(mouseX, minX, maxX);
    }

    private void ReadKeyboard(float minX, float maxX)
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        float direction = 0f;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) direction -= 1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) direction += 1f;
        if (direction != 0f) targetX = Mathf.Clamp(targetX + direction * keyboardSpeed * Time.deltaTime, minX, maxX);
    }

    // Does a ball at this position touch the top of the paddle?
    public bool IsHitBy(Vector2 ballPosition, float radius)
    {
        if (ballPosition.y < Y) return false;

        float closestX = Mathf.Clamp(ballPosition.x, X - HalfWidth, X + HalfWidth);
        float closestY = Mathf.Clamp(ballPosition.y, Y - HalfHeight, Y + HalfHeight);
        return (ballPosition - new Vector2(closestX, closestY)).sqrMagnitude <= radius * radius;
    }

    // Changes the width straight away
    public void SetWidth(float newWidth)
    {
        targetWidth = newWidth;
        ApplyWidth(newWidth);
    }

    // Changes the width smoothly over a moment
    public void ResizeTo(float newWidth)
    {
        targetWidth = newWidth;
    }

    private void ApplyWidth(float newWidth)
    {
        currentWidth = newWidth;
        baseScale = new Vector3(currentWidth / spriteSize.x, thickness / spriteSize.y, 1f);
        transform.localScale = baseScale;
    }

    // Briefly glow in a colour, e.g. when catching a power-up
    public void Flash(Color color)
    {
        flashColor = color;
        flash = 1f;
    }

    public void OnHit()
    {
        flashColor = Color.white;
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
