using UnityEngine;

public class InterferenceObject : MapObject
{
    [Header("干扰属性")]
    public float interferenceAmount; // 干扰量（0-1）
    public Vector2 velocity;
    public Vector2 direction;

    private SpriteRenderer sr;

    protected override void Awake()
    {
        base.Awake();
        objectType = MapObjectType.Interference;
        sr = GetComponent<SpriteRenderer>();
        // 干扰量与alpha关联
        Color color = sr.color;
        color.a = interferenceAmount;
        sr.color = color;
        // 自由悬浮状态
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = velocity;
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        
    }
}