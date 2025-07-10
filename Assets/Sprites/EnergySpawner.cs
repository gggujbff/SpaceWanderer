using UnityEngine;

public class EnergySpawner : MonoBehaviour
{
    [Header("能量块设置")]
    public GameObject energyPrefab;  // 能量块预制体
    public int spawnCount = 10;      // 生成数量
    public float minDistance = 2f;   // 能量块之间的最小距离
    
    [Header("生成区域")]
    public Vector2 spawnAreaMin = new Vector2(-10, -5);  // 生成区域左下角
    public Vector2 spawnAreaMax = new Vector2(10, 5);    // 生成区域右上角

    private void Start()
    {
        SpawnEnergies();
    }

    // 生成能量块
    private void SpawnEnergies()
    {
        for (int i = 0; i < spawnCount; i++)
        {
            Vector2 randomPos = GetRandomPosition();
            if (IsPositionValid(randomPos))
            {
                SpawnEnergyAt(randomPos);
            }
            else
            {
                // 如果位置无效，尝试减少数量或使用其他策略
                Debug.LogWarning($"找不到有效位置生成第 {i+1} 个能量块");
                i--;  // 重试当前位置
            }
        }
    }

    // 获取随机位置
    private Vector2 GetRandomPosition()
    {
        float x = Random.Range(spawnAreaMin.x, spawnAreaMax.x);
        float y = Random.Range(spawnAreaMin.y, spawnAreaMax.y);
        return new Vector2(x, y);
    }

    // 检查位置是否有效（与已生成的能量块保持最小距离）
    private bool IsPositionValid(Vector2 position)
    {
        // 获取场景中已有的所有能量块
        GameObject[] existingEnergies = GameObject.FindGameObjectsWithTag("Energy");
        
        foreach (GameObject energy in existingEnergies)
        {
            float distance = Vector2.Distance(position, energy.transform.position);
            if (distance < minDistance)
            {
                return false;  // 距离太近，位置无效
            }
        }
        
        return true;  // 位置有效
    }

    // 在指定位置生成能量块
    private void SpawnEnergyAt(Vector2 position)
    {
        GameObject energy = Instantiate(energyPrefab, position, Quaternion.identity);
        energy.tag = "Energy";  // 确保标签正确
        energy.name = "Energy_" + Random.Range(1000, 9999);  // 为能量块命名
        
        // 可选：随机设置能量值
        Energy energyComponent = energy.GetComponent<Energy>();
        if (energyComponent != null)
        {
            energyComponent.energyAmount = Random.Range(5f, 15f);
        }
    }
}