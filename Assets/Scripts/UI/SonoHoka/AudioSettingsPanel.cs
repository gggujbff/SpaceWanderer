using UnityEngine;
using UnityEngine.UI;

public class AudioSettingsPanel : MonoBehaviour
{
    [Header("Panel Settings")]
    [SerializeField] private Vector2 panelSize = new Vector2(600, 800);
    [SerializeField] private Color panelColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
    [SerializeField] private string titleText = "音量设置";
    [SerializeField] private float padding = 30f; // 内边距
    [SerializeField] private float sectionSpacing = 60f; // 大幅增加模块间距
    [SerializeField] private float elementSpacing = 20f; // 元素间距
    [SerializeField] private float toggleSize = 25f;
    
    private GameObject canvasObject;
    private GameObject panel;

    private void Start()
    {
        CreateIndependentCanvas();
        CreateUIElements();
    }

    private void CreateIndependentCanvas()
    {
        canvasObject = new GameObject("AudioSettingsCanvas");
        canvasObject.transform.SetParent(transform, false);
        
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasObject.AddComponent<GraphicRaycaster>();
    }

    private void CreateUIElements()
    {
        if (canvasObject == null)
        {
            Debug.LogError("Canvas未创建，无法生成UI元素！");
            return;
        }

        // 创建主Panel
        panel = new GameObject("AudioSettingsPanel");
        panel.transform.SetParent(canvasObject.transform);
        
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.sizeDelta = panelSize;
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = panelColor;
        
        // 添加垂直布局组件
        VerticalLayoutGroup layoutGroup = panel.AddComponent<VerticalLayoutGroup>();
        layoutGroup.padding = new RectOffset((int)padding, (int)padding, (int)padding, (int)padding);
        layoutGroup.spacing = sectionSpacing; // 使用更大的模块间距
        layoutGroup.childAlignment = TextAnchor.UpperCenter;
        layoutGroup.childControlHeight = true;
        layoutGroup.childControlWidth = true;
        
        // 添加内容大小适配组件
        ContentSizeFitter sizeFitter = panel.AddComponent<ContentSizeFitter>();
        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        
        // 创建标题
        CreateTitle(panel.transform);
        
        // 创建音量控制区域
        CreateVolumeControlSection("总音量", panel.transform);
        CreateVolumeControlSection("背景音乐", panel.transform);
        CreateVolumeControlSection("特效音乐", panel.transform);
    }

    private void CreateTitle(Transform parent)
    {
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(parent);
        
        RectTransform rect = titleObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, 70);
        
        Text titleTextComp = titleObj.AddComponent<Text>();
        titleTextComp.text = titleText;
        titleTextComp.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        titleTextComp.fontSize = 34;
        titleTextComp.alignment = TextAnchor.MiddleCenter;
        titleTextComp.color = Color.white;
        
