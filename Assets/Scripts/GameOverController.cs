using UnityEngine;

// 游戏结束方式枚举
public enum GameOverType
{
    Collision,  // 碰撞结束
    Time        // 时间结束
}

public class GameOverController : MonoBehaviour
{
    [Header("结束方式设置")]
    [Tooltip("选择游戏结束的触发方式")]
    public GameOverType gameOverType;

    [Header("时间结束设置")]
    [Tooltip("当选择时间结束时的倒计时时间(秒)")]
    [SerializeField] private float gameTime = 60f;

    private float timer;  // 计时器
    private HookSystem hookSystem;  // 分数系统引用

    private void Start()
    {
        // 初始化计时器
        timer = 0f;

        // 尝试获取场景中的HookSystem
        hookSystem = FindObjectOfType<HookSystem>();
        if (hookSystem == null)
        {
            Debug.LogWarning("场景中未找到HookSystem脚本，请确保场景中存在该脚本");
        }
    }

    private void Update()
    {
        // 如果是时间结束模式，进行计时
        if (gameOverType == GameOverType.Time)
        {
            timer += Time.deltaTime;
            
            if (timer >= gameTime)
            {
                GameOver();
            }
        }
    }

    // 2D碰撞检测（如果使用2D碰撞体，请启用此方法并注释上面的3D碰撞方法）
    private void OnCollisionEnter2D(Collision2D collision)
    {
         if (gameOverType == GameOverType.Collision && 
            collision.gameObject.CompareTag("Player"))
        {
            GameOver();
        }
    }

    /// <summary>
    /// 游戏结束处理
    /// </summary>
    private void GameOver()
    {
        // 暂停游戏
        Time.timeScale = 0f;

        // 获取当前分数
        int currentScore = 0;
        if (hookSystem != null)
        {
            currentScore = hookSystem.currentScore;
        }
        else
        {
            Debug.LogWarning("无法获取分数，HookSystem脚本不存在或未找到");
        }

        // 输出游戏结束信息和分数
        Debug.Log($"游戏结束 当前分数：{currentScore}");
    }
}