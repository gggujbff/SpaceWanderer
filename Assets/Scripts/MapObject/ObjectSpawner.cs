using UnityEngine;
using System.Collections.Generic;

public class ObjectSpawner : MonoBehaviour
{
    [Header("全局配置")]
    [Tooltip("是否在游戏开始时自动启动生成功能")]
    public bool autoSpawn = true;

    [Tooltip("是否在场景视图中显示生成区域的可视化边界")]
    public bool showSpawnAreas = true;

    [Header("生成区域配置")]
    [Tooltip("管理所有生成区域的列表，可以添加、删除和调整顺序")]
    public List<SpawnZone> spawnZones = new List<SpawnZone>();

    private void Start()
    {
        InitializeAllZones();
        enabled = autoSpawn;
    }

    private void InitializeAllZones()
    {
        foreach (var zone in spawnZones)
        {
            zone.Initialize();
        }
    }

    private void Update()
    {
        foreach (var zone in spawnZones)
        {
            if (zone.CanSpawn() && zone.ShouldSpawn())
            {
                SpawnObjectInZone(zone);
            }
        }
    }

    private void SpawnObjectInZone(SpawnZone zone)
    {
        SpawnableItem itemToSpawn = zone.SelectRandomItem();
        if (itemToSpawn == null || itemToSpawn.prefab == null) return;

        Vector2 spawnPosition = zone.GetRandomSpawnPosition();
        GameObject spawnedObject = Instantiate(itemToSpawn.prefab, spawnPosition, Quaternion.identity);
        
        // 设置MovingObstacle的属性
        if (spawnedObject.TryGetComponent<MovingObstacle>(out var obstacle))
        {
            float speed = itemToSpawn.isMoving ? Random.Range(itemToSpawn.minSpeed, itemToSpawn.maxSpeed) : 0f;
            Vector2 direction = Random.insideUnitCircle.normalized;
            obstacle.SetVelocity(direction * speed);
            obstacle.SetMass(itemToSpawn.GetRandomMass());
        }

        // 设置CollectibleObject的属性
        if (spawnedObject.TryGetComponent<CollectibleObject>(out var collectible))
        {
            float speed = itemToSpawn.isMoving ? Random.Range(itemToSpawn.minSpeed, itemToSpawn.maxSpeed) : 0f;
            Vector2 direction = Random.insideUnitCircle.normalized;
            collectible.SetVelocity(direction * speed);
            collectible.SetMass(itemToSpawn.GetRandomMass());
        }

        spawnedObject.AddComponent<ObjectTracker>().Initialize(zone);
        zone.currentObjectCount++;
        zone.lastSpawnTime = Time.time; // 记录生成时间
    }

    private void OnDrawGizmos()
    {
        if (!showSpawnAreas) return;

        foreach (var zone in spawnZones)
        {
            zone.DrawGizmos();
        }
    }
}

[System.Serializable]
public class SpawnZone
{
    [Tooltip("生成区域的名称，用于在编辑器中识别不同区域")]
    public string zoneName = "生成区域";

    [Tooltip("定义区域的矩形范围 (x,y) 是左下角坐标，(width,height) 是尺寸")]
    public Rect spawnArea = new Rect(-5f, -5f, 10f, 10f);

    [Tooltip("在场景视图中显示的区域颜色，帮助可视化区域位置")]
    public Color areaColor = Color.green;

    [Tooltip("该区域内允许同时存在的最大物体数量")]
    [Min(0)] public int maxObjects = 5;

    [Tooltip("理论上每秒生成的物体数量 (实际生成受随机概率影响)")]
    [Min(0.01f)] public float spawnRate = 1f;

    [Tooltip("每次尝试生成时实际生成的概率 (0-1之间)，值越高生成越频繁")]
    [Range(0f, 1f)] public float randomSpawnChance = 0.1f;

    [Tooltip("生成尝试之间的最小间隔时间 (秒)，防止生成过于频繁")]
    [Min(0.01f)] public float minSpawnInterval = 0.2f;

    [Tooltip("是否使用权重系统来决定生成哪种物体，启用后将根据spawnWeight属性随机选择")]
    public bool useWeightedSpawn = false;

    [Tooltip("该区域可以生成的物体列表及其配置")]
    public List<SpawnableItem> spawnableItems = new List<SpawnableItem>();

    [HideInInspector] public int currentObjectCount = 0;
    [HideInInspector] public float lastSpawnTime = 0f;

    public void Initialize()
    {
        currentObjectCount = 0;
        lastSpawnTime = -minSpawnInterval; // 初始化为允许立即生成
        
        foreach (var item in spawnableItems)
        {
            item.Validate();
        }
    }

    public bool CanSpawn()
    {
        return currentObjectCount < maxObjects;
    }

