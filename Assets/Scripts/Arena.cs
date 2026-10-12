using UnityEngine;

// The play area. It zooms the camera so the play area is always the same width on every phone
// and fits the background to the screen. There are no visible walls: the ball bounces off the edges of the screen.
public class Arena : MonoBehaviour
{
    public static Arena Instance { get; private set; }

    // Width of the play area in world units, the same on every phone
    private const float PlayWidth = 9f;
    private const float MinHalfHeight = 7f;
    private const int StarCount = 60;

    // Edges of the play area: the left, right and top edges of the screen
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

    // Set when building the scene in the editor: outside Play mode, Screen.width/height aren't the Game view's size
    private Vector2Int? editorScreenSize;
    private int ScreenWidth => editorScreenSize?.x ?? Screen.width;
    private int ScreenHeight => editorScreenSize?.y ?? Screen.height;
    private Rect SafeArea => editorScreenSize.HasValue ? new Rect(0f, 0f, ScreenWidth, ScreenHeight) : Screen.safeArea;

    private SpriteRenderer background;
    private SpriteRenderer dangerZone;

    private Transform[] stars;
    private SpriteRenderer[] starRenderers;
    private float[] starSpeeds;
    private float[] starPhases;

    public void Init(Camera camera, Vector2Int? editorScreenSize = null)
    {
        Instance = this;
        this.editorScreenSize = editorScreenSize;
        cam = camera;
        center = cam.transform.position;

        if (SceneObjects.GetOrCreate(transform, "Background", out background))
        {
            background.sprite = SpriteFactory.BackgroundGradient;
            background.sortingLayerName = "Background";
            background.sortingOrder = 0;
        }

        if (SceneObjects.GetOrCreate(transform, "Danger Zone", out dangerZone))
        {
            dangerZone.sprite = SpriteFactory.DangerGradient;
            dangerZone.color = Theme.Danger;
            dangerZone.sortingLayerName = "Background";
            dangerZone.sortingOrder = 2;
        }

        RemoveOldWalls();

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
        lastScreenWidth = ScreenWidth;
        lastScreenHeight = ScreenHeight;
        lastSafeArea = SafeArea;

        float aspect = editorScreenSize.HasValue ? (float)ScreenWidth / ScreenHeight : cam.aspect;
        cam.orthographicSize = Mathf.Max(PlayWidth / 2f / aspect, MinHalfHeight);
        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * aspect;
        screenHalfWidth = halfWidth;

        float left = center.x - halfWidth;
        float right = center.x + halfWidth;
        float bottom = center.y - halfHeight;
        float top = center.y + halfHeight;

        // The paddle still sits above the home bar / navigation bar, so convert that part of the safe area to world height
        float safeBottom = bottom + SafeArea.yMin / ScreenHeight * halfHeight * 2f;

        // A little bigger than the screen so camera shake never shows the edges
        Place(background.transform, center, new Vector2(halfWidth * 2f + 2f, halfHeight * 2f + 2f), background.sprite);
        Place(dangerZone.transform, new Vector2(center.x, bottom + 0.75f), new Vector2(halfWidth * 2f, 1.5f), dangerZone.sprite);

        Left = left;
        Right = right;
        Top = top;
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

    // Earlier versions had visible walls saved in the scene; remove them
    private void RemoveOldWalls()
    {
        foreach (string name in new[] { "Left Wall", "Right Wall", "Top Wall" })
        {
            Transform wall = transform.Find(name);
            if (wall == null) continue;
            if (Application.isPlaying) Destroy(wall.gameObject);
            else DestroyImmediate(wall.gameObject);
        }
    }

    private void CreateStars()
    {
        stars = new Transform[StarCount];
        starRenderers = new SpriteRenderer[StarCount];
        starSpeeds = new float[StarCount];
        starPhases = new float[StarCount];

        SceneObjects.GetOrCreate(transform, "Stars", out GameObject starsObject);
        Transform parent = starsObject.transform;

        for (int i = 0; i < StarCount; i++)
        {
            if (SceneObjects.GetOrCreate(parent, "Star " + i, out SpriteRenderer sr))
            {
                sr.sprite = SpriteFactory.Circle;
                sr.color = Theme.Stars;
                sr.sortingLayerName = "Background";
                sr.sortingOrder = 1;
                float size = Random.Range(0.03f, 0.09f);
                sr.transform.localScale = new Vector3(size, size, 1f);
            }

            stars[i] = sr.transform;
            starRenderers[i] = sr;
            starSpeeds[i] = Random.Range(0.1f, 0.4f);
            starPhases[i] = Random.Range(0f, Mathf.PI * 2f);
        }
    }

    private static void Place(Transform target, Vector2 position, Vector2 size, Sprite sprite)
    {
        Vector2 spriteSize = sprite.bounds.size;
        target.position = new Vector3(position.x, position.y, 0f);
        target.localScale = new Vector3(size.x / spriteSize.x, size.y / spriteSize.y, 1f);
    }
}
