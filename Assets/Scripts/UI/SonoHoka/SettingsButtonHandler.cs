using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[RequireComponent(typeof(Button))]
public class SettingsButtonHandler : MonoBehaviour
{
    [Header("设置")]
    [SerializeField] private GameObject windowPrefab; // 悬浮窗预制体
    [SerializeField] private bool useOverlay = true; // 是否使用遮罩层
    [SerializeField] private Color overlayColor = new Color(0, 0, 0, 0.5f); // 遮罩颜色
    [SerializeField] private int baseSortingOrder = 10; // 基础层级（确保在其他UI上方）

    private Button spawnButton;
    private GameObject currentWindow;
    private GameObject overlay; // 遮罩层（作为悬浮窗的子物体）
    private List<Selectable> disabledUIElements = new List<Selectable>(); // 记录被禁用的UI

    private void Start()
    {
        spawnButton = GetComponent<Button>();
        if (spawnButton != null)
        {
            spawnButton.onClick.AddListener(SpawnWindow);
        }
        else
        {
            Debug.LogError("当前物体上没有Button组件！");
        }
    }

    private void SpawnWindow()
    {
        // 避免重复创建
        if (currentWindow != null) return;

        // 1. 创建悬浮窗（直接在场景根节点）
        currentWindow = Instantiate(windowPrefab);
        currentWindow.name = windowPrefab.name + "(Clone)";

        // 确保悬浮窗有Canvas（如果没有则自动添加）
        Canvas windowCanvas = currentWindow.GetComponent<Canvas>();
        if (windowCanvas == null)
        {
            windowCanvas = currentWindow.AddComponent<Canvas>();
            windowCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            currentWindow.AddComponent<CanvasScaler>();
            currentWindow.AddComponent<GraphicRaycaster>();
        }
        // 设置悬浮窗的层级（确保在遮罩上方）
        windowCanvas.sortingOrder = baseSortingOrder + 1;

        // 定位悬浮窗到屏幕中心
        RectTransform windowRect = currentWindow.GetComponent<RectTransform>();
        if (windowRect != null)
        {
            windowRect.anchorMin = new Vector2(0.5f, 0.5f);
            windowRect.anchorMax = new Vector2(0.5f, 0.5f);
            windowRect.pivot = new Vector2(0.5f, 0.5f);
            windowRect.anchoredPosition = Vector2.zero;
        }

        // 2. 创建遮罩并设为悬浮窗的子物体（实现位置跟随）
        if (useOverlay)
        {
            CreateOverlayAsChild(currentWindow.transform, windowCanvas);
        }

        // 3. 禁用其他UI功能
        DisableOtherUI();

        // 4. 绑定关闭事件
        BindCloseEvent();
    }

    // 创建作为悬浮窗子物体的遮罩
    private void CreateOverlayAsChild(Transform parent, Canvas windowCanvas)
    {
        // 创建遮罩物体
        overlay = new GameObject("Overlay");
        overlay.transform.SetParent(parent); // 设为悬浮窗的子物体

        // 设置遮罩的RectTransform（全屏大小）
        RectTransform overlayRect = overlay.AddComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.sizeDelta = Vector2.zero;
        overlayRect.anchoredPosition = Vector2.zero;
        overlayRect.localScale = Vector3.one;

        // 添加遮罩图片
        Image overlayImage = overlay.AddComponent<Image>();
        overlayImage.color = overlayColor;

        // 确保遮罩在悬浮窗内容下方（通过Canvas层级控制）
        Canvas overlayCanvas = overlay.AddComponent<Canvas>();
        overlayCanvas.overrideSorting = true; // 允许覆盖排序
        overlayCanvas.sortingOrder = windowCanvas.sortingOrder - 1; // 比悬浮窗低1级

        // 让遮罩可点击（用于关闭）
        Button overlayButton = overlay.AddComponent<Button>();
        overlayButton.transition = Selectable.Transition.None; // 去除点击效果
    }

    // 禁用其他UI
    private void DisableOtherUI()
    {
        Selectable[] allUI = FindObjectsOfType<Selectable>(true);
        foreach (Selectable ui in allUI)
        {
            // 排除当前按钮和悬浮窗内的UI
            bool isCurrentButton = ui.gameObject == gameObject;
            bool isInWindow = currentWindow != null && ui.transform.IsChildOf(currentWindow.transform);

            if (!isCurrentButton && !isInWindow && ui.interactable)
            {
                ui.interactable = false;
                disabledUIElements.Add(ui);
            }
        }
    }

    // 绑定关闭事件
    private void BindCloseEvent()
    {
        // 查找悬浮窗内的关闭按钮
        Button closeButton = currentWindow.GetComponentInChildren<Button>(true);
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseWindow);
        }
        // 同时绑定遮罩的关闭事件
        if (overlay != null)
        {
            Button overlayButton = overlay.GetComponent<Button>();
            if (overlayButton != null)
            {
                overlayButton.onClick.AddListener(CloseWindow);
            }
        }
    }

    // 关闭悬浮窗并恢复UI
    private void CloseWindow()
    {
        // 恢复其他UI
        foreach (Selectable ui in disabledUIElements)
        {
            if (ui != null) ui.interactable = true;
        }
        disabledUIElements.Clear();

        // 销毁悬浮窗（遮罩作为子物体会一起被销毁）
        if (currentWindow != null)
        {
            Destroy(currentWindow);
            currentWindow = null;
            overlay = null; // 遮罩已随父物体销毁
        }
    }

    private void OnDestroy()
    {
        if (spawnButton != null)
        {
            spawnButton.onClick.RemoveListener(SpawnWindow);
        }
    }
}