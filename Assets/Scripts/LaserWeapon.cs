using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class LaserWeapon : MonoBehaviour
{
    [Tooltip("激光射线渲染组件（LineRenderer）")]
    public LineRenderer laserLine;

    [Header("发射相关")]
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
    public float fireHeat = 30f; // 发射一次激光产生的热量

    private List<string> destroyableTags = new List<string> { "Obstacle" };

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
        HandleAimAndFire();

        if (isAiming)
        {
            UpdateMouseWorldPosition();
        }
    }

    private void HandleAimAndFire()
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
        return (Time.time - lastFireTime >= cooldown) && hookSystem.currentTemperature < hookSystem.overheatThreshold;
    }

    private bool CanFire()
    {
        return (Time.time - lastFireTime >= cooldown) && hookSystem.currentTemperature < hookSystem.overheatThreshold;
    }

    public void FireLaser()
    {
        if (laserLine == null)
        {
            return;
        }

        Vector2 fireDirection = (mouseWorldPos - (Vector2)transform.position).normalized;
        StartCoroutine(KeepFiringLaser(fireDirection));

        lastFireTime = Time.time;

        // 调用 HookSystem 的方法增加温度
        hookSystem.AddHeat(fireHeat);
    }

    private IEnumerator<WaitForEndOfFrame> KeepFiringLaser(Vector2 direction)
    {
        float fireTimer = 0f;
        float maxLength = 100f;
        float currentLength = 0f;

        laserLine.enabled = true;

        while (fireTimer < fireDuration)
        {
            if (currentLength < maxLength)
            {
                currentLength += laserGrowSpeed * Time.deltaTime;
                currentLength = Mathf.Min(currentLength, maxLength);
            }

            UpdateLaserLineProgressive(direction, currentLength);
            ApplyLaserEffectWithinLength(direction, currentLength);

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
        if (!isAiming || Camera.main == null) return;

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
    }
}