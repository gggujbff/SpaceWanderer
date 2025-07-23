using UnityEngine;

public class CollectibleObject : MonoBehaviour
{
    public enum CollectibleSubType { Resource, Prop, Garbage, CollectibleObstacle }
    public enum CollectibleState { FreeFloating, AttachedToObstacle, Grabbed, Colliding, Destroyed, Harvested }

    [Header("可采集属性")]
    public CollectibleSubType subType;
    
    [Tooltip("可收集的分数值")]
    public int scoreValue = 10;
    
    [Tooltip("可收集道具")]
    public string propTag;
    
    [Tooltip("质量")]
    public float mass = 1f;
    public float destroyedMomentum = 10f;
    public Vector2 velocity;
    public CollectibleState currentState;

    [Header("碰撞伤害设置")]
    [Tooltip("伤害系数（用于调控动量伤害的平衡）")]
    [Range(0.1f, 2f)] public float damageCoefficient = 0.5f; // 原伤害变量改为系数

    private Rigidbody2D rb;
    private bool pendingDestroy = false;
    private bool hasCollidedWithPlayer = false; // 防止单次碰撞多次触发伤害

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
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 检测与飞船（Player）的碰撞
        if (collision.gameObject.CompareTag("Player") && 
            !isDestroyedState() && 
            !hasCollidedWithPlayer)
        {
            hasCollidedWithPlayer = true; // 标记已碰撞，避免帧内多次触发
            CalculateAndApplyPlayerDamage(collision);
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        // 离开碰撞时重置标记，允许再次碰撞触发伤害
        if (collision.gameObject.CompareTag("Player"))
        {
            hasCollidedWithPlayer = false;
        }
    }

    /// <summary>
    /// 计算并对飞船施加伤害（基于动量）
    /// </summary>
    private void CalculateAndApplyPlayerDamage(Collision2D collision)
    {
        // 相对速度大小（碰撞瞬间的相对速度）
        float relativeSpeed = collision.relativeVelocity.magnitude;
        
        // 动量 = 质量 × 速度（使用自身质量和相对速度计算）
        float momentum = mass * relativeSpeed;
        
        // 最终伤害 = 动量 × 伤害系数（系数用于平衡整体伤害数值）
        float damage = momentum * damageCoefficient;
        
        // 对飞船造成伤害
        HookSystem.Instance.TakeDamage(damage);
        
        // 可选：自身也受到碰撞影响（例如被弹开）
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
            int destroyScore = Mathf.Max(1, scoreValue);
            HookSystem.Instance.AddScore(destroyScore);
            DestroyObject();
            return;
        }

        currentState = CollectibleState.Harvested;

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

        Destroy(gameObject);
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