using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    public void PlayAIMode() => SceneManager.LoadScene("AI");
    public void PlayFeederMode() => SceneManager.LoadScene("Machine");
    public void BackLobby() => SceneManager.LoadScene("Main");
    public void Quit() => Application.Quit();
}
