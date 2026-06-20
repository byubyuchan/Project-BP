using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Scene Settings")]
    public string customGameSceneName = "CustomLobbyScene";

    public void OnCustomGameClicked()
    {
        SceneManager.LoadScene(customGameSceneName);
    }

    public void OnQuitButtonClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}