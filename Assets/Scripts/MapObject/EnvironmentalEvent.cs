using UnityEngine;

public class EnvironmentalEventManager : MonoBehaviour
{
    public static EnvironmentalEventManager Instance;

    [Header("射线风暴属性")]
    public Vector2 sourceDirection; // 来源方向（单位向量，如 Vector2.right）
    public float intensity; // 强度（影响范围和伤害）
    public float baseDamage; // 基础伤害
    public bool isBlocked; // 是否被遮挡

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        if (!isBlocked)
        {
            // 检测范围内的玩家（HookSystem挂载的对象，标签设为"Player"）
            Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, intensity * 5f);
            foreach (var col in hitColliders)
            {
                if (col.CompareTag("Player"))
                {
                    // 直接调用HookSystem的TakeDamage方法（无需PlayerHealth类）
                    float damage = baseDamage * intensity * Time.deltaTime; // 按时间计算伤害
                    HookSystem.Instance.TakeDamage(damage);
                }
            }
        }
    }

    // 检测是否被固定障碍物遮挡
    public void CheckBlocked()
    {
        // 射线检测：从事件中心向来源方向发射射线
        RaycastHit2D hit = Physics2D.Raycast(transform.position, sourceDirection, 100f);
        isBlocked = hit.collider != null && hit.collider.GetComponent<FixedObstacle>() != null;
    }
}