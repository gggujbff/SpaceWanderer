using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SkillCooldownUI : MonoBehaviour
{
    public MissileLauncher missileLauncher;
    public LaserWeapon laserWeapon;

    public Image missileIcon;
    public Image laserIcon;
    public Image emptySkillIcon;

    public TextMeshProUGUI missileCooldownText;
    public TextMeshProUGUI laserCooldownText;
    public TextMeshProUGUI emptySkillCooldownText;

    private float missileCooldown;
    private float laserCooldown;
    private float emptySkillCooldown = 0f;
    private float emptySkillLastFireTime = float.MinValue;

    // 存储技能图标的原始颜色
    private Color missileOriginalColor;
    private Color laserOriginalColor;
    private Color emptySkillOriginalColor;

    // 冷却时的透明度
    [Range(0f, 1f)]
    public float cooldownAlpha = 0.5f;

    void Start()
    {
        if (missileLauncher != null)
        {
            missileCooldown = missileLauncher.cooldown;
        }
        else
        {
            Debug.LogError("MissileLauncher 引用为空，请在 Inspector 面板中赋值！");
        }

        if (laserWeapon != null)
        {
            laserCooldown = laserWeapon.cooldown;
        }
        else
        {
            Debug.LogError("LaserWeapon 引用为空，请在 Inspector 面板中赋值！");
        }

        // 存储初始颜色
        if (missileIcon != null)
            missileOriginalColor = missileIcon.color;
        
        if (laserIcon != null)
            laserOriginalColor = laserIcon.color;
        
        if (emptySkillIcon != null)
            emptySkillOriginalColor = emptySkillIcon.color;
    }

    void Update()
    {
        if (missileLauncher != null)
        {
            UpdateSkillCooldown(missileLauncher.lastFireTime, missileCooldown, missileIcon, missileCooldownText, missileOriginalColor);
        }

        if (laserWeapon != null)
        {
            UpdateSkillCooldown(laserWeapon.lastFireTime, laserCooldown, laserIcon, laserCooldownText, laserOriginalColor);
        }

        UpdateSkillCooldown(emptySkillLastFireTime, emptySkillCooldown, emptySkillIcon, emptySkillCooldownText, emptySkillOriginalColor);
    }

    private void UpdateSkillCooldown(float lastFireTime, float cooldown, Image icon, TextMeshProUGUI cooldownText, Color originalColor)
    {
        if (icon == null || cooldownText == null)
            return;

        float remainingCooldown = Mathf.Max(0, cooldown - (Time.time - lastFireTime));
        float cooldownPercentage = remainingCooldown / cooldown;

        if (remainingCooldown <= 0)
        {
            // 技能就绪时恢复原始颜色
            icon.color = originalColor;
            cooldownText.text = "";
        }
        else
        {
            // 根据冷却进度计算颜色和透明度
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

    public void UseEmptySkill()
    {
        emptySkillLastFireTime = Time.time;
    }
}    