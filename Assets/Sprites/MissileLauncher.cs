using UnityEngine;

public class MissileLauncher : MonoBehaviour
{
    [Header("飞弹配置")]
    [Tooltip("飞弹预制体")]
    public GameObject missilePrefab;

    [Header("发射参数")]
    [Tooltip("发射冷却时间（秒）")]
    public float cooldown = 2f;
    [Tooltip("每次发射消耗的能量值")]
    public int energyCost = 5;
    [Tooltip("飞弹初始速度（米/秒）")]
    public float missileSpeed = 10f;
    [Tooltip("发射按键")]
    public KeyCode fireKey = KeyCode.Alpha1;

    private float lastFireTime = float.MinValue; // 上次发射时间
    private HookSystem hookSystem; // 能量管理系统引用

    void Awake()
    {
        hookSystem = GetComponent<HookSystem>();
    }

    void Update()
    {
        if (Input.GetKeyDown(fireKey) && CanFire())
        {
            FireMissile();
        }
    }

    // 判断是否可以发射飞弹
    private bool CanFire()
    {
        return (Time.time - lastFireTime >= cooldown) && 
               (hookSystem != null && hookSystem.currentEnergy >= energyCost);
    }

    // 飞弹发射（包含能量调试）
    public void FireMissile()
    {
        if (missilePrefab == null)
        {
            Debug.LogError("未指定飞弹预制体！");
            return;
        }

        // 计算鼠标位置与发射方向
        Vector3 mouseWorldPos3D = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 mouseWorldPos = new Vector2(mouseWorldPos3D.x, mouseWorldPos3D.y);
        Vector2 fireDirection = (mouseWorldPos - (Vector2)transform.position).normalized;

        // 实例化并初始化飞弹
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
            Debug.LogWarning("飞弹预制体缺少Missile组件！");
        }

        // 消耗能量并输出调试信息
        if (hookSystem != null)
        {
            float energyBefore = hookSystem.currentEnergy; // 记录消耗前的能量
            hookSystem.currentEnergy -= energyCost;
            float energyAfter = hookSystem.currentEnergy;  // 记录消耗后的能量

            // 输出能量变化调试
            Debug.Log($"飞弹发射 - 消耗能量: {energyCost} | 剩余能量: {energyAfter}");
        }

        lastFireTime = Time.time;
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.2f); // 发射点标记

        if (Application.isPlaying)
        {
            Vector3 mouseWorldPos3D = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Gizmos.DrawLine(
                transform.position, 
                new Vector2(mouseWorldPos3D.x, mouseWorldPos3D.y)
            );
        }
    }
}