// 创建一个新脚本：CueAppearance.cs
using UnityEngine;

public class CueAppearance : MonoBehaviour
{
    public enum CueType { Standard, Magic, Futuristic }
    
    public CueType cueType = CueType.Standard;
    public Color cueColor = Color.white;
    public float cueLength = 1.45f;
    public float cueThickness = 0.01f;
    
    // 外观元素引用
    public GameObject specialEffectPrefab; // 特殊效果预制体
    
    private Renderer cueRenderer;
    
    void Start()
    {
        cueRenderer = GetComponentInChildren<Renderer>();
        if (cueRenderer == null)
        {
            Debug.LogWarning("找不到球杆渲染器，无法更改外观");
            return;
        }
        
        ApplyCueAppearance();
    }
    
    void ApplyCueAppearance()
    {
        // 设置基本材质颜色
        if (cueRenderer.material != null)
        {
            cueRenderer.material.color = cueColor;
        }
        
        // 设置尺寸
        Transform cueModel = transform.Find("Model"); // 假设模型在Model子对象
        if (cueModel != null)
        {
            Vector3 scale = cueModel.localScale;
            scale.y = cueLength; // 假设y轴是长度
            scale.x = scale.z = cueThickness;
            cueModel.localScale = scale;
        }
        
        // 根据球杆类型添加特殊效果
        switch(cueType)
        {
            case CueType.Magic:
                // 添加魔法粒子效果
                if (specialEffectPrefab != null)
                {
                    GameObject effect = Instantiate(specialEffectPrefab, transform);
                    effect.transform.localPosition = new Vector3(0, cueLength/2, 0); // 在杆尖
                }
                break;
                
            case CueType.Futuristic:
                // 添加未来风格的发光效果
                if (cueRenderer.material != null)
                {
                    cueRenderer.material.EnableKeyword("_EMISSION");
                    cueRenderer.material.SetColor("_EmissionColor", cueColor * 2.0f);
                }
                break;
        }
    }
}