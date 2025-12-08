using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement; // Essential for scene management

public class MenuController : MonoBehaviour
{
    // Make sure this name matches the scene in your Build Settings exactly!
    public string sceneToLoad = "GameScene";

    // This public function can be called by a Button's OnClick event
    public void StartGame()
    {
        SceneManager.LoadScene(sceneToLoad);
    }
    public void ExitGame()
    {
        #if UNITY_EDITOR
        // This code will ONLY execute when running in the Unity Editor
        // It stops the play session in the Editor
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif

    }
    public void Settings()
    {
        Debug.Log("Feature not implemented yet");
        // Implement settings functionality here
    }
}