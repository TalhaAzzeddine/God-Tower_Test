using UnityEngine;
using UnityEngine.SceneManagement;

public class AutoLoadGameScene : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "GameScene";


    public void StartGame()
    {
        LoadGameScene();

    }

    private void LoadGameScene()
    {
        if (string.IsNullOrEmpty(gameSceneName))
        {
            Debug.LogError("Game scene name is empty.");
            return;
        }

        Debug.Log($"[AutoLoad] Loading scene: {gameSceneName}");

        SceneManager.LoadScene(gameSceneName);
    }
}