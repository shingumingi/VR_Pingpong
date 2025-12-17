using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    public Robot robot;
    public void PlayAIMode() => SceneManager.LoadScene("AI_Modified");
    public void PlayFeederMode() => SceneManager.LoadScene("Feeder_Modified");
    public void BackLobby() => SceneManager.LoadScene("Main");
    public void Quit() => Application.Quit();
    float baseFixedDelta;

    void Awake()
    {
        baseFixedDelta = Time.fixedDeltaTime;
    }

    public void SetSlow()
    {
        Time.timeScale = 0.5f;
        Time.fixedDeltaTime = baseFixedDelta * Time.timeScale;
    }

    public void SetNormal()
    {
        Time.timeScale = 1.0f;
        Time.fixedDeltaTime = baseFixedDelta;
    }

    public void SetFast()
    {
        Time.timeScale = 1.2f;
        Time.fixedDeltaTime = baseFixedDelta * Time.timeScale;
    }
    public void SetEasyDifficulty() => DifficultySettings.Current = DifficultyLevel.Easy;
    public void SetNormalDifficulty() => DifficultySettings.Current = DifficultyLevel.Normal;
    public void SetHardDifficulty() => DifficultySettings.Current = DifficultyLevel.Hard;
}
