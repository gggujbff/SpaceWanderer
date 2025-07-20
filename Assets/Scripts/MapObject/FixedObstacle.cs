using UnityEngine;

public class FixedObstacle : MapObject
{
    protected override void Awake()
    {
        base.Awake();
        objectType = MapObjectType.FixedObstacle;
        // 固定状态：禁用运动
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Static;
        }
    }

    // 被钩爪碰撞时：阻止钩爪前进，强制回收
    public override void OnHookCollision(HookSystem hook)
    {
        hook.RetrieveHook(); // 触发钩爪回收
    }
}