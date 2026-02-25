# Swizzle访问错误全面修复

## 错误描述

```
ERROR: 0:264: 'assign' : cannot convert from 'highp 4-component vector of float' to 'highp 2-component vector of float'
```

这是GLSL着色器编译错误，由**错误的swizzle访问**导致，例如：
- `vec2Value.z` - vec2只有x,y分量，没有z
- `vec2Value.w` - vec2只有x,y分量，没有w
- `u_TilingOffset.xy.z` - .xy返回vec2，vec2没有.z

---

## 根本原因

Unity HLSL到LayaAir GLSL转换过程中，可能产生错误的链式swizzle访问：

```glsl
// ❌ 错误：.xy返回vec2，vec2没有.z分量
float offsetZ = u_TilingOffset.xy.z;

// ✅ 正确：直接访问.z分量
float offsetZ = u_TilingOffset.z;
```

---

## 修复内容

### 修复位置1: FixShaderTypeMismatch函数
**文件**: `CustomShaderExporter.cs`
**位置**: 约5509-5525行

添加了全面的swizzle修复模式：

```csharp
// ⭐ 0. 修复错误的swizzle访问 (CRITICAL FIX)
// 修复 .xy.z 和 .xy.w (vec2错误访问z/w分量)
content = Regex.Replace(content, @"(\w+)\.xy\.z\b", "$1.z");
content = Regex.Replace(content, @"(\w+)\.xy\.w\b", "$1.w");

// 修复 .xyz.w (vec3错误访问w分量)
content = Regex.Replace(content, @"(\w+)\.xyz\.w\b", "$1.w");

// 修复单分量后再访问其他分量的错误
content = Regex.Replace(content, @"(\w+)\.x\.y\b", "$1.y");
content = Regex.Replace(content, @"(\w+)\.x\.z\b", "$1.z");
content = Regex.Replace(content, @"(\w+)\.x\.w\b", "$1.w");
content = Regex.Replace(content, @"(\w+)\.y\.z\b", "$1.z");
content = Regex.Replace(content, @"(\w+)\.y\.w\b", "$1.w");
content = Regex.Replace(content, @"(\w+)\.z\.w\b", "$1.w");
```

### 修复位置2: ConvertUnityShaderToLaya函数
**文件**: `CustomShaderExporter.cs`
**位置**: 约3875-3888行

在HLSL到GLSL转换中也应用了相同的修复。

---

## 修复的错误模式

| 错误模式 | 原因 | 修复 |
|---------|------|------|
| `.xy.z` | vec2没有z分量 | → `.z` |
| `.xy.w` | vec2没有w分量 | → `.w` |
| `.xyz.w` | vec3没有w分量 | → `.w` |
| `.x.y` | float没有y分量 | → `.y` |
| `.x.z` | float没有z分量 | → `.z` |
| `.x.w` | float没有w分量 | → `.w` |
| `.y.z` | float没有z分量 | → `.z` |
| `.y.w` | float没有w分量 | → `.w` |
| `.z.w` | float没有w分量 | → `.w` |

---

## 如何应用修复

### 步骤1: 删除已导出的Shader文件

**⚠️ 重要**: 必须删除之前导出的shader文件，否则旧的错误shader仍然会被使用。

```bash
# 在LayaAir项目目录中
# 删除Shaders文件夹中的Artist_Effect相关shader
cd <LayaAir项目路径>/assets/Shaders
rm Artist_Effect_Effect_FullEffect.shader
# 或者删除整个Shaders文件夹
rm -rf Shaders/
```

### 步骤2: 清理LayaAir Shader缓存

LayaAir IDE可能缓存了shader编译结果，需要清理：

**方法1: 删除缓存文件夹**
```bash
# 在LayaAir项目目录
rm -rf .laya/cache/
rm -rf .laya/shader_cache/
```

**方法2: 在LayaAir IDE中**
1. 菜单: `项目` → `清理缓存`
2. 或者: 关闭IDE，手动删除 `.laya` 文件夹，重新打开

### 步骤3: 重新导出Unity场景

在Unity中：
1. 删除之前导出的文件夹
2. 使用LayaAir导出插件重新导出场景
3. 检查Unity Console，应该看到：
   ```
   LayaAir3D: Applied swizzle access fix (vec2/vec3/vec4 invalid swizzle patterns)
   ```

### 步骤4: 验证修复

在LayaAir IDE中：
1. 刷新资源（F5）
2. 运行场景
3. 检查Console，shader编译错误应该消失
4. 观察渲染效果是否正常

---

## 调试日志

修复应用后，会在Unity Console中看到：

```
LayaAir3D: Applied swizzle access fix (vec2/vec3/vec4 invalid swizzle patterns)
```

如果在导出后的shader文件中仍然看到 `.xy.z` 或类似模式，说明修复未生效，请检查：
1. 插件代码是否正确保存
2. Unity是否重新编译了插件DLL
3. 是否重启了Unity Editor

---

## 常见问题

### Q1: 为什么修复后还是报错？

**A**: 可能原因：
1. ❌ 使用了旧的shader缓存 → 删除LayaAir的 `.laya/cache/` 文件夹
2. ❌ Unity使用了旧的插件DLL → 重启Unity Editor
3. ❌ 没有删除旧的导出文件 → 删除Shaders文件夹

### Q2: 修复会影响正常的shader吗？

**A**: 不会。修复只替换**错误的**swizzle模式：
- ✅ 正常的 `.xyz` 不会被修改
- ✅ 正常的 `.xy` 不会被修改
- ❌ 只有 `.xy.z` 这种错误模式才会被修复为 `.z`

### Q3: 如何确认修复已应用？

**A**: 三种方式：
1. Unity Console有 "Applied swizzle access fix" 日志
2. 导出的.shader文件中搜索 `.xy.z`，应该找不到
3. LayaAir IDE中shader编译成功

---

## 技术细节

### GLSL Swizzle规则

GLSL中的swizzle访问规则：
- **vec2**: 可以访问 `.x` `.y` `.xy` `.yx` `.xx` `.yy`
- **vec3**: 可以访问 `.x` `.y` `.z` `.xyz` `.rgb` 等
- **vec4**: 可以访问 `.x` `.y` `.z` `.w` `.xyzw` `.rgba` 等

**❌ 错误**: 链式swizzle后访问不存在的分量
```glsl
vec4 v = vec4(1, 2, 3, 4);
float a = v.xy.z;  // ❌ v.xy返回vec2，vec2没有.z
```

**✅ 正确**: 直接访问原始向量的分量
```glsl
vec4 v = vec4(1, 2, 3, 4);
float a = v.z;     // ✅ 直接访问vec4的.z
```

### Unity HLSL vs GLSL差异

Unity HLSL和GLSL在向量访问上的差异：
- Unity HLSL可能允许更灵活的swizzle语法
- GLSL规范更严格，不允许链式swizzle访问不存在的分量
- 转换过程中需要识别并修复这些差异

---

## 相关文档

- **材质导出修复**: `MATERIAL_EXPORT_UNIVERSAL_FIX.md`
- **Shader导出文档**: `MESH_SIMPLIFIER_SETUP.md`
- **粒子Mesh优化**: `PARTICLE_MESH_OPTIMIZATION.md`

---

## 修改记录

| 日期 | 修改内容 |
|------|---------|
| 2024 | 首次添加 .xy.z 和 .xy.w 修复 |
| 2024 | 扩展为全面的swizzle修复，覆盖所有错误模式 |
| 2024 | 在FixShaderTypeMismatch和ConvertUnityShaderToLaya两处都应用修复 |
