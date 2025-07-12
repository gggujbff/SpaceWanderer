using UnityEngine;

public class Obstacle : MonoBehaviour
{
    [Header("基础属性")]
    [Tooltip("质量范围（kg），同时影响大小缩放")]
    public Vector2 massRange = new Vector2(1f, 5f);
    [Tooltip("质量与大小的缩放比例（单位：米/kg）")]
    public float sizePerMass = 0.3f;

    [Header("移动参数")]
    [Range(0.1f, 3f)] public float minSpeed = 0.5f;
    [Range(1f, 5f)] public float maxSpeed = 2f;
    [Range(-30f, 30f)] public float maxRotation = 15f;

    [Header("物理参数")]
    [Range(0.1f, 1.5f)] public float bounceFactor = 0.8f;

    [Header("调试")]
    public bool debugCollisions = false;

    [HideInInspector] public float currentMass;
    private Rigidbody2D rb;
    private SpriteRenderer sr;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        
        // 若未通过发射器初始化，则使用随机质量
        if (Mathf.Approximately(currentMass, 0))
        {
            InitMassAndSize(Random.Range(massRange.x, massRange.y));
        }
        
        InitPhysics();
    }

    // 初始化质量和大小
    public void InitMassAndSize(float mass)
    {
        currentMass = Mathf.Clamp(mass, massRange.x, massRange.y);
        float scale = currentMass * sizePerMass;
        transform.localScale = new Vector3(scale, scale, 1f);

        // 自动适配所有类型的碰撞体
        AdjustColliderSize(scale);
    }

    // 调整碰撞体大小（支持多种类型）
    private void AdjustColliderSize(float scale)
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

    // 初始化物理属性
    private void InitPhysics()
    {
        rb.mass = currentMass;
        rb.drag = 0;
        rb.angularDrag = 0.1f;
        rb.gravityScale = 0;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    // 设置初始移动
    public void SetInitialMovement(Vector2 velocity, float angularVelocity)
    {
        rb.velocity = velocity;
        rb.angularVelocity = angularVelocity;
    }

    // 碰撞处理
    private void OnCollisionEnter2D(Collision2D collision)
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

    // 太空物理碰撞计算
    private void ApplySpaceCollision(Collision2D collision)
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
}