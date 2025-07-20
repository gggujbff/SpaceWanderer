using UnityEngine;
using System.Collections.Generic;

public class ObjectSpawner : MonoBehaviour
{
    // 单例模式
    public static ObjectSpawner Instance;

    [Header("全局配置")]
    [Tooltip("物体生成的区域范围")]
    public Rect spawnArea = new Rect(-10f, -10f, 20f, 20f);
    
    [Tooltip("场景中允许存在的最大物体数量")]
    public int maxObjects = 15;
    
    [Tooltip("是否启用自动生成")]
    public bool autoSpawn = true;
    
    [Tooltip("是否在Scene视图中显示生成区域")]
    public bool showSpawnArea = true;
    
    [Tooltip("生成区域的绘制颜色")]
    public Color areaColor = Color.green;

    // 物体预制体及频率配置
    [Header("生成项配置")]
    [Tooltip("所有可生成的物体及其生成频率")]
    [SerializeField] private SpawnableItem[] spawnableItems;

    private float[] nextSpawnTimes;
    private int currentObjectCount = 0;

    [System.Serializable]
    public class SpawnableItem
    {
        [Tooltip("要生成的物体预制体")]
        public GameObject prefab;
        
        [Tooltip("生成频率（值越大生成越快，每秒尝试生成的次数）")]
        [Min(0.01f)] public float spawnFrequency = 0.5f; // 生成频率（每秒次数）
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
        
        if (autoSpawn)
            enabled = true;
        else
            enabled = false;
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
            // 计算生成间隔 = 1/频率
            float spawnInterval = 1f / spawnableItems[i].spawnFrequency;
            nextSpawnTimes[i] = Time.time + spawnInterval;
        }
    }

    private void Update()
    {
        if (currentObjectCount >= maxObjects)
            return;

        for (int i = 0; i < spawnableItems.Length; i++)
        {
            var item = spawnableItems[i];
            
            if (item.prefab == null || currentObjectCount >= maxObjects)
                continue;
                
            float spawnInterval = 1f / item.spawnFrequency;
            if (Time.time >= nextSpawnTimes[i])
            {
                SpawnObject(item.prefab);
                nextSpawnTimes[i] = Time.time + spawnInterval;
            }
        }
    }

    private GameObject SpawnObject(GameObject prefab)
    {
        Vector2 spawnPosition = GetRandomSpawnPosition();
        GameObject spawnedObject = Instantiate(prefab, spawnPosition, Quaternion.identity);
        
        // 设置障碍物的随机速度
        if (spawnedObject.TryGetComponent<MovingObstacle>(out var obstacle))
        {
            float randomSpeed = Random.Range(1f, 3f);
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            obstacle.velocity = randomDir * randomSpeed;
        }
        
        spawnedObject.transform.SetParent(transform);
        RegisterSpawnedObject(spawnedObject);
        
        return spawnedObject;
    }

    private Vector2 GetRandomSpawnPosition()
    {
        float x = Random.Range(spawnArea.xMin, spawnArea.xMax);
        float y = Random.Range(spawnArea.yMin, spawnArea.yMax);
        return new Vector2(x, y);
    }

    private void RegisterSpawnedObject(GameObject obj)
    {
        currentObjectCount++;
        
        if (obj.TryGetComponent<ObjectDespawnListener>(out var listener))
        {
            listener.onDespawn += OnObjectDespawned;
        }
        else
        {
            var newListener = obj.AddComponent<ObjectDespawnListener>();
            newListener.onDespawn += OnObjectDespawned;
        }
    }

    private void OnObjectDespawned(GameObject obj)
    {
        currentObjectCount--;
    }

    // 启用自动生成
    public void StartAutoSpawn()
    {
        autoSpawn = true;
        enabled = true;
        InitializeSpawnTimes();
    }

    // 停止自动生成
    public void StopAutoSpawn()
    {
        autoSpawn = false;
        enabled = false;
    }

    // 在Scene视图中绘制生成区域
    private void OnDrawGizmos()
    {
        if (!showSpawnArea)
            return;
            
        Gizmos.color = areaColor;
        
        // 绘制矩形边界
        Vector3 bottomLeft = new Vector3(spawnArea.xMin, spawnArea.yMin, 0);
        Vector3 bottomRight = new Vector3(spawnArea.xMax, spawnArea.yMin, 0);
        Vector3 topRight = new Vector3(spawnArea.xMax, spawnArea.yMax, 0);
        Vector3 topLeft = new Vector3(spawnArea.xMin, spawnArea.yMax, 0);
        
        Gizmos.DrawLine(bottomLeft, bottomRight);
        Gizmos.DrawLine(bottomRight, topRight);
        Gizmos.DrawLine(topRight, topLeft);
        Gizmos.DrawLine(topLeft, bottomLeft);
        
        // 在矩形中心显示物体数量
        if (Application.isPlaying)
        {
            GUIStyle style = new GUIStyle();
            style.normal.textColor = areaColor;
            style.fontSize = 14;
            Vector3 center = new Vector3(spawnArea.center.x, spawnArea.center.y, 0);
            UnityEditor.Handles.Label(center, $"Objects: {currentObjectCount}/{maxObjects}", style);
        }
    }
}

// 物体销毁监听器（保持不变）
public class ObjectDespawnListener : MonoBehaviour
{
    [Tooltip("物体销毁时触发的事件")]
    public System.Action<GameObject> onDespawn;

    private void OnDestroy()
    {
        onDespawn?.Invoke(gameObject);
    }
}