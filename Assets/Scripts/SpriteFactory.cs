using UnityEngine;

// Draws the shapes and icons the game uses. Tools > Keep the Ball Up > Build Scene saves them as PNG files in Assets/Sprites,
// so the objects saved in the scene can use them. If a file is missing, the sprite is drawn in code instead.
// Every sprite is 1 unit tall, so scaling a transform to (w, h) gives a sprite w by h units in size.
// Icons are white so they can be tinted any colour.
public static class SpriteFactory
{
    public const string AssetFolder = "Assets/Sprites/";

    public enum Shape
    {
        Circle, Glow, Pill, Square, RoundedBox, BackgroundGradient, DangerGradient,
        Ring, PauseIcon, PlayIcon, RestartIcon, HomeIcon, SoundIcon, MusicIcon, VibrateIcon,
        WideIcon, SlowIcon, ShieldIcon, DoubleIcon, TinyIcon, FastIcon, RoundedOutline
    }

    private const float RoundedBoxRadius = 48f;
    private const int IconSize = 128;

    private static readonly Sprite[] cache = new Sprite[System.Enum.GetValues(typeof(Shape)).Length];

    // Smooth, anti-aliased filled circle
    public static Sprite Circle => Get(Shape.Circle);

    // Soft round glow that fades out to the edges
    public static Sprite Glow => Get(Shape.Glow);

    // Rounded bar (4:1), used for the paddle. Scale it to any width/height.
    public static Sprite Pill => Get(Shape.Pill);

    public static Sprite Square => Get(Shape.Square);

    // Rounded rectangle for UI buttons. It's 9-sliced, so it stretches to any size without bending the corners.
    public static Sprite RoundedBox => Get(Shape.RoundedBox);

    // Just the edge of RoundedBox, for button outlines. Also 9-sliced.
    public static Sprite RoundedOutline => Get(Shape.RoundedOutline);

    // The background colours, from bottom to top
    public static Sprite BackgroundGradient => Get(Shape.BackgroundGradient);

    // White fading out upwards, tinted red for the danger zone at the bottom of the screen
    public static Sprite DangerGradient => Get(Shape.DangerGradient);

    // Circle outline, used for power-up orbs and the HUD timer rings
    public static Sprite Ring => Get(Shape.Ring);

    public static Sprite Icon(Shape icon) => Get(icon);

    public static Sprite PowerUpIcon(PowerUpType type) => Get(PowerUpIconShape(type));

    // The icons are listed in the same order as PowerUpType
    public static Shape PowerUpIconShape(PowerUpType type) => Shape.WideIcon + (int)type;

