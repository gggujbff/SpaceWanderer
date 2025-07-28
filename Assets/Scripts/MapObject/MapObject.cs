using UnityEngine;

public class MapObject : MonoBehaviour
{
    [Header("基础属性")]
    public MapObjectType objectType; // 物体类型
    public Vector2 centerOfMass; // 质心（默认图像中心）
    protected Collider2D objectCollider; // 图像判定范围（碰撞体）
    protected SpriteRenderer spriteRenderer; // 用于获取图像范围

    protected virtual void Awake()
    {
        // 初始化碰撞体和质心
        objectCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            centerOfMass = spriteRenderer.bounds.center; // 质心设为图像中心
    }

    // 物体被钩爪碰撞时的处理（由子类实现）
    public virtual void OnHookCollision(HookSystem hook) { }

    // 物体被钩爪抓取时的处理（仅可采集物体实现）
    public virtual bool OnGrabbed(HookTipCollisionHandler hookTip) { return false; }
}