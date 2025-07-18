using UnityEngine;

public class ObstacleLauncher : MonoBehaviour
{
    [Header("发射器配置")]
    public GameObject obstaclePrefab;     // 障碍预制体（需挂载Obstacle）
    
    [Tooltip("障碍生成间隔")]
    public float launchInterval = 2f;     // 发射间隔（秒）
    
    [Header("其他参数")]
    [Tooltip("最小速度")]
    public float minLaunchSpeed = 1f;     // 最小发射速度
    
    [Tooltip("最大速度")]
    public float maxLaunchSpeed = 3f;     // 最大发射速度
    
    
    [Tooltip("最大旋转速度")]
    public float maxRotationSpeed = 15f;  // 最大旋转速度

    [Header("生成范围")]
    public float areaWidth = 10f;         // 随机位置区域宽度
    public float areaHeight = 8f;         // 随机位置区域高度
    
    [Tooltip("中心不生成区域的半径")]
    public float centerSafeRadius = 3f;   // 中心安全区半径


    private bool autoLaunch = true;
    private Vector2 massRange = new Vector2(1f, 5f); // 质量范围
    private float launchTimer = 0f;
    private int launchCount = 1;


    private void Update()
    {
        if (autoLaunch)
        {
            launchTimer += Time.deltaTime;
            if (launchTimer >= launchInterval)
            {
                launchTimer = 0;
                LaunchObstacles();
            }
        }
    }

    public void LaunchObstacles()     // 发射障碍
    {
        for (int i = 0; i < launchCount; i++)
        {
            LaunchSingleObstacle();
        }
    }

    private void LaunchSingleObstacle()     // 发射单个障碍
    {
        Vector3 spawnPos = GetRandomPosition();

        float randomMass = Random.Range(massRange.x, massRange.y);

        GameObject obstacle = Instantiate(obstaclePrefab, spawnPos, Quaternion.identity);
        Obstacle spaceObstacle = obstacle.GetComponent<Obstacle>();
        if (spaceObstacle == null)
        {
            Destroy(obstacle);
            return;
        }

        spaceObstacle.InitMassAndSize(randomMass);

        float randomAngle = Random.Range(0f, 360f);
        Vector2 direction = new Vector2(
            Mathf.Cos(randomAngle * Mathf.Deg2Rad),
            Mathf.Sin(randomAngle * Mathf.Deg2Rad)
        );
        float randomSpeed = Random.Range(minLaunchSpeed, maxLaunchSpeed);
        Vector2 velocity = direction * randomSpeed;

        float randomAngularVelocity = Random.Range(-maxRotationSpeed, maxRotationSpeed);

        spaceObstacle.SetInitialMovement(velocity, randomAngularVelocity);
    }

    private Vector3 GetRandomPosition()      // 获取随机位置（避开中心安全区）
    {
        Vector3 pos;
        int retryCount = 0;
        
        do
        {
            pos = transform.position + new Vector3(
                Random.Range(-areaWidth / 2, areaWidth / 2),
                Random.Range(-areaHeight / 2, areaHeight / 2),
                0
            );
            retryCount++;
            
            if (retryCount > 10)
            {
                float angle = Random.Range(0f, 360f);
                float edgeDistance = centerSafeRadius + 0.1f;
                pos = transform.position + new Vector3(
                    Mathf.Cos(angle * Mathf.Deg2Rad) * edgeDistance,
                    Mathf.Sin(angle * Mathf.Deg2Rad) * edgeDistance,
                    0
                );
                break;
            }
        } 
        while (Vector3.Distance(pos, transform.position) < centerSafeRadius);
        
        return pos;
    }

    private void OnDrawGizmosSelected()    // 编辑器显示发射区域
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, new Vector3(areaWidth, areaHeight, 0.1f));
        
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, centerSafeRadius);
    }
}