using System.Collections.Generic;
using UnityEngine;

public class ShieldController : MonoBehaviour
{
    [Header("冷却配置")]
    public float cooldownDuration = 3f;

    [Header("护盾预制体")]
    public GameObject shieldPrefab;

    [Header("过热参数")]
    public float activateHeat = 20f;
    public float heatPerSecond = 5f;

    [Header("护盾视觉效果参数")]
    public float shieldFadeSpeed = 2f;
    public float maxRadius = 1f;
    public float radiusGrowSpeed = 3f;
    public float closeAnimationDuration = 0.5f;

    [Header("护盾角度控制")]
    [Range(0f, 360f)]
    public float shieldAngle = 180f;
    public float angleOffset = 0f;

    private float minRotationAngle = -180f;
    private float maxRotationAngle = 180f;

    [Header("碰撞体细分参数")]
    [Range(3, 36)]
    public int collisionSegments = 12;

    private float collisionSizeMultiplier = 0.5f;

    [Header("护盾属性")]
    [Tooltip("护盾最大生命值")]
    public float maxShieldHealth = 100f;
    [Tooltip("护盾当前生命值")]
    [SerializeField] private float currentShieldHealth;
    [Tooltip("护盾伤害系数（影响最终受到的伤害值）")]
    public float shieldDamageCoefficient = 0.8f;
    [Tooltip("需要与护盾发生交互的物体标签")]
    public List<string> targetTags = new List<string> { "Obstacle", "Collectible" };
    [Tooltip("护盾被击中时的特效预制体")]
    public GameObject hitEffectPrefab;

    [Header("鼠标跟随设置")]
    [Tooltip("是否允许鼠标控制护盾方向")]
    public bool allowMouseControl = true;
    [Tooltip("护盾跟随鼠标的旋转速度（度/秒）")]
    public float rotationSpeed = 180f;
    [Tooltip("鼠标检测的Z轴偏移（用于2D世界坐标转换）")]
    public float mouseZOffset = 10f;

    private enum ShieldState { Closed, Active, Closing, Cooldown }
    private ShieldState state = ShieldState.Closed;
    private float cooldownTimer = 0f;
    private float closeAnimationTimer = 0f;
    private GameObject currentShield;
    private Transform shieldSpawnPoint;
    private KeyCode toggleKey = KeyCode.S;
    private HookSystem hookSystem;

    // Shader属性控制
    private Material shieldMaterial;
    private int radiusPropertyId;
    private int intensityPropertyId;
    private int anglePropertyId;
    private int offsetPropertyId;
    private float currentRadius;
    private float targetRadius;

    private PolygonCollider2D shieldCollider;

    private void Start()
    {
        shieldSpawnPoint = transform;
        hookSystem = HookSystem.Instance;
        if (hookSystem != null)
        {
            // 订阅钩爪系统的过热冷却事件（用于自动关闭护盾）
            hookSystem.OnOverheatEnterCooling += ForceCloseShieldOnOverheat;
        }
        radiusPropertyId = Shader.PropertyToID("_Radius");
        intensityPropertyId = Shader.PropertyToID("_ShieldIntensity");
        anglePropertyId = Shader.PropertyToID("_ShieldAngle");
        offsetPropertyId = Shader.PropertyToID("_AngleOffset");
        
        currentShieldHealth = maxShieldHealth; // 初始化护盾生命值
    }

    private void Update()
    {
        switch (state)
        {
            case ShieldState.Closed:
                if (Input.GetKeyDown(toggleKey) && CanActivateShield())
                {
                    ActivateShield();
                }
                break;

            case ShieldState.Active:
                UpdateShieldAnimation();
                if (Input.GetKeyDown(toggleKey))
                {
                    StartCloseAnimation();
                }
                else
                {
                    if (hookSystem != null)
                    {
                        hookSystem.currentTemperature += heatPerSecond * Time.deltaTime;
                    }
                }
                break;

            case ShieldState.Closing:
                closeAnimationTimer += Time.deltaTime;
                float progress = closeAnimationTimer / closeAnimationDuration;

                currentRadius = Mathf.Lerp(maxRadius, 0, progress);
                UpdateShieldVisualAndCollision();

                float intensity = Mathf.Lerp(1f, 0f, progress);
                shieldMaterial.SetFloat(intensityPropertyId, intensity);

                if (progress >= 1f)
                {
                    Destroy(currentShield);
                    currentShield = null;
                    Destroy(shieldMaterial);
                    shieldMaterial = null;

                    cooldownTimer = cooldownDuration;
                    state = ShieldState.Cooldown;
                }
                break;

            case ShieldState.Cooldown:
                cooldownTimer -= Time.deltaTime;
                if (cooldownTimer <= 0f)
                {
                    state = ShieldState.Closed;
                }
                break;
        }
    }

