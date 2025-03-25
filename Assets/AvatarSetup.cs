using UnityEngine;

public class AvatarSetup : MonoBehaviour
{
    [Header("Avatar Parts")]
    public Transform headPart;
    public Transform leftHandPart;
    public Transform rightHandPart;
    
    [Header("References")]
    public Camera playerCamera;
    public Transform leftController;
    public Transform rightController;
    public GameObject cueStick;
    
    [Header("Offset Settings")]
    public Vector3 headOffset = Vector3.zero;
    public Vector3 leftHandOffset = Vector3.zero;
    public Vector3 rightHandOffset = Vector3.zero;
    public Vector3 cueStickOffset = new Vector3(0, 0, 0.2f);
    public Vector3 cueStickRotation = Vector3.zero;
    
    // 添加的组件引用
    private AvatarHeadSync headSync;
    private AvatarHandSync leftHandSync;
    private AvatarHandSync rightHandSync;
    private CueStickHandler cueStickHandler;
    
    private void Start()
    {
        SetupComponents();
    }
    
    private void SetupComponents()
    {
        // 设置头部同步
        if (headPart && playerCamera)
        {
            headSync = gameObject.GetComponent<AvatarHeadSync>();
            if (headSync == null)
                headSync = gameObject.AddComponent<AvatarHeadSync>();
                
            headSync.head = headPart;
            headSync.camera = playerCamera.transform;
            headSync.offset = headOffset;
        }
        
        // 设置左手同步
        if (leftHandPart && leftController)
        {
            leftHandSync = gameObject.GetComponent<AvatarHandSync>();
            if (leftHandSync == null || !leftHandSync.isLeft)
            {
                // 查找左手同步组件
                AvatarHandSync[] handSyncs = gameObject.GetComponents<AvatarHandSync>();
                leftHandSync = null;
                foreach (var hs in handSyncs)
                {
                    if (hs.isLeft)
                    {
                        leftHandSync = hs;
                        break;
                    }
                }
                
                // 如果找不到左手同步组件，创建一个
                if (leftHandSync == null)
                    leftHandSync = gameObject.AddComponent<AvatarHandSync>();
            }
            
            leftHandSync.hand = leftHandPart;
            leftHandSync.controller = leftController;
            leftHandSync.isLeft = true;
            leftHandSync.offset = leftHandOffset;
        }
        
        // 设置右手同步
        if (rightHandPart && rightController)
        {
            rightHandSync = gameObject.GetComponent<AvatarHandSync>();
            if (rightHandSync == null || rightHandSync.isLeft)
            {
                // 查找右手同步组件
                AvatarHandSync[] handSyncs = gameObject.GetComponents<AvatarHandSync>();
                rightHandSync = null;
                foreach (var hs in handSyncs)
                {
                    if (!hs.isLeft)
                    {
                        rightHandSync = hs;
                        break;
                    }
                }
                
                // 如果找不到右手同步组件，创建一个
                if (rightHandSync == null)
                    rightHandSync = gameObject.AddComponent<AvatarHandSync>();
            }
            
            rightHandSync.hand = rightHandPart;
            rightHandSync.controller = rightController;
            rightHandSync.isLeft = false;
            rightHandSync.offset = rightHandOffset;
        }
        
        // 设置球杆处理
        if (cueStick && rightHandPart)
        {
            cueStickHandler = gameObject.GetComponent<CueStickHandler>();
            if (cueStickHandler == null)
                cueStickHandler = gameObject.AddComponent<CueStickHandler>();
                
            cueStickHandler.rightHand = rightHandPart;
            cueStickHandler.cueStick = cueStick.transform;
            cueStickHandler.positionOffset = cueStickOffset;
            cueStickHandler.rotationOffset = cueStickRotation;
        }
    }
    
    // 用于在运行时手动调整偏移量的方法
    public void UpdateOffsets(Vector3 head, Vector3 leftHand, Vector3 rightHand, Vector3 cueStickPos, Vector3 cueStickRot)
    {
        headOffset = head;
        leftHandOffset = leftHand;
        rightHandOffset = rightHand;
        cueStickOffset = cueStickPos;
        cueStickRotation = cueStickRot;
        
        // 更新组件中的偏移量
        if (headSync) headSync.offset = headOffset;
        if (leftHandSync) leftHandSync.offset = leftHandOffset;
        if (rightHandSync) rightHandSync.offset = rightHandOffset;
        if (cueStickHandler)
        {
            cueStickHandler.positionOffset = cueStickOffset;
            cueStickHandler.rotationOffset = cueStickRotation;
        }
    }
}