using System.Collections.Generic;
using UnityEngine;

public class ShieldCollisionHandler : MonoBehaviour
{
    [Tooltip("需要与护盾发生交互的物体标签")]
    public List<string> targetTags = new List<string> { "Obstacle", "Collectible" };

    [Header("护盾属性")]
    [Tooltip("护盾最大生命值")]
    public float maxShieldHealth = 100f;
    [Tooltip("护盾当前生命值（运行时自动更新）")]
    [SerializeField] public float currentShieldHealth;
    [Tooltip("护盾伤害系数（影响最终受到的伤害值）")]
    public float shieldDamageCoefficient = 0.8f;

    [Header("反馈设置")]
    [Tooltip("护盾被击中时的特效预制体")]
    public GameObject hitEffectPrefab;
    [Tooltip("是否显示调试信息")]
    public bool showDebugInfo = true;

    // 事件声明
    public System.Action<float> onShieldHealthChanged;
    public System.Action onShieldDestroyed;

    private HookSystem hookSystem;
    private Rigidbody2D shieldRigidbody;

    private void Start()
    {
        currentShieldHealth = maxShieldHealth;
        hookSystem = HookSystem.Instance;
        shieldRigidbody = GetComponentInParent<Rigidbody2D>();
        
        if (shieldRigidbody == null)
        {
            shieldRigidbody = GetComponent<Rigidbody2D>();
            if (shieldRigidbody == null)
            {
                Debug.LogWarning("护盾未找到Rigidbody2D组件，碰撞物理计算将受影响");
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 过滤不相关标签的物体
        if (!targetTags.Contains(collision.gameObject.tag)) return;

        // 获取碰撞物的CollectibleObject组件
        CollectibleObject collideObject = collision.gameObject.GetComponent<CollectibleObject>();
        if (collideObject == null)
        {
            if (showDebugInfo)
                Debug.LogWarning($"碰撞物 {collision.gameObject.name} 缺少CollectibleObject组件，忽略碰撞");
            return;
        }

        // 忽略已销毁状态的物体
        if (collideObject.isDestroyedState()) return;

        // 执行碰撞伤害计算
        HandleShieldCollision(collideObject, collision);
    }

    private void HandleShieldCollision(CollectibleObject collideObject, Collision2D collision)
    {
        if (hookSystem == null || shieldRigidbody == null) return;

        // 1. 获取核心物理参数
        float shipMass = hookSystem.spaceShipMass;
        float objectMass = collideObject.mass;
        float reducedMass = (shipMass * objectMass) / (shipMass + objectMass);

        // 2. 计算相对速度
        Vector2 relativeVelocity = collision.relativeVelocity;
        float relativeSpeed = relativeVelocity.magnitude;

        // 3. 计算有效动量
        float effectiveMomentum = reducedMass * relativeSpeed;

        // 4. 计算护盾受到的最终伤害
        float shieldDamage = effectiveMomentum * collideObject.damageCoefficient * shieldDamageCoefficient;

        if (showDebugInfo)
        {
            Debug.Log($"=== 护盾碰撞计算 ===");
            Debug.Log($"碰撞物: {collideObject.name} (质量: {objectMass})");
            Debug.Log($"飞船质量: {shipMass}, 约化质量: {reducedMass:F2}");
            Debug.Log($"相对速度: {relativeSpeed:F2}, 有效动量: {effectiveMomentum:F2}");
            Debug.Log($"护盾伤害: {shieldDamage:F2} (剩余生命值: {currentShieldHealth - shieldDamage:F2})");
        }

        // 5. 应用伤害到护盾
        TakeDamage(shieldDamage, collision.GetContact(0).point);
    }

    private void TakeDamage(float damage, Vector2 hitPoint)
    {
        // 减少护盾生命值
        currentShieldHealth = Mathf.Max(0, currentShieldHealth - damage);
        
        // 触发生命值变化事件
        onShieldHealthChanged?.Invoke(currentShieldHealth);

        // 播放击中特效
        if (hitEffectPrefab != null)
        {
            GameObject effect = Instantiate(hitEffectPrefab, hitPoint, Quaternion.identity);
            Destroy(effect, 1f);
        }

        // 护盾失效逻辑
        if (currentShieldHealth <= 0)
        {
            onShieldDestroyed?.Invoke();
        }
    }
}