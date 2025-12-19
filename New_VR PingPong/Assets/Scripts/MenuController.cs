using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    public Robot robot;
    public void PlayAIMode() => SceneManager.LoadScene("AI_Modified");
    public void PlayFeederMode() => SceneManager.LoadScene("Feeder_Modified");
    public void BackLobby() => SceneManager.LoadScene("Main");
    public void Quit() => Application.Quit();

    void SetTime(float s)
    {
        Time.timeScale = s;
        Time.fixedDeltaTime = 0.02f * s;
    }

    public void SetSlow() => SetTime(0.5f);
    public void SetNormal() => SetTime(0.8f);
    public void SetHard() => SetTime(1.1f);
    public void SetEasyDifficulty() => DifficultySettings.Current = DifficultyLevel.Easy;
    public void SetNormalDifficulty() => DifficultySettings.Current = DifficultyLevel.Normal;
    public void SetHardDifficulty() => DifficultySettings.Current = DifficultyLevel.Hard;
}
