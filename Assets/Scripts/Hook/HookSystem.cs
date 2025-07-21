using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class HookSystem : MonoBehaviour
{
    // 钩爪状态：待发射、发射中、回收中
    public enum HookState { ReadyToLaunch, Launching, Retrieving }
    // 旋转方向：顺时针、逆时针
    public enum RotationDir { Clockwise, CounterClockwise }
    // 过热系统状态：过热中，正常运行，冷却中
    public enum OverheatState { Overheating, Normal, Cooling }

    [Header("钩爪素材")]
    public GameObject hookTipPrefab; // 钩爪尖端预制体

    [Header("绳索设置")]
    public Material ropeMaterial; // 绳索材质
    public Color ropeColor = Color.yellow;
    [Range(0.1f, 1f)] public float ropeWidth = 0.4f; // 增大宽度
    public string ropeSortingLayer = "Default";
    public int ropeSortingOrder = 50; //确保在默认层级最上层

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

    [Header("过热参数")]
    public float initialTemperature = 0f; // 初始温度
    public float overheatThreshold = 100f; // 过热阈值温度
    public float maxOverheatTime = 5f; // 过热后最大运行时间
    public float rotateSwitchHeat = 5f; // 旋转方向切换产生的热量
    public float accelerateHeatPerSecond = 8f; // 加速时每秒产生的热量
    public float coolingRate = 10f; // 冷却速率

    [Header("冷却CD")]
    public float rotateSwitchCD = 1f;
    public float accelerateCD = 2f;

    [Header("加速度")]
    public float rotationSmoothSpeed = 5f;
    public float lengthSmoothSpeed = 5f;
    public float switchDirSmoothSpeed = 8f;

    [Header("UI显示")]
    public Slider temperatureSlider;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI temperaturePercentText;
    public Slider healthSlider;
    public TextMeshProUGUI healthPercentText;

    [Header("玩家生命属性")]
    public float maxHealth = 100f;

    [Header("物理属性")]
    public float hookTipMass = 0.5f;

    [Header("屏幕边界设置")]
    public Camera mainCamera;

    [HideInInspector] public HookState currentState = HookState.ReadyToLaunch;
    [HideInInspector] public RotationDir currentDir = RotationDir.Clockwise;
    [HideInInspector] public float currentLength = 0f;
    [HideInInspector] public float currentRotation = 0f;
    [HideInInspector] public float currentTemperature; // 当前温度
    [HideInInspector] public float currentHealth;
    [HideInInspector] public OverheatState currentOverheatState = OverheatState.Normal; // 当前过热状态
    [HideInInspector] public float currentOverheatTime = 0f; // 当前过热时间

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

    // 新增参数
    public float k = 0.1f; // 发热功率系数
    public float a = 1f; // 调整拖拽发热功率的系数
    public float c = 1f; // 飞船热容
    public float ambientTemperature = 0f; // 环境温度

    private float grabbedMass = 0f; // 新增：当前钩中的物体总质量

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
        ropeRenderer.enabled = true;

        if (ropeMaterial == null)
        {
            ropeMaterial = new Material(Shader.Find("Unlit/Color"));
            ropeMaterial.color = ropeColor; // 用用户设置的颜色
        }
        else
        {
            if (ropeMaterial.shader.name != "Unlit/Color")
            {
                ropeMaterial.shader = Shader.Find("Unlit/Color");
            }
            ropeMaterial.color = new Color(ropeColor.r, ropeColor.g, ropeColor.b, 1f); // 强制不透明
        }

        ropeRenderer.material = ropeMaterial;
        ropeRenderer.startColor = new Color(ropeColor.r, ropeColor.g, ropeColor.b, 1f);
        ropeRenderer.endColor = new Color(ropeColor.r, ropeColor.g, ropeColor.b, 1f);
        ropeRenderer.widthMultiplier = ropeWidth;
        ropeRenderer.positionCount = 2;
        ropeRenderer.useWorldSpace = true;

        ropeRenderer.sortingLayerName = ropeSortingLayer;
        ropeRenderer.sortingOrder = ropeSortingOrder;

        ropeRenderer.allowOcclusionWhenDynamic = false;
        ropeRenderer.receiveShadows = false;
        ropeRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    // 初始化钩爪尖端
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

    // 初始化状态
    public void Init()
    {
        currentState = HookState.ReadyToLaunch;
        currentDir = RotationDir.Clockwise;
        currentLength = standbyDistance; 
        currentRotation = 0f;
        currentTemperature = initialTemperature;
        currentHealth = maxHealth;
        currentScore = 0;
        currentOverheatState = OverheatState.Normal;
        currentOverheatTime = 0f;

        rotateSwitchCDTimer = 0f;
        accelerateCDTimer = 0f;
        isAccelerating = false;
        isSwitchingDir = false;

        currentRotateSpeed = baseRotateSpeed;
        currentLaunchSpeed = baseLaunchSpeed;
        currentRetrieveSpeed = baseRetrieveSpeed;

        grabbedMass = 0f; // 初始化钩中物体总质量

        UpdateUIDisplay();
    }
    
    private void Update()
    {
        UpdateCDTimers(Time.deltaTime);
        HandleInput();
        UpdateState(Time.deltaTime);
        UpdateHookPosition();
        
        UpdateUIDisplay();
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

        if (Input.GetKeyDown(KeyCode.A) && !isSwitchingDir && currentOverheatState == OverheatState.Normal &&
            rotateSwitchCDTimer <= 0 && currentState == HookState.ReadyToLaunch)
        {
            StartSwitchRotationDir();
        }

        bool isShiftPressed = Input.GetKey(KeyCode.LeftShift);
        if (isShiftPressed && !isAccelerating && currentOverheatState == OverheatState.Normal && accelerateCDTimer <= 0)
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
                if (IsOutsideScreenBounds(hookTip.position))
                {
                    RetrieveHook();
                }
                break;
            case HookState.Retrieving:
                UpdateRetrieving(deltaTime);
                break;
        }

        // 计算发热功率
        float heatGenerationPower = CalculateHeatGenerationPower();
        // 计算散热功率
        float heatDissipationPower = CalculateHeatDissipationPower();

        // 计算热量变化
        float heatChange = (heatGenerationPower - heatDissipationPower) * deltaTime;
        // 计算温度变化
        float temperatureChange = heatChange / c;

        currentTemperature += temperatureChange;

        switch (currentOverheatState)
        {
            case OverheatState.Normal:
                if (currentTemperature >= overheatThreshold)
                {
                    currentOverheatState = OverheatState.Overheating;
                    currentOverheatTime = 0f;
                    isAccelerating = false;
                    accelerateCDTimer = accelerateCD;
                }
                break;
            case OverheatState.Overheating:
                currentOverheatTime += deltaTime;
                if (currentOverheatTime >= maxOverheatTime)
                {
                    currentOverheatState = OverheatState.Cooling;
                }
                break;
            case OverheatState.Cooling:
                currentTemperature = Mathf.Max(0, currentTemperature - coolingRate * deltaTime);
                if (currentTemperature <= 0)
                {
                    currentOverheatState = OverheatState.Normal;
                }
                break;
        }

        // 计算旋转、发射速度目标值（保留原有逻辑）
        float targetRotate = isAccelerating ? accelerateRotateSpeed : baseRotateSpeed;
        float targetLaunch = isAccelerating ? accelerateLaunchSpeed : baseLaunchSpeed;

        // 计算回收速度目标值（核心修改：质量直接影响基础速度，加速在此基础上叠加）
        float massInfluence = 1 + grabbedMass; // 质量影响系数（可调整为1 + 0.5*grabbedMass弱化影响）
        float massAdjustedBase = baseRetrieveSpeed / massInfluence; // 质量调整后的基础回收速度
        float massAdjustedAccelerate = accelerateRetrieveSpeed / massInfluence; // 质量调整后的加速回收速度
        float targetRetrieve = isAccelerating ? massAdjustedAccelerate : massAdjustedBase;
        targetRetrieve = Mathf.Max(1f, targetRetrieve); // 确保最低速度，避免无法回收

        // 直接设置为目标速度（移除平滑过渡）
        currentRotateSpeed = targetRotate * (currentDir == RotationDir.Clockwise ? 1 : -1);
        currentLaunchSpeed = targetLaunch;
        currentRetrieveSpeed = targetRetrieve;
    }

    private float CalculateHeatGenerationPower()
    {
        float speed;
        switch (currentState)
        {
            case HookState.ReadyToLaunch:
                speed = Mathf.Abs(currentRotateSpeed);
                break;
            case HookState.Launching:
                speed = currentLaunchSpeed;
                break;
            case HookState.Retrieving:
                speed = currentRetrieveSpeed;
                break;
            default:
                speed = 0f;
                break;
        }

        // 这里简单假设没有拖拽重物，若需要考虑拖拽重物，可添加相应逻辑
        return k * speed;
    }

    private float CalculateHeatDissipationPower()
    {
        float deltaTemperature = currentTemperature - ambientTemperature;
        return deltaTemperature * k;
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
        currentTemperature += rotateSwitchHeat;
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
            hookTipCollisionHandler?.OnRetrieveComplete();
        }
        else
        {
            // 在钩回过程中持续增加热度
            float heatPerUnitMass = hookTipCollisionHandler.heatPerUnitMass;
            float heatGenerated = grabbedMass * heatPerUnitMass * deltaTime;
            currentTemperature += heatGenerated;
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

        ropeRenderer.positionCount = 2;
        ropeRenderer.SetPosition(0, startPos);
        ropeRenderer.SetPosition(1, endPos);

        ropeRenderer.widthMultiplier = ropeWidth;
        ropeRenderer.startColor = new Color(ropeColor.r, ropeColor.g, ropeColor.b, 1f);
        ropeRenderer.endColor = new Color(ropeColor.r, ropeColor.g, ropeColor.b, 1f);
        ropeRenderer.enabled = true;
    }

    // 以下UI和状态方法完全保留
    private void UpdateUIDisplay()
    {
        if (temperatureSlider != null) temperatureSlider.value = currentTemperature;
        if (temperaturePercentText != null)
            temperaturePercentText.text = $"{(currentTemperature / overheatThreshold) * 100f:F1}%";
        if (healthSlider != null) healthSlider.value = currentHealth;
        if (healthPercentText != null)
            healthPercentText.text = $"{(currentHealth / maxHealth) * 100f:F1}%";
        if (scoreText != null)
            scoreText.text = $"分数: {currentScore}";
    }

    private void InitUI()
    {
        if (temperatureSlider != null)
        {
            temperatureSlider.maxValue = overheatThreshold;
            temperatureSlider.value = currentTemperature;
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

    private void Die() { }  // 待实现

    public void GrabCooling(float coolingAmount)
    {
        currentTemperature = Mathf.Max(0, currentTemperature - coolingAmount);
        if (currentTemperature <= 0 && currentOverheatState == OverheatState.Cooling)
        {
            currentOverheatState = OverheatState.Normal;
        }
        UpdateUIDisplay();
    }

    public float CurrentLaunchSpeed => currentLaunchSpeed;

    //检测位置是否超出屏幕边界
    private bool IsOutsideScreenBounds(Vector3 worldPosition)
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main; // 如果未赋值则尝试获取主相机
            if (mainCamera == null) return false;
        }

        Vector3 screenPosition = mainCamera.WorldToScreenPoint(worldPosition);

        float buffer = 10f;
        return screenPosition.x < -buffer || 
               screenPosition.x > Screen.width + buffer || 
               screenPosition.y < -buffer || 
               screenPosition.y > Screen.height + buffer;
    }
    
    public void AddHeat(float heat)
    {
        currentTemperature += heat;
    }

    // 新增方法：添加钩中物体的质量
    public void AddGrabbedMass(float mass)
    {
        grabbedMass += mass;
    }

    // 新增方法：重置钩中物体的质量
    public void ResetGrabbedMass()
    {
        grabbedMass = 0f;
    }
}