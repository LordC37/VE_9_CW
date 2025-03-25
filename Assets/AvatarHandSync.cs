using UnityEngine;
using Oculus.VR;

public class AvatarHandSync : MonoBehaviour
{
    // 定义左右控制器类型
    public OVRInput.Controller leftController = OVRInput.Controller.LTouch;
    public OVRInput.Controller rightController = OVRInput.Controller.RTouch;

    // Avatar 左右手的 Transform
    public Transform leftHand;
    public Transform rightHand;

    // Update 每帧调用一次
    void Update()
    {
        UpdateHandPositions();
    }

    void UpdateHandPositions()
    {
        // 获取左右控制器的位置
        Vector3 leftPos = OVRInput.GetLocalControllerPosition(leftController);
        Vector3 rightPos = OVRInput.GetLocalControllerPosition(rightController);

        // 获取左右控制器的旋转
        Quaternion leftRot = OVRInput.GetLocalControllerRotation(leftController);
        Quaternion rightRot = OVRInput.GetLocalControllerRotation(rightController);

        // 直接将位置和旋转应用到 Avatar 的手部
        leftHand.localPosition = leftPos;
        leftHand.localRotation = leftRot;
        rightHand.localPosition = rightPos;
        rightHand.localRotation = rightRot;
    }
}