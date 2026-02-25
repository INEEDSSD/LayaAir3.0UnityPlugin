# 材质导出通用修复 - Universal Material Export Fix

## 修复概述

根据正确材质版本 `fx_mat_1714_yanqiu_wenli_002_3.lmat` 的分析，实现了三个关键的通用修复，适用于所有自定义shader的材质导出。

---

## 修复1: Unity Keywords到Laya Defines映射（P0优先级）

### 问题
原始导出缺失从Unity材质Keywords到Laya Defines的映射，导致shader功能无法正确启用。

### 解决方案
**文件**: `CustomShaderExporter.cs`
**位置**: `ExportMaterialFile` 函数，添加在纹理导出之前（约8484-8503行）

```csharp
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
```

### 转换规则
| Unity Keyword | Laya Define | 规则 |
|--------------|-------------|------|
| _LAYERTYPE_THREE | LAYERTYPE_THREE | 去掉前缀 _ |
| _USEDISTORT0_ON | USEDISTORT0 | 去掉前缀 _ 和后缀 _ON |
| _WRAPMODE_DEFAULT | WRAPMODE_DEFAULT | 去掉前缀 _ |

**正则表达式**: `^_?(.+?)(?:_ON)?$` → `$1`

### 辅助函数
**位置**: 约5981行之后

```csharp
/// <summary>
/// 将Unity Keyword转换为Laya Define
/// 规则：去掉前缀 _ 和后缀 _ON
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
```

---

## 修复2: 纹理Tiling/Offset自动导出（P0优先级）

### 问题
Unity材质中的纹理缩放（Tiling）和偏移（Offset）信息未被导出，导致渲染效果与Unity不一致。

### 解决方案
**文件**: `CustomShaderExporter.cs`
**位置**: `ExportMaterialFile` 函数中的纹理导出部分（约8553-8562行）

```csharp
case ShaderUtil.ShaderPropertyType.TexEnv:
    ExportTextureProperty(material, propName, layaName, textures, defines, resoureMap, shader, i);

    // ⭐ FIX 2/3: 导出纹理Tiling/Offset（通用方案）
    // 规则：Unity的 _MainTex/_BaseMap/_AlbedoTexture → u_TilingOffset
    //       其他纹理 _XXX → u_XXX_ST
    // 格式：[scaleX, scaleY, offsetX, offsetY]
    ExportTextureTilingOffset(material, propName, layaName, props);
    break;
```

### 映射规则
| Unity纹理属性 | Unity Scale | Unity Offset | Laya属性名 | Laya值 |
|--------------|------------|-------------|-----------|--------|
| _MainTex | (2, 1) | (0, 0) | u_TilingOffset | [2, 1, 0, 0] |
| _DetailTex | (1, 1) | (0, 0) | u_DetailTex_ST | [1, 1, 0, 0] |
| _DetailTex2 | (2, 0.7) | (0, 0) | u_DetailTex2_ST | [2, 0.7, 0, 0] |
| _DistortTex0 | (5, 0.5) | (0, 0) | u_DistortTex0_ST | [5, 0.5, 0, 0] |

**格式**: `[scaleX, scaleY, offsetX, offsetY]`

### 辅助函数
**位置**: 约8708行之后

```csharp
/// <summary>
/// 导出纹理Tiling/Offset（通用方案）
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
```

---

## 修复3: 移除粒子Shader的错误Defines（P1优先级）

### 问题
原始导出自动为PARTICLESHURIKEN类型添加 `COLOR` 和 `ENABLEVERTEXCOLOR` defines，这些是mesh shader的宏定义，不应该出现在粒子shader中。

### 解决方案
**文件**: `CustomShaderExporter.cs`
**位置**: `ExportMaterialFile` 函数（约8533-8536行）

**修改前**:
```csharp
// Effect类型（粒子）默认启用顶点颜色和COLOR宏
if (materialType == LayaMaterialType.PARTICLESHURIKEN)
{
    defines.Add("COLOR");
    defines.Add("ENABLEVERTEXCOLOR");
}
```

**修改后**:
```csharp
// ⭐ FIX 3/3: 不再自动为Effect类型添加COLOR和ENABLEVERTEXCOLOR
// 这些defines应该由Keywords映射生成，或者由shader特征检测生成
// 粒子shader（如Artist_Effect系列）使用不同的渲染逻辑，不需要这些defines
```

### 原因
- **COLOR** 和 **ENABLEVERTEXCOLOR** 用于mesh材质的顶点颜色处理
- 粒子shader（Artist_Effect系列）有自己的渲染逻辑，不需要这些宏
- 粒子shader需要的宏定义（如LAYERTYPE_THREE、USEDISTORT0等）应该由Keywords映射自动生成

