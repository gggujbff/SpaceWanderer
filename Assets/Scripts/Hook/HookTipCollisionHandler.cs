using UnityEngine;

public class HookTipCollisionHandler : MonoBehaviour
{
    public HookSystem hookSystem;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 处理与可采集物体的碰撞
        if (other.CompareTag("Collectible"))
        {
            if (other.TryGetComponent<CollectibleObject>(out var collectible))
            {
                collectible.OnHookCollision(hookSystem);
                
                if (collectible.OnGrabbed(this))
                {
                    hookSystem.RetrieveHook();
                }
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 处理与障碍物的碰撞
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            if (collision.gameObject.TryGetComponent<MovingObstacle>(out var obstacle))
            {
                float hookMomentum = hookSystem.CurrentLaunchSpeed * hookSystem.hookTipMass;
                
                // 障碍物受到伤害
                obstacle.TakeDamage(hookMomentum);
                
                // 根据碰撞力度决定是否回收钩爪
                if (hookMomentum >= obstacle.destroyedMomentum * 0.7f)
                {
                    hookSystem.RetrieveHook();
                }
            }
        }
    }

    // 当钩爪回收完成时调用
    public void OnRetrieveComplete()
    {
        // 查找所有已抓取的物体并处理收获
        CollectibleObject[] collectibles = GetComponentsInChildren<CollectibleObject>();
        foreach (var collectible in collectibles)
        {
            collectible.OnHarvested();
        }
    }
}