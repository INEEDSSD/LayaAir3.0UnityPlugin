# Unity与Laya材质对比分析

## 文件对比
- **Unity材质**: `fx_mat_1714_yanqiu_wenli_002.mat`
- **Laya材质**: `fx_mat_1714_yanqiu_wenli_002.lmat`

---

## 关键差异分析

### ❌ 问题1: Keywords/Defines映射缺失

#### Unity Keywords (第14-17行)
```yaml
m_ValidKeywords:
- _LAYERTYPE_THREE
- _USEDISTORT0_ON
- _WRAPMODE_DEFAULT
```

#### Laya Defines (第226-232行)
```json
"defines":[
  "ALBEDOTEXTURE",
  "DETAILTEX",
  "DETAILTEX2MAP",
  "DISTORTTEX0MAP",
  "COLOR",              // ❌ 不应该有（这是非粒子shader的）
  "ENABLEVERTEXCOLOR"   // ❌ 不应该有（这是非粒子shader的）
]
```

#### 缺失的Defines
- ❌ **LAYERTYPE_THREE** - 图层类型（三层混合）
- ❌ **USEDISTORT0** - 使用扭曲0
- ❌ **WRAPMODE_DEFAULT** - UV包裹模式

#### 错误的Defines
- ❌ **COLOR** - 这是非粒子shader的define，粒子应该用TINTCOLOR
- ❌ **ENABLEVERTEXCOLOR** - 这是非粒子shader的define

---

### ❌ 问题2: 纹理Tiling和Offset缺失

#### Unity纹理属性 (第28-84行)
```yaml
- _MainTex:
    m_Texture: {fileID: 2800000, guid: 3fc41f42b1a158c4e90672b3764368c0}
    m_Scale: {x: 2, y: 1}         # ⚠️ Tiling
    m_Offset: {x: 0, y: 0}

- _DetailTex2:
    m_Texture: {fileID: 2800000, guid: ae066660700f88a4abbfd7378906aa99}
    m_Scale: {x: 2, y: 0.7}       # ⚠️ Tiling不是(1,1)
    m_Offset: {x: 0, y: 0}

- _DistortTex0:
    m_Texture: {fileID: 2800000, guid: af8bded043b9d7441aee1854c4f1e892}
    m_Scale: {x: 5, y: 0.5}       # ⚠️ Tiling不是(1,1)
    m_Offset: {x: 0, y: 0}
```

#### Laya材质中纹理属性
```json
{
  "name":"u_AlbedoTexture",
  "constructParams":[256, 256, 1, false, true, true],
  "propertyParams":{
    "filterMode":1,
    "wrapModeU":0,
    "wrapModeV":0,
    "anisoLevel":1
  },
  "path":"res://2ef73840-398a-49dc-ba74-672afcb8aa18"
}
```

#### ❌ 缺失内容
- **u_TilingOffset**: 主纹理的Tiling/Offset (应该是[2, 1, 0, 0])
- **u_DetailTex2Tiling**: DetailTex2的Tiling (应该是[2, 0.7])
- **u_DistortTex0Tiling**: DistortTex0的Tiling (应该是[5, 0.5])

---

### ❌ 问题3: 混合模式映射

#### Unity混合模式 (第105行)
```yaml
- _DstBlend: 10     # Unity中的OneMinusSrcColor
- _ZWrite: 0        # 禁用深度写入
- _Alpha: 1
```

#### Laya混合模式 (第6-8行)
```json
"s_Blend":0,         // 自定义混合
"s_BlendSrc":1,      // One
"s_BlendDst":7,      // ❓ 需要确认是否正确
```

#### Unity BlendMode枚举
- 0 = Zero
- 1 = One
- 2 = DstColor
- 3 = SrcColor
- 4 = OneMinusDstColor
- 5 = SrcAlpha
- 6 = OneMinusSrcColor  ← **Unity中_DstBlend:6**
- 7 = DstAlpha
- 8 = OneMinusDstAlpha
- 9 = SrcAlphaSaturate
- 10 = OneMinusSrcColor  ← **Unity材质中是10，但标准定义是6**

