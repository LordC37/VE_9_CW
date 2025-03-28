using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class DualModeCueController : MonoBehaviour
{
    [Header("References")]
    public Transform cueEnd;
    public Transform pivotPoint;
    public GameObject whiteBall;
    public Transform leftHandTransform; // 左手位置
    public Transform rightHandTransform; // 右手位置

    [Header("Settings")]
    public float rotationSpeed = 100f;
    public float maxPower = 10f;
    public float powerMultiplier = 10f;
    public float resetSpeed = 2f;
    public float minDistanceFromBall = 0.2f;
    public float maxDistanceFromBall = 1.0f;
    public bool useVRMode = false; // 是否使用VR模式，默认为鼠标模式

    [Header("Debug")]
    public bool canStrike = true;
    
    // 鼠标控制相关变量
    private bool isAiming = false;
    private bool isPowerAdjusting = false;
    private float currentPower = 0f;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Vector3 strikingPosition;
    private bool isResetting = false;
    
    // VR控制相关变量
    private bool isLeftHandGripping = false;
    private bool isRightHandGripping = false;
    private Vector3 rightHandStartPosition;
    private float vrStrikePower = 0f;
    
    // 共用变量
    private Rigidbody rb;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }
        
        // 保存杆的初始位置和旋转
        if (pivotPoint != null)
        {
            originalPosition = pivotPoint.position;
            originalRotation = pivotPoint.rotation;
        }
        
        // 如果没有指定白球，尝试在场景中找到
        if (whiteBall == null)
        {
            whiteBall = GameObject.FindGameObjectWithTag("WhiteBall");
            if (whiteBall == null)
            {
                whiteBall = GameObject.Find("Ball1");
                Debug.Log("使用Ball1作为白球");
            }
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
            }
        }
    }
    
    void Update()
    {
        if (whiteBall == null) return;
        
        // 根据模式选择不同的控制方法
        if (useVRMode && IsVRAvailable())
        {
            UpdateVRControls();
        }
        else
        {
            UpdateMouseControls();
        }
    }
    
    // 检查VR设备是否可用
    bool IsVRAvailable()
    {
        return leftHandTransform != null && rightHandTransform != null;
    }
    
    void UpdateMouseControls()
    {
        // 切换瞄准模式
        if (Input.GetMouseButtonDown(0) && canStrike)
        {
            isAiming = true;
            isPowerAdjusting = false;
        }
        
        // 结束瞄准，开始蓄力
        if (Input.GetMouseButtonUp(0) && isAiming)
        {
            isAiming = false;
            isPowerAdjusting = true;
        }
        
        // 释放力量，击球
        if (Input.GetMouseButtonDown(0) && isPowerAdjusting)
        {
            StrikeWithMouse();
            isPowerAdjusting = false;
            isResetting = true;
        }
        
        // 瞄准模式 - 使用鼠标水平旋转
        if (isAiming)
        {
            float mouseX = Input.GetAxis("Mouse X");
            RotateCue(mouseX);
            PositionCueForAiming();
        }
        
        // 蓄力模式
        if (isPowerAdjusting)
        {
            float mouseY = Input.GetAxis("Mouse Y");
            AdjustPower(mouseY);
            PositionCueForStrike();
        }
        
        // 复位杆
        if (isResetting)
        {
            ResetCue();
        }
    }
    
    void UpdateVRControls()
    {
        // 检测左手握杆状态（模拟为手靠近杆）
        float leftHandDistance = Vector3.Distance(leftHandTransform.position, transform.position);
        if (leftHandDistance < 0.2f && Input.GetKeyDown(KeyCode.LeftShift)) // 在VR中这会是手柄按钮
        {
            isLeftHandGripping = true;
        }
        else if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            isLeftHandGripping = false;
        }
        
        // 检测右手握杆状态
        float rightHandDistance = Vector3.Distance(rightHandTransform.position, transform.position);
        if (rightHandDistance < 0.2f && Input.GetKeyDown(KeyCode.Space)) // 在VR中这会是手柄按钮
        {
            isRightHandGripping = true;
            rightHandStartPosition = rightHandTransform.position;
            vrStrikePower = 0f;
        }
        else if (Input.GetKeyUp(KeyCode.Space) && isRightHandGripping)
        {
            // 当右手释放时击球
            if (vrStrikePower > 0)
            {
                StrikeWithVR();
            }
            isRightHandGripping = false;
        }
        
        // 如果左手握住，控制杆的朝向
        if (isLeftHandGripping)
        {
            // 杆跟随左手
            transform.position = leftHandTransform.position;
            transform.rotation = leftHandTransform.rotation;
            
            // 可以添加一些偏移以使杆看起来握在手中
            transform.position += transform.right * 0.05f;
        }
        
        // 如果右手握住，计算击球力量
        if (isRightHandGripping)
        {
            // 计算右手沿杆方向的移动
            Vector3 movement = rightHandTransform.position - rightHandStartPosition;
            float movementAlongCue = Vector3.Dot(movement, -transform.forward);
            
            // 累积力量
            if (movementAlongCue > 0)
            {
                vrStrikePower = Mathf.Clamp(movementAlongCue * powerMultiplier, 0, maxPower);
                Debug.Log("VR蓄力: " + vrStrikePower);
            }
        }
    }
    
    void RotateCue(float mouseX)
    {
        if (pivotPoint != null && whiteBall != null)
        {
            pivotPoint.position = whiteBall.transform.position;
            pivotPoint.Rotate(Vector3.up, mouseX * rotationSpeed * Time.deltaTime);
        }
    }
    
    void PositionCueForAiming()
    {
        if (pivotPoint != null && whiteBall != null && cueEnd != null)
        {
            Vector3 directionFromBall = -pivotPoint.forward;
            pivotPoint.position = whiteBall.transform.position + directionFromBall * minDistanceFromBall;
        }
    }
    
    void AdjustPower(float mouseY)
    {
        currentPower = Mathf.Clamp(currentPower + mouseY * Time.deltaTime * powerMultiplier, 0, maxPower);
        Debug.Log("鼠标蓄力: " + currentPower);
    }
    
    void PositionCueForStrike()
    {
        if (pivotPoint != null && whiteBall != null)
        {
            Vector3 directionFromBall = -pivotPoint.forward;
            float pullbackDistance = minDistanceFromBall + (currentPower / maxPower) * (maxDistanceFromBall - minDistanceFromBall);
            pivotPoint.position = whiteBall.transform.position + directionFromBall * pullbackDistance;
            strikingPosition = pivotPoint.position;
        }
    }
    
    void StrikeWithMouse()
    {
        if (whiteBall != null)
        {
            Rigidbody ballRigidbody = whiteBall.GetComponent<Rigidbody>();
            
            if (ballRigidbody != null)
            {
                // 击球方向
                Vector3 strikeDirection = pivotPoint.forward;
                
                Debug.Log("鼠标击球，力量: " + currentPower);
                ballRigidbody.AddForce(strikeDirection * currentPower, ForceMode.Impulse);
                
                // 通知网络组件
                SyncedPoolBall ballSync = whiteBall.GetComponent<SyncedPoolBall>();
                if (ballSync != null)
                {
                    ballSync.OnHit(strikeDirection * currentPower);
                }
                
                // 重置力度
                currentPower = 0;
                
                // 禁止再次击球直到杆复位
                canStrike = false;
            }
        }
    }
    
    void StrikeWithVR()
    {
        if (whiteBall != null && cueEnd != null)
        {
            Rigidbody ballRigidbody = whiteBall.GetComponent<Rigidbody>();
            
            if (ballRigidbody != null)
            {
                // 从杆头到球的方向
                Vector3 strikeDirection = (whiteBall.transform.position - cueEnd.position).normalized;
                
                Debug.Log("VR击球，力量: " + vrStrikePower);
                ballRigidbody.AddForce(strikeDirection * vrStrikePower, ForceMode.Impulse);
                
                // 通知网络组件
                SyncedPoolBall ballSync = whiteBall.GetComponent<SyncedPoolBall>();
                if (ballSync != null)
                {
                    ballSync.OnHit(strikeDirection * vrStrikePower);
                }
                
                // 重置力度
                vrStrikePower = 0;
            }
        }
    }
    
    void ResetCue()
    {
        // 将杆逐渐移回白球附近的初始位置
        if (pivotPoint != null && whiteBall != null)
        {
            Vector3 targetPosition = whiteBall.transform.position - pivotPoint.forward * minDistanceFromBall;
            pivotPoint.position = Vector3.Lerp(pivotPoint.position, targetPosition, resetSpeed * Time.deltaTime);
            
            // 检查杆是否已经足够接近目标位置
            if (Vector3.Distance(pivotPoint.position, targetPosition) < 0.05f)
            {
                isResetting = false;
                canStrike = true; // 重新允许击球
            }
        }
    }
    
    // 当在VR模式下时，可以在编辑器中手动切换以测试
    public void ToggleVRMode()
    {
        useVRMode = !useVRMode;
        Debug.Log("VR模式: " + (useVRMode ? "开启" : "关闭"));
    }
    
    // 在Inspector中提供切换按钮
    void OnGUI()
    {
        if (Application.isEditor && GUI.Button(new Rect(10, 10, 150, 30), "切换VR/鼠标模式"))
        {
            ToggleVRMode();
        }
    }
}