using UnityEngine;

public enum GameMode { Easy, Normal, Hard, Chaos }

// The difficulty modes the player picks on the menu. Each one tunes the ball, the paddle and the power-ups.
public class Difficulty
{
    public string Name;
    public string Description;
    public Color Color;

    // Multiplies the ball's start and top speed
    public float BallSpeed;
    // Multiplies how quickly the ball speeds up over time and on each hit
    public float SpeedGain;
    // Multiplies the paddle's width
    public float PaddleWidth;
    // Average seconds between power-ups
    public float PowerUpInterval;
    // Chance that a power-up is a bad one (TINY paddle or FAST ball)
    public float BadChance;
    // How fast power-ups fall, in units per second
    public float FallSpeed;

    public static readonly Difficulty[] All =
    {
        new Difficulty
        {
            Name = "EASY", Description = "Slower ball, wider paddle", Color = Theme.LevelColors[1],
            BallSpeed = 0.8f, SpeedGain = 0.6f, PaddleWidth = 1.25f, PowerUpInterval = 8f, BadChance = 0f, FallSpeed = 2.2f
        },
        new Difficulty
        {
            Name = "NORMAL", Description = "Some power-ups are bad", Color = Theme.LevelColors[0],
            BallSpeed = 1f, SpeedGain = 1f, PaddleWidth = 1f, PowerUpInterval = 10f, BadChance = 0.25f, FallSpeed = 2.6f
        },
        new Difficulty
        {
            Name = "HARD", Description = "Fast ball, narrow paddle", Color = Theme.LevelColors[3],
            BallSpeed = 1.2f, SpeedGain = 1.4f, PaddleWidth = 0.85f, PowerUpInterval = 12f, BadChance = 0.4f, FallSpeed = 3.2f
        },
        new Difficulty
        {
            Name = "CHAOS", Description = "Power-ups everywhere, half bad", Color = Theme.LevelColors[4],
            BallSpeed = 1.05f, SpeedGain = 1.2f, PaddleWidth = 1f, PowerUpInterval = 3.5f, BadChance = 0.5f, FallSpeed = 3f
        },
    };

    public static Difficulty Current => All[(int)Settings.Mode];
}
