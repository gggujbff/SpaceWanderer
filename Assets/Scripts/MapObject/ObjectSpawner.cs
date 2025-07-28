using UnityEngine;
using System.Collections.Generic;

public class ObjectSpawner : MonoBehaviour
{
    [Header("全局配置")]
    public bool autoSpawn = true;
    public bool showSpawnAreas = true;

    [Header("生成区域配置")]
    public List<SpawnZone> spawnZones = new List<SpawnZone>();

    private void Start()
    {
        InitializeAllZones();
        enabled = autoSpawn;
    }

    private void InitializeAllZones()
    {
        foreach (var zone in spawnZones)
            zone.Initialize();
    }

    private void Update()
    {
        foreach (var zone in spawnZones)
        {
            if (zone.CanSpawn() && zone.ShouldSpawn())
                SpawnObjectInZone(zone);
        }
    }

    private void SpawnObjectInZone(SpawnZone zone)
    {
        SpawnableItem itemToSpawn = zone.SelectRandomItem();
        if (itemToSpawn == null || itemToSpawn.prefab == null) return;

        Vector2 spawnPosition = zone.GetRandomSpawnPosition();
        Quaternion randomRotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
        GameObject spawnedObject = Instantiate(itemToSpawn.prefab, spawnPosition, randomRotation);

        if (spawnedObject.TryGetComponent<CollectibleObject>(out var collectible))
        {
            // 生成速度（如启用 isMoving）
            float speed = itemToSpawn.isMoving ?
                (itemToSpawn.useFixedSpeed ? itemToSpawn.fixedSpeed :
                Random.Range(itemToSpawn.minSpeed, itemToSpawn.maxSpeed)) : 0f;

            // 生成随机方向（x/y 在 -1 到 1，避免接近零）
            Vector2 direction;
            do
            {
                direction = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f));
            } while (direction.magnitude < 0.1f);

            // 设置速度与方向
            collectible.SetInitialVelocity(speed, direction);

            // 设置质量（支持固定）
            float objectMass = itemToSpawn.useFixedMass ?
                itemToSpawn.fixedMass : itemToSpawn.GetRandomMass();

            if (objectMass > 0)
                collectible.SetMass(objectMass);

            // 设置类型（根据Tag）
            if (spawnedObject.CompareTag("Obstacle"))
                collectible.subType = CollectibleObject.CollectibleSubType.RegularObstacle;
            else if (spawnedObject.CompareTag("CollectibleObstacle"))
                collectible.subType = CollectibleObject.CollectibleSubType.CollectibleObstacle;
            else
                collectible.subType = CollectibleObject.CollectibleSubType.Resource;
        }

        spawnedObject.AddComponent<ObjectTracker>().Initialize(zone);
        zone.currentObjectCount++;
        zone.lastSpawnTime = Time.time;
    }

    private void OnDrawGizmos()
    {
        if (!showSpawnAreas) return;
        foreach (var zone in spawnZones)
            zone.DrawGizmos();
    }
}

[System.Serializable]
public class SpawnZone
{
    public string zoneName = "生成区域";
    public Rect spawnArea = new Rect(-5f, -5f, 10f, 10f);
    public Color areaColor = Color.green;
    [Min(0)] public int maxObjects = 5;
    [Min(0.01f)] public float spawnRate = 1f;
    [Range(0f, 1f)] public float randomSpawnChance = 0.1f;
    [Min(0.01f)] public float minSpawnInterval = 0.2f;
    public bool useWeightedSpawn = false;
    public List<SpawnableItem> spawnableItems = new List<SpawnableItem>();
    [HideInInspector] public int currentObjectCount = 0;
    [HideInInspector] public float lastSpawnTime = 0f;

    public void Initialize()
    {
        currentObjectCount = 0;
        lastSpawnTime = -minSpawnInterval;
        foreach (var item in spawnableItems)
            item.Validate();
    }

    public bool CanSpawn() => currentObjectCount < maxObjects;

