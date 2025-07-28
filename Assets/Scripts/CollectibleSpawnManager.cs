using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class SpawnData
{
    [Header("基础设置")]
    [Tooltip("要生成的Collectible预制体")]
    public GameObject collectiblePrefab;
    
    [Tooltip("游戏开始后多久生成（秒）")]
    public float spawnTime = 0f;
    
    [Tooltip("该物体需要到达目标位置的时间（游戏开始后秒数）")]
    public float targetArrivalTime = 5f;
    
    [Header("目标位置设置（2D坐标）")]
    [Tooltip("物体需要到达的目标位置（X坐标）")]
    public float targetX = 0f;
    
    [Tooltip("物体需要到达的目标位置（Y坐标）")]
    public float targetY = 0f;

    [HideInInspector] public bool hasSpawned = false; // 标记是否已生成

    // 计算并返回目标位置的2D向量
    public Vector2 GetTargetPosition()
    {
        return new Vector2(targetX, targetY);
    }
}

public class CollectibleSpawnManager : MonoBehaviour
{
    [Header("生成配置")]
    [Tooltip("所有需要生成的物体配置列表")]
    public List<SpawnData> spawnConfigs = new List<SpawnData>();

    [Header("调试设置")]
    [Tooltip("是否显示调试信息")]
    public bool showDebugInfo = true;

    // 统一游戏时间（自游戏开始统一游戏时间（自游戏开始后秒数）
    private float currentGameTime = 0f;


    private void Update()
    {
        // 更新统一游戏时间（基于关卡加载后的时间）
        currentGameTime = Time.timeSinceLevelLoad;

        // 检查并生成到达生成时间的物体
        CheckAndSpawnCollectibles();
    }

    /// <summary>
    /// 检查所有配置，生成到达生成时间的物体
    /// </summary>
    private void CheckAndSpawnCollectibles()
    {
        foreach (var config in spawnConfigs)
        {
            // 跳过已生成或无效的配置
            if (config.hasSpawned || !IsConfigValid(config))
                continue;

            // 当游戏时间到达生成时间时执行生成
            if (currentGameTime >= config.spawnTime)
            {
                SpawnCollectible(config);
                config.hasSpawned = true;
            }
        }
    }

    /// <summary>
    /// 生成Collectible物体并计算生成位置
    /// </summary>
    private void SpawnCollectible(SpawnData config)
    {
        // 获取预制体中的Collectible组件
        CollectibleObject prefabCollectible = config.collectiblePrefab.GetComponent<CollectibleObject>();
        if (prefabCollectible == null)
        {
            Debug.LogError($"【Spawn Error】预制体 {config.collectiblePrefab.name} 未挂载CollectibleObject组件！");
            return;
        }

        // 计算运动时间（从生成到到达目标位置的时间差）
        float moveDuration = config.targetArrivalTime - config.spawnTime;
        if (moveDuration <= 0)
        {
            Debug.LogError($"【Time Error】{config.collectiblePrefab.name} 到达时间必须晚于生成时间！（生成时间：{config.spawnTime}，到达时间：{config.targetArrivalTime}）");
            return;
        }

        // 1. 计算物体的运动速度向量（基于预制体自身配置）
        Vector2 velocityDir = prefabCollectible.initialDirection; // 已标准化
        float speed = prefabCollectible.initialSpeed;
        Vector2 movementVelocity = velocityDir * speed;

        // 2. 计算生成位置（反向推导：生成位置 = 目标位置 - 速度×运动时间）
        Vector2 targetPosition = new Vector2(config.targetX, config.targetY); // 直接使用X、Y坐标
        Vector2 spawnPosition = targetPosition - movementVelocity * moveDuration;

        // 3. 实例化物体
        GameObject spawnedObject = Instantiate(
            config.collectiblePrefab, 
            spawnPosition, 
            Quaternion.identity, 
            transform // 父物体设为管理器，方便层级管理
        );

        // 4. 初始化物体运动状态
        CollectibleObject spawnedCollectible = spawnedObject.GetComponent<CollectibleObject>();
        if (spawnedCollectible != null)
        {
            // 强制应用速度（覆盖预制体初始设置，确保运动轨迹正确）
            spawnedCollectible.SetInitialVelocity(speed, velocityDir);
            
            // 应用初始旋转（如果预制体有配置）
            spawnedCollectible.ApplyInitialRotation();

            if (showDebugInfo)
            {
                Debug.Log($"=== 生成物体 ===");
                Debug.Log($"物体名称：{spawnedObject.name}");
                Debug.Log($"生成时间：{config.spawnTime}s，到达时间：{config.targetArrivalTime}s");
                Debug.Log($"运动时间：{moveDuration}s，速度：{movementVelocity}");
                Debug.Log($"生成位置：{spawnPosition}，目标位置（X={config.targetX}, Y={config.targetY}）");
            }
        }
        else
        {
            Debug.LogError($"【Component Error】生成的物体 {spawnedObject.name} 未挂载CollectibleObject组件！");
        }
    }

    /// <summary>
    /// 验证配置是否有效
    /// </summary>
    private bool IsConfigValid(SpawnData config)
    {
        // 基础校验
        if (config.collectiblePrefab == null)
        {
            Debug.LogError("SpawnData中未设置collectiblePrefab！");
            return false;
        }

        // 时间逻辑校验
        if (config.targetArrivalTime <= config.spawnTime)
        {
            Debug.LogError($"{config.collectiblePrefab.name} 到达时间必须大于生成时间！");
            return false;
        }

        // 组件校验
        if (config.collectiblePrefab.GetComponent<CollectibleObject>() == null)
        {
            Debug.LogError($"{config.collectiblePrefab.name} 未挂载CollectibleObject组件！");
            return false;
        }

        return true;
    }

    private void OnDrawGizmos()
    {
        // 绘制目标位置辅助线（场景视图可视化）
        foreach (var config in spawnConfigs)
        {
            Vector2 targetPos = new Vector2(config.targetX, config.targetY);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(targetPos, 0.5f); // 目标位置标记（2D圆形）
            Gizmos.DrawLine(
                targetPos, 
                targetPos + Vector2.up * 1.5f // 绘制向上的指示线
            );
        }
    }
}