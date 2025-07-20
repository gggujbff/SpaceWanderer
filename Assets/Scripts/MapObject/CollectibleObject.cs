using UnityEngine;

public class CollectibleObject : MonoBehaviour
{
    public enum CollectibleSubType { Resource, Prop, Garbage, CollectibleObstacle }

    // 可采集属性
    [Header("可采集属性")]
    public CollectibleSubType subType;
    public int scoreValue;
    public float energyValue;
    public string propTag;
    public float mass = 1f;
    public float destroyedMomentum;
    public Vector2 velocity;

    // 状态
    /*[Header("状态")]*/
    public enum CollectibleState { FreeFloating, Colliding, Grabbed, Harvested, Destroyed }
    public CollectibleState currentState;

    private Rigidbody2D rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.mass = mass;
            rb.velocity = velocity;
            rb.gravityScale = 0f; // 无重力
        }
        
        currentState = CollectibleState.FreeFloating;
    }

    public void OnHookCollision(HookSystem hook)
    {
        if (currentState != CollectibleState.Destroyed)
        {
            currentState = CollectibleState.Colliding;
            float hookMomentum = hook.CurrentLaunchSpeed * hook.hookTipMass;
            
            if (hookMomentum >= destroyedMomentum)
            {
                DestroyObject();
            }
        }
    }

    public bool OnGrabbed(HookTipCollisionHandler hookTip)
    {
        if (currentState == CollectibleState.FreeFloating)
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
        currentState = CollectibleState.Harvested;
        
        switch (subType)
        {
            case CollectibleSubType.Resource:
                HookSystem.Instance.AddScore(scoreValue);
                HookSystem.Instance.GrabEnergy(energyValue);
                break;
            case CollectibleSubType.Prop:
                HookSystem.Instance.AddScore(scoreValue);
                if (propTag == "Energy")
                {
                    HookSystem.Instance.GrabEnergy(energyValue);
                }
                break;
            case CollectibleSubType.Garbage:
                HookSystem.Instance.AddScore(scoreValue);
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