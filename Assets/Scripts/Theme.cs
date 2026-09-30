using UnityEngine;

// All the game's colours in one place. Change these to re-theme the whole game.
public static class Theme
{
    public static readonly Color BackgroundTop = new Color32(0x0F, 0x0C, 0x29, 0xFF);
    public static readonly Color BackgroundBottom = new Color32(0x30, 0x2B, 0x63, 0xFF);
    public static readonly Color Stars = new Color32(0xFF, 0xFF, 0xFF, 0xFF);

    public static readonly Color Walls = new Color32(0x3D, 0xF5, 0xFF, 0xFF);
    public static readonly Color Danger = new Color32(0xFF, 0x3B, 0x6B, 0xFF);

    public static readonly Color Paddle = new Color32(0x3D, 0xF5, 0xFF, 0xFF);
    public static readonly Color Ball = new Color32(0xFF, 0xD2, 0x3F, 0xFF);
    public static readonly Color BallGlow = new Color32(0xFF, 0x8E, 0x3C, 0xFF);

    public static readonly Color Text = new Color32(0xF5, 0xF3, 0xFF, 0xFF);
    public static readonly Color Highlight = new Color32(0xFF, 0xD2, 0x3F, 0xFF);

    public static readonly Color ButtonPrimary = new Color32(0xFF, 0xD2, 0x3F, 0xFF);
    public static readonly Color ButtonPrimaryText = new Color32(0x1B, 0x10, 0x36, 0xFF);
    public static readonly Color ButtonSecondary = new Color32(0xFF, 0xFF, 0xFF, 0x26);

    // The walls change colour every level so players can feel the game moving on
    public static readonly Color[] LevelColors =
    {
        new Color32(0x3D, 0xF5, 0xFF, 0xFF), // cyan
        new Color32(0x5C, 0xFF, 0x8A, 0xFF), // green
        new Color32(0xB4, 0x7C, 0xFF, 0xFF), // purple
        new Color32(0xFF, 0x8E, 0x3C, 0xFF), // orange
        new Color32(0xFF, 0x5C, 0xC8, 0xFF), // pink
        new Color32(0xFF, 0x3B, 0x6B, 0xFF), // red
    };
}
