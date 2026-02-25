# GLSL类型不匹配全面修复方案

## 错误症状

```
ERROR: 0:264: 'assign' : cannot convert from 'highp 4-component vector of float' to 'highp 2-component vector of float'
```

这是WebGL Shader编译器报告的GLSL类型不匹配错误。

---

## 问题分类

### 类型1: 错误的Swizzle访问 ⭐ 已修复
```glsl
// ❌ 错误: vec2没有.z分量
float value = u_TilingOffset.xy.z;

// ✅ 正确: 直接访问.z
float value = u_TilingOffset.z;
```

### 类型2: vec4到vec2的赋值 ⭐ 已修复
```glsl
// ❌ 错误: 需要显式转换
vec2 uv = v_Texcoord0;

// ✅ 正确: 添加.xy
vec2 uv = v_Texcoord0.xy;
```

### 类型3: 函数参数类型不匹配 ⭐ 已修复
```glsl
// ❌ 错误: texture2D需要vec2 UV
vec4 color = texture2D(u_Texture, v_Texcoord0);

// ✅ 正确: 传递vec2
vec4 color = texture2D(u_Texture, v_Texcoord0.xy);
```

### 类型4: 多余的vec2()构造函数 ⭐ 已修复
```glsl
// ❌ 冗余但可能导致编译器警告
vec2 uv = vec2(v_Texcoord0.xy);

// ✅ 简洁: .xy已经是vec2
vec2 uv = v_Texcoord0.xy;
```

---

## 修复实现

### 修复位置: CustomShaderExporter.cs

#### 1. FixShaderTypeMismatch 函数增强 (约5504-5750行)

```csharp
private static string FixShaderTypeMismatch(string content)
{
    // ⭐ 0. 修复错误的swizzle访问 (CRITICAL)
    content = Regex.Replace(content, @"(\w+)\.xy\.z\b", "$1.z");
    content = Regex.Replace(content, @"(\w+)\.xy\.w\b", "$1.w");
    content = Regex.Replace(content, @"(\w+)\.xyz\.w\b", "$1.w");
    // ... 更多swizzle模式

    // 1-8. 原有的v_Texcoord0修复逻辑
    // ...

    // ⭐ 9. 修复vec4到vec2的赋值问题 (NEW)
    content = FixVec4ToVec2Assignments(content);

    // ⭐ 10. 修复多余的vec2()构造函数 (NEW)
    content = RemoveRedundantVec2Constructors(content);

    // ⭐ 11. 修复函数参数类型不匹配 (NEW)
    content = FixFunctionParameterTypeMismatch(content);

    return content;
}
```

#### 2. 新增辅助函数

**FixVec4ToVec2Assignments** (约5650-5700行)
- 自动收集所有vec4变量
- 检测vec4到vec2的赋值
- 自动添加.xy后缀

**RemoveRedundantVec2Constructors** (约5700-5720行)
- 检测 `vec2(something.xy)` 模式
- 移除冗余的vec2()构造函数

**FixFunctionParameterTypeMismatch** (约5720-5750行)
- 检测texture2D等函数的UV参数
- 自动为vec4变量添加.xy

#### 3. 验证函数

**ValidateShaderContent** (约5750-5830行)
- 导出后自动验证shader内容
- 检测潜在的类型不匹配
- 输出警告信息到Unity Console

---

## 自动修复模式总结

| 错误模式 | 自动修复 | 示例 |
|---------|---------|------|
| `.xy.z` | → `.z` | `u_TilingOffset.xy.z` → `u_TilingOffset.z` |
| `.xy.w` | → `.w` | `u_TilingOffset.xy.w` → `u_TilingOffset.w` |
| `.x.y` | → `.y` | `value.x.y` → `value.y` |
| `vec2 uv = vec4Var;` | → `vec2 uv = vec4Var.xy;` | 自动添加.xy |
| `texture2D(tex, vec4)` | → `texture2D(tex, vec4.xy)` | UV参数修复 |
| `vec2(vec2Value.xy)` | → `vec2Value.xy` | 移除冗余构造 |
| `v_Texcoord0 + expr` | → `v_Texcoord0.xy + expr` | 算术运算修复 |