    public bool ShouldSpawn()
    {
        float timeSinceLastSpawn = Time.time - lastSpawnTime;
        
        // 计算生成间隔，取最大的合理值
        float spawnInterval = Mathf.Max(1f / spawnRate, minSpawnInterval);
        
        if (timeSinceLastSpawn < spawnInterval) 
            return false;
        
        return Random.value < randomSpawnChance;
    }

    public SpawnableItem SelectRandomItem()
    {
        if (spawnableItems.Count == 0) return null;
        
        if (useWeightedSpawn)
        {
            float totalWeight = 0f;
            foreach (var item in spawnableItems)
            {
                totalWeight += item.spawnWeight;
            }
            
            float randomValue = Random.value * totalWeight;
            float cumulativeWeight = 0f;
            
            foreach (var item in spawnableItems)
            {
                cumulativeWeight += item.spawnWeight;
                if (randomValue <= cumulativeWeight)
                {
                    return item;
                }
            }
        }
        
        return spawnableItems[Random.Range(0, spawnableItems.Count)];
    }

    public Vector2 GetRandomSpawnPosition()
    {
        float x = Random.Range(spawnArea.xMin, spawnArea.xMax);
        float y = Random.Range(spawnArea.yMin, spawnArea.yMax);
        return new Vector2(x, y);
    }

    public void DrawGizmos()
    {
        Gizmos.color = areaColor;
        
        Vector3 bottomLeft = new Vector3(spawnArea.xMin, spawnArea.yMin, 0);
        Vector3 bottomRight = new Vector3(spawnArea.xMax, spawnArea.yMin, 0);
        Vector3 topRight = new Vector3(spawnArea.xMax, spawnArea.yMax, 0);
        Vector3 topLeft = new Vector3(spawnArea.xMin, spawnArea.yMax, 0);
        
        Gizmos.DrawLine(bottomLeft, bottomRight);
        Gizmos.DrawLine(bottomRight, topRight);
        Gizmos.DrawLine(topRight, topLeft);
        Gizmos.DrawLine(topLeft, bottomLeft);
        
        GUIStyle style = new GUIStyle();
        style.normal.textColor = areaColor;
        style.fontSize = 14;
        style.fontStyle = FontStyle.Bold;
        
        Vector3 labelPosition = new Vector3(spawnArea.center.x, spawnArea.center.y, 0);
        UnityEditor.Handles.Label(labelPosition, zoneName, style);
        
        string countText = $"{currentObjectCount}/{maxObjects}";
        Vector3 countPosition = new Vector3(spawnArea.center.x, spawnArea.yMax + 0.5f, 0);
        UnityEditor.Handles.Label(countPosition, countText, style);
    }
}

[System.Serializable]
public class SpawnableItem
{
    [Tooltip("生成时使用的预制体")]
    public GameObject prefab;

    [Tooltip("该物体的生成频率 (值越高生成越频繁)，实际生成还受区域spawnRate和randomSpawnChance影响")]
    [Min(0.01f)] public float spawnFrequency = 0.5f;

    [Tooltip("当区域启用权重生成时，该值决定此物体被选中的概率 (权重越高越容易生成)")]
    [Min(0.1f)] public float spawnWeight = 1f;

    [Tooltip("生成的物体是否应该移动")]
    public bool isMoving = true;

    [Tooltip("当物体移动时的最小速度")]
    [Min(0f)] public float minSpeed = 1f;

    [Tooltip("当物体移动时的最大速度")]
    [Min(0f)] public float maxSpeed = 3f;

    [Tooltip("是否为生成的物体使用随机质量值")]
    public bool useRandomMass = true;

    [Tooltip("当useRandomMass未启用时，物体使用的固定质量值")]
    [Min(0.1f)] public float fixedMass = 1f;

    [Tooltip("当useRandomMass启用时，物体的最小质量值")]
    [Min(0.1f)] public float minMass = 1f;

    [Tooltip("当useRandomMass启用时，物体的最大质量值")]
    [Min(0.1f)] public float maxMass = 3f;

    public void Validate()
    {
        spawnFrequency = Mathf.Max(0.01f, spawnFrequency);
        minSpeed = Mathf.Max(0f, minSpeed);
        maxSpeed = Mathf.Max(minSpeed, maxSpeed);
        spawnWeight = Mathf.Max(0.1f, spawnWeight);
        minMass = Mathf.Max(0.1f, minMass);
        maxMass = Mathf.Max(minMass, maxMass);
    }

    public float GetRandomMass()
    {
        return useRandomMass ? Random.Range(minMass, maxMass) : fixedMass;
    }
}

public class ObjectTracker : MonoBehaviour
{
    private SpawnZone parentZone;

    public void Initialize(SpawnZone zone)
    {
        parentZone = zone;
    }

    private void OnDestroy()
    {
        if (parentZone != null)
        {
            parentZone.currentObjectCount--;
        }
    }
}