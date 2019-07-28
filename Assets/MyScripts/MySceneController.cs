using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MySceneController : MonoBehaviour {
    private string sceneName;

    private void Start()
    {
        sceneName = SceneManager.GetActiveScene().name;
    }

    public void SwitchScene()
    {
        if (sceneName == "Bone")
        {
            SceneManager.LoadScene("Muscle");
        }

        if (sceneName == "Muscle")
        {
            SceneManager.LoadScene("Bone");
        }
    }
}
