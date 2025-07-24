using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SkillCooldownUI : MonoBehaviour
{
    // 发射器引用
    public MissileLauncher missileLauncher;
    public LaserWeapon laserWeapon;
    public NetLauncher netLauncher;

    // 技能图标与冷却文本
    public Image missileIcon;
    public Image laserIcon;
    public Image netIcon;
    public TextMeshProUGUI missileCooldownText;
    public TextMeshProUGUI laserCooldownText;
    public TextMeshProUGUI netCooldownText;
    public TextMeshProUGUI netCountText;

    // 新增：三个发射器的int变量显示文本（位置留空）
    public TextMeshProUGUI missileIntValueText;  // 导弹的int变量显示
    public TextMeshProUGUI laserIntValueText;   // 激光的int变量显示
    public TextMeshProUGUI netIntValueText;     // 捕网的int变量显示

    // 冷却时间与颜色变量
    private float missileCooldown;
    private float laserCooldown;
    private float netCooldown;
    private Color missileOriginalColor;
    private Color laserOriginalColor;
    private Color netOriginalColor;
    [Range(0f, 1f)]
    public float cooldownAlpha = 0.5f;

    void Start()
    {
        // 初始化冷却时间
        if (missileLauncher != null)
            missileCooldown = missileLauncher.cooldown;
        else
            Debug.LogError("MissileLauncher 引用为空！");

        if (laserWeapon != null)
            laserCooldown = laserWeapon.cooldown;
        else
            Debug.LogError("LaserWeapon 引用为空！");

        if (netLauncher != null)
            netCooldown = netLauncher.cooldown;
        else
            Debug.LogError("NetLauncher 引用为空！");

        // 存储原始颜色
        if (missileIcon != null)
            missileOriginalColor = missileIcon.color;
        if (laserIcon != null)
            laserOriginalColor = laserIcon.color;
        if (netIcon != null)
            netOriginalColor = netIcon.color;
    }

    void Update()
    {
        // 导弹技能更新
        if (missileLauncher != null)
        {
            UpdateSkillCooldown(missileLauncher.lastFireTime, missileCooldown, missileIcon, missileCooldownText, missileOriginalColor);
            // 新增：更新导弹的int变量显示
            if (missileIntValueText != null)
            {
                // 注意：此处需替换为MissileLauncher中实际的int变量名（例如：missileCount）
                missileIntValueText.text = missileLauncher.currentMissileCount.ToString(); 
            }
        }

        // 激光技能更新
        if (laserWeapon != null)
        {
            UpdateSkillCooldown(laserWeapon.lastFireTime, laserCooldown, laserIcon, laserCooldownText, laserOriginalColor);
            // 新增：更新激光的int变量显示
            if (laserIntValueText != null)
            {
                // 注意：此处需替换为LaserWeapon中实际的int变量名（例如：laserCharge）
                laserIntValueText.text = laserWeapon.fireCount.ToString();
            }
        }

        // 捕网技能更新
        if (netLauncher != null)
        {
            UpdateSkillCooldown(netLauncher.lastFireTime, netCooldown, netIcon, netCooldownText, netOriginalColor);
            if (netCountText != null)
            {
                netCountText.text = $"{netLauncher.currentNetCount}/{netLauncher.maxNetCount}";
            }
            // 新增：更新捕网的int变量显示
            if (netIntValueText != null)
            {
                // 注意：此处需替换为NetLauncher中实际的int变量名（例如：netAmmo）
                netIntValueText.text = netLauncher.currentNetCount.ToString();
            }
        }
    }

    private void UpdateSkillCooldown(float lastFireTime, float cooldown, Image icon, TextMeshProUGUI cooldownText, Color originalColor)
    {
        if (icon == null || cooldownText == null)
            return;

        float remainingCooldown = Mathf.Max(0, cooldown - (Time.time - lastFireTime));
        float cooldownPercentage = remainingCooldown / cooldown;

        if (remainingCooldown <= 0)
        {
            icon.color = originalColor;
            cooldownText.text = "";
        }
        else
        {
            float alpha = Mathf.Lerp(cooldownAlpha, 1f, 1 - cooldownPercentage);
            icon.color = new Color(
                originalColor.r * (0.5f + cooldownPercentage * 0.5f),
                originalColor.g * (0.5f + cooldownPercentage * 0.5f),
                originalColor.b * (0.5f + cooldownPercentage * 0.5f),
                alpha
            );
            cooldownText.text = remainingCooldown.ToString("F1") + "s";
        }
    }
}