using UnityEngine;
using System.Collections.Generic;

public class Net : MonoBehaviour
{
    [Header("捕网属性")]
    [Tooltip("捕获范围半径")]
    public float explosionRadius = 2f;

    [Tooltip("最大飞行距离")]
    public float maxRange = 20f;

    [Header("捕网大小")]
    [Tooltip("捕网长度")]
    public float capsuleLength = 1f;

    [Tooltip("捕网半径")]
    public float capsuleRadius = 0.3f;

    private List<string> destroyableTags = new List<string> { "Obstacle", "Collectible" };

    // 胶囊体方向（水平或垂直）
    private CapsuleDirection2D capsuleDirection = CapsuleDirection2D.Horizontal;

    // 组件引用
    private Rigidbody2D rb;
    private CapsuleCollider2D netCollider;

    // 运行时参数
    private float speed;
    private Vector2 spawnPosition;
    private Vector2 fireDirection;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        netCollider = GetComponent<CapsuleCollider2D>();
        spawnPosition = transform.position;

        if (netCollider == null)
        {
            netCollider = gameObject.AddComponent<CapsuleCollider2D>();
        }

        netCollider.size = new Vector2(capsuleLength, capsuleRadius * 2);
        netCollider.direction = capsuleDirection;
        netCollider.isTrigger = true;
    }

    public void Initialize(float netSpeed, Vector2 direction)
    {
        speed = netSpeed;
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
        ProcessObjectsInRange();
        Destroy(gameObject);
    }

    private void ProcessObjectsInRange()
    {
        Collider2D[] collidersInRange = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        foreach (Collider2D collider in collidersInRange)
        {
            if (collider.CompareTag("Obstacle"))
            {
                Destroy(collider.gameObject);
            }
            else if (collider.CompareTag("Collectible"))
            {
                CollectibleObject collectible = collider.GetComponent<CollectibleObject>();
                if (collectible != null)
                {
                    collectible.OnHarvested();
                }
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