---

## 验证测试

### 测试材质
- **Unity材质**: `fx_mat_1714_yanqiu_wenli_002.mat`
- **正确版本**: `fx_mat_1714_yanqiu_wenli_002_3.lmat`

### 验证点1: Keywords映射
```csharp
// Unity Keywords:
_LAYERTYPE_THREE
_USEDISTORT0_ON
_WRAPMODE_DEFAULT

// 应该转换为Laya Defines:
LAYERTYPE_THREE
USEDISTORT0
WRAPMODE_DEFAULT
```

### 验证点2: 纹理Tiling
```csharp
// Unity:
_MainTex.scale = (2, 1), offset = (0, 0)
_DetailTex2.scale = (2, 0.7), offset = (0, 0)
_DistortTex0.scale = (5, 0.5), offset = (0, 0)

// 应该导出为:
"u_TilingOffset": [2, 1, 0, 0],
"u_DetailTex2_ST": [2, 0.7, 0, 0],
"u_DistortTex0_ST": [5, 0.5, 0, 0]
```

### 验证点3: Defines完整性
```csharp
// 最终Defines应该包含:
1. 纹理Defines（自动生成）: ALBEDOTEXTURE, DETAILTEX, DETAILTEX2MAP, DISTORTTEX0MAP
2. Keywords转换的Defines: LAYERTYPE_THREE, USEDISTORT0, WRAPMODE_DEFAULT
3. 不应该包含: COLOR, ENABLEVERTEXCOLOR（粒子shader不需要）
```

---

## 通用性说明

这三个修复是**通用方案**，适用于：

1. ✅ **所有自定义shader** - 不依赖shader名称，基于Unity材质API自动检测
2. ✅ **粒子shader** - Artist_Effect系列、BR_Effect系列等
3. ✅ **Mesh shader** - 标准PBR、BlinnPhong、自定义mesh shader
4. ✅ **Effect shader** - 特效shader、透明shader等

### 核心原则
- **基于Unity API** - 使用 `material.shaderKeywords`、`material.GetTextureScale/Offset()`
- **不依赖配置** - 不需要预定义映射表，自动转换
- **保持兼容性** - 与现有导出逻辑兼容，不影响已配置的shader

---

## 修改文件列表

| 文件 | 修改内容 | 行数范围 |
|------|---------|---------|
| CustomShaderExporter.cs | 添加Keywords映射 | ~8484-8503 |
| CustomShaderExporter.cs | 添加ConvertKeywordToDefine函数 | ~5981-6007 |
| CustomShaderExporter.cs | 添加纹理Tiling/Offset导出 | ~8553-8562 |
| CustomShaderExporter.cs | 添加ExportTextureTilingOffset函数 | ~8708-8757 |
| CustomShaderExporter.cs | 移除错误的粒子shader定义 | ~8533-8536 |

---

## 预期效果

修复后，自定义shader材质导出应该：

1. ✅ **正确的shader功能** - Keywords映射确保shader分支正确执行
2. ✅ **正确的纹理缩放** - 渲染效果与Unity一致
3. ✅ **干净的宏定义** - 只包含实际需要的defines，无多余宏

---

## 调试日志

修复后会输出详细的调试信息：

```
LayaAir3D: Converted keyword '_LAYERTYPE_THREE' to define 'LAYERTYPE_THREE'
LayaAir3D: Converted keyword '_USEDISTORT0_ON' to define 'USEDISTORT0'
LayaAir3D: Converted keyword '_WRAPMODE_DEFAULT' to define 'WRAPMODE_DEFAULT'
LayaAir3D: Exported texture tiling/offset '_MainTex' as 'u_TilingOffset': [2, 1, 0, 0]
LayaAir3D: Exported texture tiling/offset '_DetailTex2' as 'u_DetailTex2_ST': [2, 0.7, 0, 0]
LayaAir3D: Exported texture tiling/offset '_DistortTex0' as 'u_DistortTex0_ST': [5, 0.5, 0, 0]
LayaAir3D: Exported material 'fx_mat_1714_yanqiu_wenli_002' as type 'Artist_Effect_Effect_FullEffect' (MaterialType: Custom)
```

---

## 下一步

1. 在Unity中导出使用自定义shader的场景
2. 检查LayaAir IDE中的材质渲染效果
3. 对比Unity和Laya的渲染结果
4. 如有问题，查看Unity Console中的调试日志

---

## 参考文档

- **材质对比分析**: `MATERIAL_EXPORT_CORRECT_VERSION_ANALYSIS.md`
- **逐步测试指南**: `STEP_BY_STEP_TEST_GUIDE.md`
- **正确材质版本**: `fx_mat_1714_yanqiu_wenli_002_3.lmat`
