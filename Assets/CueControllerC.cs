using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class CueControllerC : MonoBehaviour
{
    public Transform cueEnd;
    public GameObject whiteBall;
    public XRController xrController;
    public float powerMultiplier = 1000f;
    public float maxPower = 1000000f;
    public float autoAimDistance = 0.5f;
    
    private Vector3 lastControllerPosition;
    private bool isAiming = false;
    private bool canStrike = true;
    private LineRenderer aimLine;
    
    void Start()
    {
        // 查找白球
        if (whiteBall == null)
        {
            whiteBall = GameObject.FindGameObjectWithTag("WhiteBall");
            if (whiteBall == null)
                whiteBall = GameObject.Find("Ball1");
        }
        
        // 查找杆头
        if (cueEnd == null)
        {
            cueEnd = transform.Find("CueTip");
            if (cueEnd == null)
            {
                GameObject tip = new GameObject("CueTip");
                tip.transform.parent = transform;
                tip.transform.localPosition = new Vector3(0, 0, 1);
                cueEnd = tip.transform;
            }
        }
        
        // 添加XRGrabInteractable
        var grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grabInteractable == null)
        {
            grabInteractable = gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            grabInteractable.attachTransform = transform;
        }
        
        grabInteractable.selectEntered.AddListener(OnGrab);
        grabInteractable.selectExited.AddListener(OnRelease);
        
        // 创建瞄准线
        GameObject lineObj = new GameObject("AimLine");
        lineObj.transform.parent = transform;
        aimLine = lineObj.AddComponent<LineRenderer>();
        aimLine.startWidth = 0.01f;
        aimLine.endWidth = 0.001f;
        aimLine.material = new Material(Shader.Find("Sprites/Default"));
        aimLine.startColor = Color.green;
        aimLine.endColor = Color.yellow;
        aimLine.positionCount = 2;
        aimLine.enabled = false;
        
        if (xrController != null)
            lastControllerPosition = xrController.transform.position;
    }
    
    void Update()
    {
        if (!isAiming || whiteBall == null || xrController == null) return;
        
        // 获取控制器位置和按钮状态
        Vector3 currentPosition = xrController.transform.position;
        
        // 检查是否在自动瞄准范围内
        float distanceToWhiteBall = Vector3.Distance(transform.position, whiteBall.transform.position);
        
        if (distanceToWhiteBall < autoAimDistance)
        {
            // 自动瞄准模式
            Vector3 controllerForward = xrController.transform.forward;
            controllerForward.y *= 0.3f; // 减少垂直影响但不完全消除
            
            // 设置位置和朝向
            transform.position = whiteBall.transform.position - controllerForward.normalized * 0.2f;
            transform.rotation = Quaternion.LookRotation(controllerForward);
            
            // 更新瞄准线
            aimLine.enabled = true;
            aimLine.SetPosition(0, cueEnd.position);
            aimLine.SetPosition(1, whiteBall.transform.position + controllerForward * 2f);
            aimLine.startColor = Color.green; // 绿色表示自动瞄准激活
        }
        else
        {
            // 正常移动模式
            Vector3 movement = currentPosition - lastControllerPosition;
            transform.position += movement;
            
            // 尝试跟随控制器旋转
            transform.rotation = Quaternion.Lerp(transform.rotation, 
                                            xrController.transform.rotation, 
                                            Time.deltaTime * 5f);
            
            // 更新瞄准线
            if (Vector3.Distance(cueEnd.position, whiteBall.transform.position) < 1.0f)
            {
                aimLine.enabled = true;
                Vector3 direction = (whiteBall.transform.position - cueEnd.position).normalized;
                aimLine.SetPosition(0, cueEnd.position);
                aimLine.SetPosition(1, whiteBall.transform.position + direction * 2f);
                aimLine.startColor = Color.red; // 红色表示正常瞄准
            }
            else
            {
                aimLine.enabled = false;
            }
        }
        
        // 检测击球
        bool triggerPressed = false;
        xrController.inputDevice.TryGetFeatureValue(CommonUsages.triggerButton, out triggerPressed);
        
        if (triggerPressed && canStrike)
        {
            float power = 0;
            if (distanceToWhiteBall < autoAimDistance)
            {
                // 在自动瞄准范围内使用更高的固定力量
                power = maxPower * 0.7f;
            }
            else
            {
                // 使用控制器速度计算力量
                Vector3 velocity = Vector3.zero;
                if (xrController.inputDevice.TryGetFeatureValue(CommonUsages.deviceVelocity, out velocity))
                {
                    power = Mathf.Clamp(velocity.magnitude * powerMultiplier, 0, maxPower);
                }
                else
                {
                    power = maxPower * 0.5f; // 默认力量
                }
            }
            
            Strike(power);
        }
        
        lastControllerPosition = currentPosition;
    }
    
    void OnGrab(SelectEnterEventArgs args)
    {
        isAiming = true;
    }
    
    void OnRelease(SelectExitEventArgs args)
    {
        isAiming = false;
        aimLine.enabled = false;
    }
    
    void Strike(float power)
    {
        if (whiteBall == null) return;
        
        Rigidbody ballRigidbody = whiteBall.GetComponent<Rigidbody>();
        if (ballRigidbody == null) return;
        
        // 使用cueEnd到白球的方向
        Vector3 direction = (whiteBall.transform.position - cueEnd.position).normalized;
        
        ballRigidbody.AddForce(direction * power, ForceMode.Impulse);
        
        // 同步网络状态
        SyncedPoolBall syncBall = whiteBall.GetComponent<SyncedPoolBall>();
        if (syncBall != null)
            syncBall.OnHit(direction * power);
        
        canStrike = false;
        Invoke("ResetStrike", 2f);
    }
    

    void ResetStrike()
    {
        canStrike = true;
    }
}