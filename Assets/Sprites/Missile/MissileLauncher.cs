using UnityEngine;

public class MissileLauncher : MonoBehaviour
{
    [Tooltip("导弹的预制体")]
    public GameObject missilePrefab;

    [Header("数量管理")]
    [Tooltip("当前持有的导弹数量")]
    public int currentMissileCount = 5;
    [Tooltip("最多可存储的导弹数量")]
    public int maxMissileCount = 10;

    [Header("发射参数")]
    [Tooltip("发射冷却时间")]
    public float cooldown = 2f;
    [Tooltip("能量消耗值")]
    public int energyCost = 5;
    [Tooltip("飞行速度")]
    public float missileSpeed = 10f;
    [Tooltip("瞄准状态的时间")]
    public float aimCancelTime = 3f;

    [Header("瞄准反馈")]
    public float aimTargetRadius = 10f;
    [Tooltip("瞄准线和指示器的基础颜色")]
    public Color aimColor = new Color(1, 1, 0, 0.9f); // 黄色透明

    private KeyCode fireKey = KeyCode.Alpha1;
    private HookSystem hookSystem;           // 能量管理系统引用
    private float lastFireTime = float.MinValue; // 上次发射的时间戳
    private bool isAiming = false;           // 是否处于瞄准状态
    private float aimingTimer = 0f;          // 瞄准状态的计时（用于超时判断）
    private Vector2 mouseWorldPos;           // 鼠标在世界空间中的位置

    private static Texture2D _lineTex;

    void Awake()
    {
        hookSystem = GetComponent<HookSystem>();
        currentMissileCount = Mathf.Clamp(currentMissileCount, 0, maxMissileCount);

        if (_lineTex == null)
        {
            _lineTex = new Texture2D(1, 1);
            _lineTex.SetPixel(0, 0, Color.white);
            _lineTex.Apply();
        }
    }

    void Update()
    {
        HandleAimAndFire();

        if (isAiming)
        {
            UpdateMouseWorldPosition();
        }
    }

    private void HandleAimAndFire()    //处理瞄准和发射的核心逻辑
    {
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
                    FireMissile();
                }
                isAiming = false;
            }
        }
    }

    private void UpdateMouseWorldPosition()    //更新鼠标位置
    {
        Vector3 mouseWorldPos3D = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos = new Vector2(mouseWorldPos3D.x, mouseWorldPos3D.y);
    }

    private bool CanStartAim()    //可以进入瞄准状态
    {
        return (Time.time - lastFireTime >= cooldown) && (currentMissileCount > 0);
    }

    private bool CanFire()    //判断是否可以发射飞弹
    {
        return (Time.time - lastFireTime >= cooldown) &&
               (hookSystem != null && hookSystem.currentEnergy >= energyCost) &&
               (currentMissileCount > 0);
    }

    public void FireMissile()    //发射飞弹的具体逻辑
    {
        if (missilePrefab == null)
        {
            Debug.LogError("未指定飞弹预制体！请在Inspector中赋值missilePrefab");
            return;
        }

        Vector2 fireDirection = (mouseWorldPos - (Vector2)transform.position).normalized;

        GameObject missile = Instantiate(
            missilePrefab, 
            transform.position, 
            Quaternion.LookRotation(Vector3.forward, fireDirection)
        );

        Missile missileComponent = missile.GetComponent<Missile>();
        if (missileComponent != null)
        {
            missileComponent.Initialize(missileSpeed, fireDirection);
        }
        else
        {
            Debug.LogWarning("飞弹预制体缺少Missile组件！飞弹可能无法正常飞行");
        }

        currentMissileCount--;
        if (hookSystem != null)
        {
            hookSystem.currentEnergy -= energyCost;
        }

        lastFireTime = Time.time;
    }

    void OnGUI()    //绘制瞄准反馈
    {
        if (!isAiming || Camera.main == null) return;

        Vector3 screenStart = Camera.main.WorldToScreenPoint(transform.position);
        Vector3 screenEnd = Camera.main.WorldToScreenPoint(mouseWorldPos);

        screenStart.y = Screen.height - screenStart.y;
        screenEnd.y = Screen.height - screenEnd.y;

        float t = aimingTimer / aimCancelTime;
        Color currentColor = Color.Lerp(aimColor, Color.red, t);

        DrawLine(screenStart, screenEnd, currentColor, 2f);
        DrawCircle(screenEnd, aimTargetRadius, currentColor);
    }

    private void DrawLine(Vector2 pointA, Vector2 pointB, Color color, float width)    //画瞄准线
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
}