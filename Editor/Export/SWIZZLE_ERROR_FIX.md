# Shader编译错误修复：vec4到vec2的非法类型转换

## 错误信息
```
Error compiling shader 'Artist_Effect_Effect_FullEffect' (pipelineMode=Forward)
ERROR: 0:266: 'assign' : cannot convert from 'highp 4-component vector of float' to 'highp 2-component vector of float'
```

实际错误不在266行，错误行号可能因shader而异。

---

## 根本原因

### 问题代码（导出版本）
```glsl
// 第695-696行
vec2 mainUV = vec2(v_Texcoord0.zw + ((u_Layer0UVMode == 0.0 ? v_Texcoord0.xy : screenUV) * u_TilingOffset.xy + vec2(u_MainTex_OffsetX_Custom < 1.0 ? u_TilingOffset.xy.z : v_Texcoord7[u_MainTex_OffsetX_Custom - 1.0],
u_MainTex_OffsetY_Custom < 1.0 ? u_TilingOffset.xy.w : v_Texcoord7[u_MainTex_OffsetY_Custom - 1.0])));
```

**错误**：`u_TilingOffset.xy.z` 和 `u_TilingOffset.xy.w`

### 正确代码（AI版本）
```glsl
// 第719-720行
vec2 mainUV = vec2(v_Texcoord0.zw + ((u_Layer0UVMode == 0.0 ? v_Texcoord0.xy : screenUV) * u_TilingOffset.xy + vec2(u_MainTex_OffsetX_Custom < 1.0 ? u_TilingOffset.z : v_Texcoord7[u_MainTex_OffsetX_Custom - 1.0],
u_MainTex_OffsetY_Custom < 1.0 ? u_TilingOffset.w : v_Texcoord7[u_MainTex_OffsetY_Custom - 1.0])));
```

**正确**：`u_TilingOffset.z` 和 `u_TilingOffset.w`

---

## 技术分析

### Swizzle访问规则
- `u_TilingOffset` 是 `vec4` (x, y, z, w)
- `u_TilingOffset.xy` 是 `vec2` (x, y)
- **vec2 没有 .z 和 .w 分量**

### 错误链
1. `u_TilingOffset.xy` → 返回 vec2
2. `.z` → **错误！vec2 没有 z 分量**
3. 尝试访问不存在的分量导致类型转换错误

### 正确访问
- 直接访问：`u_TilingOffset.z` ✅ (vec4的第3个分量)
- 直接访问：`u_TilingOffset.w` ✅ (vec4的第4个分量)

---

## 修复方案

### 修复1：临时修复shader文件（测试用）
```bash
# 自动修复导出的shader文件
sed -i 's/\.xy\.z/.z/g; s/\.xy\.w/.w/g' "shader_file.shader"
```

**影响的模式**：
- `u_TilingOffset.xy.z` → `u_TilingOffset.z`
- `u_TilingOffset.xy.w` → `u_TilingOffset.w`
- `u_DetailTex_ST.xy.z` → `u_DetailTex_ST.z`
- `u_DetailTex_ST.xy.w` → `u_DetailTex_ST.w`
- `u_DissolveTexture_ST.xy.z` → `u_DissolveTexture_ST.z`
- `u_DissolveTexture_ST.xy.w` → `u_DissolveTexture_ST.w`

### 修复2：插件通用修复（永久）

**位置**：`CustomShaderExporter.cs:3875`（在ConvertHLSLToGLSL函数中）

```csharp
conversionTimer.Restart();

// ⭐ 修复错误的swizzle访问：.xy.z 和 .xy.w
// vec2没有.z和.w分量，这是Unity shader代码转换错误导致的
// 例如：u_TilingOffset.xy.z -> u_TilingOffset.z
code = Regex.Replace(code, @"(\w+)\.xy\.z\b", "$1.z");
code = Regex.Replace(code, @"(\w+)\.xy\.w\b", "$1.w");

// ============================================
// 类型转换（注意顺序，先转换长的）
// ============================================
code = Regex.Replace(code, @"\bfloat4x4\b", "mat4");
```

**说明**：
- 在HLSL到GLSL转换的早期阶段清理错误的swizzle访问
- 使用正则表达式匹配所有 `.xy.z` 和 `.xy.w` 模式
- 替换为正确的 `.z` 和 `.w` 访问

---

## 为什么会产生这个错误？

### 可能原因1：Unity shader中的复杂表达式
Unity shader可能使用了宏或复杂的表达式，在展开后产生了`.xy.z`这样的代码。

### 可能原因2：多次字符串替换
插件可能在多个阶段替换变量名，导致：
1. `_MainTex_ST.z` → `u_MainTex_ST.z`
2. 某个阶段错误地将`.z`替换成了`.xy.z`
3. 最后`u_MainTex_ST` → `u_TilingOffset`
4. 结果：`u_TilingOffset.xy.z` ❌

### 解决策略
在HLSL转换的最早阶段清理这些错误，确保后续所有处理都基于正确的代码。

---

## 测试验证

### 1. 导出新的shader
```
Unity Editor -> LayaAir3D Export -> Export Custom Shader
```

### 2. 检查生成的shader代码
```bash
# 搜索是否还有.xy.z或.xy.w
grep -n "\.xy\.[zw]" exported_shader.shader
```

**预期结果**：没有输出（表示没有错误模式）

### 3. 在Laya引擎中加载
- 检查控制台是否有shader编译错误
- 确认材质渲染正常

---

## 受影响的shader类型

这个错误影响所有使用纹理Tiling/Offset的粒子特效shader，特别是：
- 使用`u_TilingOffset`（主纹理）
- 使用`u_DetailTex_ST`（细节纹理）
- 使用`u_DissolveTexture_ST`（溶解纹理）
- 使用CustomData控制纹理offset的shader

---

## 修复优先级

| 优先级 | 问题 | 影响 |
|--------|------|------|
| 🔴 P0 | Swizzle访问错误 | **Shader无法编译，材质完全不工作** |

---

## 相关文件

- **修改文件**: `CustomShaderExporter.cs` (3875行)
- **参考文件**:
  - `Artist_Effect_Effect_FullEffect.shader` (AI正确版本)
  - `Artist_Effect_Effect_FullEffect_export.shader` (导出版本，已修复)

---

## 修复日期
2026-02-12

## 修复状态
✅ **已完成** - 通用修复，自动清理所有.xy.z和.xy.w错误模式
