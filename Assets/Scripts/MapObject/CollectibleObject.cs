using UnityEngine;

public class CollectibleObject : MonoBehaviour
{
    public enum CollectibleSubType { Resource, Prop, Garbage, CollectibleObstacle }
    public enum CollectibleState { FreeFloating, AttachedToObstacle, Grabbed, Colliding, Destroyed, Harvested }

    [Header("可采集属性")]
    public CollectibleSubType subType;
    
    [Tooltip("可收集的分数值")]
    public int scoreValue = 10; // 默认值设为10
    
    [Tooltip("可收集的能量值")]
    public float energyValue = 5f; // 默认值设为5
    public string propTag;
    public float mass = 1f;
    public float destroyedMomentum = 10f; // 添加默认值
    public Vector2 velocity;
    public CollectibleState currentState;

    private Rigidbody2D rb;
    private bool pendingDestroy = false; // 延迟销毁标志

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

    public void OnHookCollision(HookSystem hook)
    {
        if (currentState != CollectibleState.Destroyed && currentState != CollectibleState.Grabbed)
        {
            currentState = CollectibleState.Colliding;
            float hookMomentum = hook.CurrentLaunchSpeed * hook.hookTipMass;

            if (hookMomentum >= destroyedMomentum)
            {
                // 标记为延迟销毁，回收完成时销毁
                pendingDestroy = true;
            }
        }
    }

    public bool OnGrabbed(HookTipCollisionHandler hookTip)
    {
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
        if (currentState == CollectibleState.Harvested || currentState == CollectibleState.Destroyed)
            return;

        // 修复：即使物体被破坏，也先计算分数和能量再销毁
        if (pendingDestroy)
        {
            // 计算破坏时的分数和能量（可根据需求调整比例）
            int destroyScore = Mathf.Max(1, scoreValue); // 至少1分
            float destroyEnergy = Mathf.Max(0.1f, energyValue); // 至少0.1能量
            
            HookSystem.Instance.AddScore(destroyScore);
            HookSystem.Instance.GrabEnergy(destroyEnergy);
            
            DestroyObject();
            return;
        }

        currentState = CollectibleState.Harvested;

        switch (subType)
        {
            case CollectibleSubType.Resource:
                HookSystem.Instance.AddScore(scoreValue);
                HookSystem.Instance.GrabEnergy(energyValue);
                Debug.Log($"收集资源：加分{scoreValue}，加能量{energyValue}");
                break;
            case CollectibleSubType.Prop:
                HookSystem.Instance.AddScore(scoreValue);
                Debug.Log($"收集道具：加分{scoreValue}");
                if (propTag == "Energy")
                {
                    HookSystem.Instance.GrabEnergy(energyValue);
                    Debug.Log($"道具提供能量：{energyValue}");
                }
                break;
            case CollectibleSubType.Garbage:
                HookSystem.Instance.AddScore(scoreValue);
                Debug.Log($"收集垃圾：加分{scoreValue}");
                break;
            case CollectibleSubType.CollectibleObstacle:
                HookSystem.Instance.AddScore(scoreValue / 2); // 障碍物分数减半
                Debug.Log($"破坏障碍物：加分{scoreValue/2}");
                break;
        }

        Destroy(gameObject);
    }

    public void DestroyObject()
    {
        currentState = CollectibleState.Destroyed;
        Destroy(gameObject);
    }
}