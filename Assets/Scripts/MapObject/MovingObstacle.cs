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
    public Vector2 velocity;
    public GameObject fragmentPrefab;

    [Header("运动参数")]
    public bool useSineMovement = false;
    public float waveAmplitude = 0.5f;
    public float waveFrequency = 2f;
    public float rotationSpeed = 10f;

    [Header("物理属性（失重环境）")]
    [Tooltip("弹性系数（0=完全非弹性，1=完全弹性）")]
    [Range(0f, 1f)] public float restitution = 0.6f; // 障碍物弹性较低
    [Tooltip("摩擦系数（0=无摩擦，1=最大摩擦）")]
    [Range(0f, 1f)] public float friction = 0.3f;

    private Rigidbody2D rb;
    private bool isDestroyed = false;
    private Vector2 initialVelocity;
    private bool hasCollidedWithPlayer = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.mass = mass;
            rb.velocity = velocity;
            rb.gravityScale = 0f;       // 失重环境
            rb.drag = 0f;               // 无空气阻力
            rb.angularDrag = 0.2f;      // 略大的旋转阻力
            initialVelocity = velocity;
        }
    }

    private void Update()
    {
        if (rb != null && useSineMovement && !isDestroyed)
        {
            // 正弦曲线运动（仅在未被碰撞破坏时生效）
            float waveOffset = Mathf.Sin(Time.time * waveFrequency) * waveAmplitude;
            rb.velocity = new Vector2(initialVelocity.x, initialVelocity.y + waveOffset);
            
            // 自转
            transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDestroyed) return;

        // 碰撞到玩家
        if (collision.gameObject.CompareTag("Player") && !hasCollidedWithPlayer)
        {
            hasCollidedWithPlayer = true;
            CalculateAndApplyPlayerDamage(collision);
            ApplyPhysicsCollision(collision); // 应用物理碰撞效果
        }
        // 碰撞到钩爪
        else if (collision.gameObject.CompareTag("Hook"))
        {
            HookSystem hookSystem = HookSystem.Instance;
            float hookMomentum = hookSystem.CurrentLaunchSpeed * hookSystem.hookTipMass;
            
            // 障碍物受到伤害
            TakeDamage(hookMomentum);
            
            // 如果动量超过阈值，立即销毁障碍物
            if (hookMomentum >= destroyedMomentum)
            {
                DestroyObstacle();
            }
        }
        // 与其他可交互物体碰撞
        else if (collision.gameObject.TryGetComponent<CollectibleObject>(out _) || 
                 collision.gameObject.TryGetComponent<MovingObstacle>(out _))
        {
            ApplyPhysicsCollision(collision);
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        // 重置玩家碰撞标记
        if (collision.gameObject.CompareTag("Player"))
        {
            hasCollidedWithPlayer = false;
        }
    }

    /// <summary>
    /// 应用符合失重环境的物理碰撞效果（动量守恒）
    /// </summary>
    private void ApplyPhysicsCollision(Collision2D collision)
    {
        Rigidbody2D otherRb = collision.rigidbody;
        if (rb == null || otherRb == null || isDestroyed) return;

        // 获取碰撞点法线和切线方向
        ContactPoint2D contact = collision.contacts[0];
        Vector2 normal = contact.normal;
        Vector2 tangent = new Vector2(-normal.y, normal.x);

        // 飞船不参与物理反应，仅自身反弹
        if (otherRb.CompareTag("Player"))
        {
            // 自己反弹一点（模拟动量损失）
            float thisNormalSpeed = Vector2.Dot(rb.velocity, normal);
            float newSpeed = -thisNormalSpeed * restitution;
            Vector2 newVelocity = rb.velocity + normal * (newSpeed - thisNormalSpeed);
            rb.velocity = newVelocity;

            // 可选：增加轻微扰动避免卡住
            ApplyAntiStickForce(rb, null);
            return;
        }

        // 分解速度为法线和切线分量
        float thisNormalSpeedFull = Vector2.Dot(rb.velocity, normal);
        float otherNormalSpeedFull = Vector2.Dot(otherRb.velocity, normal);
        float thisTangentSpeed = Vector2.Dot(rb.velocity, tangent);
        float otherTangentSpeed = Vector2.Dot(otherRb.velocity, tangent);

        // 计算碰撞后法线方向速度（动量守恒 + 弹性系数）
        float massSum = mass + otherRb.mass;
        float thisNewNormalSpeed = (thisNormalSpeedFull * (mass - restitution * otherRb.mass) + 
                                    2 * restitution * otherRb.mass * otherNormalSpeedFull) / massSum;
        float otherNewNormalSpeed = (otherNormalSpeedFull * (otherRb.mass - restitution * mass) + 
                                     2 * restitution * mass * thisNormalSpeedFull) / massSum;

        // 计算碰撞后切线方向速度（应用摩擦力）
        float thisNewTangentSpeed = Mathf.Lerp(thisTangentSpeed, otherTangentSpeed, friction);
        float otherNewTangentSpeed = Mathf.Lerp(otherTangentSpeed, thisTangentSpeed, friction);

        // 应用最终速度
        rb.velocity = normal * thisNewNormalSpeed + tangent * thisNewTangentSpeed;
        otherRb.velocity = normal * otherNewNormalSpeed + tangent * otherNewTangentSpeed;

        ApplyAntiStickForce(rb, otherRb);
    }


    /// <summary>
    /// 应用微小随机力防止物体碰撞后粘连
    /// </summary>
    private void ApplyAntiStickForce(Rigidbody2D rb1, Rigidbody2D rb2)
    {
        Vector2 randomDir = Random.insideUnitCircle.normalized * 0.02f;
        rb1?.AddForce(randomDir, ForceMode2D.Impulse);
        rb2?.AddForce(-randomDir, ForceMode2D.Impulse);
    }


    /// <summary>
    /// 计算并对玩家施加伤害
    /// </summary>
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
        
        // 生成碎片效果
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
        
        // 如果动量超过阈值，立即销毁障碍物
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
            rb.velocity = newVelocity;
            initialVelocity = newVelocity; // 同步初始速度用于正弦运动
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