**注意**: Unity材质中`_DstBlend: 10`可能是非标准值，需要检查shader定义。

---

## 完整的缺失属性清单

### 1. Defines缺失
```json
// 应该添加的defines:
"LAYERTYPE_THREE",      // _LAYERTYPE_THREE → LAYERTYPE_THREE
"USEDISTORT0",          // _USEDISTORT0_ON → USEDISTORT0
"WRAPMODE_DEFAULT",     // _WRAPMODE_DEFAULT → WRAPMODE_DEFAULT

// 应该移除的defines（粒子shader不需要）:
"COLOR",                // ❌ 移除
"ENABLEVERTEXCOLOR"     // ❌ 移除
```

### 2. 纹理Tiling/Offset缺失
```json
// 主纹理
"u_TilingOffset": [2, 1, 0, 0],

// 其他纹理的Tiling（如果需要单独控制）
"u_DetailTex2_ST": [2, 0.7, 0, 0],
"u_DistortTex0_ST": [5, 0.5, 0, 0]
```

### 3. 渲染状态
```json
// 检查Alpha值
"u_Alpha": 1,           // ✅ 已有

// 检查BlendDst是否正确
"s_BlendDst": 7,        // 需要确认映射是否正确
```

---

## Unity Keywords到Laya Defines映射规则

| Unity Keyword | Laya Define | 说明 |
|--------------|-------------|------|
| _LAYERTYPE_ONE | LAYERTYPE_ONE | 单层 |
| _LAYERTYPE_TWO | LAYERTYPE_TWO | 双层 |
| **_LAYERTYPE_THREE** | **LAYERTYPE_THREE** | **三层（缺失）** |
| **_USEDISTORT0_ON** | **USEDISTORT0** | **扭曲0开关（缺失）** |
| _WRAPMODE_CLAMP | WRAPMODE_CLAMP | UV Clamp模式 |
| _WRAPMODE_REPEAT | WRAPMODE_REPEAT | UV Repeat模式 |
| **_WRAPMODE_DEFAULT** | **WRAPMODE_DEFAULT** | **UV默认模式（缺失）** |

---

## 修复建议

### 立即修复（高优先级）
1. **Keywords到Defines的映射** - 确保所有Unity Keywords正确转换为Laya Defines
2. **纹理Tiling/Offset导出** - 添加纹理的Tiling和Offset信息到材质

### 测试修复（中优先级）
3. **混合模式映射** - 验证BlendMode枚举值的正确性
4. **移除错误的Defines** - 粒子shader不应该有COLOR和ENABLEVERTEXCOLOR

---

## 测试建议

### 手动测试步骤
1. 修改Laya材质的defines，添加缺失的宏：
```json
"defines":[
  "ALBEDOTEXTURE",
  "DETAILTEX",
  "DETAILTEX2MAP",
  "DISTORTTEX0MAP",
  "LAYERTYPE_THREE",      // 添加
  "USEDISTORT0",          // 添加
  "WRAPMODE_DEFAULT"      // 添加
]
```

2. 添加纹理Tiling：
```json
"u_TilingOffset": [2, 1, 0, 0],
// 或者在textures中添加tilingOffset属性
```

3. 重新加载材质，观察效果是否正确

---

## 修复优先级

| 优先级 | 问题 | 影响 |
|--------|------|------|
| 🔴 P0 | Keywords映射缺失 | **功能完全不正确** |
| 🔴 P0 | 纹理Tiling缺失 | **渲染效果严重错误** |
| 🟡 P1 | 错误的Defines | 可能导致shader编译警告 |
| 🟢 P2 | BlendMode验证 | 可能导致混合效果略有差异 |
