using UnityEngine;

public class Obstacle : MonoBehaviour
{
    [Header("基础属性")]
    [Tooltip("障碍物的大小范围")]
    public Vector2 massRange = new Vector2(1f, 5f);
    
    [Tooltip("障碍物发射的最小速度")]
    public float minSpeed = 0.5f;
    
    [Tooltip("障碍物发射的最大速度")]
    public float maxSpeed = 2f;
    
    [Tooltip("障碍物的最大旋转速度")]
    public float maxRotation = 15f;

    [Header("物理模拟")]
    [Tooltip("碰撞反弹系数：0=完全不反弹，1=完全弹性碰撞")]
    private float bounceFactor = 1f;

    [Header("生命周期")]
    [Tooltip("障碍物自动销毁的时间（防止卡住等情况长时间存在占用内存）")]
    public float autoDestroyTime = 30f;
    
    [Tooltip("是否在障碍物离开屏幕后自动销毁")]
    public bool destroyOnExitScreen = true;
    
    [Tooltip("障碍物离开屏幕后延迟销毁的时间（秒）")]
    public float destroyDelayAfterExit = 2f;

    [HideInInspector] public float currentMass;
    private float sizePerMass = 0.3f;
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private bool isOffScreen = false;
    private float offScreenTimer = 0f;
    private float lifeTimer = 0f;
    private bool debugLifetime = false;  // 调试生命周期 
    public bool debugCollisions = false;  //启用后，将在控制台打印碰撞信息并在场景中显示碰撞法线

    
    
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        
        if (Mathf.Approximately(currentMass, 0))
        {
            InitMassAndSize(Random.Range(massRange.x, massRange.y));
        }
        
        InitPhysics();
    }

    void Start()
    {
        if (autoDestroyTime > 0)
        {
            lifeTimer = autoDestroyTime;
            if (debugLifetime) Debug.Log($"[{name}] 初始化，将在 {autoDestroyTime} 秒后销毁");
        }
    }

    void Update()
    {
        if (lifeTimer > 0)
        {
            lifeTimer -= Time.deltaTime;
            if (lifeTimer <= 0)
            {
                if (debugLifetime) Debug.Log($"[{name}] 生命周期结束，自动销毁");
                DestroyObstacle();
                return;
            }
        }

        if (destroyOnExitScreen)
        {
            CheckScreenBounds();
        }
    }

    private void CheckScreenBounds()    // 检查是否离开屏幕边界
    {
        Vector3 screenPoint = Camera.main.WorldToViewportPoint(transform.position);
        bool isCurrentlyOffScreen = 
            screenPoint.x < -0.1f || screenPoint.x > 1.1f ||
            screenPoint.y < -0.1f || screenPoint.y > 1.1f;

        if (isCurrentlyOffScreen)
        {
            if (!isOffScreen)
            {
                isOffScreen = true;
                offScreenTimer = 0f;
                if (debugLifetime) Debug.Log($"[{name}] 离开屏幕，开始计时销毁");
            }
            else
            {
                offScreenTimer += Time.deltaTime;
                if (offScreenTimer >= destroyDelayAfterExit)
                {
                    if (debugLifetime) Debug.Log($"[{name}] 在屏幕外超过 {destroyDelayAfterExit} 秒，销毁");
                    DestroyObstacle();
                }
            }
        }
        else
        {
            if (isOffScreen && debugLifetime)
            {
                Debug.Log($"[{name}] 回到屏幕内，重置销毁计时器");
            }
                
            isOffScreen = false;
            offScreenTimer = 0f;
        }
    }

    public void InitMassAndSize(float mass)    // 初始化质量和大小
    {
        currentMass = Mathf.Clamp(mass, massRange.x, massRange.y);
        float scale = currentMass * sizePerMass;
        transform.localScale = new Vector3(scale, scale, 1f);

        // 自动适配所有类型的碰撞体
        AdjustColliderSize(scale);
    }

    private void AdjustColliderSize(float scale)    // 调整碰撞体大小（支持多种类型）
    {
        // 支持CircleCollider2D
        CircleCollider2D circleCol = GetComponent<CircleCollider2D>();
        if (circleCol != null)
        {
            circleCol.radius = 0.5f * scale; // 假设原始半径为0.5
        }

        // 支持BoxCollider2D
        BoxCollider2D boxCol = GetComponent<BoxCollider2D>();
        if (boxCol != null)
        {
            boxCol.size = Vector2.one * scale; // 假设原始大小为1x1
        }

        // 支持PolygonCollider2D
        PolygonCollider2D polyCol = GetComponent<PolygonCollider2D>();
        if (polyCol != null)
        {
            // 多边形碰撞体需要特殊处理（缩放顶点）
            Vector2[] originalPoints = polyCol.points;
            Vector2[] scaledPoints = new Vector2[originalPoints.Length];
            
            for (int i = 0; i < originalPoints.Length; i++)
            {
                scaledPoints[i] = originalPoints[i] * scale;
            }
            
            polyCol.points = scaledPoints;
        }
    }

    private void InitPhysics()    // 初始化物理属性
    {
        rb.mass = currentMass;
        rb.drag = 0;
        rb.angularDrag = 0.1f;
        rb.gravityScale = 0;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    public void SetInitialMovement(Vector2 velocity, float angularVelocity)    // 设置初始移动
    {
        rb.velocity = velocity;
        rb.angularVelocity = angularVelocity;
    }

    private void OnCollisionEnter2D(Collision2D collision)    // 碰撞处理
    {
        if (debugCollisions)
        {
            Debug.Log($"[{name}]（质量：{currentMass}）与 [{collision.gameObject.name}] 碰撞");
            foreach (var contact in collision.contacts)
            {
                Debug.DrawRay(contact.point, contact.normal * 0.5f, Color.red, 2f);
            }
        }

        ApplySpaceCollision(collision);
    }

    private void ApplySpaceCollision(Collision2D collision)    // 太空物理碰撞计算
    {
        Rigidbody2D otherRb = collision.rigidbody;
        if (otherRb == null) return;

        Obstacle otherObstacle = collision.gameObject.GetComponent<Obstacle>();
        if (otherObstacle == null) return;

        Vector2 normal = collision.contacts[0].normal;
        Vector2 relativeVelocity = rb.velocity - otherRb.velocity;

        float impulseMagnitude = 
            -(1 + bounceFactor) * Vector2.Dot(relativeVelocity, normal) / 
            (1 / currentMass + 1 / otherObstacle.currentMass);

        Vector2 impulse = normal * impulseMagnitude;
        rb.AddForce(impulse, ForceMode2D.Impulse);
        otherRb.AddForce(-impulse, ForceMode2D.Impulse);
    }

    public virtual void DestroyObstacle()    // 销毁障碍物
    {
        Destroy(gameObject);
    }
}