using UnityEngine;
using UnityEngine.UI;

public class NetLauncher : MonoBehaviour
{
    [Header("使用控制")]
    [Tooltip("是否可以使用该系统")]
    public bool canUse = true;  // 新增使用控制变量
    
    
    [Tooltip("捕网的预制体")]
    public GameObject netPrefab;

    [Header("数量管理")]
    [Tooltip("当前持有的捕网数量")]
    public int currentNetCount = 5;
    [Tooltip("最多可存储的捕网数量")]
    public int maxNetCount = 10;

    [Header("发射参数")]
    [Tooltip("发射冷却时间")]
    public float cooldown = 2f;
    [Tooltip("飞行速度")]
    public float netSpeed = 10f;
    [Tooltip("瞄准状态的时间")]
    public float aimCancelTime = 3f;

    [Header("发射起点偏移")]
    [Tooltip("捕网从角色中心沿方向偏移的距离")]
    public float netOffsetDistance = 0.5f;  // 新增偏移量

    [Header("瞄准反馈")]
    public float aimTargetRadius = 10f;
    [Tooltip("瞄准线和指示器的基础颜色")]
    public Color aimColor = new Color(1, 1, 0, 0.9f); // 黄色透明

    [Header("过热参数")]
    public float launchHeat = 20f; // 发射一次捕网产生的热量

    [Header("UI显示")]
    public Slider temperatureSlider;



    private KeyCode fireKey = KeyCode.Alpha3;
    private HookSystem hookSystem;           // 能量管理系统引用
    [HideInInspector] public float lastFireTime = float.MinValue; // 上次发射的时间戳
    private bool isAiming = false;           // 是否处于瞄准状态
    private float aimingTimer = 0f;          // 瞄准状态的计时（用于超时判断）
    private Vector2 mouseWorldPos;           // 鼠标在世界空间中的位置

    private static Texture2D _lineTex;

    void Awake()
    {
        hookSystem = GetComponent<HookSystem>();
        currentNetCount = Mathf.Clamp(currentNetCount, 0, maxNetCount);

        if (_lineTex == null)
        {
            _lineTex = new Texture2D(1, 1);
            _lineTex.SetPixel(0, 0, Color.white);
            _lineTex.Apply();
        }
    }

    void Update()
    {
        if (!canUse)  // 新增使用控制检查
        {
            if (isAiming)
                isAiming = false;
            return;
        }

        HandleAimAndFire();

        if (isAiming)
        {
            UpdateMouseWorldPosition();
        }

        UpdateUIDisplay();
    }

    private void HandleAimAndFire()    // 新增瞄准和发射接口
    {
        if (!canUse)  // 新增使用控制检查
            return;

        if (Input.GetKeyDown(fireKey) && !isAiming && CanStartAim())
        {
            isAiming = true;
            aimingTimer = 0f;
        }

        if (isAiming)
        {
            aimingTimer += Time.deltaTime;

            if (aimingTimer >= aimCancelTime)
            {
                isAiming = false;
            }

            if (Input.GetKeyUp(fireKey))
            {
                if (CanFire())
                {
                    FireNet();
                }
                isAiming = false;
            }
        }
    }

    private void UpdateMouseWorldPosition()   
    {
        Vector3 mouseWorldPos3D = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos = new Vector2(mouseWorldPos3D.x, mouseWorldPos3D.y);
    }

    private bool CanStartAim()
    {
        return canUse &&  // 新增使用控制检查
               (Time.time - lastFireTime >= cooldown) && 
               (currentNetCount > 0) && 
               hookSystem.currentTemperature < hookSystem.overheatThreshold;
    }

    private bool CanFire()
    {
        return canUse &&  // 新增使用控制检查
               (Time.time - lastFireTime >= cooldown) &&
               (currentNetCount > 0) && 
               hookSystem.currentTemperature < hookSystem.overheatThreshold;
    }

    public void FireNet()  
    {
        if (!canUse)  // 新增使用控制检查
            return;

        if (netPrefab == null)
        {
            return;
        }

        Vector2 fireDirection = (mouseWorldPos - (Vector2)transform.position).normalized;
        Vector2 origin = (Vector2)transform.position + fireDirection * netOffsetDistance; // 新增发射起点偏移

        GameObject net = Instantiate(
            netPrefab,
            origin,
            Quaternion.LookRotation(Vector3.forward, fireDirection)
        );

        Net netComponent = net.GetComponent<Net>();
        if (netComponent != null)
        {
            netComponent.Initialize(netSpeed, fireDirection);
        }

        currentNetCount--;
        lastFireTime = Time.time;

        // 调用 HookSystem 的方法增加温度
        hookSystem.AddHeat(launchHeat);
    }

    void OnGUI()
    {
        if (!isAiming || Camera.main == null || !canUse)  // 新增使用控制检查
            return;

        Vector2 fireDir = (mouseWorldPos - (Vector2)transform.position).normalized;
        Vector3 worldOrigin = (Vector2)transform.position + fireDir * netOffsetDistance;
        Vector3 screenStart = Camera.main.WorldToScreenPoint(worldOrigin);
        Vector3 screenEnd = Camera.main.WorldToScreenPoint(mouseWorldPos);

        screenStart.y = Screen.height - screenStart.y;
        screenEnd.y = Screen.height - screenEnd.y;

        float t = aimingTimer / aimCancelTime;
        Color currentColor = Color.Lerp(aimColor, Color.red, t);

        DrawLine(screenStart, screenEnd, currentColor, 2f);
        DrawCircle(screenEnd, aimTargetRadius, currentColor);
    }

    private void DrawLine(Vector2 pointA, Vector2 pointB, Color color, float width)  // 新增绘制线段接口
    {
        Matrix4x4 matrix = GUI.matrix;
        Color savedColor = GUI.color;

        Vector2 delta = pointB - pointA;
        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        float length = delta.magnitude;

        GUI.color = color;
        GUIUtility.RotateAroundPivot(angle, pointA);
        GUI.DrawTexture(new Rect(pointA.x, pointA.y - width / 2, length, width), _lineTex);

        GUI.matrix = matrix;
        GUI.color = savedColor;
    }

    private void DrawCircle(Vector2 center, float radius, Color color) 
    {
        const int segments = 24;
        float angleStep = 360f / segments;
        Vector2 prevPoint = center + new Vector2(Mathf.Cos(0), Mathf.Sin(0)) * radius;

        for (int i = 1; i <= segments; i++)
        {
            float rad = Mathf.Deg2Rad * angleStep * i;
            Vector2 newPoint = center + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * radius;
            DrawLine(prevPoint, newPoint, color, 2f);
            prevPoint = newPoint;
        }
    }

    private void UpdateUIDisplay()
    {
        if (temperatureSlider != null)
        {
            temperatureSlider.value = hookSystem.currentTemperature;
        }
    }

    public void AddcurrentNetCount(int count)
    {
        currentNetCount = Mathf.Clamp(currentNetCount + count, 0, maxNetCount);
    }
}