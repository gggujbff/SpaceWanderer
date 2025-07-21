using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PauseMenuManager : MonoBehaviour
{
    public GameObject stopUI;
    public Button continueButton;
    public SettingsButtonHandler settingsButtonHandler; // 引用设置按钮处理器

    private bool isPaused = false;

    void Start()
    {
        stopUI.SetActive(false);
        continueButton.gameObject.SetActive(false);
        continueButton.onClick.AddListener(ResumeGame);
        
        if (settingsButtonHandler != null)
        {
            settingsButtonHandler.OnWindowOpened += HandleWindowOpened;
            settingsButtonHandler.OnWindowClosed += HandleWindowClosed;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!isPaused)
            {
                PauseGame();
            }
            else if (settingsButtonHandler == null || !settingsButtonHandler.IsWindowOpen())
            {
                ResumeGame();
            }
        }
    }

    void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
        stopUI.SetActive(true);
        continueButton.gameObject.SetActive(true);
        EventSystem.current.SetSelectedGameObject(continueButton.gameObject);
    }

    void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        stopUI.SetActive(false);
        continueButton.gameObject.SetActive(false);
    }

    // 处理悬浮窗打开事件
    private void HandleWindowOpened()
    {
        EventSystem.current.SetSelectedGameObject(null);
    }

    // 处理悬浮窗关闭事件
    private void HandleWindowClosed()
    {
        if (isPaused)
        {
            EventSystem.current.SetSelectedGameObject(continueButton.gameObject);
        }
    }

    private void OnDestroy()
    {
        if (settingsButtonHandler != null)
        {
            settingsButtonHandler.OnWindowOpened -= HandleWindowOpened;
            settingsButtonHandler.OnWindowClosed -= HandleWindowClosed;
        }
    }
}    