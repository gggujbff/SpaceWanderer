using System.Collections.Generic;
using UnityEngine;

public class ShieldController : MonoBehaviour
{
    [Header("冷却配置")]
    public float cooldownDuration = 3f;

    [Header("护盾预制体")]
    public GameObject shieldPrefab;

    [Header("过热参数")]
    public float activateHeat = 20f; // 激活护盾产生的热量
    public float heatPerSecond = 5f; // 护盾持续开启每秒产生的热量

    private List<string> destructibleTags = new List<string> { "Obstacle" };  // 护盾碰撞时销毁的物体标签名单
    private enum ShieldState { Closed, Active, Cooldown }
    private ShieldState state = ShieldState.Closed;
    private float cooldownTimer = 0f;
    private GameObject currentShield;
    private Transform shieldSpawnPoint;
    private KeyCode toggleKey = KeyCode.S;  // 切换护盾的按键

    private HookSystem hookSystem;

    private void Start()
    {
        shieldSpawnPoint = this.transform;
        hookSystem = GetComponent<HookSystem>();
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
                if (Input.GetKeyDown(toggleKey))
                {
                    DeactivateShield();
                }
                else
                {
                    // 护盾激活时持续放热
                    if (hookSystem != null)
                    {
                        hookSystem.currentTemperature += heatPerSecond * Time.deltaTime;
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

    private bool CanActivateShield()
    {
        return hookSystem != null && hookSystem.currentOverheatState == HookSystem.OverheatState.Normal && cooldownTimer <= 0f;
    }

    private void ActivateShield()
    {
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

        // 激活时增加温度
        if (hookSystem != null)
        {
            hookSystem.currentTemperature += activateHeat;
        }
    }

    private void DeactivateShield()
    {
        if (currentShield != null)
        {
            Destroy(currentShield);
            currentShield = null;
        }
        cooldownTimer = cooldownDuration;
        state = ShieldState.Cooldown;
    }
}