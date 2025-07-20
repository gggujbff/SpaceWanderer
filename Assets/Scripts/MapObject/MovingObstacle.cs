using UnityEngine;

public class MovingObstacle : MonoBehaviour
{
    // 基础属性
    [Header("基础属性")]
    
    [Tooltip("生命值")]
    public float health = 10f;
    
    [Tooltip("伤害")]
    public float damage = 5f;
    
    [Tooltip("质量")]
    public float mass = 2f;
    
    public float destroyedMomentum = 5f;
    public Vector2 velocity;
    public GameObject fragmentPrefab;

    // 运动参数
    [Header("运动参数")]
    public bool useSineMovement = false;
    public float waveAmplitude = 0.5f;
    public float waveFrequency = 2f;
    public float rotationSpeed = 10f;

    private Rigidbody2D rb;
    private bool isDestroyed = false;
    private Vector2 initialVelocity;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.mass = mass;
            rb.velocity = velocity;
            rb.gravityScale = 0f; // 无重力
            rb.drag = 0f;         // 无阻力
            
            initialVelocity = velocity;
        }
    }

    private void Update()
    {
        if (rb != null && useSineMovement)
        {
            // 正弦曲线运动
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
        if (collision.gameObject.CompareTag("Player"))
        {
            HookSystem.Instance.TakeDamage(damage);
            TakeDamage(collision.relativeVelocity.magnitude * mass);
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

    // 新增：钩爪碰撞处理方法
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
}