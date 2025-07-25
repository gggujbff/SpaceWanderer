using UnityEngine;

public class HookTipCollisionHandler : MonoBehaviour
{
    public HookSystem hookSystem;
    public float heatPerUnitMass = 1f;
    public float maxGrabbableMass = 5f;
    
    [Header("调试选项")]
    public bool logCollisions = true;

    private bool hasGrabbedObject = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 仅在Launching状态且未钩取物体时检测碰撞
        if (hookSystem.currentState != HookSystem.HookState.Launching || hasGrabbedObject)
        {
            if (logCollisions) Debug.Log($"忽略碰撞：状态不符（当前状态：{hookSystem.currentState}，已钩取：{hasGrabbedObject}）");
            return;
        }

        if (logCollisions) Debug.Log($"触发器碰撞: {other.gameObject.name} (标签: {other.tag})");
        
        // 统一处理所有可抓取物（标签为Collectible）
        if (other.CompareTag("Collectible"))
        {
            HandleCollectibleCollision(other.gameObject);
        }
    }

    // 处理实体碰撞（针对带碰撞体的可抓取物）
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 仅在Launching状态且未钩取物体时检测碰撞
        if (hookSystem.currentState != HookSystem.HookState.Launching || hasGrabbedObject)
        {
            if (logCollisions) Debug.Log($"忽略碰撞：状态不符（当前状态：{hookSystem.currentState}，已钩取：{hasGrabbedObject}）");
            return;
        }

        if (logCollisions) Debug.Log($"实体碰撞: {collision.gameObject.name} (标签: {collision.gameObject.tag})");
        
        // 统一处理所有可抓取物（标签为Collectible）
        if (collision.gameObject.CompareTag("Collectible"))
        {
            HandleCollectibleCollision(collision.gameObject);
        }
    }

    // 统一处理所有可抓取物（包括原Collectible和Obstacle）
    private void HandleCollectibleCollision(GameObject targetObj)
    {
        var collectible = targetObj.GetComponent<CollectibleObject>();
        if (collectible == null || isInvalidState(collectible.currentState))
        {
            if (logCollisions) Debug.Log($"物体不可钩取: {targetObj.name} (状态: {collectible?.currentState})");
            return;
        }

        var rb = targetObj.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            if (logCollisions) Debug.LogError($"物体缺少Rigidbody2D: {targetObj.name}");
            return;
        }

        // 检查质量限制
        if (rb.mass > maxGrabbableMass)
        {
            if (logCollisions) Debug.Log($"物体质量 {rb.mass} 超过限制 {maxGrabbableMass}，开始收回钩爪: {targetObj.name}");
            hookSystem.RetrieveHook();
            return;
        }

        // 标记已钩取物体，防止重复钩取
        hasGrabbedObject = true;

        // 处理钩取逻辑（调用CollectibleObject的OnHookCollision）
        collectible.OnHookCollision(hookSystem);
        
        // 尝试通过OnGrabbed方法确认抓取（兼容原有接口）
        bool grabSuccess = collectible.OnGrabbed(this);
        if (!grabSuccess)
        {
            if (logCollisions) Debug.LogWarning($"抓取失败: {targetObj.name}");
            hookSystem.RetrieveHook();
            return;
        }
        
        // 禁用物体的碰撞体，防止干扰钩爪回收
        var collider = targetObj.GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = false;
        }
        
        if (logCollisions) Debug.Log($"成功钩取物体: {targetObj.name} (类型: {collectible.subType})");
        
        ApplyHookEffects(rb);
    }

    // 应用钩取效果（热量、质量等）
    private void ApplyHookEffects(Rigidbody2D rb)
    {
        float heatGenerated = rb.mass * heatPerUnitMass;
        hookSystem.AddHeat(heatGenerated);
        hookSystem.AddGrabbedMass(rb.mass);
        hookSystem.RetrieveHook();
    }

    // 回收完成后重置状态
    public void OnRetrieveComplete()
    {
        // 处理所有子物体中的可收集物
        CollectibleObject[] collectedObjects = GetComponentsInChildren<CollectibleObject>(true);
        foreach (var collectible in collectedObjects)
        {
            if (collectible != null && collectible.currentState != CollectibleObject.CollectibleState.Harvested)
            {
                collectible.OnHarvested(); // 触发收集逻辑（计分、道具增加等）
            }
        }

        hasGrabbedObject = false;
        hookSystem.ResetGrabbedMass();
    }

    public void ResetGrabState()
    {
        hasGrabbedObject = false;
    }

    // 判断物体状态是否允许被抓取
    private bool isInvalidState(CollectibleObject.CollectibleState state)
    {
        return state == CollectibleObject.CollectibleState.Destroyed || 
               state == CollectibleObject.CollectibleState.Harvested || 
               state == CollectibleObject.CollectibleState.Grabbed;
    }
}