using UnityEngine;
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
}