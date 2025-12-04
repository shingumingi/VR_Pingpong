using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    public void PlayAIMode() => SceneManager.LoadScene("AI_Modified");
    public void PlayFeederMode() => SceneManager.LoadScene("Feeder_Modified");
    public void BackLobby() => SceneManager.LoadScene("Main");
    public void Quit() => Application.Quit();
    public void SetSlow() => Ball.speedScale = 0.5f;
    public void SetNormal() => Ball.speedScale = 1f;
    public void SetFast() => Ball.speedScale = 1.5f;
    public void SetEasyDifficulty() => DifficultySettings.Current = DifficultyLevel.Easy;
    public void SetNormalDifficulty() => DifficultySettings.Current = DifficultyLevel.Normal;
    public void SetHardDifficulty() => DifficultySettings.Current = DifficultyLevel.Hard;
}
