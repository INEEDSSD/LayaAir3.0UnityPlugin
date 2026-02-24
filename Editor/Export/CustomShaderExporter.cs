using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// LayaAir Shader类型枚举
/// 对应LayaAir引擎中的 ShaderFeatureType
/// </summary>
public enum LayaShaderType
{
    None = -1,          // 无类型
    Default = 0,        // 默认类型
    D3 = 1,             // 3D渲染Shader（PBR、BlinnPhong、Unlit等3D物体材质）
    D2_primitive = 2,   // 2D图元Shader
    D2_TextureSV = 3,   // 2D纹理Shader
    D2_BaseRenderNode2D = 4, // 2D基础渲染节点Shader
    PostProcess = 5,    // 后处理Shader
    Sky = 6,            // 天空盒Shader
    Effect = 7          // 特效Shader（粒子系统材质、Trail拖尾等）
}

/// <summary>
/// LayaAir 材质类型（对应MetarialPropData.json中的targeName）
/// </summary>
public enum LayaMaterialType
{
    Unknown,            // 未知类型
    PBR,                // PBR材质 (Standard, URP/Lit等)
    Unlit,              // 无光照材质
    BLINNPHONG,         // BlinnPhong光照材质
    SkyBox,             // 天空盒材质
    SkyProcedural,      // 程序化天空盒
    SkyPanoramic,       // 全景天空盒
    PARTICLESHURIKEN,   // 粒子材质
    Custom              // 自定义材质（需要生成自定义Shader）
}

/// <summary>
/// 自定义Shader导出器 - 将Unity自定义Shader导出为LayaAir .shader文件
/// 并导出使用该Shader的材质
/// 
/// 转换规则参考: Unity2Laya_ShaderMapping.md
/// 示例参考: FishStandard_Base_ori.shader -> FishStandard_Base.shader
/// </summary>
internal class CustomShaderExporter
{
    // ==================== 混合架构支持 ====================

    // 映射引擎实例
    private static ShaderMappingEngine mappingEngine = null;

    // 是否使用映射表模式
    private static bool useMappingTableMode = false;

    // 映射表模式是否已初始化
    private static bool mappingEngineInitialized = false;

    // 性能统计
    private static System.Diagnostics.Stopwatch conversionTimer = new System.Diagnostics.Stopwatch();
    private static long builtInConversionTime = 0;
    private static long mappingTableConversionTime = 0;

    // ==================== 原有字段 ====================

    // 已导出的Shader缓存，避免重复导出
    private static HashSet<string> exportedShaders = new HashSet<string>();

    // Unity到Laya的属性名映射表（扩展版）
    private static readonly Dictionary<string, string> PropertyNameMappings = new Dictionary<string, string>
    {
        // 基础颜色/纹理
        { "_MainTex", "u_AlbedoTexture" },
        { "_BaseMap", "u_AlbedoTexture" },
        { "_Color", "u_AlbedoColor" },
        { "_BaseColor", "u_AlbedoColor" },
        { "_ColorIntensity", "u_ColorIntensity" },
        { "_Alpha", "u_Alpha" },
        
        // 粒子/Effect专用（参考Unity2Laya_ShaderMapping.md）
        { "_LayerColor", "u_LayerColor" },
        { "_LayerMultiplier", "u_LayerMultiplier" },
        
        // 法线
        { "_BumpMap", "u_NormalTexture" },
        { "_NormalMap", "u_NormalTexture" },
        { "_BumpScale", "u_NormalScale" },
        { "_NormalScale", "u_NormalScale" },
        
        // PBR属性
        { "_Metallic", "u_Metallic" },
        { "_MetallicRemapMin", "u_MetallicRemapMin" },
        { "_MetallicRemapMax", "u_MetallicRemapMax" },
        { "_Smoothness", "u_Smoothness" },
        { "_SmoothnessRemapMin", "u_SmoothnessRemapMin" },
        { "_SmoothnessRemapMax", "u_SmoothnessRemapMax" },
        { "_OcclusionStrength", "u_OcclusionStrength" },
        { "_MAER", "u_MAER" },
        
        // Alpha测试
        { "_Cutoff", "u_AlphaTestValue" },
        { "_AlphaCutoff", "u_AlphaTestValue" },
        
        // 自发光
        { "_EmissionColor", "u_EmissionColor" },
        { "_EmissionTexture", "u_EmissionTexture" },
        { "_EmissionScale", "u_EmissionIntensity" },
        
        // Mask贴图
        { "_Mask", "u_Mask" },
        
        // IBL
        { "_IBLMap", "u_IBLMap" },
        { "_IBLMapColor", "u_IBLMapColor" },
        { "_IBLMapIntensity", "u_IBLMapIntensity" },
        { "_IBLMapPower", "u_IBLMapPower" },
        { "_IBLMapRotateX", "u_IBLMapRotateX" },
        { "_IBLMapRotateY", "u_IBLMapRotateY" },
        { "_IBLMapRotateZ", "u_IBLMapRotateZ" },
        
        // Matcap
        { "_MatcapMap", "u_MatcapMap" },
        { "_MatcapAngle", "u_MatcapAngle" },
        { "_MatcapStrength", "u_MatcapStrength" },
        { "_MatcapPow", "u_MatcapPow" },
        { "_MatcapColor", "u_MatcapColor" },
        
        // Matcap Add
        { "_MatcapAddMap", "u_MatcapAddMap" },
        { "_MatcapAddAngle", "u_MatcapAddAngle" },
        { "_MatcapAddStrength", "u_MatcapAddStrength" },
        { "_MatcapAddPow", "u_MatcapAddPow" },
        { "_MatcapAddColor", "u_MatcapAddColor" },
        
        // NPR卡通渲染
        { "_MedColor", "u_MedColor" },
        { "_MedThreshold", "u_MedThreshold" },
        { "_MedSmooth", "u_MedSmooth" },
        { "_ShadowColor", "u_ShadowColor" },
        { "_ShadowThreshold", "u_ShadowThreshold" },
        { "_ShadowSmooth", "u_ShadowSmooth" },
        { "_ReflectColor", "u_ReflectColor" },
        { "_ReflectThreshold", "u_ReflectThreshold" },
        { "_ReflectSmooth", "u_ReflectSmooth" },
        { "_GIIntensity", "u_GIIntensity" },
        
        // 高光
        { "_SpecularHighlights", "u_SpecularHighlights" },
        { "_GGXSpecular", "u_GGXSpecular" },
        { "_SpecularColor", "u_SpecularColor" },
        { "_SpecularIntensity", "u_SpecularIntensity" },
        { "_SpecularLightOffset", "u_SpecularLightOffset" },
        { "_SpecularThreshold", "u_SpecularThreshold" },
        { "_SpecularSmooth", "u_SpecularSmooth" },
        
        // Fresnel
        { "_DirectionalFresnel", "u_DirectionalFresnel" },
        { "_FresnelColor", "u_FresnelColor" },
        { "_fresnelOffset", "u_fresnelOffset" },
        { "_FresnelThreshold", "u_FresnelThreshold" },
        { "_FresnelSmooth", "u_FresnelSmooth" },
        { "_FresnelIntensity", "u_FresnelIntensity" },
        { "_FresnelMetallic", "u_FresnelMetallic" },
        { "_FresnelFit", "u_FresnelFit" },
        
        // Rim边缘光
        { "_RimColor", "u_RimColor" },
        { "_RimPower", "u_RimPower" },
        { "_RimIntensity", "u_RimIntensity" },
        { "_RimStart", "u_RimStart" },
        { "_RimEnd", "u_RimEnd" },
        { "_RimOffset", "u_RimOffset" },
        
        // HSV调整
        { "_AdjustHSV", "u_AdjustHSV" },
        { "_AdjustHue", "u_AdjustHue" },
        { "_AdjustSaturation", "u_AdjustSaturation" },
        { "_AdjustValue", "u_AdjustValue" },
        
        // 对比度
        { "_OriginalColor", "u_OriginalColor" },
        { "_Contrast", "u_Contrast" },
        { "_ContrastScale", "u_ContrastScale" },
        
        // Tonemapping
        { "_u_ToneWeight", "u_ToneWeight" },
        { "_u_WhitePoint", "u_WhitePoint" },
        
        // 自定义光照
        { "_SelfLight", "u_SelfLight" },
        { "_SelfLightDir", "u_SelfLightDir" },
    };

    // Unity到Laya的纹理宏定义映射表
    private static readonly Dictionary<string, string> TextureDefineMappings = new Dictionary<string, string>
    {
        { "_MainTex", "ALBEDOTEXTURE" },
        { "_BaseMap", "ALBEDOTEXTURE" },
        { "_BumpMap", "NORMALTEXTURE" },
        { "_NormalMap", "NORMALTEXTURE" },
        { "_MAER", "MAERMAP" },
        { "_Mask", "MASKMAP" },
        { "_IBLMap", "IBLMAP" },
        { "_MatcapMap", "MATCAPMAP" },
        { "_MatcapAddMap", "MATCAPADDMAP" },
        { "_EmissionTexture", "EMISSIONTEXTURE" },
    };

    /// <summary>
    /// 清除导出缓存（每次导出开始时调用）
    /// </summary>
    public static void ClearCache()
    {
        exportedShaders.Clear();
    }

    /// <summary>
    /// 自动导出自定义Shader材质
    /// </summary>
    public static void WriteAutoCustomShaderMaterial(Material material, JSONObject jsonData, ResoureMap resoureMap, MaterialFile materialFile = null)
    {
        if (material == null)
        {
            Debug.LogError("LayaAir3D: Invalid material for custom shader export");
            return;
        }

        Shader shader = material.shader;
        string shaderName = shader.name;

        // 生成LayaAir Shader名称（去除路径分隔符，转换为合法名称）
        string layaShaderName = GenerateLayaShaderName(shaderName);

        Debug.Log($"LayaAir3D: Exporting custom shader material: {material.name} (Shader: {shaderName} -> {layaShaderName})");

        // 导出Shader文件（如果还没导出过）
        if (!exportedShaders.Contains(shaderName))
        {
            ExportShaderFile(shader, layaShaderName, resoureMap, materialFile);
            exportedShaders.Add(shaderName);
        }

        // 导出材质文件 (⭐ 传递materialFile以支持粒子Mesh模式检测)
        ExportMaterialFile(material, shader, layaShaderName, jsonData, resoureMap, materialFile);
    }

    /// <summary>
    /// 生成LayaAir Shader名称
    /// </summary>
    private static string GenerateLayaShaderName(string unityShaderName)
    {
        // 移除路径分隔符，转换为合法名称
        string name = unityShaderName.Replace("/", "_").Replace(" ", "_").Replace("-", "_");
        // 移除特殊字符
        name = Regex.Replace(name, @"[^a-zA-Z0-9_]", "");
        return name;
    }

    /// <summary>
    /// 导出Shader文件
    /// </summary>
    private static void ExportShaderFile(Shader shader, string layaShaderName, ResoureMap resoureMap, MaterialFile materialFile = null)
    {
        // 收集Shader属性
        List<ShaderProperty> properties = CollectShaderProperties(shader);

        // 尝试读取Unity Shader源代码
        string shaderPath = AssetDatabase.GetAssetPath(shader);
        string shaderSourceCode = null;

        if (!string.IsNullOrEmpty(shaderPath) && File.Exists(shaderPath))
        {
            shaderSourceCode = File.ReadAllText(shaderPath);
            Debug.Log($"LayaAir3D: Read shader source from: {shaderPath}");
        }

        // 生成Shader文件内容
        string shaderContent;
        if (!string.IsNullOrEmpty(shaderSourceCode))
        {
            // 有源代码，进行HLSL到GLSL的转换（ConvertUnityShaderToLaya内部已调用FormatShaderContent）
            shaderContent = ConvertUnityShaderToLaya(layaShaderName, properties, shader.name, shaderSourceCode, materialFile);
        }
        else
        {
            // 没有源代码，使用模板生成
            shaderContent = GenerateShaderFileContent(layaShaderName, properties, shader.name, materialFile);
            // 格式化模板生成的shader内容
            shaderContent = FormatShaderContent(shaderContent);
        }

        // ⭐ 修复GLSL类型不匹配问题 (v_Texcoord0的vec4/vec2转换)
        shaderContent = FixShaderTypeMismatch(shaderContent);

        // ⭐ 全面的类型检查和自动修复 (检测所有赋值中的类型不匹配)
        shaderContent = ComprehensiveTypeCheck(shaderContent);

        // ⭐ 验证shader内容，检测潜在的类型不匹配问题
        ValidateShaderContent(shaderContent, layaShaderName);

        // 创建Shader文件 - 放置在Shaders文件夹中
        string outputPath = "Shaders/" + layaShaderName + ".shader";
        ShaderFile shaderFile = new ShaderFile(outputPath, shaderContent);
        resoureMap.AddExportFile(shaderFile);

        Debug.Log($"LayaAir3D: Generated shader file: {outputPath} (ShaderType detected from: {shader.name})");
    }

    #region HLSL to GLSL Converter

    /// <summary>
    /// 判断shader是否是粒子系统shader（而非mesh shader）
    /// </summary>
    private static bool IsParticleShader(LayaMaterialType materialType, string unityShaderName, string sourceCode, MaterialFile materialFile = null)
    {
        // ⭐ 优先级1: 检查实际的渲染器组件类型（最准确的方法）
        if (materialFile != null)
        {
            bool usedByParticle = materialFile.IsUsedByParticleSystem();
            bool usedByMesh = materialFile.IsUsedByMeshRenderer();

            Debug.Log($"LayaAir3D: Checking renderer usage for '{unityShaderName}' - UsedByParticle: {usedByParticle}, UsedByMesh: {usedByMesh}");

            // ⭐ 关键：只要被ParticleSystemRenderer使用，就判定为粒子shader
            // 即使同时被MeshRenderer使用，粒子系统优先（因为粒子系统的Mesh渲染模式）
            if (usedByParticle)
            {
                Debug.Log($"LayaAir3D: Detected as ParticleShader (Used by ParticleSystemRenderer): {unityShaderName}");
                return true;
            }

            // 明确只被MeshRenderer使用
            if (usedByMesh && !usedByParticle)
            {
                Debug.Log($"LayaAir3D: Detected as MeshShader (Only used by MeshRenderer/SkinnedMeshRenderer): {unityShaderName}");
                return false;
            }

            // 如果没有任何renderer使用信息（usedByParticle和usedByMesh都为false）
            // 继续使用后续的启发式检测
            Debug.Log($"LayaAir3D: No renderer usage info found, using heuristic detection for: {unityShaderName}");
        }
        else
        {
            Debug.Log($"LayaAir3D: MaterialFile is null, using heuristic detection for: {unityShaderName}");
        }

        // 优先级2: 明确的粒子材质类型
        if (materialType == LayaMaterialType.PARTICLESHURIKEN)
        {
            Debug.Log($"LayaAir3D: Detected as ParticleShader (MaterialType: PARTICLESHURIKEN)");
            return true;
        }

        // 优先级3: shader名称检查
        string lowerName = unityShaderName.ToLower();

        // 明确包含Particle关键字（最明确的粒子shader标识）
        if (lowerName.Contains("particle") || lowerName.Contains("shurike") || lowerName.Contains("trail"))
        {
            Debug.Log($"LayaAir3D: Detected as ParticleShader (Name contains particle keywords): {unityShaderName}");
            return true;
        }

        // Artist_Effect系列是粒子特效shader
        if (lowerName.Contains("artist") && lowerName.Contains("effect"))
        {
            Debug.Log($"LayaAir3D: Detected as ParticleShader (Artist_Effect series): {unityShaderName}");
            return true;
        }

        // BR_Effect系列也是粒子特效shader
        if (lowerName.Contains("br_effect") || lowerName.Contains("breffect"))
        {
            Debug.Log($"LayaAir3D: Detected as ParticleShader (BR_Effect series): {unityShaderName}");
            return true;
        }

        // 包含"effect"但明确不是mesh shader的特征
        if (lowerName.Contains("effect"))
        {
            // 粒子相关关键字
            if (lowerName.Contains("particle") ||
                lowerName.Contains("shurike") ||
                lowerName.Contains("trail") ||
                lowerName.Contains("additive") ||
                lowerName.Contains("alpha blend") ||
                lowerName.Contains("multiply") ||
                lowerName.Contains("blend"))
            {
                Debug.Log($"LayaAir3D: Detected as ParticleShader (Effect shader with particle keywords): {unityShaderName}");
                return true;
            }
        }

        // 优先级3: 检查源码特征（仅在名称不明确时使用）
        if (!string.IsNullOrEmpty(sourceCode))
        {
            // 粒子shader的明确特征（高优先级）
            bool hasParticleFeature = sourceCode.Contains("ParticleSystem") ||
                                     sourceCode.Contains("PARTICLE_") ||
                                     sourceCode.Contains("Billboard") ||
                                     sourceCode.Contains("_RENDERMODE_") ||
                                     sourceCode.Contains("a_DirectionTime") ||
                                     sourceCode.Contains("a_ShapePosition");

            if (hasParticleFeature)
            {
                Debug.Log($"LayaAir3D: Detected as ParticleShader (has particle features in code)");
                return true;
            }

            // Mesh shader的明显特征
            bool hasMeshVertexInput = (sourceCode.Contains(": POSITION") || sourceCode.Contains(": SV_POSITION")) &&
                                      (sourceCode.Contains(": TEXCOORD") || sourceCode.Contains("TEXCOORD0"));

            // Mesh shader的常见函数调用
            bool hasMeshFunction = sourceCode.Contains("UnityObjectToClipPos") ||
                                  sourceCode.Contains("mul(UNITY_MATRIX_MVP") ||
                                  sourceCode.Contains("mul(unity_MatrixMVP") ||
                                  sourceCode.Contains("mul(unity_ObjectToWorld");

            // ⭐ 重要：只有在明确是mesh特征，且shader名称不包含effect关键字时，才判定为mesh shader
            // 因为很多粒子shader也使用标准顶点输入格式
            if ((hasMeshVertexInput || hasMeshFunction) && !lowerName.Contains("effect"))
            {
                Debug.Log($"LayaAir3D: Detected as MeshShader (has mesh features, no effect keyword)");
                return false;
            }
        }

        // 优先级4: Effect类型的默认判定
        // ⭐ 重要：包含"effect"的shader，默认判定为粒子shader（因为大多数effect shader用于粒子系统）
        if (lowerName.Contains("effect"))
        {
            Debug.Log($"LayaAir3D: Detected as ParticleShader (Effect shader - default to particle): {unityShaderName}");
            return true;
        }

        // 5. 其他情况默认为mesh shader
        Debug.Log($"LayaAir3D: Defaulting to MeshShader (cannot determine)");
        return false;
    }

    /// <summary>
    /// 将Unity Shader源代码转换为LayaAir Shader
    /// </summary>
    private static string ConvertUnityShaderToLaya(string layaShaderName, List<ShaderProperty> properties,
        string unityShaderName, string sourceCode, MaterialFile materialFile = null)
    {
        StringBuilder sb = new StringBuilder();

        // 检测材质类型
        LayaMaterialType materialType = DetectMaterialType(unityShaderName);

        // 解析Unity Shader代码
        ShaderParseResult parseResult = ParseUnityShader(sourceCode);

        // 保存properties到parseResult，供后续使用
        parseResult.properties = properties;

        // ⭐ 关键修复：先检测是否是粒子shader，再确定ShaderType
        // 这样可以确保粒子shader使用正确的ShaderType (Effect)
        parseResult.isParticleBillboard = IsParticleShader(materialType, unityShaderName, sourceCode, materialFile);

        // 根据isParticleBillboard结果确定ShaderType
        LayaShaderType shaderType;
        if (parseResult.isParticleBillboard)
        {
            // 粒子shader统一使用Effect类型
            shaderType = LayaShaderType.Effect;
            Debug.Log($"LayaAir3D: Particle shader detected, using ShaderType: Effect");
        }
        else if (materialType == LayaMaterialType.Custom)
        {
            shaderType = DetectCustomShaderType(unityShaderName, properties);
        }
        else
        {
            shaderType = GetShaderTypeFromMaterialType(materialType);
        }

        string shaderTypeStr = GetShaderTypeString(shaderType);

        Debug.Log($"LayaAir3D: Converting shader '{unityShaderName}' - MaterialType: {materialType}, ShaderType: {shaderType}, IsParticle: {parseResult.isParticleBillboard}");
        
        // ==================== Shader3D 配置块 ====================
        sb.AppendLine("Shader3D Start");
        sb.AppendLine("{");
        sb.AppendLine($"    type:Shader3D,");
        sb.AppendLine($"    name:{layaShaderName},");
        sb.AppendLine(parseResult.isParticleBillboard ? "    enableInstancing:true," : "    enableInstancing:false,");
        sb.AppendLine("    supportReflectionProbe:false,");
        sb.AppendLine($"    shaderType:{shaderTypeStr},");
        
        // uniformMap
        sb.AppendLine("    uniformMap:{");
        GenerateUniformMapFromProperties(sb, properties, parseResult);
        sb.AppendLine("    },");
        
        // defines - 从shader_feature和multi_compile提取
        sb.AppendLine("    defines: {");
        GenerateDefinesFromParseResult(sb, parseResult);
        sb.AppendLine("    },");
        
        // attributeMap - 根据shader类型声明不同的顶点属性
        if (parseResult.isParticleBillboard)
        {
            // 粒子shader的attributeMap
            sb.AppendLine("    attributeMap: {");
            sb.AppendLine("        a_DirectionTime: Vector4,");
            sb.AppendLine("        a_MeshPosition: Vector3,");
            sb.AppendLine("        a_MeshColor: Vector4,");
            sb.AppendLine("        a_MeshTextureCoordinate: Vector2,");
            sb.AppendLine("        a_ShapePositionStartLifeTime: Vector4,");
            sb.AppendLine("        a_CornerTextureCoordinate: Vector4,");
            sb.AppendLine("        a_StartColor: Vector4,");
            sb.AppendLine("        a_EndColor: Vector4,");
            sb.AppendLine("        a_StartSize: Vector3,");
            sb.AppendLine("        a_StartRotation0: Vector3,");
            sb.AppendLine("        a_StartSpeed: Float,");
            sb.AppendLine("        a_Random0: Vector4,");
            sb.AppendLine("        a_Random1: Vector4,");
            sb.AppendLine("        a_SimulationWorldPostion: Vector3,");
            sb.AppendLine("        a_SimulationWorldRotation: Vector4,");
            sb.AppendLine("        a_SimulationUV: Vector4");
            sb.AppendLine("    },");
        }
        else
        {
            // Mesh shader的attributeMap（标准mesh属性）
            sb.AppendLine("    attributeMap: {");
            sb.AppendLine("        a_Position: Vector3,");
            sb.AppendLine("        a_Normal: Vector3,");
            sb.AppendLine("        a_Color: Vector4,");
            sb.AppendLine("        a_Texcoord0: Vector2,");

            // 如果有自定义数据，添加UV1
            bool hasCustomData = false;
            foreach (var prop in properties)
            {
                if (prop.unityName.Contains("CustomData") || prop.unityName.Contains("_Custom"))
                {
                    hasCustomData = true;
                    break;
                }
            }

            if (hasCustomData)
            {
                sb.AppendLine("        a_Tangent0: Vector4,");
                sb.AppendLine("        a_Texcoord1: Vector2");
            }
            else
            {
                sb.AppendLine("        a_Tangent0: Vector4");
            }

            sb.AppendLine("    },");
        }
        
        // shaderPass
        sb.AppendLine("    shaderPass:[");
        sb.AppendLine("        {");
        sb.AppendLine("            pipeline:Forward,");
        sb.AppendLine($"            VS:{layaShaderName}VS,");
        sb.AppendLine($"            FS:{layaShaderName}FS");
        sb.AppendLine("        }");
        sb.AppendLine("    ]");
        sb.AppendLine("}");
        sb.AppendLine("Shader3D End");
        sb.AppendLine();
        
        // ==================== GLSL 代码块 ====================
        sb.AppendLine("GLSL Start");
        
        // 生成顶点着色器（会收集所有varying并保存到parseResult）
        GenerateConvertedVertexShader(sb, layaShaderName, parseResult);
        
        // 生成片元着色器（使用VS中保存的varying，确保一致）
        GenerateConvertedFragmentShader(sb, layaShaderName, parseResult);
        
        sb.AppendLine("GLSL End");
        
        // 验证和清理生成的代码
        string result = sb.ToString();
        result = ValidateAndCleanGLSL(result);

        // 格式化shader内容
        result = FormatShaderContent(result);

        // 生成转换总结报告
        GenerateConversionSummary(parseResult, properties, result);

        return result;
    }

    /// <summary>
    /// 生成shader转换总结报告
    /// </summary>
    private static void GenerateConversionSummary(ShaderParseResult parseResult, List<ShaderProperty> properties, string resultCode)
    {
        Debug.Log("==================== Shader Export Summary ====================");
        Debug.Log($"Shader Name: {parseResult.shaderName}");
        Debug.Log($"Shader Type: {(parseResult.isParticleBillboard ? "Particle System" : "Mesh Effect")}");

        // 架构模式信息
        if (useMappingTableMode)
        {
            Debug.Log($"Architecture: Hybrid (Mapping Table Mode)");
            Debug.Log($"  └─ Mapping table rules applied first");
            Debug.Log($"  └─ Built-in rules as fallback");
        }
        else
        {
            Debug.Log($"Architecture: Built-in (Hardcoded Rules Mode)");
            Debug.Log($"  └─ All rules from C# code");
        }

        Debug.Log("");

        // 统计信息
        int propertyCount = properties != null ? properties.Count : 0;
        int defineCount = parseResult.shaderFeatures.Count + parseResult.multiCompiles.Count;
        int varyingCount = parseResult.collectedVaryings != null ? parseResult.collectedVaryings.Count : 0;
        int codeLines = resultCode.Split('\n').Length;

        Debug.Log($"Properties: {propertyCount}");
        Debug.Log($"Defines: {defineCount}");
        Debug.Log($"Varyings: {varyingCount}");
        Debug.Log($"Total Lines: {codeLines}");
        Debug.Log("");

        // 功能检测
        Debug.Log("Detected Features:");
        List<string> features = new List<string>();

        if (HasPropertyByName(parseResult, "DetailTex2")) features.Add("✓ 3-Layer Textures");
        else if (HasPropertyByName(parseResult, "DetailTex")) features.Add("✓ 2-Layer Textures");
        else features.Add("✓ Single Texture");

        if (HasPropertyByName(parseResult, "Dissolve")) features.Add("✓ Dissolve Effect");
        if (HasPropertyByName(parseResult, "FadeEdge")) features.Add("✓ Fade Edge");
        if (HasPropertyByName(parseResult, "Distort")) features.Add("✓ Distortion");
        if (HasPropertyByName(parseResult, "Rim")) features.Add("✓ Rim Lighting");
        if (HasPropertyByName(parseResult, "Lighting") || HasPropertyByName(parseResult, "MainLight")) features.Add("✓ Custom Lighting");
        if (HasPropertyByName(parseResult, "VertexOffset") || HasPropertyByName(parseResult, "VertexAmplitude")) features.Add("✓ Vertex Offset");
        if (HasPropertyByName(parseResult, "Rotate") || HasPropertyByName(parseResult, "RotateAngle")) features.Add("✓ UV Rotation");
        if (HasPropertyByName(parseResult, "Scroll")) features.Add("✓ UV Scrolling");
        if (HasPropertyByName(parseResult, "NormalMap")) features.Add("✓ Normal Mapping");
        if (HasPropertyByName(parseResult, "CustomData")) features.Add("✓ Custom Data");
        if (HasPropertyByName(parseResult, "Polar")) features.Add("✓ Polar Coordinates");
        if (HasPropertyByName(parseResult, "GradientMap")) features.Add("✓ Gradient Remap");

        foreach (var feature in features)
        {
            Debug.Log($"  {feature}");
        }

        if (features.Count == 0)
        {
            Debug.Log("  (No special effects detected)");
        }

        Debug.Log("");

        // 警告和建议
        List<string> warnings = new List<string>();
        List<string> suggestions = new List<string>();

        if (resultCode.Contains("vec3(0.0)") || resultCode.Contains("vec4(0.0)"))
        {
            warnings.Add("⚠ Hardcoded zero vectors detected - mesh may not be visible");
        }

        if (!parseResult.isParticleBillboard && !resultCode.Contains("vertex.positionOS"))
        {
            warnings.Add("⚠ Mesh shader does not use vertex.positionOS - may have positioning issues");
        }

        if (varyingCount == 0)
        {
            warnings.Add("⚠ No varyings detected - VS to FS data passing may not work");
        }

        if (varyingCount > 16)
        {
            suggestions.Add("ℹ Many varyings ({varyingCount}) - consider optimizing to reduce interpolator usage");
        }

        if (propertyCount > 50)
        {
            suggestions.Add($"ℹ Many properties ({propertyCount}) - shader may be complex");
        }

        if (parseResult.isParticleBillboard && resultCode.Contains("Particle.shader template not found"))
        {
            warnings.Add("⚠ Particle template not found - using fallback code (may be incomplete)");
        }

        if (warnings.Count > 0)
        {
            Debug.Log("Warnings:");
            foreach (var warning in warnings)
            {
                Debug.LogWarning($"  {warning}");
            }
            Debug.Log("");
        }

        if (suggestions.Count > 0)
        {
            Debug.Log("Suggestions:");
            foreach (var suggestion in suggestions)
            {
                Debug.Log($"  {suggestion}");
            }
            Debug.Log("");
        }

        // 转换率估计
        int estimatedConversionRate = EstimateConversionRate(parseResult, resultCode);
        string rateColor = estimatedConversionRate >= 80 ? "Good" :
                          estimatedConversionRate >= 60 ? "Fair" : "Poor";

        Debug.Log($"Estimated Conversion Rate: ~{estimatedConversionRate}% ({rateColor})");
        Debug.Log("");

        // 性能统计
        if (useMappingTableMode)
        {
            Debug.Log("Performance:");
            Debug.Log($"  Mapping Table: {mappingTableConversionTime}ms");
            if (builtInConversionTime > 0)
            {
                Debug.Log($"  Built-in Fallback: {builtInConversionTime}ms");
                Debug.Log($"  Total: {mappingTableConversionTime + builtInConversionTime}ms");
            }
            Debug.Log("");
        }
        else
        {
            Debug.Log("Performance:");
            Debug.Log($"  Built-in Conversion: {builtInConversionTime}ms");
            Debug.Log("");
        }

        if (estimatedConversionRate < 80)
        {
            Debug.Log("Note: Conversion rate below 80% - manual adjustment may be needed");
            Debug.Log("Please test the exported shader in LayaAir and verify rendering");
        }
        else
        {
            Debug.Log("Shader export looks good! Test in LayaAir to confirm");
        }

        Debug.Log("===============================================================");
    }

    /// <summary>
    /// 估算shader转换率
    /// </summary>
    private static int EstimateConversionRate(ShaderParseResult parseResult, string resultCode)
    {
        int score = 100;

        // 基础检查
        if (parseResult.isParticleBillboard)
        {
            // 粒子shader检查
            if (!resultCode.Contains("computeParticlePosition") && !resultCode.Contains("center"))
                score -= 30; // 缺少粒子位置计算

            if (!resultCode.Contains("computeParticleColor") && !resultCode.Contains("v_Color"))
                score -= 10; // 缺少粒子颜色

            if (!resultCode.Contains("Billboard") && !resultCode.Contains("corner"))
                score -= 15; // 缺少Billboard计算
        }
        else
        {
            // Mesh shader检查
            if (!resultCode.Contains("vertex.positionOS") && !resultCode.Contains("a_Position"))
                score -= 40; // 缺少顶点位置

            if (resultCode.Contains("vec3(0.0)") || resultCode.Contains("vec4(0.0)"))
                score -= 20; // 有硬编码零向量

            if (!resultCode.Contains("getWorldMatrix") && !resultCode.Contains("worldMat"))
                score -= 15; // 缺少世界变换
        }

        // 通用检查
        if (parseResult.collectedVaryings == null || parseResult.collectedVaryings.Count == 0)
            score -= 15; // 没有varying

        if (!resultCode.Contains("gl_Position"))
            score -= 25; // 缺少位置输出

        if (!resultCode.Contains("gl_FragColor") && !resultCode.Contains("out"))
            score -= 10; // 缺少颜色输出

        // Unity特有函数未转换
        if (resultCode.Contains("UnityObjectToClipPos"))
            score -= 10;

        if (resultCode.Contains("UNITY_MATRIX"))
            score -= 5;

        return Math.Max(0, Math.Min(100, score));
    }

    /// <summary>
    /// Unity Shader解析结果
    /// </summary>
    private class ShaderParseResult
    {
        public string shaderName;
        public List<string> shaderFeatures = new List<string>();  // shader_feature定义
        public List<string> multiCompiles = new List<string>();   // multi_compile定义
        public List<string> includes = new List<string>();        // include文件
        public Dictionary<string, string> variables = new Dictionary<string, string>(); // 变量声明
        public string vertexStructName = "";  // appdata结构体名称
        public string vertexStruct = "";      // appdata结构体内容
        public string v2fStructName = "";     // v2f结构体名称
        public string v2fStruct = "";         // v2f结构体内容
        public string vertexCode = "";        // 顶点着色器代码
        public string fragmentCode = "";      // 片元着色器代码
        public List<string> customFunctions = new List<string>(); // 自定义函数
        public Dictionary<string, VaryingInfo> varyings = new Dictionary<string, VaryingInfo>(); // varying映射
        public bool usesVertexColor = false;  // 是否使用顶点颜色
        public bool usesUV = false;           // 是否使用UV
        public bool isParticleBillboard = false;  // 是否是粒子Billboard模式
        public Dictionary<string, string> collectedVaryings = new Dictionary<string, string>(); // 收集到的所有varying（VS生成后传递给FS）
        public string varyingDeclarations = "";  // VS生成的varying声明字符串，FS直接复用
        public List<ShaderProperty> properties = new List<ShaderProperty>(); // Shader属性列表
    }

    /// <summary>
    /// Varying信息
    /// </summary>
    private class VaryingInfo
    {
        public string originalName;   // 原始名称（如 uv, color）
        public string glslName;       // GLSL名称（如 v_Texcoord0）
        public string glslType;       // GLSL类型（如 vec2）
        public string semantic;       // 语义（如 TEXCOORD0）
    }

    /// <summary>
    /// 解析Unity Shader源代码
    /// </summary>
    private static ShaderParseResult ParseUnityShader(string sourceCode)
    {
        ShaderParseResult result = new ShaderParseResult();
        
        // 提取Shader名称
        var nameMatch = Regex.Match(sourceCode, @"Shader\s+""([^""]+)""");
        if (nameMatch.Success)
        {
            result.shaderName = nameMatch.Groups[1].Value;
        }
        
        // 提取CGPROGRAM/HLSLPROGRAM块
        var cgMatch = Regex.Match(sourceCode, @"(CGPROGRAM|HLSLPROGRAM)(.*?)(ENDCG|ENDHLSL)", 
            RegexOptions.Singleline | RegexOptions.IgnoreCase);
        
        if (cgMatch.Success)
        {
            string cgCode = cgMatch.Groups[2].Value;
            
            // 提取shader_feature
            var featureMatches = Regex.Matches(cgCode, @"#pragma\s+shader_feature[_\w]*\s+(.+)");
            foreach (Match m in featureMatches)
            {
                string features = m.Groups[1].Value.Trim();
                var parts = features.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    if (!part.StartsWith("_") || part.Contains("_ON"))
                    {
                        result.shaderFeatures.Add(part);
                    }
                }
            }
            
            // 提取multi_compile
            var multiMatches = Regex.Matches(cgCode, @"#pragma\s+multi_compile[_\w]*\s+(.+)");
            foreach (Match m in multiMatches)
            {
                string compiles = m.Groups[1].Value.Trim();
                result.multiCompiles.AddRange(compiles.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries));
            }
            
            // 提取include
            var includeMatches = Regex.Matches(cgCode, @"#include\s+""([^""]+)""");
            foreach (Match m in includeMatches)
            {
                result.includes.Add(m.Groups[1].Value);
            }
            
            // 提取变量声明（支持条件编译块内的变量）
            ExtractVariables(cgCode, result);
            
            // 提取appdata结构体
            var appdataMatch = Regex.Match(cgCode, 
                @"struct\s+(appdata\w*|a2v|VertexInput)\s*\{([^}]+)\}", 
                RegexOptions.Singleline);
            if (appdataMatch.Success)
            {
                result.vertexStructName = appdataMatch.Groups[1].Value;
                result.vertexStruct = appdataMatch.Groups[2].Value;
            }
            
            // 提取v2f结构体
            var v2fMatch = Regex.Match(cgCode, 
                @"struct\s+(v2f|VertexOutput|Varyings)\s*\{([^}]+)\}", 
                RegexOptions.Singleline);
            if (v2fMatch.Success)
            {
                result.v2fStructName = v2fMatch.Groups[1].Value;
                result.v2fStruct = v2fMatch.Groups[2].Value;
                ParseVaryings(result);
            }
            
            // 提取顶点着色器函数 - 使用括号平衡
            result.vertexCode = ExtractFunctionBody(cgCode, "vert");
            
            // 提取片元着色器函数 - 使用括号平衡
            result.fragmentCode = ExtractFunctionBody(cgCode, "frag");
            
            // 提取自定义函数 - 使用平衡括号匹配
            ExtractCustomFunctions(cgCode, result);
            
            // 检测是否使用顶点颜色
            result.usesVertexColor = DetectVertexColorUsage(cgCode, result);
            
            // 检测是否使用UV
            result.usesUV = DetectUVUsage(cgCode, result);
        }
        
        return result;
    }

    /// <summary>
    /// 检测是否使用顶点颜色
    /// </summary>
    private static bool DetectVertexColorUsage(string cgCode, ShaderParseResult result)
    {
        // 检查appdata/v2f结构体中是否有COLOR语义
        if (!string.IsNullOrEmpty(result.vertexStruct))
        {
            if (Regex.IsMatch(result.vertexStruct, @":\s*COLOR\d*\s*;", RegexOptions.IgnoreCase))
                return true;
        }
        if (!string.IsNullOrEmpty(result.v2fStruct))
        {
            if (Regex.IsMatch(result.v2fStruct, @":\s*COLOR\d*\s*;", RegexOptions.IgnoreCase))
                return true;
        }
        
        // 检查代码中是否使用了顶点颜色相关的变量
        // v.color, i.color, o.color, vertex.color, input.color 等
        string[] colorPatterns = new string[]
        {
            @"\bv\.color\b",
            @"\bi\.color\b",
            @"\bo\.color\b",
            @"\bvertex\.color\b",
            @"\binput\.color\b",
            @"\bIN\.color\b",
            @"\bOUT\.color\b",
            @"\bv\.vcolor\b",
            @"\bi\.vcolor\b",
            @"\bvertexColor\b",
            @"\bv_VertexColor\b",
            @"\ba_Color\b"
        };
        
        foreach (var pattern in colorPatterns)
        {
            if (Regex.IsMatch(cgCode, pattern, RegexOptions.IgnoreCase))
                return true;
        }
        
        // 检查varying映射中是否有颜色
        foreach (var kvp in result.varyings)
        {
            if (kvp.Value.semantic != null && kvp.Value.semantic.StartsWith("COLOR", StringComparison.OrdinalIgnoreCase))
                return true;
            if (kvp.Key.ToLower().Contains("color"))
                return true;
        }
        
        return false;
    }

    /// <summary>
    /// 检测是否使用UV
    /// </summary>
    private static bool DetectUVUsage(string cgCode, ShaderParseResult result)
    {
        // 检查appdata/v2f结构体中是否有TEXCOORD语义
        if (!string.IsNullOrEmpty(result.vertexStruct))
        {
            if (Regex.IsMatch(result.vertexStruct, @":\s*TEXCOORD\d*\s*;", RegexOptions.IgnoreCase))
                return true;
        }
        if (!string.IsNullOrEmpty(result.v2fStruct))
        {
            if (Regex.IsMatch(result.v2fStruct, @":\s*TEXCOORD\d*\s*;", RegexOptions.IgnoreCase))
                return true;
        }
        
        // 检查代码中是否使用了UV相关的变量
        string[] uvPatterns = new string[]
        {
            @"\bv\.uv\b",
            @"\bi\.uv\b",
            @"\bo\.uv\b",
            @"\bv\.texcoord\b",
            @"\bi\.texcoord\b",
            @"\btex2D\s*\(",
            @"\btexture2D\s*\(",
            @"\bTRANSFORM_TEX\s*\("
        };
        
        foreach (var pattern in uvPatterns)
        {
            if (Regex.IsMatch(cgCode, pattern, RegexOptions.IgnoreCase))
                return true;
        }
        
        return false;
    }

    /// <summary>
    /// 提取函数体（使用括号平衡）
    /// </summary>
    private static string ExtractFunctionBody(string cgCode, string funcName)
    {
        // 匹配函数声明
        var funcPattern = $@"\b\w+\s+{funcName}\s*\([^)]*\)(?:\s*:\s*\w+)?\s*\{{";
        var match = Regex.Match(cgCode, funcPattern, RegexOptions.Singleline);
        
        if (!match.Success)
            return "";
        
        int braceStart = match.Index + match.Length - 1;
        int braceCount = 1;
        int i = braceStart + 1;
        
        while (i < cgCode.Length && braceCount > 0)
        {
            char c = cgCode[i];
            if (c == '{') braceCount++;
            else if (c == '}') braceCount--;
            i++;
        }
        
        if (braceCount == 0)
        {
            // 返回函数体内容（不包括外层大括号）
            return cgCode.Substring(braceStart + 1, i - braceStart - 2);
        }
        
        return "";
    }

    /// <summary>
    /// 提取变量声明
    /// </summary>
    private static void ExtractVariables(string cgCode, ShaderParseResult result)
    {
        // 匹配各种变量声明格式
        var patterns = new[]
        {
            @"^\s*(sampler2D|samplerCUBE)\s+(\w+)\s*;",
            @"^\s*(float4?|half4?|int|fixed4?|float4x4|half4x4|float3x3|half3x3|float2|half2|float3|half3)\s+(\w+)\s*;",
            @"^\s*(float4?|half4?|int|fixed4?)\s+(\w+)\s*\[\s*\d+\s*\]\s*;"  // 数组
        };
        
        foreach (var pattern in patterns)
        {
            var varMatches = Regex.Matches(cgCode, pattern, RegexOptions.Multiline);
            foreach (Match m in varMatches)
            {
                string varType = m.Groups[1].Value;
                string varName = m.Groups[2].Value;
                if (!result.variables.ContainsKey(varName))
                {
                    result.variables[varName] = varType;
                }
            }
        }
    }

    /// <summary>
    /// 解析v2f结构体中的varying
    /// </summary>
    private static void ParseVaryings(ShaderParseResult result)
    {
        if (string.IsNullOrEmpty(result.v2fStruct))
            return;
        
        // 解析结构体成员
        var memberMatches = Regex.Matches(result.v2fStruct, 
            @"(float4?|half4?|fixed4?|float2|half2|float3|half3)\s+(\w+)\s*:\s*(\w+)");
        
        int texcoordIndex = 0;
        foreach (Match m in memberMatches)
        {
            string type = m.Groups[1].Value;
            string name = m.Groups[2].Value;
            string semantic = m.Groups[3].Value.ToUpper();
            
            // 跳过SV_POSITION
            if (semantic == "SV_POSITION" || semantic == "POSITION")
                continue;
            
            var info = new VaryingInfo
            {
                originalName = name,
                glslType = ConvertTypeToGLSL(type),
                semantic = semantic
            };
            
            // 根据语义确定GLSL名称
            if (semantic.StartsWith("TEXCOORD"))
            {
                int idx = 0;
                if (semantic.Length > 8)
                    int.TryParse(semantic.Substring(8), out idx);
                info.glslName = $"v_Texcoord{idx}";
            }
            else if (semantic == "COLOR" || semantic == "COLOR0")
            {
                info.glslName = "v_VertexColor";
            }
            else if (semantic == "NORMAL")
            {
                info.glslName = "v_NormalWS";
            }
            else
            {
                info.glslName = "v_" + name;
            }
            
            result.varyings[name] = info;
        }
    }

    /// <summary>
    /// 提取自定义函数（使用括号平衡）
    /// </summary>
    private static void ExtractCustomFunctions(string cgCode, ShaderParseResult result)
    {
        // 匹配函数声明 - 支持更多返回类型
        var funcPattern = @"(float2?|float3?|float4?|half2?|half3?|half4?|fixed2?|fixed3?|fixed4?|void|int|bool|float4x4|float3x3|mat4|mat3)\s+(\w+)\s*\([^)]*\)\s*\{";
        var matches = Regex.Matches(cgCode, funcPattern);
        
        HashSet<string> addedFunctions = new HashSet<string>();
        
        foreach (Match m in matches)
        {
            string funcName = m.Groups[2].Value;
            
            // 跳过vert和frag以及已添加的函数
            if (funcName == "vert" || funcName == "frag" || addedFunctions.Contains(funcName))
                continue;
            
            // 找到函数体（使用括号平衡）
            int startIndex = m.Index;
            int braceStart = cgCode.IndexOf('{', startIndex);
            if (braceStart < 0) continue;
            
            int braceCount = 1;
            int i = braceStart + 1;
            while (i < cgCode.Length && braceCount > 0)
            {
                if (cgCode[i] == '{') braceCount++;
                else if (cgCode[i] == '}') braceCount--;
                i++;
            }
            
            if (braceCount == 0)
            {
                string funcCode = cgCode.Substring(startIndex, i - startIndex);
                result.customFunctions.Add(funcCode);
                addedFunctions.Add(funcName);
            }
        }
    }

    // LayaAir引擎内置Uniform变量列表（不需要在材质中重新定义）
    // 参考 LayaAir_BuiltIn_Uniforms.md 文档
    private static readonly HashSet<string> EngineBuiltInUniforms = new HashSet<string>
    {
        // 场景相关 (SceneCommon.glsl)
        "u_Time", "u_FogParams", "u_FogColor", "u_GIRotate", "u_DirationLightCount",

        // 相机相关 (CameraCommon.glsl)
        "u_CameraPos", "u_View", "u_Projection", "u_ViewProjection",
        "u_CameraDirection", "u_CameraUp", "u_Viewport", "u_ProjectionParams",
        "u_OpaqueTextureParams", "u_ZBufferParams",
        "u_CameraDepthTexture", "u_CameraDepthNormalsTexture", "u_CameraOpaqueTexture",

        // 精灵/物体相关 (Sprite3DCommon.glsl)
        "u_WorldMat", "u_WorldInvertFront",

        // 光照相关 (Lighting.glsl)
        "u_DirLightColor", "u_DirLightDirection", "u_DirLightMode",
        "u_PointLightColor", "u_PointLightPos", "u_PointLightRange", "u_PointLightMode",
        "u_SpotLightColor", "u_SpotLightPos", "u_SpotLightDirection", "u_SpotLightRange", "u_SpotLightSpot", "u_SpotLightMode",
        "u_LightBuffer", "u_LightClusterBuffer",

        // 阴影相关 (ShadowCommon.glsl)
        "u_ShadowLightDirection", "u_ShadowBias", "u_ShadowSplitSpheres", "u_ShadowMatrices",
        "u_ShadowMapSize", "u_ShadowParams", "u_SpotShadowMapSize", "u_SpotViewProjectMatrix",
        "u_ShadowMap", "u_SpotShadowMap",

        // 全局光照相关 (globalIllumination.glsl)
        "u_AmbientColor", "u_IblSH", "u_IBLTex", "u_IBLRoughnessLevel", "u_AmbientIntensity", "u_ReflectionIntensity",
        "u_AmbientSHAr", "u_AmbientSHAg", "u_AmbientSHAb", "u_AmbientSHBr", "u_AmbientSHBg", "u_AmbientSHBb", "u_AmbientSHC",
        "u_ReflectTexture", "u_ReflectCubeHDRParams",
        "u_LightMap", "u_LightMapDirection", "u_LightmapScaleOffset",
        "u_SpecCubeProbePosition", "u_SpecCubeBoxMax", "u_SpecCubeBoxMin",

        // 体积光照探针 (VolumetricGI.glsl)
        "u_VolGIProbeCounts", "u_VolGIProbeStep", "u_VolGIProbeStartPosition", "u_VolGIProbeParams",
        "u_ProbeIrradiance", "u_ProbeDistance",

        // 粒子系统相关 (particleShuriKenSpriteVS.glsl)
        // ⭐ 这些uniform由particleShuriKenSpriteVS.glsl提供，不要在uniformMap中重复声明
        "u_CurrentTime", "u_Gravity", "u_DragConstanct",
        "u_WorldPosition", "u_WorldRotation",
        "u_ScalingMode", "u_PositionScale", "u_SizeScale", "u_SimulationSpace",
        "u_VOLSpaceType", "u_VOLVelocityConst", "u_VOLVelocityConstMax",
        // Gradient相关的uniform也由particleShuriKenSpriteVS.glsl提供
        "u_ColorOverLifeGradientAlphas", "u_ColorOverLifeGradientColors", "u_ColorOverLifeGradientRanges",
        "u_MaxColorOverLifeGradientAlphas", "u_MaxColorOverLifeGradientColors", "u_MaxColorOverLifeGradientRanges",
        "u_SOLSizeGradient", "u_SOLSizeGradientMax",
        "u_SOLSizeGradientX", "u_SOLSizeGradientY", "u_SOLSizeGradientZ",
        "u_SOLSizeGradientMaxX", "u_SOLSizeGradientMaxY", "u_SOLSizeGradientMaxZ",
        "u_VOLVelocityGradientX", "u_VOLVelocityGradientY", "u_VOLVelocityGradientZ",
        "u_VOLVelocityGradientMaxX", "u_VOLVelocityGradientMaxY", "u_VOLVelocityGradientMaxZ",

        // Unity RenderPipeline特有，Laya粒子不需要
        "u_Stencil", "u_StencilComp", "u_StencilOp", "u_StencilReadMask", "u_StencilWriteMask", "u_ColorMask"
    };

    /// <summary>
    /// 检查是否是引擎内置Uniform
    /// </summary>
    private static bool IsEngineBuiltInUniform(string uniformName)
    {
        return EngineBuiltInUniforms.Contains(uniformName);
    }

    /// <summary>
    /// 从属性和解析结果生成uniformMap
    /// </summary>
    private static void GenerateUniformMapFromProperties(StringBuilder sb, List<ShaderProperty> properties, ShaderParseResult parseResult)
    {
        sb.AppendLine("        // Basic");
        sb.AppendLine("        u_AlphaTestValue: { type: Float, default: 0.5, range: [0.0, 1.0] },");
        sb.AppendLine("        u_TilingOffset: { type: Vector4, default: [1, 1, 0, 0] },");
        
        // 已添加的属性（包括引擎内置的，避免重复）
        HashSet<string> addedProps = new HashSet<string>(EngineBuiltInUniforms);
        addedProps.Add("u_AlphaTestValue");
        addedProps.Add("u_TilingOffset");
        
        sb.AppendLine();
        sb.AppendLine("        // Shader Properties");
        
        // 收集需要_ST的纹理
        List<string> texturesNeedingST = new List<string>();
        
        foreach (var prop in properties)
        {
            // 跳过引擎内置变量
            if (IsEngineBuiltInUniform(prop.layaName))
            {
                Debug.Log($"LayaAir3D: Skipping engine built-in uniform: {prop.layaName}");
                continue;
            }
            
            if (addedProps.Contains(prop.layaName))
                continue;
            addedProps.Add(prop.layaName);
            
            string uniformLine = GenerateUniformLine(prop);
            sb.AppendLine($"        {uniformLine}");
            
            // 如果是纹理，记录需要生成_ST
            if (prop.type == ShaderUtil.ShaderPropertyType.TexEnv)
            {
                texturesNeedingST.Add(prop.layaName);
            }
        }
        
        // 生成纹理的_ST uniform（用于TRANSFORM_TEX）
        sb.AppendLine();
        sb.AppendLine("        // Texture Tiling/Offset");
        foreach (var texName in texturesNeedingST)
        {
            string stName = texName + "_ST";
            if (!addedProps.Contains(stName) && !IsEngineBuiltInUniform(stName))
            {
                sb.AppendLine($"        {stName}: {{ type: Vector4, default: [1, 1, 0, 0] }},");
                addedProps.Add(stName);
            }
        }
        
        // 添加常用的_ST（可能在代码中使用但不在属性中）
        string[] commonTextures = { "u_MainTex_ST", "u_AlbedoTexture_ST", "u_DetailTex_ST", "u_NormalTexture_ST" };
        foreach (var stName in commonTextures)
        {
            if (!addedProps.Contains(stName) && !IsEngineBuiltInUniform(stName))
            {
                sb.AppendLine($"        {stName}: {{ type: Vector4, default: [1, 1, 0, 0] }},");
                addedProps.Add(stName);
            }
        }

        // ⭐ 添加Scroll相关uniforms（如果检测到有Scroll属性或者是粒子shader）
        // 这些uniforms在GenerateParticleVertexCode中会被使用
        if (parseResult.isParticleBillboard || HasPropertyByName(parseResult, "Scroll"))
        {
            // 基础Scroll uniforms (Layer 0)
            if (!addedProps.Contains("u_Scroll0X"))
            {
                sb.AppendLine($"        u_Scroll0X: {{ type: Float, default: 0.0 }},");
                addedProps.Add("u_Scroll0X");
            }
            if (!addedProps.Contains("u_Scroll0Y"))
            {
                sb.AppendLine($"        u_Scroll0Y: {{ type: Float, default: 0.0 }},");
                addedProps.Add("u_Scroll0Y");
            }

            // Scroll1 uniforms (Layer 1) - 如果有Scroll1或DetailTex属性
            if (HasPropertyByName(parseResult, "Scroll1") || HasPropertyByName(parseResult, "DetailTex"))
            {
                if (!addedProps.Contains("u_Scroll1X"))
                {
                    sb.AppendLine($"        u_Scroll1X: {{ type: Float, default: 0.0 }},");
                    addedProps.Add("u_Scroll1X");
                }
                if (!addedProps.Contains("u_Scroll1Y"))
                {
                    sb.AppendLine($"        u_Scroll1Y: {{ type: Float, default: 0.0 }},");
                    addedProps.Add("u_Scroll1Y");
                }
            }

            // Scroll2 uniforms (Layer 2) - 如果有Scroll2或DetailTex2属性
            if (HasPropertyByName(parseResult, "Scroll2") || HasPropertyByName(parseResult, "DetailTex2"))
            {
                if (!addedProps.Contains("u_Scroll2X"))
                {
                    sb.AppendLine($"        u_Scroll2X: {{ type: Float, default: 0.0 }},");
                    addedProps.Add("u_Scroll2X");
                }
                if (!addedProps.Contains("u_Scroll2Y"))
                {
                    sb.AppendLine($"        u_Scroll2Y: {{ type: Float, default: 0.0 }},");
                    addedProps.Add("u_Scroll2Y");
                }
            }

            // Distort Scroll uniforms - 如果有Distort0属性
            if (HasPropertyByName(parseResult, "Distort0"))
            {
                if (!addedProps.Contains("u_Distort0X"))
                {
                    sb.AppendLine($"        u_Distort0X: {{ type: Float, default: 0.0 }},");
                    addedProps.Add("u_Distort0X");
                }
                if (!addedProps.Contains("u_Distort0Y"))
                {
                    sb.AppendLine($"        u_Distort0Y: {{ type: Float, default: 0.0 }},");
                    addedProps.Add("u_Distort0Y");
                }
            }

            // DissolveDistort Scroll uniforms - 如果有DissolveDistort属性
            if (HasPropertyByName(parseResult, "DissolveDistort"))
            {
                if (!addedProps.Contains("u_DissolveDistortX"))
                {
                    sb.AppendLine($"        u_DissolveDistortX: {{ type: Float, default: 0.0 }},");
                    addedProps.Add("u_DissolveDistortX");
                }
                if (!addedProps.Contains("u_DissolveDistortY"))
                {
                    sb.AppendLine($"        u_DissolveDistortY: {{ type: Float, default: 0.0 }},");
                    addedProps.Add("u_DissolveDistortY");
                }
            }

            // VertexAmplitudeTex Scroll uniforms - 如果有VertexAmplitudeTex属性
            if (HasPropertyByName(parseResult, "VertexAmplitudeTex"))
            {
                if (!addedProps.Contains("u_VertexAmplitudeTexScroll0X"))
                {
                    sb.AppendLine($"        u_VertexAmplitudeTexScroll0X: {{ type: Float, default: 0.0 }},");
                    addedProps.Add("u_VertexAmplitudeTexScroll0X");
                }
                if (!addedProps.Contains("u_VertexAmplitudeTexScroll0Y"))
                {
                    sb.AppendLine($"        u_VertexAmplitudeTexScroll0Y: {{ type: Float, default: 0.0 }},");
                    addedProps.Add("u_VertexAmplitudeTexScroll0Y");
                }
            }
        }

        // ⭐ 不需要手动添加粒子系统uniforms
        // 所有粒子系统相关的uniform（u_CurrentTime, u_Gravity, u_WorldPosition等）
        // 都由particleShuriKenSpriteVS.glsl提供，已添加到EngineBuiltInUniforms列表中
        // 重复声明会导致编译错误
    }

    /// <summary>
    /// 从解析结果生成defines
    /// </summary>
    private static void GenerateDefinesFromParseResult(StringBuilder sb, ShaderParseResult parseResult)
    {
        HashSet<string> addedDefines = new HashSet<string>();
        
        // 粒子shader使用TINTCOLOR/ADDTIVEFOG/RENDERMODE_MESH（参考Particle.shader模板），非粒子使用COLOR/ENABLEVERTEXCOLOR
        if (parseResult.isParticleBillboard)
        {
            // ⭐ 粒子mesh模式：添加RENDERMODE_MESH define（用于区分mesh和billboard模式）
            sb.AppendLine("        RENDERMODE_MESH: { type: bool, default: false },");
            addedDefines.Add("RENDERMODE_MESH");

            sb.AppendLine("        TINTCOLOR: { type: bool, default: true },");
            addedDefines.Add("TINTCOLOR");
            sb.AppendLine("        ADDTIVEFOG: { type: bool, default: true },");
            addedDefines.Add("ADDTIVEFOG");
        }
        else
        {
            // 如果使用了顶点颜色，添加COLOR和ENABLEVERTEXCOLOR宏
            if (parseResult.usesVertexColor)
            {
                sb.AppendLine("        COLOR: { type: bool, default: true },");
                addedDefines.Add("COLOR");
                sb.AppendLine("        ENABLEVERTEXCOLOR: { type: bool, default: true },");
                addedDefines.Add("ENABLEVERTEXCOLOR");
            }
            
            // 如果使用了UV，添加UV宏
            if (parseResult.usesUV)
            {
                sb.AppendLine("        UV: { type: bool, default: true },");
                addedDefines.Add("UV");
            }
        }
        
        // 从shader_feature提取defines
        foreach (var feature in parseResult.shaderFeatures)
        {
            string cleanFeature = NormalizeDefineName(feature);
            
            if (!string.IsNullOrEmpty(cleanFeature) && !addedDefines.Contains(cleanFeature))
            {
                sb.AppendLine($"        {cleanFeature}: {{ type: bool, default: false }},");
                addedDefines.Add(cleanFeature);
            }
        }
        
        // 从multi_compile提取defines
        foreach (var compile in parseResult.multiCompiles)
        {
            string cleanCompile = NormalizeDefineName(compile);
            
            if (!string.IsNullOrEmpty(cleanCompile) && !addedDefines.Contains(cleanCompile))
            {
                sb.AppendLine($"        {cleanCompile}: {{ type: bool, default: false }},");
                addedDefines.Add(cleanCompile);
            }
        }
        
        // 从properties推断defines
        InferDefinesFromProperties(sb, parseResult, addedDefines);

        // 添加常用的defines（可能在代码中使用）
        // 粒子Effect shader常用：LAYERTYPE_ONE/TWO/THREE, WRAPMODE_DEFAULT/CLAMP/REPEAT
        string[] commonDefines = {
            "LAYERTYPE_ONE", "LAYERTYPE_TWO", "LAYERTYPE_THREE",
            "WRAPMODE_DEFAULT", "WRAPMODE_CLAMP", "WRAPMODE_REPEAT", "WRAPMODE_MIRROR",
            "USERIM", "USERIMMAP", "USENPR", "EMISSION", "USELIGHTING",
            "USEVERTEXOFFSET", "USEDISSOLVE", "USEFADEEDGE", "USEDISTORT0",
            "USECUSTOMDATA", "USEGRADIENTMAP0", "USENORMALMAPFORRIM", "USEDISSOLVEDISTORT",
            "ROTATIONTEX", "ROTATIONTEXTWO", "ROTATIONTEXTHREE", "ROTATIONTEXFOUR",
            "USEPOLAR"
        };
        foreach (var def in commonDefines)
        {
            if (!addedDefines.Contains(def))
            {
                // 检查代码中是否使用了这个宏
                bool usedInCode = false;
                if (!string.IsNullOrEmpty(parseResult.vertexCode) && parseResult.vertexCode.Contains(def))
                    usedInCode = true;
                if (!string.IsNullOrEmpty(parseResult.fragmentCode) && parseResult.fragmentCode.Contains(def))
                    usedInCode = true;
                foreach (var func in parseResult.customFunctions)
                {
                    if (func.Contains(def))
                    {
                        usedInCode = true;
                        break;
                    }
                }

                if (usedInCode)
                {
                    sb.AppendLine($"        {def}: {{ type: bool, default: false }},");
                    addedDefines.Add(def);
                }
            }
        }
    }

    /// <summary>
    /// 从properties推断需要的defines
    /// </summary>
    private static void InferDefinesFromProperties(StringBuilder sb, ShaderParseResult parseResult, HashSet<string> addedDefines)
    {
        HashSet<string> inferredDefines = new HashSet<string>();

        foreach (var prop in parseResult.properties)
        {
            string propName = prop.unityName.ToLower();

            // Layer相关（纹理叠加）
            if (propName.Contains("detailtex2"))
            {
                inferredDefines.Add("LAYERTYPE_THREE");
                inferredDefines.Add("LAYERTYPE_TWO");
                inferredDefines.Add("LAYERTYPE_ONE");
            }
            else if (propName.Contains("detailtex"))
            {
                inferredDefines.Add("LAYERTYPE_TWO");
                inferredDefines.Add("LAYERTYPE_ONE");
            }

            // Dissolve相关
            if (propName.Contains("dissolve"))
            {
                inferredDefines.Add("USEDISSOLVE");
                if (propName.Contains("fadeedge"))
                    inferredDefines.Add("USEFADEEDGE");
                if (propName.Contains("dissolvedistort"))
                    inferredDefines.Add("USEDISSOLVEDISTORT");
            }

            // Distort相关（扭曲）
            if (propName.Contains("distorttex") && !propName.Contains("dissolve"))
            {
                inferredDefines.Add("USEDISTORT0");
            }

            // Rim相关（边缘光）
            if (propName.Contains("rim"))
            {
                inferredDefines.Add("USERIM");
                if (propName.Contains("rimmap") && !propName.Contains("mask"))
                    inferredDefines.Add("USERIMMAP");
            }

            // Lighting相关
            if (propName.Contains("effectmainlight") || propName.Contains("lighting"))
            {
                inferredDefines.Add("USELIGHTING");
            }

            // 顶点位移
            if (propName.Contains("vertexoffset") || propName.Contains("vertexamplitude"))
            {
                inferredDefines.Add("USEVERTEXOFFSET");
            }

            // 旋转
            if (propName.Contains("rotateangle"))
            {
                if (propName.Contains("02"))
                    inferredDefines.Add("ROTATIONTEXTWO");
                else if (propName.Contains("03"))
                    inferredDefines.Add("ROTATIONTEXTHREE");
                else if (propName.Contains("04"))
                    inferredDefines.Add("ROTATIONTEXFOUR");
                else if (!propName.Contains("0")) // 基础rotation，排除02/03/04
                    inferredDefines.Add("ROTATIONTEX");
            }

            // Polar Coordinates
            if (propName.Contains("polar"))
            {
                inferredDefines.Add("USEPOLAR");
            }

            // GradientMap
            if (propName.Contains("gradientmap"))
            {
                inferredDefines.Add("USEGRADIENTMAP0");
            }

            // NormalMap for Rim
            if (propName.Contains("normaltexture") || (propName.Contains("normalmap") && propName.Contains("rim")))
            {
                inferredDefines.Add("USENORMALMAPFORRIM");
            }

            // CustomData
            if (propName.Contains("customdata"))
            {
                inferredDefines.Add("USECUSTOMDATA");
            }

            // WrapMode
            if (propName.Contains("wrapmode"))
            {
                inferredDefines.Add("WRAPMODE_CLAMP");
                inferredDefines.Add("WRAPMODE_REPEAT");
            }

            // NPR (Non-Photorealistic Rendering)
            if (propName.Contains("medcolor") || propName.Contains("shadowthreshold"))
            {
                inferredDefines.Add("USENPR");
            }

            // Emission (自发光)
            if (propName.Contains("emission"))
            {
                inferredDefines.Add("EMISSION");
            }
        }

        // 输出推断出的defines
        foreach (var def in inferredDefines.OrderBy(d => d))
        {
            if (!addedDefines.Contains(def))
            {
                sb.AppendLine($"        {def}: {{ type: bool, default: false }},");
                addedDefines.Add(def);
            }
        }
    }

    /// <summary>
    /// 从函数代码中提取函数名
    /// </summary>
    private static string ExtractFunctionName(string funcCode)
    {
        var match = Regex.Match(funcCode, @"\b(\w+)\s*\(");
        if (match.Success)
        {
            string name = match.Groups[1].Value;
            // 跳过类型名
            string[] types = { "float", "vec2", "vec3", "vec4", "mat2", "mat3", "mat4", "int", "bool", "void", "half", "fixed" };
            if (!Array.Exists(types, t => t == name))
            {
                return name;
            }
            // 如果第一个匹配是类型，找下一个
            match = Regex.Match(funcCode, @"\b(?:float|vec2|vec3|vec4|mat2|mat3|mat4|int|bool|void|half|fixed)\s+(\w+)\s*\(");
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
        }
        return null;
    }

    /// <summary>
    /// 标准化宏定义名称
    /// </summary>
    private static string NormalizeDefineName(string feature)
    {
        string cleanFeature = feature.Trim();

        // 跳过空的或者__开头的内部宏
        if (string.IsNullOrEmpty(cleanFeature) || cleanFeature.StartsWith("__"))
            return null;

        // ⭐ 修复：跳过以#开头的非法标识符（如#pragma、#include等）
        if (cleanFeature.StartsWith("#"))
            return null;

        // ⭐ 修复：跳过pragma指令的参数关键词（这些不是defines）
        string[] pragmaKeywords = {
            "pragma", "include", "define", "ifdef", "ifndef", "endif",
            "vertex", "fragment", "geometry", "hull", "domain",
            "target", "only_renderers", "exclude_renderers",
            "multi_compile", "shader_feature", "require",
            "skip_variants", "hardware_tier_variants",
            // 平台名称
            "xbox360", "xboxone", "ps3", "ps4", "ps5", "psp2",
            "n3ds", "wiiu", "switch", "gles", "gles3",
            "metal", "vulkan", "d3d9", "d3d11", "d3d12",
            "glcore", "xboxseries", "ps5", "stadia"
        };
        string lowerFeature = cleanFeature.ToLower();
        foreach (var keyword in pragmaKeywords)
        {
            if (lowerFeature == keyword || lowerFeature.Contains(keyword + "_"))
                return null;
        }

        // 移除前导下划线
        if (cleanFeature.StartsWith("_"))
            cleanFeature = cleanFeature.Substring(1);

        // 处理_ON后缀
        if (cleanFeature.EndsWith("_ON"))
            cleanFeature = cleanFeature.Substring(0, cleanFeature.Length - 3);

        // 跳过常见的Unity内部宏
        string[] skipMacros = {
            "INSTANCING", "PROCEDURAL", "DOTS", "STEREO",
            "FOG_LINEAR", "FOG_EXP", "FOG_EXP2",
            "SHADOWS_SCREEN", "SHADOWS_SOFT", "SHADOWS_HARD"
        };
        foreach (var skip in skipMacros)
        {
            if (cleanFeature.Contains(skip))
                return null;
        }

        return cleanFeature;
    }

    /// <summary>
    /// 预处理：转换所有代码并收集所有varying
    /// 确保VS和FS使用相同的varying声明
    /// </summary>
    private static void PreprocessAndCollectVaryings(ShaderParseResult parseResult)
    {
        // 收集基础varying
        Dictionary<string, string> allVaryings = CollectAllVaryings(parseResult);
        
        // 转换顶点着色器代码并提取varying
        if (!string.IsNullOrEmpty(parseResult.vertexCode))
        {
            string convertedVertCode = ConvertVertexShaderCode(parseResult.vertexCode, parseResult);
            ExtractVaryingsFromCode(convertedVertCode, allVaryings);
        }
        
        // 总是从默认顶点代码中提取varying（因为可能会用到）
        StringBuilder defaultVS = new StringBuilder();
        GenerateDefaultVertexCode(defaultVS, parseResult);
        ExtractVaryingsFromCode(defaultVS.ToString(), allVaryings);
        
        // 转换片元着色器代码并提取varying
        if (!string.IsNullOrEmpty(parseResult.fragmentCode))
        {
            string convertedFragCode = ConvertFragmentShaderCode(parseResult.fragmentCode, parseResult);
            ExtractVaryingsFromCode(convertedFragCode, allVaryings);
        }
        
        // 总是从默认片元代码中提取varying（因为可能会用到）
        StringBuilder defaultFS = new StringBuilder();
        GenerateDefaultFragmentCode(defaultFS, parseResult);
        ExtractVaryingsFromCode(defaultFS.ToString(), allVaryings);
        
        // 转换自定义函数并提取varying
        foreach (var func in parseResult.customFunctions)
        {
            string convertedFunc = ConvertHLSLFunction(func);
            ExtractVaryingsFromCode(convertedFunc, allVaryings);
        }
        
        // 保存收集到的varying
        parseResult.collectedVaryings = allVaryings;
    }

    /// <summary>
    /// 生成转换后的顶点着色器
    /// </summary>
    private static void GenerateConvertedVertexShader(StringBuilder sb, string shaderName, ShaderParseResult parseResult)
    {
        // 转换代码
        string convertedVertCode = "";
        List<string> convertedFunctions = new List<string>();
        
        if (!string.IsNullOrEmpty(parseResult.vertexCode))
        {
            convertedVertCode = ConvertVertexShaderCode(parseResult.vertexCode, parseResult);
        }
        
        // 转换需要的自定义函数
        foreach (var func in parseResult.customFunctions)
        {
            string funcName = ExtractFunctionName(func);
            if (!string.IsNullOrEmpty(funcName) && 
                !string.IsNullOrEmpty(parseResult.vertexCode) && 
                parseResult.vertexCode.Contains(funcName))
            {
                convertedFunctions.Add(ConvertHLSLFunction(func));
            }
        }
        
        // 收集所有varying并保存到parseResult，确保VS和FS使用相同的varying
        Dictionary<string, string> allVaryings = CollectAllVaryings(parseResult);
        
        // 从VS转换后的代码中提取varying
        ExtractVaryingsFromCode(convertedVertCode, allVaryings);
        foreach (var func in convertedFunctions)
        {
            ExtractVaryingsFromCode(func, allVaryings);
        }
        
        // 同时从FS代码中提取varying（确保VS和FS的varying完全一致）
        if (!string.IsNullOrEmpty(parseResult.fragmentCode))
        {
            string convertedFragCode = ConvertFragmentShaderCode(parseResult.fragmentCode, parseResult);
            ExtractVaryingsFromCode(convertedFragCode, allVaryings);
        }
        
        // 从所有自定义函数中提取varying
        foreach (var func in parseResult.customFunctions)
        {
            string convertedFunc = ConvertHLSLFunction(func);
            ExtractVaryingsFromCode(convertedFunc, allVaryings);
        }

        // ⭐ 关键修复：如果是粒子shader，添加粒子专用的varying
        // ParticleShaderTemplate使用的varying: v_Color, v_TextureCoordinate, v_ScreenPos, v_Texcoord0
        // 这些varying不会从Unity代码中提取（因为粒子shader使用ParticleShaderTemplate，不使用Unity转换代码）
        if (parseResult.isParticleBillboard)
        {
            Debug.Log("LayaAir3D: Adding particle-specific varyings");

            // 添加粒子专用的varying（如果还没有）
            if (!allVaryings.ContainsKey("v_Color"))
            {
                allVaryings["v_Color"] = "vec4";
                Debug.Log("LayaAir3D: Added varying vec4 v_Color");
            }

            if (!allVaryings.ContainsKey("v_TextureCoordinate"))
            {
                allVaryings["v_TextureCoordinate"] = "vec2";
                Debug.Log("LayaAir3D: Added varying vec2 v_TextureCoordinate");
            }

            if (!allVaryings.ContainsKey("v_ScreenPos"))
            {
                allVaryings["v_ScreenPos"] = "vec4";
                Debug.Log("LayaAir3D: Added varying vec4 v_ScreenPos");
            }

            // ⭐ 关键修复：粒子mesh模式需要v_MeshColor传递顶点颜色
            if (!allVaryings.ContainsKey("v_MeshColor"))
            {
                allVaryings["v_MeshColor"] = "vec4";
                Debug.Log("LayaAir3D: Added varying vec4 v_MeshColor for particle mesh mode");
            }

            // ⭐ 关键修复：粒子shader需要v_Texcoord0为vec4（用于Scroll功能的.zw分量）
            if (!allVaryings.ContainsKey("v_Texcoord0"))
            {
                allVaryings["v_Texcoord0"] = "vec4";
                Debug.Log("LayaAir3D: Added varying vec4 v_Texcoord0 for particle shader");
            }
            else if (allVaryings["v_Texcoord0"] == "vec2")
            {
                // 如果已经存在但是vec2，强制改为vec4
                allVaryings["v_Texcoord0"] = "vec4";
                Debug.Log("LayaAir3D: Changed v_Texcoord0 from vec2 to vec4 for particle shader");
            }

            // ⭐ 优化：移除粒子shader不需要的varying
            // v_PositionCS和v_PositionWS来自Unity mesh shader，粒子shader中从未使用
            // v_Texcoord2/3/8用于法线贴图（TBN矩阵），仅在USELIGHTING时需要
            // v_Texcoord7用于自定义数据，仅在USECUSTOMDATA时需要
            // 保留这些varying的声明，但它们可能不会被赋值（除非启用相应的特性）

            string[] unusedVaryingsForParticles = { "v_PositionCS", "v_PositionWS" };
            foreach (var unusedVarying in unusedVaryingsForParticles)
            {
                if (allVaryings.ContainsKey(unusedVarying))
                {
                    allVaryings.Remove(unusedVarying);
                    Debug.Log($"LayaAir3D: Removed unused varying for particle shader: {unusedVarying}");
                }
            }
        }

        // 保存到parseResult，供FS使用
        parseResult.collectedVaryings = allVaryings;

        // 开始生成shader
        sb.AppendLine($"#defineGLSL {shaderName}VS");
        sb.AppendLine($"    #define SHADER_NAME {shaderName}");
        sb.AppendLine();
        
        // 根据是否是粒子shader选择不同的includes
        if (parseResult.isParticleBillboard)
        {
            // 粒子shader的VS includes（参考Particle.shader模板）

            // ⭐ 包含particleShuriKenSpriteVS.glsl以获得正确的uniform和宏声明
            // 这个include提供了：
            // 1. gradient函数所需的uniform（Buffer类型等）
            // 2. COLORCOUNT和COLORCOUNT_HALF宏定义
            // 必须在MathGradient.glsl之前include
            sb.AppendLine("    #include \"Camera.glsl\";");
            sb.AppendLine("    #include \"particleShuriKenSpriteVS.glsl\";");
            sb.AppendLine();

            // ⭐ 不需要手动定义COLORCOUNT宏，particleShuriKenSpriteVS.glsl已经定义了

            sb.AppendLine("    #include \"Math.glsl\";");
            sb.AppendLine("    #include \"MathGradient.glsl\";");
            sb.AppendLine("    #include \"Color.glsl\";");
            sb.AppendLine("    #include \"Scene.glsl\";");
            sb.AppendLine("    #include \"SceneFogInput.glsl\";");
        }
        else
        {
            // 标准shader的includes
            sb.AppendLine("    #include \"Math.glsl\";");
            sb.AppendLine("    #include \"Scene.glsl\";");
            sb.AppendLine("    #include \"SceneFogInput.glsl\";");
            sb.AppendLine("    #include \"Camera.glsl\";");
            sb.AppendLine("    #include \"Sprite3DVertex.glsl\";");
            sb.AppendLine("    #include \"VertexCommon.glsl\";");
        }
        sb.AppendLine();
        
        // 生成varying声明并保存，供FS直接复用
        StringBuilder varyingSb = new StringBuilder();
        GenerateVaryingDeclarationsFromDict(varyingSb, allVaryings);
        parseResult.varyingDeclarations = varyingSb.ToString();

        // 粒子shader：顶点颜色varying统一为v_Color（参考i.vcolor->v_Color）
        if (parseResult.isParticleBillboard)
        {
            string decl = parseResult.varyingDeclarations;
            decl = Regex.Replace(decl, @"varying\s+vec4\s+v_VertexColor\s*;", "varying vec4 v_Color;");
            decl = Regex.Replace(decl, @"varying\s+vec4\s+v_Texcoord1\s*;", "varying vec4 v_Color;");
            var lines = decl.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var seen = new HashSet<string>();
            var uniqueLines = new List<string>();
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (!string.IsNullOrEmpty(trimmed) && !seen.Contains(trimmed))
                {
                    seen.Add(trimmed);
                    uniqueLines.Add("    " + trimmed);
                }
            }
            parseResult.varyingDeclarations = string.Join("\n", uniqueLines) + "\n";

            // ⭐ 在去重后，将v_MeshColor用条件编译包裹（参考AI版本shader）
            // 格式：紧凑，没有空行，v_MeshColor在所有varying之后
            if (parseResult.varyingDeclarations.Contains("varying vec4 v_MeshColor"))
            {
                // 移除v_MeshColor所在行（包括前后空行）
                string declWithoutMeshColor = Regex.Replace(parseResult.varyingDeclarations,
                    @"\n?\s*varying\s+vec4\s+v_MeshColor\s*;\n?", "");

                // 在末尾添加条件编译版本（紧凑格式）
                parseResult.varyingDeclarations = declWithoutMeshColor +
                    "\n#ifdef RENDERMODE_MESH\n    varying vec4 v_MeshColor;\n#endif\n";

                Debug.Log("LayaAir3D: Wrapped v_MeshColor with conditional compilation (moved to end)");
            }
        }
        
        Debug.Log($"[VS] allVaryings.Count = {allVaryings.Count}");
        Debug.Log($"[VS] varyingDeclarations length = {parseResult.varyingDeclarations.Length}");
        Debug.Log($"[VS] varyingDeclarations = \n{parseResult.varyingDeclarations}");
        
        sb.Append(parseResult.varyingDeclarations);
        sb.AppendLine();

        // ⭐ 关键修复：粒子函数库必须在main函数之前添加（全局作用域）
        // 在GLSL中，不能在main()函数内部定义其他函数
        if (parseResult.isParticleBillboard)
        {
            // 粒子shader：添加粒子函数库（在main之前）
            try
            {
                Debug.Log("LayaAir3D: Adding particle function library (before main function)");
                sb.Append(ParticleShaderTemplate.GetParticleVertexFunctions());
                sb.AppendLine();
            }
            catch (Exception e)
            {
                Debug.LogError($"LayaAir3D: Failed to add particle functions: {e.Message}");
            }
        }
        else
        {
            // Mesh shader：添加必要的辅助函数（transformUV等）
            GenerateHelperFunctions(sb, parseResult);
        }

        // 添加转换后的自定义函数
        foreach (var func in convertedFunctions)
        {
            sb.AppendLine(IndentCode(func, "    "));
            sb.AppendLine();
        }

        // 生成main函数
        sb.AppendLine("    void main()");
        sb.AppendLine("    {");
        
        // 粒子shader不使用Vertex结构体，直接使用粒子attribute
        if (!parseResult.isParticleBillboard)
        {
            sb.AppendLine("        Vertex vertex;");
            sb.AppendLine("        getVertexParams(vertex);");
            sb.AppendLine();
        }
        
        // ⭐ 关键修复：如果是粒子shader，强制使用ParticleShaderTemplate，不使用转换后的mesh代码
        // 即使Unity源码被成功解析，Unity的Artist_Effect shader使用mesh inputs (POSITION, TEXCOORD0)
        // 但在LayaAir中必须使用粒子Billboard代码（a_DirectionTime, a_ShapePositionStartLifeTime等）
        if (parseResult.isParticleBillboard)
        {
            // 粒子shader：强制使用ParticleShaderTemplate，不使用转换后的Unity代码
            Debug.Log("LayaAir3D: Particle shader detected - using ParticleShaderTemplate (ignoring Unity converted code)");
            GenerateParticleVertexCode(sb, parseResult);
        }
        else if (!string.IsNullOrEmpty(convertedVertCode))
        {
            // Mesh shader：使用转换后的Unity代码
            sb.AppendLine(IndentCode(convertedVertCode, "        "));
        }
        else
        {
            // Mesh shader：没有Unity代码，使用默认mesh代码
            GenerateMeshVertexCode(sb, parseResult);
        }

        sb.AppendLine();

        // ⭐ 优化：粒子shader的ParticleShaderTemplate已经包含remapPositionZ和FogHandle
        // 避免重复添加，只对非粒子shader添加这些调用
        if (!parseResult.isParticleBillboard)
        {
            sb.AppendLine("        gl_Position = remapPositionZ(gl_Position);");
            sb.AppendLine();
            sb.AppendLine("    #ifdef FOG");
            sb.AppendLine("        FogHandle(gl_Position.z);");
            sb.AppendLine("    #endif");
        }

        sb.AppendLine("    }");
        sb.AppendLine("#endGLSL");
        sb.AppendLine();
    }

    /// <summary>
    /// 生成转换后的片元着色器
    /// </summary>
    private static void GenerateConvertedFragmentShader(StringBuilder sb, string shaderName, ShaderParseResult parseResult)
    {
        // 转换代码
        string convertedFragCode = "";
        List<string> convertedFunctions = new List<string>();
        HashSet<string> addedFunctionNames = new HashSet<string>();
        
        if (!string.IsNullOrEmpty(parseResult.fragmentCode))
        {
            convertedFragCode = ConvertFragmentShaderCode(parseResult.fragmentCode, parseResult);
        }
        
        // 转换自定义函数（去重）
        foreach (var func in parseResult.customFunctions)
        {
            string funcName = ExtractFunctionName(func);
            if (!string.IsNullOrEmpty(funcName) && addedFunctionNames.Contains(funcName))
                continue;
            
            string convertedFunc = ConvertHLSLFunction(func);
            convertedFunctions.Add(convertedFunc);
            
            if (!string.IsNullOrEmpty(funcName))
                addedFunctionNames.Add(funcName);
        }
        
        // 开始生成shader
        sb.AppendLine($"#defineGLSL {shaderName}FS");
        sb.AppendLine($"    #define SHADER_NAME {shaderName}");
        sb.AppendLine();
        
        // 根据是否是粒子shader选择不同的includes（参考手动可行的Particle模板）
        if (parseResult.isParticleBillboard)
        {
            // 粒子shader的FS includes
            sb.AppendLine("    #include \"Scene.glsl\";");
            sb.AppendLine("    #include \"SceneFog.glsl\";");
            sb.AppendLine("    #include \"Color.glsl\";");
            sb.AppendLine("    #include \"Camera.glsl\";");
        }
        else
        {
            // 标准shader的FS includes
            sb.AppendLine("    #include \"Color.glsl\";");
            sb.AppendLine("    #include \"Scene.glsl\";");
            sb.AppendLine("    #include \"SceneFog.glsl\";");
            sb.AppendLine("    #include \"Camera.glsl\";");
            sb.AppendLine("    #include \"Sprite3DFrag.glsl\";");
        }
        sb.AppendLine();
        
        // 直接使用VS中保存的varying声明字符串，确保VS和FS完全一致
        Debug.Log($"[FS] parseResult.varyingDeclarations is null? {parseResult.varyingDeclarations == null}");
        Debug.Log($"[FS] parseResult.varyingDeclarations length = {(parseResult.varyingDeclarations?.Length ?? -1)}");
        Debug.Log($"[FS] parseResult.collectedVaryings.Count = {parseResult.collectedVaryings?.Count ?? -1}");
        
        if (!string.IsNullOrEmpty(parseResult.varyingDeclarations))
        {
            Debug.Log("[FS] Using varyingDeclarations from VS");
            sb.Append(parseResult.varyingDeclarations);
        }
        else
        {
            Debug.Log("[FS] varyingDeclarations is empty, using fallback");
            // 如果VS没有保存varying声明，重新生成（兜底逻辑）
            Dictionary<string, string> allVaryings = parseResult.collectedVaryings;
            if (allVaryings == null || allVaryings.Count == 0)
            {
                Debug.Log("[FS] collectedVaryings is empty, re-collecting");
                allVaryings = CollectAllVaryings(parseResult);
                ExtractVaryingsFromCode(convertedFragCode, allVaryings);
                foreach (var func in convertedFunctions)
                {
                    ExtractVaryingsFromCode(func, allVaryings);
                }
            }
            Debug.Log($"[FS] Fallback allVaryings.Count = {allVaryings.Count}");
            GenerateVaryingDeclarationsFromDict(sb, allVaryings);
        }
        sb.AppendLine();
        
        // 添加转换后的自定义函数
        foreach (var func in convertedFunctions)
        {
            sb.AppendLine(IndentCode(func, "    "));
            sb.AppendLine();
        }
        
        // 生成main函数
        sb.AppendLine("    void main()");
        sb.AppendLine("    {");
        
        // 添加转换后的片元着色器代码
        if (!string.IsNullOrEmpty(convertedFragCode))
        {
            sb.AppendLine(IndentCode(convertedFragCode, "        "));
        }
        else
        {
            // 默认片元处理
            GenerateDefaultFragmentCode(sb, parseResult);
        }

        // ⭐ 粒子系统mesh模式：在最终输出前乘以mesh顶点颜色（参考AI版本shader）
        if (parseResult.isParticleBillboard)
        {
            sb.AppendLine();
            sb.AppendLine("    #ifdef RENDERMODE_MESH");
            sb.AppendLine("        // Multiply by mesh vertex color in mesh mode");
            sb.AppendLine("        gl_FragColor *= v_MeshColor;");
            sb.AppendLine("    #endif");
        }

        sb.AppendLine();
        sb.AppendLine("    #ifdef FOG");
        sb.AppendLine("        gl_FragColor.rgb = scenUnlitFog(gl_FragColor.rgb);");
        sb.AppendLine("    #endif");
        sb.AppendLine();
        sb.AppendLine("        gl_FragColor = outputTransform(gl_FragColor);");
        sb.AppendLine("    }");
        sb.AppendLine("#endGLSL");
        sb.AppendLine();
    }

    /// <summary>
    /// 收集所有需要的varying（用于确保VS和FS声明一致）
    /// </summary>
    private static Dictionary<string, string> CollectAllVaryings(ShaderParseResult parseResult)
    {
        Dictionary<string, string> varyings = new Dictionary<string, string>();
        
        // 1. 从解析结果的varyings映射中收集
        foreach (var kvp in parseResult.varyings)
        {
            var info = kvp.Value;
            if (!varyings.ContainsKey(info.glslName))
            {
                varyings[info.glslName] = info.glslType;
            }
        }
        
        // 2. 从顶点着色器代码中提取（o.xxx = ...）
        if (!string.IsNullOrEmpty(parseResult.vertexCode))
        {
            var outputMatches = Regex.Matches(parseResult.vertexCode, @"\bo\.(\w+)\s*=");
            foreach (Match m in outputMatches)
            {
                string varName = m.Groups[1].Value;
                
                // 跳过已知的位置输出
                if (varName == "vertex" || varName == "pos" || varName == "position")
                    continue;
                
                // 检查是否已经在varyings映射中
                string glslName = "v_" + varName;
                foreach (var kvp in parseResult.varyings)
                {
                    if (kvp.Key == varName)
                    {
                        glslName = kvp.Value.glslName;
                        break;
                    }
                }
                
                if (!varyings.ContainsKey(glslName))
                {
                    varyings[glslName] = InferVaryingType(varName, parseResult);
                }
            }
        }
        
        // 3. 从片元着色器代码中提取（i.xxx）
        if (!string.IsNullOrEmpty(parseResult.fragmentCode))
        {
            var inputMatches = Regex.Matches(parseResult.fragmentCode, @"\bi\.(\w+)\b");
            foreach (Match m in inputMatches)
            {
                string varName = m.Groups[1].Value;
                string glslName = "v_" + varName;
                
                foreach (var kvp in parseResult.varyings)
                {
                    if (kvp.Key == varName)
                    {
                        glslName = kvp.Value.glslName;
                        break;
                    }
                }
                
                if (!varyings.ContainsKey(glslName))
                {
                    varyings[glslName] = InferVaryingType(varName, parseResult);
                }
            }
        }
        
        // 4. 从自定义函数中提取
        foreach (var func in parseResult.customFunctions)
        {
            // 检查 i.xxx 模式
            var funcInputMatches = Regex.Matches(func, @"\bi\.(\w+)\b");
            foreach (Match m in funcInputMatches)
            {
                string varName = m.Groups[1].Value;
                string glslName = "v_" + varName;
                
                foreach (var kvp in parseResult.varyings)
                {
                    if (kvp.Key == varName)
                    {
                        glslName = kvp.Value.glslName;
                        break;
                    }
                }
                
                if (!varyings.ContainsKey(glslName))
                {
                    varyings[glslName] = InferVaryingType(varName, parseResult);
                }
            }
            
            // 检查 o.xxx 模式
            var funcOutputMatches = Regex.Matches(func, @"\bo\.(\w+)\s*=");
            foreach (Match m in funcOutputMatches)
            {
                string varName = m.Groups[1].Value;
                if (varName == "vertex" || varName == "pos" || varName == "position")
                    continue;
                    
                string glslName = "v_" + varName;
                foreach (var kvp in parseResult.varyings)
                {
                    if (kvp.Key == varName)
                    {
                        glslName = kvp.Value.glslName;
                        break;
                    }
                }
                
                if (!varyings.ContainsKey(glslName))
                {
                    varyings[glslName] = InferVaryingType(varName, parseResult);
                }
            }
        }
        
        // 5. 添加基本varying（确保始终存在）
        // ⭐ 修复：粒子shader需要v_Texcoord0为vec4（用于.zw存储滚动UV）
        if (!varyings.ContainsKey("v_Texcoord0"))
        {
            // 粒子shader或者使用了Scroll相关的shader都需要vec4
            bool needsVec4 = parseResult.isParticleBillboard ||
                            HasPropertyByName(parseResult, "Scroll") ||
                            parseResult.vertexCode.Contains("u_Scroll0X") ||
                            parseResult.vertexCode.Contains("u_Scroll0Y");
            varyings["v_Texcoord0"] = needsVec4 ? "vec4" : "vec2";
        }
        if (!varyings.ContainsKey("v_VertexColor"))
            varyings["v_VertexColor"] = "vec4";
        if (!varyings.ContainsKey("v_PositionWS"))
            varyings["v_PositionWS"] = "vec3";
        if (!varyings.ContainsKey("v_PositionCS"))
            varyings["v_PositionCS"] = "vec4";
        if (!varyings.ContainsKey("v_ScreenPos"))
            varyings["v_ScreenPos"] = "vec4";

        return varyings;
    }

    /// <summary>
    /// 生成varying声明
    /// </summary>
    private static void GenerateVaryingDeclarations(StringBuilder sb, ShaderParseResult parseResult)
    {
        // 收集所有需要的varying
        Dictionary<string, string> allVaryings = CollectAllVaryings(parseResult);
        
        // 按名称排序以确保VS和FS输出顺序一致
        var sortedVaryings = allVaryings.OrderBy(kvp => kvp.Key).ToList();
        
        foreach (var kvp in sortedVaryings)
        {
            sb.AppendLine($"    varying {kvp.Value} {kvp.Key};");
        }
    }

    /// <summary>
    /// 从字典生成varying声明
    /// </summary>
    private static void GenerateVaryingDeclarationsFromDict(StringBuilder sb, Dictionary<string, string> varyings)
    {
        if (varyings == null || varyings.Count == 0)
            return;

        // ⭐ 按名称排序，但v_MeshColor要放在最后（参考AI辅助转换的shader）
        var sortedVaryings = varyings.OrderBy(kvp => kvp.Key).ToList();

        // 分离v_MeshColor
        string meshColorType = null;
        var normalVaryings = new List<KeyValuePair<string, string>>();

        foreach (var kvp in sortedVaryings)
        {
            if (kvp.Key == "v_MeshColor")
            {
                meshColorType = kvp.Value;
            }
            else
            {
                normalVaryings.Add(kvp);
            }
        }

        // 先输出所有普通varying
        foreach (var kvp in normalVaryings)
        {
            sb.AppendLine($"    varying {kvp.Value} {kvp.Key};");
        }

        // 最后输出v_MeshColor（不带条件编译，后续处理）
        if (meshColorType != null)
        {
            sb.AppendLine($"    varying {meshColorType} v_MeshColor;");
        }
    }

    /// <summary>
    /// 从转换后的GLSL代码中提取varying引用
    /// </summary>
    private static void ExtractVaryingsFromCode(string code, Dictionary<string, string> varyings)
    {
        if (string.IsNullOrEmpty(code))
            return;
        
        // 匹配 v_xxx 模式（varying引用）
        var matches = Regex.Matches(code, @"\bv_(\w+)\b");
        foreach (Match m in matches)
        {
            string glslName = "v_" + m.Groups[1].Value;
            if (!varyings.ContainsKey(glslName))
            {
                // 推断类型
                string varName = m.Groups[1].Value;
                string glslType = InferVaryingTypeFromName(varName);
                varyings[glslName] = glslType;
            }
        }
    }

    /// <summary>
    /// 根据变量名推断varying类型（不依赖parseResult）
    /// </summary>
    private static string InferVaryingTypeFromName(string varName)
    {
        string lowerName = varName.ToLower();
        
        if (lowerName.Contains("uv") || lowerName.Contains("texcoord"))
            return "vec2";
        if (lowerName.Contains("color") || lowerName.Contains("col") || lowerName == "vertexcolor")
            return "vec4";
        if (lowerName.Contains("normal") || lowerName.Contains("tangent") || 
            lowerName.Contains("dir") || lowerName.Contains("view"))
            return "vec3";
        if (lowerName == "positionws" || lowerName == "worldpos")
            return "vec3";
        if (lowerName.Contains("screen") || lowerName.Contains("grab") || 
            lowerName == "positioncs" || lowerName == "screenpos")
            return "vec4";
        
        // 默认vec4
        return "vec4";
    }

    /// <summary>
    /// 推断varying类型
    /// </summary>
    private static string InferVaryingType(string varName, ShaderParseResult parseResult)
    {
        string lowerName = varName.ToLower();
        
        // 根据名称推断类型
        if (lowerName.Contains("uv") || lowerName.Contains("texcoord"))
            return "vec2";
        if (lowerName.Contains("color") || lowerName.Contains("col"))
            return "vec4";
        if (lowerName.Contains("normal") || lowerName.Contains("tangent") || 
            lowerName.Contains("pos") || lowerName.Contains("dir") || lowerName.Contains("view"))
            return "vec3";
        if (lowerName.Contains("screen") || lowerName.Contains("grab"))
            return "vec4";
        
        // 默认vec4
        return "vec4";
    }

    /// <summary>
    /// 生成默认顶点代码
    /// </summary>
    private static void GenerateDefaultVertexCode(StringBuilder sb, ShaderParseResult parseResult)
    {
        if (parseResult.isParticleBillboard)
        {
            // 粒子系统：使用Particle.shader模板的完整代码
            GenerateParticleVertexCode(sb, parseResult);
        }
        else
        {
            // 标准Mesh：生成mesh shader的顶点代码
            GenerateMeshVertexCode(sb, parseResult);
        }
    }

    /// <summary>
    /// 生成辅助函数（transformUV等）
    /// </summary>
    private static void GenerateHelperFunctions(StringBuilder sb, ShaderParseResult parseResult)
    {
        // 检查是否需要transformUV函数（在顶点偏移、UV变换等场景中使用）
        bool needsTransformUV = HasPropertyByName(parseResult, "VertexAmplitude") ||
                                HasPropertyByName(parseResult, "VertexOffset") ||
                                HasPropertyByName(parseResult, "_ST");

        if (needsTransformUV)
        {
            sb.AppendLine("    // UV变换辅助函数");
            sb.AppendLine("    vec2 transformUV(vec2 uv, vec4 tilingOffset) {");
            sb.AppendLine("        return uv * tilingOffset.xy + tilingOffset.zw;");
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        // 检查是否需要UV旋转函数
        bool needsRotateUV = HasPropertyByName(parseResult, "Rotate") ||
                            HasPropertyByName(parseResult, "RotateAngle");

        if (needsRotateUV)
        {
            sb.AppendLine("    // UV旋转辅助函数");
            sb.AppendLine("    vec2 rotateUV(vec2 uv, float centerX, float centerY, float angle) {");
            sb.AppendLine("        float rad = angle * 0.01745329; // degrees to radians");
            sb.AppendLine("        float cosAngle = cos(rad);");
            sb.AppendLine("        float sinAngle = sin(rad);");
            sb.AppendLine("        vec2 center = vec2(centerX, centerY);");
            sb.AppendLine("        vec2 delta = uv - center;");
            sb.AppendLine("        mat2 rotMat = mat2(cosAngle, -sinAngle, sinAngle, cosAngle);");
            sb.AppendLine("        return (rotMat * delta) + center;");
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        // 检查是否需要Polar坐标转换
        bool needsPolar = HasPropertyByName(parseResult, "Polar") ||
                         HasPropertyByName(parseResult, "UsePolar");

        if (needsPolar)
        {
            sb.AppendLine("    // Polar坐标转换函数");
            sb.AppendLine("    vec2 polarCoordinates(vec2 uv, vec2 center, float radialScale, float lengthScale) {");
            sb.AppendLine("        vec2 delta = uv - center;");
            sb.AppendLine("        float radius = length(delta) * 2.0 * radialScale;");
            sb.AppendLine("        float angle = atan(delta.x, delta.y) * (1.0 / 6.28318530718) * lengthScale;");
            sb.AppendLine("        return vec2(radius, angle);");
            sb.AppendLine("    }");
            sb.AppendLine();
        }
    }

    /// <summary>
    /// 生成Mesh shader的顶点代码
    /// </summary>
    private static void GenerateMeshVertexCode(StringBuilder sb, ShaderParseResult parseResult)
    {
        // ⭐ 简化版：只生成基本的mesh变换代码
        // 根据实际的varying来决定需要生成哪些代码

        sb.AppendLine("        vec3 positionOS = vertex.positionOS;");
        sb.AppendLine();

        // 顶点偏移处理（如果有）
        if (HasPropertyByName(parseResult, "VertexOffset") || HasPropertyByName(parseResult, "VertexAmplitude"))
        {
            sb.AppendLine("    #ifdef USEVERTEXOFFSET");
            sb.AppendLine("        // 顶点偏移效果");
            sb.AppendLine("        vec4 vertexAmplitudeTex = texture2D(u_VertexAmplitudeTex, ");
            sb.AppendLine("            transformUV(vertex.texCoord0, u_VertexAmplitudeTex_ST) + ");
            sb.AppendLine("            fract(vec2(u_VertexAmplitudeTexScroll0X, u_VertexAmplitudeTexScroll0Y) * u_Time));");
            sb.AppendLine("        vec4 vertexAmplitudeMaskTex = texture2D(u_VertexAmplitudeMaskTex, ");
            sb.AppendLine("            transformUV(vertex.texCoord0, u_VertexAmplitudeMaskTex_ST));");
            sb.AppendLine();
            sb.AppendLine("        if (u_VertexOffsetMode == 1.0) {");
            sb.AppendLine("            // axis mode - 沿任意方向偏移");
            sb.AppendLine("            positionOS += u_VertexAmplitude * (2.0 * vertexAmplitudeTex.rgb - 1.0) * vertexAmplitudeMaskTex.r;");
            sb.AppendLine("        } else {");
            sb.AppendLine("            // normal mode - 沿法线方向偏移");
            sb.AppendLine("            positionOS += vertex.normalOS * u_VertexAmplitude * vertexAmplitudeTex.r * vertexAmplitudeMaskTex.r;");
            sb.AppendLine("        }");
            sb.AppendLine("    #endif");
            sb.AppendLine();
        }

        // 标准mesh变换
        sb.AppendLine("        mat4 worldMat = getWorldMatrix();");
        sb.AppendLine("        vec4 positionWS = worldMat * vec4(positionOS, 1.0);");

        // 只在有v_PositionWS varying时才生成赋值
        if (parseResult.collectedVaryings != null && parseResult.collectedVaryings.ContainsKey("v_PositionWS"))
        {
            sb.AppendLine("        v_PositionWS = positionWS.xyz / positionWS.w;");
            sb.AppendLine();
            sb.AppendLine("        gl_Position = getPositionCS(v_PositionWS);");
        }
        else
        {
            sb.AppendLine("        gl_Position = getPositionCS(positionWS.xyz / positionWS.w);");
        }

        // 只在有v_PositionCS varying时才生成赋值
        if (parseResult.collectedVaryings != null && parseResult.collectedVaryings.ContainsKey("v_PositionCS"))
        {
            sb.AppendLine("        v_PositionCS = gl_Position;");
        }

        // 只在有v_ScreenPos varying时才生成赋值
        if (parseResult.collectedVaryings != null && parseResult.collectedVaryings.ContainsKey("v_ScreenPos"))
        {
            sb.AppendLine("        v_ScreenPos = gl_Position * 0.5 + vec4(0.5 * gl_Position.w);");
        }
        sb.AppendLine();

        // UV坐标 - 根据v_Texcoord0的类型决定如何赋值
        if (parseResult.collectedVaryings != null && parseResult.collectedVaryings.ContainsKey("v_Texcoord0"))
        {
            string texcoordType = parseResult.collectedVaryings["v_Texcoord0"];
            if (texcoordType == "vec4")
            {
                // vec4类型，可以同时存储两套UV
                sb.AppendLine("        // UV坐标");
                sb.AppendLine("        v_Texcoord0.xy = vertex.texCoord0;");
                sb.AppendLine("        v_Texcoord0.zw = vec2(0.0); // 预留");
            }
            else if (texcoordType == "vec2")
            {
                // vec2类型，只存储一套UV
                sb.AppendLine("        // UV坐标");
                sb.AppendLine("        v_Texcoord0 = vertex.texCoord0;");
            }
            sb.AppendLine();
        }

        // 顶点颜色 - 使用正确的varying名称
        if (parseResult.collectedVaryings != null)
        {
            if (parseResult.collectedVaryings.ContainsKey("v_Color"))
            {
                sb.AppendLine("        // 顶点颜色");
                sb.AppendLine("        v_Color = vertex.vertexColor;");
                sb.AppendLine();
            }
            else if (parseResult.collectedVaryings.ContainsKey("v_VertexColor"))
            {
                sb.AppendLine("        // 顶点颜色");
                sb.AppendLine("        v_VertexColor = vertex.vertexColor;");
                sb.AppendLine();
            }
        }

        // 法线和切线（用于Rim和光照）- 只在需要时生成
        if (HasPropertyByName(parseResult, "Rim") || HasPropertyByName(parseResult, "Lighting") ||
            HasPropertyByName(parseResult, "NormalMap"))
        {
            sb.AppendLine("        // 法线变换");
            sb.AppendLine("        mat3 normalMat = transpose(inverse(mat3(worldMat)));");
            sb.AppendLine();

            // 只在对应的varying存在时才生成代码
            bool hasTexcoord3 = parseResult.collectedVaryings != null && parseResult.collectedVaryings.ContainsKey("v_Texcoord3");
            bool hasTexcoord2 = parseResult.collectedVaryings != null && parseResult.collectedVaryings.ContainsKey("v_Texcoord2");
            bool hasTexcoord8 = parseResult.collectedVaryings != null && parseResult.collectedVaryings.ContainsKey("v_Texcoord8");

            if (hasTexcoord3 || hasTexcoord2 || hasTexcoord8)
            {
                sb.AppendLine("    #ifdef USENORMALMAPFORRIM");
                sb.AppendLine("        vec3 normalWorld = normalize(normalMat * vertex.normalOS);");
                sb.AppendLine("        vec3 tangentWorld = normalize((worldMat * vec4(vertex.tangentOS.xyz, 0.0)).xyz);");
                sb.AppendLine("        float tangentSign = vertex.tangentOS.w;");
                sb.AppendLine("        vec3 binormalWorld = cross(normalWorld, tangentWorld) * tangentSign;");
                if (hasTexcoord3) sb.AppendLine("        v_Texcoord3 = normalWorld;");
                if (hasTexcoord8) sb.AppendLine("        v_Texcoord8 = tangentWorld;");
                sb.AppendLine("    #endif");
                sb.AppendLine();

                if (hasTexcoord2)
                {
                    sb.AppendLine("    #ifdef USERIM");
                    sb.AppendLine("        #ifdef USENORMALMAPFORRIM");
                    sb.AppendLine("            v_Texcoord2 = binormalWorld;");
                    sb.AppendLine("        #else");
                    sb.AppendLine("            // 视空间法线（用于Rim效果）");
                    sb.AppendLine("            mat3 viewIT = transpose(inverse(mat3(u_View * worldMat)));");
                    sb.AppendLine("            v_Texcoord2 = normalize(viewIT * vertex.normalOS);");
                    sb.AppendLine("        #endif");
                    sb.AppendLine("    #endif");
                    sb.AppendLine();
                }

                if (hasTexcoord3)
                {
                    sb.AppendLine("    #ifdef USELIGHTING");
                    sb.AppendLine("        v_Texcoord3 = normalize(normalMat * vertex.normalOS);");
                    sb.AppendLine("    #endif");
                    sb.AppendLine();
                }
            }
        }

        // UV滚动（多层纹理）- 只在对应varying存在时生成
        bool hasTexcoord4 = parseResult.collectedVaryings != null && parseResult.collectedVaryings.ContainsKey("v_Texcoord4");
        if (hasTexcoord4)
        {
            sb.AppendLine("    #if defined(LAYERTYPE_TWO) || defined(LAYERTYPE_THREE)");
            sb.AppendLine("        v_Texcoord4.xy = fract(vec2(u_Scroll1X, u_Scroll1Y) * u_Time);");
            sb.AppendLine("    #endif");
            sb.AppendLine();
            sb.AppendLine("    #ifdef LAYERTYPE_THREE");
            sb.AppendLine("        v_Texcoord4.zw = fract(vec2(u_Scroll2X, u_Scroll2Y) * u_Time);");
            sb.AppendLine("    #endif");
            sb.AppendLine();
        }

        // Distort UV滚动 - 只在对应varying存在时生成
        bool hasTexcoord6 = parseResult.collectedVaryings != null && parseResult.collectedVaryings.ContainsKey("v_Texcoord6");
        if (hasTexcoord6)
        {
            sb.AppendLine("    #ifdef USEDISTORT0");
            sb.AppendLine("        v_Texcoord6.xy = fract(vec2(u_Distort0X, u_Distort0Y) * u_Time);");
            sb.AppendLine("    #endif");
            sb.AppendLine();
            sb.AppendLine("    #ifdef USEDISSOLVEDISTORT");
            sb.AppendLine("        v_Texcoord6.zw = fract(vec2(u_DissolveDistortX, u_DissolveDistortY) * u_Time);");
            sb.AppendLine("    #endif");
            sb.AppendLine();
        }

        // UV旋转预计算 - 只在对应varying存在时生成
        bool hasTexcoord9 = parseResult.collectedVaryings != null && parseResult.collectedVaryings.ContainsKey("v_Texcoord9");
        if (hasTexcoord9 && (HasPropertyByName(parseResult, "Rotation") || HasPropertyByName(parseResult, "RotateAngle")))
        {
            sb.AppendLine("    #ifdef ROTATIONTEX");
            sb.AppendLine("        float rad1 = u_RotateAngle * 0.01745329;");
            sb.AppendLine("        v_Texcoord9.x = cos(rad1);");
            sb.AppendLine("        v_Texcoord9.y = sin(rad1);");
            sb.AppendLine("    #endif");
            sb.AppendLine();
            sb.AppendLine("    #ifdef ROTATIONTEXTWO");
            sb.AppendLine("        float rad2 = u_RotateAngle02 * 0.01745329;");
            sb.AppendLine("        v_Texcoord9.z = cos(rad2);");
            sb.AppendLine("        v_Texcoord9.w = sin(rad2);");
            sb.AppendLine("    #endif");
            sb.AppendLine();
        }

        // ⭐ 移除CustomData处理 - 这需要a_Texcoord1属性，但简单shader没有
        // 只有明确需要CustomData且attributeMap中有a_Texcoord1时才处理
        // if (HasPropertyByName(parseResult, "CustomData"))
        // {
        //     sb.AppendLine("    #ifdef USECUSTOMDATA");
        //     sb.AppendLine("        v_Texcoord7.xy = vertex.texCoord0.zw;  // CustomData from UV0.zw");
        //     sb.AppendLine("        v_Texcoord7.zw = vertex.texCoord1;     // CustomData from UV1.xy");
        //     sb.AppendLine("    #endif");
        //     sb.AppendLine();
        // }
    }

    /// <summary>
    /// 生成粒子系统顶点代码（使用Particle.shader模板）
    /// 注意：只生成main函数的body，不生成函数定义（函数定义已在main之前添加）
    /// </summary>
    private static void GenerateParticleVertexCode(StringBuilder sb, ShaderParseResult parseResult)
    {
        // ⭐ 使用ParticleShaderTemplate（最可靠的方法）
        // 注意：函数库已经在main函数之前添加过了，这里只添加main函数body
        try
        {
            Debug.Log("LayaAir3D: Generating particle main function body from ParticleShaderTemplate");

            // ⭐ 关键：不再添加函数库（已在main之前添加）
            // sb.Append(ParticleShaderTemplate.GetParticleVertexFunctions());  // ← 注释掉

            // 获取完整的main函数
            string mainFunc = ParticleShaderTemplate.GetParticleVertexMainFunction();

            // 移除main函数的开头声明（因为外层已经有了void main() {）
            if (mainFunc.Contains("void main()"))
            {
                int bodyStart = mainFunc.IndexOf("{");
                if (bodyStart != -1)
                {
                    // 提取main函数体内容（不包括void main()和最外层的{}）
                    int bodyEnd = mainFunc.LastIndexOf("}");
                    if (bodyEnd > bodyStart)
                    {
                        mainFunc = mainFunc.Substring(bodyStart + 1, bodyEnd - bodyStart - 1);
                    }
                }
            }

            // 添加main函数body（已经正确缩进）
            sb.AppendLine(mainFunc);

            // 添加特效相关的varying赋值
            AddEffectVaryingAssignments(sb, parseResult);

            Debug.Log("LayaAir3D: Successfully generated particle main body using ParticleShaderTemplate");
            return;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"LayaAir3D: Failed to use ParticleShaderTemplate: {e.Message}");
            Debug.LogWarning($"LayaAir3D: Stack trace: {e.StackTrace}");
            Debug.LogWarning("LayaAir3D: Falling back to template file extraction");
        }

        // Fallback 1: 尝试从Particle.shader模板文件读取
        string[] possiblePaths = new string[]
        {
            Path.Combine(Application.dataPath, "LayaAir3.0UnityPlugin/template/Particle.shader"),
            Path.Combine(Application.dataPath.Replace("/Assets", ""), "Assets/LayaAir3.0UnityPlugin/template/Particle.shader"),
            Path.Combine(Directory.GetCurrentDirectory(), "template/Particle.shader"),
            Path.Combine(Directory.GetCurrentDirectory(), "Assets/LayaAir3.0UnityPlugin/template/Particle.shader")
        };

        string particleTemplatePath = null;
        string particleVSCode = null;

        // 尝试所有可能的路径
        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                particleTemplatePath = path;
                Debug.Log($"LayaAir3D: Found Particle.shader template file at: {path}");
                break;
            }
        }

        if (particleTemplatePath != null)
        {
            try
            {
                string particleTemplate = File.ReadAllText(particleTemplatePath);
                // 提取main函数的内容（粒子系统的核心计算代码）
                particleVSCode = ExtractParticleMainFunction(particleTemplate);

                if (!string.IsNullOrEmpty(particleVSCode))
                {
                    Debug.Log("LayaAir3D: Successfully extracted particle VS code from template file");
                    sb.AppendLine(particleVSCode);

                    // 添加特效相关的varying赋值
                    AddEffectVaryingAssignments(sb, parseResult);
                    return;
                }
                else
                {
                    Debug.LogWarning("LayaAir3D: Failed to extract particle main function from template file");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"LayaAir3D: Failed to read Particle.shader template: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning("LayaAir3D: Particle.shader template file not found in any expected location");
            Debug.LogWarning("LayaAir3D: Searched paths:");
            foreach (var path in possiblePaths)
            {
                Debug.LogWarning($"  - {path}");
            }
        }

        // Fallback 2: 使用内置的粒子系统代码（简化版但已验证正确）
        Debug.LogWarning("LayaAir3D: Using built-in particle code as final fallback");
        GenerateBuiltInParticleVertexCode(sb, parseResult);
    }

    /// <summary>
    /// 从Particle.shader提取main函数内容（增强版）
    /// </summary>
    private static string ExtractParticleMainFunction(string particleTemplate)
    {
        try
        {
            // 查找"void main()"到"}"的内容（在ParticleVS中）
            int vsStart = particleTemplate.IndexOf("#defineGLSL ParticleVS");
            if (vsStart == -1)
            {
                // 尝试其他可能的标记
                vsStart = particleTemplate.IndexOf("#defineGLSL");
                if (vsStart == -1)
                {
                    Debug.LogWarning("LayaAir3D: Cannot find #defineGLSL in Particle.shader template");
                    return null;
                }
            }

            // 查找void main()
            int mainStart = particleTemplate.IndexOf("void main()", vsStart);
            if (mainStart == -1)
            {
                Debug.LogWarning("LayaAir3D: Cannot find void main() in Particle.shader template");
                return null;
            }

            // 查找main函数的开始花括号
            int mainBodyStart = particleTemplate.IndexOf("{", mainStart);
            if (mainBodyStart == -1)
            {
                Debug.LogWarning("LayaAir3D: Cannot find opening brace for main() function");
                return null;
            }

            // 计数花括号来找到匹配的结束花括号
            int braceCount = 0;
            int pos = mainBodyStart;
            bool inString = false;
            bool inComment = false;
            bool inLineComment = false;
            char prevChar = '\0';

            while (pos < particleTemplate.Length)
            {
                char c = particleTemplate[pos];

                // 处理字符串内的字符（忽略引号内的花括号）
                if (c == '"' && prevChar != '\\')
                {
                    inString = !inString;
                }
                // 处理注释
                else if (!inString && c == '/' && pos + 1 < particleTemplate.Length)
                {
                    if (particleTemplate[pos + 1] == '/')
                    {
                        inLineComment = true;
                        pos++;
                    }
                    else if (particleTemplate[pos + 1] == '*')
                    {
                        inComment = true;
                        pos++;
                    }
                }
                else if (inLineComment && c == '\n')
                {
                    inLineComment = false;
                }
                else if (inComment && c == '*' && pos + 1 < particleTemplate.Length && particleTemplate[pos + 1] == '/')
                {
                    inComment = false;
                    pos++;
                }
                // 只在非字符串、非注释中计数花括号
                else if (!inString && !inComment && !inLineComment)
                {
                    if (c == '{') braceCount++;
                    if (c == '}')
                    {
                        braceCount--;
                        if (braceCount == 0)
                        {
                            // 提取main函数体（不包括最外层的{}）
                            string mainBody = particleTemplate.Substring(mainBodyStart + 1, pos - mainBodyStart - 1);

                            // 验证提取的代码
                            if (mainBody.Trim().Length < 10)
                            {
                                Debug.LogWarning("LayaAir3D: Extracted main function body is too short, might be invalid");
                                return null;
                            }

                            Debug.Log($"LayaAir3D: Successfully extracted {mainBody.Length} characters from particle main function");
                            return mainBody;
                        }
                    }
                }

                prevChar = c;
                pos++;
            }

            Debug.LogWarning("LayaAir3D: Cannot find closing brace for main() function (unbalanced braces)");
            return null;
        }
        catch (Exception e)
        {
            Debug.LogError($"LayaAir3D: Exception while extracting particle main function: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// 添加特效相关的varying赋值（在粒子计算之后）
    /// </summary>
    private static void AddEffectVaryingAssignments(StringBuilder sb, ShaderParseResult parseResult)
    {
        // 这些赋值应该在粒子计算完成后、gl_Position=remapPositionZ之前执行

        sb.AppendLine();
        sb.AppendLine("        // 特效相关的varying赋值");

        // ⭐ 通用修复：确保v_Texcoord0总是被赋值（很多FS会使用它）
        // 如果有Scroll属性，后面会覆盖；如果没有，至少有一个默认值
        if (parseResult.collectedVaryings != null &&
            parseResult.collectedVaryings.ContainsKey("v_Texcoord0"))
        {
            string texcoordType = parseResult.collectedVaryings["v_Texcoord0"];
            if (texcoordType == "vec4")
            {
                sb.AppendLine("        v_Texcoord0.xy = v_TextureCoordinate;");
                sb.AppendLine("        v_Texcoord0.zw = gl_Position.xy; // 默认屏幕坐标");
            }
            else if (texcoordType == "vec2")
            {
                sb.AppendLine("        v_Texcoord0 = v_TextureCoordinate;");
            }
        }

        // 屏幕坐标（如果需要）
        if (HasPropertyByName(parseResult, "Distort") || HasPropertyByName(parseResult, "Screen") ||
            parseResult.collectedVaryings.ContainsKey("v_ScreenPos"))
        {
            sb.AppendLine("        v_ScreenPos.xy = (gl_Position.xy + gl_Position.w) * 0.5;");
            sb.AppendLine("        v_ScreenPos.zw = gl_Position.zw;");
        }

        // ⭐ 修复：移除v_Texcoord5赋值
        // 原代码：sb.AppendLine("        v_Texcoord5 = gl_Position;");
        // 问题：v_Texcoord5没有在varying中声明，且FS中未使用
        // 屏幕坐标已经通过v_ScreenPos传递，不需要额外的v_Texcoord5

        // UV滚动
        if (HasPropertyByName(parseResult, "Scroll"))
        {
            sb.AppendLine("        v_Texcoord0.xy = v_TextureCoordinate;");
            sb.AppendLine("        v_Texcoord0.zw = fract(vec2(u_Scroll0X, u_Scroll0Y) * u_Time);");

            if (HasPropertyByName(parseResult, "Scroll1") || HasPropertyByName(parseResult, "DetailTex"))
            {
                sb.AppendLine("    #if defined(LAYERTYPE_TWO) || defined(LAYERTYPE_THREE)");
                sb.AppendLine("        v_Texcoord4.xy = fract(vec2(u_Scroll1X, u_Scroll1Y) * u_Time);");
                sb.AppendLine("    #endif");
            }

            if (HasPropertyByName(parseResult, "Scroll2") || HasPropertyByName(parseResult, "DetailTex2"))
            {
                sb.AppendLine("    #ifdef LAYERTYPE_THREE");
                sb.AppendLine("        v_Texcoord4.zw = fract(vec2(u_Scroll2X, u_Scroll2Y) * u_Time);");
                sb.AppendLine("    #endif");
            }

            if (HasPropertyByName(parseResult, "Distort0"))
            {
                sb.AppendLine("    #ifdef USEDISTORT0");
                sb.AppendLine("        v_Texcoord6.xy = fract(vec2(u_Distort0X, u_Distort0Y) * u_Time);");
                sb.AppendLine("    #endif");
            }

            if (HasPropertyByName(parseResult, "DissolveDistort"))
            {
                sb.AppendLine("    #ifdef USEDISSOLVEDISTORT");
                sb.AppendLine("        v_Texcoord6.zw = fract(vec2(u_DissolveDistortX, u_DissolveDistortY) * u_Time);");
                sb.AppendLine("    #endif");
            }
        }

        // 旋转预计算
        if (HasPropertyByName(parseResult, "RotateAngle"))
        {
            sb.AppendLine("    #ifdef ROTATIONTEX");
            sb.AppendLine("        float rad1 = u_RotateAngle * 0.01745329;");
            sb.AppendLine("        v_Texcoord9.x = cos(rad1);");
            sb.AppendLine("        v_Texcoord9.y = sin(rad1);");
            sb.AppendLine("    #endif");

            if (HasPropertyByName(parseResult, "RotateAngle02"))
            {
                sb.AppendLine("    #ifdef ROTATIONTEXTWO");
                sb.AppendLine("        float rad2 = u_RotateAngle02 * 0.01745329;");
                sb.AppendLine("        v_Texcoord9.z = cos(rad2);");
                sb.AppendLine("        v_Texcoord9.w = sin(rad2);");
                sb.AppendLine("    #endif");
            }
        }
    }

    /// <summary>
    /// 生成内置的粒子系统顶点代码（作为后备方案）
    /// </summary>
    private static void GenerateBuiltInParticleVertexCode(StringBuilder sb, ShaderParseResult parseResult)
    {
        // 这是一个简化的粒子系统实现，当Particle.shader模板不可用时使用
        sb.AppendLine("        // 简化的粒子系统实现（后备方案）");
        sb.AppendLine("        float age = u_Time - a_DirectionTime.w;");
        sb.AppendLine("        float normalizedAge = age / a_ShapePositionStartLifeTime.w;");
        sb.AppendLine();
        sb.AppendLine("        if (normalizedAge < 1.0) {");
        sb.AppendLine("            vec3 startVelocity = a_DirectionTime.xyz * a_StartSpeed;");
        sb.AppendLine("            vec3 gravityVelocity = u_Gravity * age;");
        sb.AppendLine();
        sb.AppendLine("            vec4 worldRotation;");
        sb.AppendLine("            if (u_SimulationSpace == 0)");
        sb.AppendLine("                worldRotation = a_SimulationWorldRotation;");
        sb.AppendLine("            else");
        sb.AppendLine("                worldRotation = u_WorldRotation;");
        sb.AppendLine();
        sb.AppendLine("            // 简化的位置计算");
        sb.AppendLine("            vec3 center = a_ShapePositionStartLifeTime.xyz + startVelocity * age;");
        sb.AppendLine("            center += 0.5 * gravityVelocity * age;");
        sb.AppendLine();
        sb.AppendLine("            if (u_SimulationSpace == 0)");
        sb.AppendLine("                center = center + a_SimulationWorldPostion;");
        sb.AppendLine("            else if (u_SimulationSpace == 1)");
        sb.AppendLine("                center = center + u_WorldPosition;");
        sb.AppendLine();
        sb.AppendLine("            // Billboard");
        sb.AppendLine("            vec2 corner = a_CornerTextureCoordinate.xy;");
        sb.AppendLine("            vec3 cameraUpVector = normalize(u_CameraUp);");
        sb.AppendLine("            vec3 sideVector = normalize(cross(u_CameraDirection, cameraUpVector));");
        sb.AppendLine("            vec3 upVector = normalize(cross(sideVector, u_CameraDirection));");
        sb.AppendLine();
        sb.AppendLine("            vec2 size = a_StartSize.xy;");
        sb.AppendLine("            corner *= size;");
        sb.AppendLine("            center += u_SizeScale.xzy * (corner.x * sideVector + corner.y * upVector);");
        sb.AppendLine();
        sb.AppendLine("            gl_Position = u_Projection * u_View * vec4(center, 1.0);");
        sb.AppendLine();
        sb.AppendLine("            // 颜色");
        sb.AppendLine("            vec4 startcolor = gammaToLinear(a_StartColor);");
        sb.AppendLine("            v_Color = startcolor;");
        sb.AppendLine();
        sb.AppendLine("            // UV");
        sb.AppendLine("            vec2 simulateUV = a_SimulationUV.xy + a_CornerTextureCoordinate.zw * a_SimulationUV.zw;");
        sb.AppendLine("            v_TextureCoordinate = simulateUV;");
        sb.AppendLine("        } else {");
        sb.AppendLine("            gl_Position = vec4(2.0, 2.0, 2.0, 1.0);");
        sb.AppendLine("        }");

        // 添加特效varying赋值
        AddEffectVaryingAssignments(sb, parseResult);
    }

    /// <summary>
    /// 检查是否有指定名称的属性
    /// </summary>
    private static bool HasPropertyByName(ShaderParseResult parseResult, string namePattern)
    {
        foreach (var prop in parseResult.properties)
        {
            if (prop.unityName.Contains(namePattern) || prop.layaName.Contains(namePattern))
                return true;
        }
        return false;
    }

    /// <summary>
    /// 生成默认片元代码
    /// </summary>
    private static void GenerateDefaultFragmentCode(StringBuilder sb, ShaderParseResult parseResult)
    {
        sb.AppendLine("        vec4 color = vec4(1.0);");
        sb.AppendLine();
        sb.AppendLine("    #ifdef UV");
        sb.AppendLine("        vec2 uv = v_Texcoord0;");
        sb.AppendLine("    #else");
        sb.AppendLine("        vec2 uv = vec2(0.0);");
        sb.AppendLine("    #endif");
        sb.AppendLine();
        sb.AppendLine("    #ifdef MAINTEX");
        sb.AppendLine("        color = texture2D(u_MainTex, uv);");
        sb.AppendLine("    #endif");
        sb.AppendLine();
        sb.AppendLine("    #ifdef COLOR");
        sb.AppendLine("        #ifdef ENABLEVERTEXCOLOR");
        sb.AppendLine("        color *= v_VertexColor;");
        sb.AppendLine("        #endif");
        sb.AppendLine("    #endif");
        sb.AppendLine();
        sb.AppendLine("        gl_FragColor = color;");
    }

    /// <summary>
    /// 转换HLSL类型到GLSL类型
    /// </summary>
    private static string ConvertTypeToGLSL(string hlslType)
    {
        switch (hlslType.ToLower())
        {
            case "float": return "float";
            case "float2": case "half2": case "fixed2": return "vec2";
            case "float3": case "half3": case "fixed3": return "vec3";
            case "float4": case "half4": case "fixed4": return "vec4";
            case "half": case "fixed": return "float";
            case "float4x4": case "half4x4": return "mat4";
            case "float3x3": case "half3x3": return "mat3";
            case "float2x2": case "half2x2": return "mat2";
            case "sampler2d": return "sampler2D";
            case "samplercube": return "samplerCube";
            case "int": return "int";
            case "bool": return "bool";
            default: return "vec4";
        }
    }

    /// <summary>
    /// 初始化映射引擎（混合架构）
    /// </summary>
    private static void InitializeMappingEngine()
    {
        if (mappingEngineInitialized)
            return;

        mappingEngineInitialized = true;

        // 检查用户自定义映射表
        string projectMappingPath = Path.Combine(Directory.GetCurrentDirectory(), "ProjectSettings/LayaShaderMappings.json");
        bool hasUserMappings = File.Exists(projectMappingPath);

        if (hasUserMappings)
        {
            Debug.Log("==================== Shader Mapping Engine ====================");
            Debug.Log("LayaAir3D: Custom shader mappings detected");
            Debug.Log("LayaAir3D: Initializing mapping table mode...");

            // 初始化映射引擎
            mappingEngine = new ShaderMappingEngine();

            // 加载内置映射表
            string builtinMappingPath = Path.Combine(Application.dataPath, "LayaAir3.0UnityPlugin/Editor/Mappings/builtin_unity_to_laya.json");
            if (File.Exists(builtinMappingPath))
            {
                if (mappingEngine.LoadMappings(builtinMappingPath))
                {
                    Debug.Log("LayaAir3D: ✓ Loaded builtin mappings");
                }
            }
            else
            {
                Debug.LogWarning($"LayaAir3D: Builtin mapping file not found: {builtinMappingPath}");
            }

            // 加载用户映射表（可覆盖内置规则）
            if (mappingEngine.LoadMappings(projectMappingPath))
            {
                Debug.Log($"LayaAir3D: ✓ Loaded custom mappings from: {projectMappingPath}");
                useMappingTableMode = true;
            }

            if (useMappingTableMode)
            {
                Debug.Log("LayaAir3D: Mapping table mode ENABLED");
                Debug.Log("LayaAir3D: Priority: Custom rules → Builtin rules → Built-in code fallback");
            }
            else
            {
                Debug.LogWarning("LayaAir3D: Failed to load mappings, falling back to built-in mode");
            }

            Debug.Log("===============================================================");
        }
        else
        {
            Debug.Log("LayaAir3D: No custom mappings found, using built-in conversion mode");
            Debug.Log($"LayaAir3D: To enable mapping table mode, create: {projectMappingPath}");
            useMappingTableMode = false;
        }
    }

    /// <summary>
    /// 检查是否需要回退到内置规则
    /// </summary>
    private static bool CheckNeedsFallback(string code)
    {
        // 检查常见的Unity特有标识符
        string[] unityIdentifiers = new string[]
        {
            "UnityObjectToClipPos",
            "UnityObjectToWorldNormal",
            "UnityWorldToObjectDir",
            "UNITY_MATRIX_MVP",
            "UNITY_MATRIX_MV",
            "UNITY_MATRIX_V",
            "UNITY_MATRIX_P",
            "UNITY_MATRIX_IT_MV",
            "TRANSFER_SHADOW",
            "SHADOW_COORDS",
            "SHADOW_ATTENUATION"
        };

        foreach (var identifier in unityIdentifiers)
        {
            if (code.Contains(identifier))
                return true;
        }

        return false;
    }

    /// <summary>
    /// 应用内置转换规则作为回退
    /// </summary>
    private static string ApplyBuiltInConversionAsFallback(string code)
    {
        // Unity矩阵转换
        code = Regex.Replace(code, @"\bUNITY_MATRIX_MVP\b", "(u_Projection * u_View * u_World)");
        code = Regex.Replace(code, @"\bUNITY_MATRIX_MV\b", "(u_View * u_World)");
        code = Regex.Replace(code, @"\bUNITY_MATRIX_V\b", "u_View");
        code = Regex.Replace(code, @"\bUNITY_MATRIX_P\b", "u_Projection");
        code = Regex.Replace(code, @"\bUNITY_MATRIX_IT_MV\b", "transpose(inverse(u_View * u_World))");

        // UnityObjectToClipPos - 直接展开为矩阵乘法，避免函数调用的类型不匹配问题
        code = Regex.Replace(code, @"UnityObjectToClipPos\s*\(\s*([^)]+)\s*\)",
            m => {
                string arg = m.Groups[1].Value.Trim();
                // 如果参数已经是vec4，直接使用；否则转换为vec4
                if (arg.StartsWith("vec4(") || arg.Contains(".xyzw"))
                    return $"(u_ViewProjection * u_WorldMat * {arg})";
                else
                    return $"(u_ViewProjection * u_WorldMat * vec4({arg}, 1.0))";
            });

        // UnityObjectToWorldNormal
        code = Regex.Replace(code, @"UnityObjectToWorldNormal\s*\(\s*([^)]+)\s*\)",
            m => $"normalize((transpose(inverse(getWorldMatrix())) * vec4({m.Groups[1].Value}, 0.0)).xyz)");

        // Unity阴影相关（简化处理）
        code = Regex.Replace(code, @"TRANSFER_SHADOW\s*\([^)]*\)\s*;?", "");
        code = Regex.Replace(code, @"SHADOW_COORDS\s*\([^)]*\)", "");
        code = Regex.Replace(code, @"SHADOW_ATTENUATION\s*\([^)]*\)", "1.0");

        return code;
    }

    /// <summary>
    /// 转换HLSL函数到GLSL
    /// </summary>
    private static string ConvertHLSLFunction(string hlslFunc)
    {
        string code = ConvertHLSLToGLSL(hlslFunc);
        return code;
    }

    /// <summary>
    /// 转换顶点着色器代码
    /// </summary>
    private static string ConvertVertexShaderCode(string hlslCode, ShaderParseResult parseResult)
    {
        string code = ConvertHLSLToGLSL(hlslCode);
        
        // 替换输出变量 o.xxx
        code = Regex.Replace(code, @"\bo\.vertex\s*=", "gl_Position =");
        code = Regex.Replace(code, @"\bo\.pos\s*=", "gl_Position =");
        
        // 替换varying输出（o.xxx -> v_xxx，使用映射表）
        foreach (var kvp in parseResult.varyings)
        {
            string pattern = $@"\bo\.{Regex.Escape(kvp.Key)}\b";
            code = Regex.Replace(code, pattern, kvp.Value.glslName);
        }
        // 通用替换
        code = Regex.Replace(code, @"\bo\.(\w+)", "v_$1");
        
        // 替换输入变量 v.xxx（根据appdata结构体名称）
        string inputVar = "v";
        // 先处理特殊情况：v.vertex.xyz 直接转换为 vertex.positionOS（避免多余的vec4包装）
        code = Regex.Replace(code, $@"\b{inputVar}\.vertex\.xyz\b", "vertex.positionOS");
        code = Regex.Replace(code, $@"\b{inputVar}\.vertex\.xy\b", "vertex.positionOS.xy");
        code = Regex.Replace(code, $@"\b{inputVar}\.vertex\.xz\b", "vertex.positionOS.xz");
        // 然后处理普通情况：v.vertex 转换为 vec4(vertex.positionOS, 1.0)
        code = Regex.Replace(code, $@"\b{inputVar}\.vertex\b", "vec4(vertex.positionOS, 1.0)");
        code = Regex.Replace(code, $@"\b{inputVar}\.normal\b", "vertex.normalOS");
        code = Regex.Replace(code, $@"\b{inputVar}\.tangent\b", "vertex.tangentOS");
        code = Regex.Replace(code, $@"\b{inputVar}\.uv0\b", "vertex.texCoord0");
        code = Regex.Replace(code, $@"\b{inputVar}\.uv1\b", "vertex.texCoord1");
        code = Regex.Replace(code, $@"\b{inputVar}\.uv\b", "vertex.texCoord0");
        code = Regex.Replace(code, $@"\b{inputVar}\.texcoord0?\b", "vertex.texCoord0");
        code = Regex.Replace(code, $@"\b{inputVar}\.texcoord1\b", "vertex.texCoord1");
        code = Regex.Replace(code, $@"\b{inputVar}\.color\b", "vertex.vertexColor");
        code = Regex.Replace(code, $@"\b{inputVar}\.vcolor\b", "vertex.vertexColor");
        
        // 粒子Billboard模式：特殊变量转换
        if (parseResult.isParticleBillboard)
        {
            // ========== 粒子系统没有Vertex结构体，所有vertex.xxx都需要替换 ==========
            
            // 位置相关
            code = Regex.Replace(code, @"\bvertex\.positionOS\b", "vec3(0.0)"); // 粒子位置由粒子系统计算
            code = Regex.Replace(code, @"\bvertex\.position\b", "vec3(0.0)");
            
            // 法线相关
            code = Regex.Replace(code, @"\bvertex\.normalOS\b", "vec3(0.0, 0.0, 1.0)"); // 粒子默认法线朝向相机
            code = Regex.Replace(code, @"\bvertex\.normal\b", "vec3(0.0, 0.0, 1.0)");
            
            // 切线相关
            code = Regex.Replace(code, @"\bvertex\.tangentOS\b", "vec4(1.0, 0.0, 0.0, 1.0)");
            code = Regex.Replace(code, @"\bvertex\.tangent\b", "vec4(1.0, 0.0, 0.0, 1.0)");
            
            // 顶点颜色 -> 粒子起始颜色
            code = Regex.Replace(code, @"\bvertex\.vertexColor\b", "a_StartColor");
            code = Regex.Replace(code, @"\bvertex\.color\b", "a_StartColor");
            
            // UV坐标 -> a_CornerTextureCoordinate.zw（粒子UV）
            // texCoord0
            code = Regex.Replace(code, @"\bvertex\.texCoord0\.xy\b", "a_CornerTextureCoordinate.zw");
            code = Regex.Replace(code, @"\bvertex\.texCoord0\.x\b", "a_CornerTextureCoordinate.z");
            code = Regex.Replace(code, @"\bvertex\.texCoord0\.y\b", "a_CornerTextureCoordinate.w");
            code = Regex.Replace(code, @"\bvertex\.texCoord0\b(?!\.)", "a_CornerTextureCoordinate.zw");
            code = Regex.Replace(code, @"\bvertex\.texCoord0\.xyzw\b", "vec4(a_CornerTextureCoordinate.zw, 0.0, 0.0)");
            code = Regex.Replace(code, @"\bvertex\.texCoord0\.zw\b", "vec2(0.0, 0.0)");
            
            // texCoord1 -> 粒子CustomData使用a_SimulationUV.zw（参考手动可行版本）
            code = Regex.Replace(code, @"\bvertex\.texCoord1\.xy\b", "a_SimulationUV.zw");
            code = Regex.Replace(code, @"\bvertex\.texCoord1\.zw\b", "a_SimulationUV.zw");
            code = Regex.Replace(code, @"\bvertex\.texCoord1\b(?!\.)", "a_SimulationUV.zw");
            
            // uv, uv0, uv1 等简写形式
            code = Regex.Replace(code, @"\bvertex\.uv0\b", "a_CornerTextureCoordinate.zw");
            code = Regex.Replace(code, @"\bvertex\.uv1\b", "a_CornerTextureCoordinate.zw");
            code = Regex.Replace(code, @"\bvertex\.uv\b", "a_CornerTextureCoordinate.zw");
            
            // 捕获所有剩余的 vertex.xxx 并替换为合理的默认值
            // 这是一个兜底处理，避免遗漏
            code = Regex.Replace(code, @"\bvertex\.(\w+)\b", match =>
            {
                string member = match.Groups[1].Value.ToLower();
                switch (member)
                {
                    case "positionos":
                    case "position":
                        return "vec3(0.0)";
                    case "normalos":
                    case "normal":
                        return "vec3(0.0, 0.0, 1.0)";
                    case "tangentos":
                    case "tangent":
                        return "vec4(1.0, 0.0, 0.0, 1.0)";
                    case "vertexcolor":
                    case "color":
                        return "a_StartColor";
                    case "texcoord0":
                    case "texcoord1":
                    case "texcoord":
                    case "uv":
                    case "uv0":
                    case "uv1":
                        return "a_CornerTextureCoordinate.zw";
                    default:
                        // 未知的vertex成员，返回零向量
                        return "vec4(0.0)";
                }
            });
            
            // ========== 粒子系统Uniform替换 ==========
            
            // u_WorldMat -> 粒子系统没有这个，粒子位置已经是世界空间
            code = Regex.Replace(code, @"\bu_WorldMat\s*\*\s*vec4\s*\(\s*([^,]+)\s*,\s*1\.0\s*\)", "vec4($1, 1.0)");
            code = Regex.Replace(code, @"\bu_WorldMat\b", "mat4(1.0)"); // 单位矩阵作为fallback
            
            // getWorldMatrix() -> 粒子系统没有这个函数
            code = Regex.Replace(code, @"\bgetWorldMatrix\s*\(\s*\)", "mat4(1.0)");
            
            // getPositionCS -> 粒子系统使用 u_Projection * u_View
            code = Regex.Replace(code, @"\bgetPositionCS\s*\(\s*([^)]+)\s*\)", m => {
                string arg = m.Groups[1].Value.Trim();
                // 检查参数是否已经是vec4
                if (arg.StartsWith("vec4(") || arg.Contains(".xyzw") || arg == "v_PositionWS_vec4")
                    return $"(u_Projection * u_View * {arg})";
                else
                    return $"(u_Projection * u_View * vec4({arg}, 1.0))";
            });
            
            // initPixelParams, getVertexParams 等函数在粒子中不存在
            code = Regex.Replace(code, @"\bgetVertexParams\s*\(\s*\w+\s*\)\s*;?", "// getVertexParams not available in particle shader");
            code = Regex.Replace(code, @"\binitPixelParams\s*\(\s*\w+\s*,\s*\w+\s*\)\s*;?", "// initPixelParams not available in particle shader");
            
            // ========== Unity粒子变量到LayaAir粒子变量映射 ==========

            // ⭐ 修复：_Time -> u_Time (LayaAir引擎内置时间变量，粒子系统也使用u_Time)
            // Unity _Time: float4 (t/20, t, t*2, t*3)
            // LayaAir u_Time: float (场景运行时间)

            // ⭐ 修复1：先处理vec4赋值的情况（类型转换）
            code = Regex.Replace(code,
                @"(float4|vec4)\s+(\w+)\s*=\s*_Time\s*\+\s*_TimeEditor\b",
                "$1 $2 = vec4(u_Time * 0.05, u_Time, u_Time * 2.0, u_Time * 3.0)");
            code = Regex.Replace(code,
                @"(float4|vec4)\s+(\w+)\s*=\s*_TimeEditor\s*\+\s*_Time\b",
                "$1 $2 = vec4(u_Time * 0.05, u_Time, u_Time * 2.0, u_Time * 3.0)");
            code = Regex.Replace(code,
                @"(float4|vec4)\s+(\w+)\s*=\s*_Time\b",
                "$1 $2 = vec4(u_Time * 0.05, u_Time, u_Time * 2.0, u_Time * 3.0)");

            // ⭐ 修复2：处理分量访问
            code = Regex.Replace(code, @"\b_Time\.y\b", "u_Time");
            code = Regex.Replace(code, @"\b_Time\.x\b", "(u_Time * 0.05)");
            code = Regex.Replace(code, @"\b_Time\.z\b", "(u_Time * 2.0)");
            code = Regex.Replace(code, @"\b_Time\.w\b", "(u_Time * 3.0)");
            code = Regex.Replace(code, @"\b_Time\.g\b", "u_Time");
            code = Regex.Replace(code, @"\b_Time\.r\b", "(u_Time * 0.05)");
            code = Regex.Replace(code, @"\b_Time\.b\b", "(u_Time * 2.0)");
            code = Regex.Replace(code, @"\b_Time\.a\b", "(u_Time * 3.0)");

            // ⭐ 修复3：其他情况的_Time转换为u_Time
            code = Regex.Replace(code, @"\b_Time\b", "u_Time");

            // ⭐ 修复4：_TimeEditor是Unity编辑器专用变量，运行时不存在
            code = Regex.Replace(code, @"\bu_Time\s*\+\s*_TimeEditor\b", "u_Time /* editor-only var removed */");
            code = Regex.Replace(code, @"\b_TimeEditor\s*\+\s*u_Time\b", "u_Time /* editor-only var removed */");
            code = Regex.Replace(code, @"\b_TimeEditor\b", "0.0 /* editor-only */");

            // Unity矩阵 -> 粒子系统矩阵（粒子已经在世界空间）
            code = Regex.Replace(code, @"\bUNITY_MATRIX_MVP\b", "(u_Projection * u_View)");
            code = Regex.Replace(code, @"\bUNITY_MATRIX_VP\b", "(u_Projection * u_View)");
            code = Regex.Replace(code, @"\bUNITY_MATRIX_V\b", "u_View");
            code = Regex.Replace(code, @"\bUNITY_MATRIX_P\b", "u_Projection");
            code = Regex.Replace(code, @"\bu_ViewProjection\b", "(u_Projection * u_View)");
            
            // 相机位置
            code = Regex.Replace(code, @"\b_WorldSpaceCameraPos\b", "u_CameraPos");
            
            // ========== PixelParams/Pixel结构体也不存在 ==========
            code = Regex.Replace(code, @"\bpixel\.positionWS\b", "v_PositionWS");
            code = Regex.Replace(code, @"\bpixel\.normalWS\b", "vec3(0.0, 0.0, 1.0)");
            code = Regex.Replace(code, @"\bpixel\.tangentWS\b", "vec3(1.0, 0.0, 0.0)");
            code = Regex.Replace(code, @"\bpixel\.bitangentWS\b", "vec3(0.0, 1.0, 0.0)");
            code = Regex.Replace(code, @"\bpixel\.viewDirWS\b", "normalize(u_CameraPos - v_PositionWS)");
            code = Regex.Replace(code, @"\bpixel\.(\w+)\b", "vec4(0.0)"); // 其他pixel成员
            
            // ========== Vertex/PixelParams声明也要移除 ==========
            code = Regex.Replace(code, @"Vertex\s+vertex\s*;\s*\n?", "");
            code = Regex.Replace(code, @"PixelParams\s+pixel\s*;\s*\n?", "");
            
            // Varying映射：粒子顶点颜色用v_Color（参考i.vcolor->v_Color）
            code = Regex.Replace(code, @"\bv_VertexColor\b", "v_Color");
            code = Regex.Replace(code, @"(\bv_Texcoord1\s*)=(\s*a_StartColor\b)", "v_Color =$2");  // VS输出
        }
        
        // Unity特有函数 - 使用平衡括号
        code = ConvertUnityVertexFunctions(code);
        
        // TRANSFORM_TEX - 使用平衡括号
        code = ConvertTransformTex(code);
        
        // UNITY_TRANSFER_FOG - 移除
        code = Regex.Replace(code, @"UNITY_TRANSFER_FOG\s*\([^)]*\)\s*;?", "");
        
        // 移除return o;
        code = Regex.Replace(code, @"\breturn\s+o\s*;", "");
        
        // 移除v2f o; 声明（已经在main中处理）
        code = Regex.Replace(code, @"(v2f|VertexOutput|Varyings)\s+o\s*;\s*\n?", "");
        
        // 移除无效的类型声明（如 vec2 o; 等转换后的残留）
        code = Regex.Replace(code, @"(vec2|vec3|vec4|float|int)\s+o\s*;\s*\n?", "");
        
        // 修复屏幕坐标引用（v_vertex -> gl_Position 或 v_PositionCS）
        code = Regex.Replace(code, @"\bv_vertex\b", "gl_Position");
        code = Regex.Replace(code, @"\bv_pos\b(?!\w)", "gl_Position");
        
        // 粒子shader后处理：替换ConvertUnityVertexFunctions生成的u_WorldMat
        if (parseResult.isParticleBillboard)
        {
            // ConvertUnityVertexFunctions会生成 u_ViewProjection * u_WorldMat，需要替换
            // 粒子系统中位置已经是世界空间，不需要u_WorldMat
            code = Regex.Replace(code, @"\bu_ViewProjection\s*\*\s*u_WorldMat\b", "(u_Projection * u_View)");
            code = Regex.Replace(code, @"\bu_WorldMat\b", "mat4(1.0)");
            
            // 替换normalize((u_WorldMat * vec4(xxx, 0.0)).xyz) -> normalize(xxx)
            code = Regex.Replace(code, @"normalize\s*\(\s*\(\s*mat4\s*\(\s*1\.0\s*\)\s*\*\s*vec4\s*\(\s*([^,]+)\s*,\s*0\.0\s*\)\s*\)\.xyz\s*\)", "normalize($1)");
            
            // 替换(mat4(1.0) * vec4(xxx, 1.0)) -> vec4(xxx, 1.0)
            code = Regex.Replace(code, @"\(\s*mat4\s*\(\s*1\.0\s*\)\s*\*\s*(vec4\s*\([^)]+\))\s*\)", "$1");
        }
        
        // 确保屏幕坐标varying被设置
        if (!code.Contains("v_ScreenPos =") && !code.Contains("v_ScreenPos="))
        {
            // 在gl_Position赋值后添加屏幕坐标计算
            code = Regex.Replace(code, 
                @"(gl_Position\s*=\s*[^;]+;)", 
                "$1\n        v_ScreenPos = gl_Position * 0.5 + vec4(0.5 * gl_Position.w);");
        }
        
        // 确保v_PositionCS被设置
        if (!code.Contains("v_PositionCS =") && !code.Contains("v_PositionCS="))
        {
            code = Regex.Replace(code, 
                @"(gl_Position\s*=\s*[^;]+;)", 
                "$1\n        v_PositionCS = gl_Position;");
        }
        
        return code;
    }

    /// <summary>
    /// 转换Unity顶点着色器特有函数
    /// </summary>
    private static string ConvertUnityVertexFunctions(string code)
    {
        // UnityObjectToClipPos
        code = ConvertFunctionWithBalancedParens(code, "UnityObjectToClipPos", (args) =>
        {
            string arg = args.Trim();
            // 确保参数是vec4类型
            if (arg.StartsWith("vec4(") || arg.Contains(".xyzw"))
                return $"(u_ViewProjection * u_WorldMat * {arg})";
            else if (arg.EndsWith(".xyz") || arg.EndsWith(".rgb"))
                return $"(u_ViewProjection * u_WorldMat * vec4({arg}, 1.0))";
            else
                return $"(u_ViewProjection * u_WorldMat * vec4({arg}, 1.0))";
        });
        
        // UnityObjectToWorldNormal
        code = ConvertFunctionWithBalancedParens(code, "UnityObjectToWorldNormal", (args) =>
        {
            return $"normalize((u_WorldMat * vec4({args}, 0.0)).xyz)";
        });
        
        // UnityObjectToWorldDir
        code = ConvertFunctionWithBalancedParens(code, "UnityObjectToWorldDir", (args) =>
        {
            return $"normalize((u_WorldMat * vec4({args}, 0.0)).xyz)";
        });
        
        // UnityWorldToObjectDir
        code = ConvertFunctionWithBalancedParens(code, "UnityWorldToObjectDir", (args) =>
        {
            return $"normalize((inverse(u_WorldMat) * vec4({args}, 0.0)).xyz)";
        });
        
        // ComputeScreenPos
        code = ConvertFunctionWithBalancedParens(code, "ComputeScreenPos", (args) =>
        {
            return $"({args} * 0.5 + 0.5)";
        });
        
        // ComputeGrabScreenPos
        code = ConvertFunctionWithBalancedParens(code, "ComputeGrabScreenPos", (args) =>
        {
            return $"({args} * 0.5 + 0.5)";
        });
        
        // SafeNormalize
        code = ConvertFunctionWithBalancedParens(code, "SafeNormalize", (args) =>
        {
            return $"normalize({args} + vec3(0.0001))";
        });
        
        return code;
    }

    /// <summary>
    /// 转换TRANSFORM_TEX宏
    /// </summary>
    private static string ConvertTransformTex(string code)
    {
        return ConvertFunctionWithBalancedParens(code, "TRANSFORM_TEX", (args) =>
        {
            var parts = SplitFunctionArgs(args, 2);
            if (parts.Count >= 2)
            {
                string uv = parts[0].Trim();
                string texName = parts[1].Trim();

                // ⭐ 修复：移除可能的前缀（_或u_）
                // Unity: TRANSFORM_TEX(uv, _distort_tex)
                // 在ConvertHLSLToGLSL之后可能已被转换为: TRANSFORM_TEX(uv, u_distort_tex)
                if (texName.StartsWith("_"))
                    texName = texName.Substring(1);
                else if (texName.StartsWith("u_"))
                    texName = texName.Substring(2);

                // 特殊纹理名映射（参考Unity2Laya_ShaderMapping.md：主纹理用u_TilingOffset）
                string stName;
                if (texName == "MainTex" || texName == "BaseMap" || texName == "AlbedoTexture")
                    stName = "u_TilingOffset";  // 粒子/Effect主纹理统一用u_TilingOffset
                else if (texName == "BumpMap" || texName == "NormalMap" || texName == "NormalTexture")
                    stName = "u_NormalTexture_ST";
                else if (texName == "FadeEdgeTexture")
                    stName = "u_FadeEdgeTexture_ST";
                else if (texName == "DissolveDistortTex")
                    stName = "u_DissolveDistortTex_ST";
                else if (texName == "GradientMapTex0" || texName == "GradientMapTex0Map")
                    stName = "u_GradientMapTex0_ST";
                else
                    stName = $"u_{texName}_ST";

                // 如果uv包含运算符，需要用括号包围以保证正确的运算优先级
                // 例如: TRANSFORM_TEX(a + b, tex) -> ((a + b) * tex_ST.xy + tex_ST.zw)
                string uvExpr = uv;
                if (uv.Contains("+") || uv.Contains("-") || uv.Contains("*") || uv.Contains("/"))
                {
                    // 检查是否已经被括号包围
                    if (!uv.TrimStart().StartsWith("(") || !uv.TrimEnd().EndsWith(")"))
                        uvExpr = $"({uv})";
                }

                return $"({uvExpr} * {stName}.xy + {stName}.zw)";
            }
            return args;
        });
    }

    /// <summary>
    /// 转换片元着色器代码
    /// </summary>
    private static string ConvertFragmentShaderCode(string hlslCode, ShaderParseResult parseResult)
    {
        string code = ConvertHLSLToGLSL(hlslCode);
        
        // 替换输入变量（i.xxx -> v_xxx，使用映射表）
        foreach (var kvp in parseResult.varyings)
        {
            string pattern = $@"\bi\.{Regex.Escape(kvp.Key)}\b";
            code = Regex.Replace(code, pattern, kvp.Value.glslName);
        }
        // 通用替换
        code = Regex.Replace(code, @"\bi\.(\w+)", "v_$1");
        
        // 修复屏幕坐标引用
        code = Regex.Replace(code, @"\bv_vertex\b", "v_ScreenPos");
        code = Regex.Replace(code, @"\bv_screenPos\b", "v_ScreenPos");
        code = Regex.Replace(code, @"\bv_grabPos\b", "v_ScreenPos");
        
        // ========== 粒子shader特殊处理 ==========
        if (parseResult.isParticleBillboard)
        {
            // 粒子shader中没有pixel/PixelParams结构体
            // pixel.xxx -> 对应的varying或默认值
            code = Regex.Replace(code, @"\bpixel\.positionWS\b", "v_PositionWS");
            code = Regex.Replace(code, @"\bpixel\.normalWS\b", "vec3(0.0, 0.0, 1.0)");
            code = Regex.Replace(code, @"\bpixel\.tangentWS\b", "vec3(1.0, 0.0, 0.0)");
            code = Regex.Replace(code, @"\bpixel\.bitangentWS\b", "vec3(0.0, 1.0, 0.0)");
            code = Regex.Replace(code, @"\bpixel\.viewDirWS\b", "normalize(u_CameraPos - v_PositionWS)");
            code = Regex.Replace(code, @"\bpixel\.uv0\b", "v_TextureCoordinate");
            code = Regex.Replace(code, @"\bpixel\.uv\b", "v_TextureCoordinate");
            code = Regex.Replace(code, @"\bpixel\.color\b", "v_Color");
            code = Regex.Replace(code, @"\bpixel\.vertexColor\b", "v_Color");
            
            // 捕获所有剩余的 pixel.xxx
            code = Regex.Replace(code, @"\bpixel\.(\w+)\b", match =>
            {
                string member = match.Groups[1].Value.ToLower();
                switch (member)
                {
                    case "positionws":
                        return "v_PositionWS";
                    case "normalws":
                        return "vec3(0.0, 0.0, 1.0)";
                    case "tangentws":
                        return "vec3(1.0, 0.0, 0.0)";
                    case "bitangentws":
                        return "vec3(0.0, 1.0, 0.0)";
                    case "viewdirws":
                        return "normalize(u_CameraPos - v_PositionWS)";
                    case "uv0":
                    case "uv":
                    case "uv1":
                        return "v_TextureCoordinate";
                    case "color":
                    case "vertexcolor":
                        return "v_Color";
                    default:
                        return "vec4(0.0)";
                }
            });
            
            // 移除PixelParams声明
            code = Regex.Replace(code, @"PixelParams\s+pixel\s*;\s*\n?", "");
            code = Regex.Replace(code, @"\binitPixelParams\s*\([^)]*\)\s*;?", "");
            
            // ⭐ 修复：粒子shader中的时间变量统一使用u_Time（引擎内置变量）
            // Unity _Time: float4 (t/20, t, t*2, t*3)
            // LayaAir u_Time: float (场景运行时间)

            // ⭐ 修复1：先处理vec4赋值的情况（类型转换）
            code = Regex.Replace(code,
                @"(float4|vec4)\s+(\w+)\s*=\s*_Time\s*\+\s*u_TimeEditor\b",
                "$1 $2 = vec4(u_Time * 0.05, u_Time, u_Time * 2.0, u_Time * 3.0)");
            code = Regex.Replace(code,
                @"(float4|vec4)\s+(\w+)\s*=\s*u_TimeEditor\s*\+\s*_Time\b",
                "$1 $2 = vec4(u_Time * 0.05, u_Time, u_Time * 2.0, u_Time * 3.0)");
            code = Regex.Replace(code,
                @"(float4|vec4)\s+(\w+)\s*=\s*_Time\b",
                "$1 $2 = vec4(u_Time * 0.05, u_Time, u_Time * 2.0, u_Time * 3.0)");
            code = Regex.Replace(code,
                @"(float4|vec4)\s+(\w+)\s*=\s*u_Time\s*\+\s*u_TimeEditor\b",
                "$1 $2 = vec4(u_Time * 0.05, u_Time, u_Time * 2.0, u_Time * 3.0)");
            code = Regex.Replace(code,
                @"(float4|vec4)\s+(\w+)\s*=\s*u_TimeEditor\s*\+\s*u_Time\b",
                "$1 $2 = vec4(u_Time * 0.05, u_Time, u_Time * 2.0, u_Time * 3.0)");

            // ⭐ 修复2：处理分量访问
            code = Regex.Replace(code, @"\b_Time\.y\b", "u_Time");
            code = Regex.Replace(code, @"\b_Time\.x\b", "(u_Time * 0.05)");
            code = Regex.Replace(code, @"\b_Time\.z\b", "(u_Time * 2.0)");
            code = Regex.Replace(code, @"\b_Time\.w\b", "(u_Time * 3.0)");
            code = Regex.Replace(code, @"\b_Time\.g\b", "u_Time");
            code = Regex.Replace(code, @"\b_Time\.r\b", "(u_Time * 0.05)");
            code = Regex.Replace(code, @"\b_Time\.b\b", "(u_Time * 2.0)");
            code = Regex.Replace(code, @"\b_Time\.a\b", "(u_Time * 3.0)");

            // ⭐ 修复3：其他情况的_Time转换为u_Time
            code = Regex.Replace(code, @"\b_Time\b", "u_Time");

            // ⭐ 修复4：_TimeEditor是Unity编辑器专用变量，运行时不存在（FS）
            code = Regex.Replace(code, @"\bu_Time\s*\+\s*u_TimeEditor\b", "u_Time /* editor-only var removed */");
            code = Regex.Replace(code, @"\bu_TimeEditor\s*\+\s*u_Time\b", "u_Time /* editor-only var removed */");
            code = Regex.Replace(code, @"\bu_TimeEditor\b", "0.0 /* editor-only */");

            // 相机位置
            code = Regex.Replace(code, @"\b_WorldSpaceCameraPos\b", "u_CameraPos");
            
            // 主纹理_ST：粒子主纹理统一用u_TilingOffset（参考Unity2Laya_ShaderMapping.md）
            code = Regex.Replace(code, @"\bu_MainTex_ST\b", "u_TilingOffset");
            code = Regex.Replace(code, @"\bu_AlbedoTexture_ST\b", "u_TilingOffset");
            
            // 法线贴图_ST：Unity可能用u_NormalMap_ST，Laya粒子模板用u_NormalTexture_ST
            code = Regex.Replace(code, @"\bu_NormalMap_ST\b", "u_NormalTexture_ST");
            
            // Varying映射：粒子颜色用v_Color（参考：i.vcolor->v_Color）
            code = Regex.Replace(code, @"\bv_VertexColor\b", "v_Color");

            // ⭐ 修复：Uniform颜色映射（与uniformMap中的定义保持一致）
            // 粒子shader uniformMap中定义的是u_AlbedoColor，但代码中可能使用u_Color
            code = Regex.Replace(code, @"\bu_Color\b", "u_AlbedoColor");
            code = Regex.Replace(code, @"\bv_Texcoord1\b", "v_Color");  // 当v_Texcoord1用于顶点颜色时
            
            // 修复语法错误：mat3(transpose); (inverse(u_View)) 等
            code = Regex.Replace(code, @"\(\s*mat3\s*\(\s*transpose\s*\)\s*;\s*\(\s*inverse\s*\(\s*u_View\s*\)\s*\)", "mat3(inverse(transpose(u_View)))");
            code = Regex.Replace(code, @"\(\s*mat3\s*\(\s*transpose\s*\)\s*;\s*\(\s*inverse\s*\(\s*u_View\s*\*\s*mat4\s*\(\s*1\.0\s*\)\s*\)\s*\)", "mat3(inverse(transpose(u_View)))");
        }
        
        // 替换return为gl_FragColor赋值
        code = Regex.Replace(code, @"\breturn\s+([^;]+)\s*;", "gl_FragColor = $1;");
        
        // UNITY_APPLY_FOG - 移除（在main函数末尾统一处理）
        code = Regex.Replace(code, @"UNITY_APPLY_FOG[_\w]*\s*\([^)]*\)\s*;?", "");
        
        // 移除facing参数相关
        code = Regex.Replace(code, @",?\s*float\s+facing\s*:\s*VFACE", "");
        code = Regex.Replace(code, @"\bfacing\b", "1.0");

        // ⭐ v_Texcoord0的处理已经在ApplySystematicTypeFixes的阶段6中智能处理
        // 不在这里强制替换，避免错误地把vec4变成vec2

        // 最后一次修复.rgb和.a运算中的vec4变量
        code = Regex.Replace(code,
            @"(\.(?:rgb|a)\s*[\*\+\-/]?=\s*)([^;]+);",
            match => {
                string prefix = match.Groups[1].Value;
                string expr = match.Groups[2].Value;
                bool isRgb = prefix.Contains("rgb");

                // 根据是.rgb还是.a来添加相应的分量访问
                if (isRgb)
                {
                    expr = Regex.Replace(expr, @"\bv_Color(?!\.)", "v_Color.rgb");
                    expr = Regex.Replace(expr, @"\bu_TintColor(?!\.)", "u_TintColor.rgb");
                    expr = Regex.Replace(expr, @"\bu_AlbedoColor(?!\.)", "u_AlbedoColor.rgb");
                    expr = Regex.Replace(expr, @"\bu_BaseColor(?!\.)", "u_BaseColor.rgb");
                }
                else
                {
                    expr = Regex.Replace(expr, @"\bv_Color(?!\.)", "v_Color.a");
                    expr = Regex.Replace(expr, @"\bu_TintColor(?!\.)", "u_TintColor.a");
                    expr = Regex.Replace(expr, @"\bu_AlbedoColor(?!\.)", "u_AlbedoColor.a");
                    expr = Regex.Replace(expr, @"\bu_BaseColor(?!\.)", "u_BaseColor.a");
                }

                // 清理重复的分量访问
                expr = Regex.Replace(expr, @"\.rgb\.rgb", ".rgb");
                expr = Regex.Replace(expr, @"\.a\.a", ".a");

                return $"{prefix}{expr};";
            });

        return code;
    }

    /// <summary>
    /// HLSL到GLSL的通用代码转换（混合架构）
    /// </summary>
    private static string ConvertHLSLToGLSL(string hlslCode)
    {
        // 初始化映射引擎
        InitializeMappingEngine();

        string code = hlslCode;

        // ==================== 混合架构：映射表优先模式 ====================
        if (useMappingTableMode && mappingEngine != null && mappingEngine.HasRules())
        {
            conversionTimer.Restart();

            // 优先使用映射表转换
            code = mappingEngine.ApplyMappings(code, "all");

            mappingTableConversionTime = conversionTimer.ElapsedMilliseconds;

            Debug.Log($"LayaAir3D: Mapping table applied - {mappingEngine.GetStatistics()}");

            // 检查是否还有未转换的Unity特有标识符
            bool needsFallback = CheckNeedsFallback(code);

            if (needsFallback)
            {
                Debug.Log("LayaAir3D: Some Unity-specific code not covered by mapping table, applying built-in fallback...");

                conversionTimer.Restart();

                // 使用内置规则作为回退
                code = ApplyBuiltInConversionAsFallback(code);

                builtInConversionTime = conversionTimer.ElapsedMilliseconds;
            }

            return code;
        }

        // ==================== 内置模式：硬编码规则 ====================

        conversionTimer.Restart();

        // ⭐ 修复错误的swizzle访问 (CRITICAL FIX)
        // vec2没有.z和.w分量，vec3没有.w分量，这是Unity shader代码转换错误导致的
        // 例如：u_TilingOffset.xy.z -> u_TilingOffset.z
        code = Regex.Replace(code, @"(\w+)\.xy\.z\b", "$1.z");
        code = Regex.Replace(code, @"(\w+)\.xy\.w\b", "$1.w");
        code = Regex.Replace(code, @"(\w+)\.xyz\.w\b", "$1.w");
        code = Regex.Replace(code, @"(\w+)\.x\.y\b", "$1.y");
        code = Regex.Replace(code, @"(\w+)\.x\.z\b", "$1.z");
        code = Regex.Replace(code, @"(\w+)\.x\.w\b", "$1.w");
        code = Regex.Replace(code, @"(\w+)\.y\.z\b", "$1.z");
        code = Regex.Replace(code, @"(\w+)\.y\.w\b", "$1.w");
        code = Regex.Replace(code, @"(\w+)\.z\.w\b", "$1.w");

        // ============================================
        // 类型转换（注意顺序，先转换长的）
        // ============================================
        code = Regex.Replace(code, @"\bfloat4x4\b", "mat4");
        code = Regex.Replace(code, @"\bfloat3x3\b", "mat3");
        code = Regex.Replace(code, @"\bfloat2x2\b", "mat2");
        code = Regex.Replace(code, @"\bhalf4x4\b", "mat4");
        code = Regex.Replace(code, @"\bhalf3x3\b", "mat3");
        code = Regex.Replace(code, @"\bhalf2x2\b", "mat2");
        code = Regex.Replace(code, @"\bfloat4\b", "vec4");
        code = Regex.Replace(code, @"\bfloat3\b", "vec3");
        code = Regex.Replace(code, @"\bfloat2\b", "vec2");
        code = Regex.Replace(code, @"\bhalf4\b", "vec4");
        code = Regex.Replace(code, @"\bhalf3\b", "vec3");
        code = Regex.Replace(code, @"\bhalf2\b", "vec2");
        code = Regex.Replace(code, @"\bhalf\b(?!\s*\()", "float"); // 避免匹配half()构造函数
        code = Regex.Replace(code, @"\bfixed4\b", "vec4");
        code = Regex.Replace(code, @"\bfixed3\b", "vec3");
        code = Regex.Replace(code, @"\bfixed2\b", "vec2");
        code = Regex.Replace(code, @"\bfixed\b(?!\s*\()", "float");
        code = Regex.Replace(code, @"\bsamplerCUBE\b", "samplerCube");
        
        // ============================================
        // 纹理采样函数（使用平衡括号匹配）
        // ============================================
        code = ConvertTextureSampling(code);
        
        // ============================================
        // 数学函数
        // ============================================
        code = Regex.Replace(code, @"\blerp\b", "mix");
        code = ConvertSaturate(code); // 使用平衡括号处理saturate
        code = Regex.Replace(code, @"\bfrac\b", "fract");
        code = Regex.Replace(code, @"\bddx\b", "dFdx");
        code = Regex.Replace(code, @"\bddy\b", "dFdy");
        code = Regex.Replace(code, @"\brsqrt\b", "inversesqrt");
        
        // atan2(y, x) -> atan(y, x) - GLSL的atan支持两个参数
        code = Regex.Replace(code, @"\batan2\b", "atan");
        
        // mul函数 - 使用平衡括号匹配
        code = ConvertMulFunction(code);
        
        // sincos函数转换 - 使用平衡括号匹配处理更复杂的情况
        code = ConvertSinCosFunction(code);
        
        // clip函数转换为discard - 使用平衡括号
        code = ConvertClipFunction(code);
        
        // ============================================
        // 类型转换语法修复 (float3x3)x -> mat3(x)
        // ============================================
        code = ConvertTypeCastSyntax(code);
        
        // ============================================
        // Unity内置变量（在变量名转换之前处理）
        // 参考 LayaAir_BuiltIn_Uniforms.md 文档
        // ============================================
        
        // 时间变量 (SceneCommon.glsl)
        // Unity _Time: float4 (t/20, t, t*2, t*3)
        // LayaAir u_Time: float 场景运行时间（秒）

        // ⭐ 修复1：先处理vec4赋值的情况（类型转换）
        // Unity: vec4 time = _Time + _TimeEditor;
        // LayaAir: vec4 time = vec4(u_Time * 0.05, u_Time, u_Time * 2.0, u_Time * 3.0);
        code = Regex.Replace(code,
            @"(float4|vec4)\s+(\w+)\s*=\s*_Time\s*\+\s*_TimeEditor\b",
            "$1 $2 = vec4(u_Time * 0.05, u_Time, u_Time * 2.0, u_Time * 3.0)");
        code = Regex.Replace(code,
            @"(float4|vec4)\s+(\w+)\s*=\s*_TimeEditor\s*\+\s*_Time\b",
            "$1 $2 = vec4(u_Time * 0.05, u_Time, u_Time * 2.0, u_Time * 3.0)");

        // ⭐ 修复2：处理分量访问（必须在整体替换之前）
        code = Regex.Replace(code, @"_Time\.y", "u_Time");      // t -> u_Time
        code = Regex.Replace(code, @"_Time\.x", "(u_Time * 0.05)"); // t/20
        code = Regex.Replace(code, @"_Time\.z", "(u_Time * 2.0)");  // t*2
        code = Regex.Replace(code, @"_Time\.w", "(u_Time * 3.0)");  // t*3
        code = Regex.Replace(code, @"_Time\.g", "u_Time");      // t
        code = Regex.Replace(code, @"_Time\.r", "(u_Time * 0.05)");
        code = Regex.Replace(code, @"_Time\.b", "(u_Time * 2.0)");
        code = Regex.Replace(code, @"_Time\.a", "(u_Time * 3.0)");

        // ⭐ 修复3：处理剩余的vec4类型赋值（没有_TimeEditor的情况）
        code = Regex.Replace(code,
            @"(float4|vec4)\s+(\w+)\s*=\s*_Time\b",
            "$1 $2 = vec4(u_Time * 0.05, u_Time, u_Time * 2.0, u_Time * 3.0)");

        // ⭐ 修复4：处理其他情况的_Time（不是vec4赋值，也不是分量访问）
        // 这种情况通常是错误的，但为了兼容性，转换为u_Time
        code = Regex.Replace(code, @"\b_Time\b", "u_Time");

        // ⭐ 修复5：_TimeEditor是Unity编辑器专用变量，运行时不存在
        // 移除所有包含_TimeEditor的加法表达式（如果还有的话）
        code = Regex.Replace(code, @"\bu_Time\s*\+\s*_TimeEditor\b", "u_Time /* editor-only var removed */");
        code = Regex.Replace(code, @"\b_TimeEditor\s*\+\s*u_Time\b", "u_Time /* editor-only var removed */");
        code = Regex.Replace(code, @"\b_Time\s*\+\s*_TimeEditor\b", "u_Time /* editor-only var removed */");
        code = Regex.Replace(code, @"\b_TimeEditor\s*\+\s*_Time\b", "u_Time /* editor-only var removed */");
        // 单独出现的_TimeEditor替换为0.0
        code = Regex.Replace(code, @"\b_TimeEditor\b", "0.0 /* editor-only */");

        // _SinTime, _CosTime
        code = Regex.Replace(code, @"_SinTime\.w", "sin(u_Time)");
        code = Regex.Replace(code, @"_SinTime\.z", "sin(u_Time * 0.5)");
        code = Regex.Replace(code, @"_SinTime\.y", "sin(u_Time * 0.25)");
        code = Regex.Replace(code, @"_SinTime\.x", "sin(u_Time * 0.125)");
        code = Regex.Replace(code, @"_CosTime\.w", "cos(u_Time)");
        code = Regex.Replace(code, @"_CosTime\.z", "cos(u_Time * 0.5)");
        code = Regex.Replace(code, @"_CosTime\.y", "cos(u_Time * 0.25)");
        code = Regex.Replace(code, @"_CosTime\.x", "cos(u_Time * 0.125)");
        
        // 矩阵变量 (CameraCommon.glsl, Sprite3DCommon.glsl)
        code = Regex.Replace(code, @"\bunity_MatrixInvV\b", "inverse(u_View)");
        code = Regex.Replace(code, @"\bunity_WorldTransformParams\.w\b", "1.0");
        code = Regex.Replace(code, @"\bUNITY_MATRIX_IT_MV\b", "transpose(inverse(u_View * u_WorldMat))");
        code = Regex.Replace(code, @"\bUNITY_MATRIX_MVP\b", "(u_ViewProjection * u_WorldMat)");
        code = Regex.Replace(code, @"\bUNITY_MATRIX_MV\b", "(u_View * u_WorldMat)");
        code = Regex.Replace(code, @"\bUNITY_MATRIX_V\b", "u_View");
        code = Regex.Replace(code, @"\bUNITY_MATRIX_P\b", "u_Projection");
        code = Regex.Replace(code, @"\bUNITY_MATRIX_M\b", "u_WorldMat");
        code = Regex.Replace(code, @"\bUNITY_MATRIX_VP\b", "u_ViewProjection");
        code = Regex.Replace(code, @"\bunity_MatrixVP\b", "u_ViewProjection");
        code = Regex.Replace(code, @"\bunity_ObjectToWorld\b", "u_WorldMat");
        code = Regex.Replace(code, @"\bunity_WorldToObject\b", "inverse(u_WorldMat)");
        
        // 相机变量 (CameraCommon.glsl)
        code = Regex.Replace(code, @"\b_WorldSpaceCameraPos\b", "u_CameraPos");
        code = Regex.Replace(code, @"\b_ProjectionParams\b", "u_ProjectionParams");
        code = Regex.Replace(code, @"\b_ZBufferParams\b", "u_ZBufferParams");
        code = Regex.Replace(code, @"\b_ScreenParams\b", "u_Viewport");
        
        // 光照变量 (Lighting.glsl)
        // 注意：文档中是 u_DirLightDirection，不是 u_DirationLightDirection
        code = Regex.Replace(code, @"\b_WorldSpaceLightPos0\b", "u_DirLightDirection");
        code = Regex.Replace(code, @"\b_LightColor0\b", "vec4(u_DirLightColor, 1.0)");
        
        // 雾效变量 (SceneCommon.glsl)
        code = Regex.Replace(code, @"\bunity_FogParams\b", "u_FogParams");
        code = Regex.Replace(code, @"\bunity_FogColor\b", "u_FogColor");
        
        // 环境光 (globalIllumination.glsl)
        code = Regex.Replace(code, @"\bunity_AmbientSky\b", "u_AmbientColor");
        code = Regex.Replace(code, @"\bunity_AmbientEquator\b", "u_AmbientColor");
        code = Regex.Replace(code, @"\bunity_AmbientGround\b", "u_AmbientColor");
        code = Regex.Replace(code, @"\bUNITY_LIGHTMODEL_AMBIENT\b", "u_AmbientColor");
        
        // 光照贴图 (globalIllumination.glsl)
        code = Regex.Replace(code, @"\bunity_Lightmap\b", "u_LightMap");
        code = Regex.Replace(code, @"\bunity_LightmapST\b", "u_LightmapScaleOffset");
        
        // 反射探针 (globalIllumination.glsl)
        code = Regex.Replace(code, @"\bunity_SpecCube0\b", "u_IBLTex");
        code = Regex.Replace(code, @"\bunity_SpecCube0_HDR\b", "u_ReflectCubeHDRParams");
        
        // ============================================
        // 变量名转换 (_XXX -> u_XXX)
        // 注意：需要避免转换已经是u_开头的，以及特殊前缀
        // ============================================
        // ⭐ 修复：匹配所有以_开头的变量名（包括小写字母开头的）
        // 原规则：@"(?<![a-zA-Z0-9_])_([A-Z]\w*)\b" 只匹配大写字母开头
        // 新规则：匹配大写或小写字母开头的变量名
        code = Regex.Replace(code, @"(?<![a-zA-Z0-9_])_([a-zA-Z]\w*)\b", "u_$1");

        // ============================================
        // 特殊纹理名称映射（在通用转换之后）
        // ============================================
        code = Regex.Replace(code, @"\bu_MainTex\b", "u_AlbedoTexture");
        code = Regex.Replace(code, @"\bu_BaseMap\b", "u_AlbedoTexture");
        code = Regex.Replace(code, @"\bu_Color\b(?!\.)", "u_AlbedoColor"); // 避免匹配 u_Color.xxx
        code = Regex.Replace(code, @"\bu_BaseColor\b", "u_AlbedoColor");
        code = Regex.Replace(code, @"\bu_BumpMap\b", "u_NormalTexture");
        code = Regex.Replace(code, @"\bu_NormalMap\b", "u_NormalTexture");
        code = Regex.Replace(code, @"\bu_BumpScale\b", "u_NormalScale");
        code = Regex.Replace(code, @"\bu_Cutoff\b", "u_AlphaTestValue");
        code = Regex.Replace(code, @"\bu_EmissionMap\b", "u_EmissionTexture");
        
        // 修复双前缀 u_u_ -> u_
        code = Regex.Replace(code, @"\bu_u_", "u_");
        
        // ============================================
        // 条件编译转换
        // ============================================
        // 处理复杂的条件编译：#ifdef A || defined(B) -> #if defined(A) || defined(B)
        code = Regex.Replace(code, @"#ifdef\s+(\w+)\s*\|\|\s*defined\s*\(\s*(\w+)\s*\)", "#if defined($1) || defined($2)");
        code = Regex.Replace(code, @"#ifdef\s+(\w+)\s*&&\s*defined\s*\(\s*(\w+)\s*\)", "#if defined($1) && defined($2)");
        
        // 修复 #elif 后面直接跟宏名的错误语法
        // #elif MACRO_NAME -> #elif defined(MACRO_NAME)
        // 注意：要排除已经是 defined() 形式的
        code = Regex.Replace(code, @"#elif\s+(?!defined\s*\()(\w+)\s*$", "#elif defined($1)", RegexOptions.Multiline);
        
        // 简单的条件编译
        code = Regex.Replace(code, @"#if\s+defined\s*\(\s*(\w+)\s*\)\s*$", "#ifdef $1", RegexOptions.Multiline);
        code = Regex.Replace(code, @"#if\s+!\s*defined\s*\(\s*(\w+)\s*\)\s*$", "#ifndef $1", RegexOptions.Multiline);
        code = Regex.Replace(code, @"#elif\s+defined\s*\(\s*(\w+)\s*\)\s*$", "#elif defined($1)", RegexOptions.Multiline);
        
        // ============================================
        // 移除不支持的Unity宏
        // ============================================
        code = Regex.Replace(code, @"UNITY_FOG_COORDS\s*\(\s*\d+\s*\)\s*;?", "");
        code = Regex.Replace(code, @"UNITY_VERTEX_INPUT_INSTANCE_ID\s*;?", "");
        code = Regex.Replace(code, @"UNITY_VERTEX_OUTPUT_STEREO\s*;?", "");
        code = Regex.Replace(code, @"UNITY_SETUP_INSTANCE_ID\s*\([^)]*\)\s*;?", "");
        code = Regex.Replace(code, @"UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO\s*\([^)]*\)\s*;?", "");
        code = Regex.Replace(code, @"UNITY_INITIALIZE_OUTPUT\s*\([^)]*\)\s*;?", "");
        code = Regex.Replace(code, @"UNITY_BRANCH\s*", "");
        code = Regex.Replace(code, @"UNITY_UNROLL\s*", "");
        code = Regex.Replace(code, @"UNITY_LOOP\s*", "");
        
        // ============================================
        // 修复宏名称
        // ============================================
        // u_XXX_ON -> XXX (宏开关)
        code = Regex.Replace(code, @"\bu_(\w+)_ON\b", "$1");
        // _XXX_ON -> XXX (可能残留的)
        code = Regex.Replace(code, @"\b_(\w+)_ON\b", "$1");
        
        // 修复条件编译中的宏名（移除u_前缀）
        code = Regex.Replace(code, @"#ifdef\s+u_([A-Z]\w*)\b", "#ifdef $1");
        code = Regex.Replace(code, @"#ifndef\s+u_([A-Z]\w*)\b", "#ifndef $1");
        code = Regex.Replace(code, @"#if\s+defined\s*\(\s*u_([A-Z]\w*)\s*\)", "#if defined($1)");
        code = Regex.Replace(code, @"#elif\s+defined\s*\(\s*u_([A-Z]\w*)\s*\)", "#elif defined($1)");
        code = Regex.Replace(code, @"defined\s*\(\s*u_([A-Z]\w*)\s*\)", "defined($1)");
        
        // 修复 #elif 直接跟 u_XXX 的情况（先转换为 defined() 形式，再移除 u_ 前缀）
        // 这种情况可能在前面的转换中产生
        code = Regex.Replace(code, @"#elif\s+u_([A-Z]\w*)\s*$", "#elif defined($1)", RegexOptions.Multiline);
        
        // ============================================
        // 常量转换
        // ============================================
        code = Regex.Replace(code, @"\bHALF_MIN\b", "0.00006103515625");
        code = Regex.Replace(code, @"\bHALF_MAX\b", "65504.0");
        code = Regex.Replace(code, @"\bFLT_MIN\b", "1.175494351e-38");
        code = Regex.Replace(code, @"\bFLT_MAX\b", "3.402823466e+38");
        code = Regex.Replace(code, @"\bFLT_EPSILON\b", "1.192092896e-07");
        code = Regex.Replace(code, @"(?<![A-Z_])PI\b", "3.14159265359");
        code = Regex.Replace(code, @"\bTWO_PI\b", "6.28318530718");
        code = Regex.Replace(code, @"\bHALF_PI\b", "1.57079632679");
        code = Regex.Replace(code, @"\bINV_PI\b", "0.31830988618");
        code = Regex.Replace(code, @"\bINV_TWO_PI\b", "0.15915494309");
        
        // ============================================
        // Unity URP/HDRP 函数转换
        // ============================================
        // GlossyEnvironmentReflection - 简化为反射采样
        code = ConvertGlossyEnvironmentReflection(code);
        
        // GetViewForwardDir
        code = ConvertFunctionWithBalancedParens(code, "GetViewForwardDir", (args) =>
        {
            return "normalize(u_CameraPos - v_PositionWS)";
        });
        
        // TransformObjectToWorld
        code = ConvertFunctionWithBalancedParens(code, "TransformObjectToWorld", (args) =>
        {
            return $"(u_WorldMat * vec4({args}, 1.0)).xyz";
        });
        
        // TransformWorldToObject
        code = ConvertFunctionWithBalancedParens(code, "TransformWorldToObject", (args) =>
        {
            return $"(inverse(u_WorldMat) * vec4({args}, 1.0)).xyz";
        });
        
        // TransformObjectToWorldDir
        code = ConvertFunctionWithBalancedParens(code, "TransformObjectToWorldDir", (args) =>
        {
            return $"normalize((u_WorldMat * vec4({args}, 0.0)).xyz)";
        });
        
        // TransformObjectToWorldNormal
        code = ConvertFunctionWithBalancedParens(code, "TransformObjectToWorldNormal", (args) =>
        {
            return $"normalize((u_WorldMat * vec4({args}, 0.0)).xyz)";
        });

        // TRANSFORM_TEX - 纹理坐标转换宏
        code = ConvertTransformTex(code);

        // ============================================
        // 数字后缀处理（移除f/h后缀）
        // 注意：使用负向后顾断言确保数字前面不是字母，避免将 v2f 转换为 v2.0
        // ============================================
        code = Regex.Replace(code, @"(?<![a-zA-Z_])(\d+\.\d*)[fFhH]\b", "$1");
        code = Regex.Replace(code, @"(?<![a-zA-Z_])(\d+)[fFhH]\b", "$1.0");
        
        // ============================================
        // 修复float类型与整数比较的问题
        // GLSL中float不能直接与int比较，需要将整数转换为浮点数
        // 例如: if (u_Mode == 1) -> if (u_Mode == 1.0)
        // ============================================
        // 匹配 u_XXX == 整数 或 u_XXX != 整数 等比较
        code = Regex.Replace(code, @"(\bu_\w+\s*)(==|!=|<|>|<=|>=)\s*(\d+)(?!\.)", "$1$2 $3.0");
        // 匹配 整数 == u_XXX 等比较
        code = Regex.Replace(code, @"(?<!\.)(\d+)\s*(==|!=|<|>|<=|>=)(\s*u_\w+\b)", "$1.0 $2$3");
        // 匹配其他可能的变量与整数比较（更通用的情况）
        // 注意：这里要小心不要影响数组索引等合法的整数使用
        // 只处理比较运算符两边的情况
        code = Regex.Replace(code, @"(\b[a-zA-Z_]\w*\s*)(==|!=)\s*(\d+)(?!\.|\s*\])", "$1$2 $3.0");
        code = Regex.Replace(code, @"(?<!\.)(\d+)\s*(==|!=)(\s*[a-zA-Z_]\w*\b)(?!\s*\[)", "$1.0 $2$3");

        // ============================================
        // 修复特定模式的类型不匹配错误
        // HLSL允许隐式类型转换，但GLSL不允许
        // 针对性地修复常见的类型错误模式
        // ============================================

        // 修复模式: vec2 var = scalar_expr * scalar_expr;
        // 例如: vec2 distortOffset = distortCol.r * u_QD;
        // 转换为: vec2 distortOffset = vec2(distortCol.r * u_QD);
        code = Regex.Replace(code,
            @"(vec2\s+\w+\s*=\s*)(?!vec[234]\()([^;]+?)(\s*\*\s*[^;]+?)(;)",
            match => {
                string prefix = match.Groups[1].Value;
                string expr = match.Groups[2].Value + match.Groups[3].Value;
                string semicolon = match.Groups[4].Value;

                // 检查表达式是否包含成员访问符（点号）或者已经有vec构造函数
                // 如果表达式看起来像是标量运算（包含.r .g .b .a或.x .y .z .w等swizzle）
                if ((expr.Contains(".r") || expr.Contains(".g") || expr.Contains(".b") || expr.Contains(".a") ||
                     expr.Contains(".x") || expr.Contains(".y") || expr.Contains(".z") || expr.Contains(".w")) &&
                    !expr.TrimStart().StartsWith("vec") &&
                    !expr.Contains("vertex.texCoord") && // 不处理已经是vec2的成员
                    !expr.Contains("v_Texcoord"))        // 不处理varying
                {
                    return $"{prefix}vec2({expr.Trim()}){semicolon}";
                }
                return match.Value;
            });

        // 修复模式: vec3 var = scalar; 或 vec3 var = vec2_expr;
        // 但要非常小心，不要误处理正确的代码

        // 修复模式: vec4 var = vec3_expr;
        // 例如: vec4 c = u_TintColor * v_VertexColor * m * n * 2.0;
        // 这种通常是正确的（vec4 * vec4 = vec4），不需要修复
        // 只修复明显错误的情况，如 vec4 var = vertex.positionOS (vec3);
        code = Regex.Replace(code,
            @"(vec4\s+\w+\s*=\s*)(?!vec[234]\()(\w+\.positionOS)(;)",
            "$1vec4($2, 1.0)$3");

        // ============================================
        // 清理多余空行
        // ============================================
        code = Regex.Replace(code, @"\n\s*\n\s*\n", "\n\n");

        // ============================================
        // ⭐ 修复粒子shader特有的类型问题
        // ============================================

        // 修复1: texture2D中v_Texcoord0需要.xy（当它是vec4时）
        // 匹配所有 texture2D(xxx, v_Texcoord0xxx) 但 v_Texcoord0 后面不是 .xy/.zw
        code = Regex.Replace(code,
            @"texture2D\s*\(\s*(\w+)\s*,\s*v_Texcoord0(?!\.)",
            "texture2D($1, v_Texcoord0.xy");

        // 修复2: 向量vec4类型uniform在vec2运算时需要.xy
        // 匹配: u_UVScroll/u_TilingOffset等在运算或函数参数中，但后面不是.xy/.zw/.xyz/.xyzw
        code = Regex.Replace(code,
            @"\b(u_UVScroll|u_TilingOffset|u_Scroll)(?!\.)([\s\)\+\-\*,])",
            match =>
            {
                string varName = match.Groups[1].Value;
                string suffix = match.Groups[2].Value;
                return $"{varName}.xy{suffix}";
            });

        // 修复3: 整数字面量乘法转换为浮点数
        // 匹配: col.rgb *= 2 * v_Color 或 *= 2 * v_Color 等模式
        // 非常重要：必须在处理向量操作之前先把整数转为浮点数
        code = Regex.Replace(code,
            @"(\*=)\s*([0-9]+)(?!\.)\s*\*",
            "$1 $2.0 *");

        // 修复: = 2* v_Color (等号后紧跟整数乘v_Color)
        code = Regex.Replace(code,
            @"(=\s*)([0-9]+)(?!\.)\s*\*\s*(v_Color|u_TintColor|v_VertexColor)\b",
            "$1$2.0 * $3");

        // 修复: 一般情况的 整数 * v_Color
        code = Regex.Replace(code,
            @"([^\d\.])\s*([0-9]+)(?!\.)\s*\*\s*(v_Color|u_TintColor|v_VertexColor)(?!\.)",
            match =>
            {
                string prefix = match.Groups[1].Value;
                string num = match.Groups[2].Value;
                string varName = match.Groups[3].Value;
                return $"{prefix} {num}.0 * {varName}";
            });

        // 修复4: 整数乘以向量需要加.0（更通用的情况）
        // 匹配: 2 * vec_var 或 vec_var * 2
        code = Regex.Replace(code,
            @"([^\d\.])([0-9]+)\s*\*\s*(v_\w+|u_\w+)\b",
            match =>
            {
                string prefix = match.Groups[1].Value;
                string num = match.Groups[2].Value;
                string varName = match.Groups[3].Value;
                // 只转换颜色、顶点相关的向量
                if (varName.Contains("Color") || varName.Contains("Vertex") || varName.Contains("Tint"))
                {
                    return $"{prefix}{num}.0 * {varName}";
                }
                return match.Value;
            });

        // 修复5: 确保向量分量访问（v_Color需要.rgb）
        // 匹配: 数字 * v_Color * xxx.rgb，v_Color应该是v_Color.rgb
        code = Regex.Replace(code,
            @"(\d+\.0|\d+)\s*\*\s*(v_Color|v_VertexColor)(?!\.)\s*\*\s*(\w+)\.rgb",
            "$1 * $2.rgb * $3.rgb");

        // 修复6: v_Color在任何与.rgb操作的表达式中都需要加.rgb
        // 处理 *= ... * v_Color * ...的情况
        code = Regex.Replace(code,
            @"\.rgb\s*\*=([^;]*)\b(v_Color|v_VertexColor)(?!\.)\b([^;]*?)(u_\w+)\.rgb",
            match =>
            {
                string expr1 = match.Groups[1].Value;
                string vColor = match.Groups[2].Value;
                string expr2 = match.Groups[3].Value;
                string uniform = match.Groups[4].Value;
                return $".rgb *={expr1}{vColor}.rgb{expr2}{uniform}.rgb";
            });

        // 修复7: 特殊处理 *= 数字 * v_Color * xxx.rgb 的完整模式
        code = Regex.Replace(code,
            @"\*=\s*(\d+\.0)\s*\*\s*(v_Color|v_VertexColor)(?!\.)\s*\*\s*(\w+)\.rgb",
            "*= $1 * $2.rgb * $3.rgb");

        // ============================================
        // ⭐ 系统性类型修复：处理所有向量维度不匹配问题
        // ============================================
        code = ApplySystematicTypeFixes(code);

        // 记录内置模式的转换时间
        if (!useMappingTableMode)
        {
            builtInConversionTime = conversionTimer.ElapsedMilliseconds;
        }

        return code;
    }

    /// <summary>
    /// 根据GLSL类型返回默认值
    /// </summary>
    private static string GetDefaultValueForType(string glslType)
    {
        switch (glslType)
        {
            case "float": return "0.0";
            case "vec2": return "vec2(0.0, 0.0)";
            case "vec3": return "vec3(0.0, 0.0, 0.0)";
            case "vec4": return "vec4(0.0, 0.0, 0.0, 0.0)";
            case "int": return "0";
            case "mat2": return "mat2(1.0)";
            case "mat3": return "mat3(1.0)";
            case "mat4": return "mat4(1.0)";
            default: return "0.0";
        }
    }

    /// <summary>
    /// 系统性类型修复：自动检测并修复所有向量维度不匹配问题
    /// </summary>
    private static string ApplySystematicTypeFixes(string code)
    {

        // ============================================
        // 阶段0: 移除/替换Unity平台特定代码
        // ============================================

        // 移除Unity条件编译块 - 智能保留else分支内容
        // 处理 #if UNITY_XXX ... #else ... #endif - 保留else分支
        code = Regex.Replace(code,
            @"#\s*if\s+UNITY_[^\n]+\s*\n(.*?)#\s*else\s*\n(.*?)#\s*endif",
            match => match.Groups[2].Value,  // 保留else分支的内容
            RegexOptions.Singleline);

        // 处理 #ifdef UNITY_XXX ... #else ... #endif - 保留else分支
        code = Regex.Replace(code,
            @"#\s*ifdef\s+UNITY_[^\n]+\s*\n(.*?)#\s*else\s*\n(.*?)#\s*endif",
            match => match.Groups[2].Value,  // 保留else分支的内容
            RegexOptions.Singleline);

        // 处理 #ifndef UNITY_XXX ... #else ... #endif - 保留if分支（因为UNITY_XXX不存在）
        code = Regex.Replace(code,
            @"#\s*ifndef\s+UNITY_[^\n]+\s*\n(.*?)#\s*else\s*\n(.*?)#\s*endif",
            match => match.Groups[1].Value,  // 保留if分支的内容
            RegexOptions.Singleline);

        // 处理没有else的单分支条件编译 - 完全移除
        // #if UNITY_XXX ... #endif (without else)
        code = Regex.Replace(code,
            @"#\s*if\s+UNITY_[^\n]+\s*\n.*?#\s*endif",
            "",
            RegexOptions.Singleline);

        // #ifdef UNITY_XXX ... #endif (without else)
        code = Regex.Replace(code,
            @"#\s*ifdef\s+UNITY_[^\n]+\s*\n.*?#\s*endif",
            "",
            RegexOptions.Singleline);

        // #ifndef UNITY_XXX ... #endif (without else) - 保留内容（因为UNITY_XXX不存在，条件为真）
        code = Regex.Replace(code,
            @"#\s*ifndef\s+UNITY_[^\n]+\s*\n(.*?)#\s*endif",
            match => match.Groups[1].Value,  // 保留内容
            RegexOptions.Singleline);

        // 移除带defined的复杂条件编译
        // #if defined(UNITY_XXX) ... #else ... #endif - 保留else分支
        code = Regex.Replace(code,
            @"#\s*if\s+defined\s*\(\s*UNITY_[^\)]+\).*?\n(.*?)#\s*else\s*\n(.*?)#\s*endif",
            match => match.Groups[2].Value,
            RegexOptions.Singleline);

        // #if !defined(UNITY_XXX) ... #else ... #endif - 保留if分支
        code = Regex.Replace(code,
            @"#\s*if\s+!defined\s*\(\s*UNITY_[^\)]+\).*?\n(.*?)#\s*else\s*\n(.*?)#\s*endif",
            match => match.Groups[1].Value,
            RegexOptions.Singleline);

        // 单分支defined
        code = Regex.Replace(code,
            @"#\s*if\s+defined\s*\(\s*UNITY_[^\)]+\).*?\n.*?#\s*endif",
            "",
            RegexOptions.Singleline);

        code = Regex.Replace(code,
            @"#\s*if\s+!defined\s*\(\s*UNITY_[^\)]+\).*?\n(.*?)#\s*endif",
            match => match.Groups[1].Value,
            RegexOptions.Singleline);

        // 移除Unity宏调用（UNITY_开头的函数调用）
        // UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
        code = Regex.Replace(code,
            @"UNITY_[A-Z_0-9]+\s*\([^)]*\)\s*;?\s*\n?",
            "");

        // 替换Unity屏幕纹理采样宏 - 保留为texture2D调用
        // UNITY_SAMPLE_SCREENSPACE_TEXTURE(tex, uv) -> texture2D(tex, uv)
        code = Regex.Replace(code,
            @"UNITY_SAMPLE_SCREENSPACE_TEXTURE\s*\(\s*([^,]+)\s*,\s*([^)]+)\)",
            "texture2D($1, $2)");

        // 替换Unity纹理采样宏（其他变体）
        code = Regex.Replace(code,
            @"UNITY_SAMPLE_TEX2D\s*\(\s*([^,]+)\s*,\s*([^)]+)\)",
            "texture2D($1, $2)");

        code = Regex.Replace(code,
            @"UNITY_SAMPLE_TEX2D_SAMPLER\s*\(\s*([^,]+)\s*,\s*([^,]+)\s*,\s*([^)]+)\)",
            "texture2D($1, $3)");

        // 移除Unity相关的空语句和多余空行
        code = Regex.Replace(code, @"\n\s*\n\s*\n+", "\n\n");

        // ============================================
        // 阶段0.5: 修复错误的变量声明链和类型不匹配
        // ============================================

        // 检测并修复类型不匹配的连续赋值
        // 例如: vec4 a = vec4 b = vec2 c = vec2(1, 2);
        // 这种情况通常是由于Unity宏替换或者换行符丢失导致的

        // 多遍处理，因为可能有多层嵌套
        for (int pass = 0; pass < 3; pass++)
        {
            // 模式1: 检测 "类型1 变量1 = 类型2 变量2 =" 的模式
            // 这会匹配链式赋值的前面部分，拆分成独立语句
            code = Regex.Replace(code,
                @"((?:vec[234]|float|int|mat[234])\s+\w+)\s*=\s*((?:vec[234]|float|int|mat[234])\s+\w+)\s*=",
                match => {
                    string decl1 = match.Groups[1].Value;  // vec4 a
                    string decl2 = match.Groups[2].Value;  // vec4 b
                    // 拆分成两个独立声明，第一个先初始化为默认值
                    string type1 = Regex.Match(decl1, @"(vec[234]|float|int|mat[234])").Value;
                    string defaultVal = GetDefaultValueForType(type1);
                    return $"{decl1} = {defaultVal};\n        {decl2} =";
                });

            // 模式2: 检测 "类型1 变量1 = 类型2 变量2 = 表达式;" 且类型不同
            code = Regex.Replace(code,
                @"(vec[234]|float|int|mat[234])\s+(\w+)\s*=\s*(vec[234]|float|int|mat[234])\s+(\w+)\s*=\s*([^;]+);",
                match => {
                    string type1 = match.Groups[1].Value;
                    string var1 = match.Groups[2].Value;
                    string type2 = match.Groups[3].Value;
                    string var2 = match.Groups[4].Value;
                    string expr = match.Groups[5].Value;

                    // 如果类型不同，拆分声明
                    if (type1 != type2)
                    {
                        string defaultVal1 = GetDefaultValueForType(type1);
                        return $"{type1} {var1} = {defaultVal1};\n        {type2} {var2} = {expr};";
                    }
                    // 类型相同，保持原样（这是合法的链式赋值）
                    return match.Value;
                });
        }

        // 模式3: 修复语句合并导致的缺少换行问题
        // vec4 screenColor01 = ...; vec4 screenColor02 = ...（缺少换行）
        code = Regex.Replace(code,
            @";\s*((?:vec[234]|float|int|mat[234])\s+\w+\s*=)",
            ";\n        $1");

        // 模式4: 清理可能产生的多余空行
        code = Regex.Replace(code, @"\n\s*\n\s*\n+", "\n\n");

        // 模式4.5: 移除未使用的varying声明（避免编译错误）
        // 检测varying声明但在代码中从未使用的变量
        var varyingMatches = Regex.Matches(code, @"varying\s+(vec[234]|float|int|mat[234])\s+(\w+)\s*;");
        foreach (Match match in varyingMatches)
        {
            string varyingName = match.Groups[2].Value;
            // 检查这个varying是否在代码中被使用（赋值或读取）
            string usagePattern = $@"\b{varyingName}\s*[=\.]";
            if (!Regex.IsMatch(code, usagePattern))
            {
                // 未使用，移除声明
                code = code.Replace(match.Value, $"// {match.Value} (unused)");
            }
        }

        // 模式5: 修复不完整的赋值语句（变量声明后面只有 = )
        // vec4 x = ); -> 删除整行
        code = Regex.Replace(code,
            @"(?:vec[234]|float|int|mat[234])\s+\w+\s*=\s*\)\s*;",
            "// removed incomplete assignment");

        // 模式6: 修复vec类型之间的不匹配赋值
        // vec4 x = expr.xy; -> vec4 x = vec4(expr.xy, 0.0, 0.0);
        // vec2 x = vec4(...); -> vec2 x = vec4(...).xy;
        // vec4 x = vec2(...); -> vec4 x = vec4(vec2(...), 0.0, 0.0);

        // 修复6a: vec4 = xxx.xy（vec2赋值给vec4）
        code = Regex.Replace(code,
            @"(vec4)\s+(\w+)\s*=\s*([^;]+\.xy)(?!\s*[,\)])\s*;",
            match => {
                string type = match.Groups[1].Value;
                string varName = match.Groups[2].Value;
                string expr = match.Groups[3].Value;
                // 如果表达式很简单（单个变量.xy），可能原意是使用整个vec4
                if (Regex.IsMatch(expr, @"^\w+\.xy$"))
                {
                    string baseVar = expr.Substring(0, expr.Length - 3);
                    return $"{type} {varName} = {baseVar};";
                }
                return $"{type} {varName} = vec4({expr}, 0.0, 0.0);";
            });

        // 修复6b: vec2 = vec4(...)（vec4赋值给vec2）
        code = Regex.Replace(code,
            @"(vec2)\s+(\w+)\s*=\s*(vec4\([^)]+\))\s*;",
            match => {
                string type = match.Groups[1].Value;
                string varName = match.Groups[2].Value;
                string expr = match.Groups[3].Value;
                return $"{type} {varName} = {expr}.xy;";
            });

        // 修复6c: vec4 = vec2(...)（vec2赋值给vec4）
        code = Regex.Replace(code,
            @"(vec4)\s+(\w+)\s*=\s*(vec2\([^)]+\))\s*;",
            match => {
                string type = match.Groups[1].Value;
                string varName = match.Groups[2].Value;
                string expr = match.Groups[3].Value;
                return $"{type} {varName} = vec4({expr}, 0.0, 0.0);";
            });

        // ============================================
        // 阶段1: 修复vec2运算中的vec4变量（自动添加.xy）
        // ============================================

        // 修复1a: vec2(...) + v_Texcoord0  -> vec2(...) + v_Texcoord0.xy
        code = Regex.Replace(code,
            @"(vec2\s*\([^)]+\))\s*([\+\-])\s*v_Texcoord0(?!\.)",
            "$1 $2 v_Texcoord0.xy");

        // 修复1b: v_Texcoord0 + vec2(...)  -> v_Texcoord0.xy + vec2(...)
        code = Regex.Replace(code,
            @"\bv_Texcoord0(?!\.)\s*([\+\-])\s*(vec2\s*\()",
            "v_Texcoord0.xy $1 $2");

        // 修复1c: texture2D参数中的vec4变量（强制修复，多次应用）
        // 第一遍：直接在texture2D第二个参数开头的v_Texcoord0
        code = Regex.Replace(code,
            @"texture2D\s*\(\s*(\w+)\s*,\s*v_Texcoord0(?!\.)",
            "texture2D($1, v_Texcoord0.xy");

        // 第二遍：texture2D内部任何位置的v_Texcoord0
        // 匹配整个texture2D调用，修复其中的v_Texcoord0
        code = Regex.Replace(code,
            @"texture2D\s*\([^)]+\)",
            match => {
                string texCall = match.Value;
                // 在这个texture2D调用内部，为所有v_Texcoord0添加.xy
                texCall = Regex.Replace(texCall, @"\bv_Texcoord0(?!\.)", "v_Texcoord0.xy");
                return texCall;
            });

        // 修复1d: vec2()构造函数的参数中出现vec4
        // vec2(expr + v_Texcoord0) -> vec2(expr + v_Texcoord0.xy)
        code = Regex.Replace(code,
            @"vec2\s*\(([^)]*)\bv_Texcoord0(?!\.)\b([^)]*)\)",
            match => {
                string content = match.Groups[1].Value + "v_Texcoord0.xy" + match.Groups[2].Value;
                return $"vec2({content})";
            });

        // ============================================
        // 阶段2: 修复vec4 uniform在vec2上下文中的使用
        // ============================================

        // 修复2a: vec2运算中的vec4 uniform（如u_UVScroll, u_TilingOffset等）
        string[] vec4Uniforms = { "u_UVScroll", "u_TilingOffset", "u_Scroll", "u_distort_tex_ST", "u_MainTex_ST" };
        foreach (var uniformName in vec4Uniforms)
        {
            // 在vec2构造函数、texture2D、或vec2运算中，自动添加.xy
            code = Regex.Replace(code,
                $@"\b{uniformName}(?!\.)([\s\)\+\-\*,;])",
                match => {
                    string suffix = match.Groups[1].Value;
                    // 检查上下文，如果在需要vec2的地方，添加.xy
                    return $"{uniformName}.xy{suffix}";
                });
        }

        // ============================================
        // 阶段3: 通用整数字面量修复（整数->浮点数）
        // ============================================

        // ⭐ 核心原则：只转换数字字面量，不触碰标识符中的数字和浮点数
        // 数字字面量定义：
        //   - 前后都不是字母、数字、点号、下划线的数字序列
        //   - 覆盖所有运算符：算术、比较、三元、逻辑等
        //   - 排除数组索引（方括号内）
        //   - 排除科学记数法中的指数部分（1e-5不转换成1e-5.0）

        // 多遍扫描，确保完全转换
        for (int pass = 0; pass < 2; pass++)
        {
            // 模式1a: 加减号（排除科学记数法） + 空格 + 整数
            code = Regex.Replace(code, @"(?<![eE])([\+\-]\s+)(\d+)(?![0-9.a-zA-Z_])", "$1$2.0");

            // 模式1b: 其他运算符 + 空格 + 整数
            code = Regex.Replace(code, @"([\*\/%=<>!?:&|\(,]\s+)(\d+)(?![0-9.a-zA-Z_])", "$1$2.0");

            // 模式2a: 加减号（排除科学记数法） + 整数（无空格）
            code = Regex.Replace(code, @"(?<![eE])([\+\-])(\d+)(?![0-9.a-zA-Z_])", "$1$2.0");

            // 模式2b: 其他运算符 + 整数（无空格）
            code = Regex.Replace(code, @"([\*\/%=<>!?:&|\(,])(\d+)(?![0-9.a-zA-Z_])", "$1$2.0");

            // 模式3: 整数 + 空格 + 运算符/分隔符
            code = Regex.Replace(code, @"(?<![0-9.a-zA-Z_])(\d+)(\s+[\+\-\*\/%<>!?:&|\),;])", "$1.0$2");

            // 模式4: 整数 + 运算符/分隔符（无空格）
            code = Regex.Replace(code, @"(?<![0-9.a-zA-Z_])(\d+)([\+\-\*\/%<>!?:&|\),;])", "$1.0$2");
        }

        // 向量/矩阵构造函数中的整数
        code = Regex.Replace(code,
            @"(vec[234]|mat[234])\s*\(([^)]+)\)",
            match => {
                string vecType = match.Groups[1].Value;
                string args = match.Groups[2].Value;
                // 转换参数列表中的整数字面量（排除科学记数法）
                args = Regex.Replace(args, @"(?<![eE])([,\(\+\-]\s*)(\d+)(?![0-9.a-zA-Z_])", "$1$2.0");
                args = Regex.Replace(args, @"([,\(\*\/%<>!?:&|]\s*)(\d+)(?![0-9.a-zA-Z_])", "$1$2.0");
                args = Regex.Replace(args, @"(?<![0-9.a-zA-Z_])(\d+)(\s*[,\)\+\-\*\/%<>!?:&|])", "$1.0$2");
                return $"{vecType}({args})";
            });

        // ============================================
        // 阶段4: 智能修复vec3/vec4变量的分量访问
        // ============================================

        // 已知的vec4变量列表
        string[] vec4Vars = { "v_Color", "v_VertexColor", "u_TintColor", "u_AlbedoColor", "u_BaseColor", "u_Color" };

        // 修复4a: .rgb 上下文中的vec4变量
        // 匹配任何包含 .rgb 的赋值或运算语句
        code = Regex.Replace(code,
            @"((?:^|\s)(?:\w+\.)?rgb\s*[\*\+\-\/]?=\s*)([^;]+);",
            match => {
                string prefix = match.Groups[1].Value;
                string expr = match.Groups[2].Value;

                // 为所有vec4变量添加.rgb
                foreach (var vec4Var in vec4Vars)
                {
                    // 只在没有分量访问的情况下添加.rgb
                    expr = Regex.Replace(expr, $@"\b{vec4Var}(?!\.)", $"{vec4Var}.rgb");
                }

                // 清理重复
                expr = Regex.Replace(expr, @"\.rgb\.rgb", ".rgb");

                return $"{prefix}{expr};";
            }, RegexOptions.Multiline);

        // 修复4b: .rgb = 表达式中的vec4变量需要.rgb
        code = Regex.Replace(code,
            @"(\.rgb\s*=\s*)([^;]+);",
            match => {
                string prefix = match.Groups[1].Value;
                string expr = match.Groups[2].Value;

                foreach (var vec4Var in vec4Vars)
                {
                    expr = Regex.Replace(expr, $@"\b{vec4Var}(?!\.)", $"{vec4Var}.rgb");
                }

                expr = Regex.Replace(expr, @"\.rgb\.rgb", ".rgb");
                return $"{prefix}{expr};";
            });

        // 修复4c: 表达式中vec4与xxx.rgb运算的情况
        // 只要看到 xxx.rgb，那么同一运算中的vec4变量也需要.rgb
        foreach (var vec4Var in vec4Vars)
        {
            // vec4 * xxx.rgb -> vec4.rgb * xxx.rgb
            code = Regex.Replace(code,
                $@"\b{vec4Var}(?!\.)\s*\*\s*(\w+)\.rgb",
                $"{vec4Var}.rgb * $1.rgb");

            // xxx.rgb * vec4 -> xxx.rgb * vec4.rgb
            code = Regex.Replace(code,
                $@"(\w+)\.rgb\s*\*\s*{vec4Var}(?!\.)",
                $"$1.rgb * {vec4Var}.rgb");
        }

        // ============================================
        // 阶段4.5: 修复.a分量运算中的vec4变量
        // ============================================

        // 修复4.5a: .a *= 表达式中的v_Color需要.a
        code = Regex.Replace(code,
            @"(\.a\s*[\*\+\-]?=\s*)([^;]+);",
            match => {
                string prefix = match.Groups[1].Value;
                string expr = match.Groups[2].Value;

                // 在表达式中为v_Color添加.a（如果后面不是.rgb也不是.a）
                expr = Regex.Replace(expr, @"\bv_Color(?!\.(?:rgb|a))\b", "v_Color.a");
                expr = Regex.Replace(expr, @"\bv_VertexColor(?!\.(?:rgb|a))\b", "v_VertexColor.a");

                // 修复可能的重复.a.a
                expr = Regex.Replace(expr, @"v_Color\.a\.a", "v_Color.a");
                expr = Regex.Replace(expr, @"v_VertexColor\.a\.a", "v_VertexColor.a");

                return $"{prefix}{expr};";
            });

        // 修复4.5b: xxx.a * v_Color -> xxx.a * v_Color.a
        code = Regex.Replace(code,
            @"(\w+)\.a\s*\*\s*v_Color(?!\.)",
            "$1.a * v_Color.a");

        // 修复4.5c: v_Color * xxx.a -> v_Color.a * xxx.a
        code = Regex.Replace(code,
            @"\bv_Color(?!\.)\s*\*\s*(\w+)\.a\b",
            "v_Color.a * $1.a");

        // ============================================
        // 阶段4.6: 通用vec4 Color类型uniform的分量修复
        // ============================================

        // 已知的vec4/Color类型变量
        string[] vec4ColorVars = { "v_Color", "v_VertexColor", "u_TintColor", "u_AlbedoColor", "u_BaseColor", "u_Color" };

        foreach (var colorVar in vec4ColorVars)
        {
            // 在.rgb上下文中自动添加.rgb
            code = Regex.Replace(code,
                $@"\b{colorVar}(?!\.)\s*\*\s*(\w+)\.rgb",
                $"{colorVar}.rgb * $1.rgb");

            code = Regex.Replace(code,
                $@"(\w+)\.rgb\s*\*\s*{colorVar}(?!\.)",
                $"$1.rgb * {colorVar}.rgb");

            // 在.a上下文中自动添加.a
            code = Regex.Replace(code,
                $@"\b{colorVar}(?!\.(?:rgb|a))\s*\*\s*(\w+)\.a",
                $"{colorVar}.a * $1.a");

            code = Regex.Replace(code,
                $@"(\w+)\.a\s*\*\s*{colorVar}(?!\.(?:rgb|a))",
                $"$1.a * {colorVar}.a");
        }

        // ============================================
        // 阶段5: 修复vec2()构造函数中的问题
        // ============================================

        // 修复5a: vec2(vec4的分量运算)
        // 查找所有vec2(...)中的内容，检查是否有未处理的vec4变量
        code = Regex.Replace(code,
            @"vec2\s*\(\s*\(\s*([^)]+)\s*\)\s*\)",
            match => {
                string inner = match.Groups[1].Value;
                // 如果inner包含_ST但没有.xy/.zw，添加.xy
                if (inner.Contains("_ST") && !inner.Contains(".xy") && !inner.Contains(".zw"))
                {
                    inner = Regex.Replace(inner, @"\b(\w+_ST)(?!\.)", "$1.xy");
                }
                return $"vec2(({inner}))";
            });

        // ============================================
        // 阶段6: 强制修复特定的已知vec4变量在vec2上下文中的使用
        // ============================================

        // ⭐ 修复6: 智能处理v_Texcoord0的分量访问
        // 关键：根据赋值目标类型决定是否需要.xy

        // 修复6a: vec2 = v_Texcoord0 -> vec2 = v_Texcoord0.xy
        code = Regex.Replace(code,
            @"(vec2\s+\w+\s*=\s*)v_Texcoord0(?!\.)",
            "$1v_Texcoord0.xy");

        // 修复6b: 已有vec2变量赋值 = v_Texcoord0 -> = v_Texcoord0.xy
        code = Regex.Replace(code,
            @"(?<=\s)(\w+\.(?:xy|rg))\s*=\s*v_Texcoord0(?!\.)",
            "$1 = v_Texcoord0.xy");

        // 修复6c: vec4 = v_Texcoord0.xy -> vec4 = v_Texcoord0 (移除不必要的.xy)
        code = Regex.Replace(code,
            @"(vec4\s+\w+\s*=\s*)v_Texcoord0\.xy(?!\s*[,\+\-\*\/])",
            "$1v_Texcoord0");

        // 修复6d: texture2D中的v_Texcoord0如果没有分量，添加.xy
        // 已经在阶段1中处理过了，这里只清理重复

        // 修复6e: 移除可能产生的重复.xy或错误的分量组合
        code = Regex.Replace(code, @"v_Texcoord0\.xy\.xy", "v_Texcoord0.xy");
        code = Regex.Replace(code, @"v_Texcoord0\.xy\.zw", "v_Texcoord0.xy");
        code = Regex.Replace(code, @"v_Texcoord0\.xy\.x(?!y)", "v_Texcoord0.x");
        code = Regex.Replace(code, @"v_Texcoord0\.xy\.y(?!x)", "v_Texcoord0.y");
        code = Regex.Replace(code, @"v_Texcoord0\.xy\.r", "v_Texcoord0.x");
        code = Regex.Replace(code, @"v_Texcoord0\.xy\.g", "v_Texcoord0.y");

        // ============================================
        // 阶段7: 最后清理：确保所有_ST uniforms在需要vec2的地方有.xy
        // ============================================

        // 修复7a: 在texture2D的第二个参数中，_ST应该有.xy或.zw
        code = Regex.Replace(code,
            @"texture2D\s*\([^,]+,\s*([^)]*)\)",
            match => {
                string args = match.Value;
                // 在纹理坐标参数中，为所有_ST添加.xy（如果没有）
                args = Regex.Replace(args, @"\b(\w+_ST)(?!\.(?:xy|zw))\b", "$1.xy");
                return args;
            });

        // 修复7b: vec2构造函数中的_ST
        code = Regex.Replace(code,
            @"vec2\s*\([^)]*\)",
            match => {
                string vecExpr = match.Value;
                // 在vec2(...)中为_ST添加.xy
                vecExpr = Regex.Replace(vecExpr, @"\b(\w+_ST)(?!\.(?:xy|zw))\b", "$1.xy");
                return vecExpr;
            });

        // ============================================
        // 阶段7.5: 修复texture()函数调用（GLSL ES 2.0不支持texture()）
        // ============================================

        // GLSL ES 2.0只支持texture2D(), texture2DProj(), textureCube()等
        // texture()是GLSL ES 3.0的函数，需要替换为texture2D()
        code = Regex.Replace(code,
            @"\btexture\s*\(",
            "texture2D(");

        // ============================================
        // 阶段8: 修复加法/减法运算中的向量类型不匹配
        // ============================================

        // 修复8a: vec2 + vec4 -> vec2 + vec4.xy
        // 修复8b: vec3 + vec4 -> vec3 + vec4.rgb
        // 修复8c: vec4 + vec2 -> vec4 + vec4(vec2, 0.0, 0.0)

        // 检测所有加减法运算，分析左右操作数的类型
        // 模式：(类型 变量 = 表达式 +/- 表达式)

        // 修复8a: 在加减法中，如果发现vec4变量与vec2类型运算，自动添加.xy
        code = Regex.Replace(code,
            @"(vec2\s*\([^)]+\))\s*([\+\-])\s*\bv_Texcoord0(?!\.)",
            "$1 $2 v_Texcoord0.xy");

        code = Regex.Replace(code,
            @"\bv_Texcoord0(?!\.)\s*([\+\-])\s*(vec2\s*\()",
            "v_Texcoord0.xy $1 $2");

        // 修复8b: vec4 uniforms与vec2类型运算
        foreach (var uniformName in vec4Uniforms)
        {
            // vec2(...) + u_Uniform -> vec2(...) + u_Uniform.xy
            code = Regex.Replace(code,
                $@"(vec2\s*\([^)]+\))\s*([\+\-])\s*\b{uniformName}(?!\.)",
                $"$1 $2 {uniformName}.xy");

            // u_Uniform + vec2(...) -> u_Uniform.xy + vec2(...)
            code = Regex.Replace(code,
                $@"\b{uniformName}(?!\.)\s*([\+\-])\s*(vec2\s*\()",
                $"{uniformName}.xy $1 $2");
        }

        // 修复8c: 在赋值语句中检测类型不匹配的加法
        // vec2 result = expr + v_Texcoord0 -> vec2 result = expr + v_Texcoord0.xy
        code = Regex.Replace(code,
            @"(vec2\s+\w+\s*=\s*[^;]*)([\+\-])\s*v_Texcoord0(?!\.)",
            match => {
                string prefix = match.Groups[1].Value;
                string op = match.Groups[2].Value;
                // 检查prefix中是否已经有v_Texcoord0.xy
                if (prefix.Contains("v_Texcoord0.xy"))
                    return match.Value;
                return $"{prefix}{op} v_Texcoord0.xy";
            });

        return code;
    }

    /// <summary>
    /// 转换GlossyEnvironmentReflection函数
    /// </summary>
    private static string ConvertGlossyEnvironmentReflection(string code)
    {
        return ConvertFunctionWithBalancedParens(code, "GlossyEnvironmentReflection", (args) =>
        {
            var parts = SplitFunctionArgs(args, 3);
            if (parts.Count >= 2)
            {
                // 简化为反射采样
                return $"vec3(0.0)"; // 需要反射探针支持，暂时返回0
            }
            return "vec3(0.0)";
        });
    }

    /// <summary>
    /// 转换sincos函数 - 使用平衡括号匹配
    /// sincos(angle, out s, out c) -> s = sin(angle); c = cos(angle);
    /// </summary>
    private static string ConvertSinCosFunction(string code)
    {
        // 匹配 sincos(expr, var1, var2);
        var pattern = @"sincos\s*\(";
        int startIdx = 0;
        StringBuilder result = new StringBuilder();
        
        while (startIdx < code.Length)
        {
            var match = Regex.Match(code.Substring(startIdx), pattern);
            if (!match.Success)
            {
                result.Append(code.Substring(startIdx));
                break;
            }
            
            int funcStart = startIdx + match.Index;
            result.Append(code.Substring(startIdx, match.Index));
            
            // 找到左括号位置
            int parenStart = funcStart + match.Length - 1;
            int parenCount = 1;
            int i = parenStart + 1;
            
            while (i < code.Length && parenCount > 0)
            {
                if (code[i] == '(') parenCount++;
                else if (code[i] == ')') parenCount--;
                i++;
            }
            
            if (parenCount == 0)
            {
                string args = code.Substring(parenStart + 1, i - parenStart - 2);
                var parts = SplitFunctionArgs(args, 3);
                
                if (parts.Count >= 3)
                {
                    string angle = parts[0].Trim();
                    string sinVar = parts[1].Trim();
                    string cosVar = parts[2].Trim();
                    result.Append($"{sinVar} = sin({angle}); {cosVar} = cos({angle});");
                }
                else
                {
                    result.Append($"/* sincos conversion failed: {args} */");
                }
                
                // 跳过原来的分号（已经在转换中添加了）
                if (i < code.Length && code[i] == ';')
                    i++;
                    
                startIdx = i;
            }
            else
            {
                result.Append(code.Substring(funcStart, i - funcStart));
                startIdx = i;
            }
        }
        
        return result.ToString();
    }

    /// <summary>
    /// 转换类型转换语法 (type)expr -> type(expr)
    /// 例如: (mat3)matrix -> mat3(matrix)
    /// </summary>
    private static string ConvertTypeCastSyntax(string code)
    {
        // 匹配 (mat4), (mat3), (mat2), (vec4), (vec3), (vec2), (float), (int) 类型转换
        string[] types = { "mat4", "mat3", "mat2", "vec4", "vec3", "vec2", "float", "int", "bool" };
        
        foreach (var type in types)
        {
            // 匹配 (type)identifier 或 (type)(expr)
            // (mat3)someMatrix -> mat3(someMatrix)
            code = Regex.Replace(code, $@"\(\s*{type}\s*\)\s*(\w+)", $"{type}($1)");
            // (mat3)(expr) -> mat3(expr) - 这种情况括号已经存在
            code = Regex.Replace(code, $@"\(\s*{type}\s*\)\s*\(", $"{type}(");
        }
        
        return code;
    }

    /// <summary>
    /// 转换纹理采样函数（使用平衡括号）
    /// </summary>
    private static string ConvertTextureSampling(string code)
    {
        // tex2D
        code = ConvertFunctionWithBalancedParens(code, "tex2D", (args) =>
        {
            var parts = SplitFunctionArgs(args, 2);
            if (parts.Count >= 2)
            {
                string texName = ConvertTextureName(parts[0].Trim());
                return $"texture2D({texName}, {parts[1]})";
            }
            return $"texture2D({args})";
        });
        
        // tex2Dlod
        code = ConvertFunctionWithBalancedParens(code, "tex2Dlod", (args) =>
        {
            var parts = SplitFunctionArgs(args, 2);
            if (parts.Count >= 2)
            {
                string texName = ConvertTextureName(parts[0].Trim());
                return $"texture2D({texName}, {parts[1]}.xy)";
            }
            return $"texture2D({args})";
        });
        
        // texCUBE
        code = ConvertFunctionWithBalancedParens(code, "texCUBE", (args) =>
        {
            var parts = SplitFunctionArgs(args, 2);
            if (parts.Count >= 2)
            {
                string texName = ConvertTextureName(parts[0].Trim());
                return $"textureCube({texName}, {parts[1]})";
            }
            return $"textureCube({args})";
        });
        
        // texCUBElod
        code = ConvertFunctionWithBalancedParens(code, "texCUBElod", (args) =>
        {
            var parts = SplitFunctionArgs(args, 2);
            if (parts.Count >= 2)
            {
                string texName = ConvertTextureName(parts[0].Trim());
                return $"textureCube({texName}, {parts[1]}.xyz)";
            }
            return $"textureCube({args})";
        });
        
        return code;
    }

    /// <summary>
    /// 转换纹理名称
    /// </summary>
    private static string ConvertTextureName(string texName)
    {
        // 先转换 _XXX -> u_XXX
        if (texName.StartsWith("_"))
            texName = "u_" + texName.Substring(1);
        
        // 特殊纹理名映射
        switch (texName)
        {
            case "u_MainTex": return "u_AlbedoTexture";
            case "u_BaseMap": return "u_AlbedoTexture";
            case "u_BumpMap": return "u_NormalTexture";
            case "u_NormalMap": return "u_NormalTexture";
            case "u_EmissionMap": return "u_EmissionTexture";
            default: return texName;
        }
    }

    /// <summary>
    /// 转换saturate函数
    /// </summary>
    private static string ConvertSaturate(string code)
    {
        return ConvertFunctionWithBalancedParens(code, "saturate", (args) =>
        {
            return $"clamp({args}, 0.0, 1.0)";
        });
    }

    /// <summary>
    /// 转换mul函数
    /// </summary>
    private static string ConvertMulFunction(string code)
    {
        return ConvertFunctionWithBalancedParens(code, "mul", (args) =>
        {
            var parts = SplitFunctionArgs(args, 2);
            if (parts.Count >= 2)
                return $"({parts[0]} * {parts[1]})";
            return args;
        });
    }

    /// <summary>
    /// 转换clip函数
    /// </summary>
    private static string ConvertClipFunction(string code)
    {
        return ConvertFunctionWithBalancedParens(code, "clip", (args) =>
        {
            return $"if (({args}) < 0.0) {{ discard; }}";
        });
    }

    /// <summary>
    /// 使用平衡括号转换函数调用
    /// </summary>
    private static string ConvertFunctionWithBalancedParens(string code, string funcName, Func<string, string> converter)
    {
        StringBuilder result = new StringBuilder();
        int i = 0;
        
        while (i < code.Length)
        {
            // 查找函数名
            int funcStart = code.IndexOf(funcName, i);
            if (funcStart < 0)
            {
                result.Append(code.Substring(i));
                break;
            }
            
            // 检查是否是完整的函数名（前面不是字母数字下划线）
            if (funcStart > 0 && (char.IsLetterOrDigit(code[funcStart - 1]) || code[funcStart - 1] == '_'))
            {
                result.Append(code.Substring(i, funcStart - i + funcName.Length));
                i = funcStart + funcName.Length;
                continue;
            }
            
            // 检查后面是否是左括号
            int afterFunc = funcStart + funcName.Length;
            while (afterFunc < code.Length && char.IsWhiteSpace(code[afterFunc]))
                afterFunc++;
            
            if (afterFunc >= code.Length || code[afterFunc] != '(')
            {
                result.Append(code.Substring(i, afterFunc - i));
                i = afterFunc;
                continue;
            }
            
            // 找到匹配的右括号
            int parenStart = afterFunc;
            int parenCount = 1;
            int j = parenStart + 1;
            
            while (j < code.Length && parenCount > 0)
            {
                if (code[j] == '(') parenCount++;
                else if (code[j] == ')') parenCount--;
                j++;
            }
            
            if (parenCount == 0)
            {
                // 提取参数
                string args = code.Substring(parenStart + 1, j - parenStart - 2);
                string converted = converter(args);
                
                result.Append(code.Substring(i, funcStart - i));
                result.Append(converted);
                i = j;
            }
            else
            {
                // 括号不匹配，保持原样
                result.Append(code.Substring(i, j - i));
                i = j;
            }
        }
        
        return result.ToString();
    }

    /// <summary>
    /// 分割函数参数（考虑嵌套括号）
    /// </summary>
    private static List<string> SplitFunctionArgs(string args, int maxParts)
    {
        List<string> parts = new List<string>();
        StringBuilder current = new StringBuilder();
        int parenCount = 0;
        int bracketCount = 0;
        
        for (int i = 0; i < args.Length; i++)
        {
            char c = args[i];
            
            if (c == '(' || c == '[') { parenCount++; current.Append(c); }
            else if (c == ')' || c == ']') { parenCount--; current.Append(c); }
            else if (c == ',' && parenCount == 0 && parts.Count < maxParts - 1)
            {
                parts.Add(current.ToString().Trim());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        
        if (current.Length > 0)
            parts.Add(current.ToString().Trim());
        
        return parts;
    }

    /// <summary>
    /// 缩进代码
    /// </summary>
    private static string IndentCode(string code, string indent)
    {
        if (string.IsNullOrEmpty(code))
            return "";
        
        var lines = code.Split(new[] { '\n' }, StringSplitOptions.None);
        StringBuilder sb = new StringBuilder();
        
        foreach (var line in lines)
        {
            string trimmed = line.TrimEnd('\r');
            if (!string.IsNullOrWhiteSpace(trimmed))
            {
                sb.AppendLine(indent + trimmed.TrimStart());
            }
            else
            {
                sb.AppendLine();
            }
        }
        
        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// 验证生成的GLSL代码
    /// </summary>
    private static string ValidateAndCleanGLSL(string code)
    {
        // ========== 第一步：检查括号匹配 ==========
        int parenCount = 0;
        int braceCount = 0;
        int bracketCount = 0;
        bool inString = false;
        bool inComment = false;
        bool inLineComment = false;
        char prevChar = '\0';

        foreach (char c in code)
        {
            // 跳过字符串和注释中的括号
            if (c == '"' && prevChar != '\\')
            {
                inString = !inString;
            }
            else if (!inString && c == '/' && prevChar == '/')
            {
                inLineComment = true;
            }
            else if (!inString && c == '*' && prevChar == '/')
            {
                inComment = true;
            }
            else if (inComment && c == '/' && prevChar == '*')
            {
                inComment = false;
            }
            else if (inLineComment && c == '\n')
            {
                inLineComment = false;
            }
            else if (!inString && !inComment && !inLineComment)
            {
                switch (c)
                {
                    case '(': parenCount++; break;
                    case ')': parenCount--; break;
                    case '{': braceCount++; break;
                    case '}': braceCount--; break;
                    case '[': bracketCount++; break;
                    case ']': bracketCount--; break;
                }
            }

            prevChar = c;
        }

        // 报告括号问题
        if (parenCount != 0)
        {
            Debug.LogWarning($"LayaAir3D: GLSL code has unbalanced parentheses: {parenCount} (+ indicates extra '(', - indicates extra ')')");
        }
        if (braceCount != 0)
        {
            Debug.LogWarning($"LayaAir3D: GLSL code has unbalanced braces: {braceCount}");
        }
        if (bracketCount != 0)
        {
            Debug.LogWarning($"LayaAir3D: GLSL code has unbalanced brackets: {bracketCount}");
        }

        // ========== 第二步：检查常见的GLSL错误 ==========

        // 检查是否有Unity特有的函数未转换
        string[] unityFunctions = { "UnityObjectToClipPos", "UnityWorldToClipPos", "UNITY_MATRIX_", "tex2Dlod" };
        foreach (var func in unityFunctions)
        {
            if (code.Contains(func))
            {
                Debug.LogWarning($"LayaAir3D: Found Unity-specific function '{func}' in GLSL code - may need manual conversion");
            }
        }

        // 检查是否有HLSL类型未转换
        if (code.Contains("half") && !code.Contains("// half"))
        {
            Debug.LogWarning("LayaAir3D: Found HLSL 'half' type - should be converted to 'float' in GLSL");
            code = Regex.Replace(code, @"\bhalf\b", "float");
        }
        if (code.Contains("fixed") && !code.Contains("// fixed"))
        {
            Debug.LogWarning("LayaAir3D: Found HLSL 'fixed' type - should be converted to 'float' in GLSL");
            code = Regex.Replace(code, @"\bfixed\b", "float");
        }

        // 检查是否有错误的向量构造（如vec3(0.0)应该是vec3(0.0, 0.0, 0.0)）
        var singleArgVecs = Regex.Matches(code, @"\b(vec[234])\s*\(\s*([0-9.]+)\s*\)");
        if (singleArgVecs.Count > 0)
        {
            Debug.LogWarning($"LayaAir3D: Found {singleArgVecs.Count} single-argument vector constructor(s) - these are valid but may indicate hardcoded values");
        }

        // ========== 第三步：语法清理 ==========

        // ===== 修复texture2D参数类型错误 =====
        // texture2D的第二个参数应该是vec2，不应该被包装成vec4
        // 修复: texture2D(tex, vec4(...).xy) -> texture2D(tex, ...)
        code = Regex.Replace(code, @"texture2D\s*\(\s*([^,]+),\s*vec4\s*\(([^)]+)\)\s*\.xy\s*\)", "texture2D($1, $2)");

        // ===== 修复vec2 += vec4类型不匹配 =====
        // 修复: vec2变量 += texture2D(...) -> vec2变量 += texture2D(...).xy
        code = Regex.Replace(code, @"(\b\w+UV\b)\s*\+=\s*(texture2D\s*\([^)]+\))\s*\*", "$1 += $2.xy *");

        // ===== 修复错误的分号位置 =====
        // 修复多余的单独分号行（通常在逗号后）
        code = Regex.Replace(code, @",\s*\n\s*;\s*\n", ",\n");

        // 修复单独的分号行（if/else后面的孤立分号）
        code = Regex.Replace(code, @"(if|else|else\s+if)\s*\([^)]*\)\s*\n\s*;\s*\n", "$1($2)\n");

        // ===== 修复transpose(inverse(...))语法错误 - 通用方案 =====
        // 不限定特定矩阵变量名（如u_View），匹配所有这种模式

        // ⭐ STEP 1: 首先处理最复杂的模式 - 带有mat3包装的完整错误模式
        // 通用匹配: (mat3(transpose); (inverse(任何矩阵)) * xxx)
        code = Regex.Replace(code,
            @"\(\s*mat3\s*\(\s*transpose\s*\)\s*;\s*\(\s*inverse\s*\(\s*(\w+)\s*\)\s*\)\s*\*\s*([^)]+)\s*\)",
            "mat3(transpose(inverse($1))) * $2");

        // ⭐ STEP 2: 修复分号问题（通用）
        // 修复: transpose); (inverse -> transpose(inverse
        code = Regex.Replace(code, @"transpose\s*\)\s*;\s*\(\s*inverse", "transpose(inverse");

        // ⭐ STEP 3: 修复mat3嵌套调用的语法错误（通用）
        // 修复完整模式: (mat3(transpose(inverse(xxx)) * 应该是 mat3(transpose(inverse(xxx))) *
        code = Regex.Replace(code, @"\(\s*mat3\s*\(\s*transpose\s*\(\s*inverse\s*\(\s*([^)]+)\s*\)\s*\)\s*\*", "mat3(transpose(inverse($1))) *");

        // 修复没有前置括号的情况
        code = Regex.Replace(code, @"mat3\s*\(\s*transpose\s*\(\s*inverse\s*\(\s*([^)]+)\s*\)\s*\)\s*\*", "mat3(transpose(inverse($1))) *");

        // ⭐ STEP 4: 修复另一种模式: mat3(inverse(transpose(...))) 顺序错误（通用）
        code = Regex.Replace(code, @"mat3\s*\(\s*inverse\s*\(\s*transpose\s*\(\s*([^)]+)\s*\)\s*\)\s*\)", "mat3(transpose(inverse($1)))");

        // ⭐ STEP 5: 修复缺少mat3闭合括号的情况（通用）
        // 查找: mat3(transpose(inverse(xxx)) 后面直接跟 * 或 ) ，缺少一个闭合括号
        code = Regex.Replace(code, @"mat3\s*\(\s*transpose\s*\(\s*inverse\s*\(\s*([^)]+)\s*\)\s*\)\s*\)(?=\s*[*)])", "mat3(transpose(inverse($1)))");

        // ⭐ STEP 6: 修复遗留的复杂模式（通用）
        // 修复: (mat3(...); (...)) 带有分号和多余括号的情况
        code = Regex.Replace(code, @"\(\s*mat3\s*\([^;]+transpose[^;]+\)\s*;\s*\([^)]*inverse[^)]+\)\s*\*\s*(\w+)\s*\)\)",
            m => {
                // 提取inverse中的矩阵变量名
                var inverseMatch = Regex.Match(m.Value, @"inverse\s*\(\s*(\w+)\s*\)");
                // 提取最后的被乘变量名
                var varMatch = Regex.Match(m.Value, @"\*\s*(\w+)\s*\)");

                if (inverseMatch.Success && varMatch.Success)
                {
                    string matrixName = inverseMatch.Groups[1].Value;
                    string varName = varMatch.Groups[1].Value;
                    return $"mat3(transpose(inverse({matrixName}))) * {varName}";
                }

                return m.Value;
            });

        // ⭐ 修复mix函数的类型精确性
        // 当给vec3变量赋值时，mix的参数应该是vec3而不是标量
        // 模式: vec3 xxx = mix(0.0, 1.0, ...) 或 vec3 xxx = mix(0, 1, ...)
        code = Regex.Replace(code, @"(vec3\s+\w+\s*=\s*mix\s*\()\s*0\.0\s*,\s*1\.0\s*,", "$1vec3(0.0), vec3(1.0),");
        code = Regex.Replace(code, @"(vec3\s+\w+\s*=\s*mix\s*\()\s*0\s*,\s*1\s*,", "$1vec3(0.0), vec3(1.0),");

        // 处理已存在vec3变量的赋值
        // 收集所有vec3变量
        var vec3VarMatches = Regex.Matches(code, @"vec3\s+(\w+)");
        var vec3Vars = new HashSet<string>();
        foreach (Match m in vec3VarMatches)
        {
            vec3Vars.Add(m.Groups[1].Value);
        }

        // 修复这些vec3变量的mix赋值
        foreach (var varName in vec3Vars)
        {
            // 模式: vec3Var = mix(0.0, 1.0, ...)
            code = Regex.Replace(code, $@"({varName}\s*=\s*mix\s*\()\s*0\.0\s*,\s*1\.0\s*,", "$1vec3(0.0), vec3(1.0),");
            code = Regex.Replace(code, $@"({varName}\s*=\s*mix\s*\()\s*0\s*,\s*1\s*,", "$1vec3(0.0), vec3(1.0),");
        }

        // ⭐ 修复mat2构造函数 - GLSL使用列优先矩阵 [完全通用方案]
        // GLSL中mat2是列优先，对于2D旋转矩阵：
        // Unity HLSL风格（行优先）: mat2(a, -b, b, a)
        // GLSL风格（列优先）: mat2(a, b, -b, a)
        // 通用模式：mat2(变量1, -变量2, 变量2, 变量1) -> mat2(变量1, 变量2, -变量2, 变量1)

        // 匹配任意变量名的模式：mat2(X, -Y, Y, X) -> mat2(X, Y, -Y, X)
        code = Regex.Replace(code,
            @"mat2\s*\(\s*(\w+)\s*,\s*-(\w+)\s*,\s*\2\s*,\s*\1\s*\)",
            "mat2($1, $2, -$2, $1)");

        // 处理换行的情况
        code = Regex.Replace(code,
            @"mat2\s*\(\s*(\w+)\s*,\s*-(\w+)\s*,\s*\n\s*\2\s*,\s*\1\s*\)",
            "mat2($1, $2, -$2, $1)");

        // ⭐ 修复矩阵乘法顺序 - 完全通用方案，基于类型系统
        // GLSL中矩阵乘法: mat * vec，而不是 vec * mat
        // 通过类型推断识别矩阵变量，而不是依赖命名习惯

        // 步骤1: 收集所有矩阵类型变量（mat2, mat3, mat4）
        var matrixVarPattern = @"(mat[234])\s+(\w+)\s*=";
        var matrixVarMatches = Regex.Matches(code, matrixVarPattern);
        var matrixVars = new Dictionary<string, string>(); // 变量名 -> 类型
        foreach (Match m in matrixVarMatches)
        {
            string matType = m.Groups[1].Value;
            string varName = m.Groups[2].Value;
            if (!matrixVars.ContainsKey(varName))
            {
                matrixVars.Add(varName, matType);
            }
        }

        // 步骤2: 修复这些矩阵变量的乘法顺序
        foreach (var kvp in matrixVars)
        {
            string matVar = kvp.Key;
            // 匹配: (非矩阵变量 * 矩阵变量) -> (矩阵变量 * 非矩阵变量)
            // 确保第一个操作数不是矩阵类型
            code = Regex.Replace(code,
                $@"\((\w+)\s*\*\s*{Regex.Escape(matVar)}\)",
                m => {
                    string firstOperand = m.Groups[1].Value;
                    // 如果第一个操作数也是矩阵，不修改（矩阵与矩阵相乘顺序可能是正确的）
                    if (matrixVars.ContainsKey(firstOperand))
                    {
                        return m.Value;
                    }
                    // 否则，交换顺序
                    return $"({matVar} * {firstOperand})";
                });
        }

        // ⭐ 修复变量赋值后的条件修改顺序 - 完全通用方案
        // 检测模式：变量A = 变量B; 后跟 #ifdef ... 变量A *= 变量C; #endif
        // 这种模式应该改为：#ifdef ... 变量B *= 变量C; #endif 然后 变量A = 变量B;
        // 不依赖特定变量名，适用于所有这种模式

        // 通用模式：匹配 "变量A = 变量B;" 后面跟 "#ifdef ... 变量A *= 变量C; #endif"
        var postAssignModPattern = @"(\w+)\s*=\s*(\w+)\s*;\s*\n\s*(#ifdef\s+\w+\s*\n\s*(?:\/\/[^\n]*\n\s*)?\1\s*\*=\s*\w+\s*;\s*\n\s*#endif)";
        var postAssignMatches = Regex.Matches(code, postAssignModPattern, RegexOptions.Singleline);

        foreach (Match match in postAssignMatches)
        {
            string targetVar = match.Groups[1].Value;  // 被赋值的变量
            string sourceVar = match.Groups[2].Value;  // 源变量
            string conditionalBlock = match.Groups[3].Value;  // 条件修改块

            // 提取条件宏和修改变量
            var detailMatch = Regex.Match(conditionalBlock, $@"#ifdef\s+(\w+).*?{Regex.Escape(targetVar)}\s*\*=\s*(\w+)\s*;.*?#endif", RegexOptions.Singleline);
            if (detailMatch.Success)
            {
                string defineVar = detailMatch.Groups[1].Value;
                string modifierVar = detailMatch.Groups[2].Value;

                // 查找源变量的最后一次修改（在赋值之前）
                // 模式1：sourceVar.xxx = ...
                // 模式2：sourceVar *= += -= /= ...
                var lastModPatterns = new[] {
                    $@"({Regex.Escape(sourceVar)}\.[\w]+\s*=\s*[^;]+;)\s*\n\s*{Regex.Escape(targetVar)}\s*=\s*{Regex.Escape(sourceVar)}\s*;",
                    $@"({Regex.Escape(sourceVar)}\s*[*+/\-]=\s*[^;]+;)\s*\n\s*{Regex.Escape(targetVar)}\s*=\s*{Regex.Escape(sourceVar)}\s*;"
                };

                bool isFixed = false;
                foreach (var lastModPattern in lastModPatterns)
                {
                    var lastModMatch = Regex.Match(code, lastModPattern);
                    if (lastModMatch.Success)
                    {
                        // 在源变量最后修改后，目标赋值前插入条件修改
                        string insertion = $@"
#ifdef {defineVar}
        {sourceVar} *= {modifierVar};
#endif";

                        string replacement = lastModMatch.Groups[1].Value + insertion + $"\n        {targetVar} = {sourceVar};";
                        code = Regex.Replace(code, lastModPattern, replacement);

                        // 移除原来的后置条件编译块
                        code = code.Replace(conditionalBlock, "");

                        Debug.Log($"LayaAir3D: Fixed post-assignment conditional modification - {modifierVar} applied to {sourceVar} before {targetVar} assignment");
                        isFixed = true;
                        break;
                    }
                }

                // 如果找不到明确的最后修改，尝试直接在赋值前插入
                if (!isFixed)
                {
                    var simplePattern = $@"{Regex.Escape(targetVar)}\s*=\s*{Regex.Escape(sourceVar)}\s*;";
                    var simpleMatch = Regex.Match(code, simplePattern);
                    if (simpleMatch.Success)
                    {
                        string insertion = $@"#ifdef {defineVar}
        {sourceVar} *= {modifierVar};
#endif
        ";
                        string replacement = insertion + simpleMatch.Value;
                        code = code.Replace(simpleMatch.Value, replacement);

                        // 移除原来的后置条件编译块
                        code = code.Replace(conditionalBlock, "");

                        Debug.Log($"LayaAir3D: Fixed post-assignment conditional modification (simple) - {modifierVar} applied before {targetVar} = {sourceVar}");
                    }
                }
            }
        }

        // 移除多余的空语句
        code = Regex.Replace(code, @";\s*;", ";");

        // 移除空的if语句（if后只有分号）
        code = Regex.Replace(code, @"if\s*\([^)]*\)\s*;\s*\n", "");
        code = Regex.Replace(code, @"else\s+if\s*\([^)]*\)\s*;\s*\n", "");
        code = Regex.Replace(code, @"else\s*;\s*\n", "");

        // 修复三元运算符和vec2构造器换行导致的逗号分号问题
        // 匹配: 逗号后跟分号（错误的语法）
        code = Regex.Replace(code, @",\s*;\s*", ", ");

        // 修复函数调用被错误换行的情况
        // 例如: func(a,\n;\nb) -> func(a,\nb)
        code = Regex.Replace(code, @",\s*\n\s*;\s*\n\s*", ",\n    ");

        // 修复u_NormalMap_ST -> u_NormalTexture_ST
        code = Regex.Replace(code, @"\bu_NormalMap_ST\b", "u_NormalTexture_ST");

        // 移除残留的无效声明（如 vec2.0 o; 等）
        code = Regex.Replace(code, @"\b(vec2|vec3|vec4|float|int|mat2|mat3|mat4)\s*\.\s*\d+\s+\w+\s*;", "");

        // 移除空的struct声明
        code = Regex.Replace(code, @"struct\s+\w+\s*\{\s*\}\s*;?", "");
        
        // 注意：不要移除"重复"的varying声明！
        // VS和FS是两个独立的shader块，它们各自都需要声明相同的varying
        // 只在同一个shader块内移除重复声明
        
        // 分别处理VS和FS块内的重复varying
        code = RemoveDuplicateVaryingsInShaderBlocks(code);
        
        // ============================================
        // 修复语法问题
        // ============================================
        
        // ⭐ 修复缺少分号的语句（在行尾的赋值/声明后）
        // 注意：必须排除if/while/for等控制流语句中的条件表达式，否则会错误地在if(condition)后加分号
        // 匹配：行首的变量赋值（不在括号内）后没有分号，后面紧跟换行
        // 使用更精确的模式：不匹配括号内的等号（避免匹配if (x == 0)这种情况）
        // code = Regex.Replace(code, @"(\w+\s*=\s*[^;{}\n]+)(\s*\n\s*(?![\s{]))", "$1;$2");
        // ⭐ 注释掉上面的正则，因为它会错误地给if (condition)添加分号
        // 如果代码是从ParticleShaderTemplate生成的，应该已经有正确的分号
        
        // 修复括号位置错误：) 后面紧跟 ( 应该有操作符或分号
        // 例如：)(  可能是漏了分号或操作符
        code = Regex.Replace(code, @"\)\s*\((?!\s*\))", "); (");
        
        // 修复双前缀问题
        code = Regex.Replace(code, @"\bu_u_", "u_");
        
        // 修复 texture2D 等函数调用缺少分号
        code = Regex.Replace(code, @"(texture2D\s*\([^)]+\))\s*\n", "$1;\n");
        code = Regex.Replace(code, @"(textureCube\s*\([^)]+\))\s*\n", "$1;\n");
        
        // 清理多余的空行
        code = Regex.Replace(code, @"\n\s*\n\s*\n", "\n\n");
        
        // 修复可能的语法问题：移除行首的逗号
        code = Regex.Replace(code, @"^\s*,", "", RegexOptions.Multiline);
        
        // 移除空的uniform块
        code = Regex.Replace(code, @"uniform\s*\{\s*\}", "");
        
        // 移除多余的分号（连续分号）
        code = Regex.Replace(code, @";\s*;+", ";");
        
        // 修复 #endif 后面可能缺少换行
        code = Regex.Replace(code, @"(#endif)([^\s\n])", "$1\n$2");

        return code;
    }

    /// <summary>
    /// 格式化shader内容，提升可读性
    /// </summary>
    private static string FormatShaderContent(string content)
    {
        if (string.IsNullOrEmpty(content))
            return content;

        // 第一步：基础清理
        content = Regex.Replace(content, @"[ \t]+$", "", RegexOptions.Multiline); // 移除行尾空白
        content = Regex.Replace(content, @"\n{3,}", "\n\n"); // 最多保留一个空行

        // 第二步：分离Shader3D块和GLSL块进行格式化
        var parts = SplitShaderIntoParts(content);
        var formattedParts = new List<string>();

        foreach (var part in parts)
        {
            if (part.type == ShaderPartType.Shader3D)
            {
                formattedParts.Add(FormatShader3DBlock(part.content));
            }
            else if (part.type == ShaderPartType.GLSL)
            {
                formattedParts.Add(FormatGLSLBlock(part.content));
            }
            else
            {
                formattedParts.Add(part.content);
            }
        }

        // 合并并最终清理
        string result = string.Join("\n\n", formattedParts);
        result = Regex.Replace(result, @"\n{3,}", "\n\n"); // 再次清理多余空行
        result = result.TrimEnd() + "\n"; // 确保文件末尾只有一个换行

        return result;
    }

    /// <summary>
    /// 修复shader中的GLSL类型不匹配问题
    /// 主要解决v_Texcoord0 (vec4) 与 vec2 之间的类型转换问题
    /// </summary>
    private static string FixShaderTypeMismatch(string content)
    {
        if (string.IsNullOrEmpty(content))
            return content;

        // ⭐ 0. 修复错误的swizzle访问 (CRITICAL FIX)
        // vec2没有.z和.w分量，vec3没有.w分量，这是常见的GLSL类型错误
        // 例如：u_TilingOffset.xy.z -> u_TilingOffset.z
        // 这个修复必须在最前面，因为它修复的是根本性的类型错误

        // 修复 .xy.z 和 .xy.w (vec2错误访问z/w分量)
        content = Regex.Replace(content, @"(\w+)\.xy\.z\b", "$1.z");
        content = Regex.Replace(content, @"(\w+)\.xy\.w\b", "$1.w");

        // 修复 .xyz.w (vec3错误访问w分量)
        content = Regex.Replace(content, @"(\w+)\.xyz\.w\b", "$1.w");

        // 修复 .x.y / .x.z / .y.z 等单分量后再访问其他分量的错误
        content = Regex.Replace(content, @"(\w+)\.x\.y\b", "$1.y");
        content = Regex.Replace(content, @"(\w+)\.x\.z\b", "$1.z");
        content = Regex.Replace(content, @"(\w+)\.x\.w\b", "$1.w");
        content = Regex.Replace(content, @"(\w+)\.y\.z\b", "$1.z");
        content = Regex.Replace(content, @"(\w+)\.y\.w\b", "$1.w");
        content = Regex.Replace(content, @"(\w+)\.z\.w\b", "$1.w");

        Debug.Log("LayaAir3D: Applied swizzle access fix (vec2/vec3/vec4 invalid swizzle patterns)");

        // 1. 修复 texture2D() 调用中的 v_Texcoord0 -> v_Texcoord0.xy
        // 匹配: texture2D(texture_name, v_Texcoord0 ...)
        // 必须在texture2D的第二个参数位置
        content = Regex.Replace(
            content,
            @"\btexture2D\s*\(\s*([^,]+),\s*v_Texcoord0(?![.\w])",
            "texture2D($1, v_Texcoord0.xy",
            RegexOptions.Multiline
        );

        // 2. 修复 vec2(...) + v_Texcoord0 -> vec2(...) + v_Texcoord0.xy
        content = Regex.Replace(
            content,
            @"(\bvec2\s*\([^)]+\))\s*([+\-])\s*v_Texcoord0(?![.\w])",
            "$1 $2 v_Texcoord0.xy",
            RegexOptions.Multiline
        );

        // 3. 修复 v_Texcoord0 + (expr) -> v_Texcoord0.xy + (expr)
        // 匹配括号表达式，如 v_Texcoord0 + (u_Time * ...)
        content = Regex.Replace(
            content,
            @"v_Texcoord0(?![.\w])\s*([+\-])\s*\(",
            "v_Texcoord0.xy $1 (",
            RegexOptions.Multiline
        );

        // 4. 修复 v_Texcoord0 + expr（非括号） -> v_Texcoord0.xy + expr
        // 匹配到行尾或逗号、分号等结束符
        content = Regex.Replace(
            content,
            @"v_Texcoord0(?![.\w])\s*([+\-])\s*([^,;)\n]+)",
            "v_Texcoord0.xy $1 $2",
            RegexOptions.Multiline
        );

        // 5. 修复 texture2D 中的复杂表达式
        // texture2D(tex, expr + v_Texcoord0) 或 texture2D(tex, v_Texcoord0 + expr)
        // 这个规则确保texture2D的第二个参数中的v_Texcoord0都有.xy
        var lines = content.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];

            // 如果行中包含texture2D和v_Texcoord0（但不是v_Texcoord0.xy/.x/.y/.z/.w）
            if (line.Contains("texture2D") &&
                Regex.IsMatch(line, @"v_Texcoord0(?![.\w])"))
            {
                // 在texture2D的参数中，所有v_Texcoord0都应该是v_Texcoord0.xy
                // 匹配texture2D的第二个参数
                line = Regex.Replace(line,
                    @"(texture2D\s*\([^,]+,\s*)([^)]+)",
                    m => {
                        string prefix = m.Groups[1].Value;
                        string uvExpr = m.Groups[2].Value;
                        // 在UV表达式中，替换所有裸的v_Texcoord0为v_Texcoord0.xy
                        uvExpr = Regex.Replace(uvExpr, @"v_Texcoord0(?![.\w])", "v_Texcoord0.xy");
                        return prefix + uvExpr;
                    }
                );
                lines[i] = line;
            }

            // 处理其他包含v_Texcoord0算术运算的行
            if (Regex.IsMatch(line, @"v_Texcoord0(?![.\w])\s*[+\-]") ||
                Regex.IsMatch(line, @"[+\-]\s*v_Texcoord0(?![.\w])"))
            {
                // 检查是否是UV/坐标相关的计算（排除varying声明）
                if (!line.Contains("varying") &&
                    (line.Contains("texture2D") || line.Contains("UV") || line.Contains("uv") ||
                     line.Contains("*") || line.Contains("u_Time") || line.Contains("Scroll") ||
                     line.Contains("vec2")))
                {
                    // 替换所有算术运算中的v_Texcoord0
                    line = Regex.Replace(line, @"v_Texcoord0(?![.\w])(\s*[+\-])", "v_Texcoord0.xy$1");
                    line = Regex.Replace(line, @"([+\-]\s*)v_Texcoord0(?![.\w])", "$1v_Texcoord0.xy");
                    lines[i] = line;
                }
            }
        }
        content = string.Join("\n", lines);

        // 6. 修复嵌套的 vec2((expr + v_Texcoord0))
        content = Regex.Replace(
            content,
            @"vec2\s*\(\s*\(([^)]+\s*[+\-]\s*)v_Texcoord0(?![.\w])",
            "vec2(($1v_Texcoord0.xy",
            RegexOptions.Multiline
        );

        // 7. 修复 texture() 函数调用为 texture2D()
        // 在GLSL ES 2.0中，应该使用texture2D而不是texture
        content = Regex.Replace(
            content,
            @"\btexture\s*\(",
            "texture2D(",
            RegexOptions.Multiline
        );

        // 8. 最终清理：确保没有遗漏的v_Texcoord0在运算中
        // 再次扫描，替换任何在表达式中但没有.xy的v_Texcoord0
        content = Regex.Replace(
            content,
            @"([=+\-*/,\(]\s*)v_Texcoord0(?![.\w])(?=\s*[+\-*/,\)])",
            "$1v_Texcoord0.xy",
            RegexOptions.Multiline
        );

        // ⭐ 9. 修复vec4到vec2的赋值问题 (CRITICAL FIX FOR TYPE MISMATCH)
        // 检测并修复 "vec2 varName = vec4Value;" 这样的赋值
        // 需要自动添加 .xy 使其变成 "vec2 varName = vec4Value.xy;"
        content = FixVec4ToVec2Assignments(content);

        // ⭐ 10. 修复多余的vec2()构造函数
        // 例如: vec2(vec2_expression) -> vec2_expression
        content = RemoveRedundantVec2Constructors(content);

        // ⭐ 11. 修复函数参数类型不匹配
        // 检测函数调用中vec4传给vec2参数的情况
        content = FixFunctionParameterTypeMismatch(content);

        // ⭐ 12. 修复texture2D返回值在vec2算术运算中的类型不匹配 (CRITICAL)
        // 例如: vec2Var += texture2D(...) * strength
        // texture2D返回vec4，需要添加.xy或.rg
        content = FixTexture2DInVec2Operations(content);

        Debug.Log("LayaAir3D: Applied comprehensive type mismatch fixes");

        return content;
    }

    /// <summary>
    /// 修复vec4到vec2的赋值问题
    /// 例如: vec2 uv = v_Texcoord0; -> vec2 uv = v_Texcoord0.xy;
    /// </summary>
    private static string FixVec4ToVec2Assignments(string content)
    {
        if (string.IsNullOrEmpty(content))
            return content;

        // 模式1: vec2 varName = vec4Value;
        // 检测vec4类型的varying/uniform变量赋值给vec2
        var vec4Variables = new HashSet<string>();

        // 收集所有vec4类型的变量
        var vec4Matches = Regex.Matches(content, @"(?:varying|uniform|attribute)\s+vec4\s+(\w+)");
        foreach (Match match in vec4Matches)
        {
            vec4Variables.Add(match.Groups[1].Value);
        }

        // 也收集函数参数中的vec4
        var funcVec4Matches = Regex.Matches(content, @"(?:in|out|inout)\s+vec4\s+(\w+)");
        foreach (Match match in funcVec4Matches)
        {
            vec4Variables.Add(match.Groups[1].Value);
        }

        // 对每个vec4变量，在赋值给vec2时自动添加.xy
        foreach (var vec4Var in vec4Variables)
        {
            // 模式: vec2 xxx = vec4Var;
            content = Regex.Replace(
                content,
                $@"\bvec2\s+\w+\s*=\s*{vec4Var}(?![.\w])\s*;",
                m => m.Value.Replace($"{vec4Var};", $"{vec4Var}.xy;")
            );

            // 模式: someVec2 = vec4Var;
            content = Regex.Replace(
                content,
                $@"(\w+)\s*=\s*{vec4Var}(?![.\w])\s*;",
                m => {
                    // 检查左边的变量是否可能是vec2
                    string lhs = m.Groups[1].Value;
                    if (lhs.Contains("UV") || lhs.Contains("uv") || lhs.Contains("coord") || lhs.Contains("Coord"))
                    {
                        return m.Value.Replace($"{vec4Var};", $"{vec4Var}.xy;");
                    }
                    return m.Value;
                }
            );
        }

        return content;
    }

    /// <summary>
    /// 移除多余的vec2()构造函数
    /// 例如: vec2(vec2_expression) -> vec2_expression
    /// </summary>
    private static string RemoveRedundantVec2Constructors(string content)
    {
        if (string.IsNullOrEmpty(content))
            return content;

        // 检测并移除 vec2(vec2(...)) 这样的嵌套
        // 注意：只移除明显冗余的情况，保留有意义的类型转换

        // 模式: vec2(something.xy) 通常是多余的
        // 因为 .xy 已经返回 vec2 了
        content = Regex.Replace(
            content,
            @"vec2\s*\(\s*(\w+\.(xy|zw))\s*\)",
            "$1"
        );

        return content;
    }

    /// <summary>
    /// 修复函数参数类型不匹配
    /// 检测函数调用中vec4传给vec2参数的情况
    /// </summary>
    private static string FixFunctionParameterTypeMismatch(string content)
    {
        if (string.IsNullOrEmpty(content))
            return content;

        // 收集所有vec4类型的变量
        var vec4Variables = new HashSet<string>();
        var vec4Matches = Regex.Matches(content, @"(?:varying|uniform|attribute)\s+vec4\s+(\w+)");
        foreach (Match match in vec4Matches)
        {
            vec4Variables.Add(match.Groups[1].Value);
        }

        // 检测常见的需要vec2参数的函数
        var vec2Functions = new[] { "texture2D", "texture", "RotateUV", "TransformUV" };

        foreach (var func in vec2Functions)
        {
            foreach (var vec4Var in vec4Variables)
            {
                // 模式: function(tex, vec4Var) -> function(tex, vec4Var.xy)
                // 第二个参数通常是UV坐标，应该是vec2
                content = Regex.Replace(
                    content,
                    $@"({func}\s*\([^,]+,\s*){vec4Var}(?![.\w])",
                    $"$1{vec4Var}.xy"
                );
            }
        }

        return content;
    }

    /// <summary>
    /// 修复texture2D返回值在vec2算术运算中的类型不匹配
    /// 例如: vec2Var += texture2D(...) * strength -> vec2Var += texture2D(...).xy * strength
    /// </summary>
    private static string FixTexture2DInVec2Operations(string content)
    {
        if (string.IsNullOrEmpty(content))
            return content;

        // 收集所有vec2类型的变量
        var vec2Variables = new HashSet<string>();

        // 从声明中收集vec2变量
        var vec2Matches = Regex.Matches(content, @"vec2\s+(\w+)");
        foreach (Match match in vec2Matches)
        {
            vec2Variables.Add(match.Groups[1].Value);
        }

        // 从varying声明中收集vec2变量
        var varyingVec2Matches = Regex.Matches(content, @"varying\s+vec2\s+(\w+)");
        foreach (Match match in varyingVec2Matches)
        {
            vec2Variables.Add(match.Groups[1].Value);
        }

        Debug.Log($"LayaAir3D: Found {vec2Variables.Count} vec2 variables for texture2D fix");

        // 修复每个vec2变量的texture2D赋值
        // ⭐ 使用改进的方法：逐行处理，使用更宽松的匹配来处理嵌套括号
        var lines = content.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];

            foreach (var vec2Var in vec2Variables)
            {
                // 检测: vec2Var += texture2D(...) * 或 vec2Var = texture2D(...) *
                // 使用宽松匹配：从texture2D开始到行尾的分号
                if (Regex.IsMatch(line, $@"{vec2Var}\s*[+]?=\s*texture2D\s*\("))
                {
                    // 检查是否已经有.xy或.rg
                    if (!Regex.IsMatch(line, @"texture2D\s*\([^;]+\)\s*\.(xy|rg)"))
                    {
                        // 查找texture2D调用的结束位置（找到匹配的右括号）
                        int texture2DStart = line.IndexOf("texture2D");
                        if (texture2DStart >= 0)
                        {
                            int openParen = line.IndexOf('(', texture2DStart);
                            if (openParen >= 0)
                            {
                                int parenCount = 1;
                                int closeParen = openParen + 1;

                                // 找到匹配的右括号
                                while (closeParen < line.Length && parenCount > 0)
                                {
                                    if (line[closeParen] == '(') parenCount++;
                                    else if (line[closeParen] == ')') parenCount--;
                                    closeParen++;
                                }

                                if (parenCount == 0)
                                {
                                    // 找到了匹配的右括号，在后面插入.xy
                                    // 检查右括号后是否直接是.xy或.rg
                                    if (closeParen < line.Length && line[closeParen] != '.')
                                    {
                                        lines[i] = line.Insert(closeParen, ".xy");
                                        Debug.Log($"LayaAir3D: Fixed texture2D in line (with nested parens): {vec2Var} += texture2D(...).xy");
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        content = string.Join("\n", lines);

        Debug.Log("LayaAir3D: Applied texture2D in vec2 operations fix");

        return content;
    }

    /// <summary>
    /// 验证shader内容，检测潜在的类型不匹配问题
    /// 这个函数不修改内容，只输出警告
    /// </summary>
    private static void ValidateShaderContent(string content, string shaderName)
    {
        if (string.IsNullOrEmpty(content))
            return;

        var issues = new List<string>();

        // 检测1: 错误的swizzle访问
        if (Regex.IsMatch(content, @"\w+\.xy\.[zw]"))
        {
            var matches = Regex.Matches(content, @"(\w+\.xy\.[zw])");
            foreach (Match match in matches)
            {
                issues.Add($"Invalid swizzle access: {match.Value}");
            }
        }

        if (Regex.IsMatch(content, @"\w+\.[xyzw]\.[xyzw]"))
        {
            var matches = Regex.Matches(content, @"(\w+\.[xyzw]\.[xyzw])");
            foreach (Match match in matches)
            {
                issues.Add($"Invalid chained swizzle: {match.Value}");
            }
        }

        // 检测2: vec4变量可能未添加.xy的情况
        var vec4Variables = new HashSet<string>();
        var vec4Matches = Regex.Matches(content, @"(?:varying|uniform|attribute)\s+vec4\s+(\w+)");
        foreach (Match match in vec4Matches)
        {
            vec4Variables.Add(match.Groups[1].Value);
        }

        foreach (var vec4Var in vec4Variables)
        {
            // 检测: vec2 xxx = vec4Var; (没有.xy)
            if (Regex.IsMatch(content, $@"vec2\s+\w+\s*=\s*{vec4Var}(?![.\w])\s*;"))
            {
                issues.Add($"Possible type mismatch: vec2 assignment from {vec4Var} without .xy");
            }

            // 检测: texture2D(..., vec4Var) (没有.xy)
            if (Regex.IsMatch(content, $@"texture2D\s*\([^,]+,\s*{vec4Var}(?![.\w])\s*\)"))
            {
                issues.Add($"Possible type mismatch: texture2D UV parameter {vec4Var} without .xy");
            }
        }

        // 检测3: vec2构造函数接收vec4
        if (Regex.IsMatch(content, @"vec2\s*\(\s*vec4\s+"))
        {
            issues.Add("Possible issue: vec2() constructor with vec4 type");
        }

        // 检测4: texture2D在vec2算术运算中没有swizzle
        if (Regex.IsMatch(content, @"\w+\s*\+=\s*texture2D\s*\([^)]+\)(?!\.xy)(?!\.rg)\s*\*"))
        {
            var matches = Regex.Matches(content, @"(\w+\s*\+=\s*texture2D\s*\([^)]+\)(?!\.xy)(?!\.rg)\s*\*[^;]+;)");
            foreach (Match match in matches)
            {
                issues.Add($"Possible type mismatch: texture2D result in vec2 operation without .xy: {match.Value.Substring(0, Math.Min(60, match.Value.Length))}...");
            }
        }

        // 输出问题
        if (issues.Count > 0)
        {
            Debug.LogWarning($"LayaAir3D: Shader '{shaderName}' validation found {issues.Count} potential issue(s):");
            foreach (var issue in issues)
            {
                Debug.LogWarning($"  - {issue}");
            }
            Debug.LogWarning("  Note: These may have been auto-fixed. Check the exported shader if compilation fails.");
        }
        else
        {
            Debug.Log($"LayaAir3D: Shader '{shaderName}' validation passed (no obvious type mismatches detected)");
        }
    }

    /// <summary>
    /// ⭐ 全面的类型检查和自动修复
    /// 检测并修复所有赋值中的类型不匹配问题
    /// </summary>
    private static string ComprehensiveTypeCheck(string content)
    {
        if (string.IsNullOrEmpty(content))
            return content;

        // 第一步：收集所有变量及其类型
        var variableTypes = new Dictionary<string, string>();

        // 收集varying/uniform/attribute声明
        var declMatches = Regex.Matches(content, @"(varying|uniform|attribute)\s+(vec2|vec3|vec4|float|int|mat2|mat3|mat4|sampler2D|samplerCube)\s+(\w+)");
        foreach (Match match in declMatches)
        {
            string varType = match.Groups[2].Value;
            string varName = match.Groups[3].Value;
            variableTypes[varName] = varType;
        }

        // 收集局部变量声明
        var localVarMatches = Regex.Matches(content, @"^\s*(vec2|vec3|vec4|float|int|mat2|mat3|mat4)\s+(\w+)\s*[=;]", RegexOptions.Multiline);
        foreach (Match match in localVarMatches)
        {
            string varType = match.Groups[1].Value;
            string varName = match.Groups[2].Value;
            if (!variableTypes.ContainsKey(varName))
            {
                variableTypes[varName] = varType;
            }
        }

        Debug.Log($"LayaAir3D: ComprehensiveTypeCheck - Found {variableTypes.Count} variables");

        int fixCount = 0;

        // 第二步：修复vec4到vec2/vec3的赋值
        foreach (var kvp in variableTypes)
        {
            string varName = kvp.Key;
            string varType = kvp.Value;

            // 情况1: vec2 = vec4变量 (没有swizzle)
            if (varType == "vec4")
            {
                // vec2 xxx = vec4Var;
                var pattern1 = $@"(vec2\s+\w+\s*=\s*)({varName})(?![.\w])(\s*;)";
                if (Regex.IsMatch(content, pattern1))
                {
                    content = Regex.Replace(content, pattern1, "$1$2.xy$3");
                    fixCount++;
                    Debug.Log($"LayaAir3D: Fixed vec2 = {varName} → vec2 = {varName}.xy");
                }

                // vec2Var = vec4Var;
                foreach (var targetVar in variableTypes)
                {
                    if (targetVar.Value == "vec2")
                    {
                        var pattern2 = $@"({targetVar.Key}\s*=\s*)({varName})(?![.\w])(\s*;)";
                        if (Regex.IsMatch(content, pattern2))
                        {
                            content = Regex.Replace(content, pattern2, "$1$2.xy$3");
                            fixCount++;
                            Debug.Log($"LayaAir3D: Fixed {targetVar.Key} = {varName} → {targetVar.Key} = {varName}.xy");
                        }
                    }
                    else if (targetVar.Value == "vec3")
                    {
                        var pattern3 = $@"({targetVar.Key}\s*=\s*)({varName})(?![.\w])(\s*;)";
                        if (Regex.IsMatch(content, pattern3))
                        {
                            content = Regex.Replace(content, pattern3, "$1$2.xyz$3");
                            fixCount++;
                            Debug.Log($"LayaAir3D: Fixed {targetVar.Key} = {varName} → {targetVar.Key} = {varName}.xyz");
                        }
                    }
                }
            }

            // 情况2: vec3 = vec4变量
            if (varType == "vec4")
            {
                // vec3 xxx = vec4Var;
                var pattern4 = $@"(vec3\s+\w+\s*=\s*)({varName})(?![.\w])(\s*;)";
                if (Regex.IsMatch(content, pattern4))
                {
                    content = Regex.Replace(content, pattern4, "$1$2.xyz$3");
                    fixCount++;
                    Debug.Log($"LayaAir3D: Fixed vec3 = {varName} → vec3 = {varName}.xyz");
                }
            }

            // 情况3: vec3 = vec2变量 (需要扩展)
            if (varType == "vec2")
            {
                foreach (var targetVar in variableTypes)
                {
                    if (targetVar.Value == "vec3")
                    {
                        var pattern5 = $@"({targetVar.Key}\s*=\s*)({varName})(?![.\w])(\s*;)";
                        if (Regex.IsMatch(content, pattern5))
                        {
                            // vec3不能直接从vec2转换，需要用户手动修复，这里只记录
                            Debug.LogWarning($"LayaAir3D: Type mismatch detected: {targetVar.Key} (vec3) = {varName} (vec2) - Manual fix may be needed");
                        }
                    }
                    else if (targetVar.Value == "vec4")
                    {
                        var pattern6 = $@"({targetVar.Key}\s*=\s*)({varName})(?![.\w])(\s*;)";
                        if (Regex.IsMatch(content, pattern6))
                        {
                            // vec4不能直接从vec2转换，需要用户手动修复
                            Debug.LogWarning($"LayaAir3D: Type mismatch detected: {targetVar.Key} (vec4) = {varName} (vec2) - Manual fix may be needed");
                        }
                    }
                }
            }
        }

        // 第三步：修复texture2D作为UV参数（应该是vec2）
        foreach (var kvp in variableTypes)
        {
            if (kvp.Value == "vec4")
            {
                // texture2D(sampler, vec4Var) → texture2D(sampler, vec4Var.xy)
                var pattern7 = $@"(texture2D\s*\([^,]+,\s*)({kvp.Key})(?![.\w])(\s*\))";
                if (Regex.IsMatch(content, pattern7))
                {
                    content = Regex.Replace(content, pattern7, "$1$2.xy$3");
                    fixCount++;
                    Debug.Log($"LayaAir3D: Fixed texture2D UV parameter: {kvp.Key} → {kvp.Key}.xy");
                }
            }
        }

        // 第四步：修复函数参数中的类型不匹配
        // 常见函数签名：fract(vec2), mix(vec3, vec3, float), etc.
        var commonVec2Functions = new[] { "fract", "floor", "ceil", "abs", "normalize", "length" };
        foreach (var func in commonVec2Functions)
        {
            foreach (var kvp in variableTypes)
            {
                if (kvp.Value == "vec4")
                {
                    // func(vec4Var) where func expects vec2
                    var pattern8 = $@"({func}\s*\()({kvp.Key})(?![.\w])(\s*\))";
                    var matches = Regex.Matches(content, pattern8);
                    if (matches.Count > 0)
                    {
                        // 这个需要上下文判断，先记录
                        Debug.LogWarning($"LayaAir3D: Potential type mismatch in {func}({kvp.Key}) - vec4 may need swizzle");
                    }
                }
            }
        }

        // 第五步：修复构造函数中的类型不匹配
        // vec2(vec4Var) → vec2(vec4Var.xy)
        foreach (var kvp in variableTypes)
        {
            if (kvp.Value == "vec4")
            {
                var pattern9 = $@"(vec2\s*\()({kvp.Key})(?![.\w])(\s*\))";
                if (Regex.IsMatch(content, pattern9))
                {
                    content = Regex.Replace(content, pattern9, "$1$2.xy$3");
                    fixCount++;
                    Debug.Log($"LayaAir3D: Fixed vec2 constructor: vec2({kvp.Key}) → vec2({kvp.Key}.xy)");
                }

                var pattern10 = $@"(vec3\s*\()({kvp.Key})(?![.\w])(\s*\))";
                if (Regex.IsMatch(content, pattern10))
                {
                    content = Regex.Replace(content, pattern10, "$1$2.xyz$3");
                    fixCount++;
                    Debug.Log($"LayaAir3D: Fixed vec3 constructor: vec3({kvp.Key}) → vec3({kvp.Key}.xyz)");
                }
            }
            else if (kvp.Value == "vec3")
            {
                var pattern11 = $@"(vec2\s*\()({kvp.Key})(?![.\w])(\s*\))";
                if (Regex.IsMatch(content, pattern11))
                {
                    content = Regex.Replace(content, pattern11, "$1$2.xy$3");
                    fixCount++;
                    Debug.Log($"LayaAir3D: Fixed vec2 constructor: vec2({kvp.Key}) → vec2({kvp.Key}.xy)");
                }
            }
        }

        // 第六步：修复texture2D在复杂表达式中的类型不匹配
        // 模式: vec2Var += texture2D(...) * scalar (没有.xy)
        // 这个比FixTexture2DInVec2Operations更强大，能处理复杂的嵌套括号
        var vec2Vars = variableTypes.Where(kvp => kvp.Value == "vec2").Select(kvp => kvp.Key).ToList();

        foreach (var vec2Var in vec2Vars)
        {
            // 使用更宽松的正则，匹配texture2D(...) * 但没有.xy/.rg的情况
            // 查找: vec2Var += texture2D(任意内容) * 任意内容;
            // 确保texture2D后面不是 .xy 或 .rg
            var pattern12 = $@"({vec2Var}\s*\+=\s*texture2D\s*\([^;]*?\))(?!\.xy)(?!\.rg)(\s*\*\s*[^;]+;)";
            var matches = Regex.Matches(content, pattern12);

            if (matches.Count > 0)
            {
                foreach (Match match in matches)
                {
                    string original = match.Value;
                    // 在texture2D的右括号后插入.xy
                    string fixed_str = match.Groups[1].Value + ".xy" + match.Groups[2].Value;
                    content = content.Replace(original, fixed_str);
                    fixCount++;
                    Debug.Log($"LayaAir3D: Fixed texture2D in complex expression: {vec2Var} += texture2D(...).xy * ...");
                }
            }

            // 同样处理 = 赋值
            var pattern13 = $@"({vec2Var}\s*=\s*texture2D\s*\([^;]*?\))(?!\.xy)(?!\.rg)(\s*\*\s*[^;]+;)";
            matches = Regex.Matches(content, pattern13);

            if (matches.Count > 0)
            {
                foreach (Match match in matches)
                {
                    string original = match.Value;
                    string fixed_str = match.Groups[1].Value + ".xy" + match.Groups[2].Value;
                    content = content.Replace(original, fixed_str);
                    fixCount++;
                    Debug.Log($"LayaAir3D: Fixed texture2D in complex expression: {vec2Var} = texture2D(...).xy * ...");
                }
            }
        }

        // 第七步：检测可疑的变量名使用
        // 如果v_ScreenPos存在，但代码中使用了v_Texcoord5.xy / v_Texcoord5.w的模式，可能是错误
        if (variableTypes.ContainsKey("v_ScreenPos") && variableTypes.ContainsKey("v_Texcoord5"))
        {
            // 检测: xxx = v_Texcoord5.xy / v_Texcoord5.w
            if (Regex.IsMatch(content, @"\w+\s*=\s*v_Texcoord5\.xy\s*/\s*v_Texcoord5\.w"))
            {
                Debug.LogWarning($"LayaAir3D: Suspicious pattern detected: 'v_Texcoord5.xy / v_Texcoord5.w' - Should this be 'v_ScreenPos.xy / v_ScreenPos.w'?");

                // 自动修复：如果是计算screenUV的模式，替换为v_ScreenPos
                var pattern14 = @"(vec2\s+screenUV\s*=\s*)v_Texcoord5(\.xy\s*/\s*)v_Texcoord5(\.w\s*;)";
                if (Regex.IsMatch(content, pattern14))
                {
                    content = Regex.Replace(content, pattern14, "$1v_ScreenPos$2v_ScreenPos$3");
                    fixCount++;
                    Debug.Log($"LayaAir3D: Fixed screenUV calculation: v_Texcoord5 → v_ScreenPos");
                }
            }
        }

        if (fixCount > 0)
        {
            Debug.Log($"LayaAir3D: ComprehensiveTypeCheck applied {fixCount} automatic type fixes");
        }
        else
        {
            Debug.Log($"LayaAir3D: ComprehensiveTypeCheck - No type fixes needed");
        }

        return content;
    }

    enum ShaderPartType { Header, Shader3D, GLSL }

    class ShaderPart
    {
        public ShaderPartType type;
        public string content;
    }

    /// <summary>
    /// 分离shader为不同的部分
    /// </summary>
    private static List<ShaderPart> SplitShaderIntoParts(string content)
    {
        var parts = new List<ShaderPart>();
        var lines = content.Split('\n');
        var currentPart = new StringBuilder();
        ShaderPartType currentType = ShaderPartType.Header;

        foreach (var line in lines)
        {
            string trimmed = line.Trim();

            if (trimmed == "Shader3D Start")
            {
                if (currentPart.Length > 0)
                {
                    parts.Add(new ShaderPart { type = currentType, content = currentPart.ToString().Trim() });
                    currentPart.Clear();
                }
                currentType = ShaderPartType.Shader3D;
                currentPart.AppendLine(line);
            }
            else if (trimmed == "Shader3D End")
            {
                currentPart.AppendLine(line);
                parts.Add(new ShaderPart { type = currentType, content = currentPart.ToString().Trim() });
                currentPart.Clear();
                currentType = ShaderPartType.Header;
            }
            else if (trimmed == "GLSL Start")
            {
                if (currentPart.Length > 0)
                {
                    parts.Add(new ShaderPart { type = currentType, content = currentPart.ToString().Trim() });
                    currentPart.Clear();
                }
                currentType = ShaderPartType.GLSL;
                currentPart.AppendLine(line);
            }
            else if (trimmed == "GLSL End")
            {
                currentPart.AppendLine(line);
                parts.Add(new ShaderPart { type = currentType, content = currentPart.ToString().Trim() });
                currentPart.Clear();
                currentType = ShaderPartType.Header;
            }
            else
            {
                currentPart.AppendLine(line);
            }
        }

        if (currentPart.Length > 0)
        {
            parts.Add(new ShaderPart { type = currentType, content = currentPart.ToString().Trim() });
        }

        return parts;
    }

    /// <summary>
    /// 格式化Shader3D配置块
    /// </summary>
    private static string FormatShader3DBlock(string content)
    {
        var lines = content.Split('\n');
        var result = new StringBuilder();
        int indentLevel = 0;

        foreach (var line in lines)
        {
            string trimmed = line.Trim();

            if (string.IsNullOrWhiteSpace(trimmed))
                continue;

            if (trimmed == "Shader3D Start" || trimmed == "Shader3D End")
            {
                result.AppendLine(trimmed);
            }
            else if (trimmed == "{")
            {
                result.AppendLine(trimmed);
                indentLevel++;
            }
            else if (trimmed == "}" || trimmed == "},")
            {
                indentLevel = Math.Max(0, indentLevel - 1); // 确保不会变成负数
                result.AppendLine(new string(' ', indentLevel * 4) + trimmed);
            }
            else
            {
                result.AppendLine(new string(' ', Math.Max(0, indentLevel) * 4) + trimmed); // 确保不会变成负数
            }
        }

        return result.ToString().TrimEnd();
    }

    /// <summary>
    /// 格式化GLSL代码块
    /// </summary>
    private static string FormatGLSLBlock(string content)
    {
        var lines = content.Split('\n');
        var result = new StringBuilder();
        int indentLevel = 0;
        string previousLine = "";
        bool needsBlankLine = false;

        foreach (var line in lines)
        {
            string trimmed = line.Trim();

            if (string.IsNullOrWhiteSpace(trimmed))
            {
                // 保留空行，但不连续
                if (!string.IsNullOrWhiteSpace(previousLine))
                {
                    result.AppendLine();
                    previousLine = "";
                }
                continue;
            }

            // GLSL Start/End 顶格
            if (trimmed == "GLSL Start" || trimmed == "GLSL End")
            {
                if (needsBlankLine && result.Length > 0)
                    result.AppendLine();
                result.AppendLine(trimmed);
                previousLine = trimmed;
                needsBlankLine = false;
                continue;
            }

            // 预处理指令顶格
            if (trimmed.StartsWith("#"))
            {
                // 在#define SHADER_NAME之前加空行（新函数开始）
                if (trimmed.StartsWith("#define SHADER_NAME") && !string.IsNullOrWhiteSpace(previousLine))
                {
                    result.AppendLine();
                }
                result.AppendLine(trimmed);
                previousLine = trimmed;
                needsBlankLine = false;
                continue;
            }

            // 计算缩进
            if (trimmed.EndsWith("{"))
            {
                result.AppendLine(new string(' ', Math.Max(0, indentLevel) * 4) + trimmed);
                indentLevel++;
                needsBlankLine = false;
            }
            else if (trimmed.StartsWith("}"))
            {
                indentLevel = Math.Max(0, indentLevel - 1); // 确保不会变成负数
                result.AppendLine(new string(' ', indentLevel * 4) + trimmed);
                needsBlankLine = true; // 函数结束后需要空行
            }
            else
            {
                result.AppendLine(new string(' ', Math.Max(0, indentLevel) * 4) + trimmed);
                needsBlankLine = false;
            }

            previousLine = trimmed;
        }

        return result.ToString().TrimEnd();
    }
    
    /// <summary>
    /// 在每个shader块内移除重复的varying声明
    /// VS和FS是独立的块，各自需要声明相同的varying
    /// </summary>
    private static string RemoveDuplicateVaryingsInShaderBlocks(string code)
    {
        // 匹配 #defineGLSL ... #endGLSL 块
        return Regex.Replace(code, @"(#defineGLSL\s+\w+)([\s\S]*?)(#endGLSL)", (match) =>
        {
            string header = match.Groups[1].Value;
            string body = match.Groups[2].Value;
            string footer = match.Groups[3].Value;
            
            // 在这个块内移除重复的varying声明
            HashSet<string> seenVaryings = new HashSet<string>();
            body = Regex.Replace(body, @"varying\s+(vec2|vec3|vec4|float|int|mat2|mat3|mat4)\s+(\w+)\s*;", (varyingMatch) =>
            {
                string varyingName = varyingMatch.Groups[2].Value;
                if (seenVaryings.Contains(varyingName))
                {
                    return ""; // 移除块内重复的声明
                }
                else
                {
                    seenVaryings.Add(varyingName);
                    return varyingMatch.Value; // 保留第一个
                }
            });
            
            return header + body + footer;
        });
    }

    #endregion

    /// <summary>
    /// Shader属性信息
    /// </summary>
    private class ShaderProperty
    {
        public string unityName;
        public string layaName;
        public ShaderUtil.ShaderPropertyType type;
        public string define;  // 纹理关联的宏定义
        public bool isNormal;  // 是否是法线贴图
        public bool isCubemap; // 是否是Cubemap
        public float rangeMin; // Range类型的最小值
        public float rangeMax; // Range类型的最大值
        public float defaultFloat; // Float默认值
        public Vector4 defaultVector; // Vector默认值
        public Color defaultColor; // Color默认值
        public string description; // 属性描述
    }

    /// <summary>
    /// 收集Shader属性
    /// </summary>
    private static List<ShaderProperty> CollectShaderProperties(Shader shader)
    {
        List<ShaderProperty> properties = new List<ShaderProperty>();
        int propertyCount = ShaderUtil.GetPropertyCount(shader);
        
        for (int i = 0; i < propertyCount; i++)
        {
            string propName = ShaderUtil.GetPropertyName(shader, i);
            ShaderUtil.ShaderPropertyType propType = ShaderUtil.GetPropertyType(shader, i);
            
            // 跳过内部属性
            if (IsInternalProperty(propName))
                continue;
            
            ShaderProperty prop = new ShaderProperty();
            prop.unityName = propName;
            prop.layaName = ConvertToLayaPropertyName(propName);
            prop.type = propType;
            prop.description = ShaderUtil.GetPropertyDescription(shader, i);
            prop.isNormal = propName.ToLower().Contains("normal") || propName.ToLower().Contains("bump");
            prop.isCubemap = propName.ToLower().Contains("ibl") || propName.ToLower().Contains("cube") || 
                            prop.description.ToLower().Contains("cube");
            
            // 获取Range属性的范围
            if (propType == ShaderUtil.ShaderPropertyType.Range)
            {
                prop.rangeMin = ShaderUtil.GetRangeLimits(shader, i, 1);
                prop.rangeMax = ShaderUtil.GetRangeLimits(shader, i, 2);
            }
            
            // 纹理属性生成宏定义
            if (propType == ShaderUtil.ShaderPropertyType.TexEnv)
            {
                prop.define = GenerateTextureDefine(propName);
                
                // 检查纹理维度判断是否是Cubemap
                var texDim = ShaderUtil.GetTexDim(shader, i);
                if (texDim == UnityEngine.Rendering.TextureDimension.Cube)
                {
                    prop.isCubemap = true;
                }
            }
            
            properties.Add(prop);
        }
        
        return properties;
    }

    /// <summary>
    /// 检查是否是内部属性（渲染状态相关，不需要导出）
    /// </summary>
    private static bool IsInternalProperty(string propName)
    {
        // 渲染状态属性
        if (propName.StartsWith("_Src") || propName.StartsWith("_Dst"))
            return true;
            
        // 常见内部属性列表
        string[] internalProps = new string[]
        {
            "_ZWrite", "_ZWriteAnimatable", "_ZTest", "_Cull", "_CullMode",
            "_Mode", "_Surface", "_SurfaceType", "_Blend", "_BlendMode",
            "_AlphaClip", "_QueueOffset", "_RenderQueueOffset",
            "_Foldout", "_FoldoutSurface", "_FoldoutSurfaceEnd", "_FoldoutAdvance",
            "_SplitLine", "_Header", "_Space"
        };
        
        foreach (var internalProp in internalProps)
        {
            if (propName == internalProp || propName.StartsWith("_Foldout"))
                return true;
        }
        
        return false;
    }

    /// <summary>
    /// 转换为LayaAir属性名
    /// </summary>
    private static string ConvertToLayaPropertyName(string unityName)
    {
        // 使用预定义映射表
        if (PropertyNameMappings.ContainsKey(unityName))
        {
            return PropertyNameMappings[unityName];
        }
        
        // TilingOffset特殊处理
        if (unityName.EndsWith("_ST"))
        {
            return "u_TilingOffset";
        }
        
        // 默认转换：移除下划线前缀，添加u_前缀
        string name = unityName.TrimStart('_');
        return "u_" + name;
    }

    /// <summary>
    /// 生成纹理宏定义
    /// </summary>
    private static string GenerateTextureDefine(string propName)
    {
        // 使用预定义映射表
        if (TextureDefineMappings.ContainsKey(propName))
        {
            return TextureDefineMappings[propName];
        }
        
        // 默认：转换为大写并添加后缀
        string name = propName.TrimStart('_').ToUpper();
        // 如果不以MAP/TEXTURE结尾，添加MAP后缀
        if (!name.EndsWith("MAP") && !name.EndsWith("TEXTURE") && !name.EndsWith("TEX"))
        {
            name += "MAP";
        }
        return name;
    }

    /// <summary>
    /// 将Unity Keyword转换为Laya Define
    /// 规则：去掉前缀 _ 和后缀 _ON
    /// 示例：_LAYERTYPE_THREE → LAYERTYPE_THREE, _USEDISTORT0_ON → USEDISTORT0
    /// </summary>
    private static string ConvertKeywordToDefine(string unityKeyword)
    {
        if (string.IsNullOrEmpty(unityKeyword))
            return null;

        // 去掉前缀 _
        string define = unityKeyword.TrimStart('_');

        // 去掉后缀 _ON（如果有）
        if (define.EndsWith("_ON"))
        {
            define = define.Substring(0, define.Length - 3);
        }

        // 如果结果为空，返回null
        if (string.IsNullOrEmpty(define))
            return null;

        return define;
    }

    /// <summary>
    /// 检测Shader是否包含NPR特性（卡通渲染）
    /// </summary>
    private static bool HasNPRFeatures(List<ShaderProperty> properties)
    {
        foreach (var prop in properties)
        {
            // 检测NPR相关属性
            if (prop.unityName.Contains("Med") || prop.unityName.Contains("Shadow") ||
                prop.unityName.Contains("Reflect") || prop.unityName.Contains("GI") ||
                prop.unityName.Contains("Stylized") || prop.unityName.Contains("Toon"))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 检测Shader是否包含IBL特性
    /// </summary>
    private static bool HasIBLFeatures(List<ShaderProperty> properties)
    {
        foreach (var prop in properties)
        {
            if (prop.unityName.Contains("IBL"))
                return true;
        }
        return false;
    }

    /// <summary>
    /// 检测Shader是否包含Matcap特性
    /// </summary>
    private static bool HasMatcapFeatures(List<ShaderProperty> properties)
    {
        foreach (var prop in properties)
        {
            if (prop.unityName.Contains("Matcap"))
                return true;
        }
        return false;
    }

    /// <summary>
    /// 检测Shader是否包含Fresnel特性
    /// </summary>
    private static bool HasFresnelFeatures(List<ShaderProperty> properties)
    {
        foreach (var prop in properties)
        {
            if (prop.unityName.Contains("Fresnel") || prop.unityName.Contains("fresnel"))
                return true;
        }
        return false;
    }

    /// <summary>
    /// 检测Shader是否包含Rim边缘光特性
    /// </summary>
    private static bool HasRimFeatures(List<ShaderProperty> properties)
    {
        foreach (var prop in properties)
        {
            if (prop.unityName.Contains("Rim") && !prop.unityName.Contains("Remap"))
                return true;
        }
        return false;
    }

    /// <summary>
    /// 检测Shader是否包含HSV调整特性
    /// </summary>
    private static bool HasHSVFeatures(List<ShaderProperty> properties)
    {
        foreach (var prop in properties)
        {
            if (prop.unityName.Contains("HSV") || prop.unityName.Contains("Hue") ||
                prop.unityName.Contains("Saturation"))
                return true;
        }
        return false;
    }

    /// <summary>
    /// 检测Shader是否包含Tonemapping特性
    /// </summary>
    private static bool HasTonemappingFeatures(List<ShaderProperty> properties)
    {
        foreach (var prop in properties)
        {
            if (prop.unityName.Contains("Tone") || prop.unityName.Contains("WhitePoint"))
                return true;
        }
        return false;
    }

    /// <summary>
    /// 根据Unity Shader名称检测LayaAir材质类型
    /// 参考 MetarialPropData.json 中的映射关系
    /// 
    /// 注意：这里只检测"内置"或"简单"的材质类型
    /// 复杂的自定义Shader（如Artist_Effect等）会被归类为Custom
    /// </summary>
    private static LayaMaterialType DetectMaterialType(string shaderName)
    {
        string lowerName = shaderName.ToLower();
        
        // ============================================
        // 1. 首先检测是否是LayaAir内置的简单粒子材质
        // 这些是MetarialPropData.json中定义的标准粒子Shader
        // ============================================
        
        // Laya内置粒子/特效材质 - 这些是简单的粒子材质
        if (lowerName == "laya/legacy/particle" ||
            lowerName == "laya/legacy/effect" ||
            lowerName == "laya/legacy/trail")
        {
            return LayaMaterialType.PARTICLESHURIKEN;
        }
        
        // Unity内置粒子材质 - 这些也是简单的粒子材质
        // 注意：必须精确匹配或以"particles/"开头
        if (lowerName.StartsWith("particles/") ||
            lowerName.StartsWith("mobile/particles/") ||
            lowerName.StartsWith("legacy shaders/particles/") ||
            lowerName.StartsWith("universal render pipeline/particles/"))
        {
            return LayaMaterialType.PARTICLESHURIKEN;
        }
        
        // ============================================
        // 2. 天空盒材质
        // ============================================
        if (lowerName.StartsWith("skybox/") || lowerName == "skybox")
        {
            if (lowerName.Contains("procedural"))
                return LayaMaterialType.SkyProcedural;
            if (lowerName.Contains("panoramic"))
                return LayaMaterialType.SkyPanoramic;
            return LayaMaterialType.SkyBox;
        }
        
        // Laya内置天空盒
        if (lowerName == "laya/legacy/sky box")
        {
            return LayaMaterialType.SkyBox;
        }
        
        // ============================================
        // 3. Unlit材质
        // ============================================
        if (lowerName.StartsWith("unlit/") || 
            lowerName == "laya/unlit" ||
            lowerName.StartsWith("universal render pipeline/unlit"))
        {
            return LayaMaterialType.Unlit;
        }
        
        // ============================================
        // 4. BlinnPhong材质
        // ============================================
        if (lowerName == "laya/blinnphong" ||
            lowerName.StartsWith("legacy shaders/diffuse") ||
            lowerName.StartsWith("legacy shaders/bumped") ||
            lowerName.StartsWith("mobile/diffuse") ||
            lowerName.StartsWith("mobile/bumped") ||
            lowerName.StartsWith("mobile/vertexlit"))
        {
            return LayaMaterialType.BLINNPHONG;
        }
        
        // ============================================
        // 5. PBR材质 - Standard, URP/Lit, HDRP/Lit等
        // ============================================
        if (lowerName == "standard" || 
            lowerName == "standard (specular setup)" ||
            lowerName == "universal render pipeline/lit" ||
            lowerName == "hdrp/lit")
        {
            return LayaMaterialType.PBR;
        }
        
        // ============================================
        // 6. 其他所有自定义Shader
        // 包括复杂的特效Shader（如Artist_Effect等）
        // 这些需要完整的自定义转换
        // ============================================
        return LayaMaterialType.Custom;
    }

    /// <summary>
    /// 根据材质类型确定ShaderType
    /// </summary>
    private static LayaShaderType GetShaderTypeFromMaterialType(LayaMaterialType materialType)
    {
        switch (materialType)
        {
            case LayaMaterialType.PARTICLESHURIKEN:
                return LayaShaderType.Effect;  // 粒子使用Effect类型
            case LayaMaterialType.SkyBox:
            case LayaMaterialType.SkyProcedural:
            case LayaMaterialType.SkyPanoramic:
                return LayaShaderType.Sky;     // 天空盒使用Sky类型
            case LayaMaterialType.PBR:
            case LayaMaterialType.BLINNPHONG:
            case LayaMaterialType.Unlit:
                return LayaShaderType.D3;      // 标准3D材质使用D3类型
            case LayaMaterialType.Custom:
            default:
                return LayaShaderType.D3;      // 自定义材质默认使用D3类型
        }
    }

    /// <summary>
    /// 根据Shader名称和属性进一步确定自定义Shader的ShaderType
    /// 用于Custom类型的Shader
    ///
    /// ⭐ 重要：只有真正的粒子系统shader才应该使用LayaShaderType.Effect
    /// Mesh特效shader（虽然名称包含effect但用于MeshRenderer）应该使用LayaShaderType.D3
    /// </summary>
    private static LayaShaderType DetectCustomShaderType(string shaderName, List<ShaderProperty> properties)
    {
        string lowerName = shaderName.ToLower();

        // ⭐ 修复：只有真正的粒子系统shader才使用Effect类型
        // Mesh特效shader虽然名称包含"effect"，但应该使用D3类型
        // 注意：这里只做初步判断，最终由IsParticleShader确认
        bool mightBeParticle = lowerName.Contains("particle") ||  // 明确的粒子关键字
                               lowerName.Contains("shurike") ||   // 粒子系统名称
                               lowerName.Contains("trail");        // 拖尾粒子

        // 如果可能是粒子，返回Effect类型
        // 但最终是否使用粒子attributeMap由IsParticleShader决定
        if (mightBeParticle)
        {
            return LayaShaderType.Effect;
        }

        // ⭐ Mesh特效shader（包含effect/fx/vfx/additive但不是粒子系统）使用D3类型
        // 例如：BR_Effect_Mask_Additive, Effect_Basic_Additive等
        // 这些shader用于MeshRenderer，应该使用标准的Mesh attributeMap
        if (lowerName.Contains("effect") ||
            lowerName.Contains("fx") ||
            lowerName.Contains("vfx") ||
            lowerName.Contains("additive") ||
            lowerName.Contains("_add"))
        {
            Debug.Log($"LayaAir3D: Detected Mesh Effect shader (not particle): {shaderName} -> ShaderType: D3");
            return LayaShaderType.D3;  // ⭐ 使用D3而不是Effect
        }

        // 检测是否是后处理Shader
        if (lowerName.Contains("postprocess") ||
            lowerName.Contains("post process") ||
            lowerName.Contains("bloom") ||
            lowerName.Contains("blur") ||
            lowerName.Contains("tonemapping") ||
            lowerName.Contains("colorgrading") ||
            lowerName.Contains("vignette") ||
            lowerName.Contains("dof") ||
            lowerName.Contains("depth of field") ||
            lowerName.Contains("ssao") ||
            lowerName.Contains("screen space"))
        {
            return LayaShaderType.PostProcess;
        }

        // 检测是否是2D Shader
        if (lowerName.Contains("2d") ||
            lowerName.Contains("sprite") ||
            lowerName.Contains("ui") ||
            lowerName.Contains("canvas"))
        {
            return LayaShaderType.D2_BaseRenderNode2D;
        }

        // 默认为3D Shader
        return LayaShaderType.D3;
    }

    /// <summary>
    /// 根据Unity Shader名称和属性检测LayaAir ShaderType
    /// </summary>
    /// <param name="shaderName">Unity Shader名称</param>
    /// <param name="properties">Shader属性列表</param>
    /// <returns>LayaAir Shader类型</returns>
    private static LayaShaderType DetectShaderType(string shaderName, List<ShaderProperty> properties)
    {
        // 先检测材质类型，再根据材质类型确定ShaderType
        LayaMaterialType materialType = DetectMaterialType(shaderName);
        return GetShaderTypeFromMaterialType(materialType);
    }

    /// <summary>
    /// 获取ShaderType的字符串表示
    /// </summary>
    private static string GetShaderTypeString(LayaShaderType shaderType)
    {
        switch (shaderType)
        {
            case LayaShaderType.None: return "None";
            case LayaShaderType.Default: return "Default";
            case LayaShaderType.D3: return "D3";
            case LayaShaderType.D2_primitive: return "D2_primitive";
            case LayaShaderType.D2_TextureSV: return "D2_TextureSV";
            case LayaShaderType.D2_BaseRenderNode2D: return "D2_BaseRenderNode2D";
            case LayaShaderType.PostProcess: return "PostProcess";
            case LayaShaderType.Sky: return "Sky";
            case LayaShaderType.Effect: return "Effect";
            default: return "D3";
        }
    }

    /// <summary>
    /// 生成Shader文件内容 - 根据材质类型生成不同的Shader
    /// </summary>
    /// <param name="shaderName">LayaAir Shader名称（转换后的）</param>
    /// <param name="properties">Shader属性列表</param>
    /// <param name="unityShaderName">Unity原始Shader名称（用于类型检测）</param>
    /// <param name="materialFile">材质文件（用于检测实际使用的渲染器类型）</param>
    private static string GenerateShaderFileContent(string shaderName, List<ShaderProperty> properties, string unityShaderName = null, MaterialFile materialFile = null)
    {
        // 检测材质类型
        string shaderNameForDetection = unityShaderName ?? shaderName;
        LayaMaterialType materialType = DetectMaterialType(shaderNameForDetection);

        // 如果有MaterialFile，根据实际使用的渲染器类型来决定shader类型
        LayaShaderType shaderType;
        if (materialFile != null)
        {
            bool isParticle = materialFile.IsUsedByParticleSystem();
            bool isMesh = materialFile.IsUsedByMeshRenderer();

            if (isParticle && !isMesh)
            {
                shaderType = LayaShaderType.Effect;
                Debug.Log($"LayaAir3D: Using Effect shader type (used by ParticleSystemRenderer): {shaderName}");
            }
            else if (isMesh && !isParticle)
            {
                shaderType = LayaShaderType.D3;
                Debug.Log($"LayaAir3D: Using D3 shader type (used by MeshRenderer): {shaderName}");
            }
            else
            {
                // Fallback to material type detection
                shaderType = GetShaderTypeFromMaterialType(materialType);
            }
        }
        else
        {
            shaderType = GetShaderTypeFromMaterialType(materialType);
        }
        
        // 对于Custom类型，进一步检测ShaderType
        if (materialType == LayaMaterialType.Custom)
        {
            shaderType = DetectCustomShaderType(shaderNameForDetection, properties);
        }
        
        Debug.Log($"LayaAir3D: Shader '{shaderNameForDetection}' detected as MaterialType: {materialType}, ShaderType: {shaderType}");
        
        // 根据材质类型选择不同的生成策略
        switch (materialType)
        {
            case LayaMaterialType.PARTICLESHURIKEN:
                // 简单粒子材质 - 使用简化的Effect模板
                return GenerateEffectShaderContent(shaderName, properties, shaderType);
                
            case LayaMaterialType.Unlit:
                return GenerateUnlitShaderContent(shaderName, properties, shaderType);
                
            case LayaMaterialType.SkyBox:
            case LayaMaterialType.SkyProcedural:
            case LayaMaterialType.SkyPanoramic:
                return GenerateSkyShaderContent(shaderName, properties, shaderType, materialType);
                
            case LayaMaterialType.BLINNPHONG:
                return GenerateBlinnPhongShaderContent(shaderName, properties, shaderType);
                
            case LayaMaterialType.PBR:
                return GeneratePBRShaderContent(shaderName, properties, shaderType);
                
            case LayaMaterialType.Custom:
            default:
                // 自定义Shader - 根据ShaderType生成对应的基础模板
                // 注意：复杂的自定义Shader需要手动转换GLSL代码
                Debug.LogWarning($"LayaAir3D: Custom shader '{shaderNameForDetection}' detected. " +
                    "The generated shader is a basic template. You may need to manually convert the GLSL code.");
                return GenerateCustomShaderContent(shaderName, properties, shaderType);
        }
    }

    /// <summary>
    /// 生成自定义Shader内容 - 当没有源代码时使用的基础模板
    /// </summary>
    private static string GenerateCustomShaderContent(string shaderName, List<ShaderProperty> properties, LayaShaderType shaderType)
    {
        StringBuilder sb = new StringBuilder();
        string shaderTypeStr = GetShaderTypeString(shaderType);
        
        // ==================== Shader3D 配置块 ====================
        sb.AppendLine("Shader3D Start");
        sb.AppendLine("{");
        sb.AppendLine($"    type:Shader3D,");
        sb.AppendLine($"    name:{shaderName},");
        sb.AppendLine("    enableInstancing:false,");
        sb.AppendLine("    supportReflectionProbe:false,");
        sb.AppendLine($"    shaderType:{shaderTypeStr},");
        
        // uniformMap - 导出所有属性
        sb.AppendLine("    uniformMap:{");
        sb.AppendLine("        // Basic");
        sb.AppendLine("        u_AlphaTestValue: { type: Float, default: 0.5, range: [0.0, 1.0] },");
        sb.AppendLine("        u_TilingOffset: { type: Vector4, default: [1, 1, 0, 0] },");
        // ⭐ 修复：移除u_Time定义（u_Time是引擎内置uniform，不需要在shader中定义）
        // sb.AppendLine("        u_Time: { type: Vector4, default: [0, 0, 0, 0] },");
        sb.AppendLine("        u_AlbedoColor: { type: Color, default: [1, 1, 1, 1] },");
        sb.AppendLine("        u_AlbedoIntensity: { type: Float, default: 1.0, range: [0.0, 4.0] },");

        HashSet<string> addedProps = new HashSet<string> { "u_AlphaTestValue", "u_TilingOffset", "u_AlbedoColor", "u_AlbedoIntensity" };
        
        // 添加所有属性
        sb.AppendLine();
        sb.AppendLine("        // Shader Properties");
        foreach (var prop in properties)
        {
            if (addedProps.Contains(prop.layaName))
                continue;
            addedProps.Add(prop.layaName);
            
            string uniformLine = GenerateUniformLine(prop);
            sb.AppendLine($"        {uniformLine}");
        }
        
        sb.AppendLine("    },");
        
        // defines - 根据shader类型设置不同的defines
        sb.AppendLine("    defines: {");
        if (shaderType == LayaShaderType.Effect)
        {
            // 粒子shader的defines（参考Particle.shader模板）
            sb.AppendLine("        TINTCOLOR: { type: bool, default: true },");
            sb.AppendLine("        ADDTIVEFOG: { type: bool, default: true }");
        }
        else
        {
            sb.AppendLine("        COLOR: { type: bool, default: true },");
            sb.AppendLine("        ENABLEVERTEXCOLOR: { type: bool, default: true }");
        }
        sb.AppendLine("    },");
        
        // attributeMap - 粒子shader需要声明粒子系统的顶点属性
        if (shaderType == LayaShaderType.Effect)
        {
            sb.AppendLine("    attributeMap: {");
            sb.AppendLine("        a_DirectionTime: Vector4,");
            sb.AppendLine("        a_MeshPosition: Vector3,");
            sb.AppendLine("        a_MeshColor: Vector4,");
            sb.AppendLine("        a_MeshTextureCoordinate: Vector2,");
            sb.AppendLine("        a_ShapePositionStartLifeTime: Vector4,");
            sb.AppendLine("        a_CornerTextureCoordinate: Vector4,");
            sb.AppendLine("        a_StartColor: Vector4,");
            sb.AppendLine("        a_EndColor: Vector4,");
            sb.AppendLine("        a_StartSize: Vector3,");
            sb.AppendLine("        a_StartRotation0: Vector3,");
            sb.AppendLine("        a_StartSpeed: Float,");
            sb.AppendLine("        a_Random0: Vector4,");
            sb.AppendLine("        a_Random1: Vector4,");
            sb.AppendLine("        a_SimulationWorldPostion: Vector3,");
            sb.AppendLine("        a_SimulationWorldRotation: Vector4,");
            sb.AppendLine("        a_SimulationUV: Vector4");
            sb.AppendLine("    },");
        }
        
        // shaderPass
        sb.AppendLine("    shaderPass:[");
        sb.AppendLine("        {");
        sb.AppendLine("            pipeline:Forward,");
        sb.AppendLine($"            VS:{shaderName}VS,");
        sb.AppendLine($"            FS:{shaderName}FS");
        sb.AppendLine("        }");
        sb.AppendLine("    ]");
        sb.AppendLine("}");
        sb.AppendLine("Shader3D End");
        sb.AppendLine();
        
        // ==================== GLSL 代码块 ====================
        sb.AppendLine("GLSL Start");
        
        // 根据ShaderType生成对应的基础着色器
        if (shaderType == LayaShaderType.Effect)
        {
            // Effect类型（粒子/特效）使用粒子专用的shader生成函数
            GenerateParticleBillboardVertexShader(sb, shaderName);
            GenerateParticleFragmentShader(sb, shaderName);
        }
        else
        {
            // 默认使用简单的Unlit风格
            GenerateEffectVertexShader(sb, shaderName);
            GenerateEffectFragmentShader(sb, shaderName);
        }
        
        sb.AppendLine("GLSL End");
        
        return sb.ToString();
    }

    /// <summary>
    /// 生成Effect类型Shader（粒子/特效）
    /// 粒子使用Billboard模式，UV从 a_CornerTextureCoordinate.zw 获取
    /// </summary>
    private static string GenerateEffectShaderContent(string shaderName, List<ShaderProperty> properties, LayaShaderType shaderType)
    {
        StringBuilder sb = new StringBuilder();
        string shaderTypeStr = GetShaderTypeString(shaderType);
        
        // ==================== Shader3D 配置块 ====================
        sb.AppendLine("Shader3D Start");
        sb.AppendLine("{");
        sb.AppendLine($"    type:Shader3D,");
        sb.AppendLine($"    name:{shaderName},");
        sb.AppendLine("    enableInstancing:false,");
        sb.AppendLine("    supportReflectionProbe:false,");
        sb.AppendLine($"    shaderType:{shaderTypeStr},");
        
        // uniformMap - 从Unity Shader属性生成（使用properties参数）
        sb.AppendLine("    uniformMap:{");

        // 添加基础粒子系统uniforms
        sb.AppendLine("        u_AlphaTestValue: { type: Float, default: 0.5, range: [0.0, 1.0] },");
        sb.AppendLine("        u_TilingOffset: { type: Vector4, default: [1, 1, 0, 0] },");

        // 从properties列表生成完整的uniformMap
        if (properties != null && properties.Count > 0)
        {
            foreach (var prop in properties)
            {
                string uniformLine = GenerateUniformLine(prop);
                if (!string.IsNullOrEmpty(uniformLine))
                {
                    sb.AppendLine(uniformLine);
                }

                // 为纹理属性生成_ST变量（Tiling/Offset）
                if (prop.type == ShaderUtil.ShaderPropertyType.TexEnv && !prop.layaName.EndsWith("_ST"))
                {
                    string stName = $"{prop.layaName}_ST";
                    sb.AppendLine($"        {stName}: {{ type: Vector4, default: [1, 1, 0, 0] }},");
                }
            }
        }
        else
        {
            // 如果没有properties，使用最小配置
            sb.AppendLine("        u_Tintcolor: { type: Color, default: [1, 1, 1, 1] },");
            sb.AppendLine("        u_texture: { type: Texture2D, default: \"white\", options: { define: \"DIFFUSEMAP\" } },");
        }

        sb.AppendLine("    },");

        // defines - 从properties生成（纹理defines + 特性开关）
        sb.AppendLine("    defines: {");
        sb.AppendLine("        RENDERMODE_MESH: { type: bool, default: false },");
        sb.AppendLine("        TINTCOLOR: { type: bool, default: true },");
        sb.AppendLine("        ADDTIVEFOG: { type: bool, default: true },");

        // 从properties生成defines
        if (properties != null && properties.Count > 0)
        {
            // 收集所有texture的defines
            HashSet<string> addedDefines = new HashSet<string>();

            foreach (var prop in properties)
            {
                if (prop.type == ShaderUtil.ShaderPropertyType.TexEnv && !string.IsNullOrEmpty(prop.define))
                {
                    if (!addedDefines.Contains(prop.define))
                    {
                        sb.AppendLine($"        {prop.define}: {{ type: bool, default: false }},");
                        addedDefines.Add(prop.define);
                    }
                }

                // 为Use/Enable开头的Float属性生成define
                string lowerName = prop.unityName.ToLower();
                if ((lowerName.StartsWith("use") || lowerName.StartsWith("_use")) &&
                    (prop.type == ShaderUtil.ShaderPropertyType.Float || prop.type == ShaderUtil.ShaderPropertyType.Range))
                {
                    // 例如: _UseDissolve -> USEDISSOLVE
                    string defineName = prop.unityName.TrimStart('_').ToUpper();
                    if (!addedDefines.Contains(defineName))
                    {
                        sb.AppendLine($"        {defineName}: {{ type: bool, default: false }},");
                        addedDefines.Add(defineName);
                    }
                }
            }
        }

        sb.AppendLine("    },");
        
        // attributeMap - 粒子系统的顶点属性（参考Particle.shader模板）
        sb.AppendLine("    attributeMap: {");
        sb.AppendLine("        a_DirectionTime: Vector4,");
        sb.AppendLine("        a_MeshPosition: Vector3,");
        sb.AppendLine("        a_MeshColor: Vector4,");
        sb.AppendLine("        a_MeshTextureCoordinate: Vector2,");
        sb.AppendLine("        a_ShapePositionStartLifeTime: Vector4,");
        sb.AppendLine("        a_CornerTextureCoordinate: Vector4,");
        sb.AppendLine("        a_StartColor: Vector4,");
        sb.AppendLine("        a_EndColor: Vector4,");
        sb.AppendLine("        a_StartSize: Vector3,");
        sb.AppendLine("        a_StartRotation0: Vector3,");
        sb.AppendLine("        a_StartSpeed: Float,");
        sb.AppendLine("        a_Random0: Vector4,");
        sb.AppendLine("        a_Random1: Vector4,");
        sb.AppendLine("        a_SimulationWorldPostion: Vector3,");
        sb.AppendLine("        a_SimulationWorldRotation: Vector4,");
        sb.AppendLine("        a_SimulationUV: Vector4");
        sb.AppendLine("    },");
        
        // shaderPass
        sb.AppendLine("    shaderPass:[");
        sb.AppendLine("        {");
        sb.AppendLine("            pipeline:Forward,");
        sb.AppendLine($"            VS:{shaderName}VS,");
        sb.AppendLine($"            FS:{shaderName}FS");
        sb.AppendLine("        }");
        sb.AppendLine("    ]");
        sb.AppendLine("}");
        sb.AppendLine("Shader3D End");
        sb.AppendLine();
        
        // ==================== GLSL 代码块 ====================
        sb.AppendLine("GLSL Start");
        
        // 生成粒子Billboard顶点着色器（直接使用a_CornerTextureCoordinate）
        GenerateParticleBillboardVertexShader(sb, shaderName);
        
        // 生成粒子片元着色器（使用粒子专用includes）
        GenerateParticleFragmentShader(sb, shaderName);
        
        sb.AppendLine("GLSL End");
        
        return sb.ToString();
    }

    /// <summary>
    /// 生成粒子片元着色器（参考Particle.shader模板）
    /// 使用 ParticleShaderTemplate 提供完整的粒子颜色空间支持
    /// </summary>
    private static void GenerateParticleFragmentShader(StringBuilder sb, string shaderName)
    {
        sb.AppendLine($"#defineGLSL {shaderName}FS");
        sb.AppendLine();
        sb.AppendLine($"#define SHADER_NAME {shaderName}");
        sb.AppendLine();
        // 粒子shader的FS includes（参考Particle.shader模板）
        sb.AppendLine("#include \"Scene.glsl\";");
        sb.AppendLine("#include \"SceneFog.glsl\";");
        sb.AppendLine("#include \"Color.glsl\";");
        sb.AppendLine("#include \"Camera.glsl\";");
        sb.AppendLine();

        // varying声明（与VS保持一致，参考AI版本使用条件编译）
        sb.AppendLine("#ifdef RENDERMODE_MESH");
        sb.AppendLine("varying vec4 v_MeshColor;");
        sb.AppendLine("#endif");
        sb.AppendLine("varying vec4 v_Color;");
        sb.AppendLine("varying vec2 v_TextureCoordinate;");
        sb.AppendLine("varying vec4 v_ScreenPos;");
        sb.AppendLine();

        // ========== 粒子颜色空间常量（来自ParticleShaderTemplate） ==========
        sb.AppendLine(ParticleShaderTemplate.GetParticleFragmentConstants());
        sb.AppendLine();

        // main函数（参考引擎标准粒子shader模板）
        sb.AppendLine("void main()");
        sb.AppendLine("{");
        sb.AppendLine("    vec4 color;");
        sb.AppendLine();
        sb.AppendLine("#ifdef RENDERMODE_MESH");
        sb.AppendLine("    // Mesh mode: start with mesh vertex color");
        sb.AppendLine("    color = v_MeshColor;");
        sb.AppendLine("#else");
        sb.AppendLine("    // Billboard mode: start with white");
        sb.AppendLine("    color = vec4(1.0);");
        sb.AppendLine("#endif");
        sb.AppendLine();
        sb.AppendLine("#ifdef DIFFUSEMAP");
        sb.AppendLine("    vec4 colorT = texture2D(u_texture, v_TextureCoordinate);");
        sb.AppendLine("    #ifdef Gamma_u_texture");
        sb.AppendLine("    colorT = gammaToLinear(colorT);");
        sb.AppendLine("    #endif");
        sb.AppendLine("    color *= colorT;");
        sb.AppendLine("#endif");
        sb.AppendLine();
        // 注意：不要在这里再次乘以v_MeshColor，因为已经在初始化时设置了（6659-6665行）
        // 如果在这里再乘，会导致v_MeshColor^2，颜色变暗
        sb.AppendLine("#ifdef TINTCOLOR");
        sb.AppendLine("    color *= u_Tintcolor * c_ColorSpace * v_Color;");
        sb.AppendLine("#else");
        sb.AppendLine("    color *= v_Color;");
        sb.AppendLine("#endif");
        sb.AppendLine();
        sb.AppendLine("#ifdef ALPHATEST");
        sb.AppendLine("    if (color.a < u_AlphaTestValue)");
        sb.AppendLine("    {");
        sb.AppendLine("        discard;");
        sb.AppendLine("    }");
        sb.AppendLine("#endif");
        sb.AppendLine();
        sb.AppendLine("    gl_FragColor = color;");
        sb.AppendLine();
        sb.AppendLine("#ifdef FOG");
        sb.AppendLine("    gl_FragColor.rgb = scenUnlitFog(gl_FragColor.rgb);");
        sb.AppendLine("#endif");
        sb.AppendLine();
        sb.AppendLine("    gl_FragColor = outputTransform(gl_FragColor);");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("#endGLSL");
        sb.AppendLine();
    }

    /// <summary>
    /// 生成粒子Billboard顶点着色器（参考Particle.shader模板）
    /// 粒子系统不使用Vertex结构体，直接使用粒子attribute计算位置
    /// 使用 ParticleShaderTemplate 提供80%+ Unity兼容性
    /// </summary>
    private static void GenerateParticleBillboardVertexShader(StringBuilder sb, string shaderName)
    {
        sb.AppendLine($"#defineGLSL {shaderName}VS");
        sb.AppendLine();
        sb.AppendLine($"#define SHADER_NAME {shaderName}");
        sb.AppendLine();
        // 粒子shader的VS includes（参考Particle.shader模板）
        sb.AppendLine("#include \"Camera.glsl\";");
        sb.AppendLine("#include \"particleShuriKenSpriteVS.glsl\";");
        sb.AppendLine("#include \"Math.glsl\";");
        sb.AppendLine("#include \"MathGradient.glsl\";");
        sb.AppendLine("#include \"Color.glsl\";");
        sb.AppendLine("#include \"Scene.glsl\";");
        sb.AppendLine("#include \"SceneFogInput.glsl\";");
        sb.AppendLine();

        // varying声明（参考AI版本使用条件编译）
        sb.AppendLine("#ifdef RENDERMODE_MESH");
        sb.AppendLine("varying vec4 v_MeshColor;");
        sb.AppendLine("#endif");
        sb.AppendLine("varying vec4 v_Color;");
        sb.AppendLine("varying vec2 v_TextureCoordinate;");
        sb.AppendLine("varying vec4 v_ScreenPos;");
        sb.AppendLine();

        // ========== 注入完整的粒子函数库（来自ParticleShaderTemplate） ==========
        sb.Append(ParticleShaderTemplate.GetParticleVertexFunctions());

        // ========== 注入完整的main函数（来自ParticleShaderTemplate） ==========
        sb.Append(ParticleShaderTemplate.GetParticleVertexMainFunction());

        sb.AppendLine();
        sb.AppendLine("#endGLSL");
        sb.AppendLine();
    }

    /// <summary>
    /// 生成普通Effect顶点着色器（非粒子）
    /// 使用 vertex.texCoord0 作为UV
    /// </summary>
    private static void GenerateEffectVertexShader(StringBuilder sb, string shaderName)
    {
        sb.AppendLine($"#defineGLSL {shaderName}VS");
        sb.AppendLine($"    #define SHADER_NAME {shaderName}");
        sb.AppendLine();
        sb.AppendLine("    #include \"Math.glsl\";");
        sb.AppendLine();
        sb.AppendLine("    #include \"Scene.glsl\";");
        sb.AppendLine("    #include \"SceneFogInput.glsl\";");
        sb.AppendLine();
        sb.AppendLine("    #include \"Camera.glsl\";");
        sb.AppendLine("    #include \"Sprite3DVertex.glsl\";");
        sb.AppendLine();
        sb.AppendLine("    #include \"VertexCommon.glsl\";");
        sb.AppendLine();
        sb.AppendLine("    #ifdef UV");
        sb.AppendLine("    varying vec2 v_Texcoord0;");
        sb.AppendLine("    #endif // UV");
        sb.AppendLine();
        sb.AppendLine("    #ifdef COLOR");
        sb.AppendLine("    varying vec4 v_VertexColor;");
        sb.AppendLine("    #endif // COLOR");
        sb.AppendLine();
        sb.AppendLine("    void main()");
        sb.AppendLine("    {");
        sb.AppendLine("        Vertex vertex;");
        sb.AppendLine("        getVertexParams(vertex);");
        sb.AppendLine();
        sb.AppendLine("    #ifdef UV");
        sb.AppendLine("        v_Texcoord0 = transformUV(vertex.texCoord0, u_TilingOffset);");
        sb.AppendLine("    #endif // UV");
        sb.AppendLine();
        sb.AppendLine("    #ifdef COLOR");
        sb.AppendLine("        v_VertexColor = vertex.vertexColor;");
        sb.AppendLine("    #endif // COLOR");
        sb.AppendLine();
        sb.AppendLine("        mat4 worldMat = getWorldMatrix();");
        sb.AppendLine("        vec4 pos = (worldMat * vec4(vertex.positionOS, 1.0));");
        sb.AppendLine("        vec3 positionWS = pos.xyz / pos.w;");
        sb.AppendLine();
        sb.AppendLine("        gl_Position = getPositionCS(positionWS);");
        sb.AppendLine("        gl_Position = remapPositionZ(gl_Position);");
        sb.AppendLine();
        sb.AppendLine("    #ifdef FOG");
        sb.AppendLine("        FogHandle(gl_Position.z);");
        sb.AppendLine("    #endif");
        sb.AppendLine("    }");
        sb.AppendLine("#endGLSL");
        sb.AppendLine();
    }

    /// <summary>
    /// 生成Effect片元着色器
    /// </summary>
    private static void GenerateEffectFragmentShader(StringBuilder sb, string shaderName)
    {
        sb.AppendLine($"#defineGLSL {shaderName}FS");
        sb.AppendLine($"    #define SHADER_NAME {shaderName}");
        sb.AppendLine();
        sb.AppendLine("    #include \"Color.glsl\";");
        sb.AppendLine();
        sb.AppendLine("    #include \"Scene.glsl\";");
        sb.AppendLine("    #include \"SceneFog.glsl\";");
        sb.AppendLine();
        sb.AppendLine("    #include \"Camera.glsl\";");
        sb.AppendLine("    #include \"Sprite3DFrag.glsl\";");
        sb.AppendLine();
        sb.AppendLine("    #ifdef UV");
        sb.AppendLine("    varying vec2 v_Texcoord0;");
        sb.AppendLine("    #endif // UV");
        sb.AppendLine();
        sb.AppendLine("    #ifdef COLOR");
        sb.AppendLine("    varying vec4 v_VertexColor;");
        sb.AppendLine("    #endif // COLOR");
        sb.AppendLine();
        sb.AppendLine("    void main()");
        sb.AppendLine("    {");
        sb.AppendLine("        vec2 uv = vec2(0.0);");
        sb.AppendLine("    #ifdef UV");
        sb.AppendLine("        uv = v_Texcoord0;");
        sb.AppendLine("    #endif // UV");
        sb.AppendLine();
        sb.AppendLine("        vec3 color = u_AlbedoColor.rgb;");
        sb.AppendLine("        float alpha = u_AlbedoColor.a;");
        sb.AppendLine();
        sb.AppendLine("    #ifdef ALBEDOTEXTURE");
        sb.AppendLine("        vec4 albedoSampler = texture2D(u_AlbedoTexture, uv);");
        sb.AppendLine("        #ifdef Gamma_u_AlbedoTexture");
        sb.AppendLine("        albedoSampler = gammaToLinear(albedoSampler);");
        sb.AppendLine("        #endif // Gamma_u_AlbedoTexture");
        sb.AppendLine("        color *= albedoSampler.rgb;");
        sb.AppendLine("        alpha *= albedoSampler.a;");
        sb.AppendLine("    #endif // ALBEDOTEXTURE");
        sb.AppendLine();
        sb.AppendLine("        color *= u_AlbedoIntensity;");
        sb.AppendLine();
        sb.AppendLine("    #ifdef COLOR");
        sb.AppendLine("        #ifdef ENABLEVERTEXCOLOR");
        sb.AppendLine("        color *= v_VertexColor.rgb;");
        sb.AppendLine("        alpha *= v_VertexColor.a;");
        sb.AppendLine("        #endif // ENABLEVERTEXCOLOR");
        sb.AppendLine("    #endif // COLOR");
        sb.AppendLine();
        sb.AppendLine("    #ifdef ALPHATEST");
        sb.AppendLine("        if (alpha < u_AlphaTestValue)");
        sb.AppendLine("            discard;");
        sb.AppendLine("    #endif // ALPHATEST");
        sb.AppendLine();
        sb.AppendLine("    #ifdef FOG");
        sb.AppendLine("        color = scenUnlitFog(color);");
        sb.AppendLine("    #endif // FOG");
        sb.AppendLine();
        sb.AppendLine("        gl_FragColor = vec4(color, alpha);");
        sb.AppendLine("        gl_FragColor = outputTransform(gl_FragColor);");
        sb.AppendLine("    }");
        sb.AppendLine("#endGLSL");
        sb.AppendLine();
    }

    /// <summary>
    /// 生成Unlit类型Shader
    /// </summary>
    private static string GenerateUnlitShaderContent(string shaderName, List<ShaderProperty> properties, LayaShaderType shaderType)
    {
        // Unlit和Effect类似，但shaderType是D3
        StringBuilder sb = new StringBuilder();
        string shaderTypeStr = GetShaderTypeString(shaderType);
        
        sb.AppendLine("Shader3D Start");
        sb.AppendLine("{");
        sb.AppendLine($"    type:Shader3D,");
        sb.AppendLine($"    name:{shaderName},");
        sb.AppendLine("    enableInstancing:true,");
        sb.AppendLine("    supportReflectionProbe:false,");
        sb.AppendLine($"    shaderType:{shaderTypeStr},");
        
        sb.AppendLine("    uniformMap:{");
        sb.AppendLine("        u_AlphaTestValue: { type: Float, default: 0.5, range: [0.0, 1.0] },");
        sb.AppendLine("        u_TilingOffset: { type: Vector4, default: [1, 1, 0, 0] },");
        sb.AppendLine("        u_AlbedoColor: { type: Color, default: [1, 1, 1, 1] },");
        sb.AppendLine("        u_AlbedoTexture: { type: Texture2D, options: { define: \"ALBEDOTEXTURE\" } },");
        sb.AppendLine("        u_AlbedoIntensity: { type: Float, default: 1.0, range: [0.0, 4.0] },");
        sb.AppendLine("    },");
        
        // Unlit类型默认开启COLOR宏以支持顶点颜色
        sb.AppendLine("    defines: {");
        sb.AppendLine("        COLOR: { type: bool, default: true },");
        sb.AppendLine("        ENABLEVERTEXCOLOR: { type: bool, default: true }");
        sb.AppendLine("    },");
        
        sb.AppendLine("    shaderPass:[");
        sb.AppendLine("        {");
        sb.AppendLine("            pipeline:Forward,");
        sb.AppendLine($"            VS:{shaderName}VS,");
        sb.AppendLine($"            FS:{shaderName}FS");
        sb.AppendLine("        }");
        sb.AppendLine("    ]");
        sb.AppendLine("}");
        sb.AppendLine("Shader3D End");
        sb.AppendLine();
        
        sb.AppendLine("GLSL Start");
        GenerateEffectVertexShader(sb, shaderName);
        GenerateEffectFragmentShader(sb, shaderName);
        sb.AppendLine("GLSL End");
        
        return sb.ToString();
    }

    /// <summary>
    /// 生成Sky类型Shader
    /// </summary>
    private static string GenerateSkyShaderContent(string shaderName, List<ShaderProperty> properties, LayaShaderType shaderType, LayaMaterialType materialType)
    {
        // 天空盒Shader - 简化版本
        StringBuilder sb = new StringBuilder();
        string shaderTypeStr = GetShaderTypeString(shaderType);
        
        sb.AppendLine("Shader3D Start");
        sb.AppendLine("{");
        sb.AppendLine($"    type:Shader3D,");
        sb.AppendLine($"    name:{shaderName},");
        sb.AppendLine("    enableInstancing:false,");
        sb.AppendLine("    supportReflectionProbe:false,");
        sb.AppendLine($"    shaderType:{shaderTypeStr},");
        
        sb.AppendLine("    uniformMap:{");
        sb.AppendLine("        u_TintColor: { type: Color, default: [0.5, 0.5, 0.5, 0.5] },");
        sb.AppendLine("        u_Exposure: { type: Float, default: 1.0, range: [0.0, 8.0] },");
        sb.AppendLine("        u_Rotation: { type: Float, default: 0.0, range: [0.0, 360.0] },");
        if (materialType == LayaMaterialType.SkyPanoramic)
        {
            sb.AppendLine("        u_Texture: { type: Texture2D },");
        }
        else
        {
            sb.AppendLine("        u_CubeTexture: { type: TextureCube },");
        }
        sb.AppendLine("    },");
        
        sb.AppendLine("    defines: {},");
        
        sb.AppendLine("    shaderPass:[");
        sb.AppendLine("        {");
        sb.AppendLine("            pipeline:Forward,");
        sb.AppendLine($"            VS:{shaderName}VS,");
        sb.AppendLine($"            FS:{shaderName}FS");
        sb.AppendLine("        }");
        sb.AppendLine("    ]");
        sb.AppendLine("}");
        sb.AppendLine("Shader3D End");
        sb.AppendLine();
        
        // 天空盒GLSL代码需要特殊处理，这里使用简化版本
        sb.AppendLine("GLSL Start");
        GenerateSkyVertexShader(sb, shaderName);
        GenerateSkyFragmentShader(sb, shaderName, materialType);
        sb.AppendLine("GLSL End");
        
        return sb.ToString();
    }

    /// <summary>
    /// 生成Sky顶点着色器
    /// </summary>
    private static void GenerateSkyVertexShader(StringBuilder sb, string shaderName)
    {
        sb.AppendLine($"#defineGLSL {shaderName}VS");
        sb.AppendLine($"    #define SHADER_NAME {shaderName}");
        sb.AppendLine();
        sb.AppendLine("    #include \"Math.glsl\";");
        sb.AppendLine("    #include \"Camera.glsl\";");
        sb.AppendLine("    #include \"Sprite3DVertex.glsl\";");
        sb.AppendLine("    #include \"VertexCommon.glsl\";");
        sb.AppendLine();
        sb.AppendLine("    varying vec3 v_Texcoord;");
        sb.AppendLine();
        sb.AppendLine("    vec3 RotateAroundYInDegrees(vec3 vertex, float degrees)");
        sb.AppendLine("    {");
        sb.AppendLine("        float alpha = degrees * PI / 180.0;");
        sb.AppendLine("        float sina = sin(alpha);");
        sb.AppendLine("        float cosa = cos(alpha);");
        sb.AppendLine("        mat2 m = mat2(cosa, -sina, sina, cosa);");
        sb.AppendLine("        vec2 rotated = m * vertex.xz;");
        sb.AppendLine("        return vec3(rotated.x, vertex.y, rotated.y);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    void main()");
        sb.AppendLine("    {");
        sb.AppendLine("        Vertex vertex;");
        sb.AppendLine("        getVertexParams(vertex);");
        sb.AppendLine();
        sb.AppendLine("        vec3 rotated = RotateAroundYInDegrees(vertex.positionOS, u_Rotation);");
        sb.AppendLine("        mat4 worldMat = getWorldMatrix();");
        sb.AppendLine("        vec4 pos = worldMat * vec4(rotated, 1.0);");
        sb.AppendLine("        gl_Position = getPositionCS(pos.xyz);");
        sb.AppendLine("        gl_Position = remapPositionZ(gl_Position);");
        sb.AppendLine("        v_Texcoord = vertex.positionOS;");
        sb.AppendLine("    }");
        sb.AppendLine("#endGLSL");
        sb.AppendLine();
    }

    /// <summary>
    /// 生成Sky片元着色器
    /// </summary>
    private static void GenerateSkyFragmentShader(StringBuilder sb, string shaderName, LayaMaterialType materialType)
    {
        sb.AppendLine($"#defineGLSL {shaderName}FS");
        sb.AppendLine($"    #define SHADER_NAME {shaderName}");
        sb.AppendLine();
        sb.AppendLine("    #include \"Color.glsl\";");
        sb.AppendLine("    #include \"Camera.glsl\";");
        sb.AppendLine("    #include \"Sprite3DFrag.glsl\";");
        sb.AppendLine();
        sb.AppendLine("    varying vec3 v_Texcoord;");
        sb.AppendLine();
        sb.AppendLine("    void main()");
        sb.AppendLine("    {");
        if (materialType == LayaMaterialType.SkyPanoramic)
        {
            sb.AppendLine("        vec3 dir = normalize(v_Texcoord);");
            sb.AppendLine("        vec2 uv;");
            sb.AppendLine("        uv.x = atan(dir.z, dir.x) / (2.0 * PI) + 0.5;");
            sb.AppendLine("        uv.y = asin(dir.y) / PI + 0.5;");
            sb.AppendLine("        vec4 tex = texture2D(u_Texture, uv);");
        }
        else
        {
            sb.AppendLine("        vec4 tex = textureCube(u_CubeTexture, v_Texcoord);");
        }
        sb.AppendLine("        vec3 color = tex.rgb * u_TintColor.rgb * 2.0;");
        sb.AppendLine("        color *= u_Exposure;");
        sb.AppendLine("        gl_FragColor = vec4(color, 1.0);");
        sb.AppendLine("        gl_FragColor = outputTransform(gl_FragColor);");
        sb.AppendLine("    }");
        sb.AppendLine("#endGLSL");
        sb.AppendLine();
    }

    /// <summary>
    /// 生成BlinnPhong类型Shader
    /// </summary>
    private static string GenerateBlinnPhongShaderContent(string shaderName, List<ShaderProperty> properties, LayaShaderType shaderType)
    {
        // BlinnPhong使用简化的光照模型
        StringBuilder sb = new StringBuilder();
        string shaderTypeStr = GetShaderTypeString(shaderType);
        
        sb.AppendLine("Shader3D Start");
        sb.AppendLine("{");
        sb.AppendLine($"    type:Shader3D,");
        sb.AppendLine($"    name:{shaderName},");
        sb.AppendLine("    enableInstancing:true,");
        sb.AppendLine("    supportReflectionProbe:false,");
        sb.AppendLine($"    shaderType:{shaderTypeStr},");
        
        sb.AppendLine("    uniformMap:{");
        sb.AppendLine("        u_AlphaTestValue: { type: Float, default: 0.5, range: [0.0, 1.0] },");
        sb.AppendLine("        u_TilingOffset: { type: Vector4, default: [1, 1, 0, 0] },");
        sb.AppendLine("        u_DiffuseColor: { type: Color, default: [1, 1, 1, 1] },");
        sb.AppendLine("        u_DiffuseTexture: { type: Texture2D, options: { define: \"DIFFUSEMAP\" } },");
        sb.AppendLine("        u_MaterialSpecular: { type: Color, default: [1, 1, 1, 1] },");
        sb.AppendLine("        u_SpecularTexture: { type: Texture2D, options: { define: \"SPECULARMAP\" } },");
        sb.AppendLine("        u_NormalTexture: { type: Texture2D, options: { define: \"NORMALTEXTURE\" } },");
        sb.AppendLine("        u_Shininess: { type: Float, default: 32.0, range: [1.0, 256.0] },");
        sb.AppendLine("        u_AlbedoIntensity: { type: Float, default: 1.0, range: [0.0, 4.0] },");
        sb.AppendLine("    },");
        
        // BlinnPhong类型默认开启COLOR宏以支持顶点颜色
        sb.AppendLine("    defines: {");
        sb.AppendLine("        COLOR: { type: bool, default: true },");
        sb.AppendLine("        ENABLEVERTEXCOLOR: { type: bool, default: true }");
        sb.AppendLine("    },");
        
        sb.AppendLine("    shaderPass:[");
        sb.AppendLine("        {");
        sb.AppendLine("            pipeline:Forward,");
        sb.AppendLine($"            VS:{shaderName}VS,");
        sb.AppendLine($"            FS:{shaderName}FS");
        sb.AppendLine("        }");
        sb.AppendLine("    ]");
        sb.AppendLine("}");
        sb.AppendLine("Shader3D End");
        sb.AppendLine();
        
        sb.AppendLine("GLSL Start");
        GenerateBlinnPhongVertexShader(sb, shaderName);
        GenerateBlinnPhongFragmentShader(sb, shaderName);
        sb.AppendLine("GLSL End");
        
        return sb.ToString();
    }

    /// <summary>
    /// 生成BlinnPhong顶点着色器
    /// </summary>
    private static void GenerateBlinnPhongVertexShader(StringBuilder sb, string shaderName)
    {
        sb.AppendLine($"#defineGLSL {shaderName}VS");
        sb.AppendLine($"    #define SHADER_NAME {shaderName}");
        sb.AppendLine();
        sb.AppendLine("    #include \"Math.glsl\";");
        sb.AppendLine("    #include \"Scene.glsl\";");
        sb.AppendLine("    #include \"SceneFogInput.glsl\";");
        sb.AppendLine("    #include \"Camera.glsl\";");
        sb.AppendLine("    #include \"Sprite3DVertex.glsl\";");
        sb.AppendLine("    #include \"VertexCommon.glsl\";");
        sb.AppendLine();
        sb.AppendLine("    varying vec2 v_Texcoord0;");
        sb.AppendLine("    varying vec3 v_Normal;");
        sb.AppendLine("    varying vec3 v_PositionWS;");
        sb.AppendLine();
        sb.AppendLine("    #ifdef COLOR");
        sb.AppendLine("    varying vec4 v_VertexColor;");
        sb.AppendLine("    #endif");
        sb.AppendLine();
        sb.AppendLine("    void main()");
        sb.AppendLine("    {");
        sb.AppendLine("        Vertex vertex;");
        sb.AppendLine("        getVertexParams(vertex);");
        sb.AppendLine();
        sb.AppendLine("        v_Texcoord0 = transformUV(vertex.texCoord0, u_TilingOffset);");
        sb.AppendLine();
        sb.AppendLine("        mat4 worldMat = getWorldMatrix();");
        sb.AppendLine("        vec4 pos = worldMat * vec4(vertex.positionOS, 1.0);");
        sb.AppendLine("        v_PositionWS = pos.xyz / pos.w;");
        sb.AppendLine("        v_Normal = normalize((worldMat * vec4(vertex.normalOS, 0.0)).xyz);");
        sb.AppendLine();
        sb.AppendLine("    #ifdef COLOR");
        sb.AppendLine("        v_VertexColor = vertex.vertexColor;");
        sb.AppendLine("    #endif");
        sb.AppendLine();
        sb.AppendLine("        gl_Position = getPositionCS(v_PositionWS);");
        sb.AppendLine("        gl_Position = remapPositionZ(gl_Position);");
        sb.AppendLine();
        sb.AppendLine("    #ifdef FOG");
        sb.AppendLine("        FogHandle(gl_Position.z);");
        sb.AppendLine("    #endif");
        sb.AppendLine("    }");
        sb.AppendLine("#endGLSL");
        sb.AppendLine();
    }

    /// <summary>
    /// 生成BlinnPhong片元着色器
    /// </summary>
    private static void GenerateBlinnPhongFragmentShader(StringBuilder sb, string shaderName)
    {
        sb.AppendLine($"#defineGLSL {shaderName}FS");
        sb.AppendLine($"    #define SHADER_NAME {shaderName}");
        sb.AppendLine();
        sb.AppendLine("    #include \"Color.glsl\";");
        sb.AppendLine("    #include \"Scene.glsl\";");
        sb.AppendLine("    #include \"SceneFog.glsl\";");
        sb.AppendLine("    #include \"Camera.glsl\";");
        sb.AppendLine("    #include \"Sprite3DFrag.glsl\";");
        sb.AppendLine();
        sb.AppendLine("    varying vec2 v_Texcoord0;");
        sb.AppendLine("    varying vec3 v_Normal;");
        sb.AppendLine("    varying vec3 v_PositionWS;");
        sb.AppendLine();
        sb.AppendLine("    #ifdef COLOR");
        sb.AppendLine("    varying vec4 v_VertexColor;");
        sb.AppendLine("    #endif");
        sb.AppendLine();
        sb.AppendLine("    void main()");
        sb.AppendLine("    {");
        sb.AppendLine("        vec2 uv = v_Texcoord0;");
        sb.AppendLine("        vec3 normal = normalize(v_Normal);");
        sb.AppendLine();
        sb.AppendLine("        // Diffuse");
        sb.AppendLine("        vec4 diffuseColor = u_DiffuseColor;");
        sb.AppendLine("    #ifdef DIFFUSEMAP");
        sb.AppendLine("        vec4 diffuseTex = texture2D(u_DiffuseTexture, uv);");
        sb.AppendLine("        #ifdef Gamma_u_DiffuseTexture");
        sb.AppendLine("        diffuseTex = gammaToLinear(diffuseTex);");
        sb.AppendLine("        #endif");
        sb.AppendLine("        diffuseColor *= diffuseTex;");
        sb.AppendLine("    #endif");
        sb.AppendLine();
        sb.AppendLine("    #ifdef COLOR");
        sb.AppendLine("        #ifdef ENABLEVERTEXCOLOR");
        sb.AppendLine("        diffuseColor *= v_VertexColor;");
        sb.AppendLine("        #endif");
        sb.AppendLine("    #endif");
        sb.AppendLine();
        sb.AppendLine("        // Simple directional light");
        sb.AppendLine("        vec3 lightDir = normalize(vec3(0.5, 1.0, 0.3));");
        sb.AppendLine("        float NdotL = max(dot(normal, lightDir), 0.0);");
        sb.AppendLine("        vec3 diffuse = diffuseColor.rgb * NdotL;");
        sb.AppendLine();
        sb.AppendLine("        // Specular");
        sb.AppendLine("        vec3 viewDir = normalize(u_CameraPos - v_PositionWS);");
        sb.AppendLine("        vec3 halfDir = normalize(lightDir + viewDir);");
        sb.AppendLine("        float NdotH = max(dot(normal, halfDir), 0.0);");
        sb.AppendLine("        vec3 specular = u_MaterialSpecular.rgb * pow(NdotH, u_Shininess);");
        sb.AppendLine();
        sb.AppendLine("        // Ambient");
        sb.AppendLine("        vec3 ambient = diffuseColor.rgb * 0.2;");
        sb.AppendLine();
        sb.AppendLine("        vec3 color = ambient + diffuse + specular;");
        sb.AppendLine("        color *= u_AlbedoIntensity;");
        sb.AppendLine();
        sb.AppendLine("    #ifdef ALPHATEST");
        sb.AppendLine("        if (diffuseColor.a < u_AlphaTestValue)");
        sb.AppendLine("            discard;");
        sb.AppendLine("    #endif");
        sb.AppendLine();
        sb.AppendLine("    #ifdef FOG");
        sb.AppendLine("        color = scenUnlitFog(color);");
        sb.AppendLine("    #endif");
        sb.AppendLine();
        sb.AppendLine("        gl_FragColor = vec4(color, diffuseColor.a);");
        sb.AppendLine("        gl_FragColor = outputTransform(gl_FragColor);");
        sb.AppendLine("    }");
        sb.AppendLine("#endGLSL");
        sb.AppendLine();
    }

    /// <summary>
    /// 生成PBR类型Shader（完整版，支持PBR+NPR混合渲染）
    /// </summary>
    private static string GeneratePBRShaderContent(string shaderName, List<ShaderProperty> properties, LayaShaderType shaderType)
    {
        StringBuilder sb = new StringBuilder();
        
        // 检测Shader特性
        bool hasNPR = HasNPRFeatures(properties);
        bool hasIBL = HasIBLFeatures(properties);
        bool hasMatcap = HasMatcapFeatures(properties);
        bool hasFresnel = HasFresnelFeatures(properties);
        bool hasRim = HasRimFeatures(properties);
        bool hasHSV = HasHSVFeatures(properties);
        bool hasTonemapping = HasTonemappingFeatures(properties);
        
        string shaderTypeStr = GetShaderTypeString(shaderType);
        
        // ==================== Shader3D 配置块 ====================
        sb.AppendLine("Shader3D Start");
        sb.AppendLine("{");
        sb.AppendLine($"    type:Shader3D,");
        sb.AppendLine($"    name:{shaderName},");
        sb.AppendLine("    enableInstancing:true,");
        sb.AppendLine("    supportReflectionProbe:true,");
        sb.AppendLine($"    shaderType:{shaderTypeStr},");
        
        // uniformMap
        sb.AppendLine("    uniformMap:{");
        
        // 基础属性
        sb.AppendLine("        // Basic");
        sb.AppendLine("        u_AlphaTestValue: { type: Float, default: 0.5, range: [0.0, 1.0] },");
        sb.AppendLine("        u_TilingOffset: { type: Vector4, default: [1, 1, 0, 0] },");
        sb.AppendLine("        u_Alpha: { type: Float, default: 1.0, range: [0.0, 1.0] },");
        
        HashSet<string> addedProps = new HashSet<string> { "u_AlphaTestValue", "u_TilingOffset", "u_Alpha" };
        
        // 按类别分组输出属性
        GenerateUniformsByCategory(sb, properties, addedProps, "Base Color", 
            new[] { "BaseMap", "BaseColor", "MainTex", "Color", "ColorIntensity", "AlbedoTexture", "AlbedoColor" });
        
        GenerateUniformsByCategory(sb, properties, addedProps, "PBR - MAER Map",
            new[] { "MAER", "Metallic", "Smoothness", "Occlusion", "AO" });
        
        GenerateUniformsByCategory(sb, properties, addedProps, "Normal Map",
            new[] { "Normal", "Bump" });
        
        GenerateUniformsByCategory(sb, properties, addedProps, "Mask",
            new[] { "Mask" });
        
        if (hasIBL)
        {
            GenerateUniformsByCategory(sb, properties, addedProps, "IBL",
                new[] { "IBL" });
        }
        
        if (hasMatcap)
        {
            GenerateUniformsByCategory(sb, properties, addedProps, "Matcap",
                new[] { "Matcap" });
        }
        
        GenerateUniformsByCategory(sb, properties, addedProps, "Emission",
            new[] { "Emission" });
        
        if (hasNPR)
        {
            GenerateUniformsByCategory(sb, properties, addedProps, "NPR Toon Shading",
                new[] { "Med", "Shadow", "Reflect", "GI" });
        }
        
        GenerateUniformsByCategory(sb, properties, addedProps, "Specular",
            new[] { "Specular", "GGX" });
        
        if (hasFresnel)
        {
            GenerateUniformsByCategory(sb, properties, addedProps, "Fresnel",
                new[] { "Fresnel", "fresnel" });
        }
        
        if (hasRim)
        {
            GenerateUniformsByCategory(sb, properties, addedProps, "Rim Effect",
                new[] { "Rim" });
        }
        
        if (hasHSV)
        {
            GenerateUniformsByCategory(sb, properties, addedProps, "HSV Adjust",
                new[] { "HSV", "Hue", "Saturation", "Value" });
        }
        
        GenerateUniformsByCategory(sb, properties, addedProps, "Contrast",
            new[] { "Contrast", "OriginalColor" });
        
        if (hasTonemapping)
        {
            GenerateUniformsByCategory(sb, properties, addedProps, "Tonemapping",
                new[] { "Tone", "WhitePoint" });
        }
        
        GenerateUniformsByCategory(sb, properties, addedProps, "Light Control",
            new[] { "SelfLight", "LightDir" });
        
        // 输出剩余未分类的属性
        GenerateRemainingUniforms(sb, properties, addedProps);
        
        // 添加必要的默认属性（如果Shader中没有定义）
        GenerateRequiredDefaults(sb, addedProps, hasNPR, hasFresnel, hasRim, hasHSV, hasTonemapping);
        
        sb.AppendLine("    },");
        
        // defines - PBR类型默认开启COLOR宏以支持顶点颜色
        sb.AppendLine("    defines: {");
        sb.AppendLine("        COLOR: { type: bool, default: true },");
        sb.AppendLine("        EMISSION: { type: bool, default: false },");
        sb.AppendLine("        ENABLEVERTEXCOLOR: { type: bool, default: true },");
        if (hasNPR)
        {
            sb.AppendLine("        USENPR: { type: bool, default: true }");
        }
        else
        {
            sb.AppendLine("        USENPR: { type: bool, default: false }");
        }
        sb.AppendLine("    },");
        
        // shaderPass
        sb.AppendLine("    shaderPass:[");
        sb.AppendLine("        {");
        sb.AppendLine("            pipeline:Forward,");
        sb.AppendLine($"            VS:{shaderName}VS,");
        sb.AppendLine($"            FS:{shaderName}FS");
        sb.AppendLine("        }");
        sb.AppendLine("    ]");
        sb.AppendLine("}");
        sb.AppendLine("Shader3D End");
        sb.AppendLine();
        
        // ==================== GLSL 代码块 ====================
        sb.AppendLine("GLSL Start");
        
        // 生成顶点着色器
        GenerateVertexShader(sb, shaderName, properties);
        
        // 生成片元着色器
        GenerateFragmentShader(sb, shaderName, properties, hasNPR, hasIBL, hasMatcap, hasFresnel, hasRim, hasHSV, hasTonemapping);
        
        sb.AppendLine("GLSL End");
        
        return sb.ToString();
    }

    /// <summary>
    /// 按类别生成Uniform属性
    /// </summary>
    private static void GenerateUniformsByCategory(StringBuilder sb, List<ShaderProperty> properties, 
        HashSet<string> addedProps, string categoryName, string[] keywords)
    {
        List<ShaderProperty> categoryProps = new List<ShaderProperty>();
        
        foreach (var prop in properties)
        {
            if (addedProps.Contains(prop.layaName))
                continue;
                
            foreach (var keyword in keywords)
            {
                if (prop.unityName.Contains(keyword))
                {
                    categoryProps.Add(prop);
                    break;
                }
            }
        }
        
        if (categoryProps.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine($"        // {categoryName}");
            
            foreach (var prop in categoryProps)
            {
                if (addedProps.Contains(prop.layaName))
                    continue;
                addedProps.Add(prop.layaName);
                
                string uniformLine = GenerateUniformLine(prop);
                if (!string.IsNullOrEmpty(uniformLine))
                {
                    sb.AppendLine(uniformLine);
                }
            }
        }
    }

    /// <summary>
    /// 生成剩余未分类的Uniform属性
    /// </summary>
    private static void GenerateRemainingUniforms(StringBuilder sb, List<ShaderProperty> properties, HashSet<string> addedProps)
    {
        List<ShaderProperty> remainingProps = new List<ShaderProperty>();
        
        foreach (var prop in properties)
        {
            if (!addedProps.Contains(prop.layaName))
            {
                remainingProps.Add(prop);
            }
        }
        
        if (remainingProps.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("        // Other Properties");
            
            foreach (var prop in remainingProps)
            {
                addedProps.Add(prop.layaName);
                string uniformLine = GenerateUniformLine(prop);
                if (!string.IsNullOrEmpty(uniformLine))
                {
                    sb.AppendLine(uniformLine);
                }
            }
        }
    }

    /// <summary>
    /// 生成必要的默认属性（确保Shader中使用的变量都有定义）
    /// </summary>
    private static void GenerateRequiredDefaults(StringBuilder sb, HashSet<string> addedProps, 
        bool hasNPR, bool hasFresnel, bool hasRim, bool hasHSV, bool hasTonemapping)
    {
        List<string> requiredDefaults = new List<string>();
        
        // 基础必需属性
        AddDefaultIfMissing(requiredDefaults, addedProps, "u_AlbedoColor", "{ type: Color, default: [1, 1, 1, 1] }");
        AddDefaultIfMissing(requiredDefaults, addedProps, "u_NormalScale", "{ type: Float, default: 1.0 }");
        AddDefaultIfMissing(requiredDefaults, addedProps, "u_Metallic", "{ type: Float, default: 0.0, range: [0.0, 1.0] }");
        AddDefaultIfMissing(requiredDefaults, addedProps, "u_MetallicRemapMin", "{ type: Float, default: 0.0 }");
        AddDefaultIfMissing(requiredDefaults, addedProps, "u_MetallicRemapMax", "{ type: Float, default: 1.0 }");
        AddDefaultIfMissing(requiredDefaults, addedProps, "u_Smoothness", "{ type: Float, default: 0.5, range: [0.0, 1.0] }");
        AddDefaultIfMissing(requiredDefaults, addedProps, "u_SmoothnessRemapMin", "{ type: Float, default: 0.0 }");
        AddDefaultIfMissing(requiredDefaults, addedProps, "u_SmoothnessRemapMax", "{ type: Float, default: 1.0 }");
        AddDefaultIfMissing(requiredDefaults, addedProps, "u_OcclusionStrength", "{ type: Float, default: 1.0, range: [0.0, 1.5] }");
        AddDefaultIfMissing(requiredDefaults, addedProps, "u_EmissionColor", "{ type: Color, default: [0, 0, 0, 0] }");
        AddDefaultIfMissing(requiredDefaults, addedProps, "u_EmissionIntensity", "{ type: Float, default: 1.0 }");
        AddDefaultIfMissing(requiredDefaults, addedProps, "u_ColorIntensity", "{ type: Float, default: 1.0, range: [1.0, 10.0] }");
        
        // 光照控制
        AddDefaultIfMissing(requiredDefaults, addedProps, "u_SelfLight", "{ type: Float, default: 0.0, range: [0.0, 1.0] }");
        AddDefaultIfMissing(requiredDefaults, addedProps, "u_SelfLightDir", "{ type: Vector4, default: [0, 1, 0, 1] }");
        
        // 对比度
        AddDefaultIfMissing(requiredDefaults, addedProps, "u_OriginalColor", "{ type: Float, default: 0.0, range: [0.0, 1.0] }");
        AddDefaultIfMissing(requiredDefaults, addedProps, "u_Contrast", "{ type: Float, default: 0.0, range: [0.0, 1.0] }");
        AddDefaultIfMissing(requiredDefaults, addedProps, "u_ContrastScale", "{ type: Float, default: 1.0, range: [0.0, 3.0] }");
        
        // NPR必需属性
        if (hasNPR)
        {
            AddDefaultIfMissing(requiredDefaults, addedProps, "u_MedColor", "{ type: Color, default: [1, 1, 1, 1] }");
            AddDefaultIfMissing(requiredDefaults, addedProps, "u_MedThreshold", "{ type: Float, default: 1.0, range: [0.0, 1.0] }");
            AddDefaultIfMissing(requiredDefaults, addedProps, "u_MedSmooth", "{ type: Float, default: 0.25, range: [0.0, 0.5] }");
            AddDefaultIfMissing(requiredDefaults, addedProps, "u_ShadowColor", "{ type: Color, default: [0, 0, 0, 1] }");
            AddDefaultIfMissing(requiredDefaults, addedProps, "u_ShadowThreshold", "{ type: Float, default: 0.7, range: [0.0, 1.0] }");
            AddDefaultIfMissing(requiredDefaults, addedProps, "u_ShadowSmooth", "{ type: Float, default: 0.2, range: [0.0, 0.5] }");
            AddDefaultIfMissing(requiredDefaults, addedProps, "u_ReflectColor", "{ type: Color, default: [0.02, 0.02, 0.02, 0] }");
            AddDefaultIfMissing(requiredDefaults, addedProps, "u_ReflectThreshold", "{ type: Float, default: 0.4, range: [0.0, 1.0] }");
            AddDefaultIfMissing(requiredDefaults, addedProps, "u_ReflectSmooth", "{ type: Float, default: 0.15, range: [0.0, 0.5] }");
            AddDefaultIfMissing(requiredDefaults, addedProps, "u_GIIntensity", "{ type: Float, default: 0.0, range: [0.0, 2.0] }");
            AddDefaultIfMissing(requiredDefaults, addedProps, "u_SpecularHighlights", "{ type: Float, default: 1.0, range: [0.0, 1.0] }");
            AddDefaultIfMissing(requiredDefaults, addedProps, "u_GGXSpecular", "{ type: Float, default: 1.0, range: [0.0, 1.0] }");
            AddDefaultIfMissing(requiredDefaults, addedProps, "u_SpecularColor", "{ type: Color, default: [1, 1, 1, 1] }");
            AddDefaultIfMissing(requiredDefaults, addedProps, "u_SpecularIntensity", "{ type: Float, default: 1.0 }");
            AddDefaultIfMissing(requiredDefaults, addedProps, "u_SpecularLightOffset", "{ type: Vector4, default: [0, 0, 0, 0] }");
            AddDefaultIfMissing(requiredDefaults, addedProps, "u_SpecularThreshold", "{ type: Float, default: 0.5, range: [0.1, 2.0] }");
            AddDefaultIfMissing(requiredDefaults, addedProps, "u_SpecularSmooth", "{ type: Float, default: 0.5, range: [0.0, 0.5] }");
        }
        
        // Fresnel必需属性
        if (hasFresnel)
        {
            AddDefaultIfMissing(requiredDefaults, addedProps, "u_DirectionalFresnel", "{ type: Float, default: 0.0 }");
            AddDefaultIfMissing(requiredDefaults, addedProps, "u_FresnelColor", "{ type: Color, default: [1, 0, 0, 0] }");
            AddDefaultIfMissing(requiredDefaults, addedProps, "u_fresnelOffset", "{ type: Vector4, default: [0, 0, 0, 0] }");
            AddDefaultIfMissing(requiredDefaults, addedProps, "u_FresnelThreshold", "{ type: Float, default: 0.5, range: [0.0, 1.0] }");
            AddDefaultIfMissing(requiredDefaults, addedProps, "u_FresnelSmooth", "{ type: Float, default: 0.5, range: [0.0, 0.5] }");
            AddDefaultIfMissing(requiredDefaults, addedProps, "u_FresnelIntensity", "{ type: Float, default: 1.0 }");
            AddDefaultIfMissing(requiredDefaults, addedProps, "u_FresnelMetallic", "{ type: Float, default: 1.0, range: [0.0, 1.0] }");
            AddDefaultIfMissing(requiredDefaults, addedProps, "u_FresnelFit", "{ type: Float, default: 1.0, range: [0.0, 1.0] }");
        }
        
        // Rim必需属性
        if (hasRim)
        {
            AddDefaultIfMissing(requiredDefaults, addedProps, "u_RimColor", "{ type: Color, default: [1, 0.5, 0, 1] }");
            AddDefaultIfMissing(requiredDefaults, addedProps, "u_RimPower", "{ type: Float, default: 1.0, range: [0.01, 10.0] }");
            AddDefaultIfMissing(requiredDefaults, addedProps, "u_RimIntensity", "{ type: Float, default: 0.0 }");
            AddDefaultIfMissing(requiredDefaults, addedProps, "u_RimStart", "{ type: Float, default: 0.0, range: [0.0, 1.0] }");
            AddDefaultIfMissing(requiredDefaults, addedProps, "u_RimEnd", "{ type: Float, default: 1.0, range: [0.0, 1.0] }");
            AddDefaultIfMissing(requiredDefaults, addedProps, "u_RimOffset", "{ type: Vector4, default: [0, 0, 0, 0] }");
        }
        
        // HSV必需属性
        if (hasHSV)
        {
            AddDefaultIfMissing(requiredDefaults, addedProps, "u_AdjustHSV", "{ type: Float, default: 0.0, range: [0.0, 1.0] }");
            AddDefaultIfMissing(requiredDefaults, addedProps, "u_AdjustHue", "{ type: Float, default: 0.0, range: [0.0, 360.0] }");
            AddDefaultIfMissing(requiredDefaults, addedProps, "u_AdjustSaturation", "{ type: Float, default: 1.0, range: [0.0, 1.5] }");
            AddDefaultIfMissing(requiredDefaults, addedProps, "u_AdjustValue", "{ type: Float, default: 1.0, range: [0.0, 1.5] }");
        }
        
        // Tonemapping必需属性
        if (hasTonemapping)
        {
            AddDefaultIfMissing(requiredDefaults, addedProps, "u_ToneWeight", "{ type: Float, default: 0.0, range: [0.0, 3.0] }");
            AddDefaultIfMissing(requiredDefaults, addedProps, "u_WhitePoint", "{ type: Float, default: 1.0, range: [0.0, 3.0] }");
        }
        
        // 输出缺失的默认属性
        if (requiredDefaults.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("        // Required Defaults");
            foreach (var line in requiredDefaults)
            {
                sb.AppendLine(line);
            }
        }
    }

    /// <summary>
    /// 如果属性不存在则添加默认值
    /// </summary>
    private static void AddDefaultIfMissing(List<string> defaults, HashSet<string> addedProps, string propName, string definition)
    {
        if (!addedProps.Contains(propName))
        {
            addedProps.Add(propName);
            defaults.Add($"        {propName}: {definition},");
        }
    }

    /// <summary>
    /// 生成顶点着色器
    /// </summary>
    private static void GenerateVertexShader(StringBuilder sb, string shaderName, List<ShaderProperty> properties)
    {
        sb.AppendLine($"#defineGLSL {shaderName}VS");
        sb.AppendLine($"    #define SHADER_NAME {shaderName}");
        sb.AppendLine();
        sb.AppendLine("    #include \"Math.glsl\";");
        sb.AppendLine();
        sb.AppendLine("    #include \"Scene.glsl\";");
        sb.AppendLine("    #include \"SceneFogInput.glsl\";");
        sb.AppendLine();
        sb.AppendLine("    #include \"Camera.glsl\";");
        sb.AppendLine("    #include \"Sprite3DVertex.glsl\";");
        sb.AppendLine();
        sb.AppendLine("    #include \"VertexCommon.glsl\";");
        sb.AppendLine();
        sb.AppendLine("    #include \"PBRVertex.glsl\";");
        sb.AppendLine();
        sb.AppendLine("    void main()");
        sb.AppendLine("    {");
        sb.AppendLine("        Vertex vertex;");
        sb.AppendLine("        getVertexParams(vertex);");
        sb.AppendLine();
        sb.AppendLine("        PixelParams pixel;");
        sb.AppendLine("        initPixelParams(pixel, vertex);");
        sb.AppendLine();
        sb.AppendLine("        gl_Position = getPositionCS(pixel.positionWS);");
        sb.AppendLine("        gl_Position = remapPositionZ(gl_Position);");
        sb.AppendLine();
        sb.AppendLine("    #ifdef FOG");
        sb.AppendLine("        FogHandle(gl_Position.z);");
        sb.AppendLine("    #endif // FOG");
        sb.AppendLine("    }");
        sb.AppendLine("#endGLSL");
        sb.AppendLine();
    }

    /// <summary>
    /// 生成片元着色器
    /// </summary>
    private static void GenerateFragmentShader(StringBuilder sb, string shaderName, List<ShaderProperty> properties,
        bool hasNPR, bool hasIBL, bool hasMatcap, bool hasFresnel, bool hasRim, bool hasHSV, bool hasTonemapping)
    {
        sb.AppendLine($"#defineGLSL {shaderName}FS");
        sb.AppendLine($"    #define SHADER_NAME {shaderName}");
        sb.AppendLine();
        sb.AppendLine("    #include \"Color.glsl\";");
        sb.AppendLine();
        sb.AppendLine("    #include \"Scene.glsl\";");
        sb.AppendLine("    #include \"SceneFog.glsl\";");
        sb.AppendLine();
        sb.AppendLine("    #include \"Camera.glsl\";");
        sb.AppendLine("    #include \"Sprite3DFrag.glsl\";");
        sb.AppendLine();
        sb.AppendLine("    #include \"PBRMetallicFrag.glsl\";");
        sb.AppendLine();
        
        // 生成工具函数
        GenerateUtilityFunctions(sb, hasNPR, hasFresnel, hasRim, hasHSV, hasTonemapping);
        
        // 生成initSurfaceInputs函数
        GenerateInitSurfaceInputs(sb, properties);
        
        // 生成main函数
        GenerateMainFunction(sb, properties, hasNPR, hasIBL, hasMatcap, hasFresnel, hasRim, hasHSV, hasTonemapping);
        
        sb.AppendLine("#endGLSL");
    }

    /// <summary>
    /// 生成工具函数
    /// </summary>
    private static void GenerateUtilityFunctions(StringBuilder sb, bool hasNPR, bool hasFresnel, bool hasRim, bool hasHSV, bool hasTonemapping)
    {
        sb.AppendLine("    //========================================");
        sb.AppendLine("    // Utility Functions");
        sb.AppendLine("    //========================================");
        sb.AppendLine();
        
        // LinearStep函数
        sb.AppendLine("    float LinearStep(float minVal, float maxVal, float value)");
        sb.AppendLine("    {");
        sb.AppendLine("        return clamp((value - minVal) / (maxVal - minVal + 0.0001), 0.0, 1.0);");
        sb.AppendLine("    }");
        sb.AppendLine();
        
        // remapValue函数
        sb.AppendLine("    float remapValue(float x, float t1, float t2, float s1, float s2)");
        sb.AppendLine("    {");
        sb.AppendLine("        return (x - t1) / (t2 - t1) * (s2 - s1) + s1;");
        sb.AppendLine("    }");
        sb.AppendLine();
        
        // Gamma转换
        if (hasTonemapping)
        {
            sb.AppendLine("    vec3 Gamma22ToLinear(vec3 color)");
            sb.AppendLine("    {");
            sb.AppendLine("        return pow(color, vec3(2.2));");
            sb.AppendLine("    }");
            sb.AppendLine();
        }
        
        // IBL旋转函数
        sb.AppendLine("    vec3 rotateVectorXYZ(float rx, float ry, float rz, vec3 v)");
        sb.AppendLine("    {");
        sb.AppendLine("        vec3 rot = vec3(radians(rx), radians(ry), radians(rz));");
        sb.AppendLine("        return rotationByEuler(v, rot);");
        sb.AppendLine("    }");
        sb.AppendLine();
        
        // HSV函数
        if (hasHSV)
        {
            sb.AppendLine("    vec3 RGBtoHSV(vec3 rgb)");
            sb.AppendLine("    {");
            sb.AppendLine("        float cmax = max(rgb.r, max(rgb.g, rgb.b));");
            sb.AppendLine("        float cmin = min(rgb.r, min(rgb.g, rgb.b));");
            sb.AppendLine("        float delta = cmax - cmin;");
            sb.AppendLine("        vec3 hsv = vec3(0.0, 0.0, cmax);");
            sb.AppendLine("        if (delta > 0.0001)");
            sb.AppendLine("        {");
            sb.AppendLine("            hsv.y = delta / cmax;");
            sb.AppendLine("            if (rgb.r == cmax) hsv.x = (rgb.g - rgb.b) / delta;");
            sb.AppendLine("            else if (rgb.g == cmax) hsv.x = 2.0 + (rgb.b - rgb.r) / delta;");
            sb.AppendLine("            else hsv.x = 4.0 + (rgb.r - rgb.g) / delta;");
            sb.AppendLine("            hsv.x /= 6.0;");
            sb.AppendLine("            if (hsv.x < 0.0) hsv.x += 1.0;");
            sb.AppendLine("        }");
            sb.AppendLine("        return hsv;");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("    vec3 HSVtoRGB(vec3 hsv)");
            sb.AppendLine("    {");
            sb.AppendLine("        float h = hsv.x * 6.0;");
            sb.AppendLine("        float s = hsv.y;");
            sb.AppendLine("        float v = hsv.z;");
            sb.AppendLine("        float c = v * s;");
            sb.AppendLine("        float x = c * (1.0 - abs(mod(h, 2.0) - 1.0));");
            sb.AppendLine("        float m = v - c;");
            sb.AppendLine("        vec3 rgb;");
            sb.AppendLine("        if (h < 1.0) rgb = vec3(c, x, 0.0);");
            sb.AppendLine("        else if (h < 2.0) rgb = vec3(x, c, 0.0);");
            sb.AppendLine("        else if (h < 3.0) rgb = vec3(0.0, c, x);");
            sb.AppendLine("        else if (h < 4.0) rgb = vec3(0.0, x, c);");
            sb.AppendLine("        else if (h < 5.0) rgb = vec3(x, 0.0, c);");
            sb.AppendLine("        else rgb = vec3(c, 0.0, x);");
            sb.AppendLine("        return rgb + vec3(m);");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("    vec3 AdjustHSVColor(vec3 color, float hueShift, float satMult, float valMult)");
            sb.AppendLine("    {");
            sb.AppendLine("        vec3 hsv = RGBtoHSV(color);");
            sb.AppendLine("        hsv.x = mod(hsv.x + hueShift / 360.0, 1.0);");
            sb.AppendLine("        hsv.y *= satMult;");
            sb.AppendLine("        hsv.z *= valMult;");
            sb.AppendLine("        return HSVtoRGB(hsv);");
            sb.AppendLine("    }");
            sb.AppendLine();
        }
        
        // Fresnel函数
        if (hasFresnel || hasRim)
        {
            sb.AppendLine("    vec3 FresnelCore(vec3 normal, vec3 viewDir, vec3 rimColor, float power, float intensity, float start, float end, vec3 offset)");
            sb.AppendLine("    {");
            sb.AppendLine("        vec3 N = normalize(normal);");
            sb.AppendLine("        vec3 V = normalize(viewDir) + offset;");
            sb.AppendLine("        float NdotV = 1.0 - clamp(dot(N, V), 0.0, 1.0);");
            sb.AppendLine("        float range = smoothstep(start, end, NdotV);");
            sb.AppendLine("        float fresnel = intensity * pow(range, power);");
            sb.AppendLine("        return clamp(rimColor * fresnel, 0.0, 1.0);");
            sb.AppendLine("    }");
            sb.AppendLine();
        }
        
        // NPR Radiance函数
        if (hasNPR)
        {
            sb.AppendLine("    vec3 CalculateNPRRadiance(vec3 lightDir, vec3 lightColor, vec3 normalWS)");
            sb.AppendLine("    {");
            sb.AppendLine("        float NdotL = dot(normalWS, lightDir);");
            sb.AppendLine("        float halfLambert = NdotL * 0.5 + 0.5;");
            sb.AppendLine("        float smoothMedTone = LinearStep(u_MedThreshold - u_MedSmooth, u_MedThreshold + u_MedSmooth, halfLambert);");
            sb.AppendLine("        vec3 MedToneColor = mix(u_MedColor.rgb, vec3(1.0), smoothMedTone);");
            sb.AppendLine("        float smoothShadow = LinearStep(u_ShadowThreshold - u_ShadowSmooth, u_ShadowThreshold + u_ShadowSmooth, halfLambert);");
            sb.AppendLine("        vec3 ShadowColor = mix(u_ShadowColor.rgb, MedToneColor, smoothShadow);");
            sb.AppendLine("        float smoothReflect = LinearStep(u_ReflectThreshold - u_ReflectSmooth, u_ReflectThreshold + u_ReflectSmooth, halfLambert);");
            sb.AppendLine("        vec3 ReflectColor = mix(u_ReflectColor.rgb, ShadowColor, smoothReflect);");
            sb.AppendLine("        return lightColor * ReflectColor;");
            sb.AppendLine("    }");
            sb.AppendLine();
            
            // NPR Stylized Specular
            sb.AppendLine("    vec3 StylizedSpecular(float perceptualRoughness, vec3 specularColor, vec3 normalWS, vec3 lightDir, vec3 viewDir, vec3 specularLightOffset)");
            sb.AppendLine("    {");
            sb.AppendLine("        vec3 halfDir = SafeNormalize(normalize(lightDir + specularLightOffset) + viewDir);");
            sb.AppendLine("        float NoH = clamp(dot(normalWS, halfDir), 0.0, 1.0);");
            sb.AppendLine("        float LoH = clamp(dot(lightDir, halfDir), 0.0, 1.0);");
            sb.AppendLine("        float roughness = perceptualRoughness * perceptualRoughness;");
            sb.AppendLine("        float roughness2 = roughness * roughness;");
            sb.AppendLine("        float roughness2MinusOne = roughness2 - 1.0;");
            sb.AppendLine("        float normalizationTerm = roughness * 4.0 + 2.0;");
            sb.AppendLine("        float d = NoH * NoH * roughness2MinusOne + 1.00001;");
            sb.AppendLine("        float LoH2 = LoH * LoH;");
            sb.AppendLine("        float specularTerm = roughness2 / ((d * d) * max(0.1, LoH2) * normalizationTerm);");
            sb.AppendLine("        specularTerm = max(0.0, specularTerm - MEDIUMP_FLT_MIN);");
            sb.AppendLine("        specularTerm = min(100.0, specularTerm);");
            sb.AppendLine("        specularTerm = pow(specularTerm, u_SpecularThreshold + u_SpecularSmooth);");
            sb.AppendLine("        float specularStylize = LinearStep(u_SpecularThreshold - u_SpecularSmooth, u_SpecularThreshold + u_SpecularSmooth, specularTerm);");
            sb.AppendLine("        specularTerm = mix(specularStylize, specularTerm, u_GGXSpecular);");
            sb.AppendLine("        return specularTerm * max(vec3(0.0), u_SpecularIntensity * u_SpecularColor.rgb) * specularColor;");
            sb.AppendLine("    }");
            sb.AppendLine();
        }
    }

    /// <summary>
    /// 生成initSurfaceInputs函数
    /// </summary>
    private static void GenerateInitSurfaceInputs(StringBuilder sb, List<ShaderProperty> properties)
    {
        sb.AppendLine("    //========================================");
        sb.AppendLine("    // Initialize Surface Inputs");
        sb.AppendLine("    //========================================");
        sb.AppendLine();
        sb.AppendLine("    void initSurfaceInputs(inout SurfaceInputs inputs, inout PixelParams pixel)");
        sb.AppendLine("    {");
        sb.AppendLine("        inputs.alphaTest = u_AlphaTestValue;");
        sb.AppendLine();
        sb.AppendLine("    #ifdef UV");
        sb.AppendLine("        vec2 uv = transformUV(pixel.uv0, u_TilingOffset);");
        sb.AppendLine("    #else");
        sb.AppendLine("        vec2 uv = vec2(0.0);");
        sb.AppendLine("    #endif");
        sb.AppendLine();
        
        // Diffuse Color
        sb.AppendLine("        // Diffuse Color");
        sb.AppendLine("        inputs.diffuseColor = u_AlbedoColor.rgb;");
        
        // 检查是否有ColorIntensity
        bool hasColorIntensity = false;
        foreach (var prop in properties)
        {
            if (prop.unityName.Contains("ColorIntensity"))
            {
                hasColorIntensity = true;
                break;
            }
        }
        if (hasColorIntensity)
        {
            sb.AppendLine("        inputs.diffuseColor *= u_ColorIntensity;");
        }
        
        sb.AppendLine("        inputs.alpha = u_AlbedoColor.a * u_Alpha;");
        sb.AppendLine();
        sb.AppendLine("    #ifdef COLOR");
        sb.AppendLine("        #ifdef ENABLEVERTEXCOLOR");
        sb.AppendLine("        inputs.diffuseColor *= pixel.vertexColor.xyz;");
        sb.AppendLine("        inputs.alpha *= pixel.vertexColor.a;");
        sb.AppendLine("        #endif");
        sb.AppendLine("    #endif");
        sb.AppendLine();
        sb.AppendLine("    #ifdef ALBEDOTEXTURE");
        sb.AppendLine("        vec4 albedoSampler = texture2D(u_AlbedoTexture, uv);");
        sb.AppendLine("        #ifdef Gamma_u_AlbedoTexture");
        sb.AppendLine("        albedoSampler = gammaToLinear(albedoSampler);");
        sb.AppendLine("        #endif");
        sb.AppendLine("        inputs.diffuseColor *= albedoSampler.rgb;");
        sb.AppendLine("        inputs.alpha *= albedoSampler.a;");
        sb.AppendLine("    #endif");
        sb.AppendLine();
        
        // Normal
        sb.AppendLine("        // Normal");
        sb.AppendLine("        inputs.normalTS = vec3(0.0, 0.0, 1.0);");
        sb.AppendLine("    #ifdef NORMALTEXTURE");
        sb.AppendLine("        vec3 normalSampler = texture2D(u_NormalTexture, uv).rgb;");
        sb.AppendLine("        normalSampler = normalize(normalSampler * 2.0 - 1.0);");
        sb.AppendLine("        normalSampler.y *= -1.0;");
        sb.AppendLine("        inputs.normalTS = normalScale(normalSampler, u_NormalScale);");
        sb.AppendLine("    #endif");
        sb.AppendLine();
        
        // PBR Properties
        sb.AppendLine("        // PBR Properties");
        sb.AppendLine("        inputs.metallic = u_Metallic;");
        sb.AppendLine("        inputs.smoothness = u_Smoothness;");
        sb.AppendLine("        inputs.occlusion = 1.0;");
        sb.AppendLine();
        sb.AppendLine("    #ifdef MAERMAP");
        sb.AppendLine("        vec4 maerSampler = texture2D(u_MAER, uv);");
        sb.AppendLine("        inputs.metallic = remapValue(maerSampler.r, 0.0, 1.0, u_MetallicRemapMin, u_MetallicRemapMax);");
        sb.AppendLine("        inputs.occlusion = mix(1.0, maerSampler.g, u_OcclusionStrength);");
        sb.AppendLine("        // Unity uses reverse remap: lerp(Max, Min, texA)");
        sb.AppendLine("        float remappedSmooth = mix(u_SmoothnessRemapMax, u_SmoothnessRemapMin, maerSampler.a);");
        sb.AppendLine("        inputs.smoothness = remappedSmooth * u_Smoothness;");
        sb.AppendLine("    #endif");
        sb.AppendLine();
        
        // Emission
        sb.AppendLine("        // Emission");
        sb.AppendLine("        inputs.emissionColor = vec3(0.0);");
        sb.AppendLine("    #ifdef EMISSION");
        sb.AppendLine("        inputs.emissionColor = u_EmissionColor.rgb * u_EmissionIntensity;");
        sb.AppendLine("        #ifdef EMISSIONTEXTURE");
        sb.AppendLine("        vec4 emissionSampler = texture2D(u_EmissionTexture, uv);");
        sb.AppendLine("        #ifdef Gamma_u_EmissionTexture");
        sb.AppendLine("        emissionSampler = gammaToLinear(emissionSampler);");
        sb.AppendLine("        #endif");
        sb.AppendLine("        inputs.emissionColor *= emissionSampler.rgb;");
        sb.AppendLine("        #endif");
        sb.AppendLine("    #endif");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    /// <summary>
    /// 生成main函数
    /// </summary>
    private static void GenerateMainFunction(StringBuilder sb, List<ShaderProperty> properties,
        bool hasNPR, bool hasIBL, bool hasMatcap, bool hasFresnel, bool hasRim, bool hasHSV, bool hasTonemapping)
    {
        sb.AppendLine("    //========================================");
        sb.AppendLine("    // Main Fragment Shader");
        sb.AppendLine("    //========================================");
        sb.AppendLine();
        sb.AppendLine("    void main()");
        sb.AppendLine("    {");
        sb.AppendLine("        PixelParams pixel;");
        sb.AppendLine("        getPixelParams(pixel);");
        sb.AppendLine();
        sb.AppendLine("        SurfaceInputs inputs;");
        sb.AppendLine("        initSurfaceInputs(inputs, pixel);");
        sb.AppendLine();
        sb.AppendLine("    #ifdef UV");
        sb.AppendLine("        vec2 uv = transformUV(pixel.uv0, u_TilingOffset);");
        sb.AppendLine("    #else");
        sb.AppendLine("        vec2 uv = vec2(0.0);");
        sb.AppendLine("    #endif");
        sb.AppendLine();
        sb.AppendLine("        // Get view direction and normal");
        sb.AppendLine("        vec3 positionWS = pixel.positionWS;");
        sb.AppendLine("        vec3 viewDir = normalize(u_CameraPos - positionWS);");
        sb.AppendLine();
        sb.AppendLine("        vec3 normalWS = pixel.normalWS;");
        sb.AppendLine("    #ifdef NORMALTEXTURE");
        sb.AppendLine("        normalWS = normalize(pixel.TBN * inputs.normalTS);");
        sb.AppendLine("    #endif");
        sb.AppendLine();
        sb.AppendLine("        // Light direction");
        sb.AppendLine("        vec3 selfLightDir = u_SelfLightDir.xyz;");
        sb.AppendLine("        float lightDirLen = length(selfLightDir);");
        sb.AppendLine("        vec3 lightDir = lightDirLen > 0.001 ? normalize(selfLightDir) : normalize(vec3(0.0, 1.0, 0.0));");
        sb.AppendLine("        vec3 lightColor = vec3(1.0);");
        sb.AppendLine();
        
        // Sample Mask
        sb.AppendLine("        // Sample Mask");
        sb.AppendLine("        vec4 maskTex = vec4(1.0);");
        sb.AppendLine("    #ifdef MASKMAP");
        sb.AppendLine("        maskTex = texture2D(u_Mask, uv);");
        sb.AppendLine("    #endif");
        sb.AppendLine();
        
        // Surface Color Calculation
        sb.AppendLine("        //========================================");
        sb.AppendLine("        // Calculate Surface Color");
        sb.AppendLine("        //========================================");
        sb.AppendLine();
        sb.AppendLine("        vec4 surfaceColor;");
        sb.AppendLine();
        
        if (hasNPR)
        {
            sb.AppendLine("    #ifdef USENPR");
            sb.AppendLine("        // NPR Toon Shading");
            sb.AppendLine("        vec3 specularLightOffset = mix(u_SpecularLightOffset.xyz, u_SelfLightDir.xyz, u_SelfLight);");
            sb.AppendLine("        vec3 radiance = CalculateNPRRadiance(lightDir, lightColor, normalWS);");
            sb.AppendLine("        float ndotl = LinearStep(u_ShadowThreshold - u_ShadowSmooth, u_ShadowThreshold + u_ShadowSmooth, dot(lightDir, normalWS) * 0.5 + 0.5);");
            sb.AppendLine();
            sb.AppendLine("        float oneMinusReflectivity = (1.0 - inputs.metallic) * 0.96;");
            sb.AppendLine("        vec3 diffuseColor = inputs.diffuseColor * oneMinusReflectivity;");
            sb.AppendLine("        vec3 specularColor = mix(vec3(0.04), inputs.diffuseColor, inputs.metallic);");
            sb.AppendLine("        float perceptualRoughness = 1.0 - inputs.smoothness;");
            sb.AppendLine();
            sb.AppendLine("        vec3 finalDiffuse = diffuseColor * radiance * inputs.occlusion;");
            sb.AppendLine("        vec3 gi = diffuseColor * inputs.occlusion * u_GIIntensity;");
            sb.AppendLine();
            sb.AppendLine("        vec3 specular = StylizedSpecular(perceptualRoughness, specularColor, normalWS, lightDir, viewDir, specularLightOffset) * radiance;");
            sb.AppendLine("        specular = mix(vec3(0.0), specular, u_SpecularHighlights);");
            sb.AppendLine();
            sb.AppendLine("        surfaceColor = vec4(finalDiffuse + gi + specular + inputs.emissionColor, inputs.alpha);");
            sb.AppendLine("    #else");
            sb.AppendLine("        // Standard PBR Flow");
            sb.AppendLine("        surfaceColor = PBR_Metallic_Flow(inputs, pixel);");
            sb.AppendLine("    #endif");
        }
        else
        {
            sb.AppendLine("        // Standard PBR Flow");
            sb.AppendLine("        surfaceColor = PBR_Metallic_Flow(inputs, pixel);");
        }
        sb.AppendLine();
        
        // IBL
        if (hasIBL)
        {
            sb.AppendLine("        //========================================");
            sb.AppendLine("        // IBL Reflection");
            sb.AppendLine("        //========================================");
            sb.AppendLine();
            sb.AppendLine("    #ifdef IBLMAP");
            sb.AppendLine("        vec3 reflectVector = reflect(-viewDir, normalWS);");
            sb.AppendLine("        vec3 rotatedReflect = rotateVectorXYZ(u_IBLMapRotateX, u_IBLMapRotateY, u_IBLMapRotateZ, reflectVector);");
            sb.AppendLine("        float iblPerceptualRoughness = 1.0 - inputs.smoothness;");
            sb.AppendLine("        float mip = (1.7 - iblPerceptualRoughness * 0.7) * iblPerceptualRoughness * 8.0;");
            sb.AppendLine("        vec4 iblSampler = textureCube(u_IBLMap, rotatedReflect, mip);");
            sb.AppendLine("        vec3 iblColor = pow(iblSampler.rgb, vec3(u_IBLMapPower));");
            sb.AppendLine("        iblColor *= u_IBLMapColor.rgb * u_IBLMapIntensity * inputs.metallic;");
            sb.AppendLine("        surfaceColor.rgb = mix(surfaceColor.rgb, surfaceColor.rgb + iblColor, maskTex.r);");
            sb.AppendLine("    #endif");
            sb.AppendLine();
        }
        
        // Matcap
        if (hasMatcap)
        {
            sb.AppendLine("        //========================================");
            sb.AppendLine("        // Matcap");
            sb.AppendLine("        //========================================");
            sb.AppendLine();
            sb.AppendLine("    #ifdef MATCAPMAP");
            sb.AppendLine("        float cosmc = cos(radians(u_MatcapAngle));");
            sb.AppendLine("        float sinmc = sin(radians(u_MatcapAngle));");
            sb.AppendLine("        vec3 viewNormal = (u_View * vec4(normalWS, 0.0)).xyz;");
            sb.AppendLine("        vec2 uv_Matcap = viewNormal.xy * 0.5 + 0.5;");
            sb.AppendLine("        uv_Matcap = mat2(cosmc, -sinmc, sinmc, cosmc) * (uv_Matcap - 0.5) + 0.5;");
            sb.AppendLine("        vec4 matcapSampler = texture2D(u_MatcapMap, uv_Matcap);");
            sb.AppendLine("        vec3 matcapColor = matcapSampler.rgb * u_MatcapColor.rgb * u_MatcapStrength * 5.0;");
            sb.AppendLine("        float matcapIntensity = clamp(pow(abs(matcapSampler.r), u_MatcapPow), 0.0, 1.0);");
            sb.AppendLine("        matcapColor *= matcapIntensity;");
            sb.AppendLine("        surfaceColor.rgb = mix(surfaceColor.rgb, surfaceColor.rgb + surfaceColor.rgb * matcapColor, maskTex.g);");
            sb.AppendLine("    #endif");
            sb.AppendLine();
            sb.AppendLine("    #ifdef MATCAPADDMAP");
            sb.AppendLine("        float cosmcAdd = cos(radians(u_MatcapAddAngle));");
            sb.AppendLine("        float sinmcAdd = sin(radians(u_MatcapAddAngle));");
            sb.AppendLine("        vec3 viewNormalAdd = (u_View * vec4(normalWS, 0.0)).xyz;");
            sb.AppendLine("        vec2 uv_MatcapAdd = viewNormalAdd.xy * 0.5 + 0.5;");
            sb.AppendLine("        uv_MatcapAdd = mat2(cosmcAdd, -sinmcAdd, sinmcAdd, cosmcAdd) * (uv_MatcapAdd - 0.5) + 0.5;");
            sb.AppendLine("        vec4 matcapAddSampler = texture2D(u_MatcapAddMap, uv_MatcapAdd);");
            sb.AppendLine("        float matcapAlAdd = clamp(pow(abs(matcapAddSampler.r), u_MatcapAddPow), 0.0, 1.0) * u_MatcapAddStrength;");
            sb.AppendLine("        matcapAlAdd *= maskTex.b;");
            sb.AppendLine("        surfaceColor.rgb = mix(surfaceColor.rgb, surfaceColor.rgb + matcapAddSampler.rgb * u_MatcapAddColor.rgb, matcapAlAdd);");
            sb.AppendLine("    #endif");
            sb.AppendLine();
        }
        
        // Fresnel
        if (hasFresnel)
        {
            sb.AppendLine("        //========================================");
            sb.AppendLine("        // Fresnel");
            sb.AppendLine("        //========================================");
            sb.AppendLine();
            sb.AppendLine("        {");
            sb.AppendLine("            float FresnelIntensity = mix(0.0, u_FresnelIntensity, maskTex.a);");
            sb.AppendLine("            vec3 viewDirWithOffset = normalize(viewDir + u_fresnelOffset.xyz);");
            sb.AppendLine("            vec3 cameraForward = -vec3(u_View[0][2], u_View[1][2], u_View[2][2]);");
            sb.AppendLine("            vec3 viewForwardDir = normalize(cameraForward + u_fresnelOffset.xyz);");
            sb.AppendLine("            float dotNormal = dot(normalWS, viewDirWithOffset);");
            sb.AppendLine("            float dotFit = dot(normalWS, viewForwardDir);");
            sb.AppendLine("            float linearStepInputNormal = 1.0 - clamp(dotNormal, 0.0, 1.0);");
            sb.AppendLine("            float linearStepInputFit = 1.0 - clamp(dotFit, 0.0, 1.0);");
            sb.AppendLine("            float linearStepNormal = LinearStep(u_FresnelThreshold - u_FresnelSmooth, u_FresnelThreshold + u_FresnelSmooth, linearStepInputNormal);");
            sb.AppendLine("            float linearStepFit = LinearStep(u_FresnelThreshold - u_FresnelSmooth, u_FresnelThreshold + u_FresnelSmooth, linearStepInputFit);");
            sb.AppendLine("            float ndotl_fresnel = LinearStep(u_ShadowThreshold - u_ShadowSmooth, u_ShadowThreshold + u_ShadowSmooth, 1.0);");
            sb.AppendLine("            ndotl_fresnel = mix(1.0, ndotl_fresnel, u_DirectionalFresnel);");
            sb.AppendLine("            float vertexColorR = 1.0;");
            sb.AppendLine("        #ifdef COLOR");
            sb.AppendLine("            vertexColorR = pixel.vertexColor.r;");
            sb.AppendLine("        #endif");
            sb.AppendLine("            float fresnelTermNormal = linearStepNormal * max(0.0, FresnelIntensity * vertexColorR) * ndotl_fresnel;");
            sb.AppendLine("            float fresnelTermFit = linearStepFit * max(0.0, FresnelIntensity * vertexColorR) * ndotl_fresnel;");
            sb.AppendLine("            float fresnelTermFinal = mix(fresnelTermNormal, fresnelTermFit, u_FresnelFit);");
            sb.AppendLine("            float roughness = 1.0;");
            sb.AppendLine("            float surfaceReduction = 1.0 / (roughness * roughness + 1.0);");
            sb.AppendLine("            float grazingTerm = 0.04;");
            sb.AppendLine("            vec3 indirectSpecular = vec3(0.1) * mix(1.0, inputs.metallic, u_FresnelMetallic);");
            sb.AppendLine("            vec3 fresnelSpecular = surfaceReduction * indirectSpecular * grazingTerm * fresnelTermFinal * u_FresnelColor.rgb;");
            sb.AppendLine("            surfaceColor.rgb += fresnelSpecular;");
            sb.AppendLine("        }");
            sb.AppendLine();
        }
        
        // Rim
        if (hasRim)
        {
            sb.AppendLine("        //========================================");
            sb.AppendLine("        // Rim Effect");
            sb.AppendLine("        //========================================");
            sb.AppendLine();
            sb.AppendLine("        if (u_RimIntensity > 0.001)");
            sb.AppendLine("        {");
            sb.AppendLine("            vec3 effectRim = FresnelCore(normalWS, viewDir, u_RimColor.rgb, u_RimPower, u_RimIntensity, u_RimStart, u_RimEnd, u_RimOffset.xyz);");
            sb.AppendLine("            surfaceColor.rgb += effectRim;");
            sb.AppendLine("        }");
            sb.AppendLine();
        }
        
        // Contrast
        sb.AppendLine("        //========================================");
        sb.AppendLine("        // Contrast");
        sb.AppendLine("        //========================================");
        sb.AppendLine();
        sb.AppendLine("        vec3 avgColor = vec3(0.5) * u_ContrastScale;");
        sb.AppendLine("        surfaceColor.rgb = mix(mix(vec3(0.5), surfaceColor.rgb, 1.1), surfaceColor.rgb, u_OriginalColor);");
        sb.AppendLine("        surfaceColor.rgb = mix(surfaceColor.rgb, mix(avgColor, surfaceColor.rgb, 1.1), u_Contrast);");
        sb.AppendLine();
        
        // HSV
        if (hasHSV)
        {
            sb.AppendLine("        //========================================");
            sb.AppendLine("        // HSV Adjustment");
            sb.AppendLine("        //========================================");
            sb.AppendLine();
            sb.AppendLine("        if (u_AdjustHSV > 0.5)");
            sb.AppendLine("        {");
            sb.AppendLine("            surfaceColor.rgb = AdjustHSVColor(surfaceColor.rgb, u_AdjustHue, u_AdjustSaturation, u_AdjustValue);");
            sb.AppendLine("        }");
            sb.AppendLine();
        }
        
        // Tonemapping
        if (hasTonemapping)
        {
            sb.AppendLine("        //========================================");
            sb.AppendLine("        // Tonemapping");
            sb.AppendLine("        //========================================");
            sb.AppendLine();
            sb.AppendLine("        if (u_ToneWeight > 0.001)");
            sb.AppendLine("        {");
            sb.AppendLine("            vec3 numerator = surfaceColor.rgb * (6.2 * surfaceColor.rgb + 0.5);");
            sb.AppendLine("            vec3 denominator = surfaceColor.rgb * (6.2 * surfaceColor.rgb + 1.2) + 0.06;");
            sb.AppendLine("            vec3 tonemapped = numerator / denominator;");
            sb.AppendLine("            tonemapped *= u_WhitePoint;");
            sb.AppendLine("            tonemapped = Gamma22ToLinear(tonemapped);");
            sb.AppendLine("            surfaceColor.rgb = mix(surfaceColor.rgb, tonemapped, u_ToneWeight);");
            sb.AppendLine("        }");
            sb.AppendLine();
        }
        
        // Fog and Output
        sb.AppendLine("        //========================================");
        sb.AppendLine("        // Fog and Output");
        sb.AppendLine("        //========================================");
        sb.AppendLine();
        sb.AppendLine("    #ifdef FOG");
        sb.AppendLine("        surfaceColor.rgb = sceneLitFog(surfaceColor.rgb);");
        sb.AppendLine("    #endif");
        sb.AppendLine();
        sb.AppendLine("    #ifdef ALPHATEST");
        sb.AppendLine("        if (surfaceColor.a < u_AlphaTestValue)");
        sb.AppendLine("            discard;");
        sb.AppendLine("    #endif");
        sb.AppendLine();
        sb.AppendLine("        surfaceColor.a = clamp(surfaceColor.a, 0.0, 1.0);");
        sb.AppendLine();
        sb.AppendLine("        gl_FragColor = surfaceColor;");
        sb.AppendLine("        gl_FragColor = outputTransform(gl_FragColor);");
        sb.AppendLine("    }");
    }

    /// <summary>
    /// 获取纹理的默认值
    /// </summary>
    private static string GetDefaultTextureValue(string propName)
    {
        string lowerName = propName.ToLower();

        // Normal/Bump贴图
        if (lowerName.Contains("normal") || lowerName.Contains("bump"))
            return "\"bump\"";

        // Distort贴图：默认黑色（可选效果，默认关闭）
        if (lowerName.Contains("distort"))
            return "\"black\"";

        // Dissolve Amount贴图：默认黑色
        if (lowerName.Contains("dissolveamount") || lowerName.Contains("dissolve_amount"))
            return "\"black\"";

        // 黑色贴图（通常用于遮罩、AO等）
        if (lowerName.Contains("black") || lowerName.Contains("ao") ||
            lowerName.Contains("occlusion") || lowerName.Contains("shadow"))
            return "\"black\"";

        // RimMap特殊处理：默认灰色
        if (lowerName.Contains("rimmap") && !lowerName.Contains("mask"))
            return "\"gray\"";

        // 灰色贴图
        if (lowerName.Contains("gray") || lowerName.Contains("grey") ||
            lowerName.Contains("metallic"))
            return "\"gray\"";

        // 白色贴图（默认）
        return "\"white\"";
    }

    /// <summary>
    /// 获取Range类型的默认值
    /// </summary>
    private static float GetRangeDefaultValue(string propName, float min, float max)
    {
        string lowerName = propName.ToLower();

        // ⭐ RotateAngle特殊处理：默认0.0（不是range的min值-360）
        if (lowerName.Contains("rotateangle") || lowerName.Contains("rotate_angle"))
        {
            if (min <= 0.0f && max >= 0.0f)
                return 0.0f;
            return (min + max) / 2.0f; // 如果range不包含0，使用中间值
        }

        // Distort Strength特殊处理：默认0.0（可选效果，默认关闭）
        if (lowerName.Contains("distort") && lowerName.Contains("strength"))
        {
            return 0.0f;
        }

        // Dissolve Distort Strength特殊处理：默认0.0
        if (lowerName.Contains("dissolvedistortstrength"))
        {
            return 0.0f;
        }

        // EffectMainLightIntensity特殊处理：默认5.0
        if (lowerName.Contains("effectmainlightintensity") || lowerName.Contains("effect_main_light_intensity"))
        {
            if (min <= 5.0f && max >= 5.0f)
                return 5.0f;
            return max; // 如果range不包含5.0，使用最大值
        }

        // RimLevel特殊处理：默认1.0
        if (lowerName.Contains("rimlevel") || lowerName.Contains("rim_level"))
        {
            if (min <= 1.0f && max >= 1.0f)
                return 1.0f;
            return (min + max) / 2.0f;
        }

        // RimSharp特殊处理：默认2.0
        if (lowerName.Contains("rimsharp") || lowerName.Contains("rim_sharp"))
        {
            if (min <= 2.0f && max >= 2.0f)
                return 2.0f;
            return (min + max) / 2.0f;
        }

        // Dissolve/Fade Range特殊处理：默认0.1
        if ((lowerName.Contains("dissolve") || lowerName.Contains("fade") || lowerName.Contains("edge")) &&
            lowerName.Contains("range"))
        {
            if (min <= 0.1f && max >= 0.1f)
                return 0.1f;
            return (min + max) / 2.0f;
        }

        // Multiplier/Intensity类型：默认1.0
        if (lowerName.Contains("multiplier") || lowerName.Contains("intensity") ||
            lowerName.Contains("scale") || lowerName.Contains("strength"))
        {
            // 如果range包含1.0，使用1.0
            if (min <= 1.0f && max >= 1.0f)
                return 1.0f;
            // 否则使用中间值
            return (min + max) / 2.0f;
        }

        // Center/Pivot类型：默认0.5
        if (lowerName.Contains("center") || lowerName.Contains("pivot"))
        {
            if (min <= 0.5f && max >= 0.5f)
                return 0.5f;
            return (min + max) / 2.0f;
        }

        // Threshold类型：默认0.5
        if (lowerName.Contains("threshold"))
        {
            if (min <= 0.5f && max >= 0.5f)
                return 0.5f;
            return (min + max) / 2.0f;
        }

        // Alpha/Occlusion类型：默认1.0
        if (lowerName.Contains("alpha") || lowerName.Contains("occlusion"))
        {
            if (min <= 1.0f && max >= 1.0f)
                return 1.0f;
            return max; // 通常希望完全可见/无遮挡
        }

        // Smoothness/Metallic类型：默认0.0或0.5
        if (lowerName.Contains("smoothness") || lowerName.Contains("metallic") ||
            lowerName.Contains("roughness"))
        {
            return min; // 通常从最小值开始
        }

        // 默认：使用最小值
        return min;
    }

    /// <summary>
    /// 获取Float类型的默认值
    /// </summary>
    private static string GetFloatDefaultValue(string propName)
    {
        string lowerName = propName.ToLower();

        // RotateAngle特殊处理：默认0.0（不是-360）
        if (lowerName.Contains("rotateangle") || lowerName.Contains("rotate_angle"))
        {
            return "0.0";
        }

        // Distort Strength特殊处理：默认0.0（可选效果，默认关闭）
        if (lowerName.Contains("distort") && lowerName.Contains("strength"))
        {
            return "0.0";
        }

        // Multiplier/Intensity/Scale类型：默认1.0
        if (lowerName.Contains("multiplier") || lowerName.Contains("intensity") ||
            lowerName.Contains("scale") || lowerName.Contains("strength"))
        {
            return "1.0";
        }

        // Center/Pivot类型：默认0.5
        if (lowerName.Contains("center") || lowerName.Contains("pivot") ||
            lowerName.Contains("rotatecenter"))
        {
            return "0.5";
        }

        // RemapMax类型：默认1.0
        if (lowerName.Contains("remapmax") || lowerName.Contains("max"))
        {
            return "1.0";
        }

        // Alpha类型：默认1.0（完全不透明）
        if (lowerName.Contains("alpha") && !lowerName.Contains("test"))
        {
            return "1.0";
        }

        // Level类型：默认1.0
        if (lowerName.Contains("level"))
        {
            return "1.0";
        }

        // 特殊的命名约定检查
        // 如果属性名明确表示是标志位（Use/Enable），默认0.0（关闭）
        if (lowerName.StartsWith("use") || lowerName.StartsWith("enable") ||
            lowerName.StartsWith("_use") || lowerName.StartsWith("_enable"))
        {
            return "0.0";
        }

        // 默认：0.0
        return "0.0";
    }

    /// <summary>
    /// 生成uniform行 - 支持Range、默认值等
    /// </summary>
    private static string GenerateUniformLine(ShaderProperty prop)
    {
        string typeStr;
        string defaultValue;
        string rangeStr = "";
        string options = "";

        switch (prop.type)
        {
            case ShaderUtil.ShaderPropertyType.TexEnv:
                // 判断是Cubemap还是2D纹理
                if (prop.isCubemap)
                {
                    typeStr = "TextureCube";
                    defaultValue = "\"white\"";  // Cubemap默认值
                }
                else
                {
                    typeStr = "Texture2D";
                    // 根据纹理名称推断默认值
                    defaultValue = GetDefaultTextureValue(prop.unityName);
                }

                if (!string.IsNullOrEmpty(prop.define))
                {
                    options = $", options: {{ define: \"{prop.define}\" }}";
                }
                return $"        {prop.layaName}: {{ type: {typeStr}, default: {defaultValue}{options} }},";

            case ShaderUtil.ShaderPropertyType.Color:
                typeStr = "Color";
                // 根据属性名推断默认值
                if (prop.unityName.Contains("Shadow"))
                    defaultValue = "[0, 0, 0, 1]";
                else if (prop.unityName.Contains("Emission"))
                    defaultValue = "[0, 0, 0, 0]";
                else if (prop.unityName.Contains("Reflect"))
                    defaultValue = "[0.02, 0.02, 0.02, 0]";
                else if (prop.unityName.Contains("EffectAmbientLightColor") || prop.unityName.Contains("Effect_Ambient_Light"))
                    defaultValue = "[0.5, 0.5, 0.5, 1]";  // Effect环境光默认50%
                else if (prop.unityName.Contains("EffectSSSColor") || prop.unityName.Contains("SSSColor"))
                    defaultValue = "[0, 0, 0, 1]";  // 次表面散射默认关闭
                else if (prop.unityName.Contains("RimColor") && !prop.unityName.Contains("Primary") && !prop.unityName.Contains("Secondary"))
                    defaultValue = "[0.6, 0.8, 1, 1]";  // Rim默认蓝白色
                else
                    defaultValue = "[1, 1, 1, 1]";
                return $"        {prop.layaName}: {{ type: {typeStr}, default: {defaultValue} }},";

            case ShaderUtil.ShaderPropertyType.Range:
                typeStr = "Float";
                // 使用Range的默认值（通常是中间值或最小值）
                float rangeDefault = GetRangeDefaultValue(prop.unityName, prop.rangeMin, prop.rangeMax);

                defaultValue = rangeDefault.ToString("F1");
                rangeStr = $", range: [{prop.rangeMin:F1}, {prop.rangeMax:F1}]";
                return $"        {prop.layaName}: {{ type: {typeStr}, default: {defaultValue}{rangeStr} }},";

            case ShaderUtil.ShaderPropertyType.Float:
                typeStr = "Float";
                // 根据属性名推断默认值
                defaultValue = GetFloatDefaultValue(prop.unityName);
                return $"        {prop.layaName}: {{ type: {typeStr}, default: {defaultValue} }},";

            case ShaderUtil.ShaderPropertyType.Vector:
                typeStr = "Vector4";
                // 根据属性名推断默认值
                if (prop.unityName.Contains("TilingOffset") || prop.unityName.EndsWith("_ST"))
                    defaultValue = "[1, 1, 0, 0]";
                else if (prop.unityName.Contains("EffectMainLightDir") || prop.unityName.Contains("Effect_Main_Light_Dir"))
                    defaultValue = "[-0.5, 0.5, 1.0, 0.0]";  // Effect专用光照方向
                else if (prop.unityName.Contains("LightDir"))
                    defaultValue = "[0, 1, 0, 1]";
                else if (prop.unityName.Contains("PolarControl"))
                    defaultValue = "[0.5, 0.5, 1, 1]";  // PolarCoordinates的默认值
                else
                    defaultValue = "[0, 0, 0, 0]";
                return $"        {prop.layaName}: {{ type: {typeStr}, default: {defaultValue} }},";

            default:
                return null;
        }
    }

    /// <summary>
    /// 导出材质文件
    /// </summary>
    private static void ExportMaterialFile(Material material, Shader shader, string layaShaderName,
        JSONObject jsonData, ResoureMap resoureMap, MaterialFile materialFile = null)
    {
        jsonData.AddField("version", "LAYAMATERIAL:04");
        JSONObject props = new JSONObject(JSONObject.Type.OBJECT);
        jsonData.AddField("props", props);
        
        // 检测材质类型
        LayaMaterialType materialType = DetectMaterialType(shader.name);
        
        // 设置材质类型为自定义Shader名称
        props.AddField("type", layaShaderName);
        
        // 渲染状态
        props.AddField("s_Cull", PropDatasConfig.GetCull(material));
        props.AddField("s_Blend", PropDatasConfig.GetBlend(material));
        props.AddField("s_BlendSrc", PropDatasConfig.GetSrcBlend(material));
        props.AddField("s_BlendDst", PropDatasConfig.GetDstBlend(material));
        props.AddField("alphaTest", PropDatasConfig.GetAlphaTest(material));
        props.AddField("alphaTestValue", PropDatasConfig.GetAlphaTestValue(material));
        props.AddField("renderQueue", material.renderQueue);
        props.AddField("materialRenderMode", PropDatasConfig.GetRenderModule(material));
        
        // 导出纹理
        JSONObject textures = new JSONObject(JSONObject.Type.ARRAY);
        List<string> defines = new List<string>();

        // ⭐ FIX 1/3: Unity Keywords到Laya Defines映射（通用方案）
        // 规则：去掉前缀 _ 和后缀 _ON
        // 示例：_LAYERTYPE_THREE → LAYERTYPE_THREE, _USEDISTORT0_ON → USEDISTORT0
        string[] unityKeywords = material.shaderKeywords;
        if (unityKeywords != null && unityKeywords.Length > 0)
        {
            foreach (string keyword in unityKeywords)
            {
                string layaDefine = ConvertKeywordToDefine(keyword);
                if (!string.IsNullOrEmpty(layaDefine) && !defines.Contains(layaDefine))
                {
                    defines.Add(layaDefine);
                    Debug.Log($"LayaAir3D: Converted keyword '{keyword}' to define '{layaDefine}'");
                }
            }
        }

        // 收集Shader属性用于特性检测
        List<ShaderProperty> shaderProperties = CollectShaderProperties(shader);

        // 根据材质类型和Shader特性添加必要的宏定义
        bool hasNPR = (materialType == LayaMaterialType.PBR || materialType == LayaMaterialType.Custom) && HasNPRFeatures(shaderProperties);
        bool hasEmission = false;

        // ⭐ FIX 3/3: 不再自动为Effect类型添加COLOR和ENABLEVERTEXCOLOR
        // 这些defines应该由Keywords映射生成，或者由shader特征检测生成
        // 粒子shader（如Artist_Effect系列）使用不同的渲染逻辑，不需要这些defines

        // 检测shader源码中是否使用了顶点颜色
        bool usesVertexColor = DetectVertexColorInShader(shader);
        
        int propertyCount = ShaderUtil.GetPropertyCount(shader);
        for (int i = 0; i < propertyCount; i++)
        {
            string propName = ShaderUtil.GetPropertyName(shader, i);
            ShaderUtil.ShaderPropertyType propType = ShaderUtil.GetPropertyType(shader, i);
            
            if (IsInternalProperty(propName))
                continue;
            
            string layaName = ConvertToLayaPropertyName(propName);
            
            switch (propType)
            {
                case ShaderUtil.ShaderPropertyType.TexEnv:
                    ExportTextureProperty(material, propName, layaName, textures, defines, resoureMap, shader, i);

                    // ⭐ FIX 2/3: 导出纹理Tiling/Offset（通用方案）
                    // 规则：Unity的 _MainTex/_BaseMap/_AlbedoTexture → u_TilingOffset
                    //       其他纹理 _XXX → u_XXX_ST
                    // 格式：[scaleX, scaleY, offsetX, offsetY]
                    ExportTextureTilingOffset(material, propName, layaName, props);
                    break;

                case ShaderUtil.ShaderPropertyType.Color:
                    ExportColorProperty(material, propName, layaName, props);
                    // 检测自发光
                    if (propName.Contains("Emission"))
                    {
                        Color emColor = material.GetColor(propName);
                        if (emColor.r > 0 || emColor.g > 0 || emColor.b > 0)
                        {
                            hasEmission = true;
                        }
                    }
                    break;
                    
                case ShaderUtil.ShaderPropertyType.Float:
                case ShaderUtil.ShaderPropertyType.Range:
                    ExportFloatProperty(material, propName, layaName, props);
                    // 检测自发光强度
                    if (propName.Contains("Emission") && propName.Contains("Scale"))
                    {
                        float emScale = material.GetFloat(propName);
                        if (emScale > 0)
                        {
                            hasEmission = true;
                        }
                    }
                    break;
                    
                case ShaderUtil.ShaderPropertyType.Vector:
                    ExportVectorProperty(material, propName, layaName, props);
                    break;
            }
        }
        
        props.AddField("textures", textures);
        
        // 根据材质类型添加功能性宏定义
        if (materialType == LayaMaterialType.PBR || materialType == LayaMaterialType.Custom)
        {
            if (hasNPR && !defines.Contains("USENPR"))
            {
                defines.Add("USENPR");
            }
            
            if (hasEmission && !defines.Contains("EMISSION"))
            {
                defines.Add("EMISSION");
            }
            
            // 如果使用了顶点颜色，添加COLOR和ENABLEVERTEXCOLOR宏
            if (usesVertexColor)
            {
                if (!defines.Contains("COLOR"))
                {
                    defines.Add("COLOR");
                }
                if (!defines.Contains("ENABLEVERTEXCOLOR"))
                {
                    defines.Add("ENABLEVERTEXCOLOR");
                }
            }
        }
        else if (materialType == LayaMaterialType.BLINNPHONG || materialType == LayaMaterialType.Unlit)
        {
            // BlinnPhong和Unlit类型：如果使用了顶点颜色，添加COLOR和ENABLEVERTEXCOLOR宏
            if (usesVertexColor)
            {
                if (!defines.Contains("COLOR"))
                {
                    defines.Add("COLOR");
                }
                if (!defines.Contains("ENABLEVERTEXCOLOR"))
                {
                    defines.Add("ENABLEVERTEXCOLOR");
                }
            }
        }
        // ⭐ Note: Effect类型（粒子）的宏定义由Keywords映射自动生成，不再手动添加

        // ⭐ FIX 4/4: 粒子Mesh渲染模式修复 - 添加RENDERMODE_MESH define
        // 当粒子系统使用Mesh渲染模式时，shader需要v_MeshColor变量
        // 必须添加RENDERMODE_MESH宏来启用条件编译块
        if (materialFile != null && materialFile.IsParticleMeshMode())
        {
            if (!defines.Contains("RENDERMODE_MESH"))
            {
                defines.Add("RENDERMODE_MESH");
                Debug.Log($"LayaAir3D: Added RENDERMODE_MESH define for particle mesh rendering mode");
            }
        }

        // 添加宏定义
        JSONObject definesArray = new JSONObject(JSONObject.Type.ARRAY);
        foreach (string define in defines)
        {
            definesArray.Add(define);
        }
        props.AddField("defines", definesArray);
        
        Debug.Log($"LayaAir3D: Exported material '{material.name}' as type '{layaShaderName}' (MaterialType: {materialType})");
    }

    /// <summary>
    /// 导出纹理属性
    /// </summary>
    private static void ExportTextureProperty(Material material, string propName, string layaName, 
        JSONObject textures, List<string> defines, ResoureMap resoureMap, Shader shader = null, int propertyIndex = -1)
    {
        if (!material.HasProperty(propName)) return;
        
        Texture tex = material.GetTexture(propName);
        if (tex == null) return;
        
        string texPath = AssetDatabase.GetAssetPath(tex.GetInstanceID());
        if (string.IsNullOrEmpty(texPath)) return;
        if (ResoureMap.IsBuiltinResource(texPath)) return;
        
        bool isNormal = propName.ToLower().Contains("normal") || propName.ToLower().Contains("bump");
        bool isCubemap = false;
        
        // 检测是否是Cubemap
        if (shader != null && propertyIndex >= 0)
        {
            var texDim = ShaderUtil.GetTexDim(shader, propertyIndex);
            if (texDim == UnityEngine.Rendering.TextureDimension.Cube)
            {
                isCubemap = true;
            }
        }
        
        // 也通过属性名检测Cubemap
        if (propName.ToLower().Contains("ibl") || propName.ToLower().Contains("cube"))
        {
            isCubemap = true;
        }
        
        // Cubemap需要特殊处理
        if (isCubemap)
        {
            // TODO: 导出Cubemap纹理
            // 目前Cubemap导出需要额外实现
            Debug.LogWarning($"LayaAir3D: Cubemap texture export not fully supported yet: {propName}");
            
            // 添加宏定义
            string define = GenerateTextureDefine(propName);
            if (!defines.Contains(define))
            {
                defines.Add(define);
            }
            return;
        }
        
        TextureFile textureFile = resoureMap.GetTextureFile(tex, isNormal);
        if (textureFile != null)
        {
            textures.Add(textureFile.jsonObject(layaName));
            
            // 添加宏定义
            string define = GenerateTextureDefine(propName);
            if (!defines.Contains(define))
            {
                defines.Add(define);
            }
        }
    }

    /// <summary>
    /// 导出纹理Tiling/Offset（通用方案）
    /// Unity纹理的Scale和Offset映射为Laya的_ST uniform
    /// 规则：_MainTex/_BaseMap/_AlbedoTexture → u_TilingOffset
    ///       其他纹理 _XXX → u_XXX_ST
    /// 格式：[scaleX, scaleY, offsetX, offsetY]
    /// </summary>
    private static void ExportTextureTilingOffset(Material material, string unityPropName, string layaPropName, JSONObject props)
    {
        if (!material.HasProperty(unityPropName))
            return;

        // 获取纹理的Tiling和Offset
        Vector2 scale = material.GetTextureScale(unityPropName);
        Vector2 offset = material.GetTextureOffset(unityPropName);

        // 确定Laya属性名
        string tilingOffsetName;

        // 主纹理使用 u_TilingOffset
        if (unityPropName == "_MainTex" || unityPropName == "_BaseMap" || unityPropName == "_AlbedoTexture")
        {
            tilingOffsetName = "u_TilingOffset";
        }
        else
        {
            // 其他纹理使用 u_XXX_ST
            // 去掉前缀 _，添加后缀 _ST
            string texName = unityPropName.TrimStart('_');
            tilingOffsetName = "u_" + texName + "_ST";
        }

        // 添加到材质数据
        JSONObject tilingOffsetValue = new JSONObject(JSONObject.Type.ARRAY);
        tilingOffsetValue.Add(scale.x);
        tilingOffsetValue.Add(scale.y);
        tilingOffsetValue.Add(offset.x);
        tilingOffsetValue.Add(offset.y);

        props.AddField(tilingOffsetName, tilingOffsetValue);

        Debug.Log($"LayaAir3D: Exported texture tiling/offset '{unityPropName}' as '{tilingOffsetName}': [{scale.x}, {scale.y}, {offset.x}, {offset.y}]");
    }

    /// <summary>
    /// 导出颜色属性
    /// </summary>
    private static void ExportColorProperty(Material material, string propName, string layaName, JSONObject props)
    {
        if (!material.HasProperty(propName)) return;
        
        Color color = material.GetColor(propName);
        
        JSONObject colorValue = new JSONObject(JSONObject.Type.ARRAY);
        colorValue.Add(color.r);
        colorValue.Add(color.g);
        colorValue.Add(color.b);
        colorValue.Add(color.a);
        props.AddField(layaName, colorValue);
    }

    /// <summary>
    /// 导出浮点属性
    /// </summary>
    private static void ExportFloatProperty(Material material, string propName, string layaName, JSONObject props)
    {
        if (!material.HasProperty(propName)) return;
        
        float value = material.GetFloat(propName);
        props.AddField(layaName, value);
    }

    /// <summary>
    /// 导出向量属性
    /// </summary>
    private static void ExportVectorProperty(Material material, string propName, string layaName, JSONObject props)
    {
        if (!material.HasProperty(propName)) return;
        
        Vector4 vec = material.GetVector(propName);
        
        JSONObject vecValue = new JSONObject(JSONObject.Type.ARRAY);
        vecValue.Add(vec.x);
        vecValue.Add(vec.y);
        vecValue.Add(vec.z);
        vecValue.Add(vec.w);
        props.AddField(layaName, vecValue);
    }

    /// <summary>
    /// 检测Shader是否使用了顶点颜色
    /// </summary>
    private static bool DetectVertexColorInShader(Shader shader)
    {
        if (shader == null) return false;
        
        try
        {
            // 获取Shader源码路径
            string shaderPath = AssetDatabase.GetAssetPath(shader);
            if (string.IsNullOrEmpty(shaderPath)) return false;
            
            // 读取Shader源码
            string sourceCode = "";
            if (File.Exists(shaderPath))
            {
                sourceCode = File.ReadAllText(shaderPath);
            }
            
            if (string.IsNullOrEmpty(sourceCode)) return false;
            
            // 检查结构体中是否有COLOR语义
            if (Regex.IsMatch(sourceCode, @":\s*COLOR\d*\s*;", RegexOptions.IgnoreCase))
                return true;
            
            // 检查代码中是否使用了顶点颜色相关的变量
            string[] colorPatterns = new string[]
            {
                @"\bv\.color\b",
                @"\bi\.color\b",
                @"\bo\.color\b",
                @"\bvertex\.color\b",
                @"\binput\.color\b",
                @"\bIN\.color\b",
                @"\bOUT\.color\b",
                @"\bv\.vcolor\b",
                @"\bi\.vcolor\b",
                @"\bvertexColor\b",
                @"\bv_VertexColor\b",
                @"\ba_Color\b",
                @"\bappdata_full\b"  // Unity内置结构体，包含顶点颜色
            };
            
            foreach (var pattern in colorPatterns)
            {
                if (Regex.IsMatch(sourceCode, pattern, RegexOptions.IgnoreCase))
                    return true;
            }
            
            return false;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"LayaAir3D: Failed to detect vertex color usage in shader: {e.Message}");
            return false;
        }
    }
}

/// <summary>
/// Shader文件 - 用于导出.shader文件
/// </summary>
internal class ShaderFile : FileData
{
    private string content;
    
    public ShaderFile(string path, string content) : base(path)
    {
        this.content = content;
    }
    
    public override void SaveFile(Dictionary<string, FileData> exportFiles)
    {
        try
        {
            string directory = Path.GetDirectoryName(this.outPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            File.WriteAllText(this.outPath, content, Encoding.UTF8);
            Debug.Log($"LayaAir3D: Saved shader file: {this.outPath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"LayaAir3D: Failed to save shader file: {e.Message}");
        }
    }
}
