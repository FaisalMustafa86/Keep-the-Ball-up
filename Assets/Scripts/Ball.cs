using UnityEngine;

// The ball. It moves in straight lines (no gravity), bounces off the walls and the top,
// and slowly speeds up the longer you survive, plus a little more on every hit.
// Let it fall past the paddle and it's game over.
public class Ball : MonoBehaviour
{
    [SerializeField] private float radius = 0.26f;
    [SerializeField] private float startSpeed = 8.5f;

    // Speed gained every second while the ball is in play
    [SerializeField] private float speedIncreasePerSecond = 0.08f;
    [SerializeField] private float speedIncreasePerHit = 0.12f;
    [SerializeField] private float maxSpeed = 22f;

    // How steep the bounce gets when the ball hits the very edge of the paddle (0 = straight up)
    [SerializeField, Range(10f, 80f)] private float maxBounceAngle = 60f;

    // Stops the ball from getting stuck going almost sideways between the walls
    [SerializeField, Range(0.1f, 0.7f)] private float minVerticalDirection = 0.3f;

    // Hits this close to the middle of the paddle count as "perfect" (0.2 = middle 20% on each side)
    [SerializeField, Range(0f, 0.5f)] private float perfectZone = 0.15f;

    private const int TrailLength = 12;

    public float Radius => radius;
    public float Speed => speed;

    // While held, the ball stays where it is (used for the countdown after pausing)
    public bool Held { get; set; }

    public bool IsLaunched => launched;

    private PlayerController paddle;
    private Vector2 velocity;
    private float speed;
    private bool launched;
    private bool frozen;

    private Transform[] trail;
    private SpriteRenderer[] trailRenderers;
    private Vector3[] history;

    public void Init(PlayerController player)
    {
        paddle = player;
        transform.localScale = Vector3.one * radius * 2f;

        SpriteRenderer sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteFactory.Circle;
        sr.color = Theme.Ball;
        sr.sortingLayerName = "Player";
        sr.sortingOrder = 10;

        GameObject glowObject = new GameObject("Glow");
        glowObject.transform.SetParent(transform, false);
        glowObject.transform.localScale = Vector3.one * 3.5f;
        SpriteRenderer glow = glowObject.AddComponent<SpriteRenderer>();
        glow.sprite = SpriteFactory.Glow;
        glow.color = new Color(Theme.BallGlow.r, Theme.BallGlow.g, Theme.BallGlow.b, 0.45f);
        glow.sortingLayerName = "Player";
        glow.sortingOrder = 8;

        // Fading copies of the ball that follow behind it
        Transform trailParent = new GameObject("Ball Trail").transform;
        trail = new Transform[TrailLength];
        trailRenderers = new SpriteRenderer[TrailLength];
        history = new Vector3[TrailLength];
        for (int i = 0; i < TrailLength; i++)
        {
            GameObject ghost = new GameObject("Trail " + i);
            ghost.transform.SetParent(trailParent);
            SpriteRenderer ghostRenderer = ghost.AddComponent<SpriteRenderer>();
            ghostRenderer.sprite = SpriteFactory.Circle;
            ghostRenderer.sortingLayerName = "Player";
            ghostRenderer.sortingOrder = 9;

            float t = 1f - (float)(i + 1) / (TrailLength + 1);
            ghostRenderer.color = new Color(Theme.BallGlow.r, Theme.BallGlow.g, Theme.BallGlow.b, 0.35f * t);
            ghost.transform.localScale = Vector3.one * radius * 2f * Mathf.Lerp(0.3f, 0.9f, t);

            trail[i] = ghost.transform;
            trailRenderers[i] = ghostRenderer;
        }
    }

