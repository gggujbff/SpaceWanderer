using UnityEngine;

public class HookTipCollisionHandler : MonoBehaviour
{
    public HookSystem hookSystem;
    public float heatPerUnitMass = 1f; // 每单位质量产生的热量

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Collectible"))
        {
            if (other.TryGetComponent<CollectibleObject>(out var collectible))
            {
                if (collectible.currentState == CollectibleObject.CollectibleState.FreeFloating ||
                    collectible.currentState == CollectibleObject.CollectibleState.Colliding)
                {
                    collectible.OnHookCollision(hookSystem);

                    if (collectible.OnGrabbed(this))
                    {
                        // 根据收集物质量增加温度
                        if (collectible.TryGetComponent<Rigidbody2D>(out var rb))
                        {
                            float heatGenerated = rb.mass * heatPerUnitMass;
                            //hookSystem.currentTemperature += heatGenerated;
                            hookSystem.AddGrabbedMass(rb.mass); // 添加物体质量
                        }

                        hookSystem.RetrieveHook();
                    }
                }
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            if (collision.gameObject.TryGetComponent<MovingObstacle>(out var obstacle))
            {
                float hookMomentum = hookSystem.CurrentLaunchSpeed * hookSystem.hookTipMass;

                obstacle.TakeDamage(hookMomentum);

                if (hookMomentum >= obstacle.destroyedMomentum * 0.7f)
                {
                    // 根据障碍物质量增加温度
                    if (obstacle.TryGetComponent<Rigidbody2D>(out var rb))
                    {
                        float heatGenerated = rb.mass * heatPerUnitMass;
                        hookSystem.currentTemperature += heatGenerated;
                        hookSystem.AddGrabbedMass(rb.mass); // 添加物体质量
                    }

                    hookSystem.RetrieveHook();
                }
            }
        }
    }

    public void OnRetrieveComplete()
    {
        CollectibleObject[] collectedObjects = GetComponentsInChildren<CollectibleObject>(true);
        foreach (var collectible in collectedObjects)
        {
            if (collectible != null && collectible.currentState != CollectibleObject.CollectibleState.Harvested)
            {
                Debug.Log($"回收完成：处理收集物 {collectible.name}，类型：{collectible.subType}");
                collectible.OnHarvested();
            }
        }
        hookSystem.ResetGrabbedMass(); // 回收完成后重置质量
    }
}