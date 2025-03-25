using UnityEngine;

public class AvatarHeadSync : MonoBehaviour
{
    public Transform head;
    public Transform camera;
    public Vector3 offset = Vector3.zero;
    
    private void LateUpdate()
    {
        if (camera && head)
        {
            // 将头部位置与相机同步
            head.position = camera.position + offset;
            head.rotation = camera.rotation;
            
            // 调试信息
            Debug.Log("头部位置: " + head.position + ", 相机位置: " + camera.position);
        }
    }
}