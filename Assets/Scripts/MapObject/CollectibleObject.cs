using UnityEngine;
using System.Collections;

public class CollectibleObject : MonoBehaviour
{
    public enum CollectibleSubType { Resource, Prop, Garbage, CollectibleObstacle, RegularObstacle }
    public enum CollectibleState { FreeFloating, AttachedToObstacle, Grabbed, Colliding, Damaged, Destroyed, Harvested }

    [Header("基础属性")]
    public CollectibleSubType subType;
    public CollectibleState currentState;
    
    [Tooltip("收集后获得的分数")]
    public int scoreValue = 10;

    [Tooltip("收集后获得的道具数量")]
    public int missileCount = 0; 
    public int laserCount = 0; 
    public int netCount = 0;  

    [Header("物理属性")]
    [Tooltip("物体质量")]
    public float mass = 1f;
    [Tooltip("生命值")]
    public float health = 100f; // 生命值
    [Tooltip("最大生命值")]
    public float maxHealth = 100f; // 最大生命值
    
    [Header("初始速度设置")]
    [Tooltip("初始速度大小（由Spawner控制）")]
    public float initialSpeed = 0f;
    
    [Tooltip("初始速度方向（由Spawner控制）")]
    public Vector2 initialDirection = Vector2.right;
    
    [Header("初始旋转设置")]
    [Tooltip("初始旋转角速度（正负方向随机）")]
    public float initialAngularSpeed = 0f;
    private bool initialRotationApplied = false;

    [Header("碰撞伤害设置")]
    [Tooltip("销毁所需的最小相对动量（碰撞瞬间检测）")]
    public float destroyedMomentum = 10f;
    public float damageCoefficient = 0.5f;


    [Header("显示设置")]
    public bool showDebugInfo = true; // 是否显示调试信息
    
    private Rigidbody2D rb;
    private bool hasCollidedWithPlayer = false;
    private MissileLauncher playerMissileLauncher;
    private LaserWeapon playerLaserWeapon;
    private NetLauncher playerNetLauncher;
    private bool initialVelocityApplied = false;
    private bool velocitySetBySpawner = false;
    private float restitution = 1f;
    private float friction = 0f;  //  
    private GameObject fragmentPrefab;
    private GameObject damageEffectPrefab; // 碰撞伤害效果
    private HookSystem hookSystem;


    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        hookSystem = HookSystem.Instance;
        if (hookSystem == null)
        {
            Debug.LogError("场景中未找到HookSystem实例！请确保HookSystem已挂载且设置为单例模式");
        }
        
        // 标准化初始方向向量
        NormalizeInitialDirection();
        
        if (rb != null)
        {
            rb.mass = mass;
            rb.gravityScale = 0f;
            rb.drag = 0f;
            rb.angularDrag = 0.2f;

            if (IsObstacleType())
            {
                rb.sharedMaterial = new PhysicsMaterial2D
                {
                    bounciness = restitution,
                    friction = friction
                };
            }

            if (!velocitySetBySpawner && initialSpeed > 0)
            {
                StartCoroutine(DelayedApplyVelocity());
            }
        }
        else
        {
            Debug.LogError($"{gameObject.name} 缺少Rigidbody2D组件！");
        }

