using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class LevelLoader : MonoBehaviour
{
    [Header("Drag your Black Panel here")]
    public GameObject loadingScreenPanel;

    [Header("Type the name of your scene (e.g. 'Level1')")]
    public string sceneToLoad;

    public void LoadGame()
    {
        // 1. Turn on the loading screen
        if (loadingScreenPanel != null)
        {
            loadingScreenPanel.SetActive(true);
        }

        // 2. Start the game
        StartCoroutine(LoadSceneAsync());
    }

    IEnumerator LoadSceneAsync()
    {
        // This loads the scene you typed in the Inspector
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneToLoad);

        // Wait until it's done
        while (!operation.isDone)
        {
            yield return null;
        }
    }
}