    public bool ShouldSpawn()
    {
        float timeSinceLastSpawn = Time.time - lastSpawnTime;
        float spawnInterval = Mathf.Max(1f / spawnRate, minSpawnInterval);
        return timeSinceLastSpawn >= spawnInterval && Random.value < randomSpawnChance;
    }

    public SpawnableItem SelectRandomItem()
    {
        if (spawnableItems.Count == 0) return null;

        if (useWeightedSpawn)
        {
            float totalWeight = 0f;
            foreach (var item in spawnableItems)
                totalWeight += item.spawnWeight;

            float randomValue = Random.value * totalWeight;
            float cumulativeWeight = 0f;
            foreach (var item in spawnableItems)
            {
                cumulativeWeight += item.spawnWeight;
                if (randomValue <= cumulativeWeight)
                    return item;
            }
        }

        return spawnableItems[Random.Range(0, spawnableItems.Count)];
    }

    public Vector2 GetRandomSpawnPosition()
        => new Vector2(Random.Range(spawnArea.xMin, spawnArea.xMax), Random.Range(spawnArea.yMin, spawnArea.yMax));

    public void DrawGizmos()
    {
#if UNITY_EDITOR
        Gizmos.color = areaColor;
        Vector3 bottomLeft = new Vector3(spawnArea.xMin, spawnArea.yMin, 0);
        Vector3 bottomRight = new Vector3(spawnArea.xMax, spawnArea.yMin, 0);
        Vector3 topRight = new Vector3(spawnArea.xMax, spawnArea.yMax, 0);
        Vector3 topLeft = new Vector3(spawnArea.xMin, spawnArea.yMax, 0);

        Gizmos.DrawLine(bottomLeft, bottomRight);
        Gizmos.DrawLine(bottomRight, topRight);
        Gizmos.DrawLine(topRight, topLeft);
        Gizmos.DrawLine(topLeft, bottomLeft);

        GUIStyle style = new GUIStyle { normal = { textColor = areaColor }, fontSize = 14, fontStyle = FontStyle.Bold };
        UnityEditor.Handles.Label(new Vector3(spawnArea.center.x, spawnArea.center.y, 0), zoneName, style);
        UnityEditor.Handles.Label(new Vector3(spawnArea.center.x, spawnArea.yMax + 0.5f, 0), $"{currentObjectCount}/{maxObjects}", style);
#endif
    }
}

[System.Serializable]
public class SpawnableItem
{
    public GameObject prefab;
    [Min(0.01f)] public float spawnFrequency = 0.5f;
    [Min(0.1f)] public float spawnWeight = 1f;
    public bool isMoving = true;

    public bool useFixedSpeed = false;
    [Min(0f)] public float fixedSpeed = 2f;
    [Min(0f)] public float minSpeed = 1f;
    [Min(0f)] public float maxSpeed = 3f;

    public bool useFixedMass = false;
    [Min(0f)] public float fixedMass = 1f;
    public bool useRandomMass = true;
    [Min(0.1f)] public float minMass = 1f;
    [Min(0.1f)] public float maxMass = 3f;

    public void Validate()
    {
        spawnFrequency = Mathf.Max(0.01f, spawnFrequency);
        minSpeed = Mathf.Max(0f, minSpeed);
        maxSpeed = Mathf.Max(minSpeed, maxSpeed);
        spawnWeight = Mathf.Max(0.1f, spawnWeight);
        minMass = Mathf.Max(0.1f, minMass);
        maxMass = Mathf.Max(minMass, maxMass);

        fixedSpeed = Mathf.Max(0f, fixedSpeed);
        fixedMass = Mathf.Max(0f, fixedMass);
    }

    public float GetRandomMass()
        => useRandomMass ? Random.Range(minMass, maxMass) : fixedMass;
}

public class ObjectTracker : MonoBehaviour
{
    private SpawnZone parentZone;

    public void Initialize(SpawnZone zone) => parentZone = zone;

    private void OnDestroy()
    {
        if (parentZone != null)
            parentZone.currentObjectCount--;
    }
}