        currentState = CollectibleState.FreeFloating;
        health = maxHealth; // 初始化生命值
        
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerMissileLauncher = player.GetComponent<MissileLauncher>();
            playerLaserWeapon = player.GetComponent<LaserWeapon>();
            playerNetLauncher = player.GetComponent<NetLauncher>();
        }
        else
        {
            Debug.LogWarning("场景中未找到标签为'Player'的物体");
        }
        
        if (!gameObject.CompareTag("Collectible") && 
            !gameObject.CompareTag("Player") && 
            !gameObject.CompareTag("Hook"))
        {
            Debug.LogWarning($"{gameObject.name} 的Tag未设置为'Collectible'，可能无法参与碰撞逻辑");
        }
        
        ApplyInitialRotation();
    }

    /// <summary>
    /// 标准化初始方向向量，确保其模长为1（仅表示方向）
    /// 若原始向量为零向量，默认向右（Vector2.right）
    /// </summary>
    private void NormalizeInitialDirection()
    {
        // 计算原始方向向量的模长
        float magnitude = initialDirection.magnitude;
        
        // 若模长接近0（零向量），赋予默认方向
        if (magnitude < 0.001f)
        {
            initialDirection = Vector2.right; // 默认向右
            Debug.LogWarning($"{gameObject.name} 的初始方向为零向量，已自动设置为向右（Vector2.right）");
        }
        else
        {
            // 标准化：按原比例缩放至模长=1
            initialDirection = initialDirection.normalized;
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"{gameObject.name} 标准化后方向：X={initialDirection.x:F2}, Y={initialDirection.y:F2}，模长={initialDirection.magnitude:F2}");
        }
    }

    private void ApplyInitialVelocity()
    {
        if (rb != null && !initialVelocityApplied)
        {
            Vector2 finalVelocity = initialDirection * initialSpeed;
            rb.velocity = finalVelocity;
            initialVelocityApplied = true;
            if (showDebugInfo)
                Debug.Log($"{gameObject.name} 初始速度: {finalVelocity}");
        }
    }

    public void SetInitialVelocity(float speed, Vector2 direction)
    {
        initialSpeed = speed;
        // 确保外部设置的方向也被标准化
        initialDirection = direction.normalized;
        velocitySetBySpawner = true;
        StartCoroutine(DelayedApplyVelocity());
    }

    private IEnumerator DelayedApplyVelocity()
    {
        yield return null;
        
        if (rb != null)
        {
            Vector2 finalVelocity = initialDirection * initialSpeed;
            rb.velocity = finalVelocity;
            if (showDebugInfo)
                Debug.Log($"[{Time.time}] 延迟应用速度: {finalVelocity} (物体: {gameObject.name})");
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDestroyedState()) return;

        if (showDebugInfo)
        {
            Debug.Log($"=== 碰撞发生 ===");
            Debug.Log($"碰撞物体1: {gameObject.name} (Tag: {gameObject.tag})");
            Debug.Log($"碰撞物体2: {collision.gameObject.name} (Tag: {collision.gameObject.tag})");
        }

        // 检查接触点数组是否有效，避免后续逻辑中的索引越界
        if (collision.contacts.Length == 0)
        {
            Debug.LogWarning($"碰撞无接触点: {gameObject.name} vs {collision.gameObject.name}，跳过处理");
            return;
        }

        if (collision.gameObject.CompareTag("Player") && !hasCollidedWithPlayer)
        {
            if (showDebugInfo)
                Debug.Log($"{gameObject.name} 碰撞玩家");
                
            hasCollidedWithPlayer = true;
            CalculateAndApplyPlayerDamage(collision);
            
            if (IsObstacleType())
            {
                ApplyPhysicsCollision(collision);
            }
            
            if (IsCollectibleType())
            {
                CollectForPlayer();
            }
        }
        else if (collision.gameObject.CompareTag("Hook"))
        {
            if (showDebugInfo)
                Debug.Log($"{gameObject.name} 碰撞钩爪");
            currentState = CollectibleState.Colliding;
        }
        else if (collision.gameObject.CompareTag("Collectible"))
        {
            if (showDebugInfo)
                Debug.Log($"{gameObject.name} 碰撞Collectible物体: {collision.gameObject.name}");
                
            if (collision.gameObject.TryGetComponent<CollectibleObject>(out CollectibleObject otherCollectible))
            {
                ApplyPhysicsCollision(collision);
                ApplyCollisionDamage(otherCollectible, collision);
            }
            else
            {
                Debug.LogError($"{collision.gameObject.name} 是'Collectible' Tag但未挂载CollectibleObject组件！");
            }
        }
        else
        {
            if (showDebugInfo)
                Debug.Log($"未处理的碰撞类型: {collision.gameObject.tag}");
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            hasCollidedWithPlayer = false;
        }
    }

    // 应用碰撞伤害（物体间碰撞）
    private void ApplyCollisionDamage(CollectibleObject other, Collision2D collision)
    {
        if (rb == null || other.rb == null)
        {
            Debug.LogError("缺少Rigidbody2D组件，无法计算碰撞伤害");
            return;
        }

        // 确保使用有效索引访问接触点数组
        int contactIndex = Mathf.Clamp(0, 0, collision.contacts.Length - 1);
        ContactPoint2D contact = collision.contacts[contactIndex];

        // 计算约化质量
        float m1 = mass;
        float m2 = other.mass;
        float reducedMass = (m1 * m2) / (m1 + m2);
    
        // 相对速度大小
        float relativeSpeed = collision.relativeVelocity.magnitude;
    
        // 计算有效动量（使用约化质量）
        float effectiveMomentum = reducedMass * relativeSpeed;
    
        // 计算伤害（基于动量和系数）
        float damageToThis = effectiveMomentum * damageCoefficient;
        float damageToOther = effectiveMomentum * other.damageCoefficient;

        if (showDebugInfo)
        {
            Debug.Log($"--- 碰撞伤害计算 ---");
            Debug.Log($"{gameObject.name} 质量: {m1}, {other.gameObject.name} 质量: {m2}");
            Debug.Log($"相对速度: {relativeSpeed:F2}, 约化质量: {reducedMass:F2}");
            Debug.Log($"有效动量: {effectiveMomentum:F2}");
            Debug.Log($"{gameObject.name} 受到伤害: {damageToThis:F2}");
            Debug.Log($"{other.gameObject.name} 受到伤害: {damageToOther:F2}");
        }

        // 应用伤害（使用安全索引获取的接触点）
        TakeDamage(damageToThis, contact.point);
        other.TakeDamage(damageToOther, contact.point);
    }

    private void CalculateAndApplyPlayerDamage(Collision2D collision)
    {
        // 检查HookSystem是否存在
        if (hookSystem == null)
        {
            Debug.LogError("HookSystem实例不存在，无法计算对玩家的伤害！");
            return;
        }

        if (rb == null) return;

        // 确保使用有效索引访问接触点数组
        int contactIndex = Mathf.Clamp(0, 0, collision.contacts.Length - 1);
        ContactPoint2D contact = collision.contacts[contactIndex];

        float shipMass = hookSystem.spaceShipMass;
        float reducedMass = (mass * shipMass) / (mass + shipMass);
        float relativeSpeed = collision.relativeVelocity.magnitude;
        float effectiveMomentum = reducedMass * relativeSpeed;
        
        // 使用HookSystem中的kHealth参数调整伤害比例
        float damage = effectiveMomentum * damageCoefficient * hookSystem.kHealth;

        if (showDebugInfo)
        {
            Debug.Log($"--- 玩家碰撞伤害计算 ---");
            Debug.Log($"{gameObject.name} 质量: {mass}, 飞船质量: {shipMass}");
            Debug.Log($"相对速度: {relativeSpeed:F2}, 约化质量: {reducedMass:F2}");
            Debug.Log($"有效动量: {effectiveMomentum:F2}");
            Debug.Log($"伤害系数: {damageCoefficient}, 受伤参数: {hookSystem.kHealth}");
            Debug.Log($"{gameObject.name} 对飞船造成伤害: {damage:F2}");
        }

        hookSystem.TakeDamage(damage);
        TakeDamage(damage * damageCoefficient, contact.point);
    }

    // 应用物理碰撞响应
    private void ApplyPhysicsCollision(Collision2D collision)
    {
        Rigidbody2D otherRb = collision.rigidbody;
        if (rb == null || otherRb == null || isDestroyedState()) return;

        // 确保使用有效索引访问接触点数组
        int contactIndex = Mathf.Clamp(0, 0, collision.contacts.Length - 1);
        ContactPoint2D contact = collision.contacts[contactIndex];
        
        Vector2 normal = contact.normal;
        Vector2 tangent = new Vector2(-normal.y, normal.x);

        float thisNormalSpeed = Vector2.Dot(rb.velocity, normal);
        float otherNormalSpeed = Vector2.Dot(otherRb.velocity, normal);
        float thisTangentSpeed = Vector2.Dot(rb.velocity, tangent);
        float otherTangentSpeed = Vector2.Dot(otherRb.velocity, tangent);

        float massSum = mass + otherRb.mass;
        float thisNewNormalSpeed = (thisNormalSpeed * (mass - restitution * otherRb.mass) + 
                                    2 * restitution * otherRb.mass * otherNormalSpeed) / massSum;
        float otherNewNormalSpeed = (otherNormalSpeed * (otherRb.mass - restitution * mass) + 
                                     2 * restitution * mass * thisNormalSpeed) / massSum;

        float thisNewTangentSpeed = Mathf.Lerp(thisTangentSpeed, otherTangentSpeed, friction);
        float otherNewTangentSpeed = Mathf.Lerp(otherTangentSpeed, thisTangentSpeed, friction);

        rb.velocity = normal * thisNewNormalSpeed + tangent * thisNewTangentSpeed;
        otherRb.velocity = normal * otherNewNormalSpeed + tangent * otherNewTangentSpeed;

        ApplyAntiStickForce(rb, otherRb);
    }

    // 应用反粘连力，防止物体粘在一起
    private void ApplyAntiStickForce(Rigidbody2D rb1, Rigidbody2D rb2)
    {
        Vector2 randomDir = Random.insideUnitCircle.normalized * 0.02f;
        rb1?.AddForce(randomDir, ForceMode2D.Impulse);
        rb2?.AddForce(-randomDir, ForceMode2D.Impulse);
    }

    // 处理受到的伤害
    public void TakeDamage(float damage, Vector2 hitPoint)
    {
        if (isDestroyedState()) return;
        
        // 减少生命值
        health -= damage;
        
        // 显示伤害效果（如果需要可以保留，不需要可以注释掉）
        if (damageEffectPrefab != null)
        {
            GameObject effect = Instantiate(damageEffectPrefab, hitPoint, Quaternion.identity);
            Destroy(effect, 1.0f);
        }
        
        if (showDebugInfo)
            Debug.Log($"{gameObject.name} 受到伤害: {damage:F2}, 剩余生命值: {health:F2}");

        // 检查是否应该销毁
        if (health <= 0)
        {
            DestroyObject();
        }
        else
        {
            currentState = CollectibleState.Damaged;
        }
    }

    public void OnHookCollision(HookSystem hook)
    {
        if (isDestroyedState()) return;
        currentState = CollectibleState.Colliding;
    }

    public bool OnGrabbed(HookTipCollisionHandler hookTip)
    {
        if (isDestroyedState() || !IsCollectibleType()) return false;

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

    public void OnReleased()
    {
        if (currentState != CollectibleState.Grabbed) return;

        currentState = CollectibleState.FreeFloating;
        // 关键：彻底解除与钩爪的父子关系
        if (transform.parent != null && transform.parent.CompareTag("Hook"))
        {
            transform.SetParent(null); // 解除父对象
            // 强制与钩爪位置分离（避免帧同步延迟导致的跟随）
            transform.position += (Vector3)Random.insideUnitCircle * 0.1f; 
        }

        if (rb != null)
        {
            rb.isKinematic = false; // 恢复物理响应
            // 赋予微小初速度，确保脱离钩爪
            rb.velocity = new Vector2(
                Random.Range(-1f, 1f), 
                Random.Range(-1f, 1f)
            );
        }
    }

    public void OnHarvested()
    {
        if (isDestroyedState()) return;

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

        if (IsCollectibleType())
        {
            CollectForPlayer();
        }

        Destroy(gameObject);
    }

    private void CollectForPlayer()
    {
        if (missileCount > 0 && playerMissileLauncher != null)
            playerMissileLauncher.AddcurrentMissileCount(missileCount);
        
        if (laserCount > 0 && playerLaserWeapon != null)
            playerLaserWeapon.AddcurrentLaserCount(laserCount);
        
        if (netCount > 0 && playerNetLauncher != null)
            playerNetLauncher.AddcurrentNetCount(netCount);
    }

    public void DestroyObject()
    {
        currentState = CollectibleState.Destroyed;
        if (showDebugInfo)
            Debug.Log($"{gameObject.name} 执行销毁");
        
        if (IsObstacleType() && fragmentPrefab != null)
        {
            GameObject fragments = Instantiate(fragmentPrefab, transform.position, Quaternion.identity);
            
            // 传递碎片的初始速度（模拟爆炸效果）
            Rigidbody2D[] fragmentRbs = fragments.GetComponentsInChildren<Rigidbody2D>();
            foreach (Rigidbody2D rb in fragmentRbs)
            {
                Vector2 direction = (rb.transform.position - transform.position).normalized;
                float force = Random.Range(1f, 3f);
                rb.AddForce(direction * force, ForceMode2D.Impulse);
            }
        }
        
        Destroy(gameObject);
    }

    public void SetMass(float newMass)
    {
        mass = newMass;
        if (rb != null)
            rb.mass = newMass;
    }
    
    public bool isDestroyedState()
    {
        return currentState == CollectibleState.Destroyed || currentState == CollectibleState.Harvested;
    }
    
    private bool IsCollectibleType()
    {
        return subType == CollectibleSubType.Resource || 
               subType == CollectibleSubType.Prop || 
               subType == CollectibleSubType.Garbage;
    }

    private bool IsObstacleType()
    {
        return subType == CollectibleSubType.CollectibleObstacle || 
               subType == CollectibleSubType.RegularObstacle;
    }
    
    private void ApplyInitialRotation()
    {
        if (rb != null && !initialRotationApplied)
        {
            float direction = Random.value < 0.5f ? -1f : 1f;
            rb.angularVelocity = initialAngularSpeed * direction;
            initialRotationApplied = true;
            if (showDebugInfo)
                Debug.Log($"{gameObject.name} 初始旋转角速度: {rb.angularVelocity}");
        }
    }
}