    // 修复2：当钩爪系统过热进入冷却时，强制关闭护盾
    private void ForceCloseShieldOnOverheat()
    {
        if (state == ShieldState.Active)
        {
            Debug.Log("系统过热，自动关闭护盾");
            StartCloseAnimation();
        }
    }

    private void StartCloseAnimation()
    {
        if (currentShield == null) return;

        state = ShieldState.Closing;
        closeAnimationTimer = 0f;
    }

    private void UpdateShieldAnimation()
    {
        if (shieldMaterial == null) return;

        currentRadius = Mathf.Lerp(currentRadius, targetRadius, radiusGrowSpeed * Time.deltaTime);
        UpdateShieldVisualAndCollision();

        // 鼠标跟随逻辑（仅在激活状态且允许鼠标控制时生效）
        if (allowMouseControl && currentShield != null)
        {
            UpdateShieldDirectionToMouse();
        }

        if (hookSystem != null)
        {
            float intensity = Mathf.Lerp(0.5f, 1f, hookSystem.currentTemperature / hookSystem.overheatThreshold);
            shieldMaterial.SetFloat(intensityPropertyId, intensity);
        }
        
        // 检查护盾是否已耗尽
        if (currentShieldHealth <= 0 && state == ShieldState.Active)
        {
            StartCloseAnimation();
        }
    }

