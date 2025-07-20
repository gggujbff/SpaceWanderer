using UnityEngine;

public class HookTipCollisionHandler : MonoBehaviour
{
    public HookSystem hookSystem;

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
                    hookSystem.RetrieveHook();
                }
            }
        }
    }

    public void OnRetrieveComplete()
    {
        // 使用GetComponentsInChildren确保找到所有子物体（包括嵌套层级）
        CollectibleObject[] collectedObjects = GetComponentsInChildren<CollectibleObject>(true);
        foreach (var collectible in collectedObjects)
        {
            if (collectible != null && collectible.currentState != CollectibleObject.CollectibleState.Harvested)
            {
                Debug.Log($"回收物 {collectible.name}，类型：{collectible.subType}");
                collectible.OnHarvested();
            }
        }
    }
}