        // 添加布局元素
        LayoutElement layoutElement = titleObj.AddComponent<LayoutElement>();
        layoutElement.minHeight = 70;
        layoutElement.ignoreLayout = false;
    }

    private void CreateVolumeControlSection(string name, Transform parent)
    {
        // 创建区域面板
        GameObject sectionObj = new GameObject(name + "Section");
        sectionObj.transform.SetParent(parent);
        
        RectTransform sectionRect = sectionObj.AddComponent<RectTransform>();
        sectionRect.sizeDelta = new Vector2(0, 130);
        
        Image sectionImage = sectionObj.AddComponent<Image>();
        sectionImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        
        // 添加垂直布局组件
        VerticalLayoutGroup layoutGroup = sectionObj.AddComponent<VerticalLayoutGroup>();
        layoutGroup.padding = new RectOffset(25, 25, 20, 20);
        layoutGroup.spacing = elementSpacing;
        layoutGroup.childAlignment = TextAnchor.UpperCenter;
        layoutGroup.childControlHeight = true;
        layoutGroup.childControlWidth = true;
        
        // 创建标题和开关的水平布局
        GameObject headerObj = new GameObject("Header");
        headerObj.transform.SetParent(sectionObj.transform);
        
        RectTransform headerRect = headerObj.AddComponent<RectTransform>();
        headerRect.sizeDelta = new Vector2(0, 35);
        
        HorizontalLayoutGroup headerLayout = headerObj.AddComponent<HorizontalLayoutGroup>();
        headerLayout.padding = new RectOffset(0, 0, 0, 0);
        headerLayout.spacing = 15;
        headerLayout.childAlignment = TextAnchor.MiddleCenter;
        headerLayout.childControlHeight = true;
        headerLayout.childControlWidth = true;
        
        // 创建标题
        GameObject labelObj = new GameObject(name + "Label");
        labelObj.transform.SetParent(headerObj.transform);
        
        RectTransform labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.sizeDelta = new Vector2(0, 35);
        
        Text labelText = labelObj.AddComponent<Text>();
        labelText.text = name;
        labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        labelText.fontSize = 24;
        labelText.alignment = TextAnchor.MiddleLeft;
        labelText.color = Color.white;
        
        // 添加布局元素控制扩展
        LayoutElement labelLayout = labelObj.AddComponent<LayoutElement>();
        labelLayout.ignoreLayout = false;
        labelLayout.flexibleWidth = 1;
        
        // 创建开关
        GameObject toggleObj = new GameObject(name + "Toggle");
        toggleObj.transform.SetParent(headerObj.transform);
        
        RectTransform toggleRect = toggleObj.AddComponent<RectTransform>();
        toggleRect.sizeDelta = new Vector2(toggleSize, toggleSize);
        
        Toggle toggle = toggleObj.AddComponent<Toggle>();
        
        GameObject toggleBackground = new GameObject("Background");
        toggleBackground.transform.SetParent(toggleObj.transform);
        
        RectTransform bgRect = toggleBackground.AddComponent<RectTransform>();
        bgRect.sizeDelta = new Vector2(toggleSize, toggleSize);
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        
        Image bgImage = toggleBackground.AddComponent<Image>();
        bgImage.color = Color.gray;
        
        GameObject toggleCheckmark = new GameObject("Checkmark");
        toggleCheckmark.transform.SetParent(toggleObj.transform);
        
        RectTransform checkRect = toggleCheckmark.AddComponent<RectTransform>();
        checkRect.sizeDelta = new Vector2(toggleSize * 0.8f, toggleSize * 0.8f);
        
        Image checkImage = toggleCheckmark.AddComponent<Image>();
        checkImage.color = Color.green;
        
        toggle.graphic = checkImage;
        toggle.targetGraphic = bgImage;
        toggle.isOn = true; // 默认开启
        
        // 创建滑块
        GameObject sliderObj = new GameObject(name + "Slider");
        sliderObj.transform.SetParent(sectionObj.transform);
        
        RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
        sliderRect.sizeDelta = new Vector2(0, 30);
        
        Slider slider = sliderObj.AddComponent<Slider>();
        slider.minValue = 0;
        slider.maxValue = 1;
        slider.value = 1;
        slider.wholeNumbers = false;
        
        // 创建滑块背景
        GameObject sliderBackground = new GameObject("Background");
        sliderBackground.transform.SetParent(sliderObj.transform);
        
        RectTransform sliderBgRect = sliderBackground.AddComponent<RectTransform>();
        sliderBgRect.sizeDelta = new Vector2(0, 22);
        sliderBgRect.anchorMin = new Vector2(0, 0.5f);
        sliderBgRect.anchorMax = new Vector2(1, 0.5f);
        sliderBgRect.pivot = new Vector2(0.5f, 0.5f);
        
        Image sliderBgImage = sliderBackground.AddComponent<Image>();
        sliderBgImage.color = Color.gray;
        
        // 创建滑块填充
        GameObject sliderFill = new GameObject("Fill");
        sliderFill.transform.SetParent(sliderObj.transform);
        
        RectTransform fillRect = sliderFill.AddComponent<RectTransform>();
        fillRect.sizeDelta = new Vector2(0, 22);
        fillRect.anchorMin = new Vector2(0, 0.5f);
        fillRect.anchorMax = new Vector2(1, 0.5f);
        fillRect.pivot = new Vector2(0, 0.5f);
        
        Image fillImage = sliderFill.AddComponent<Image>();
        fillImage.color = Color.blue;
        
        slider.fillRect = fillRect;
        
        // 创建滑块手柄
        GameObject sliderHandle = new GameObject("Handle");
        sliderHandle.transform.SetParent(sliderObj.transform);
        
        RectTransform handleRect = sliderHandle.AddComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(38, 38);
        
        Image handleImage = sliderHandle.AddComponent<Image>();
        handleImage.color = Color.white;
        
        slider.handleRect = handleRect;
        
        // 添加布局元素
        LayoutElement sectionLayout = sectionObj.AddComponent<LayoutElement>();
        sectionLayout.minHeight = 130;
        sectionLayout.ignoreLayout = false;
    }

    // 显示/隐藏面板
    public void TogglePanelVisibility()
    {
        if (panel != null)
            panel.SetActive(!panel.activeSelf);
    }
}