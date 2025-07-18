using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ContinueButtonController : MonoBehaviour
{
    public Button continueButton;
    public TMP_Text buttonText;

    void Start()
    {
        if (continueButton != null)
        {
            // 设置锚点
            continueButton.GetComponent<RectTransform>().anchorMin = new Vector2(0, 1);
            continueButton.GetComponent<RectTransform>().anchorMax = new Vector2(0, 1);

            // 设置轴心
            continueButton.GetComponent<RectTransform>().pivot = new Vector2(0, 1);

            // 设置位置
            continueButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(10, -10);

            // 设置大小
            continueButton.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 50);

            // 添加按钮点击事件监听
            continueButton.onClick.AddListener(OnContinueButtonClick);
        }

        if (buttonText != null)
        {
            // 设置按钮文本
            buttonText.text = "继续";
            // 设置字体大小
            buttonText.fontSize = 24;
            // 设置文本对齐方式
            buttonText.alignment = TextAlignmentOptions.Center;
        }
    }

    void OnContinueButtonClick()
    {
        // 在这里添加点击按钮后要执行的逻辑
        Debug.Log("继续按钮被点击！");
        // 例如，你可以在这里切换场景
        // UnityEngine.SceneManagement.SceneManager.LoadScene("NextScene");
    }
}