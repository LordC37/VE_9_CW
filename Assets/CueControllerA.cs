// 修改CueController.cs
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class CueControllerA : MonoBehaviour
{
    [Header("References")]
    public Transform cueEnd;
    public Transform pivotPoint;
    public GameObject whiteBall;
    public XRController xrController; // VR控制器引用

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
    
    void Start()
    {
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
        
        // 获取XRGrabInteractable组件
        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grabInteractable == null && useVRControls)
        {
            grabInteractable = gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            Debug.Log("添加了XRGrabInteractable组件");
        }
        
        // 设置抓取点
        if (grabInteractable != null)
        {
            grabInteractable.attachTransform = transform;
            
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
                
                // 根据控制器方向调整杆的方向
                pivotPoint.rotation = Quaternion.Lerp(pivotPoint.rotation, 
                                                    xrController.transform.rotation, 
                                                    Time.deltaTime * 5f);
                
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
    
    /* 其余函数保持不变 */
    void UpdateVRControls() { /* 原始代码保持不变 */ }
    void UpdateMouseControls() { /* 原始代码保持不变 */ }
    void OnGrab(SelectEnterEventArgs args) { /* 原始代码保持不变 */ }
    void OnRelease(SelectExitEventArgs args) { /* 原始代码保持不变 */ }
    void RotateCue(float mouseX) { /* 原始代码保持不变 */ }
    void PositionCueForAiming() { /* 原始代码保持不变 */ }
    void AdjustPower(float mouseY) { /* 原始代码保持不变 */ }
    void PositionCueForStrike() { /* 原始代码保持不变 */ }
    
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
        // 根据不同球杆类型播放不同效果
        switch(controlType)
        {
            case CueControlType.Horizontal:
                // 简单效果
                break;
                
            case CueControlType.Assisted:
                // 高级效果
                // 可以在这里添加粒子效果
                break;
        }
        
        // 可以在这里播放音效
        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource != null && audioSource.clip != null)
        {
            audioSource.Play();
        }
    }
    
    void ResetCue() { /* 原始代码保持不变 */ }
    private void OnDrawGizmos() { /* 原始代码保持不变 */ }
}