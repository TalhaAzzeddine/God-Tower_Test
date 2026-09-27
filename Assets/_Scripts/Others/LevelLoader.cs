using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelLoader : MonoBehaviour
{
    public void LoadLevel(string levelName)
    {
        if (string.IsNullOrEmpty(levelName))
        {
            Debug.LogError("[AutoLoad] Level name is empty.");
            return;
        }

        Debug.Log($"[AutoLoad] Loading level: {levelName}");

        SceneManager.LoadScene(levelName);
    }
}