---

## 使用方法

### ⚠️ 重要：必须清理旧文件

**步骤1: 删除LayaAir项目中的旧Shader**
```bash
# 删除Shaders文件夹
rm -rf <LayaAir项目>/assets/*/Shaders/

# 或只删除特定shader
rm <LayaAir项目>/assets/*/Shaders/Artist_Effect_Effect_FullEffect.shader
```

**步骤2: 清理LayaAir IDE缓存**
```bash
# 删除缓存目录
rm -rf <LayaAir项目>/.laya/cache/
rm -rf <LayaAir项目>/.laya/shader_cache/

# 或在IDE中: 菜单 → 项目 → 清理缓存
```

**步骤3: 重启Unity Editor**
```
确保插件DLL被重新编译
关闭Unity → 重新打开Unity项目
```

**步骤4: 重新导出场景**
```
在Unity中使用LayaAir导出插件导出场景
检查Unity Console日志
```

---

## 验证修复效果

### Unity Console日志

导出时应该看到：

```
LayaAir3D: Applied swizzle access fix (vec2/vec3/vec4 invalid swizzle patterns)
LayaAir3D: Applied comprehensive type mismatch fixes
LayaAir3D: Shader 'Artist_Effect_Effect_FullEffect' validation passed (no obvious type mismatches detected)
LayaAir3D: Generated shader file: Shaders/Artist_Effect_Effect_FullEffect.shader
```

如果有潜在问题，会看到警告：

```
LayaAir3D: Shader 'Artist_Effect_Effect_FullEffect' validation found 2 potential issue(s):
  - Invalid swizzle access: u_TilingOffset.xy.z
  - Possible type mismatch: vec2 assignment from v_Texcoord0 without .xy
  Note: These may have been auto-fixed. Check the exported shader if compilation fails.
```

### LayaAir IDE验证

1. 刷新资源（F5）
2. 运行场景
3. 检查Console，shader编译错误应该消失
4. 观察渲染效果是否正常

---

## 调试技巧

### 如果还有错误

1. **查看导出的shader文件**
   ```bash
   # 在LayaAir项目中
   code <LayaAir项目>/assets/*/Shaders/Artist_Effect_Effect_FullEffect.shader
   ```

2. **搜索潜在问题**
   ```bash
   # 搜索错误的swizzle模式
   grep -n "\.xy\.[zw]" shader.shader

   # 搜索vec4到vec2的赋值
   grep -n "vec2.*=.*v_Texcoord0[^.]" shader.shader
   ```

3. **检查具体行号**
   - WebGL错误会给出行号
   - 在shader文件中定位该行
   - 检查是否有类型不匹配

### 手动修复模板

如果自动修复失败，可以手动修复：

```glsl
// 查找类似代码：
vec2 uv = v_Texcoord0;
texture2D(tex, v_Texcoord0);
float x = value.xy.z;

// 修改为：
vec2 uv = v_Texcoord0.xy;
texture2D(tex, v_Texcoord0.xy);
float x = value.z;
```

---

## 技术原理

### GLSL类型系统

GLSL是强类型语言：
- **vec2**: 2个float分量 (x, y)
- **vec3**: 3个float分量 (x, y, z)
- **vec4**: 4个float分量 (x, y, z, w)

类型转换规则：
- ✅ `vec4.xy` → vec2 (swizzle操作)
- ✅ `vec2(vec4)` → vec2 (构造函数截断)
- ❌ `vec2 = vec4` → 错误 (需要显式转换)

### Unity HLSL vs GLSL

Unity HLSL更宽松：
```hlsl
// Unity HLSL: 隐式转换（可能允许）
float2 uv = float4Value;
```

