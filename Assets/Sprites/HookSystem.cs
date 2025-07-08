using UnityEngine;  

public class HookSystem : MonoBehaviour
{
    // 钩爪状态：待发射、发射中、回收中
    public enum HookState { ReadyToLaunch, Launching, Retrieving }
    // 旋转方向：顺时针、逆时针
    public enum RotationDir { Clockwise, CounterClockwise }

    [Header("钩爪素材")]
    [Tooltip("钩爪尖端")]
    public GameObject hookTipPrefab;
    [Tooltip("绳索")]
    public LineRenderer hookLinePrefab;

    [Header("基础属性")]
    [Tooltip("钩爪最大伸长距离")]
    public float maxLength = 10f;

    [Tooltip("初始旋转速度")]
    public float baseRotateSpeed = 30f;

    [Tooltip("初始释放速度")]
    public float baseLaunchSpeed = 10f;

    [Tooltip("初始回收速度")]
    public float baseRetrieveSpeed = 10f;

    [Header("加速属性")]
    [Tooltip("加速时旋转速度")]
    public float accelerateRotateSpeed = 100f;

    [Tooltip("加速时释放速度")]
    public float accelerateLaunchSpeed = 20f;

    [Tooltip("加速时回收速度")]
    public float accelerateRetrieveSpeed = 24f;

    [Header("能量参数")]
    [Tooltip("初始总能量值")]
    public float initialEnergy = 100f;

    [Tooltip("切换旋转方向消耗能量")]
    public float rotateSwitchEnergyCost = 5f;

    [Tooltip("加速状态每秒消耗能量")]
    public float accelerateEnergyCostPerSecond = 8f;

    [Header("冷却CD")]
    [Tooltip("旋转方向切换")]
    public float rotateSwitchCD = 1f;

    [Tooltip("加速")]
    public float accelerateCD = 2f;

    [Header("操作延迟")]
    [Tooltip("旋转方向切换前摇时间（秒）")]
    public float rotateSwitchDelay = 0.2f;

    
    [HideInInspector] public HookState currentState = HookState.ReadyToLaunch; // 当前钩爪状态
    [HideInInspector] public RotationDir currentDir = RotationDir.Clockwise; // 当前旋转方向
    [HideInInspector] public float currentLength = 0f; // 当前钩爪长度
    [HideInInspector] public float currentRotation = 0f; // 当前旋转角度（度）
    [HideInInspector] public float currentEnergy; // 当前剩余能量
    private bool isAccelerating = false; // 是否处于加速状态
    private float rotateSwitchTimer = 0f; // 旋转切换前摇计时器
    private bool isSwitchingDir = false; // 是否正在切换旋转方向
    private float rotateSwitchCDTimer = 0f; // 旋转切换冷却计时器
    private float accelerateCDTimer = 0f; // 加速冷却计时器

    
    [HideInInspector] private LineRenderer hookLine;
    [HideInInspector] private Transform hookTip;

    private void Start()
    {
        InitFromPrefabs();
        Init();
    }

    private void InitFromPrefabs()
    {
        if (hookTipPrefab != null)
        {
            GameObject tipInstance = Instantiate(hookTipPrefab, transform);
            hookTip = tipInstance.transform;
            hookTip.localPosition = Vector3.zero;
        }

        if (hookLinePrefab != null)
        {
            hookLine = Instantiate(hookLinePrefab, transform);
            hookLine.positionCount = 2;
        }
    }

    private void Update()
    {
        UpdateCDTimers(Time.deltaTime);
        HandleInput();
        UpdateState(Time.deltaTime);
        UpdateHookVisual();
    }

    public void Init()
    {
        currentState = HookState.ReadyToLaunch;
        currentDir = RotationDir.Clockwise;
        currentLength = 0f;
        currentRotation = 0f;
        currentEnergy = initialEnergy;
        rotateSwitchCDTimer = 0f;
        accelerateCDTimer = 0f;
    }

    private void UpdateCDTimers(float deltaTime) 
    {
        if (rotateSwitchCDTimer > 0) rotateSwitchCDTimer -= deltaTime;
        if (accelerateCDTimer > 0) accelerateCDTimer -= deltaTime;
    }

    private void HandleInput() // 输入检测
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
            Debug.Log($"加速状态持续消耗: -{cost:F2}, 剩余能量: {currentEnergy:F2}");
            if (currentEnergy <= 0) isAccelerating = false;
        }
    }

    private void SwitchLaunchOrRetrieve() // 切换钩爪状态
    {
        if (currentState == HookState.ReadyToLaunch)
        {
            currentState = HookState.Launching;
        }
    }

    private void StartSwitchRotationDir() // 旋转方向切换
    {
        isSwitchingDir = true;
        rotateSwitchTimer = rotateSwitchDelay;
        currentEnergy -= rotateSwitchEnergyCost;
        rotateSwitchCDTimer = rotateSwitchCD;
        Debug.Log($"切换方向消耗能量: -{rotateSwitchEnergyCost}, 剩余能量: {currentEnergy}");
    }

    private void UpdateRotation(float deltaTime) // 更新钩爪旋转角度
    {
        if (isSwitchingDir)
        {
            rotateSwitchTimer -= deltaTime;
            if (rotateSwitchTimer <= 0)
            {
                currentDir = currentDir == RotationDir.Clockwise ? RotationDir.CounterClockwise : RotationDir.Clockwise;
                isSwitchingDir = false;
            }
            return;
        }

        float speed = isAccelerating ? accelerateRotateSpeed : baseRotateSpeed;
        currentRotation += (currentDir == RotationDir.Clockwise ? speed : -speed) * deltaTime;
        currentRotation = (currentRotation % 360 + 360) % 360;
    }

    private void UpdateLaunching(float deltaTime) // 伸长钩爪
    {
        float speed = isAccelerating ? accelerateLaunchSpeed : baseLaunchSpeed;
        currentLength += speed * deltaTime;
        if (currentLength >= maxLength)
        {
            currentLength = maxLength;
            currentState = HookState.Retrieving;
        }
    }

    private void UpdateRetrieving(float deltaTime) // 缩短钩爪
    {
        float speed = isAccelerating ? accelerateRetrieveSpeed : baseRetrieveSpeed;
        currentLength -= speed * deltaTime;
        if (currentLength <= 0)
        {
            currentLength = 0;
            currentState = HookState.ReadyToLaunch;
        }
    }

    private void UpdateHookVisual() // 更新绳索和钩尖位置
    {
        if (hookLine == null || hookTip == null) return;

        Vector2 direction = new Vector2(
            Mathf.Cos(currentRotation * Mathf.Deg2Rad),
            Mathf.Sin(currentRotation * Mathf.Deg2Rad)
        ).normalized;
        Vector2 hookTipPos = (Vector2)transform.position + direction * currentLength;
        hookTip.position = hookTipPos;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        hookTip.rotation = Quaternion.Euler(0, 0, angle);

        hookLine.SetPosition(0, transform.position);
        hookLine.SetPosition(1, hookTipPos);
    }
}    