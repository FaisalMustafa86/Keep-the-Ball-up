using UnityEngine;

// Draws the simple shapes the game uses in code, so the game doesn't depend on any image files.
// Every sprite is 1 unit tall, so scaling a transform to (w, h) gives a sprite w by h units in size.
public static class SpriteFactory
{
    private static Sprite circle;
    private static Sprite glow;
    private static Sprite pill;
    private static Sprite square;
    private static Sprite roundedBox;

    // Smooth, anti-aliased filled circle
    public static Sprite Circle
    {
        get
        {
            if (circle == null)
            {
                circle = Make(128, 128, (x, y) =>
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(63.5f, 63.5f));
                    return Mathf.Clamp01(63.5f - d);
                });
            }
            return circle;
        }
    }

    // Soft round glow that fades out to the edges
    public static Sprite Glow
    {
        get
        {
            if (glow == null)
            {
                glow = Make(128, 128, (x, y) =>
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(63.5f, 63.5f)) / 63.5f;
                    float a = Mathf.Clamp01(1f - d);
                    return a * a;
                });
            }
            return glow;
        }
    }

    // Rounded bar (4:1), used for the paddle. Scale it to any width/height.
    public static Sprite Pill
    {
        get
        {
            if (pill == null)
            {
                const int w = 256, h = 64;
                const float r = h / 2f;
                pill = Make(w, h, (x, y) =>
                {
                    float cx = Mathf.Clamp(x + 0.5f, r, w - r);
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, r));
                    return Mathf.Clamp01(r - d);
                });
            }
            return pill;
        }
    }

    public static Sprite Square
    {
        get
        {
            if (square == null) square = Make(4, 4, (x, y) => 1f);
            return square;
        }
    }

    // Rounded rectangle for UI buttons. It's 9-sliced, so it stretches to any size without bending the corners.
    public static Sprite RoundedBox
    {
        get
        {
            if (roundedBox == null)
            {
                const int size = 128;
                const float r = 48f;
                Texture2D tex = MakeTexture(size, size, (x, y) =>
                {
                    float cx = Mathf.Clamp(x + 0.5f, r, size - r);
                    float cy = Mathf.Clamp(y + 0.5f, r, size - r);
                    return Mathf.Clamp01(r - Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy)) + 0.5f);
                });
                roundedBox = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f,
                                           0, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
            }
            return roundedBox;
        }
    }

    // A vertical gradient from bottom colour to top colour
    public static Sprite VerticalGradient(Color bottom, Color top)
    {
        Texture2D tex = new Texture2D(1, 64, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        for (int y = 0; y < 64; y++)
        {
            tex.SetPixel(0, y, Color.Lerp(bottom, top, y / 63f));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 64), new Vector2(0.5f, 0.5f), 64f);
    }

    private static Sprite Make(int width, int height, System.Func<int, int, float> alphaAt)
    {
        Texture2D tex = MakeTexture(width, height, alphaAt);
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), height);
    }

    private static Texture2D MakeTexture(int width, int height, System.Func<int, int, float> alphaAt)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        Color32[] pixels = new Color32[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                byte a = (byte)(Mathf.Clamp01(alphaAt(x, y)) * 255f);
                pixels[y * width + x] = new Color32(255, 255, 255, a);
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        return tex;
    }
}
