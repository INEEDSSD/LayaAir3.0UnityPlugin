
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;

internal class MaterialFile : JsonFile
{
    private Material m_material;
    private bool m_isBuiltinParticleMaterial = false;
    private HashSet<System.Type> m_rendererTypes = new HashSet<System.Type>();
    private bool m_isParticleMeshMode = false; // ⭐ Track if particle uses mesh rendering mode

    public MaterialFile(ResoureMap map, Material material, Renderer renderer = null) : base(null,new JSONObject(JSONObject.Type.OBJECT))
    {
        this.resoureMap = map;
        this.m_material = material;

        // Track which type of renderer uses this material
        if (renderer != null)
        {
            m_rendererTypes.Add(renderer.GetType());

            // ⭐ Check if this is a ParticleSystemRenderer in mesh mode
            ParticleSystemRenderer particleRenderer = renderer as ParticleSystemRenderer;
            if (particleRenderer != null)
            {
                ParticleSystemRenderMode renderMode = particleRenderer.renderMode;
                if (renderMode == ParticleSystemRenderMode.Mesh)
                {
                    m_isParticleMeshMode = true;
                    Debug.Log($"LayaAir3D: Particle system '{renderer.gameObject.name}' uses MESH render mode");
                }
            }
        }
        
        // 检查是否是内置粒子材质
        string materialPath = AssetDatabase.GetAssetPath(material.GetInstanceID());
        bool isBuiltinResource = ResoureMap.IsBuiltinResource(materialPath);
        bool isParticleMaterial = IsParticleShader(material.shader.name);
        
        if (isBuiltinResource && isParticleMaterial)
        {
            // 内置粒子材质使用模板
            m_isBuiltinParticleMaterial = true;
            this.updatePath(AssetsUtil.GetMaterialPath(material));
            WriteBuiltinParticleMaterial(material, this.jsonData, map);
        }
        else
        {
            this.updatePath(AssetsUtil.GetMaterialPath(material));
            if(material.shader.name == "Skybox/6 Sided")
            {
                MetarialUitls.WriteSkyMetarial(material, this.jsonData, map);
            }
            else
            {
                MetarialUitls.WriteMetarial(material, this.jsonData, map, this);
            }
        }
    }

    /// <summary>
    /// Add a renderer type that uses this material (called when material is reused)
    /// </summary>
    public void AddRendererUsage(Renderer renderer)
    {
        if (renderer != null)
        {
            m_rendererTypes.Add(renderer.GetType());

            // ⭐ Check if this is a ParticleSystemRenderer in mesh mode
            ParticleSystemRenderer particleRenderer = renderer as ParticleSystemRenderer;
            if (particleRenderer != null)
            {
                ParticleSystemRenderMode renderMode = particleRenderer.renderMode;
                if (renderMode == ParticleSystemRenderMode.Mesh)
                {
                    m_isParticleMeshMode = true;
                    Debug.Log($"LayaAir3D: Particle system '{renderer.gameObject.name}' uses MESH render mode");
                }
            }
        }
    }

    /// <summary>
    /// Check if this material is used by ParticleSystemRenderer
    /// </summary>
    public bool IsUsedByParticleSystem()
    {
        return m_rendererTypes.Contains(typeof(ParticleSystemRenderer));
    }

    /// <summary>
    /// Check if this material is used by MeshRenderer or SkinnedMeshRenderer
    /// </summary>
    public bool IsUsedByMeshRenderer()
    {
        return m_rendererTypes.Contains(typeof(MeshRenderer)) ||
               m_rendererTypes.Contains(typeof(SkinnedMeshRenderer));
    }

    /// <summary>
    /// ⭐ Check if this material is used by ParticleSystemRenderer in MESH rendering mode
    /// </summary>
    public bool IsParticleMeshMode()
    {
        return m_isParticleMeshMode;
    }

    /// <summary>
    /// 检查是否是粒子相关的 Shader
    /// </summary>
    private static bool IsParticleShader(string shaderName)
    {
        if (string.IsNullOrEmpty(shaderName))
            return false;

        string lowerName = shaderName.ToLower();

        // Unity内置粒子shader
        if (lowerName.Contains("particle") ||
            lowerName.Contains("additive") ||
            lowerName.Contains("alpha blended"))
            return true;

        // ⭐ Artist_Effect系列shader - 专用于粒子特效
        // 这些shader虽然不以Particles/开头，但确实用于ParticleSystem
        if (lowerName.Contains("artist") && lowerName.Contains("effect"))
            return true;

        // 其他Effect shader（但排除后处理）
        if (lowerName.Contains("effect") &&
            !lowerName.Contains("postprocess") &&
            !lowerName.Contains("post_process"))
            return true;

        return false;
    }
    
