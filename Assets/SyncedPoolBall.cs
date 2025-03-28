using UnityEngine;
using Ubiq.Messaging;

public class SyncedPoolBall : MonoBehaviour
{
    private NetworkContext context;    // 网络上下文，用于发送和接收消息
    private Vector3 lastPosition;      // 上一次同步的位置，用于检测变化


    void Start()
    {
        // 注册球到网络场景
        context = NetworkScene.Register(this);
        lastPosition = transform.position;

    }

    void Update()
    {
        if (Vector3.Distance(lastPosition, transform.position) > 0.01f)
        {
            lastPosition = transform.position;
            // 发送位置更新消息
            context.SendJson(new Message { position = transform.position });
        }
    }

    // 定义消息结构
    private struct Message
    {
        public Vector3 position;
        public Vector3 force;      // 添加力的信息
        public bool isHit;         // 指示这是否是一个击打消息
    }

    // 接收并处理其他用户发送的位置更新
    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        var msg = message.FromJson<Message>();
        
        if (msg.isHit)
        {
            // 如果是击打消息，应用力
            GetComponent<Rigidbody>().AddForce(msg.force, ForceMode.Impulse);
        }
        else
        {
            // 如果是位置更新，直接设置位置
            transform.position = msg.position;
            lastPosition = msg.position; // 更新 lastPosition，避免重复发送
        }
    }

    // 添加OnHit方法处理球被击中的情况
    public void OnHit(Vector3 force)
    {
        // 应用物理力到球上
        GetComponent<Rigidbody>().AddForce(force, ForceMode.Impulse);
        
        // 发送击打消息到网络
        context.SendJson(new Message { 
            force = force,
            isHit = true 
        });
    }
}