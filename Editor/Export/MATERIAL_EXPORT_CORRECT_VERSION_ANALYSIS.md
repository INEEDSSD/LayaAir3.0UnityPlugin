# 正确版本材质对比分析

## 正确材质版本
**文件**: `fx_mat_1714_yanqiu_wenli_002_3.lmat`

---

## 关键差异总结

### ✅ 差异1：纹理Tiling/Offset（第13-16行）

#### 正确版本
```json
"u_TilingOffset":[2,1,0,0],           // 主纹理 _MainTex
"u_DetailTex_ST":[1,1,0,0],          // 细节纹理1 _DetailTex
"u_DetailTex2_ST":[2,0.7,0,0],       // 细节纹理2 _DetailTex2
"u_DistortTex0_ST":[5,0.5,0,0],      // 扭曲纹理 _DistortTex0
```

#### 原始导出版本
```json
// ❌ 缺失所有纹理Tiling/Offset
```

---

### ✅ 差异2：Defines定义（第230-238行）

#### 正确版本
```json
"defines":[
  "ALBEDOTEXTURE",
  "DETAILTEX",
  "DETAILTEX2MAP",
  "DISTORTTEX0MAP",
  "LAYERTYPE_THREE",      // ← Unity: _LAYERTYPE_THREE
  "USEDISTORT0",          // ← Unity: _USEDISTORT0_ON
  "WRAPMODE_DEFAULT"      // ← Unity: _WRAPMODE_DEFAULT
]
```

#### 原始导出版本
```json
"defines":[
  "ALBEDOTEXTURE",
  "DETAILTEX",
  "DETAILTEX2MAP",
  "DISTORTTEX0MAP",
  "COLOR",                // ❌ 错误：非粒子shader的define
  "ENABLEVERTEXCOLOR"     // ❌ 错误：非粒子shader的define
]
```

---

## Unity材质到Laya材质映射规则

### 1. 纹理Tiling/Offset映射

| Unity纹理属性 | Unity Scale | Unity Offset | Laya属性名 | Laya值 |
|--------------|------------|-------------|-----------|--------|
| _MainTex | (2, 1) | (0, 0) | u_TilingOffset | [2, 1, 0, 0] |
| _DetailTex | (1, 1) | (0, 0) | u_DetailTex_ST | [1, 1, 0, 0] |
| _DetailTex2 | (2, 0.7) | (0, 0) | u_DetailTex2_ST | [2, 0.7, 0, 0] |
| _DistortTex0 | (5, 0.5) | (0, 0) | u_DistortTex0_ST | [5, 0.5, 0, 0] |

**格式**: `[scaleX, scaleY, offsetX, offsetY]`

**规则**:
- Unity的 `_MainTex` / `_BaseMap` / `_AlbedoTexture` → `u_TilingOffset`
- Unity的 `_XXX` → `u_XXX_ST`
- 如果Scale为(1,1)且Offset为(0,0)，仍然需要导出（保证完整性）

---

### 2. Keywords到Defines映射规则

| Unity Keyword | Laya Define | 转换规则 |
|--------------|-------------|---------|
| _LAYERTYPE_THREE | LAYERTYPE_THREE | 去掉前缀_ |
| _LAYERTYPE_TWO | LAYERTYPE_TWO | 去掉前缀_ |
| _LAYERTYPE_ONE | LAYERTYPE_ONE | 去掉前缀_ |
| _USEDISTORT0_ON | USEDISTORT0 | 去掉前缀_和后缀_ON |
| _USERIM_ON | USERIM | 去掉前缀_和后缀_ON |
| _WRAPMODE_DEFAULT | WRAPMODE_DEFAULT | 去掉前缀_ |
| _WRAPMODE_CLAMP | WRAPMODE_CLAMP | 去掉前缀_ |
| _WRAPMODE_REPEAT | WRAPMODE_REPEAT | 去掉前缀_ |

**通用规则**:
1. 去掉前缀 `_`
2. 去掉后缀 `_ON`（如果有）
3. 保留中间的名称

**正则表达式**: `^_?(.+?)(?:_ON)?$` → `$1`

---

### 3. 纹理引用到Defines的映射

| Unity纹理属性 | Laya纹理名 | 自动添加的Define |
|--------------|-----------|----------------|
| _MainTex | u_AlbedoTexture | ALBEDOTEXTURE |
| _DetailTex | u_DetailTex | DETAILTEX |
| _DetailTex2 | u_DetailTex2 | DETAILTEX2MAP |
| _DistortTex0 | u_DistortTex0 | DISTORTTEX0MAP |
| _NormalMap | u_NormalTexture | NORMALTEXTURE |
| _RimMap | u_RimMap | RIMMAP |

