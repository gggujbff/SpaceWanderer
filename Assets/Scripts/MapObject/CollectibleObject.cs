using UnityEngine;

public class CollectibleObject : MonoBehaviour
{
    public enum CollectibleSubType { Resource, Prop, Garbage, CollectibleObstacle }
    public enum CollectibleState { FreeFloating, AttachedToObstacle, Grabbed, Colliding, Destroyed, Harvested }

    [Header("可采集属性")]
    public CollectibleSubType subType;
    
    [Tooltip("可收集的分数值")]
    public int scoreValue = 10;

    [Tooltip("可收集道具数量")]
    public int missileCount = 0;  // 修正变量名（规范驼峰命名）
    public int laserCount = 0;    // 修正变量名
    
    [HideInInspector] public MissileLauncher missileLauncher;
    [HideInInspector] public LaserWeapon laserWeapon;  // 修正变量名（与类名一致）
    
    [Tooltip("质量")]
    public float mass = 1f;
    public float destroyedMomentum = 10f;
    public Vector2 velocity;
    public CollectibleState currentState;

    [Header("碰撞伤害设置")]
    [Tooltip("伤害系数（用于调控动量伤害的平衡）")]
    [Range(0.1f, 2f)] public float damageCoefficient = 0.5f;

    private Rigidbody2D rb;
    private bool pendingDestroy = false;
    private bool hasCollidedWithPlayer = false;
    // 新增：存储玩家身上的武器组件（避免重复查找）
    private MissileLauncher playerMissileLauncher;
    private LaserWeapon playerLaserWeapon;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.mass = mass;
            rb.velocity = velocity;
            rb.gravityScale = 0f;
        }

        currentState = CollectibleState.FreeFloating;

        // 初始化：找到玩家身上的武器组件（假设玩家标签为"Player"）
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerMissileLauncher = player.GetComponent<MissileLauncher>();
            playerLaserWeapon = player.GetComponent<LaserWeapon>();
        }
        else
        {
            Debug.LogWarning("场景中未找到标签为'Player'的物体，无法初始化武器引用");
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && 
            !isDestroyedState() && 
            !hasCollidedWithPlayer)
        {
            hasCollidedWithPlayer = true;
            CalculateAndApplyPlayerDamage(collision);
            // 碰撞玩家时直接收集（如果需要在抓取后收集，可移到OnHarvested方法）
            CollectForPlayer();
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            hasCollidedWithPlayer = false;
        }
    }

    private void CalculateAndApplyPlayerDamage(Collision2D collision)
    {
        float relativeSpeed = collision.relativeVelocity.magnitude;
        float momentum = mass * relativeSpeed;
        float damage = momentum * damageCoefficient;
        HookSystem.Instance.TakeDamage(damage);
        
        if (rb != null && !rb.isKinematic)
        {
            rb.AddForce(-collision.relativeVelocity * 0.1f, ForceMode2D.Impulse);
        }
    }

    public void OnHookCollision(HookSystem hook)
    {
        if (isDestroyedState()) return;

        currentState = CollectibleState.Colliding;
        float hookMomentum = hook.CurrentLaunchSpeed * hook.hookTipMass;

        if (hookMomentum >= destroyedMomentum)
        {
            pendingDestroy = true;
        }
    }

    public bool OnGrabbed(HookTipCollisionHandler hookTip)
    {
        if (isDestroyedState()) return false;

        if (currentState == CollectibleState.FreeFloating || currentState == CollectibleState.Colliding)
        {
            currentState = CollectibleState.Grabbed;
            transform.SetParent(hookTip.transform);
            transform.localPosition = Vector3.zero;

            if (rb != null)
            {
                rb.isKinematic = true;
                rb.velocity = Vector2.zero;
            }

            return true;
        }

        return false;
    }

    public void OnHarvested()
    {
        if (isDestroyedState()) return;

        if (pendingDestroy)
        {
            HookSystem.Instance.AddScore(scoreValue);
            DestroyObject();
            return;
        }

        currentState = CollectibleState.Harvested;

        // 收集分数
        switch (subType)
        {
            case CollectibleSubType.Resource:
            case CollectibleSubType.Prop:
            case CollectibleSubType.Garbage:
                HookSystem.Instance.AddScore(scoreValue);
                break;
            case CollectibleSubType.CollectibleObstacle:
                HookSystem.Instance.AddScore(scoreValue / 2);
                break;
        }

        // 收集武器数量（核心修改）
        CollectForPlayer();

        Destroy(gameObject);
    }

    // 新增：收集逻辑封装（碰撞或抓取后调用）
    private void CollectForPlayer()
    {
        // 增加导弹数量
        if (missileCount > 0 && playerMissileLauncher != null)
        {
            playerMissileLauncher.AddcurrentMissileCount(missileCount);
            Debug.Log($"收集了 {missileCount} 枚导弹，当前总数：{playerMissileLauncher.currentMissileCount}");
        }

        // 增加激光使用次数
        if (laserCount > 0 && playerLaserWeapon != null)
        {
            playerLaserWeapon.AddcurrentLaserCount(laserCount);
            //Debug.Log($"收集了 {laserCount} 次激光使用次数，当前总数：{playerLaserWeapon.fireCount}");
        }
    }

    public void DestroyObject()
    {
        currentState = CollectibleState.Destroyed;
        Destroy(gameObject);
    }

    public void SetVelocity(Vector2 newVelocity)
    {
        velocity = newVelocity;
        if (rb != null)
        {
            rb.velocity = newVelocity;
        }
    }

    public void SetMass(float newMass)
    {
        mass = newMass;
        if (rb != null)
        {
            rb.mass = newMass;
        }
    }
    
    private bool isDestroyedState()
    {
        return currentState == CollectibleState.Destroyed || currentState == CollectibleState.Harvested;
    }
}