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
    [Tooltip("绳索宽度")]
    public float ropeWidth = 0.4f;
    [Tooltip("绳索材质")]
    public Material ropeMaterial;
    
    [HideInInspector]public Color ropeColor = Color.yellow;
    [HideInInspector]public string ropeSortingLayer = "Default";
    [HideInInspector]public int ropeSortingOrder = 50;

    [Header("基础属性")]
    [Tooltip("最大长度")]
    public float maxLength = 10f;
    [Tooltip("中心偏移距离")]
    public float standbyDistance = 3f;
    [Tooltip("基础旋转速度")]
    public float baseRotateSpeed = 90f;
    [Tooltip("发射力量")]
    public float baseLaunchSpeed = 10f;
    [Tooltip("回收力量")]
    public float baseRetrieveSpeed = 10f;
    
    [Header("加速速度")]
    [Tooltip("旋转加速速度")]
    public float accelerateRotateSpeed = 180f; 
    [Tooltip("钩爪发射加速速度")]
    public float accelerateLaunchSpeed = 18f;  
    [Tooltip("钩爪回收加速速度")]
    public float accelerateRetrieveSpeed = 22f;  

    [Header("过热参数")]
    [Tooltip("初始温度")]
    public float initialTemperature = 0f;
    [Tooltip("过热阈值温度")]
    public float overheatThreshold = 100f;
    [Tooltip("过热后最大运行时间")]
    public float maxOverheatTime = 5f;
    [Tooltip("旋转方向切换产生的热量")]
    public float rotateSwitchHeat = 5f;
    [Tooltip("加速时每秒产生的热量")]
    public float accelerateHeatPerSecond = 8f;
    [Tooltip("正常情况下每秒产生的热量")]
    public float normalHeatPerSecond = 2f;
    [Tooltip("冷却速率")]
    public float coolingRate = 10f;

    [Header("冷却CD")]
    public float rotateSwitchCD = 1f;
    public float accelerateCD = 2f;

    [Header("加速度属性（平滑过渡速度）")]
    public float rotationSmoothSpeed = 5f; // 旋转速度平滑过渡的加速度
    public float lengthSmoothSpeed = 5f; // 发射/回收速度平滑过渡的加速度
    public float switchDirSmoothSpeed = 8f; // 转向时的平滑过渡加速度

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
    [HideInInspector] public float currentTemperature;
    [HideInInspector] public float currentHealth;
    [HideInInspector] public OverheatState currentOverheatState = OverheatState.Normal;
    [HideInInspector] public float currentOverheatTime = 0f;
    [HideInInspector] public float spaceShipVelocity = 0f;

    private float currentRotateSpeed; // 当前旋转速度（带方向）
    private float currentLaunchSpeed;
    private float currentRetrieveSpeed;
    private int currentScore = 0;
    private bool isAccelerating = false;
    private float rotateSwitchCDTimer = 0f;
    private float accelerateCDTimer = 0f;
    private bool isSwitchingDir = false;
    private float targetRotateSpeed; // 旋转速度的目标值（用于平滑过渡）

    private LineRenderer ropeRenderer;
    private Transform hookTip;
    private HookTipCollisionHandler hookTipCollisionHandler;

    public static HookSystem Instance;

    // 物理参数
    public float k = 0.1f;
    public float a = 1f;
    public float c = 1f;
    public float ambientTemperature = 0f;

    private float grabbedMass = 0f; // 当前钩中的物体总质量

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

    private void SetupRope()
    {
        ropeRenderer = gameObject.GetComponent<LineRenderer>() ?? gameObject.AddComponent<LineRenderer>();
        ropeRenderer.enabled = true;

        if (ropeMaterial == null)
        {
            ropeMaterial = new Material(Shader.Find("Unlit/Color"));
            ropeMaterial.color = ropeColor;
        }
        else
        {
            if (ropeMaterial.shader.name != "Unlit/Color")
            {
                ropeMaterial.shader = Shader.Find("Unlit/Color");
            }
            ropeMaterial.color = new Color(ropeColor.r, ropeColor.g, ropeColor.b, 1f);
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

        // 初始化速度（使用基础速度）
        currentRotateSpeed = baseRotateSpeed;
        currentLaunchSpeed = baseLaunchSpeed;
        currentRetrieveSpeed = baseRetrieveSpeed;
        targetRotateSpeed = currentRotateSpeed; // 初始化目标旋转速度

        grabbedMass = 0f;

        UpdateUIDisplay();
    }
    
    private void Update()
    {
        UpdateCDTimers(Time.deltaTime);
        HandleInput();
        UpdateState(Time.deltaTime);
        UpdateSpeedSmoothing(Time.deltaTime); // 处理速度平滑过渡
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
        if (isShiftPressed && !isAccelerating && currentOverheatState == OverheatState.Normal && 
            accelerateCDTimer <= 0 && currentState == HookState.ReadyToLaunch)
        {
            isAccelerating = true;
        }
        else if (!isShiftPressed && isAccelerating)
        {
            isAccelerating = false;
            accelerateCDTimer = accelerateCD;
        }
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
        
        // 修改：方向切换时保持当前加速状态
        float targetSpeed = isAccelerating ? (baseRotateSpeed + accelerateRotateSpeed) : baseRotateSpeed;
        targetRotateSpeed = targetSpeed * (currentDir == RotationDir.Clockwise ? -1 : 1);
        
        currentDir = currentDir == RotationDir.Clockwise ? RotationDir.CounterClockwise : RotationDir.Clockwise;
        currentTemperature += rotateSwitchHeat;
        rotateSwitchCDTimer = rotateSwitchCD;
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

        // 计算热量变化
        float heatGenerationPower = CalculateHeatGenerationPower();
        float heatDissipationPower = CalculateHeatDissipationPower();
        float heatChange = (heatGenerationPower - heatDissipationPower) * deltaTime;
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

        // 修改：根据加速度计算目标旋转速度
        float targetSpeed = baseRotateSpeed;
        if (isAccelerating)
        {
            targetSpeed += accelerateRotateSpeed;
        }

        // 更新旋转目标速度（考虑方向）
        if (!isSwitchingDir) // 转向过程中不更新目标速度
        {
            targetRotateSpeed = targetSpeed * (currentDir == RotationDir.Clockwise ? 1 : -1);
        }
    }

    // 修改热量生成逻辑：加速时用accelerateHeatPerSecond，常态用normalHeatPerSecond
    private float CalculateHeatGenerationPower()
    {
        // 加速状态下直接使用加速放热参数
        if (isAccelerating)
        {
            return accelerateHeatPerSecond;
        }
        // 非加速状态下使用常态放热参数
        else
        {
            return normalHeatPerSecond;
        }
    }

    private float CalculateHeatDissipationPower()
    {
        float deltaTemperature = currentTemperature - ambientTemperature;
        return deltaTemperature * k;
    }

    // 修改：基于加速度的平滑过渡算法
    private void UpdateSpeedSmoothing(float deltaTime)
    {
        // 计算当前帧允许的最大速度变化（基于加速度）
        float maxSpeedChange = (isSwitchingDir ? switchDirSmoothSpeed : rotationSmoothSpeed) * deltaTime;
        
        // 计算当前速度与目标速度的差值
        float speedDiff = targetRotateSpeed - currentRotateSpeed;
        
        // 平滑过渡到目标速度
        if (Mathf.Abs(speedDiff) > maxSpeedChange)
        {
            currentRotateSpeed += Mathf.Sign(speedDiff) * maxSpeedChange;
        }
        else
        {
            currentRotateSpeed = targetRotateSpeed;
        }

        float launchStep = rotationSmoothSpeed * deltaTime;
        float targetLaunch = isAccelerating ? accelerateLaunchSpeed : baseLaunchSpeed;
        currentLaunchSpeed = Mathf.MoveTowards(currentLaunchSpeed, targetLaunch, launchStep);

        currentRetrieveSpeed = CalculateTargetRetrieveSpeed();

        if (isSwitchingDir && Mathf.Abs(currentRotateSpeed - targetRotateSpeed) < 0.1f)
        {
            isSwitchingDir = false;
        }
    }
    
    private float CalculateTargetRetrieveSpeed()
    {
        float massResistance = 1 + (grabbedMass * grabbedMass); 
    
        float baseSpeed = isAccelerating ? accelerateRetrieveSpeed : baseRetrieveSpeed;
    
        return Mathf.Max(baseSpeed * 0.1f, baseSpeed * 2f / massResistance);
    }

    private void UpdateRotation(float deltaTime)
    {
        currentRotation += currentRotateSpeed * deltaTime;
        currentRotation = (currentRotation % 360 + 360) % 360;
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
            hookTipCollisionHandler?.ResetGrabState();
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

    private void InitUI()
    {
        if (temperatureSlider != null)
        {
            temperatureSlider.minValue = initialTemperature;
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

    private void UpdateUIDisplay()
    {
        if (temperatureSlider != null)
        {
            float displayTemperature = Mathf.Clamp(currentTemperature, initialTemperature, overheatThreshold);
            temperatureSlider.value = displayTemperature;
        }
    
        if (temperaturePercentText != null)
        {
            temperaturePercentText.text = $"{currentTemperature:F1}°C";
        }
    
        if (healthSlider != null) 
            healthSlider.value = currentHealth;
    
        if (healthPercentText != null)
            healthPercentText.text = $"{(currentHealth / maxHealth) * 100f:F1}%";
    
        if (scoreText != null)
            scoreText.text = $"分数: {currentScore}";
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
        Debug.Log("寄了！");
    }

    public float CurrentLaunchSpeed => currentLaunchSpeed;

    private bool IsOutsideScreenBounds(Vector3 worldPosition)
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
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

    public void AddGrabbedMass(float mass)
    {
        grabbedMass += mass;
    }

    public void ResetGrabbedMass()
    {
        grabbedMass = 0f;
    }
}