**规则**: 如果纹理有引用（非空），自动添加对应的Define

---

## 插件需要修改的内容

### 修改1: Keywords映射（高优先级）

**位置**: 材质导出代码（MaterialExporter.cs或类似）

**需要实现**:
```csharp
// 从Unity材质读取Keywords
string[] unityKeywords = material.shaderKeywords;

// 转换为Laya Defines
List<string> layaDefines = new List<string>();
foreach (var keyword in unityKeywords)
{
    string layaDefine = ConvertKeywordToDefine(keyword);
    if (!string.IsNullOrEmpty(layaDefine))
    {
        layaDefines.Add(layaDefine);
    }
}

// ConvertKeywordToDefine实现
private static string ConvertKeywordToDefine(string unityKeyword)
{
    // 规则：去掉前缀_和后缀_ON
    string define = unityKeyword.TrimStart('_');
    if (define.EndsWith("_ON"))
    {
        define = define.Substring(0, define.Length - 3);
    }
    return define;
}
```

---

### 修改2: 纹理Tiling/Offset导出（高优先级）

**需要实现**:
```csharp
// 遍历材质的所有纹理属性
foreach (var texProperty in material.GetTexturePropertyNames())
{
    Texture tex = material.GetTexture(texProperty);
    if (tex != null)
    {
        // 获取Tiling和Offset
        Vector2 scale = material.GetTextureScale(texProperty);
        Vector2 offset = material.GetTextureOffset(texProperty);

        // 转换属性名
        string layaPropertyName;
        if (texProperty == "_MainTex" || texProperty == "_BaseMap" || texProperty == "_AlbedoTexture")
        {
            layaPropertyName = "u_TilingOffset";
        }
        else
        {
            // 去掉前缀_，添加后缀_ST
            layaPropertyName = "u_" + texProperty.TrimStart('_') + "_ST";
        }

        // 添加到材质数据
        materialData[layaPropertyName] = new float[] { scale.x, scale.y, offset.x, offset.y };
    }
}
```

---

### 修改3: 移除粒子shader的错误Defines

**需要实现**:
```csharp
// 如果是粒子shader，移除非粒子shader的defines
if (IsParticleShader(shaderName))
{
    // 移除COLOR和ENABLEVERTEXCOLOR
    layaDefines.Remove("COLOR");
    layaDefines.Remove("ENABLEVERTEXCOLOR");

    // 确保有粒子shader必需的defines
    // （已在Keywords映射中自动添加）
}
```

---

## 完整的材质导出流程

### 流程图
```
Unity材质(.mat)
    ↓
1. 读取基础属性（Cull, Blend, RenderQueue等）
    ↓
2. 读取Float/Vector/Color参数 ✅ 已正确
    ↓
3. 读取纹理引用
    ├─→ 导出纹理文件
    ├─→ 添加纹理对应的Define ✅ 已正确
    └─→ ⭐ 导出纹理Tiling/Offset（需要添加）
    ↓
4. ⭐ 读取Keywords并转换为Defines（需要修复）
    ↓
5. ⭐ 如果是粒子shader，移除错误的Defines（需要添加）
    ↓
6. 组装为Laya材质JSON
    ↓
保存为.lmat文件
```

---

## 测试验证

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

// 应该导出为:
"u_TilingOffset": [2, 1, 0, 0],
"u_DetailTex2_ST": [2, 0.7, 0, 0]
```

### 验证点3: Defines完整性
```csharp
// 最终Defines应该包含:
1. 纹理Defines（自动生成）: ALBEDOTEXTURE, DETAILTEX等
2. Keywords转换的Defines: LAYERTYPE_THREE, USEDISTORT0等
3. 不应该包含: COLOR, ENABLEVERTEXCOLOR（粒子shader）
```

---

## 优先级

| 优先级 | 修改内容 | 影响 |
|--------|---------|------|
| 🔴 P0 | Keywords到Defines映射 | **功能完全不正确** |
| 🔴 P0 | 纹理Tiling/Offset导出 | **渲染效果严重错误** |
| 🟡 P1 | 移除错误的Defines | 避免shader编译警告 |

---

## 下一步

根据这个正确版本的材质，我将修改插件代码实现：
1. ✅ Keywords到Defines的完整映射
2. ✅ 纹理Tiling/Offset的自动导出
3. ✅ 粒子shader的Defines清理