    private static Sprite Get(Shape shape)
    {
        int i = (int)shape;
        if (cache[i] == null)
        {
#if UNITY_EDITOR
            // Use the saved PNG, so objects saved in the scene point to a real file
            cache[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(AssetFolder + shape + ".png");
#endif
            if (cache[i] == null)
            {
                Texture2D tex = Draw(shape);
                cache[i] = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f),
                                         PixelsPerUnit(shape), 0, SpriteMeshType.FullRect, Border(shape));
            }
        }
        return cache[i];
    }

    // Forget loaded sprites, e.g. after the PNG files have just been saved
    public static void ClearCache()
    {
        System.Array.Clear(cache, 0, cache.Length);
    }

    public static float PixelsPerUnit(Shape shape)
    {
        switch (shape)
        {
            case Shape.Pill: return 64f;
            case Shape.Square: return 4f;
            case Shape.RoundedBox:
            case Shape.RoundedOutline: return 100f;
            case Shape.BackgroundGradient:
            case Shape.DangerGradient: return 64f;
            default: return 128f;
        }
    }

    // 9-slice border (left, bottom, right, top) in pixels
    public static Vector4 Border(Shape shape)
    {
        return shape == Shape.RoundedBox || shape == Shape.RoundedOutline ? Vector4.one * RoundedBoxRadius : Vector4.zero;
    }

    public static Texture2D Draw(Shape shape)
    {
        Color32[] pixels = Pixels(shape, out int width, out int height);
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        tex.SetPixels32(pixels);
        tex.Apply();
        return tex;
    }

    // The pixels of a shape, bottom row first
    public static Color32[] Pixels(Shape shape, out int width, out int height)
    {
        switch (shape)
        {
            case Shape.Circle:
                return Paint(128, 128, out width, out height, (x, y) =>
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(63.5f, 63.5f));
                    return White(63.5f - d);
                });

            case Shape.Glow:
                return Paint(128, 128, out width, out height, (x, y) =>
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(63.5f, 63.5f)) / 63.5f;
                    float a = Mathf.Clamp01(1f - d);
                    return White(a * a);
                });

            case Shape.Pill:
            {
                const int w = 256, h = 64;
                const float r = h / 2f;
                return Paint(w, h, out width, out height, (x, y) =>
                {
                    float cx = Mathf.Clamp(x + 0.5f, r, w - r);
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, r));
                    return White(r - d);
                });
            }

            case Shape.Square:
                return Paint(4, 4, out width, out height, (x, y) => Color.white);

            case Shape.RoundedBox:
            {
                const int size = 128;
                const float r = RoundedBoxRadius;
                return Paint(size, size, out width, out height, (x, y) =>
                {
                    float cx = Mathf.Clamp(x + 0.5f, r, size - r);
                    float cy = Mathf.Clamp(y + 0.5f, r, size - r);
                    return White(r - Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy)) + 0.5f);
                });
            }

            case Shape.RoundedOutline:
            {
                const int size = 128;
                const float r = RoundedBoxRadius;
                const float thickness = 5f;
                return Paint(size, size, out width, out height, (x, y) =>
                {
                    float cx = Mathf.Clamp(x + 0.5f, r, size - r);
                    float cy = Mathf.Clamp(y + 0.5f, r, size - r);
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                    return White(Mathf.Clamp01(r - d + 0.5f) - Mathf.Clamp01(r - thickness - d + 0.5f));
                });
            }

            case Shape.BackgroundGradient:
                return Paint(1, 64, out width, out height, (x, y) => Color.Lerp(Theme.BackgroundBottom, Theme.BackgroundTop, y / 63f));

            case Shape.DangerGradient:
                return Paint(1, 64, out width, out height, (x, y) => Color.Lerp(new Color(1f, 1f, 1f, 0.35f), new Color(1f, 1f, 1f, 0f), y / 63f));

            default:
                return PaintIcon(IconDistance(shape), out width, out height);
        }
    }

    // ---------- Icons ----------
    // Each icon is a signed distance function on a square from -1 to 1 (y up): negative inside, positive outside.
    // That gives smooth anti-aliased edges at any size and makes shapes easy to combine.

    private delegate float Distance(Vector2 p);

    private static Distance IconDistance(Shape shape)
    {
        switch (shape)
        {
            case Shape.Ring:
                return p => Mathf.Abs(p.magnitude - 0.86f) - 0.1f;

            case Shape.PauseIcon:
                return p => Mathf.Min(Box(p, new Vector2(-0.28f, 0f), new Vector2(0.13f, 0.52f), 0.08f),
                                      Box(p, new Vector2(0.28f, 0f), new Vector2(0.13f, 0.52f), 0.08f));

            case Shape.PlayIcon:
                return p => Polygon(p, new Vector2(-0.36f, -0.52f), new Vector2(-0.36f, 0.52f), new Vector2(0.52f, 0f)) - 0.07f;

            case Shape.RestartIcon:
            {
                // A circular arrow: an arc with an arrowhead closing the gap at the top right
                float r = 0.5f;
                float end = 40f * Mathf.Deg2Rad;
                Vector2 tip = new Vector2(Mathf.Cos(end), Mathf.Sin(end)) * r;
                Vector2 radial = tip.normalized;
                Vector2 tangent = new Vector2(-radial.y, radial.x);
                return p => Mathf.Min(Arc(p, Vector2.zero, r, 100f, 400f, 0.1f),
                                      Polygon(p, tip + radial * 0.3f, tip + tangent * 0.36f, tip - radial * 0.3f) - 0.03f);
            }

            case Shape.HomeIcon:
                return p =>
                {
                    float roof = Polygon(p, new Vector2(-0.72f, 0.02f), new Vector2(0f, 0.68f), new Vector2(0.72f, 0.02f)) - 0.04f;
                    float walls = Box(p, new Vector2(0f, -0.3f), new Vector2(0.46f, 0.36f), 0.05f);
                    float door = Box(p, new Vector2(0f, -0.43f), new Vector2(0.13f, 0.24f), 0.04f);
                    return Mathf.Max(Mathf.Min(roof, walls), -door);
                };

            case Shape.SoundIcon:
                return p =>
                {
                    p += new Vector2(0.08f, 0f);
                    float body = Box(p, new Vector2(-0.45f, 0f), new Vector2(0.15f, 0.2f), 0.04f);
                    float cone = Polygon(p, new Vector2(-0.38f, 0.2f), new Vector2(0.02f, 0.52f), new Vector2(0.02f, -0.52f), new Vector2(-0.38f, -0.2f)) - 0.03f;
                    float wave1 = Arc(p, new Vector2(0.02f, 0f), 0.32f, -45f, 45f, 0.065f);
                    float wave2 = Arc(p, new Vector2(0.02f, 0f), 0.58f, -45f, 45f, 0.065f);
                    return Mathf.Min(Mathf.Min(body, cone), Mathf.Min(wave1, wave2));
                };

            case Shape.MusicIcon:
                return p =>
                {
                    // Two eighth notes joined by a beam
                    float heads = Mathf.Min(Ellipse(p, new Vector2(-0.36f, -0.46f), new Vector2(0.22f, 0.17f)),
                                            Ellipse(p, new Vector2(0.4f, -0.32f), new Vector2(0.22f, 0.17f)));
                    float stems = Mathf.Min(Box(p, new Vector2(-0.19f, 0.05f), new Vector2(0.055f, 0.5f), 0.02f),
                                            Box(p, new Vector2(0.57f, 0.19f), new Vector2(0.055f, 0.5f), 0.02f));
                    float beam = Polygon(p, new Vector2(-0.245f, 0.38f), new Vector2(0.625f, 0.52f), new Vector2(0.625f, 0.72f), new Vector2(-0.245f, 0.58f));
                    return Mathf.Min(heads, Mathf.Min(stems, beam));
                };

            case Shape.VibrateIcon:
                return p =>
                {
                    float phone = Mathf.Max(Box(p, Vector2.zero, new Vector2(0.27f, 0.5f), 0.09f),
                                            -Box(p, new Vector2(0f, 0.02f), new Vector2(0.16f, 0.35f), 0.03f));
                    // Zigzag "buzz" lines on both sides
                    Vector2 m = new Vector2(Mathf.Abs(p.x), p.y);
                    float buzz = Mathf.Min(Mathf.Min(Segment(m, new Vector2(0.45f, 0.32f), new Vector2(0.6f, 0.11f), 0.05f),
                                                     Segment(m, new Vector2(0.6f, 0.11f), new Vector2(0.45f, -0.11f), 0.05f)),
                                           Segment(m, new Vector2(0.45f, -0.11f), new Vector2(0.6f, -0.32f), 0.05f));
                    return Mathf.Min(phone, buzz);
                };

            case Shape.WideIcon:
                return p =>
                {
                    // Arrows pointing outwards
                    Vector2 m = new Vector2(Mathf.Abs(p.x), p.y);
                    float shaft = Box(m, new Vector2(0.25f, 0f), new Vector2(0.3f, 0.09f), 0.03f);
                    float head = Polygon(m, new Vector2(0.45f, 0.32f), new Vector2(0.78f, 0f), new Vector2(0.45f, -0.32f)) - 0.03f;
                    return Mathf.Min(shaft, head);
                };

            case Shape.TinyIcon:
                return p =>
                {
                    // Arrows pointing inwards at a centre line
                    Vector2 m = new Vector2(Mathf.Abs(p.x), p.y);
                    float shaft = Box(m, new Vector2(0.58f, 0f), new Vector2(0.2f, 0.09f), 0.03f);
                    float head = Polygon(m, new Vector2(0.42f, 0.3f), new Vector2(0.14f, 0f), new Vector2(0.42f, -0.3f)) - 0.03f;
                    float bar = Box(p, Vector2.zero, new Vector2(0.05f, 0.42f), 0.03f);
                    return Mathf.Min(Mathf.Min(shaft, head), bar);
                };

            case Shape.SlowIcon:
                return p =>
                {
                    // A clock
                    float face = Mathf.Abs(p.magnitude - 0.58f) - 0.09f;
                    float hourHand = Segment(p, Vector2.zero, new Vector2(0f, 0.34f), 0.07f);
                    float minuteHand = Segment(p, Vector2.zero, new Vector2(0.26f, -0.08f), 0.07f);
                    return Mathf.Min(face, Mathf.Min(hourHand, minuteHand));
                };

            case Shape.ShieldIcon:
                return p => Polygon(p, new Vector2(0f, 0.68f), new Vector2(0.56f, 0.48f), new Vector2(0.5f, -0.08f),
                                    new Vector2(0f, -0.7f), new Vector2(-0.5f, -0.08f), new Vector2(-0.56f, 0.48f)) - 0.04f;

            case Shape.DoubleIcon:
            {
                // A five-pointed star
                Vector2[] star = new Vector2[10];
                for (int i = 0; i < 10; i++)
                {
                    float angle = (90f + i * 36f) * Mathf.Deg2Rad;
                    float radius = i % 2 == 0 ? 0.72f : 0.3f;
                    star[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) - 0.06f) * radius;
                }
                return p => Polygon(p, star) - 0.03f;
            }

            default: // FastIcon
                return p => Polygon(p, new Vector2(0.18f, 0.74f), new Vector2(-0.46f, -0.06f), new Vector2(-0.04f, -0.06f),
                                    new Vector2(-0.18f, -0.74f), new Vector2(0.46f, 0.1f), new Vector2(0.05f, 0.1f)) - 0.02f;
        }
    }

    private static float Box(Vector2 p, Vector2 center, Vector2 halfSize, float round)
    {
        Vector2 d = new Vector2(Mathf.Abs(p.x - center.x), Mathf.Abs(p.y - center.y)) - halfSize + Vector2.one * round;
        return new Vector2(Mathf.Max(d.x, 0f), Mathf.Max(d.y, 0f)).magnitude + Mathf.Min(Mathf.Max(d.x, d.y), 0f) - round;
    }

    private static float Ellipse(Vector2 p, Vector2 center, Vector2 radius)
    {
        // Close enough for small, round shapes like note heads
        Vector2 q = new Vector2((p.x - center.x) / radius.x, (p.y - center.y) / radius.y);
        return (q.magnitude - 1f) * Mathf.Min(radius.x, radius.y);
    }

    private static float Segment(Vector2 p, Vector2 a, Vector2 b, float thickness)
    {
        Vector2 pa = p - a, ba = b - a;
        float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
        return (pa - ba * h).magnitude - thickness;
    }

    // A thick arc from one angle to another (degrees, anticlockwise), with rounded ends
    private static float Arc(Vector2 p, Vector2 center, float radius, float fromDegrees, float toDegrees, float thickness)
    {
        const int steps = 32;
        float d = float.MaxValue;
        Vector2 previous = center + Direction(fromDegrees) * radius;
        for (int i = 1; i <= steps; i++)
        {
            Vector2 next = center + Direction(Mathf.Lerp(fromDegrees, toDegrees, (float)i / steps)) * radius;
            d = Mathf.Min(d, Segment(p, previous, next, thickness));
            previous = next;
        }
        return d;
    }

    private static Vector2 Direction(float degrees)
    {
        float r = degrees * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(r), Mathf.Sin(r));
    }

    // Any polygon (points in order); works for concave shapes like the star and lightning bolt
    private static float Polygon(Vector2 p, params Vector2[] v)
    {
        float d = Vector2.Dot(p - v[0], p - v[0]);
        float sign = 1f;
        for (int i = 0, j = v.Length - 1; i < v.Length; j = i, i++)
        {
            Vector2 e = v[j] - v[i];
            Vector2 w = p - v[i];
            Vector2 b = w - e * Mathf.Clamp01(Vector2.Dot(w, e) / Vector2.Dot(e, e));
            d = Mathf.Min(d, Vector2.Dot(b, b));
            bool c1 = p.y >= v[i].y, c2 = p.y < v[j].y, c3 = e.x * w.y > e.y * w.x;
            if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) sign = -sign;
        }
        return sign * Mathf.Sqrt(d);
    }

    // ---------- Painting ----------

    private static Color White(float alpha) => new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));

    private static Color32[] PaintIcon(Distance distance, out int width, out int height)
    {
        const float pixelsPerUnit = IconSize / 2f;
        return Paint(IconSize, IconSize, out width, out height, (x, y) =>
        {
            Vector2 p = new Vector2((x + 0.5f) / pixelsPerUnit - 1f, (y + 0.5f) / pixelsPerUnit - 1f);
            return White(0.5f - distance(p) * pixelsPerUnit);
        });
    }

    private static Color32[] Paint(int width, int height, out int outWidth, out int outHeight, System.Func<int, int, Color> colorAt)
    {
        outWidth = width;
        outHeight = height;
        Color32[] pixels = new Color32[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                pixels[y * width + x] = colorAt(x, y);
            }
        }
        return pixels;
    }
}
