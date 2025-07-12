using UnityEngine;

public class ObstacleLauncher : MonoBehaviour
{
    [Header("发射器配置")]
    public GameObject obstaclePrefab;     // 障碍预制体（需挂载SpaceObstacle）
    public int launchCount = 1;           // 每次发射数量
    public float launchInterval = 2f;     // 发射间隔（秒）
    public bool autoLaunch = true;        // 是否自动发射

    [Header("发射参数")]
    public float minLaunchSpeed = 1f;     // 最小发射速度
    public float maxLaunchSpeed = 3f;     // 最大发射速度
    public Vector2 massRange = new Vector2(1f, 5f); // 质量范围（与障碍的massRange对应）
    public float maxRotationSpeed = 15f;  // 最大旋转速度

    [Header("发射区域")]
    public float areaWidth = 10f;         // 随机位置区域宽度
    public float areaHeight = 8f;         // 随机位置区域高度

    private float launchTimer = 0f;

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

    // 发射障碍
    public void LaunchObstacles()
    {
        for (int i = 0; i < launchCount; i++)
        {
            LaunchSingleObstacle();
        }
    }

    // 发射单个障碍
    private void LaunchSingleObstacle()
    {
        // 1. 随机位置（发射区域内）
        Vector3 spawnPos = transform.position + new Vector3(
            Random.Range(-areaWidth / 2, areaWidth / 2),
            Random.Range(-areaHeight / 2, areaHeight / 2),
            0
        );

        // 2. 随机质量
        float randomMass = Random.Range(massRange.x, massRange.y);

        // 3. 实例化障碍
        GameObject obstacle = Instantiate(obstaclePrefab, spawnPos, Quaternion.identity);
        Obstacle spaceObstacle = obstacle.GetComponent<Obstacle>();
        if (spaceObstacle == null)
        {
            Debug.LogError("障碍预制体未挂载SpaceObstacle组件！");
            Destroy(obstacle);
            return;
        }

        // 4. 设置质量和大小（核心关联逻辑）
        spaceObstacle.InitMassAndSize(randomMass);

        // 5. 随机方向和速度
        float randomAngle = Random.Range(0f, 360f);
        Vector2 direction = new Vector2(
            Mathf.Cos(randomAngle * Mathf.Deg2Rad),
            Mathf.Sin(randomAngle * Mathf.Deg2Rad)
        );
        float randomSpeed = Random.Range(minLaunchSpeed, maxLaunchSpeed);
        Vector2 velocity = direction * randomSpeed;

        // 6. 随机旋转速度
        float randomAngularVelocity = Random.Range(-maxRotationSpeed, maxRotationSpeed);

        // 7. 应用初始移动
        spaceObstacle.SetInitialMovement(velocity, randomAngularVelocity);
    }

    // 编辑器显示发射区域
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(areaWidth, areaHeight, 0.1f));
    }
}