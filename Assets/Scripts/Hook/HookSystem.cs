using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class HookSystem : MonoBehaviour
{
    // 钩爪状态：待发射、发射中、回收中
    public enum HookState { ReadyToLaunch, Launching, Retrieving }
    // 旋转方向：顺时针、逆时针
    public enum RotationDir { Clockwise, CounterClockwise }

    [Header("钩爪素材")]
    public GameObject hookTipPrefab; // 钩爪尖端预制体

    [Header("绳索设置")]
    public Material ropeMaterial; // 绳索材质（需赋值，建议用Unlit/Color）
    public Color ropeColor = Color.yellow; // 改为黄色（更醒目）
    [Range(0.1f, 1f)] public float ropeWidth = 0.4f; // 增大宽度至0.4f
    public string ropeSortingLayer = "Default"; // 保持默认层级
    public int ropeSortingOrder = 50; // 提高排序值，确保在默认层级最上层

    [Header("基础属性")]
    public float maxLength = 10f;
    public float standbyDistance = 3f; // 增大初始待机距离，确保初始可见
    public float baseRotateSpeed = 30f;
    public float baseLaunchSpeed = 10f;
    public float baseRetrieveSpeed = 10f;

    [Header("加速属性")]
    public float accelerateRotateSpeed = 100f;
    public float accelerateLaunchSpeed = 20f;
    public float accelerateRetrieveSpeed = 24f;

    [Header("能量参数")]
    public float initialEnergy = 100f;
    public float rotateSwitchEnergyCost = 5f;
    public float accelerateEnergyCostPerSecond = 8f;

    [Header("冷却CD")]
    public float rotateSwitchCD = 1f;
    public float accelerateCD = 2f;

    [Header("加速度")]
    public float rotationSmoothSpeed = 5f;
    public float lengthSmoothSpeed = 5f;
    public float switchDirSmoothSpeed = 8f;

    [Header("UI显示")]
    public Slider energySlider;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI energyPercentText;
    public Slider healthSlider;
    public TextMeshProUGUI healthPercentText;

    [Header("玩家生命属性")]
    public float maxHealth = 100f;

    [Header("物理属性")]
    public float hookTipMass = 0.5f;

    // 内部状态变量（完全保留）
    [HideInInspector] public HookState currentState = HookState.ReadyToLaunch;
    [HideInInspector] public RotationDir currentDir = RotationDir.Clockwise;
    [HideInInspector] public float currentLength = 0f;
    [HideInInspector] public float currentRotation = 0f;
    [HideInInspector] public float currentEnergy;
    [HideInInspector] public float currentHealth;

    private float currentRotateSpeed;
    public float currentLaunchSpeed;
    public float currentRetrieveSpeed;
    private int currentScore = 0;
    private bool isAccelerating = false;
    private float rotateSwitchCDTimer = 0f;
    private float accelerateCDTimer = 0f;
    private bool isSwitchingDir = false;

    private LineRenderer ropeRenderer;
    private Transform hookTip;
    private HookTipCollisionHandler hookTipCollisionHandler;

    public static HookSystem Instance;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        SetupRope();
    }

    private void Start()
    {
        InitFromPrefabs();
        Init();
        if (hookTip != null)
        {
            hookTipCollisionHandler = hookTip.GetComponent<HookTipCollisionHandler>();
            hookTipCollisionHandler.hookSystem = this;
        }
        InitUI();
    }

    // 初始化绳索（仅优化渲染可见性，不改变逻辑）
    private void SetupRope()
    {
        ropeRenderer = gameObject.GetComponent<LineRenderer>() ?? gameObject.AddComponent<LineRenderer>();
        ropeRenderer.enabled = true; // 强制启用

        // 材质处理：确保使用Unlit/Color，避免光照影响
        if (ropeMaterial == null)
        {
            ropeMaterial = new Material(Shader.Find("Unlit/Color"));
            ropeMaterial.color = ropeColor; // 用用户设置的颜色
            Debug.LogWarning("已自动创建绳索材质（Unlit/Color），建议手动赋值");
        }
        else
        {
            // 强制材质使用Unlit/Color（关键：避免3D shader在2D场景不可见）
            if (ropeMaterial.shader.name != "Unlit/Color")
            {
                ropeMaterial.shader = Shader.Find("Unlit/Color");
                Debug.LogWarning("绳索材质已自动切换为Unlit/Color，确保可见");
            }
            ropeMaterial.color = new Color(ropeColor.r, ropeColor.g, ropeColor.b, 1f); // 强制不透明
        }

        // 核心渲染参数（保留用户设置，仅强化可见性）
        ropeRenderer.material = ropeMaterial;
        ropeRenderer.startColor = new Color(ropeColor.r, ropeColor.g, ropeColor.b, 1f);
        ropeRenderer.endColor = new Color(ropeColor.r, ropeColor.g, ropeColor.b, 1f);
        ropeRenderer.widthMultiplier = ropeWidth;
        ropeRenderer.positionCount = 2;
        ropeRenderer.useWorldSpace = true;

        // 层级设置（保留用户的ropeSortingLayer和ropeSortingOrder，仅确保有效）
        ropeRenderer.sortingLayerName = ropeSortingLayer;
        ropeRenderer.sortingOrder = ropeSortingOrder;

        // 渲染优化（防止遮挡）
        ropeRenderer.allowOcclusionWhenDynamic = false;
        ropeRenderer.receiveShadows = false;
        ropeRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    // 初始化钩爪尖端（完全保留）
    private void InitFromPrefabs()
    {
        if (hookTipPrefab != null)
        {
            GameObject tipInstance = Instantiate(hookTipPrefab, transform);
            hookTip = tipInstance.transform;
            hookTip.localPosition = Vector3.zero;
            hookTip.position = new Vector3(hookTip.position.x, hookTip.position.y, transform.position.z);
        }
    }

    // 初始化状态（完全保留，仅确保初始长度足够）
    public void Init()
    {
        currentState = HookState.ReadyToLaunch;
        currentDir = RotationDir.Clockwise;
        currentLength = standbyDistance; // 初始长度=待机距离（已增大至3f）
        currentRotation = 0f;
        currentEnergy = initialEnergy;
        currentHealth = maxHealth;
        currentScore = 0;

        rotateSwitchCDTimer = 0f;
        accelerateCDTimer = 0f;
        isAccelerating = false;
        isSwitchingDir = false;

        currentRotateSpeed = baseRotateSpeed;
        currentLaunchSpeed = baseLaunchSpeed;
        currentRetrieveSpeed = baseRetrieveSpeed;

        UpdateUIDisplay();
    }

    // 以下方法完全保留，确保原有功能不变
    private void Update()
    {
        UpdateCDTimers(Time.deltaTime);
        HandleInput();
        UpdateState(Time.deltaTime);
        UpdateHookPosition();
        
        UpdateUIDisplay(); // 保证UI实时刷新
    }

    private void LateUpdate()
    {
        UpdateRopePath();
    }

    private void UpdateCDTimers(float deltaTime)
    {
        if (rotateSwitchCDTimer > 0) rotateSwitchCDTimer -= deltaTime;
        if (accelerateCDTimer > 0) accelerateCDTimer -= deltaTime;
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SwitchLaunchOrRetrieve();
        }

        if (Input.GetKeyDown(KeyCode.A) && !isSwitchingDir && currentEnergy >= rotateSwitchEnergyCost &&
            rotateSwitchCDTimer <= 0 && currentState == HookState.ReadyToLaunch)
        {
            StartSwitchRotationDir();
        }

        bool isShiftPressed = Input.GetKey(KeyCode.LeftShift);
        if (isShiftPressed && !isAccelerating && currentEnergy > 0 && accelerateCDTimer <= 0)
        {
            isAccelerating = true;
        }
        else if (!isShiftPressed && isAccelerating)
        {
            isAccelerating = false;
            accelerateCDTimer = accelerateCD;
        }
    }

    private void UpdateState(float deltaTime)
    {
        switch (currentState)
        {
            case HookState.ReadyToLaunch:
                UpdateRotation(deltaTime);
                break;
            case HookState.Launching:
                UpdateLaunching(deltaTime);
                break;
            case HookState.Retrieving:
                UpdateRetrieving(deltaTime);
                break;
        }

        if (isAccelerating)
        {
            float cost = accelerateEnergyCostPerSecond * deltaTime;
            currentEnergy = Mathf.Max(0, currentEnergy - cost);
            if (currentEnergy <= 0) isAccelerating = false;
        }

        float targetRotate = isAccelerating ? accelerateRotateSpeed : baseRotateSpeed;
        float targetLaunch = isAccelerating ? accelerateLaunchSpeed : baseLaunchSpeed;
        float targetRetrieve = isAccelerating ? accelerateRetrieveSpeed : baseRetrieveSpeed;

        currentRotateSpeed = Mathf.Lerp(currentRotateSpeed, targetRotate * (currentDir == RotationDir.Clockwise ? 1 : -1), 
            deltaTime * (isSwitchingDir ? switchDirSmoothSpeed : rotationSmoothSpeed));
        currentLaunchSpeed = Mathf.Lerp(currentLaunchSpeed, targetLaunch, deltaTime * lengthSmoothSpeed);
        currentRetrieveSpeed = Mathf.Lerp(currentRetrieveSpeed, targetRetrieve, deltaTime * lengthSmoothSpeed);
    }

    private void SwitchLaunchOrRetrieve()
    {
        if (currentState == HookState.ReadyToLaunch)
        {
            currentState = HookState.Launching;
        }
    }

    private void StartSwitchRotationDir()
    {
        isSwitchingDir = true;
        currentDir = currentDir == RotationDir.Clockwise ? RotationDir.CounterClockwise : RotationDir.Clockwise;
        currentEnergy -= rotateSwitchEnergyCost;
        rotateSwitchCDTimer = rotateSwitchCD;
    }

    private void UpdateRotation(float deltaTime)
    {
        currentRotation += currentRotateSpeed * deltaTime;
        currentRotation = (currentRotation % 360 + 360) % 360;

        if (isSwitchingDir && Mathf.Abs(currentRotateSpeed - (currentDir == RotationDir.Clockwise ? baseRotateSpeed : -baseRotateSpeed)) < 0.5f)
        {
            isSwitchingDir = false;
        }
    }

    private void UpdateLaunching(float deltaTime)
    {
        currentLength += currentLaunchSpeed * deltaTime;
        if (currentLength >= maxLength)
        {
            currentLength = maxLength;
            currentState = HookState.Retrieving;
        }
    }

    private void UpdateRetrieving(float deltaTime)
    {
        currentLength -= currentRetrieveSpeed * deltaTime;
        if (currentLength <= standbyDistance)
        {
            currentLength = standbyDistance;
            currentState = HookState.ReadyToLaunch;
            hookTipCollisionHandler?.OnRetrieveComplete(); // 回收完成，通知钩尖
        }
    }

    public void RetrieveHook()
    {
        if (currentState == HookState.Launching)
        {
            currentState = HookState.Retrieving;
        }
    }


    private void UpdateHookPosition()
    {
        if (hookTip == null) return;

        float radians = currentRotation * Mathf.Deg2Rad;
        Vector2 direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)).normalized;
        Vector2 hookTipPos = (Vector2)transform.position + direction * currentLength;
        hookTip.position = new Vector3(hookTipPos.x, hookTipPos.y, transform.position.z);

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        hookTip.rotation = Quaternion.Euler(0, 0, angle);
    }

    // 更新绳索路径（仅强化可见性，不改变逻辑）
    private void UpdateRopePath()
    {
        if (ropeRenderer == null) return;

        Vector3 startPos = new Vector3(transform.position.x, transform.position.y, transform.position.z);
        Vector3 endPos;
        if (hookTip != null)
        {
            endPos = hookTip.position;
        }
        else
        {
            float radians = currentRotation * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)).normalized;
            endPos = (Vector2)transform.position + direction * currentLength;
            endPos = new Vector3(endPos.x, endPos.y, transform.position.z);
        }

        // 强制设置路径，确保长度足够
        ropeRenderer.positionCount = 2;
        ropeRenderer.SetPosition(0, startPos);
        ropeRenderer.SetPosition(1, endPos);

        // 强制刷新参数（防止被其他逻辑覆盖）
        ropeRenderer.widthMultiplier = ropeWidth;
        ropeRenderer.startColor = new Color(ropeColor.r, ropeColor.g, ropeColor.b, 1f);
        ropeRenderer.endColor = new Color(ropeColor.r, ropeColor.g, ropeColor.b, 1f);
        ropeRenderer.enabled = true;
    }

    // 以下UI和状态方法完全保留
    private void UpdateUIDisplay()
    {
        if (energySlider != null) energySlider.value = currentEnergy;
        if (energyPercentText != null)
            energyPercentText.text = $"{(currentEnergy / initialEnergy) * 100f:F1}%";
        if (healthSlider != null) healthSlider.value = currentHealth;
        if (healthPercentText != null)
            healthPercentText.text = $"{(currentHealth / maxHealth) * 100f:F1}%";
        if (scoreText != null)
            scoreText.text = $"分数: {currentScore}";
    }

    private void InitUI()
    {
        if (energySlider != null)
        {
            energySlider.maxValue = initialEnergy;
            energySlider.value = currentEnergy;
        }
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }
        UpdateUIDisplay();
    }

    public void AddScore(int amount)
    {
        currentScore += amount;
        UpdateUIDisplay();
    }

    public void TakeDamage(float damage)
    {
        currentHealth = Mathf.Max(0, currentHealth - damage);
        if (currentHealth <= 0)
        {
            Die();
        }
        UpdateUIDisplay();
    }

    private void Die()
    {
        Debug.Log("玩家已死亡！");
    }

    public void GrabEnergy(float energyAmount)
    {
        currentEnergy = Mathf.Min(initialEnergy, currentEnergy + energyAmount);
        UpdateUIDisplay();
    }

    public float CurrentLaunchSpeed => currentLaunchSpeed;
}