using UnityEngine;
using System.Collections.Generic;

public class LaserWeapon : MonoBehaviour
{
    [Tooltip("激光射线渲染组件")]
    public LineRenderer laserLine;

    [Header("发射相关")]
    [Tooltip("发射冷却时间")]
    public float cooldown = 2f;
    [Tooltip("消耗的能量值")]
    public int energyCost = 5;
    [Tooltip("激光持续时长")]
    public float fireDuration = 2f;
    [Tooltip("瞄准持续时长")]
    public float aimCancelTime = 3f;

    [Header("激光外观")]
    [Tooltip("激光宽度（长方形宽度）")]
    public float laserWidth = 0.1f;
    [Tooltip("激光颜色")]
    public Color laserColor = new Color(1, 0, 0, 0.9f);

    private Color aimColor = new Color(1, 0, 0, 0.9f);
    private float aimTargetRadius = 10f;

    [Header("激光起点偏移")]
    [Tooltip("激光从角色中心沿方向偏移的距离")]
    public float laserOffsetDistance = 0.5f;

    private string[] blockingTags = {}; //阻挡激光终点的标签
    private List<string> destroyableTags = new List<string> { "Obstacle" , "Energy"};  //激光可摧毁的标签

    private KeyCode fireKey = KeyCode.Alpha2;
    private HookSystem hookSystem;
    private float lastFireTime = float.MinValue;
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
        laserLine.widthCurve = AnimationCurve.Constant(0f, 1f, laserWidth);

        laserLine.material = new Material(Shader.Find("Sprites/Default"));
        laserLine.startColor = laserColor;
        laserLine.endColor = laserColor;
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
        return (Time.time - lastFireTime >= cooldown);
    }

    private bool CanFire()
    {
        return (Time.time - lastFireTime >= cooldown) &&
               (hookSystem != null && hookSystem.currentEnergy >= energyCost);
    }

    public void FireLaser()
    {
        if (laserLine == null)
        {
            Debug.LogError("未正确初始化 LineRenderer！无法显示激光");
            return;
        }

        Vector2 fireDirection = (mouseWorldPos - (Vector2)transform.position).normalized;
        StartCoroutine(KeepFiringLaser(fireDirection));

        if (hookSystem != null)
        {
            hookSystem.currentEnergy -= energyCost;
            Debug.Log($"激光发射，当前剩余能量: {hookSystem.currentEnergy}");
        }

        lastFireTime = Time.time;
    }

    private System.Collections.IEnumerator KeepFiringLaser(Vector2 direction)
    {
        float fireTimer = 0f;
        laserLine.enabled = true;

        while (fireTimer < fireDuration)
        {
            UpdateLaserLine(direction);
            ApplyLaserEffect(direction);
            fireTimer += Time.deltaTime;
            yield return null;
        }

        laserLine.enabled = false;
    }

    private void UpdateLaserLine(Vector2 direction)
    {
        Vector2 origin = (Vector2)transform.position + direction * laserOffsetDistance;
        Vector2 endPos = origin + direction * 100f;

        RaycastHit2D hit = Physics2D.Raycast(origin, direction, 100f);
        if (hit.collider != null && IsBlockingTag(hit.collider.tag))
        {
            endPos = hit.point;
        }

        laserLine.SetPosition(0, origin);
        laserLine.SetPosition(1, endPos);
    }

    private void ApplyLaserEffect(Vector2 direction)
    {
        Vector2 origin = (Vector2)transform.position + direction * laserOffsetDistance;
        float laserLength = 100f;
        float width = laserWidth;

        float angle = Vector2.SignedAngle(Vector2.right, direction);

        RaycastHit2D[] hits = Physics2D.BoxCastAll(
            origin + direction * laserLength / 2f,
            new Vector2(laserLength, width),
            angle,
            Vector2.zero,
            0f
        );

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider != null && destroyableTags.Contains(hit.collider.tag))
            {
                Destroy(hit.collider.gameObject);
            }
        }
    }

    private bool IsBlockingTag(string tag)
    {
        foreach (string blockingTag in blockingTags)
        {
            if (tag == blockingTag)
            {
                return true;
            }
        }
        return false;
    }

    void OnGUI()
    {
        if (!isAiming || Camera.main == null) return;

        Vector2 fireDir = (mouseWorldPos - (Vector2)transform.position).normalized;
        Vector3 screenStart = Camera.main.WorldToScreenPoint((Vector2)transform.position + fireDir * laserOffsetDistance);
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
        UpdateLaserAppearance();
    }
}