    /// <summary>
    /// 为内置粒子材质写入数据，基于 ParticleMaterial.lmat 模板
    /// 模板结构: {"version":"LAYAMATERIAL:04","props":{"textures":[{"name":"u_texture"}],
    /// "type":"PARTICLESHURIKEN","renderQueue":3000,"materialRenderMode":2,"s_Cull":2,
    /// "s_Blend":1,"s_BlendSrc":6,"s_BlendDst":7,"s_DepthTest":1,"s_DepthWrite":false,
    /// "u_Tintcolor":[0.5,0.5,0.5,1],"defines":["TINTCOLOR"]}}
    /// </summary>
    private void WriteBuiltinParticleMaterial(Material material, JSONObject jsonData, ResoureMap resoureMap)
    {
        jsonData.AddField("version", "LAYAMATERIAL:04");
        JSONObject props = new JSONObject(JSONObject.Type.OBJECT);
        jsonData.AddField("props", props);
        
        // 纹理数组 - 内置材质通常没有可导出的纹理，添加空占位符
        JSONObject textures = new JSONObject(JSONObject.Type.ARRAY);
        JSONObject emptyTexture = new JSONObject(JSONObject.Type.OBJECT);
        emptyTexture.AddField("name", "u_texture");
        textures.Add(emptyTexture);
        props.AddField("textures", textures);
        
        // 材质类型
        props.AddField("type", "PARTICLESHURIKEN");
        
        // 渲染队列
        props.AddField("renderQueue", material.renderQueue > 0 ? material.renderQueue : 3000);
        
        // 材质渲染模式 - 2=透明/Additive
        props.AddField("materialRenderMode", 2);
        
        // 剔除模式
        int cullMode = 2;
        if (material.HasProperty("_Cull"))
        {
            cullMode = material.GetInt("_Cull");
        }
        props.AddField("s_Cull", cullMode);
        
        // 混合设置
        props.AddField("s_Blend", 1);
        
        // 源混合因子和目标混合因子 - 根据 shader 名称判断
        int srcBlend = 6; // SrcAlpha
        int dstBlend = 7; // OneMinusSrcAlpha
        
        string shaderName = material.shader.name.ToLower();
        if (shaderName.Contains("additive"))
        {
            srcBlend = 6; // SrcAlpha
            dstBlend = 1; // One
        }
        else if (shaderName.Contains("multiply"))
        {
            srcBlend = 4; // DstColor
            dstBlend = 0; // Zero
        }
        else if (shaderName.Contains("premultiply"))
        {
            srcBlend = 1; // One
            dstBlend = 7; // OneMinusSrcAlpha
        }
        
        // 如果材质有这些属性，使用材质的值
        if (material.HasProperty("_SrcBlend"))
        {
            srcBlend = ConvertUnityBlendToLaya(material.GetInt("_SrcBlend"));
        }
        if (material.HasProperty("_DstBlend"))
        {
            dstBlend = ConvertUnityBlendToLaya(material.GetInt("_DstBlend"));
        }
        
        props.AddField("s_BlendSrc", srcBlend);
        props.AddField("s_BlendDst", dstBlend);
        
        // 深度测试
        props.AddField("s_DepthTest", 1);
        
        // 深度写入 - 粒子通常关闭
        props.AddField("s_DepthWrite", false);
        
        // 颜色
        Color tintColor = new Color(0.5f, 0.5f, 0.5f, 1.0f);
        if (material.HasProperty("_TintColor"))
        {
            tintColor = material.GetColor("_TintColor");
        }
        else if (material.HasProperty("_Color"))
        {
            tintColor = material.GetColor("_Color");
        }
        else if (material.HasProperty("_BaseColor"))
        {
            tintColor = material.GetColor("_BaseColor");
        }
        
        JSONObject colorValue = new JSONObject(JSONObject.Type.ARRAY);
        colorValue.Add(tintColor.r);
        colorValue.Add(tintColor.g);
        colorValue.Add(tintColor.b);
        colorValue.Add(tintColor.a);
        props.AddField("u_Tintcolor", colorValue);
        
        // Defines
        JSONObject defines = new JSONObject(JSONObject.Type.ARRAY);
        defines.Add("TINTCOLOR");

        // ⭐ Add RENDERMODE_MESH define for particle mesh rendering mode
        if (m_isParticleMeshMode)
        {
            defines.Add("RENDERMODE_MESH");
            Debug.Log($"LayaAir3D: Added RENDERMODE_MESH define for built-in particle material in mesh mode");
        }

        props.AddField("defines", defines);
    }
    
    /// <summary>
    /// Unity BlendMode 转换为 LayaAir BlendFactor
    /// </summary>
    private static int ConvertUnityBlendToLaya(int unityBlend)
    {
        switch (unityBlend)
        {
            case 0: return 0;  // Zero
            case 1: return 1;  // One
            case 2: return 4;  // DstColor
            case 3: return 2;  // SrcColor
            case 4: return 5;  // OneMinusDstColor
            case 5: return 6;  // SrcAlpha
            case 6: return 3;  // OneMinusSrcColor
            case 7: return 8;  // DstAlpha
            case 8: return 9;  // OneMinusDstAlpha
            case 9: return 6;  // SrcAlphaSaturate -> SrcAlpha
            case 10: return 7; // OneMinusSrcAlpha
            default: return 1;
        }
    }

    protected override string getOutFilePath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return "default_material.lmat";
        }
        // 修复：安全地获取不带扩展名的路径
        int dotIndex = path.LastIndexOf('.');
        string basePath = dotIndex >= 0 ? path.Substring(0, dotIndex) : path;
        return GameObjectUitls.cleanIllegalChar(basePath, false) + ".lmat";
    }

    public override void SaveFile(Dictionary<string, FileData> exportFiles)
    {
        base.saveMeta();
        string jsonContent = this.jsonData.Print(true);
     
        FileStream fs = new FileStream(outPath, FileMode.Create, FileAccess.Write);
        StreamWriter writer = new StreamWriter(fs);
        writer.Write(jsonContent);
        writer.Close();
    }


}
