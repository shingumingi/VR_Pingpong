using UnityEngine;

public enum DifficultyLevel
{
    Easy,
    Normal,
    Hard
}

public static class DifficultySettings
{
    // 기본값은 Normal
    public static DifficultyLevel Current = DifficultyLevel.Normal;
}
