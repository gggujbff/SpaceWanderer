using UnityEngine;
using System.Collections.Generic;

public class LaserWeapon : MonoBehaviour
{
    [Header("使用控制")]
    [Tooltip("是否允许使用激光武器")]
    public bool canUse = true;
    
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

    [Header("伤害参数")]
    [Tooltip("激光每秒造成的伤害")]
    public float damagePerSecond = 20f;  // 新增：激光每秒伤害值


    private List<string> damageableTags = new List<string> { "Obstacle", "Collectible" };  // 标签列表重命名

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
        fireCount = Mathf.Min(fireCount, maxCount);
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
        return canUse && 
               (Time.time - lastFireTime >= cooldown) && 
               hookSystem.currentTemperature < hookSystem.overheatThreshold &&
               fireCount > 0;
    }

    private bool CanFire()
    {
        return canUse && 
               (Time.time - lastFireTime >= cooldown) && 
               hookSystem.currentTemperature < hookSystem.overheatThreshold &&
               fireCount > 0;
    }

    public void FireLaser()
    {
        if (!canUse || laserLine == null || fireCount <= 0)
        {
            return;
        }

        Vector2 fireDirection = (mouseWorldPos - (Vector2)transform.position).normalized;
        StartCoroutine(KeepFiringLaser(fireDirection));

        lastFireTime = Time.time;
        fireCount--;

        hookSystem.AddHeat(fireHeat);
    }

    private System.Collections.IEnumerator KeepFiringLaser(Vector2 direction)
    {
        float fireTimer = 0f;
        float maxLength = 100f;
        float currentLength = 0f;

        laserLine.enabled = true;

        while (fireTimer < fireDuration && canUse)
        {
            if (currentLength < maxLength)
            {
                currentLength += laserGrowSpeed * Time.deltaTime;
                currentLength = Mathf.Min(currentLength, maxLength);
            }

            UpdateLaserLineProgressive(direction, currentLength);
            ApplyLaserDamageWithinLength(direction, currentLength, Time.deltaTime);  // 传入deltaTime用于伤害计算

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
        if (hit.collider != null && damageableTags.Contains(hit.collider.tag))
        {
            endPos = hit.point;
        }

        laserLine.SetPosition(0, startPos);
        laserLine.SetPosition(1, endPos);
    }

    // 从直接销毁改为造成持续伤害
    private void ApplyLaserDamageWithinLength(Vector2 direction, float length, float deltaTime)
    {
        Vector2 startPos = (Vector2)transform.position + direction * laserOffsetDistance;
        RaycastHit2D[] hits = Physics2D.RaycastAll(startPos, direction, length);

        // 计算本次帧更新的伤害量（每秒伤害 × 帧时间）
        float damageThisFrame = damagePerSecond * deltaTime;

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider != null && damageableTags.Contains(hit.collider.tag))
            {
                // 尝试获取CollectibleObject组件并造成伤害
                CollectibleObject target = hit.collider.GetComponent<CollectibleObject>();
                if (target != null)
                {
                    target.TakeDamage(damageThisFrame, hit.point);
                }
            }
        }
    }

    void OnGUI()
    {
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
        
        maxCount = Mathf.Max(1, maxCount);
        fireCount = Mathf.Clamp(fireCount, 0, maxCount);
    }
    
    public void AddcurrentLaserCount(int amount)
    {
        if (canUse)
        {
            fireCount = Mathf.Min(fireCount + amount, maxCount);
        }
    }
}