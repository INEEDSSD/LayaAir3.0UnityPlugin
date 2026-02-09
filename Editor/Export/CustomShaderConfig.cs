using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 自定义Shader配置 - 简化版，主要用于存储启用状态
/// </summary>
public class CustomShaderConfig
{
    private static CustomShaderConfig _instance;
    public static CustomShaderConfig Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new CustomShaderConfig();
            }
            return _instance;
        }
    }

    // 是否启用自定义Shader导出
    public bool enableCustomShaderExport = false;

    /// <summary>
    /// 获取场景中使用的所有自定义Shader（未在内置配置中的）
    /// </summary>
    public static List<Shader> GetCustomShadersInScene()
    {
        List<Shader> customShaders = new List<Shader>();
        HashSet<string> addedShaders = new HashSet<string>();
        
        // 获取场景中所有Renderer
        Renderer[] renderers = GameObject.FindObjectsOfType<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            foreach (Material mat in renderer.sharedMaterials)
            {
                if (mat != null && mat.shader != null)
                {
                    string shaderName = mat.shader.name;
                    // 检查是否是内置Shader或已配置的Shader
                    if (!MetarialUitls.MaterialPropsConfigs.ContainsKey(shaderName) && 
                        !addedShaders.Contains(shaderName))
                    {
                        customShaders.Add(mat.shader);
                        addedShaders.Add(shaderName);
                    }
                }
            }
        }
        
        return customShaders;
    }
}
