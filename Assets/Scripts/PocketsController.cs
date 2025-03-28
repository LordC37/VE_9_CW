using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PocketsController : MonoBehaviour {
    public GameObject redBalls;
    public GameObject cueBall;
    public bool useFallbackMode = false; // 是否使用备用模式

    private Vector3 originalCueBallPosition;
    private List<int> pendingPocketedBalls = new List<int>(); // 存储待处理的已进袋球号
    private PoolGameController gameController; // 直接引用 PoolGameController 实例
    
    // 备用方式：保存已进袋的球号
    private HashSet<int> pocketedBalls = new HashSet<int>();

    void Start() {
        if (cueBall != null) {
            originalCueBallPosition = cueBall.transform.position;
        } else {
            Debug.LogError("cueBall 未设置，请在 Inspector 中赋值");
        }
        
        // 尝试查找 PoolGameController
        StartCoroutine(InitializeGameController());
    }
    
    IEnumerator InitializeGameController() {
        // 首先尝试通过 GameInstance 获取
        float timeOut = 5.0f; // 5秒超时
        float elapsed = 0f;
        
        while (PoolGameController.GameInstance == null && elapsed < timeOut) {
            yield return new WaitForSeconds(0.5f);
            elapsed += 0.5f;
        }
        
        if (PoolGameController.GameInstance != null) {
            gameController = PoolGameController.GameInstance;
            Debug.Log("通过 GameInstance 找到 PoolGameController");
        } else {
            // 如果 GameInstance 为空，直接在场景中查找
            gameController = FindObjectOfType<PoolGameController>();
            
            if (gameController != null) {
                Debug.Log("通过 FindObjectOfType 找到 PoolGameController");
            } else {
                Debug.LogWarning("未找到 PoolGameController，启用备用模式");
                useFallbackMode = true;
            }
        }
        
        // 处理之前积累的已进袋球
        if (gameController != null && pendingPocketedBalls.Count > 0) {
            foreach (int ballNumber in pendingPocketedBalls) {
                gameController.BallPocketed(ballNumber);
            }
            pendingPocketedBalls.Clear();
        } else if (useFallbackMode) {
            // 在备用模式下，只是将球添加到已进袋集合
            foreach (int ballNumber in pendingPocketedBalls) {
                pocketedBalls.Add(ballNumber);
                Debug.Log($"备用模式：球 {ballNumber} 已进袋");
            }
            pendingPocketedBalls.Clear();
        }
    }

    void OnCollisionEnter(Collision collision) {
        if (collision == null || collision.gameObject == null) return;

        if (redBalls != null) {
            foreach (var transform in redBalls.GetComponentsInChildren<Transform>()) {
                if (transform.name == collision.gameObject.name) {
                    var objectName = collision.gameObject.name;
                    GameObject.Destroy(collision.gameObject);

                    var ballNumber = int.Parse(objectName.Replace("Ball", ""));
                    
                    // 处理球进袋事件
                    if (gameController != null) {
                        gameController.BallPocketed(ballNumber);
                    } else if (useFallbackMode) {
                        // 备用模式：只是记录球已进袋
                        pocketedBalls.Add(ballNumber);
                        Debug.Log($"备用模式：球 {ballNumber} 已进袋");
                    } else {
                        Debug.LogWarning($"球 {ballNumber} 已进袋，但找不到 PoolGameController，将稍后处理");
                        pendingPocketedBalls.Add(ballNumber);
                    }
                }
            }
        }

        if (cueBall != null && cueBall.transform.name == collision.gameObject.name) {
            cueBall.transform.position = originalCueBallPosition;
        }
    }
}