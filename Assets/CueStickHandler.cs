using UnityEngine;

public class CueStickHandler : MonoBehaviour
{
    public Transform rightHand;     // 右手引用
    public Transform cueStick;      // 球杆引用
    public Vector3 positionOffset = new Vector3(0, 0, 0.2f); // 球杆相对于手的位置偏移
    public Vector3 rotationOffset = new Vector3(0, 0, 0);    // 球杆相对于手的旋转偏移
    
    private void LateUpdate()
    {
        if (rightHand && cueStick)
        {
            // 将球杆设置为右手的子物体
            if (cueStick.parent != rightHand)
            {
                cueStick.SetParent(rightHand);
                // 设置本地位置和旋转
                cueStick.localPosition = positionOffset;
                cueStick.localRotation = Quaternion.Euler(rotationOffset);
            }
            
            // 确保本地位置和旋转正确
            cueStick.localPosition = positionOffset;
            cueStick.localEulerAngles = rotationOffset;
        }
    }
}