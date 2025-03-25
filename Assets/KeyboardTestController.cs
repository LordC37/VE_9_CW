using UnityEngine;

public class KeyboardTestController : MonoBehaviour
{
    public Transform leftController;
    public Transform rightController;
    public Camera mainCamera;
    
    public float moveSpeed = 2.0f;
    public float rotateSpeed = 30.0f;
    
    private void Update()
    {
        // 移动摄像机
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        Vector3 move = new Vector3(horizontal, 0, vertical) * moveSpeed * Time.deltaTime;
        mainCamera.transform.Translate(move);
        
        // 旋转摄像机
        if (Input.GetKey(KeyCode.Q))
            mainCamera.transform.Rotate(Vector3.up, -rotateSpeed * Time.deltaTime);
        if (Input.GetKey(KeyCode.E))
            mainCamera.transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);
            
        // 上下看
        if (Input.GetKey(KeyCode.R))
            mainCamera.transform.Rotate(Vector3.right, rotateSpeed * Time.deltaTime);
        if (Input.GetKey(KeyCode.F))
            mainCamera.transform.Rotate(Vector3.right, -rotateSpeed * Time.deltaTime);
            
        // 移动手柄 - 左手
        if (leftController)
        {
            if (Input.GetKey(KeyCode.T))
                leftController.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
            if (Input.GetKey(KeyCode.G))
                leftController.Translate(Vector3.back * moveSpeed * Time.deltaTime);
            if (Input.GetKey(KeyCode.Y))
                leftController.Translate(Vector3.up * moveSpeed * Time.deltaTime);
            if (Input.GetKey(KeyCode.H))
                leftController.Translate(Vector3.down * moveSpeed * Time.deltaTime);
        }
        
        // 移动手柄 - 右手
        if (rightController)
        {
            if (Input.GetKey(KeyCode.U))
                rightController.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
            if (Input.GetKey(KeyCode.J))
                rightController.Translate(Vector3.back * moveSpeed * Time.deltaTime);
            if (Input.GetKey(KeyCode.I))
                rightController.Translate(Vector3.up * moveSpeed * Time.deltaTime);
            if (Input.GetKey(KeyCode.K))
                rightController.Translate(Vector3.down * moveSpeed * Time.deltaTime);
        }
    }
}