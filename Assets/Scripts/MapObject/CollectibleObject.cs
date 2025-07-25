using UnityEngine;
using System.Collections;

public class CollectibleObject : MonoBehaviour
{
    public enum CollectibleSubType { Resource, Prop, Garbage, CollectibleObstacle, RegularObstacle }
    public enum CollectibleState { FreeFloating, AttachedToObstacle, Grabbed, Colliding, Destroyed, Harvested }

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
    
    [Header("初始速度设置")]
    [Tooltip("初始速度大小（由Spawner控制）")]
    public float initialSpeed = 0f;
    
    [Tooltip("初始速度方向（由Spawner控制）")]
    public Vector2 initialDirection = Vector2.right;
    
    [Header("初始旋转设置")]
    [Tooltip("初始旋转角速度（正负方向随机）")]
    public float initialAngularSpeed = 0f;
    private bool initialRotationApplied = false;

    [Tooltip("销毁所需的最小相对动量（碰撞瞬间检测）")]
    public float destroyedMomentum = 10f;

    [Header("碰撞伤害设置")]
    [Range(0.1f, 2f)] public float damageCoefficient = 0.5f;

    [Header("障碍物属性（仅对障碍物类型生效）")]
    [Range(0f, 1f)] public float restitution = 0.6f;
    [Range(0f, 1f)] public float friction = 0.3f;
    public GameObject fragmentPrefab;

    [HideInInspector] public MissileLauncher missileLauncher;
    [HideInInspector] public LaserWeapon laserWeapon;
    
    private Rigidbody2D rb;
    private bool hasCollidedWithPlayer = false;
    private MissileLauncher playerMissileLauncher;
    private LaserWeapon playerLaserWeapon;
    private NetLauncher playerNetLauncher;
    private bool initialVelocityApplied = false;
    private bool velocitySetBySpawner = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
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

    private void ApplyInitialVelocity()
    {
        if (rb != null && !initialVelocityApplied)
        {
            Vector2 finalVelocity = initialDirection.normalized * initialSpeed;
            rb.velocity = finalVelocity;
            initialVelocityApplied = true;
            Debug.Log($"{gameObject.name} 初始速度: {finalVelocity}");
        }
    }

    public void SetInitialVelocity(float speed, Vector2 direction)
    {
        initialSpeed = speed;
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
            Debug.Log($"[{Time.time}] 延迟应用速度: {finalVelocity} (物体: {gameObject.name})");
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDestroyedState()) return;

        Debug.Log($"=== 碰撞发生 ===");
        Debug.Log($"碰撞物体1: {gameObject.name} (Tag: {gameObject.tag})");
        Debug.Log($"碰撞物体2: {collision.gameObject.name} (Tag: {collision.gameObject.tag})");

        if (collision.gameObject.CompareTag("Player") && !hasCollidedWithPlayer)
        {
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
            Debug.Log($"{gameObject.name} 碰撞钩爪");
            currentState = CollectibleState.Colliding;
        }
        else if (collision.gameObject.CompareTag("Collectible"))
        {
            Debug.Log($"{gameObject.name} 碰撞Collectible物体: {collision.gameObject.name}");
            if (collision.gameObject.TryGetComponent<CollectibleObject>(out CollectibleObject otherCollectible))
            {
                ApplyPhysicsCollision(collision);
                CheckCollisionMomentum(otherCollectible, collision);
            }
            else
            {
                Debug.LogError($"{collision.gameObject.name} 是'Collectible' Tag但未挂载CollectibleObject组件！");
            }
        }
        else
        {
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

    private void CheckCollisionMomentum(CollectibleObject other, Collision2D collision)
    {
        if (rb == null || other.rb == null)
        {
            Debug.LogError("缺少Rigidbody2D组件，无法计算动量");
            return;
        }

        float relativeSpeed = collision.relativeVelocity.magnitude;
        float averageMass = (mass + other.mass) / 2f;
        float effectiveMomentum = averageMass * relativeSpeed;

        Debug.Log($"--- 动量计算 ---");
        Debug.Log($"{gameObject.name} 速度: {rb.velocity.magnitude:F2}, 质量: {mass}");
        Debug.Log($"{other.gameObject.name} 速度: {other.rb.velocity.magnitude:F2}, 质量: {other.mass}");
        Debug.Log($"相对速度: {relativeSpeed:F2}, 平均质量: {averageMass:F2}");
        Debug.Log($"有效动量: {effectiveMomentum:F2}, 阈值: {destroyedMomentum:F2}");

        if (effectiveMomentum >= destroyedMomentum)
        {
            Debug.Log($">>> 动量达标！{gameObject.name} 立即销毁 <<<");
            DestroyObject();
        }
    }

    private void CalculateAndApplyPlayerDamage(Collision2D collision)
    {
        float relativeSpeed = collision.relativeVelocity.magnitude;
        float momentum = mass * relativeSpeed;
        float damage = momentum * damageCoefficient;
        Debug.Log($"{gameObject.name} 对玩家造成伤害: {damage:F2}");
        HookSystem.Instance.TakeDamage(damage);
    }

    private void ApplyPhysicsCollision(Collision2D collision)
    {
        Rigidbody2D otherRb = collision.rigidbody;
        if (rb == null || otherRb == null || isDestroyedState()) return;

        ContactPoint2D contact = collision.contacts[0];
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

    private void ApplyAntiStickForce(Rigidbody2D rb1, Rigidbody2D rb2)
    {
        Vector2 randomDir = Random.insideUnitCircle.normalized * 0.02f;
        rb1?.AddForce(randomDir, ForceMode2D.Impulse);
        rb2?.AddForce(-randomDir, ForceMode2D.Impulse);
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

    // 新增：释放物体时调用
// 在 CollectibleObject.cs 的 OnReleased 方法中修改
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
        Debug.Log($"{gameObject.name} 执行销毁");
        
        if (IsObstacleType() && fragmentPrefab != null)
        {
            Instantiate(fragmentPrefab, transform.position, Quaternion.identity);
        }
        
        Destroy(gameObject);
    }

    public void SetMass(float newMass)
    {
        mass = newMass;
        if (rb != null)
            rb.mass = newMass;
    }
    
    private bool isDestroyedState()
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
            Debug.Log($"{gameObject.name} 初始旋转角速度: {rb.angularVelocity}");
        }
    }
}