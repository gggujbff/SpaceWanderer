using UnityEngine;
using System.Collections.Generic;

public class ObjectSpawner : MonoBehaviour
{
    public static ObjectSpawner Instance;

    [Header("全局配置")]
    public Rect spawnArea = new Rect(-10f, -10f, 20f, 20f);
    public int maxObjects = 15;
    public bool autoSpawn = true;
    public bool showSpawnArea = true;
    public Color areaColor = Color.green;

    [Header("生成项配置")]
    [SerializeField] private SpawnableItem[] spawnableItems;

    private float[] nextSpawnTimes;
    private int currentObjectCount = 0;

    [System.Serializable]
    public class SpawnableItem
    {
        public GameObject prefab;
        [Min(0.01f)] public float spawnFrequency = 0.5f;

        public bool isMoving = true;
        public float minSpeed = 1f;
        public float maxSpeed = 3f;

        // ✅ 新增是否使用随机质量选项
        public bool useRandomMass = true;
        public float fixedMass = 1f; // 固定质量值
        public float minMass = 1f;   // 随机质量最小值
        public float maxMass = 3f;   // 随机质量最大值
    }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        InitializeSpawnTimes();
        enabled = autoSpawn;
    }

    private void InitializeSpawnTimes()
    {
        if (spawnableItems == null)
        {
            spawnableItems = new SpawnableItem[0];
            return;
        }

        nextSpawnTimes = new float[spawnableItems.Length];
        for (int i = 0; i < spawnableItems.Length; i++)
        {
            float spawnInterval = 1f / spawnableItems[i].spawnFrequency;
            nextSpawnTimes[i] = Time.time + spawnInterval;
        }
    }

    private void Update()
    {
        if (currentObjectCount >= maxObjects) return;

        for (int i = 0; i < spawnableItems.Length; i++)
        {
            var item = spawnableItems[i];

            if (item.prefab == null || currentObjectCount >= maxObjects)
                continue;

            float spawnInterval = 1f / item.spawnFrequency;
            if (Time.time >= nextSpawnTimes[i])
            {
                SpawnObject(item);
                nextSpawnTimes[i] = Time.time + spawnInterval;
            }
        }
    }

    private GameObject SpawnObject(SpawnableItem item)
    {
        Vector2 spawnPosition = GetRandomSpawnPosition();
        GameObject spawnedObject = Instantiate(item.prefab, spawnPosition, Quaternion.identity);

        if (spawnedObject.TryGetComponent<MovingObstacle>(out var obstacle))
        {
            float randomSpeed = item.isMoving ? Random.Range(item.minSpeed, item.maxSpeed) : 0f;
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            obstacle.SetVelocity(randomDir * randomSpeed);

            // ✅ 根据配置设置质量
            float mass = item.useRandomMass 
                ? Random.Range(item.minMass, item.maxMass) 
                : item.fixedMass;
            obstacle.SetMass(mass);
        }

        if (spawnedObject.TryGetComponent<CollectibleObject>(out var collectible))
        {
            float randomSpeed = item.isMoving ? Random.Range(item.minSpeed, item.maxSpeed) : 0f;
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            collectible.SetVelocity(randomDir * randomSpeed);

            // ✅ 根据配置设置质量
            float mass = item.useRandomMass 
                ? Random.Range(item.minMass, item.maxMass) 
                : item.fixedMass;
            collectible.SetMass(mass);
        }

        spawnedObject.transform.SetParent(transform);
        return spawnedObject;
    }

    private Vector2 GetRandomSpawnPosition()
    {
        float x = Random.Range(spawnArea.xMin, spawnArea.xMax);
        float y = Random.Range(spawnArea.yMin, spawnArea.yMax);
        return new Vector2(x, y);
    }

    private void OnDrawGizmos()
    {
        if (!showSpawnArea) return;

        Gizmos.color = areaColor;
        Vector3 bottomLeft = new Vector3(spawnArea.xMin, spawnArea.yMin, 0);
        Vector3 bottomRight = new Vector3(spawnArea.xMax, spawnArea.yMin, 0);
        Vector3 topRight = new Vector3(spawnArea.xMax, spawnArea.yMax, 0);
        Vector3 topLeft = new Vector3(spawnArea.xMin, spawnArea.yMax, 0);

        Gizmos.DrawLine(bottomLeft, bottomRight);
        Gizmos.DrawLine(bottomRight, topRight);
        Gizmos.DrawLine(topRight, topLeft);
        Gizmos.DrawLine(topLeft, bottomLeft);
    }
}