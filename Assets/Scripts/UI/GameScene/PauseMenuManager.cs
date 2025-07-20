using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PauseMenuManager : MonoBehaviour
{
    public GameObject stopUI;
    public Button continueButton;

    private bool isPaused = false;

    void Start()
    {
        // 确保初始时滤镜和按钮是隐藏的
        stopUI.gameObject.SetActive(false);
        continueButton.gameObject.SetActive(false);

        // 为继续按钮添加点击事件
        continueButton.onClick.AddListener(ResumeGame);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && !isPaused)
        {
            PauseGame();
        }
    }

    void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f; // 暂停游戏时间

        // 显示滤镜和按钮
        stopUI.gameObject.SetActive(true);
        continueButton.gameObject.SetActive(true);

        // 选中继续按钮
        EventSystem.current.SetSelectedGameObject(continueButton.gameObject);
    }

    void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f; // 恢复游戏时间

        // 隐藏滤镜和按钮
        stopUI.gameObject.SetActive(false);
        continueButton.gameObject.SetActive(false);
    }
}