using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuBehaviour : MonoBehaviour
{
    public GameObject shopMenu;
    public GameObject settingsMenu;
    public GameObject modeSelectionMenu;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        shopMenu.SetActive(false);
        settingsMenu.SetActive(false);
        modeSelectionMenu.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetShopMenu()
    {
        shopMenu.SetActive(true);
    }

    public void SetSettingsMenu()
    {
        settingsMenu.SetActive(true);
    }

    public void SetModeSelectionMenu()
    {
        modeSelectionMenu.SetActive(true);
    }

    public void PlayGame()
    {
        SceneManager.LoadScene(1);
    }
}
