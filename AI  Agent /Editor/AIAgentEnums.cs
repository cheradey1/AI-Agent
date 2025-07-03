using UnityEngine;

namespace UnityAIAgent
{
    /// <summary>
    /// Типи ігор, які можна згенерувати
    /// </summary>
    public enum GameType
    {
        Platformer,
        FPS,
        TPS,
        Puzzle,
        Strategy,
        RPG,
        Simulation,
        Racing,
        Sports,
        Adventure,
        Casual,
        Card,
        Board,
        Educational,
        Survival,
        Horror,
        Sandbox
    }
    
    /// <summary>
    /// Кількість гравців у грі
    /// </summary>
    public enum PlayerCount
    {
        SinglePlayer,
        TwoPlayer,
        ThreeToFourPlayer,
        FiveToEightPlayer,
        MassiveMultiplayer
    }
    
    /// <summary>
    /// Художні стилі для гри
    /// </summary>
    public enum ArtStyle
    {
        Realistic,
        Cartoon,
        Pixel,
        LowPoly,
        Stylized,
        Anime,
        Retro,
        Minimalist,
        HandDrawn,
        SciFi,
        Fantasy,
        Steampunk,
        PostApocalyptic,
        Cyberpunk,
        Medieval
    }
    
    /// <summary>
    /// Розміри карт/рівнів
    /// </summary>
    public enum MapSize
    {
        Tiny,
        Small,
        Medium,
        Large,
        Huge,
        Infinite
    }
    
    /// <summary>
    /// Колірні теми для інтерфейсу
    /// </summary>
    public enum ColorTheme
    {
        Light,
        Dark,
        Blue,
        Green,
        Red,
        Purple,
        Orange,
        Custom
    }
}
