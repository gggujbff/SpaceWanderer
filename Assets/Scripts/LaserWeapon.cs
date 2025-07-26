using UnityEngine;
using System.Collections.Generic;

public class LaserWeapon : MonoBehaviour
{
    [Header("使用控制")]
    [Tooltip("是否允许使用激光武器")]
    public bool canUse = true;  // 新增使用控制变量
    
    [Tooltip("激光射线渲染组件（LineRenderer）")]
    public LineRenderer laserLine;

    [Header("发射相关")]
    [Tooltip("持有数量")]
    public int fireCount = 3;

    [Tooltip("最大持有量")]
    public int maxCount = 3;

    [Tooltip("冷却时间")]
    public float cooldown = 2f;

    [Tooltip("激光持续时间")]
    public float fireDuration = 2f;

    [Tooltip("瞄准状态自动取消时间")]
    public float aimCancelTime = 3f;

    [Tooltip("中心偏移距离")]
    public float laserOffsetDistance = 0.3f;

    [Header("激光外观")]
    [Tooltip("激光的宽度")]
    public float laserWidth = 0.1f;

    [Tooltip("激光的颜色")]
    public Color laserColor = new Color(1, 0, 0, 0.9f);

    [Tooltip("激光伸长的速度")]
    public float laserGrowSpeed = 200f;

    [Tooltip("激光材质")]
    public Material laserMaterial;

    [Header("过热参数")]
    [Tooltip("激光启动时立即增加的热量")]
    public float fireHeat = 30f;

    [Tooltip("激光每秒释放的热量（持续时间内持续加热）")]
    public float continuousFireHeatRate = 5f;


    private List<string> destroyableTags = new List<string> { "Obstacle", "Collectible" };

    private Color aimColor = new Color(1, 0, 0, 0.9f);
    private float aimTargetRadius = 10f;

    private KeyCode fireKey = KeyCode.Alpha2;
    private HookSystem hookSystem;
    [HideInInspector] public float lastFireTime = float.MinValue;
    private bool isAiming = false;
    private float aimingTimer = 0f;
    private Vector2 mouseWorldPos;

    private static Texture2D _lineTex;

    void Awake()
    {
        hookSystem = GetComponent<HookSystem>();

        if (_lineTex == null)
        {
            _lineTex = new Texture2D(1, 1);
            _lineTex.SetPixel(0, 0, Color.white);
            _lineTex.Apply();
        }

        if (laserLine == null)
        {
            laserLine = gameObject.AddComponent<LineRenderer>();
        }

        UpdateLaserAppearance();
        fireCount = Mathf.Min(fireCount, maxCount); // 确保初始值不超过上限
    }

    private void UpdateLaserAppearance()
    {
        if (laserLine == null) return;

        laserLine.startWidth = laserWidth;
        laserLine.endWidth = laserWidth;

        if (laserMaterial != null)
        {
            laserLine.material = new Material(laserMaterial);
            laserLine.textureMode = LineTextureMode.Tile;
        }
        else
        {
            Debug.LogWarning("未设置 laserMaterial 材质");
            laserLine.material = new Material(Shader.Find("Sprites/Default"));
        }

        laserLine.startColor = laserColor;
        laserLine.endColor = laserColor;

        laserLine.sortingLayerName = "Default";
        laserLine.sortingOrder = 100;

        laserLine.enabled = false;
    }

    void Update()
    {
        // 如果不可用，重置状态并退出
        if (!canUse)
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
    }

    private void HandleAimAndFire()
    {
        // 不可用时直接返回
        if (!canUse)
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
                    FireLaser();
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
               hookSystem.currentTemperature < hookSystem.overheatThreshold &&
               fireCount > 0;
    }

    private bool CanFire()
    {
        return canUse &&  // 新增使用控制检查
               (Time.time - lastFireTime >= cooldown) && 
               hookSystem.currentTemperature < hookSystem.overheatThreshold &&
               fireCount > 0;
    }

