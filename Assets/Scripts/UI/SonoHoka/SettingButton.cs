using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))] // 确保必须有Button组件
public class SettingButton : MonoBehaviour
{
    private Button closeButton;
    private Canvas targetCanvas;

    private void Start()
    {
        // 获取按钮组件
        closeButton = GetComponent<Button>();
        
        // 查找最近的Canvas组件（向上查找父级）
        targetCanvas = GetComponentInParent<Canvas>();

        if (closeButton != null && targetCanvas != null)
        {
            // 添加点击事件
            closeButton.onClick.AddListener(CloseCanvas);
        }
        else
        {
            Debug.LogError("未找到按钮或Canvas组件！");
        }
    }

    private void CloseCanvas()
    {
        // 删除整个Canvas对象
        if (targetCanvas != null)
        {
            Destroy(targetCanvas.gameObject);
        }
    }
}