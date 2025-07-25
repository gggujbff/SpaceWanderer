using UnityEngine;
using System.Collections.Generic;

public class HookTipCollisionHandler : MonoBehaviour
{
    public HookSystem hookSystem;
    public float heatPerUnitMass = 1f; // 此参数已无用，可保留但不调用
    public float maxGrabbableMass = 5f;
    
    [Header("调试选项")]
    public bool logCollisions = true;

    private bool hasGrabbedObject = false;
    private List<GameObject> grabbedObjectsList = new List<GameObject>();

    private void OnTriggerEnter2D(Collider2D other)
    {
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
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (hookSystem.currentState != HookSystem.HookState.Launching || hasGrabbedObject)
        {
            if (logCollisions) Debug.Log($"忽略碰撞：状态不符（当前状态：{hookSystem.currentState}，已钩取：{hasGrabbedObject}）");
            return;
        }

        if (logCollisions) Debug.Log($"实体碰撞: {collision.gameObject.name} (标签: {collision.gameObject.tag})");
        
        if (collision.gameObject.CompareTag("Collectible"))
        {
            HandleCollectibleCollision(collision.gameObject);
        }
    }

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

        if (rb.mass > maxGrabbableMass)
        {
            if (logCollisions) Debug.Log($"物体质量 {rb.mass} 超过限制 {maxGrabbableMass}，开始收回钩爪: {targetObj.name}");
            hookSystem.RetrieveHook();
            return;
        }

        hasGrabbedObject = true;
        collectible.OnHookCollision(hookSystem);
        
        bool grabSuccess = collectible.OnGrabbed(this);
        if (!grabSuccess)
        {
            if (logCollisions) Debug.LogWarning($"抓取失败: {targetObj.name}");
            hookSystem.RetrieveHook();
            return;
        }
        
        grabbedObjectsList.Add(targetObj);
        
        var collider = targetObj.GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = false;
        }
        
        if (logCollisions) Debug.Log($"成功钩取物体: {targetObj.name} (类型: {collectible.subType})");
        
        ApplyHookEffects(rb);
    }

    private void ApplyHookEffects(Rigidbody2D rb)
    {
        // 核心修改：删除钩取时的瞬时生热代码
        // float heatGenerated = rb.mass * heatPerUnitMass;
        // hookSystem.AddHeat(heatGenerated);
        
        // 保留质量添加（用于持续生热计算）
        hookSystem.AddGrabbedMass(rb.mass);
        hookSystem.RetrieveHook();
    }

    public void OnRetrieveComplete()
    {
        CollectibleObject[] collectedObjects = GetComponentsInChildren<CollectibleObject>(true);
        foreach (var collectible in collectedObjects)
        {
            if (collectible != null && collectible.currentState != CollectibleObject.CollectibleState.Harvested)
            {
                collectible.OnHarvested();
            }
        }

        hasGrabbedObject = false;
        hookSystem.ResetGrabbedMass();
        grabbedObjectsList.Clear();
    }

    public void ResetGrabState()
    {
        hasGrabbedObject = false;
    }

    private bool isInvalidState(CollectibleObject.CollectibleState state)
    {
        return state == CollectibleObject.CollectibleState.Destroyed || 
               state == CollectibleObject.CollectibleState.Harvested || 
               state == CollectibleObject.CollectibleState.Grabbed;
    }

    public void ReleaseAllGrabbedObjects()
    {
        if (logCollisions) Debug.Log("释放所有抓取的物体");
        
        foreach (var grabbedObject in new List<GameObject>(grabbedObjectsList))
        {
            if (grabbedObject != null)
            {
                var collider = grabbedObject.GetComponent<Collider2D>();
                if (collider != null)
                {
                    collider.enabled = true;
                }
                
                var collectible = grabbedObject.GetComponent<CollectibleObject>();
                if (collectible != null)
                {
                    collectible.OnReleased();
                }
                
                grabbedObjectsList.Remove(grabbedObject);
            }
        }
        
        hasGrabbedObject = false;
    }
}