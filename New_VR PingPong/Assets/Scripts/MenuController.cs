using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    public Robot robot;

    const float BASE_FIXED_DT = 0.02f;
    public void PlayAIMode() => SceneManager.LoadScene("AI_Modified");
    public void PlayFeederMode() => SceneManager.LoadScene("Feeder_Modified");
    public void BackLobby() => SceneManager.LoadScene("Main");
    public void Quit() => Application.Quit();
    void SetGlobalTime(float scale)
    {
        Time.timeScale = scale;
        Time.fixedDeltaTime = BASE_FIXED_DT * scale;
    }

    public void SetSlow() => SetGlobalTime(0.5f);
    public void SetNormal() => SetGlobalTime(1.0f);
    public void SetFast() => SetGlobalTime(1.2f);
    public void SetEasyDifficulty() => DifficultySettings.Current = DifficultyLevel.Easy;
    public void SetNormalDifficulty() => DifficultySettings.Current = DifficultyLevel.Normal;
    public void SetHardDifficulty() => DifficultySettings.Current = DifficultyLevel.Hard;
}