GLSL更严格：
```glsl
// GLSL: 必须显式转换
vec2 uv = vec4Value;        // ❌ 编译错误
vec2 uv = vec4Value.xy;     // ✅ 正确
vec2 uv = vec2(vec4Value);  // ✅ 也正确
```

### Swizzle操作

Swizzle是GLSL的向量分量访问语法：
```glsl
vec4 v = vec4(1, 2, 3, 4);
vec2 xy = v.xy;    // (1, 2)
vec2 zw = v.zw;    // (3, 4)
vec3 xyz = v.xyz;  // (1, 2, 3)
float z = v.z;     // 3

// ❌ 错误: 链式swizzle
float z = v.xy.z;  // vec2没有.z分量
```

---

## 常见问题

### Q1: 为什么修复后还报错？

**A**: 可能的原因：
1. ❌ 使用了缓存的shader → 清理 `.laya/cache/`
2. ❌ Unity没有重新编译插件 → 重启Unity Editor
3. ❌ LayaAir IDE缓存了旧shader → 清理缓存并刷新
4. ❌ 有其他类型的错误 → 查看具体错误信息

### Q2: 修复会影响正确的代码吗？

**A**: 不会。修复使用精确的正则表达式：
- 只修复明确的错误模式
- 使用负向前瞻避免误匹配
- 只在明显需要时添加.xy后缀

### Q3: 如何确认自动修复生效？

**A**: 三种方式：
1. Unity Console有 "Applied comprehensive type mismatch fixes" 日志
2. 导出的shader文件中没有 `.xy.z` 等错误模式
3. LayaAir IDE中shader编译成功

### Q4: 验证警告是否意味着有问题？

**A**: 不一定：
- 警告只是提示可能的问题
- 实际的错误已经在修复阶段自动修正了
- 如果shader编译成功，可以忽略警告
- 如果编译失败，警告有助于定位问题

---

## 相关文档

- **Swizzle错误修复**: `SWIZZLE_ERROR_COMPREHENSIVE_FIX.md`
- **材质导出修复**: `MATERIAL_EXPORT_UNIVERSAL_FIX.md`
- **粒子优化**: `PARTICLE_MESH_OPTIMIZATION.md`

---

## 修改总结

| 文件 | 修改内容 | 行数 |
|------|---------|------|
| CustomShaderExporter.cs | 增强FixShaderTypeMismatch函数 | ~5504-5650 |
| CustomShaderExporter.cs | 新增FixVec4ToVec2Assignments | ~5650-5700 |
| CustomShaderExporter.cs | 新增RemoveRedundantVec2Constructors | ~5700-5720 |
| CustomShaderExporter.cs | 新增FixFunctionParameterTypeMismatch | ~5720-5750 |
| CustomShaderExporter.cs | 新增ValidateShaderContent | ~5750-5830 |
| CustomShaderExporter.cs | 添加shader验证调用 | ~299 |

---

## 预期效果

修复后：
- ✅ 所有shader类型不匹配自动修复
- ✅ 导出时自动验证shader内容
- ✅ Unity Console提供详细的修复和验证日志
- ✅ LayaAir IDE中shader编译成功
- ✅ 渲染效果与Unity一致

---

## 调试流程图

```
导出Shader
    ↓
FixShaderTypeMismatch (自动修复)
    ├─ 修复swizzle访问
    ├─ 修复v_Texcoord0类型
    ├─ 修复vec4到vec2赋值
    ├─ 移除冗余vec2()
    └─ 修复函数参数类型
    ↓
ValidateShaderContent (验证)
    ├─ 检测剩余的类型问题
    └─ 输出警告到Console
    ↓
保存Shader文件
    ↓
在LayaAir IDE中
    ├─ 刷新资源 (F5)
    ├─ Shader编译
    └─ 成功 ✅ / 失败 ❌
           ↓ (失败)
      查看Console错误
      定位具体行号
      手动修复或调整规则
```
