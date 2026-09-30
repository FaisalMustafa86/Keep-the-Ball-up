using UnityEngine;

// The play area. It zooms the camera so the play area is always the same width on every phone,
// fits the background and walls to the screen edges, and keeps the top wall below the notch.
public class Arena : MonoBehaviour
{
    public static Arena Instance { get; private set; }

    private const float WallThickness = 0.12f;

    // Width of the play area in world units, the same on every phone
    private const float PlayWidth = 9f;
    private const float MinHalfHeight = 7f;
    private const int StarCount = 60;

    // Inner edges of the play area (where the walls start)
    public float Left { get; private set; }
    public float Right { get; private set; }
    public float Top { get; private set; }
    public float Bottom { get; private set; }
    public float CenterX => (Left + Right) / 2f;

    // Bottom of the safe area (above the iPhone home bar / Android navigation bar)
    public float SafeBottom { get; private set; }

    private Camera cam;
    private Vector2 center;
    private int lastScreenWidth;
    private int lastScreenHeight;
    private Rect lastSafeArea;
    private float screenHalfWidth;

    private SpriteRenderer background;
    private SpriteRenderer dangerZone;
    private SpriteRenderer leftWall;
    private SpriteRenderer rightWall;
    private SpriteRenderer topWall;

    private Color wallColor = new Color(Theme.Walls.r, Theme.Walls.g, Theme.Walls.b, 0.7f);

    private Transform[] stars;
    private SpriteRenderer[] starRenderers;
    private float[] starSpeeds;
    private float[] starPhases;

    public void Init(Camera camera, SpriteRenderer existingBackground)
    {
        Instance = this;
        cam = camera;
        center = cam.transform.position;

        background = existingBackground != null ? existingBackground : CreateSprite("Background", null);
        background.sprite = SpriteFactory.VerticalGradient(Theme.BackgroundBottom, Theme.BackgroundTop);
        background.color = Color.white;
        background.sortingLayerName = "Background";
        background.sortingOrder = 0;
        background.transform.rotation = Quaternion.identity;

        dangerZone = CreateSprite("Danger Zone", transform);
        dangerZone.sprite = SpriteFactory.VerticalGradient(new Color(1f, 1f, 1f, 0.35f), new Color(1f, 1f, 1f, 0f));
        dangerZone.color = Theme.Danger;
        dangerZone.sortingLayerName = "Background";
        dangerZone.sortingOrder = 2;

        leftWall = CreateWall("Left Wall");
        rightWall = CreateWall("Right Wall");
        topWall = CreateWall("Top Wall");

        CreateStars();
        Fit();
    }

    void Update()
    {
        // Re-fit if the window / Game view is resized
        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight || Screen.safeArea != lastSafeArea)
        {
            Fit();
        }

