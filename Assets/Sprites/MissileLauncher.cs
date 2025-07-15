using UnityEngine;

public class MissileLauncher : MonoBehaviour
{
    [Header("飞弹配置")]
    public GameObject missilePrefab;

    [Header("数量管理")]
    public int currentMissileCount = 5;
    public int maxMissileCount = 10;

    [Header("发射参数")]
    public float cooldown = 2f;
    public int energyCost = 5;
    public float missileSpeed = 10f;
    public KeyCode fireKey = KeyCode.Alpha1;
    public float aimCancelTime = 3f;

    [Header("Game视图瞄准反馈")]
    public float aimTargetRadius = 10f;
    public Color aimColor = new Color(1, 1, 0, 0.9f); // 黄色透明

    private HookSystem hookSystem;
    private float lastFireTime = float.MinValue;
    private bool isAiming = false;
    private float aimingTimer = 0f;
    private Vector2 mouseWorldPos;

    // GUI用的静态纹理
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
                    FireMissile();
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
        return Time.time - lastFireTime >= cooldown && currentMissileCount > 0;
    }

    private bool CanFire()
    {
        return hookSystem != null && hookSystem.currentEnergy >= energyCost;
    }

    public void FireMissile()
    {
        if (missilePrefab == null)
        {
            Debug.LogError("未指定飞弹预制体！");
            return;
        }

        Vector2 fireDirection = (mouseWorldPos - (Vector2)transform.position).normalized;
        GameObject missile = Instantiate(missilePrefab, transform.position, Quaternion.LookRotation(Vector3.forward, fireDirection));
        Missile missileComponent = missile.GetComponent<Missile>();
        if (missileComponent != null)
        {
            missileComponent.Initialize(missileSpeed, fireDirection);
        }

        currentMissileCount--;
        if (hookSystem != null)
        {
            hookSystem.currentEnergy -= energyCost;
        }
        lastFireTime = Time.time;
    }

    void OnGUI()
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

    // ------- 内嵌绘图函数（线和圆） --------
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
        int segments = 24;
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
