using UnityEngine;

public class MovingObstacle : MonoBehaviour
{
    [Header("基础属性")]
    [Tooltip("生命值")]
    public float health = 10f;
    
    [Tooltip("伤害系数（用于调控动量伤害的平衡）")]
    [Range(0.1f, 2f)] public float damageCoefficient = 0.8f;
    
    [Tooltip("质量")]
    public float mass = 2f;
    
    public float destroyedMomentum = 5f;
    public Vector2 velocity; // 仅由Spawner设置的初始速度
    public GameObject fragmentPrefab;

    [Header("物理属性（失重环境）")]
    [Tooltip("弹性系数（0=完全非弹性，1=完全弹性）")]
    [Range(0f, 1f)] public float restitution = 0.6f;
    [Tooltip("摩擦系数（0=无摩擦，1=最大摩擦）")]
    [Range(0f, 1f)] public float friction = 0.3f;

    private Rigidbody2D rb;
    private bool isDestroyed = false;
    private bool hasCollidedWithPlayer = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.mass = mass;
            rb.velocity = velocity; // 初始速度仅由Spawner设置
            rb.gravityScale = 0f; // 失重环境
            rb.drag = 0f; // 无自主减速
            rb.angularDrag = 0.2f; // 仅保留轻微旋转阻力
        }
    }

    // 移除Update方法中的所有自主运动逻辑（原正弦运动、自转等）
    private void Update()
    {
        // 无任何自主移动代码，仅保留空方法（如需扩展可在此添加）
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDestroyed) return;

        // 碰撞到玩家
        if (collision.gameObject.CompareTag("Player") && !hasCollidedWithPlayer)
        {
            hasCollidedWithPlayer = true;
            CalculateAndApplyPlayerDamage(collision);
            ApplyPhysicsCollision(collision); // 仅由碰撞力驱动移动
        }
        // 碰撞到钩爪
        else if (collision.gameObject.CompareTag("Hook"))
        {
            HookSystem hookSystem = HookSystem.Instance;
            float hookMomentum = hookSystem.CurrentLaunchSpeed * hookSystem.hookTipMass;
            
            TakeDamage(hookMomentum);
            
            if (hookMomentum >= destroyedMomentum)
            {
                DestroyObstacle();
            }
        }
        else if (collision.gameObject.TryGetComponent<CollectibleObject>(out _) || 
                 collision.gameObject.TryGetComponent<MovingObstacle>(out _))
        {
            ApplyPhysicsCollision(collision);
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            hasCollidedWithPlayer = false;
        }
    }

    //仅应用物理碰撞效果（动量守恒）
    private void ApplyPhysicsCollision(Collision2D collision)
    {
        Rigidbody2D otherRb = collision.rigidbody;
        if (rb == null || otherRb == null || isDestroyed) return;

        ContactPoint2D contact = collision.contacts[0];
        Vector2 normal = contact.normal;
        Vector2 tangent = new Vector2(-normal.y, normal.x);

        if (otherRb.CompareTag("Player"))
        {
            float thisNormalSpeed = Vector2.Dot(rb.velocity, normal);
            float newSpeed = -thisNormalSpeed * restitution; // 仅由碰撞反弹
            Vector2 newVelocity = rb.velocity + normal * (newSpeed - thisNormalSpeed);
            rb.velocity = newVelocity;
            ApplyAntiStickForce(rb, null);
            return;
        }

        float thisNormalSpeedFull = Vector2.Dot(rb.velocity, normal);
        float otherNormalSpeedFull = Vector2.Dot(otherRb.velocity, normal);
        float thisTangentSpeed = Vector2.Dot(rb.velocity, tangent);
        float otherTangentSpeed = Vector2.Dot(otherRb.velocity, tangent);

        float massSum = mass + otherRb.mass;
        float thisNewNormalSpeed = (thisNormalSpeedFull * (mass - restitution * otherRb.mass) + 
                                    2 * restitution * otherRb.mass * otherNormalSpeedFull) / massSum;
        float otherNewNormalSpeed = (otherNormalSpeedFull * (otherRb.mass - restitution * mass) + 
                                     2 * restitution * mass * thisNormalSpeedFull) / massSum;

        float thisNewTangentSpeed = Mathf.Lerp(thisTangentSpeed, otherTangentSpeed, friction);
        float otherNewTangentSpeed = Mathf.Lerp(otherTangentSpeed, thisTangentSpeed, friction);

        rb.velocity = normal * thisNewNormalSpeed + tangent * thisNewTangentSpeed;
        otherRb.velocity = normal * otherNewNormalSpeed + tangent * otherNewTangentSpeed;

        ApplyAntiStickForce(rb, otherRb);
    }

    //应用微小力防止碰撞粘连（仅由物理碰撞触发）
    private void ApplyAntiStickForce(Rigidbody2D rb1, Rigidbody2D rb2)
    {
        Vector2 randomDir = Random.insideUnitCircle.normalized * 0.02f;
        rb1?.AddForce(randomDir, ForceMode2D.Impulse);
        rb2?.AddForce(-randomDir, ForceMode2D.Impulse);
    }

    private void CalculateAndApplyPlayerDamage(Collision2D collision)
    {
        float relativeSpeed = collision.relativeVelocity.magnitude;
        float momentum = mass * relativeSpeed;
        float damage = momentum * damageCoefficient;
        HookSystem.Instance.TakeDamage(damage);
    }

    public void TakeDamage(float damageAmount)
    {
        health -= damageAmount;
        if (health <= 0 && !isDestroyed)
        {
            DestroyObstacle();
        }
    }

    private void DestroyObstacle()
    {
        isDestroyed = true;
        
        if (fragmentPrefab != null)
        {
            Instantiate(fragmentPrefab, transform.position, Quaternion.identity);
        }
        
        Destroy(gameObject);
    }

    public void OnHookCollision(HookSystem hook)
    {
        if (isDestroyed) return;

        float hookMomentum = hook.CurrentLaunchSpeed * hook.hookTipMass;
        TakeDamage(hookMomentum);
        
        if (hookMomentum >= destroyedMomentum)
        {
            DestroyObstacle();
        }
    }
    
    public void SetVelocity(Vector2 newVelocity)
    {
        velocity = newVelocity;
        if (rb != null && !isDestroyed)
        {
            rb.velocity = newVelocity; // 仅由外部（如Spawner）设置初始速度
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
}