    public void FireLaser()
    {
        // 不可用或次数不足时直接返回
        if (!canUse || laserLine == null || fireCount <= 0)
        {
            return;
        }

        Vector2 fireDirection = (mouseWorldPos - (Vector2)transform.position).normalized;
        StartCoroutine(KeepFiringLaser(fireDirection));

        lastFireTime = Time.time;
        fireCount--; // 使用次数减一

        // 初始热量
        hookSystem.AddHeat(fireHeat);
    }

    private System.Collections.IEnumerator KeepFiringLaser(Vector2 direction)
    {
        float fireTimer = 0f;
        float maxLength = 100f;
        float currentLength = 0f;

        laserLine.enabled = true;

        while (fireTimer < fireDuration && canUse)  // 循环中检查可用性
        {
            if (currentLength < maxLength)
            {
                currentLength += laserGrowSpeed * Time.deltaTime;
                currentLength = Mathf.Min(currentLength, maxLength);
            }

            UpdateLaserLineProgressive(direction, currentLength);
            ApplyLaserEffectWithinLength(direction, currentLength);

            // 持续热量增加
            hookSystem.AddHeat(continuousFireHeatRate * Time.deltaTime);

            fireTimer += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }

        laserLine.enabled = false;
    }

    private void UpdateLaserLineProgressive(Vector2 direction, float length)
    {
        Vector2 startPos = (Vector2)transform.position + direction * laserOffsetDistance;
        Vector2 endPos = startPos + direction * length;

        RaycastHit2D hit = Physics2D.Raycast(startPos, direction, length);
        if (hit.collider != null && destroyableTags.Contains(hit.collider.tag))
        {
            endPos = hit.point;
        }

        laserLine.SetPosition(0, startPos);
        laserLine.SetPosition(1, endPos);
    }

    private void ApplyLaserEffectWithinLength(Vector2 direction, float length)
    {
        Vector2 startPos = (Vector2)transform.position + direction * laserOffsetDistance;
        RaycastHit2D[] hits = Physics2D.RaycastAll(startPos, direction, length);

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider != null && destroyableTags.Contains(hit.collider.tag))
            {
                Destroy(hit.collider.gameObject);
            }
        }
    }

    void OnGUI()
    {
        // 不可用时不绘制瞄准UI
        if (!canUse || !isAiming || Camera.main == null) return;

        Vector2 fireDir = (mouseWorldPos - (Vector2)transform.position).normalized;
        Vector3 worldOrigin = (Vector2)transform.position + fireDir * laserOffsetDistance;
        Vector3 screenStart = Camera.main.WorldToScreenPoint(worldOrigin);
        Vector3 screenEnd = Camera.main.WorldToScreenPoint(mouseWorldPos);

        screenStart.y = Screen.height - screenStart.y;
        screenEnd.y = Screen.height - screenEnd.y;

        float t = aimingTimer / aimCancelTime;
        Color currentColor = Color.Lerp(aimColor, Color.red, t);

        DrawLine(screenStart, screenEnd, currentColor, 2f);
        DrawCircle(screenEnd, aimTargetRadius, currentColor);

        // 显示剩余使用次数
        GUI.Label(new Rect(10, 10, 250, 30), $"Laser Ammo: {fireCount}/{maxCount}", new GUIStyle
        {
            fontSize = 16,
            normal = new GUIStyleState { textColor = Color.red }
        });
    }

    private void DrawLine(Vector2 pointA, Vector2 pointB, Color color, float width)
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

    private void OnValidate()
    {
        if (laserLine != null)
        {
            UpdateLaserAppearance();
        }
        
        // 确保编辑器中设置的值合理
        maxCount = Mathf.Max(1, maxCount);
        fireCount = Mathf.Clamp(fireCount, 0, maxCount);
    }
    
    // 增加激光使用次数
    public void AddcurrentLaserCount(int amount)
    {
        if (canUse)  // 仅在可用时允许增加数量
        {
            fireCount = Mathf.Min(fireCount + amount, maxCount);
        }
    }
}