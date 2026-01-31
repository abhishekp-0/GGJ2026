using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor; 
#endif

public class ScenePicker : MonoBehaviour
{
    // ---------------------------------------------------------
    // Drag your Scene file into this slot in the Inspector
    // ---------------------------------------------------------
    #if UNITY_EDITOR
    public SceneAsset sceneToLoad;
    #endif

    // This hidden variable stores the name for the game to use
    [HideInInspector]
    public string sceneNameString;

    // This runs automatically in the Editor to grab the name
    private void OnValidate()
    {
        #if UNITY_EDITOR
        if (sceneToLoad != null)
        {
            sceneNameString = sceneToLoad.name;
        }
        #endif
    }

    // LINK THIS FUNCTION TO YOUR BUTTON
    public void LoadGameScene()
    {
        if (!string.IsNullOrEmpty(sceneNameString))
        {
            SceneManager.LoadScene(sceneNameString);
        }
        else
        {
            Debug.LogError("Error: You forgot to drag a Scene file into the script!");
        }
    }
}
