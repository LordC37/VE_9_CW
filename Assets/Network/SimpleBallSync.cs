using UnityEngine;
using Ubiq.Messaging;

public class SimpleBallSync : MonoBehaviour
{
    private Rigidbody rb;
    private NetworkContext context;
    
    // 平滑同步参数
    private Vector3 targetPosition;
    private Vector3 targetVelocity;
    private float syncRate = 0.1f; // 同步间隔
    private float syncTimer = 0f;
    
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("找不到Rigidbody组件");
            enabled = false;
            return;
        }
        
        // 尝试找到并使用NetworkScene
        var networkScene = FindObjectOfType<NetworkScene>();
        if (networkScene != null)
        {
            // 这里不使用AddProcessor，而是直接使用NetworkScene的组件
            // 如果NetworkScene有其他可用的API，可以尝试使用
            Debug.Log("找到NetworkScene: " + networkScene.name);
        }
        else
        {
            Debug.LogError("场景中没有NetworkScene");
            enabled = false;
        }
        
        targetPosition = transform.position;
        targetVelocity = Vector3.zero;
    }
    
    private void Update()
    {
        // 本地球体发送位置
        if (rb != null && rb.linearVelocity.sqrMagnitude > 0.01f) // 只有在移动时才同步
        {
            syncTimer += Time.deltaTime;
            if (syncTimer >= syncRate)
            {
                // 由于无法直接使用Ubiq的API，我们暂时只打印信息
                Debug.Log("球位置：" + transform.position + "，速度：" + rb.linearVelocity);
                syncTimer = 0f;
            }
        }
    }
}