using System.Collections.Generic;
using UnityEngine;

public class ShieldController : MonoBehaviour
{
    [Header("开启初次消耗的能量")]
    public float initialEnergyCost = 10f;

    [Header("每秒消耗的能量")]
    public float energyDrainPerSecond = 5f;

    [Header("冷却配置")]
    public float cooldownDuration = 3f;

    [Header("护盾预制体")]
    public GameObject shieldPrefab;
    
    private List<string> destructibleTags = new List<string> { "Obstacle" };  //护盾碰撞时销毁的物体标签名单
    private enum ShieldState { Closed, Active, Cooldown }
    private ShieldState state = ShieldState.Closed;
    private float cooldownTimer = 0f;
    private GameObject currentShield;
    private HookSystem hookSystem;
    private Transform shieldSpawnPoint;
    private KeyCode toggleKey = KeyCode.S;  // 切换护盾的按键

    
    private float debugLogTimer = 0f;

    private void Start()
    {
        hookSystem = GetComponent<HookSystem>();
        shieldSpawnPoint = this.transform;
    }

    private void Update()
    {
        switch (state)
        {
            case ShieldState.Closed:
                if (Input.GetKeyDown(toggleKey) && hookSystem != null && hookSystem.currentEnergy >= initialEnergyCost)
                {
                    ActivateShield();
                }
                break;

            case ShieldState.Active:
                if (Input.GetKeyDown(toggleKey))
                {
                    DeactivateShield();
                }
                else
                {
                    DrainEnergy();
                    debugLogTimer += Time.deltaTime;
                    if (debugLogTimer >= 1f)
                    {
                        Debug.Log($"[护盾] 当前能量: {hookSystem.currentEnergy:F2}, 每秒消耗: {energyDrainPerSecond}");
                        debugLogTimer = 0f;
                    }
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

    private void ActivateShield()  // 开启护盾
    {
        hookSystem.currentEnergy -= initialEnergyCost;
        currentShield = Instantiate(shieldPrefab, shieldSpawnPoint.position, shieldSpawnPoint.rotation, shieldSpawnPoint);

        ShieldCollisionHandler collisionHandler = currentShield.GetComponent<ShieldCollisionHandler>();
        if (collisionHandler != null)
        {
            collisionHandler.destructibleTags = new List<string>(destructibleTags);
        }
        else
        {
            Debug.LogWarning("shieldPrefab缺少ShieldCollisionHandler组件！");
        }
        state = ShieldState.Active;
    }

    private void DeactivateShield()  //  碰撞时销毁物体
    {
        if (currentShield != null)
        {
            Destroy(currentShield);
            currentShield = null;
        }
        cooldownTimer = cooldownDuration;
        state = ShieldState.Cooldown;
    }

    private void DrainEnergy()  // 减少护盾的能量
    {
        if (hookSystem == null) return;

        float drainAmount = energyDrainPerSecond * Time.deltaTime;
        if (hookSystem.currentEnergy >= drainAmount)
        {
            hookSystem.currentEnergy -= drainAmount;
        }
        else
        {
            // 能量不足，自动关闭护盾
            DeactivateShield();
        }
    }
}
