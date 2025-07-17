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
    [Tooltip("钩爪尖端")]
    public GameObject hookTipPrefab;
    [Tooltip("绳索")]
    public LineRenderer hookLinePrefab;

    [Header("基础属性")]
    [Tooltip("钩爪最大伸长距离")]
    public float maxLength = 10f;
    
    [Tooltip("待机时与中心的距离")]
    public float standbyDistance = 2f;

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

    [Header("加速度")]
    [Tooltip("旋转加速度")]
    public float rotationSmoothSpeed = 5f;
    
    [Tooltip("钩爪加速度")]
    public float lengthSmoothSpeed = 5f;
    
    [Tooltip("方向切换加速度")]
    public float switchDirSmoothSpeed = 8f;

    [Header("UI显示")]
    [Tooltip("显示能量的滑块UI")]
    public Slider energySlider;
    
    [Tooltip("显示分数的文本UI（TextMeshPro）")]
    public TextMeshProUGUI scoreText;
    
    [Tooltip("显示能量百分比的文本UI（TextMeshPro）")]
    public TextMeshProUGUI energyPercentText;

    [HideInInspector] public HookState currentState = HookState.ReadyToLaunch;
    [HideInInspector] public RotationDir currentDir = RotationDir.Clockwise;
    [HideInInspector] public float currentLength = 0f;
    [HideInInspector] public float currentRotation = 0f;
    [HideInInspector] public float currentEnergy;

    [Tooltip("当前旋转速度（受加速和方向影响）")]
    private float currentRotateSpeed;
    
    [Tooltip("当前发射速度（受加速影响）")]
    private float currentLaunchSpeed;
    
    [Tooltip("当前回收速度（受加速影响）")]
    private float currentRetrieveSpeed;

    private int currentScore = 0;
    private bool isAccelerating = false;
    private float rotateSwitchCDTimer = 0f;
    private float accelerateCDTimer = 0f;
    private bool isSwitchingDir = false;

    private LineRenderer hookLine;
    private Transform hookTip;
    private HookTipCollisionHandler hookTipCollisionHandler;

    private void Start()
    {
        InitFromPrefabs();
        Init();
        hookTipCollisionHandler = hookTip.GetComponent<HookTipCollisionHandler>();
        hookTipCollisionHandler.hookSystem = this;
        InitUI();
    }

    private void InitUI()
    {
        if (energySlider != null)
        {
            energySlider.maxValue = initialEnergy;
            energySlider.value = currentEnergy;
        }

        if (energyPercentText != null)
        {
            float percent = (currentEnergy / initialEnergy) * 100f;
            energyPercentText.text = $"{percent:F1}%";
        }

        if (scoreText != null)
        {
            scoreText.text = $"分数: {currentScore}";
        }
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
        UpdateUIDisplay();
    }

    public void Init()
    {
        currentState = HookState.ReadyToLaunch;
        currentDir = RotationDir.Clockwise;
        currentLength = standbyDistance;
        currentRotation = 0f;
        currentEnergy = initialEnergy;
        currentScore = 0;
        rotateSwitchCDTimer = 0f;
        accelerateCDTimer = 0f;

        currentRotateSpeed = baseRotateSpeed;
        currentLaunchSpeed = baseLaunchSpeed;
        currentRetrieveSpeed = baseRetrieveSpeed;

        if (energySlider != null) energySlider.value = currentEnergy;
        if (scoreText != null) scoreText.text = $"分数: {currentScore}";
        if (energyPercentText != null)
        {
            energyPercentText.text = $"{(currentEnergy / initialEnergy) * 100f:F1}%";
        }
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

        currentRotateSpeed = Mathf.Lerp(currentRotateSpeed, targetRotate * (currentDir == RotationDir.Clockwise ? 1 : -1), deltaTime * (isSwitchingDir ? switchDirSmoothSpeed : rotationSmoothSpeed));
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
            HandleGrabbedEnergy();
        }
    }

    public void RetrieveHook()
    {
        if (currentState == HookState.Launching)
        {
            currentState = HookState.Retrieving;
        }
    }

    private void HandleGrabbedEnergy()
    {
        GameObject grabbedEnergy = hookTipCollisionHandler.GetGrabbedEnergy();
        if (grabbedEnergy != null)
        {
            Energy energyComponent = grabbedEnergy.GetComponent<Energy>();
            if (energyComponent != null)
            {
                GrabEnergy(energyComponent.energyAmount);
                currentScore += Mathf.RoundToInt(energyComponent.scoreAmount);
            }

            Destroy(grabbedEnergy);
            hookTipCollisionHandler.ReleaseGrabbedEnergy();
        }
    }

    public void GrabEnergy(float energyAmount)
    {
        currentEnergy = Mathf.Min(initialEnergy, currentEnergy + energyAmount);
    }

    private void UpdateHookVisual()
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

    private void UpdateUIDisplay()
    {
        if (energySlider != null)
        {
            energySlider.value = currentEnergy;
        }

        if (energyPercentText != null)
        {
            float energyPercent = (currentEnergy / initialEnergy) * 100f;
            energyPercentText.text = $"{energyPercent:F1}%";
        }

        if (scoreText != null)
        {
            scoreText.text = $"分数: {currentScore}";
        }
    }
}