    /// <summary>
    /// 更新护盾方向以跟随鼠标指针
    /// </summary>
    private void UpdateShieldDirectionToMouse()
    {
        // 将鼠标屏幕坐标转换为世界坐标
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = mouseZOffset; // 确保与护盾在同一Z平面
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        mouseWorldPos.z = currentShield.transform.position.z; // 忽略Z轴差异（2D场景）

        // 计算护盾到鼠标的方向向量
        Vector2 directionToMouse = (mouseWorldPos - currentShield.transform.position).normalized;

        // 计算目标角度（弧度转角度）
        float targetAngle = Mathf.Atan2(directionToMouse.y, directionToMouse.x) * Mathf.Rad2Deg;
        
        // 限制旋转范围（如果需要）
        targetAngle = Mathf.Clamp(targetAngle, minRotationAngle, maxRotationAngle);

        // 平滑旋转到目标角度
        Quaternion targetRotation = Quaternion.Euler(0, 0, targetAngle);
        currentShield.transform.rotation = Quaternion.RotateTowards(
            currentShield.transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    private void UpdateShieldVisualAndCollision()
    {
        shieldMaterial.SetFloat(radiusPropertyId, currentRadius);
        shieldMaterial.SetFloat(anglePropertyId, shieldAngle);
        shieldMaterial.SetFloat(offsetPropertyId, angleOffset);

        UpdateShieldCollider();
    }

    private void UpdateShieldCollider()
    {
        if (shieldCollider == null) return;

        List<Vector2> points = new List<Vector2>();
        points.Add(Vector2.zero);

        float halfAngle = shieldAngle * 0.5f;
        int segments = Mathf.Max(3, Mathf.CeilToInt(collisionSegments * shieldAngle / 360f));

        for (int i = 0; i <= segments; i++)
        {
            float angle = angleOffset - halfAngle + (float)i / segments * shieldAngle;
            float radians = angle * Mathf.Deg2Rad;

            // 使用碰撞体大小乘数调整碰撞点位置
            float x = Mathf.Cos(radians) * currentRadius * collisionSizeMultiplier;
            float y = Mathf.Sin(radians) * currentRadius * collisionSizeMultiplier;

            points.Add(new Vector2(x, y));
        }

        shieldCollider.SetPath(0, points.ToArray());
    }

    private bool CanActivateShield()
    {
        return hookSystem != null && 
               hookSystem.currentOverheatState == HookSystem.OverheatState.Normal && 
               cooldownTimer <= 0f && 
               currentShieldHealth > 0;
    }

    private void ActivateShield()
    {
        currentShield = Instantiate(shieldPrefab, shieldSpawnPoint.position, shieldSpawnPoint.rotation, shieldSpawnPoint);

        Renderer shieldRenderer = currentShield.GetComponent<Renderer>();
        if (shieldRenderer != null)
        {
            shieldMaterial = new Material(shieldRenderer.material);
            shieldRenderer.material = shieldMaterial;
            currentRadius = 0f;
            targetRadius = maxRadius;
            shieldMaterial.SetFloat(radiusPropertyId, currentRadius);
            shieldMaterial.SetFloat(anglePropertyId, shieldAngle);
            shieldMaterial.SetFloat(offsetPropertyId, angleOffset);
        }

        // 碰撞体设置
        shieldCollider = currentShield.GetComponent<PolygonCollider2D>();
        if (shieldCollider == null)
        {
            shieldCollider = currentShield.AddComponent<PolygonCollider2D>();
        }
        shieldCollider.isTrigger = false;
        UpdateShieldCollider();

        // 添加 Rigidbody2D
        Rigidbody2D rb = currentShield.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = currentShield.AddComponent<Rigidbody2D>();
        }
        rb.bodyType = RigidbodyType2D.Static;
        rb.simulated = true;

        // 设置弹性材质
        PhysicsMaterial2D bounceMat = new PhysicsMaterial2D("ShieldBounce");
        bounceMat.bounciness = 0.8f;
        bounceMat.friction = 0.2f;
        shieldCollider.sharedMaterial = bounceMat;

        // 配置碰撞处理器
        ShieldCollisionHandler collisionHandler = currentShield.GetComponent<ShieldCollisionHandler>();
        if (collisionHandler == null)
        {
            collisionHandler = currentShield.AddComponent<ShieldCollisionHandler>();
        }
        
        // 设置碰撞处理器属性
        collisionHandler.targetTags = new List<string>(targetTags);
        collisionHandler.maxShieldHealth = maxShieldHealth;
        collisionHandler.currentShieldHealth = currentShieldHealth;
        collisionHandler.shieldDamageCoefficient = shieldDamageCoefficient;
        collisionHandler.hitEffectPrefab = hitEffectPrefab;
        
        // 添加事件监听，当护盾生命值变化时更新控制器状态
        collisionHandler.onShieldHealthChanged += OnShieldHealthChanged;
        collisionHandler.onShieldDestroyed += OnShieldDestroyed;

        state = ShieldState.Active;

        if (hookSystem != null)
        {
            hookSystem.currentTemperature += activateHeat;
        }
    }
    
    // 护盾生命值变化回调
    private void OnShieldHealthChanged(float newHealth)
    {
        currentShieldHealth = newHealth;
    }
    
    // 护盾被摧毁回调
    private void OnShieldDestroyed()
    {
        if (state == ShieldState.Active)
        {
            StartCloseAnimation();
        }
    }


    // 清理事件订阅，避免内存泄漏
    private void OnDestroy()
    {
        if (hookSystem != null)
        {
            hookSystem.OnOverheatEnterCooling -= ForceCloseShieldOnOverheat;
        }
    }
    
    // 外部调用：恢复护盾生命值
    public void RestoreShieldHealth(float amount)
    {
        currentShieldHealth = Mathf.Min(maxShieldHealth, currentShieldHealth + amount);
        
        // 如果护盾已关闭且生命值恢复，重置冷却
        if (state == ShieldState.Cooldown && currentShieldHealth > 0)
        {
            cooldownTimer = 0f;
            state = ShieldState.Closed;
        }
    }
}