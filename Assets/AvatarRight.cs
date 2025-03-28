using System.Collections.Generic;
using UnityEngine;
using Ubiq.Avatars;
using Ubiq.Rooms;
using Ubiq.Messaging;
using Ubiq.Samples;

/// <summary>
/// 根据玩家的虚拟形象纹理决定球杆使用权限
/// </summary>
public class AvatarRight : MonoBehaviour
{
    // 球杆引用
    public GameObject cueStickA;
    public GameObject cueStickB;
    public GameObject cueStickC;

    // 纹理权限配置
    [System.Serializable]
    public class TexturePermission
    {
        public string textureName; // 纹理的名称或UUID
        public bool canUseStickA;
        public bool canUseStickB;
        public bool canUseStickC;
    }
    
    public List<TexturePermission> texturePermissions = new List<TexturePermission>();
    
    // 系统引用
    private AvatarManager avatarManager;
    private RoomClient roomClient;
    private string currentTextureUuid;
    
    private void Start()
    {
        // 获取系统组件
        var networkScene = NetworkScene.Find(this);
        roomClient = networkScene.GetComponentInChildren<RoomClient>();
        avatarManager = networkScene.GetComponentInChildren<AvatarManager>();
        
        // 初始状态下禁用所有球杆
        if (cueStickA) cueStickA.SetActive(false);
        if (cueStickB) cueStickB.SetActive(false);
        if (cueStickC) cueStickC.SetActive(false);
        
        // 首次检查权限
        CheckPermissions();
        
        // 添加纹理变化监听
        InvokeRepeating("CheckPermissions", 1f, 1f);
    }
    
    /// <summary>
    /// 检查当前玩家权限并应用
    /// </summary>
    public void CheckPermissions()
    {
        var avatar = avatarManager.FindAvatar(roomClient.Me);
        if (avatar == null) return;
        
        // 使用完全限定的类型名称，确保获取正确的组件
        var texturedAvatar = avatar.GetComponent<Ubiq.Samples.TexturedAvatar>();
        Debug.Log($"检查权限: {texturedAvatar}");
        
        if (texturedAvatar == null)
        {
            Debug.LogError("权限检查失败: 无法找到TexturedAvatar组件");
            Debug.Log("尝试查找具体类型...");

            // 使用替代方法：通过类型名称字符串查找组件
            Component[] components = avatar.GetComponents<Component>();
            foreach (var comp in components)
            {
                Debug.Log($"检查组件: {comp.GetType().FullName}");

                // 如果找到了匹配的组件类型
                if (comp.GetType().FullName.Contains("TexturedAvatar"))
                {
                    Debug.Log($"找到匹配组件: {comp.GetType().FullName}");

                    // 使用反射调用GetTexture方法
                    try
                    {
                        var method = comp.GetType().GetMethod("GetTexture");
                        var texture = method.Invoke(comp, null) as Texture2D;
                        string textureName = texture ? texture.name : "未知纹理";

                        Debug.Log($"使用反射获取纹理: {textureName}");

                        // 尝试获取UUID
                        string textureUuid = roomClient.Me["ubiq.avatar.texture.uuid"];

                        // 应用权限
                        ApplyPermissions(textureUuid, textureName);
                        return;
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"反射调用失败: {e.Message}");
                    }
                }
            }

            // 如果还是找不到，应用默认权限
            Debug.LogWarning("找不到任何TexturedAvatar组件，应用默认权限");
            if (cueStickA) cueStickA.SetActive(true);
            if (cueStickB) cueStickB.SetActive(false);
            if (cueStickC) cueStickC.SetActive(false);
            return;
        }
        
        // 原有的权限检查逻辑
        currentTextureUuid = roomClient.Me["ubiq.avatar.texture.uuid"];
        Texture2D currentTexture = texturedAvatar.GetTexture();
        string printtextureName = currentTexture ? currentTexture.name : "";
        
        ApplyPermissions(currentTextureUuid, printtextureName);
    }
    
    /// <summary>
    /// 基于当前纹理应用权限
    /// </summary>
    private void ApplyPermissions(string textureUuid, string textureName)
    {
        bool hasPermissionA = false;
        bool hasPermissionB = false;
        bool hasPermissionC = false;
        
        // 基于纹理UUID或名称判断
        foreach (var permission in texturePermissions)
        {
            if (textureUuid == permission.textureName ||
                textureName == permission.textureName)
            {
                hasPermissionA = permission.canUseStickA;
                hasPermissionB = permission.canUseStickB;
                hasPermissionC = permission.canUseStickC;
                Debug.Log($"找到纹理匹配: {permission.textureName}");
                break;
            }
        }
        
        // 应用权限
        if (cueStickA) cueStickA.SetActive(hasPermissionA);
        if (cueStickB) cueStickB.SetActive(hasPermissionB);
        if (cueStickC) cueStickC.SetActive(hasPermissionC);
        
        Debug.Log($"应用球杆权限: 纹理UUID={textureUuid}, 纹理名称={textureName}, " +
                  $"球杆A权限={hasPermissionA}, 球杆B权限={hasPermissionB}");
    }
}