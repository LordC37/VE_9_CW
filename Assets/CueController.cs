using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class CueController : MonoBehaviour
{
    
    [Header("References")]
    public Transform cueEnd;
    public Transform pivotPoint;
    public GameObject whiteBall;
    public XRController xrController; // VR控制器引用

    [Header("Particle Effects")]
    public GameObject standardHitParticle; // 标准击球粒子效果
    public GameObject assistedHitParticle; // 辅助模式击球粒子效果
    public GameObject horizontalHitParticle; // 水平模式击球粒子效果

    [Header("Settings")]
    public float rotationSpeed = 50f;
    public float maxPower = 1000000f;
    public float powerMultiplier = 1000f;
    public float resetSpeed = 2f;
    public float minDistanceFromBall = 0.02f;
    public float maxDistanceFromBall = 1.0f;
    public bool useVRControls = true; // 是否使用VR控制
    
    public enum CueControlType
    {
        Horizontal, // 只能水平移动，无瞄准线 - 给A球杆
        Standard,   // 标准控制，现有的 - 给B球杆
        Assisted    // 辅助瞄准，自动对齐 - 给C球杆
    }
    
    [Header("Control Type")]
    public CueControlType controlType = CueControlType.Standard;
    public bool showAimingLine = true; // 是否显示瞄准线
    public float autoAimDistance = 0.5f; // 自动瞄准触发距离

    public InputHelpers.Button rotationLockButton = InputHelpers.Button.TriggerButton; // 默认使用A按钮锁定旋转
    
    [Header("Debug")]
    public bool canStrike = true;
    
    private bool isAiming = false;
    private bool isPowerAdjusting = false;
    private float currentPower = 0f;
    private Vector3 lastControllerPosition;
    private bool isResetting = false;
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
    private bool isRotationLocked = false; // 是否锁定旋转
    private Vector3 lockedDirection; // 锁定时的方向向量
    private LineRenderer aimingLine; // 瞄准线
    private Quaternion initialRotation; // 保存初始旋转
    private Transform horizontalAttachTransform; // 水平的附加变换

    void Start()
    {
        // 保存初始旋转
        initialRotation = transform.rotation;
        
        // 尝试找到白球
        if (whiteBall == null)
        {
            whiteBall = GameObject.FindGameObjectWithTag("WhiteBall");
            if (whiteBall == null)
            {
                // 如果没有标签，尝试通过名称找到
                whiteBall = GameObject.Find("Ball1");
                Debug.Log("使用Ball1作为白球");
            }
        }
        
        // 创建一个水平的附加变换
        GameObject attachPoint = new GameObject("CueAttachPoint");
        attachPoint.transform.parent = transform;
        attachPoint.transform.localPosition = Vector3.zero;
        // 确保它是水平朝向的，通常球杆的前方是Z轴
        attachPoint.transform.localRotation = Quaternion.identity;
        horizontalAttachTransform = attachPoint.transform;
        
        // 获取XRGrabInteractable组件
        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grabInteractable == null && useVRControls)
        {
            grabInteractable = gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            Debug.Log("添加了XRGrabInteractable组件");
        }
        
        // 设置抓取点和约束
        if (grabInteractable != null)
        {
            // 使用我们的水平附加变换
            grabInteractable.attachTransform = horizontalAttachTransform;
            
            // 关键设置：限制旋转
            grabInteractable.trackRotation = false; // 不跟踪控制器旋转
            
            // 添加事件监听
            grabInteractable.selectEntered.AddListener(OnGrab);
            grabInteractable.selectExited.AddListener(OnRelease);
        }
        
        // 设置参考点
        if (pivotPoint == null)
            pivotPoint = transform;
        
        if (cueEnd == null)
        {
            // 尝试查找子对象作为杆头
            Transform[] children = GetComponentsInChildren<Transform>();
            foreach (Transform child in children)
            {
                if (child.name.Contains("Tip") || child.name.Contains("End"))
                {
                    cueEnd = child;
                    break;
                }
            }
            
            // 如果还是找不到，创建一个
            if (cueEnd == null)
            {
                GameObject endObj = new GameObject("CueEnd");
                endObj.transform.parent = transform;
                endObj.transform.localPosition = new Vector3(0, 0, 1); // 假设Z轴是杆的朝向
                cueEnd = endObj.transform;
                Debug.Log("创建了CueEnd对象");
            }
        }
        
        // 记录初始控制器位置
        if (xrController != null)
        {
            lastControllerPosition = xrController.transform.position;
        }
        
        // 设置瞄准线
        SetupAimingLine();
    }
    
    void SetupAimingLine()
    {
        // 只有当需要显示瞄准线时才创建
        if (showAimingLine && controlType != CueControlType.Horizontal)
        {
            // 创建瞄准线
            if (aimingLine == null)
            {
                GameObject lineObj = new GameObject("AimingLine");
                lineObj.transform.parent = transform;
                aimingLine = lineObj.AddComponent<LineRenderer>();
                aimingLine.startWidth = 0.005f;
                aimingLine.endWidth = 0.001f;
                aimingLine.material = new Material(Shader.Find("Sprites/Default"));
                aimingLine.startColor = Color.red;
                aimingLine.endColor = Color.yellow;
                aimingLine.positionCount = 2;
            }
        }
        else if (aimingLine != null)
        {
            Destroy(aimingLine.gameObject);
            aimingLine = null;
        }
    }
    
    void Update()
    {
        if (whiteBall == null) return;
        
        // 强制保持水平
        EnforceHorizontalOrientation();
        
        // 根据控制类型处理
        switch (controlType)
        {
            case CueControlType.Horizontal:
                UpdateHorizontalControls();
                break;
                
            case CueControlType.Assisted:
                UpdateAssistedControls();
                break;
                
            case CueControlType.Standard:
            default:
                if (useVRControls)
                    UpdateVRControls();
                else
                    UpdateMouseControls();
                break;
        }
        
        // 如果正在重置
        if (isResetting)
        {
            ResetCue();
        }
        
        // 更新瞄准线
        UpdateAimingLine();
    }
    
    // 新添加的函数：强制保持水平方向
    // 修改 EnforceHorizontalOrientation 函数
    void EnforceHorizontalOrientation()
    {
        if (isAiming || (grabInteractable != null && grabInteractable.isSelected))
        {
            // 保存当前位置
            Vector3 currentPosition = transform.position;
            
            // 获取当前控制器的朝向
            Vector3 controllerForward = Vector3.forward;
            if (xrController != null)
            {
                controllerForward = xrController.transform.forward;
            }
            
            // 将控制器的朝向投影到水平面上
            controllerForward.y = 0;
            if (controllerForward.magnitude > 0.01f)
            {
                controllerForward.Normalize();
                
                // 计算水平朝向的旋转
                Quaternion horizontalRotation = Quaternion.LookRotation(controllerForward);
                
                // 应用X轴270度的旋转使球杆水平，并使用控制器的水平朝向
                transform.rotation = horizontalRotation * Quaternion.Euler(90, 0, 0);
            }
            
            // 恢复位置
            transform.position = currentPosition;
            
            // 确保高度适合打球
            if (whiteBall != null)
            {
                Vector3 position = transform.position;
                position.y = whiteBall.transform.position.y + 0.1f;
                transform.position = position;
            }
        }
    }
    
    void UpdateAimingLine()
    {
        if (aimingLine != null && whiteBall != null && isAiming)
        {
            Vector3 direction = (whiteBall.transform.position - cueEnd.position).normalized;
            aimingLine.SetPosition(0, cueEnd.position);
            aimingLine.SetPosition(1, whiteBall.transform.position + direction * 2f);
            aimingLine.enabled = true;
        }
        else if (aimingLine != null)
        {
            aimingLine.enabled = false;
        }
    }
    
    void UpdateHorizontalControls()
    {
        // 水平模式 - 只允许水平移动和旋转
        if (xrController == null) return;
        
        bool triggerPressed = false;
        if (xrController.inputDevice.TryGetFeatureValue(CommonUsages.triggerButton, out bool pressed))
        {
            triggerPressed = pressed;
        }
        
        if (isAiming)
        {
            // 控制器位置变化
            Vector3 currentPosition = xrController.transform.position;
            Vector3 movement = currentPosition - lastControllerPosition;
            
            // 获取当前水平面
            Vector3 horizontalMovement = new Vector3(movement.x, 0, movement.z);
            
            // 移动球杆，但保持高度固定
            Vector3 newPosition = pivotPoint.position + horizontalMovement;
            newPosition.y = whiteBall.transform.position.y + 0.05f; // 略高于球的位置
            pivotPoint.position = newPosition;
            
            // 始终朝向白球
            if (Vector3.Distance(pivotPoint.position, whiteBall.transform.position) > 0.1f)
            {
                Vector3 directionToWhiteBall = (whiteBall.transform.position - pivotPoint.position).normalized;
                // 只调整水平旋转，保持球杆水平
                directionToWhiteBall.y = 0;
                if (directionToWhiteBall != Vector3.zero)
                {
                    pivotPoint.rotation = Quaternion.LookRotation(directionToWhiteBall);
                }
            }
            
            // 击球
            if (triggerPressed && canStrike)
            {
                // 计算白球距离作为力量因子
                float distance = Vector3.Distance(pivotPoint.position, whiteBall.transform.position);
                currentPower = Mathf.Clamp(distance * powerMultiplier, 0, maxPower);
                Strike();
            }
            
            lastControllerPosition = currentPosition;
        }
    }
    
    void UpdateAssistedControls()
    {
        // 辅助瞄准模式 - 接近白球会自动对齐，提供瞄准辅助
        if (xrController == null) return;
        
        bool triggerPressed = false;
        if (xrController.inputDevice.TryGetFeatureValue(CommonUsages.triggerButton, out bool pressed))
        {
            triggerPressed = pressed;
        }
        
        if (isAiming)
        {
            Vector3 currentPosition = xrController.transform.position;
            
            // 检查是否接近白球
            float distanceToBall = Vector3.Distance(pivotPoint.position, whiteBall.transform.position);
            
            if (distanceToBall < autoAimDistance)
            {
                // 自动瞄准
                // 获取当前控制器朝向
                Vector3 controllerForward = xrController.transform.forward;
                
                // 保持水平方向但使用控制器的前向
                controllerForward.y = 0;
                controllerForward.Normalize();
                
                // 设置球杆位置为白球后方
                pivotPoint.position = whiteBall.transform.position - controllerForward * minDistanceFromBall * 2f;
                
                // 设置朝向指向白球
                pivotPoint.rotation = Quaternion.LookRotation(controllerForward);
                
                // 显示辅助提示
                if (aimingLine != null)
                {
                    aimingLine.startColor = Color.green; // 绿色表示已自动瞄准
                }
            }
            else
            {
                // 正常移动
                Vector3 movement = currentPosition - lastControllerPosition;
                pivotPoint.position += movement;
                
                // 根据控制器方向调整杆的方向，但仅限于水平面
                Vector3 controllerForward = xrController.transform.forward;
                controllerForward.y = 0;
                controllerForward.Normalize();
                
                if (controllerForward != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(controllerForward);
                    pivotPoint.rotation = Quaternion.Lerp(pivotPoint.rotation, 
                                                        targetRotation, 
                                                        Time.deltaTime * 5f);
                }
                
                if (aimingLine != null)
                {
                    aimingLine.startColor = Color.red; // 红色表示未自动瞄准
                }
            }
            
            // 击球
            if (triggerPressed && canStrike)
            {
                // 如果在自动瞄准范围内，给予力量加成
                if (distanceToBall < autoAimDistance)
                {
                    currentPower = maxPower * 0.7f; // 70%的最大力量，确保击球有效
                }
                else
                {
                    // 基于控制器速度计算力量
                    Vector3 velocity = Vector3.zero;
                    if (xrController.inputDevice.TryGetFeatureValue(CommonUsages.deviceVelocity, out velocity))
                    {
                        currentPower = Mathf.Clamp(velocity.magnitude * powerMultiplier, 0, maxPower);
                    }
                    else
                    {
                        currentPower = maxPower * 0.5f; // 默认50%力量
                    }
                }
                
                Strike();
            }
            
            lastControllerPosition = currentPosition;
        }
    }
    
    void UpdateVRControls()
    {
        if (xrController == null) return;
        
        bool triggerPressed = false;
        if (xrController.inputDevice.TryGetFeatureValue(CommonUsages.triggerButton, out bool pressed))
        {
            triggerPressed = pressed;
        }
        
        // 检查是否按下旋转锁定按钮
        bool lockButtonPressed = false;

        InputFeatureUsage<bool> buttonFeature;
        switch (rotationLockButton)
        {
            case InputHelpers.Button.TriggerButton:
                buttonFeature = CommonUsages.triggerButton;
                break;
            case InputHelpers.Button.GripButton:
                buttonFeature = CommonUsages.gripButton;
                break;
            case InputHelpers.Button.PrimaryButton:
                buttonFeature = CommonUsages.primaryButton;
                break;
            case InputHelpers.Button.SecondaryButton:
                buttonFeature = CommonUsages.secondaryButton;
                break;
            default:
                buttonFeature = CommonUsages.triggerButton;
                break;
        }
        xrController.inputDevice.TryGetFeatureValue(buttonFeature, out lockButtonPressed);
        
        if (lockButtonPressed && !isRotationLocked)
        {
            isRotationLocked = true;
            lockedDirection = pivotPoint.forward;
        }
        else if (!lockButtonPressed)
        {
            isRotationLocked = false;
        }
        
        if (isAiming)
        {
            // 获取当前控制器位置
            Vector3 currentPosition = xrController.transform.position;
            
            // 计算移动量
            Vector3 movement = currentPosition - lastControllerPosition;
            
            // 更新位置（保持运动水平）
            Vector3 horizontalMovement = new Vector3(movement.x, 0, movement.z);
            pivotPoint.position += horizontalMovement;
            
            // 如果没有锁定旋转，则更新旋转（仅水平方向）
            if (!isRotationLocked)
            {
                Vector3 controllerForward = xrController.transform.forward;
                controllerForward.y = 0; // 保持水平
                controllerForward.Normalize();
                
                if (controllerForward != Vector3.zero)
                {
                    pivotPoint.rotation = Quaternion.LookRotation(controllerForward);
                }
            }
            else
            {
                // 锁定方向
                pivotPoint.rotation = Quaternion.LookRotation(lockedDirection); 
            }
            
            // 确保球杆保持水平
            Vector3 currentPos = pivotPoint.position;
            currentPos.y = whiteBall.transform.position.y + 0.01f; // 略高于球的位置
            pivotPoint.position = currentPos;
            
            // 击球控制
            if (triggerPressed && canStrike)
            {
                // 基于控制器速度计算力量
                Vector3 velocity = Vector3.zero;
                if (xrController.inputDevice.TryGetFeatureValue(CommonUsages.deviceVelocity, out velocity))
                {
                    currentPower = Mathf.Clamp(velocity.magnitude * powerMultiplier, 0, maxPower);
                }
                else
                {
                    currentPower = maxPower * 0.5f; // 默认50%力量
                }
                
                Strike();
            }
            
            lastControllerPosition = currentPosition;
        }
    }
    
    void UpdateMouseControls()
    {
        if (Input.GetMouseButtonDown(0) && !isAiming)
        {
            // 开始瞄准
            isAiming = true;
            isPowerAdjusting = false;
            PositionCueForAiming();
        }
        
        if (isAiming && Input.GetMouseButton(0))
        {
            // 旋转球杆进行瞄准
            float mouseX = Input.GetAxis("Mouse X");
            RotateCue(mouseX);
        }
        
        if (isAiming && Input.GetMouseButtonUp(0))
        {
            // 开始调整力量
            isPowerAdjusting = true;
        }
        
        if (isPowerAdjusting)
        {
            // 调整击球力量
            float mouseY = Input.GetAxis("Mouse Y");
            AdjustPower(mouseY);
            
            // 显示力量反馈（可以添加UI或视觉效果）
            
            if (Input.GetMouseButtonDown(0))
            {
                // 击球
                Strike();
                isPowerAdjusting = false;
                isAiming = false;
                isResetting = true;
            }
        }
    }
    
    void OnGrab(SelectEnterEventArgs args)
    {
        isAiming = true;
        Debug.Log("抓取球杆");
        
        // 确保球杆保持水平
        EnforceHorizontalOrientation();
        
        // 记录控制器初始位置
        if (xrController != null)
        {
            lastControllerPosition = xrController.transform.position;
        }
    }
    
    void OnRelease(SelectExitEventArgs args)
    {
        isAiming = false;
        Debug.Log("释放球杆");
    }
    
    void RotateCue(float mouseX)
    {
        // 仅旋转Y轴
        pivotPoint.Rotate(Vector3.up * mouseX * rotationSpeed * Time.deltaTime);
    }
    
    void PositionCueForAiming()
    {
        if (whiteBall != null)
        {
            // 设置球杆位置为白球后方
            Vector3 position = whiteBall.transform.position - pivotPoint.forward * minDistanceFromBall * 2;
            // 确保高度合适
            position.y = whiteBall.transform.position.y + 0.01f;
            pivotPoint.position = position;
        }
    }
    
    void AdjustPower(float mouseY)
    {
        // 根据鼠标纵向移动调整力量
        currentPower += mouseY * powerMultiplier * Time.deltaTime;
        currentPower = Mathf.Clamp(currentPower, 0, maxPower);
        
        // 根据力量调整球杆位置，给予视觉反馈
        PositionCueForStrike();
    }
    
    void PositionCueForStrike()
    {
        if (whiteBall != null)
        {
            // 根据力量调整球杆与白球的距离
            float distanceFactor = Mathf.Lerp(minDistanceFromBall, maxDistanceFromBall, currentPower / maxPower);
            Vector3 position = whiteBall.transform.position - pivotPoint.forward * distanceFactor;
            // 保持水平
            position.y = whiteBall.transform.position.y + 0.01f;
            pivotPoint.position = position;
        }
    }
    
    void Strike()
    {
        if (whiteBall != null)
        {
            Rigidbody ballRigidbody = whiteBall.GetComponent<Rigidbody>();
            
            if (ballRigidbody != null)
            {
                // 从球杆头到白球的方向
                Vector3 strikeDirection = Vector3.zero;
                
                switch(controlType)
                {
                    case CueControlType.Horizontal:
                        // 水平模式使用球杆到球的水平方向
                        strikeDirection = (whiteBall.transform.position - pivotPoint.position).normalized;
                        // 确保方向是水平的
                        strikeDirection.y = 0;
                        strikeDirection.Normalize();
                        break;
                        
                    case CueControlType.Assisted:
                        // 辅助模式使用精确的方向
                        strikeDirection = (whiteBall.transform.position - cueEnd.position).normalized;
                        break;
                        
                    case CueControlType.Standard:
                    default:
                        // 原始逻辑
                        if (useVRControls)
                        {
                            strikeDirection = (whiteBall.transform.position - cueEnd.position).normalized;
                        }
                        else
                        {
                            strikeDirection = pivotPoint.forward;
                        }
                        break;
                }
                
                Debug.Log("击球方向: " + strikeDirection + ", 力量: " + currentPower);
                ballRigidbody.AddForce(strikeDirection * currentPower, ForceMode.Impulse);
                
                // 通知网络组件
                SyncedPoolBall ballSync = whiteBall.GetComponent<SyncedPoolBall>();
                if (ballSync != null)
                {
                    ballSync.OnHit(strikeDirection * currentPower);
                }
                
                // 播放相应的效果
                PlayStrikeEffect();
                
                currentPower = 0;
                canStrike = false;
            }
        }
    }
    
    void PlayStrikeEffect()
    {
        // 获取击球位置 - 白球位置
        Vector3 hitPosition = whiteBall.transform.position;
        GameObject particleEffect = null;

        // 根据不同球杆类型播放不同效果
        switch(controlType)
        {
            case CueControlType.Horizontal:
                // 简单效果
                if (horizontalHitParticle != null)
                {
                    particleEffect = Instantiate(horizontalHitParticle, hitPosition, Quaternion.identity);
                }
                break;
                
            case CueControlType.Assisted:
                // 高级效果
                if (assistedHitParticle != null)
                {
                    particleEffect = Instantiate(assistedHitParticle, hitPosition, Quaternion.identity);
                    // 调整粒子速度和大小，使其更明显
                    ParticleSystem ps = particleEffect.GetComponent<ParticleSystem>();
                    if (ps != null)
                    {
                        var main = ps.main;
                        main.startSpeedMultiplier = Mathf.Clamp01(currentPower / maxPower) * 2f + 1f;
                        main.startSizeMultiplier = Mathf.Clamp01(currentPower / maxPower) * 1.5f + 0.5f;
                    }
                }
                break;
                
            case CueControlType.Standard:
            default:
                // 标准效果
                if (standardHitParticle != null)
                {
                    particleEffect = Instantiate(standardHitParticle, hitPosition, Quaternion.identity);
                }
                break;
        }
        
        // 如果创建了粒子效果，设置销毁定时器
        if (particleEffect != null)
        {
            // 使粒子朝击球方向发射
            Vector3 strikeDirection = (whiteBall.transform.position - cueEnd.position).normalized;
            particleEffect.transform.forward = strikeDirection;
            
            // 自动销毁粒子系统
            Destroy(particleEffect, 2f);
        }
        
        // 播放音效
        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource != null && audioSource.clip != null)
        {
            audioSource.Play();
        }
    }
    
    void ResetCue()
    {
        if (whiteBall != null)
        {
            // 计算理想位置：白球后方某距离
            Vector3 targetPosition = whiteBall.transform.position - pivotPoint.forward * minDistanceFromBall * 2;
            // 保持水平高度
            targetPosition.y = whiteBall.transform.position.y + 0.01f;
            
            // 平滑移动到该位置
            pivotPoint.position = Vector3.Lerp(pivotPoint.position, targetPosition, Time.deltaTime * resetSpeed);
            
            // 当接近目标位置时，重置完成
            if (Vector3.Distance(pivotPoint.position, targetPosition) < 0.01f)
            {
                isResetting = false;
                canStrike = true;
            }
        }
        else
        {
            isResetting = false;
            canStrike = true;
        }
    }
    
    private void OnDrawGizmos()
    {
        if (whiteBall != null && cueEnd != null)
        {
            // 显示从球杆头到白球的方向
            Gizmos.color = Color.red;
            Gizmos.DrawLine(cueEnd.position, whiteBall.transform.position);
            
            // 显示力量
            Gizmos.color = Color.yellow;
            Vector3 direction = (whiteBall.transform.position - cueEnd.position).normalized;
            Gizmos.DrawRay(whiteBall.transform.position, direction * (currentPower / maxPower));
        }
    }
}