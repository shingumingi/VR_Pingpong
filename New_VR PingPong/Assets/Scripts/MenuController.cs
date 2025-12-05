using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    public Robot robot;
    public void PlayAIMode() => SceneManager.LoadScene("AI_Modified");
    public void PlayFeederMode() => SceneManager.LoadScene("Feeder_Modified");
    public void BackLobby() => SceneManager.LoadScene("Main");
    public void Quit() => Application.Quit();
    public void SetSlow()
    {
        Ball.speedScale = 0.5f;
        robot.strokeSpeedScale = 2.0f;
        robot.swing_duration = 2f; // ´À¸®°Ô
        robot.recovery_time = 2f;
        robot.movement_speed = 0; // Slow
        robot.speed = 0; // Slow ball speed when calculating returns
        robot.set_movement_speed();
    }

    public void SetNormal()
    {
        Ball.speedScale = 1f;
        robot.strokeSpeedScale = 1.0f;
        robot.swing_duration = 0.3f;
        robot.recovery_time = 0.5f;
        robot.movement_speed = 1;
        robot.speed = 1;
        robot.set_movement_speed();
    }

    public void SetFast()
    {
        Ball.speedScale = 1.5f;
        robot.strokeSpeedScale = 1.5f;
        robot.swing_duration = 0.2f;
        robot.recovery_time = 0.35f;
        robot.movement_speed = 2;
        robot.speed = 2;
        robot.set_movement_speed();
    }
    public void SetEasyDifficulty() => DifficultySettings.Current = DifficultyLevel.Easy;
    public void SetNormalDifficulty() => DifficultySettings.Current = DifficultyLevel.Normal;
    public void SetHardDifficulty() => DifficultySettings.Current = DifficultyLevel.Hard;
}
