using UnityEngine;

public class HookTipCollisionHandler : MonoBehaviour
{
    public HookSystem hookSystem;
    public float heatPerUnitMass = 1f; // 每单位质量产生的热量
    public float maxGrabbableMass = 5f; // 最大可钩取质量
    
    [Header("调试选项")]
    public bool logCollisions = true;

    private bool hasGrabbedObject = false; // 标记是否已钩取物体（确保单次只钩一个）

    // 处理触发器碰撞（针对收集物）
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 仅在Launching状态且未钩取物体时检测碰撞
        if (hookSystem.currentState != HookSystem.HookState.Launching || hasGrabbedObject)
        {
            if (logCollisions) Debug.Log($"忽略碰撞：状态不符（当前状态：{hookSystem.currentState}，已钩取：{hasGrabbedObject}）");
            return;
        }

        if (logCollisions) Debug.Log($"触发器碰撞: {other.gameObject.name} (标签: {other.tag})");
        
        if (other.CompareTag("Collectible"))
        {
            HandleCollectibleCollision(other.gameObject);
        }
        else if (other.CompareTag("Obstacle"))
        {
            HandleObstacleCollision(other.gameObject);
        }
    }

    // 处理实体碰撞（针对障碍物）
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 仅在Launching状态且未钩取物体时检测碰撞
        if (hookSystem.currentState != HookSystem.HookState.Launching || hasGrabbedObject)
        {
            if (logCollisions) Debug.Log($"忽略碰撞：状态不符（当前状态：{hookSystem.currentState}，已钩取：{hasGrabbedObject}）");
            return;
        }

        if (logCollisions) Debug.Log($"实体碰撞: {collision.gameObject.name} (标签: {collision.gameObject.tag})");
        
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            HandleObstacleCollision(collision.gameObject);
        }
    }

    // 处理收集物碰撞
    private void HandleCollectibleCollision(GameObject collectibleObj)
    {
        var collectible = collectibleObj.GetComponent<CollectibleObject>();
        if (collectible == null || 
            (collectible.currentState != CollectibleObject.CollectibleState.FreeFloating && 
             collectible.currentState != CollectibleObject.CollectibleState.Colliding))
        {
            if (logCollisions) Debug.Log($"收集物不可钩取: {collectibleObj.name} (状态: {collectible?.currentState})");
            return;
        }

        var rb = collectibleObj.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            if (logCollisions) Debug.LogError($"收集物缺少Rigidbody2D: {collectibleObj.name}");
            return;
        }

        // 检查质量限制
        if (rb.mass > maxGrabbableMass)
        {
            if (logCollisions) Debug.Log($"收集物质量 {rb.mass} 超过限制 {maxGrabbableMass}，开始收回钩爪: {collectibleObj.name}");
            // 质量过大时不钩取，但触发收回钩爪
            hookSystem.RetrieveHook();
            return;
        }

        // 标记已钩取物体，防止重复钩取
        hasGrabbedObject = true;

        // 处理钩取逻辑
        collectible.OnHookCollision(hookSystem);
        
        // 禁用收集物的碰撞体，防止干扰钩爪回收
        var collider = collectibleObj.GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = false;
        }
        
        // 设置为钩爪的子物体，保持世界位置不变
        collectibleObj.transform.SetParent(transform, true);
        
        // 禁用物理模拟
        rb.isKinematic = true;
        rb.velocity = Vector2.zero;
        
        collectible.currentState = CollectibleObject.CollectibleState.Grabbed;
        if (logCollisions) Debug.Log($"成功钩取收集物: {collectibleObj.name}");
        
        ApplyHookEffects(rb);
    }

    // 处理障碍物碰撞
    private void HandleObstacleCollision(GameObject obstacleObj)
    {
        var obstacle = obstacleObj.GetComponent<MovingObstacle>();
        if (obstacle == null)
        {
            if (logCollisions) Debug.LogError($"障碍物缺少MovingObstacle组件: {obstacleObj.name}");
            return;
        }

        var rb = obstacleObj.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            if (logCollisions) Debug.LogError($"障碍物缺少Rigidbody2D: {obstacleObj.name}");
            return;
        }

        // 检查质量限制
        if (rb.mass > maxGrabbableMass)
        {
            if (logCollisions)
            {
                //Debug.Log($"障碍物质量 {rb.mass} 超过限制 {maxGrabbableMass}，开始收回钩爪: {obstacleObj.name}");
            }
            // 质量过大时不钩取，但触发收回钩爪
            hookSystem.RetrieveHook();
            return;
        }

        // 标记已钩取物体，防止重复钩取
        hasGrabbedObject = true;

        // 处理钩取逻辑
        obstacle.transform.SetParent(transform, true); // 保持世界位置
        obstacle.SetVelocity(Vector2.zero); // 停止障碍物运动
        
        // 禁用障碍物的碰撞体
        var collider = obstacleObj.GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = false;
        }
        
        if (logCollisions) Debug.Log($"成功钩取障碍物: {obstacleObj.name}");
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

    // 回收完成后重置状态（关键：重置已钩取标记，允许下次钩取）
    public void OnRetrieveComplete()
    {
        // 处理收集到的物体
        CollectibleObject[] collectedObjects = GetComponentsInChildren<CollectibleObject>(true);
        foreach (var collectible in collectedObjects)
        {
            if (collectible != null && collectible.currentState != CollectibleObject.CollectibleState.Harvested)
            {
                collectible.OnHarvested();
            }
        }

        // 移除所有附加的障碍物
        MovingObstacle[] attachedObstacles = GetComponentsInChildren<MovingObstacle>(true);
        foreach (var obstacle in attachedObstacles)
        {
            if (obstacle != null)
            {
                Destroy(obstacle.gameObject);
            }
        }

        hasGrabbedObject = false;
        hookSystem.ResetGrabbedMass();
    }

    public void ResetGrabState()
    {
        hasGrabbedObject = false;
    }
}    