        // Stars slowly drift upwards and twinkle
        for (int i = 0; i < stars.Length; i++)
        {
            Vector3 p = stars[i].position;
            p.y += starSpeeds[i] * Time.deltaTime;
            if (p.y > Top + 0.5f)
            {
                p.y = Bottom - 0.5f;
                p.x = Random.Range(Left, Right);
            }
            stars[i].position = p;

            Color c = starRenderers[i].color;
            c.a = 0.15f + 0.25f * (0.5f + 0.5f * Mathf.Sin(Time.time * 2f + starPhases[i]));
            starRenderers[i].color = c;
        }
    }

    private void Fit()
    {
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
        lastSafeArea = Screen.safeArea;

        cam.orthographicSize = Mathf.Max(PlayWidth / 2f / cam.aspect, MinHalfHeight);
        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;
        screenHalfWidth = halfWidth;

        float left = center.x - halfWidth;
        float right = center.x + halfWidth;
        float bottom = center.y - halfHeight;

        // Convert the safe area (in pixels) to world heights, to stay clear of notches and home bars
        Rect safe = Screen.safeArea;
        float safeTop = bottom + safe.yMax / Screen.height * halfHeight * 2f;
        float safeBottom = bottom + safe.yMin / Screen.height * halfHeight * 2f;

        // A little bigger than the screen so camera shake never shows the edges
        Place(background.transform, center, new Vector2(halfWidth * 2f + 2f, halfHeight * 2f + 2f), background.sprite);
        Place(dangerZone.transform, new Vector2(center.x, bottom + 0.75f), new Vector2(halfWidth * 2f, 1.5f), dangerZone.sprite);
        Place(leftWall.transform, new Vector2(left + WallThickness / 2f, center.y), new Vector2(WallThickness, halfHeight * 2f + 2f), leftWall.sprite);
        Place(rightWall.transform, new Vector2(right - WallThickness / 2f, center.y), new Vector2(WallThickness, halfHeight * 2f + 2f), rightWall.sprite);
        Place(topWall.transform, new Vector2(center.x, safeTop - WallThickness / 2f), new Vector2(halfWidth * 2f, WallThickness), topWall.sprite);

        Left = left + WallThickness;
        Right = right - WallThickness;
        Top = safeTop - WallThickness;
        Bottom = bottom;
        SafeBottom = safeBottom;

        foreach (Transform star in stars)
        {
            star.position = new Vector3(Random.Range(Left, Right), Random.Range(Bottom, Top), 0f);
        }
    }

    // Screen pixel X to world X. Ignores camera shake, so dragging stays steady.
    public float ScreenToWorldX(float screenX)
    {
        return center.x + (screenX / Screen.width - 0.5f) * screenHalfWidth * 2f;
    }

    // Briefly light up the wall the ball just hit
    public void FlashWall(Vector2 hitPoint)
    {
        SpriteRenderer wall = hitPoint.y >= Top - 0.01f ? topWall : hitPoint.x < CenterX ? leftWall : rightWall;
        wall.color = Color.white;
    }

    void LateUpdate()
    {
        FadeWall(leftWall);
        FadeWall(rightWall);
        FadeWall(topWall);
    }

    private void FadeWall(SpriteRenderer wall)
    {
        wall.color = Color.Lerp(wall.color, wallColor, Time.deltaTime * 8f);
    }

    // Change the colour of the walls (they fade to it smoothly)
    public void SetWallColor(Color color)
    {
        wallColor = new Color(color.r, color.g, color.b, 0.7f);
    }

    private SpriteRenderer CreateWall(string name)
    {
        SpriteRenderer sr = CreateSprite(name, transform);
        sr.sprite = SpriteFactory.Square;
        sr.color = wallColor;
        sr.sortingLayerName = "Player";
        return sr;
    }

    private void CreateStars()
    {
        stars = new Transform[StarCount];
        starRenderers = new SpriteRenderer[StarCount];
        starSpeeds = new float[StarCount];
        starPhases = new float[StarCount];

        Transform parent = new GameObject("Stars").transform;
        parent.SetParent(transform);

        for (int i = 0; i < StarCount; i++)
        {
            SpriteRenderer sr = CreateSprite("Star", parent);
            sr.sprite = SpriteFactory.Circle;
            sr.color = Theme.Stars;
            sr.sortingLayerName = "Background";
            sr.sortingOrder = 1;
            float size = Random.Range(0.03f, 0.09f);
            sr.transform.localScale = new Vector3(size, size, 1f);

            stars[i] = sr.transform;
            starRenderers[i] = sr;
            starSpeeds[i] = Random.Range(0.1f, 0.4f);
            starPhases[i] = Random.Range(0f, Mathf.PI * 2f);
        }
    }

    private static SpriteRenderer CreateSprite(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent);
        return go.AddComponent<SpriteRenderer>();
    }

    private static void Place(Transform target, Vector2 position, Vector2 size, Sprite sprite)
    {
        Vector2 spriteSize = sprite.bounds.size;
        target.position = new Vector3(position.x, position.y, 0f);
        target.localScale = new Vector3(size.x / spriteSize.x, size.y / spriteSize.y, 1f);
    }
}
