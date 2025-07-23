using UnityEngine;
using System.Collections.Generic;

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
    public float shieldFadeSpeed = 2f; // 淡入/淡出速度
    public float maxRadius = 1f; // 最大半径
    public float radiusGrowSpeed = 3f; // 展开速度
    public float closeAnimationDuration = 0.5f; // 关闭动画时长（关键：控制销毁过程的时间）

    private List<string> destructibleTags = new List<string> { "Obstacle" };
    private enum ShieldState { Closed, Active, Closing, Cooldown } // 新增Closing状态
    private ShieldState state = ShieldState.Closed;
    private float cooldownTimer = 0f;
    private float closeAnimationTimer = 0f; // 关闭动画计时器
    private GameObject currentShield;
    private Transform shieldSpawnPoint;
    private KeyCode toggleKey = KeyCode.S;
    private HookSystem hookSystem;

    // Shader属性控制
    private Material shieldMaterial;
    private int radiusPropertyId;
    private int intensityPropertyId;
    private float currentRadius;
    private float targetRadius;


    private void Start()
    {
        shieldSpawnPoint = transform;
        hookSystem = GetComponent<HookSystem>();
        radiusPropertyId = Shader.PropertyToID("_Radius");
        intensityPropertyId = Shader.PropertyToID("_ShieldIntensity");
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
                    StartCloseAnimation(); // 触发关闭动画，而非直接销毁
                }
                else
                {
                    if (hookSystem != null)
                    {
                        hookSystem.currentTemperature += heatPerSecond * Time.deltaTime;
                    }
                }
                break;

            // 新增：关闭动画状态
            case ShieldState.Closing:
                closeAnimationTimer += Time.deltaTime;
                float progress = closeAnimationTimer / closeAnimationDuration; // 0~1的进度值

                // 动画逻辑：半径从当前值缩小到0
                currentRadius = Mathf.Lerp(maxRadius, 0, progress);
                shieldMaterial.SetFloat(radiusPropertyId, currentRadius);

                // 同时降低透明度（通过强度属性控制）
                float intensity = Mathf.Lerp(1f, 0f, progress);
                shieldMaterial.SetFloat(intensityPropertyId, intensity);

                // 动画结束后，销毁物体并进入冷却
                if (progress >= 1f)
                {
                    Destroy(currentShield);
                    currentShield = null;
                    Destroy(shieldMaterial); // 清理材质实例
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

    // 开始关闭动画
    private void StartCloseAnimation()
    {
        if (currentShield == null) return;

        state = ShieldState.Closed; // 先临时切换状态，避免重复触发
        closeAnimationTimer = 0f;

        // 禁用碰撞检测（避免动画过程中仍能销毁物体）
        ShieldCollisionHandler collisionHandler = currentShield.GetComponent<ShieldCollisionHandler>();
        if (collisionHandler != null)
        {
            collisionHandler.enabled = false;
        }

        state = ShieldState.Closing; // 进入关闭动画状态
    }

    private void UpdateShieldAnimation()
    {
        if (shieldMaterial == null) return;

        // 展开动画：半径从0增长到maxRadius
        currentRadius = Mathf.Lerp(currentRadius, targetRadius, radiusGrowSpeed * Time.deltaTime);
        shieldMaterial.SetFloat(radiusPropertyId, currentRadius);

        // 强度随过热状态变化（可选）
        if (hookSystem != null)
        {
            float intensity = Mathf.Lerp(0.5f, 1f, hookSystem.currentTemperature / hookSystem.overheatThreshold);
            shieldMaterial.SetFloat(intensityPropertyId, intensity);
        }
    }

    private bool CanActivateShield()
    {
        return hookSystem != null && hookSystem.currentOverheatState == HookSystem.OverheatState.Normal && cooldownTimer <= 0f;
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
        }
        else
        {
            Debug.LogWarning("护盾预制体缺少Renderer组件！");
        }

        ShieldCollisionHandler collisionHandler = currentShield.GetComponent<ShieldCollisionHandler>();
        if (collisionHandler != null)
        {
            collisionHandler.destructibleTags = new List<string>(destructibleTags);
            collisionHandler.enabled = true; // 激活时启用碰撞
        }
        else
        {
            Debug.LogWarning("shieldPrefab缺少ShieldCollisionHandler组件！");
        }

        state = ShieldState.Active;

        if (hookSystem != null)
        {
            hookSystem.currentTemperature += activateHeat;
        }
    }

}