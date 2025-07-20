using UnityEngine;
using System.Collections.Generic;

public class EnergySpawner : MonoBehaviour
{
    [Header("能量块设置")]
    [Tooltip("能量块预制体")]
    public GameObject energyPrefab;
    
    [Tooltip("场景中保持的能量块数量")]
    public int targetCount = 10;
    
    [Tooltip("能量块之间的最小距离")]
    public float minDistance = 2f;
    
    [Header("生成区域")]
    [Tooltip("生成区域宽度")]
    public float areaWidth = 20f;
    
    [Tooltip("生成区域高度")]
    public float areaHeight = 10f;
    
    [Tooltip("中心不生成区域的半径")]
    public float centerSafeRadius = 3f;

    // 存储所有生成的能量块
    private List<GameObject> _activeEnergies = new List<GameObject>();

    private void Start()
    {
        SpawnInitialEnergies();
    }

    private void Update()
    {
        _activeEnergies.RemoveAll(energy => energy == null);
        
        if (_activeEnergies.Count < targetCount)
        {
            int needToSpawn = targetCount - _activeEnergies.Count;
            for (int i = 0; i < needToSpawn; i++)
            {
                TrySpawnEnergy();
            }
        }
    }

    private void SpawnInitialEnergies()  // 初始生成
    {
        for (int i = 0; i < targetCount; i++)
        {
            TrySpawnEnergy();
        }
    }

    private void TrySpawnEnergy()  // 生成能量块
    {
        Vector2 randomPos = GetRandomPosition();
        int retryCount = 0;
        
        // 最多尝试10次寻找有效位置（增加重试次数适应新规则）
        while (!IsPositionValid(randomPos) && retryCount < 10)
        {
            randomPos = GetRandomPosition();
            retryCount++;
        }
        
        if (IsPositionValid(randomPos))
        {
            GameObject energy = SpawnEnergyAt(randomPos);
            _activeEnergies.Add(energy);
        }
    }

    private Vector2 GetRandomPosition()  // 随机生成位置
    {
        Vector2 pos;
        // 循环生成位置，直到不在中心安全区内
        do
        {
            float x = transform.position.x + Random.Range(-areaWidth / 2, areaWidth / 2);
            float y = transform.position.y + Random.Range(-areaHeight / 2, areaHeight / 2);
            pos = new Vector2(x, y);
        } 
        while (Vector2.Distance(pos, transform.position) < centerSafeRadius);
        
        return pos;
    }

    private bool IsPositionValid(Vector2 position)  // 检查位置是否有效
    {
        // 检查是否在中心安全区内
        if (Vector2.Distance(position, transform.position) < centerSafeRadius)
        {
            return false;
        }
        
        // 检查与其他能量块的距离
        foreach (GameObject energy in _activeEnergies)
        {
            if (energy != null && Vector2.Distance(position, energy.transform.position) < minDistance)
            {
                return false;
            }
        }
        
        return true;
    }

    private GameObject SpawnEnergyAt(Vector2 position)  // 生成能量块
    {
        GameObject energy = Instantiate(energyPrefab, position, Quaternion.identity);
        energy.tag = "Energy";
        energy.name = "Energy_" + Random.Range(1000, 9999);
        
        // 随机设置能量值
        Energy energyComponent = energy.GetComponent<Energy>();
        if (energyComponent != null)
        {
            energyComponent.energyAmount = Random.Range(5f, 15f);
        }
        
        return energy;
    }

    private void OnDrawGizmosSelected() 
    {
        // 绘制整个生成区域
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(areaWidth, areaHeight, 0.1f));
        
        // 绘制中心安全区（不生成区域）
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, centerSafeRadius);
        
        // 显示当前能量块数量
        if (_activeEnergies != null)
        {
            Gizmos.color = Color.white;
            string countText = $"能量块: {_activeEnergies.Count}/{targetCount}";
            UnityEditor.Handles.Label(transform.position + Vector3.up * (areaHeight/2 + 0.5f), countText);
        }
    }
}