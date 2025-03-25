using UnityEngine;
using UnityEngine.XR;

public class AvatarHandSync : MonoBehaviour
{
    public Transform hand;
    public Transform controller;
    public bool isLeft = false;
    public Vector3 offset = Vector3.zero;
    
    private void LateUpdate()
    {
        if (controller && hand)
        {
            // 同步手部位置到控制器
            hand.position = controller.position + offset;
            hand.rotation = controller.rotation;
            
            // 调试信息
            Debug.Log((isLeft ? "左" : "右") + "手位置: " + hand.position + ", 控制器位置: " + controller.position);
        }
    }
}