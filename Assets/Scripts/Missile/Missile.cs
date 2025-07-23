using UnityEngine;
using System.Collections.Generic;

public class Missile : MonoBehaviour
{
    [Header("飞弹属性")]
    [Tooltip("爆炸范围半径")]
    public float explosionRadius = 2f;

    [Tooltip("最大飞行距离")]
    public float maxRange = 20f;

    [Header("导弹大小")]
    [Tooltip("导弹长度")]
    public float capsuleLength = 1f;

    [Tooltip("导弹半径")]
    public float capsuleRadius = 0.3f;

    private List<string> destroyableTags = new List<string> { "Obstacle" , "Energy"};

    // 胶囊体方向（水平或垂直）
    private CapsuleDirection2D capsuleDirection = CapsuleDirection2D.Horizontal;

    // 组件引用
    private Rigidbody2D rb;
    private CapsuleCollider2D missileCollider;

    // 运行时参数
    private float speed;
    private Vector2 spawnPosition;
    private Vector2 fireDirection;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        missileCollider = GetComponent<CapsuleCollider2D>();
        spawnPosition = transform.position;

        if (missileCollider == null)
        {
            missileCollider = gameObject.AddComponent<CapsuleCollider2D>();
        }

        missileCollider.size = new Vector2(capsuleLength, capsuleRadius * 2);
        missileCollider.direction = capsuleDirection;
        missileCollider.isTrigger = true;
    }

    public void Initialize(float missileSpeed, Vector2 direction)
    {
        speed = missileSpeed;
        fireDirection = direction;
        rb.velocity = fireDirection * speed;
    }

    void Update()
    {
        if (Vector2.Distance(transform.position, spawnPosition) > maxRange)
        {
            Explode();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (destroyableTags.Contains(other.tag))
        {
            Explode();
        }
    }

    void Explode()
    {
        DrawExplosionRange();
        DestroyObjectsInRange();
        Destroy(gameObject);
    }

    private void DestroyObjectsInRange()
    {
        Collider2D[] collidersInRange = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        foreach (Collider2D collider in collidersInRange)
        {
            if (destroyableTags.Contains(collider.tag))
            {
                Destroy(collider.gameObject);
            }
        }
    }

    void DrawExplosionRange()
    {
        GameObject indicator = new GameObject("ExplosionRange");
        indicator.transform.position = transform.position;

        MeshRenderer renderer = indicator.AddComponent<MeshRenderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));
        renderer.material.color = new Color(1f, 0.3f, 0.3f, 0.3f);

        MeshFilter meshFilter = indicator.AddComponent<MeshFilter>();
        meshFilter.mesh = CreateCircleMesh(explosionRadius);

        int effectsLayer = LayerMask.NameToLayer("Effects");
        indicator.layer = effectsLayer != -1 ? effectsLayer : 0;

        Destroy(indicator, 1f);
    }

    private Mesh CreateCircleMesh(float radius)
    {
        Mesh mesh = new Mesh();
        const int segments = 32;
        Vector3[] vertices = new Vector3[segments + 1];
        vertices[0] = Vector3.zero;

        for (int i = 0; i < segments; i++)
        {
            float angle = (float)i / segments * 2 * Mathf.PI;
            vertices[i + 1] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius;
        }

        int[] triangles = new int[segments * 3];
        for (int i = 0; i < segments; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = (i + 2 > segments) ? 1 : i + 2;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        return mesh;
    }
}