    void Update()
    {
        if (frozen || Held) return;

        if (!launched)
        {
            // Sit on the paddle until launched
            transform.position = new Vector3(paddle.X, paddle.TopY + radius + 0.02f, 0f);
            ClearTrail();
            return;
        }

        // Slowly speed up over time
        speed = Mathf.Min(speed + speedIncreasePerSecond * Time.deltaTime, maxSpeed);
        velocity = velocity.normalized * speed;

        // Move in small steps so a fast ball can never pass through the paddle or a wall
        float distance = speed * Time.deltaTime;
        int steps = Mathf.Max(1, Mathf.CeilToInt(distance / (radius * 0.5f)));
        for (int i = 0; i < steps && launched; i++)
        {
            Step(Time.deltaTime / steps);
        }

        UpdateTrail();

        if (transform.position.y < Arena.Instance.Bottom - radius * 2f)
        {
            GameManager.Instance.GameOver();
        }
    }

    private void Step(float dt)
    {
        Arena arena = Arena.Instance;
        Vector2 p = (Vector2)transform.position + velocity * dt;
        bool hitWall = false;

        if (p.x - radius < arena.Left)
        {
            p.x = arena.Left + radius;
            velocity.x = Mathf.Abs(velocity.x);
            hitWall = true;
            GameManager.Instance.OnWallHit(new Vector2(arena.Left, p.y));
        }
        else if (p.x + radius > arena.Right)
        {
            p.x = arena.Right - radius;
            velocity.x = -Mathf.Abs(velocity.x);
            hitWall = true;
            GameManager.Instance.OnWallHit(new Vector2(arena.Right, p.y));
        }

        if (p.y + radius > arena.Top)
        {
            p.y = arena.Top - radius;
            velocity.y = -Mathf.Abs(velocity.y);
            hitWall = true;
            GameManager.Instance.OnWallHit(new Vector2(p.x, arena.Top));
        }

        if (hitWall) KeepDirectionPlayable();

        if (velocity.y < 0f && paddle.IsHitBy(p, radius))
        {
            // Where the ball hits the paddle decides where it goes: middle = straight up, edges = steep angle
            float offset = Mathf.Clamp((p.x - paddle.X) / paddle.HalfWidth, -1f, 1f);
            float angle = offset * maxBounceAngle * Mathf.Deg2Rad;

            speed = Mathf.Min(speed + speedIncreasePerHit, maxSpeed);
            velocity = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * speed;
            p.y = paddle.TopY + radius;

            paddle.OnHit();
            GameManager.Instance.OnPaddleHit(p, Mathf.Abs(offset) <= perfectZone);
        }

        transform.position = p;
    }

    private void KeepDirectionPlayable()
    {
        Vector2 direction = velocity.normalized;
        if (Mathf.Abs(direction.y) < minVerticalDirection)
        {
            float signX = direction.x >= 0f ? 1f : -1f;
            float signY = direction.y >= 0f ? 1f : -1f;
            direction = new Vector2(signX * Mathf.Sqrt(1f - minVerticalDirection * minVerticalDirection), signY * minVerticalDirection);
        }
        velocity = direction * speed;
    }

    // Puts the ball on top of the paddle, waiting to be launched
    public void ResetBall()
    {
        launched = false;
        frozen = false;
        Held = false;
        speed = startSpeed;
        velocity = Vector2.zero;
        gameObject.SetActive(true);
        ClearTrail();
    }

    public void Launch()
    {
        launched = true;
        float angle = Random.Range(-25f, 25f) * Mathf.Deg2Rad;
        velocity = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * speed;
    }

    public void Freeze()
    {
        launched = false;
        frozen = true;
        gameObject.SetActive(false);
        foreach (SpriteRenderer ghost in trailRenderers) ghost.enabled = false;
    }

    private void UpdateTrail()
    {
        for (int i = TrailLength - 1; i > 0; i--) history[i] = history[i - 1];
        history[0] = transform.position;
        for (int i = 0; i < TrailLength; i++)
        {
            trail[i].position = history[i];
            trailRenderers[i].enabled = true;
        }
    }

    private void ClearTrail()
    {
        for (int i = 0; i < TrailLength; i++)
        {
            history[i] = transform.position;
            trail[i].position = transform.position;
            trailRenderers[i].enabled = false;
        }
    }
}
