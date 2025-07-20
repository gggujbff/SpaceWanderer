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
        
        // 注册悬浮窗打开/关闭事件
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
        // 当悬浮窗打开时，取消选中所有UI元素，防止ESC触发按钮事件
        EventSystem.current.SetSelectedGameObject(null);
    }

    // 处理悬浮窗关闭事件
    private void HandleWindowClosed()
    {
        // 当悬浮窗关闭时，重新选中继续按钮
        if (isPaused)
        {
            EventSystem.current.SetSelectedGameObject(continueButton.gameObject);
        }
    }

    private void OnDestroy()
    {
        // 注销事件监听
        if (settingsButtonHandler != null)
        {
            settingsButtonHandler.OnWindowOpened -= HandleWindowOpened;
            settingsButtonHandler.OnWindowClosed -= HandleWindowClosed;
        }
    }
}    