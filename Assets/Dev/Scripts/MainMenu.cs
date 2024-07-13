using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public GameObject mainMenu;
    public GameObject settingsMenu;

    public void StartGame()
    {
        SceneManager.LoadSceneAsync("LevelOne");
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void OptionsMenu()
    {
        if (mainMenu.activeInHierarchy == true)
        {
            mainMenu.SetActive(false);
            settingsMenu.SetActive(true);
        }else if (settingsMenu.activeInHierarchy == true)
        {
            mainMenu.SetActive(true);
            settingsMenu.SetActive(false);
        }
    }
}
