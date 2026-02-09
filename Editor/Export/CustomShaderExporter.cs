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
    public static void WriteAutoCustomShaderMaterial(Material material, JSONObject jsonData, ResoureMap resoureMap)
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
            ExportShaderFile(shader, layaShaderName, resoureMap);
            exportedShaders.Add(shaderName);
        }

        // 导出材质文件
        ExportMaterialFile(material, shader, layaShaderName, jsonData, resoureMap);
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
    private static void ExportShaderFile(Shader shader, string layaShaderName, ResoureMap resoureMap)
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
            // 有源代码，进行HLSL到GLSL的转换
            shaderContent = ConvertUnityShaderToLaya(layaShaderName, properties, shader.name, shaderSourceCode);
        }
        else
        {
            // 没有源代码，使用模板生成
            shaderContent = GenerateShaderFileContent(layaShaderName, properties, shader.name);
        }
        
        // 创建Shader文件
        string outputPath = layaShaderName + ".shader";
        ShaderFile shaderFile = new ShaderFile(outputPath, shaderContent);
        resoureMap.AddExportFile(shaderFile);
        
        Debug.Log($"LayaAir3D: Generated shader file: {outputPath} (ShaderType detected from: {shader.name})");
    }

    #region HLSL to GLSL Converter

    /// <summary>
    /// 将Unity Shader源代码转换为LayaAir Shader
    /// </summary>
    private static string ConvertUnityShaderToLaya(string layaShaderName, List<ShaderProperty> properties, 
        string unityShaderName, string sourceCode)
    {
        StringBuilder sb = new StringBuilder();
        
        // 检测材质类型和ShaderType
        LayaMaterialType materialType = DetectMaterialType(unityShaderName);
        LayaShaderType shaderType;
        
        if (materialType == LayaMaterialType.Custom)
        {
            shaderType = DetectCustomShaderType(unityShaderName, properties);
        }
        else
        {
            shaderType = GetShaderTypeFromMaterialType(materialType);
        }
        
        string shaderTypeStr = GetShaderTypeString(shaderType);
        
        Debug.Log($"LayaAir3D: Converting shader '{unityShaderName}' - MaterialType: {materialType}, ShaderType: {shaderType}");
        
        // 解析Unity Shader代码
        ShaderParseResult parseResult = ParseUnityShader(sourceCode);
        
        // 检测是否是粒子Billboard模式
        // Effect类型的shader或名称包含particle/effect的shader使用Billboard模式
        parseResult.isParticleBillboard = (shaderType == LayaShaderType.Effect) || 
            materialType == LayaMaterialType.PARTICLESHURIKEN ||
            unityShaderName.ToLower().Contains("particle") ||
            unityShaderName.ToLower().Contains("effect");
        
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
        
        // attributeMap - 粒子shader需要声明粒子系统的顶点属性
        if (parseResult.isParticleBillboard)
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
        
        return result;
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
    }

    /// <summary>
    /// 从解析结果生成defines
    /// </summary>
    private static void GenerateDefinesFromParseResult(StringBuilder sb, ShaderParseResult parseResult)
    {
        HashSet<string> addedDefines = new HashSet<string>();
        
        // 粒子shader使用TINTCOLOR/ADDTIVEFOG（参考Particle.shader模板），非粒子使用COLOR/ENABLEVERTEXCOLOR
        if (parseResult.isParticleBillboard)
        {
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
            sb.AppendLine("    #include \"Camera.glsl\";");
            sb.AppendLine("    #include \"particleShuriKenSpriteVS.glsl\";");
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
        }
        
        Debug.Log($"[VS] allVaryings.Count = {allVaryings.Count}");
        Debug.Log($"[VS] varyingDeclarations length = {parseResult.varyingDeclarations.Length}");
        Debug.Log($"[VS] varyingDeclarations = \n{parseResult.varyingDeclarations}");
        
        sb.Append(parseResult.varyingDeclarations);
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
        
        // 粒子shader不使用Vertex结构体，直接使用粒子attribute
        if (!parseResult.isParticleBillboard)
        {
            sb.AppendLine("        Vertex vertex;");
            sb.AppendLine("        getVertexParams(vertex);");
            sb.AppendLine();
        }
        
        // 添加转换后的顶点着色器代码
        if (!string.IsNullOrEmpty(convertedVertCode))
        {
            sb.AppendLine(IndentCode(convertedVertCode, "        "));
        }
        else
        {
            // 默认顶点处理
            GenerateDefaultVertexCode(sb, parseResult);
        }
        
        sb.AppendLine();
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
        if (!varyings.ContainsKey("v_Texcoord0"))
            varyings["v_Texcoord0"] = "vec2";
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
            
        // 按名称排序以确保VS和FS输出顺序一致
        var sortedVaryings = varyings.OrderBy(kvp => kvp.Key).ToList();
        
        foreach (var kvp in sortedVaryings)
        {
            sb.AppendLine($"    varying {kvp.Value} {kvp.Key};");
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
        sb.AppendLine("        mat4 worldMat = getWorldMatrix();");
        sb.AppendLine("        vec4 pos = worldMat * vec4(vertex.positionOS, 1.0);");
        sb.AppendLine("        v_PositionWS = pos.xyz / pos.w;");
        sb.AppendLine();
        sb.AppendLine("    #ifdef UV");
        sb.AppendLine("        v_Texcoord0 = transformUV(vertex.texCoord0, u_TilingOffset);");
        sb.AppendLine("    #endif");
        sb.AppendLine();
        sb.AppendLine("    #ifdef COLOR");
        sb.AppendLine("        v_VertexColor = vertex.vertexColor;");
        sb.AppendLine("    #endif");
        sb.AppendLine();
        sb.AppendLine("        gl_Position = getPositionCS(v_PositionWS);");
        sb.AppendLine("        v_PositionCS = gl_Position;");
        sb.AppendLine("        v_ScreenPos = gl_Position * 0.5 + vec4(0.5 * gl_Position.w);");
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
            code = Regex.Replace(code, @"\bgetPositionCS\s*\(\s*([^)]+)\s*\)", "(u_Projection * u_View * vec4($1, 1.0))");
            
            // initPixelParams, getVertexParams 等函数在粒子中不存在
            code = Regex.Replace(code, @"\bgetVertexParams\s*\(\s*\w+\s*\)\s*;?", "// getVertexParams not available in particle shader");
            code = Regex.Replace(code, @"\binitPixelParams\s*\(\s*\w+\s*,\s*\w+\s*\)\s*;?", "// initPixelParams not available in particle shader");
            
            // ========== Unity粒子变量到LayaAir粒子变量映射 ==========
            
            // _Time -> u_CurrentTime (粒子系统使用u_CurrentTime)
            code = Regex.Replace(code, @"\bu_Time\b", "u_CurrentTime"); // 通用转换后的u_Time也要改
            code = Regex.Replace(code, @"\b_Time\.y\b", "u_CurrentTime");
            code = Regex.Replace(code, @"\b_Time\.x\b", "(u_CurrentTime * 0.05)");
            code = Regex.Replace(code, @"\b_Time\.z\b", "(u_CurrentTime * 2.0)");
            code = Regex.Replace(code, @"\b_Time\.w\b", "(u_CurrentTime * 3.0)");
            
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
            return $"(u_ViewProjection * u_WorldMat * {args})";
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
                
                // 移除可能的前缀
                if (texName.StartsWith("_"))
                    texName = texName.Substring(1);
                
                // 特殊纹理名映射（参考Unity2Laya_ShaderMapping.md：主纹理用u_TilingOffset）
                string stName;
                if (texName == "MainTex" || texName == "BaseMap")
                    stName = "u_TilingOffset";  // 粒子/Effect主纹理统一用u_TilingOffset
                else if (texName == "BumpMap" || texName == "NormalMap" || texName == "u_NormalMap")
                    stName = "u_NormalTexture_ST";
                else if (texName == "FadeEdgeTexture")
                    stName = "u_FadeEdgeTexture_ST";
                else if (texName == "DissolveDistortTex")
                    stName = "u_DissolveDistortTex_ST";
                else if (texName == "GradientMapTex0" || texName == "GradientMapTex0Map")
                    stName = "u_GradientMapTex0_ST";
                else
                    stName = $"u_{texName}_ST";
                
                return $"({uv} * {stName}.xy + {stName}.zw)";
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
            
            // 粒子shader中的时间变量
            code = Regex.Replace(code, @"\bu_Time\b", "u_CurrentTime");
            code = Regex.Replace(code, @"\b_Time\.y\b", "u_CurrentTime");
            code = Regex.Replace(code, @"\b_Time\.x\b", "(u_CurrentTime * 0.05)");
            code = Regex.Replace(code, @"\b_Time\.z\b", "(u_CurrentTime * 2.0)");
            code = Regex.Replace(code, @"\b_Time\.w\b", "(u_CurrentTime * 3.0)");
            
            // 相机位置
            code = Regex.Replace(code, @"\b_WorldSpaceCameraPos\b", "u_CameraPos");
            
            // 主纹理_ST：粒子主纹理统一用u_TilingOffset（参考Unity2Laya_ShaderMapping.md）
            code = Regex.Replace(code, @"\bu_MainTex_ST\b", "u_TilingOffset");
            code = Regex.Replace(code, @"\bu_AlbedoTexture_ST\b", "u_TilingOffset");
            
            // 法线贴图_ST：Unity可能用u_NormalMap_ST，Laya粒子模板用u_NormalTexture_ST
            code = Regex.Replace(code, @"\bu_NormalMap_ST\b", "u_NormalTexture_ST");
            
            // Varying映射：粒子颜色用v_Color（参考：i.vcolor->v_Color）
            code = Regex.Replace(code, @"\bv_VertexColor\b", "v_Color");
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
        
        return code;
    }

    /// <summary>
    /// HLSL到GLSL的通用代码转换
    /// </summary>
    private static string ConvertHLSLToGLSL(string hlslCode)
    {
        string code = hlslCode;
        
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
        // Unity _Time: (t/20, t, t*2, t*3)
        // LayaAir u_Time: float 场景运行时间（秒）
        code = Regex.Replace(code, @"_Time\.y", "u_Time");      // t -> u_Time
        code = Regex.Replace(code, @"_Time\.x", "(u_Time * 0.05)"); // t/20
        code = Regex.Replace(code, @"_Time\.z", "(u_Time * 2.0)");  // t*2
        code = Regex.Replace(code, @"_Time\.w", "(u_Time * 3.0)");  // t*3
        code = Regex.Replace(code, @"_Time\.g", "u_Time");      // t
        code = Regex.Replace(code, @"_Time\.r", "(u_Time * 0.05)");
        code = Regex.Replace(code, @"_Time\.b", "(u_Time * 2.0)");
        code = Regex.Replace(code, @"_Time\.a", "(u_Time * 3.0)");
        code = Regex.Replace(code, @"\b_Time\b", "u_Time");
        
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
        code = Regex.Replace(code, @"(?<![a-zA-Z0-9_])_([A-Z]\w*)\b", "u_$1");
        
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
        // 清理多余空行
        // ============================================
        code = Regex.Replace(code, @"\n\s*\n\s*\n", "\n\n");
        
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
        // 检查括号匹配
        int parenCount = 0;
        int braceCount = 0;
        int bracketCount = 0;
        
        foreach (char c in code)
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
        
        if (parenCount != 0)
        {
            Debug.LogWarning($"LayaAir3D: GLSL code has unbalanced parentheses: {parenCount}");
        }
        if (braceCount != 0)
        {
            Debug.LogWarning($"LayaAir3D: GLSL code has unbalanced braces: {braceCount}");
        }
        if (bracketCount != 0)
        {
            Debug.LogWarning($"LayaAir3D: GLSL code has unbalanced brackets: {bracketCount}");
        }
        
        // 移除多余的空语句
        code = Regex.Replace(code, @";\s*;", ";");
        
        // 移除空的if语句
        code = Regex.Replace(code, @"if\s*\([^)]*\)\s*;", "");
        
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
        
        // 修复缺少分号的语句（在行尾的赋值/声明后）
        // 匹配：变量赋值后没有分号，后面紧跟换行
        code = Regex.Replace(code, @"(\w+\s*=\s*[^;{}\n]+)(\s*\n\s*(?![\s{]))", "$1;$2");
        
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
    /// </summary>
    private static LayaShaderType DetectCustomShaderType(string shaderName, List<ShaderProperty> properties)
    {
        string lowerName = shaderName.ToLower();
        
        // 检测是否是特效类Shader（用于MeshRenderer的特效，不是粒子）
        // 这些Shader通常用于特效网格，应该使用Effect类型
        if (lowerName.Contains("effect") || 
            lowerName.Contains("fx") ||
            lowerName.Contains("vfx") ||
            lowerName.Contains("additive"))
        {
            return LayaShaderType.Effect;
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
    private static string GenerateShaderFileContent(string shaderName, List<ShaderProperty> properties, string unityShaderName = null)
    {
        // 检测材质类型
        string shaderNameForDetection = unityShaderName ?? shaderName;
        LayaMaterialType materialType = DetectMaterialType(shaderNameForDetection);
        LayaShaderType shaderType = GetShaderTypeFromMaterialType(materialType);
        
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
        sb.AppendLine("        u_Time: { type: Vector4, default: [0, 0, 0, 0] },");
        
        HashSet<string> addedProps = new HashSet<string> { "u_AlphaTestValue", "u_TilingOffset", "u_Time" };
        
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
        
        // uniformMap - 粒子系统的uniforms（参考Particle.shader模板）
        sb.AppendLine("    uniformMap:{");
        sb.AppendLine("        u_Tintcolor: { type: Color, default: [1, 1, 1, 1] },");
        sb.AppendLine("        u_texture: { type: Texture2D, default: \"white\", options: { define: \"DIFFUSEMAP\" } },");
        sb.AppendLine("        u_TilingOffset: { type: Vector4, default: [1, 1, 1, 1] },");
        sb.AppendLine("    },");
        
        // defines - 粒子系统的defines（参考Particle.shader模板）
        sb.AppendLine("    defines: {");
        sb.AppendLine("        TINTCOLOR: { type: bool, default: true },");
        sb.AppendLine("        ADDTIVEFOG: { type: bool, default: true },");
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
        sb.AppendLine();
        
        // 常量（参考Particle.shader模板）
        sb.AppendLine("const vec4 c_ColorSpace = vec4(4.59479380, 4.59479380, 4.59479380, 2.0);");
        sb.AppendLine();
        
        // varying声明（与VS保持一致）
        sb.AppendLine("varying vec4 v_Color;");
        sb.AppendLine("varying vec2 v_TextureCoordinate;");
        sb.AppendLine();
        sb.AppendLine("#ifdef RENDERMODE_MESH");
        sb.AppendLine("varying vec4 v_MeshColor;");
        sb.AppendLine("#endif");
        sb.AppendLine();
        
        // main函数（参考Particle.shader模板）
        sb.AppendLine("void main()");
        sb.AppendLine("{");
        sb.AppendLine("    vec4 color;");
        sb.AppendLine("#ifdef RENDERMODE_MESH");
        sb.AppendLine("    color = v_MeshColor;");
        sb.AppendLine("#else");
        sb.AppendLine("    color = vec4(1.0);");
        sb.AppendLine("#endif");
        sb.AppendLine();
        sb.AppendLine("#ifdef DIFFUSEMAP");
        sb.AppendLine("    vec4 colorT = texture2D(u_texture, v_TextureCoordinate);");
        sb.AppendLine("    #ifdef Gamma_u_texture");
        sb.AppendLine("    colorT = gammaToLinear(colorT);");
        sb.AppendLine("    #endif");
        sb.AppendLine("    #ifdef TINTCOLOR");
        sb.AppendLine("    color *= colorT * u_Tintcolor * c_ColorSpace * v_Color;");
        sb.AppendLine("    #else");
        sb.AppendLine("    color *= colorT * v_Color;");
        sb.AppendLine("    #endif");
        sb.AppendLine("#else");
        sb.AppendLine("    #ifdef TINTCOLOR");
        sb.AppendLine("    color *= u_Tintcolor * c_ColorSpace * v_Color;");
        sb.AppendLine("    #else");
        sb.AppendLine("    color *= v_Color;");
        sb.AppendLine("    #endif");
        sb.AppendLine("#endif");
        sb.AppendLine();
        sb.AppendLine("#ifdef ALPHATEST");
        sb.AppendLine("    if (color.a < u_AlphaTestValue)");
        sb.AppendLine("    {");
        sb.AppendLine("        discard;");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("#endGLSL");
        sb.AppendLine();
    }

    /// <summary>
    /// 生成粒子Billboard顶点着色器（参考Particle.shader模板）
    /// 粒子系统不使用Vertex结构体，直接使用粒子attribute计算位置
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
        
        // varying声明（参考Particle.shader模板）
        sb.AppendLine("#ifdef RENDERMODE_MESH");
        sb.AppendLine("varying vec4 v_MeshColor;");
        sb.AppendLine("#endif");
        sb.AppendLine();
        sb.AppendLine("varying vec4 v_Color;");
        sb.AppendLine("varying vec2 v_TextureCoordinate;");
        sb.AppendLine();
        
        // UV变换函数
        sb.AppendLine("vec2 TransformUV(vec2 texcoord, vec4 tilingOffset)");
        sb.AppendLine("{");
        sb.AppendLine("    vec2 transTexcoord = vec2(texcoord.x, texcoord.y - 1.0) * tilingOffset.xy + vec2(tilingOffset.z, -tilingOffset.w);");
        sb.AppendLine("    transTexcoord.y += 1.0;");
        sb.AppendLine("    return transTexcoord;");
        sb.AppendLine("}");
        sb.AppendLine();
        
        // main函数（简化版，参考Particle.shader模板结构）
        sb.AppendLine("void main()");
        sb.AppendLine("{");
        sb.AppendLine("    float age = u_CurrentTime - a_DirectionTime.w;");
        sb.AppendLine("    float normalizedAge = age / a_ShapePositionStartLifeTime.w;");
        sb.AppendLine("    vec3 lifeVelocity = vec3(0.0);");
        sb.AppendLine("    if (normalizedAge < 1.0)");
        sb.AppendLine("    {");
        sb.AppendLine("        vec3 startVelocity = a_DirectionTime.xyz * a_StartSpeed;");
        sb.AppendLine("        vec3 gravityVelocity = u_Gravity * age;");
        sb.AppendLine();
        sb.AppendLine("        vec4 worldRotation;");
        sb.AppendLine("        if (u_SimulationSpace == 0)");
        sb.AppendLine("            worldRotation = a_SimulationWorldRotation;");
        sb.AppendLine("        else");
        sb.AppendLine("            worldRotation = u_WorldRotation;");
        sb.AppendLine();
        sb.AppendLine("        // drag");
        sb.AppendLine("        vec3 dragData = a_DirectionTime.xyz * mix(u_DragConstanct.x, u_DragConstanct.y, a_Random0.x);");
        sb.AppendLine("        // 计算粒子位置");
        sb.AppendLine("        vec3 center = computeParticlePosition(startVelocity, lifeVelocity, age, normalizedAge, gravityVelocity, worldRotation, dragData);");
        sb.AppendLine();
        sb.AppendLine("#ifdef SPHERHBILLBOARD");
        sb.AppendLine("        vec2 corner = a_CornerTextureCoordinate.xy;");
        sb.AppendLine("        vec3 cameraUpVector = normalize(u_CameraUp);");
        sb.AppendLine("        vec3 sideVector = normalize(cross(u_CameraDirection, cameraUpVector));");
        sb.AppendLine("        vec3 upVector = normalize(cross(sideVector, u_CameraDirection));");
        sb.AppendLine("        corner *= computeParticleSizeBillbard(a_StartSize.xy, normalizedAge);");
        sb.AppendLine("        float c = cos(a_StartRotation0.x);");
        sb.AppendLine("        float s = sin(a_StartRotation0.x);");
        sb.AppendLine("        mat2 rotation = mat2(c, -s, s, c);");
        sb.AppendLine("        corner = rotation * corner;");
        sb.AppendLine("        center += u_SizeScale.xzy * (corner.x * sideVector + corner.y * upVector);");
        sb.AppendLine("#endif");
        sb.AppendLine();
        sb.AppendLine("#ifdef RENDERMODE_MESH");
        sb.AppendLine("        vec3 size = computeParticleSizeMesh(a_StartSize, normalizedAge);");
        sb.AppendLine("        center += rotationByQuaternions(u_SizeScale * a_MeshPosition * size, worldRotation);");
        sb.AppendLine("        v_MeshColor = a_MeshColor;");
        sb.AppendLine("#endif");
        sb.AppendLine();
        sb.AppendLine("        gl_Position = u_Projection * u_View * vec4(center, 1.0);");
        sb.AppendLine("        vec4 startcolor = gammaToLinear(a_StartColor);");
        sb.AppendLine("        v_Color = computeParticleColor(startcolor, normalizedAge);");
        sb.AppendLine();
        sb.AppendLine("#ifdef DIFFUSEMAP");
        sb.AppendLine("        vec2 simulateUV;");
        sb.AppendLine("    #if defined(SPHERHBILLBOARD) || defined(STRETCHEDBILLBOARD) || defined(HORIZONTALBILLBOARD) || defined(VERTICALBILLBOARD)");
        sb.AppendLine("        simulateUV = a_SimulationUV.xy + a_CornerTextureCoordinate.zw * a_SimulationUV.zw;");
        sb.AppendLine("        v_TextureCoordinate = computeParticleUV(simulateUV, normalizedAge);");
        sb.AppendLine("    #endif");
        sb.AppendLine("    #ifdef RENDERMODE_MESH");
        sb.AppendLine("        simulateUV = a_SimulationUV.xy + a_MeshTextureCoordinate * a_SimulationUV.zw;");
        sb.AppendLine("        v_TextureCoordinate = computeParticleUV(simulateUV, normalizedAge);");
        sb.AppendLine("    #endif");
        sb.AppendLine("        v_TextureCoordinate = TransformUV(v_TextureCoordinate, u_TilingOffset);");
        sb.AppendLine("#endif");
        sb.AppendLine("    }");
        sb.AppendLine("    else");
        sb.AppendLine("    {");
        sb.AppendLine("        gl_Position = vec4(2.0, 2.0, 2.0, 1.0); // Discard");
        sb.AppendLine("    }");
        sb.AppendLine("    gl_Position = remapPositionZ(gl_Position);");
        sb.AppendLine("#ifdef FOG");
        sb.AppendLine("    FogHandle(gl_Position.z);");
        sb.AppendLine("#endif");
        sb.AppendLine("}");
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
                }
                else
                {
                    typeStr = "Texture2D";
                }
                
                if (!string.IsNullOrEmpty(prop.define))
                {
                    options = $", options: {{ define: \"{prop.define}\" }}";
                }
                return $"        {prop.layaName}: {{ type: {typeStr}{options} }},";
                
            case ShaderUtil.ShaderPropertyType.Color:
                typeStr = "Color";
                // 根据属性名推断默认值
                if (prop.unityName.Contains("Shadow"))
                    defaultValue = "[0, 0, 0, 1]";
                else if (prop.unityName.Contains("Emission"))
                    defaultValue = "[0, 0, 0, 0]";
                else if (prop.unityName.Contains("Reflect"))
                    defaultValue = "[0.02, 0.02, 0.02, 0]";
                else
                    defaultValue = "[1, 1, 1, 1]";
                return $"        {prop.layaName}: {{ type: {typeStr}, default: {defaultValue} }},";
                
            case ShaderUtil.ShaderPropertyType.Range:
                typeStr = "Float";
                // 使用Range的默认值（通常是中间值或最小值）
                float rangeDefault = prop.rangeMin;
                // 特殊属性的默认值
                if (prop.unityName.Contains("Smoothness") || prop.unityName.Contains("Metallic"))
                    rangeDefault = 0.0f;
                else if (prop.unityName.Contains("Intensity") || prop.unityName.Contains("Scale"))
                    rangeDefault = 1.0f;
                else if (prop.unityName.Contains("Threshold"))
                    rangeDefault = 0.5f;
                else if (prop.unityName.Contains("Alpha") || prop.unityName.Contains("Occlusion"))
                    rangeDefault = 1.0f;
                    
                defaultValue = rangeDefault.ToString("F1");
                rangeStr = $", range: [{prop.rangeMin:F1}, {prop.rangeMax:F1}]";
                return $"        {prop.layaName}: {{ type: {typeStr}, default: {defaultValue}{rangeStr} }},";
                
            case ShaderUtil.ShaderPropertyType.Float:
                typeStr = "Float";
                // 根据属性名推断默认值
                if (prop.unityName.Contains("Intensity") || prop.unityName.Contains("Scale"))
                    defaultValue = "1.0";
                else if (prop.unityName.Contains("RemapMax"))
                    defaultValue = "1.0";
                else
                    defaultValue = "0.0";
                return $"        {prop.layaName}: {{ type: {typeStr}, default: {defaultValue} }},";
                
            case ShaderUtil.ShaderPropertyType.Vector:
                typeStr = "Vector4";
                // 根据属性名推断默认值
                if (prop.unityName.Contains("TilingOffset") || prop.unityName.EndsWith("_ST"))
                    defaultValue = "[1, 1, 0, 0]";
                else if (prop.unityName.Contains("LightDir"))
                    defaultValue = "[0, 1, 0, 1]";
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
        JSONObject jsonData, ResoureMap resoureMap)
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
        
        // 收集Shader属性用于特性检测
        List<ShaderProperty> shaderProperties = CollectShaderProperties(shader);
        
        // 根据材质类型和Shader特性添加必要的宏定义
        bool hasNPR = (materialType == LayaMaterialType.PBR || materialType == LayaMaterialType.Custom) && HasNPRFeatures(shaderProperties);
        bool hasEmission = false;
        
        // Effect类型（粒子）默认启用顶点颜色和COLOR宏
        if (materialType == LayaMaterialType.PARTICLESHURIKEN)
        {
            defines.Add("COLOR");
            defines.Add("ENABLEVERTEXCOLOR");
        }
        
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
        // Effect类型的宏定义已在前面添加
        
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
