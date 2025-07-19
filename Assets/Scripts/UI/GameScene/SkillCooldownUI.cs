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
    }

    void Update()
    {
        if (missileLauncher != null)
        {
            UpdateSkillCooldown(missileLauncher.lastFireTime, missileCooldown, missileIcon, missileCooldownText);
        }

        if (laserWeapon != null)
        {
            UpdateSkillCooldown(laserWeapon.lastFireTime, laserCooldown, laserIcon, laserCooldownText);
        }

        UpdateSkillCooldown(emptySkillLastFireTime, emptySkillCooldown, emptySkillIcon, emptySkillCooldownText);
    }

    private void UpdateSkillCooldown(float lastFireTime, float cooldown, Image icon, TextMeshProUGUI cooldownText)
    {
        float remainingCooldown = Mathf.Max(0, cooldown - (Time.time - lastFireTime));
        float cooldownPercentage = remainingCooldown / cooldown;

        icon.color = new Color(1f, 1f, 1f, 1f - cooldownPercentage);

        if (remainingCooldown <= 0)
        {
            icon.color = Color.white;
            cooldownText.text = "";
        }
        else
        {
            cooldownText.text = remainingCooldown.ToString("F1") + "s";
        }
    }

    public void UseEmptySkill()
    {
        emptySkillLastFireTime = Time.time;
    }
}