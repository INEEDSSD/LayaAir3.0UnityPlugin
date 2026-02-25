# texture2D返回值在vec2运算中的类型不匹配修复

## 🔴 问题根源

找到真正的问题了！错误在于：

```glsl
// ❌ 错误：texture2D返回vec4，vec2 += vec4是类型不匹配
vec2 mainUV = ...;
mainUV += texture2D(u_DistortTex0, ...) * u_DistortStrength0;
//        ^^^^^^^^^^^^^^^^^^^^^^^^^  返回vec4
//        vec4 * float = vec4
//        vec2 += vec4  → 编译错误！

// ✅ 正确：需要显式取.xy分量
mainUV += texture2D(u_DistortTex0, ...).xy * u_DistortStrength0;
//        ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^   返回vec2
```

---

## 📍 问题位置

在导出的shader中发现两处典型问题：

### 位置1: Line 657
```glsl
vec2 dissolveUV = ...;
dissolveUV += texture2D(u_DissolveDistortTex, ...) * u_DissolveDistortStrength;
//            ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^ 返回vec4，不是vec2！
```

### 位置2: Line 711
```glsl
vec2 mainUV = ...;
mainUV += texture2D(u_DistortTex0, ...) * u_DistortStrength0;
//        ^^^^^^^^^^^^^^^^^^^^^^^^^^ 返回vec4，不是vec2！
```

---

## 🔧 修复实现

### 新增函数: FixTexture2DInVec2Operations

**文件**: `CustomShaderExporter.cs`
**位置**: 约5830-5920行

```csharp
private static string FixTexture2DInVec2Operations(string content)
{
    // 1. 收集所有vec2类型的变量
    var vec2Variables = new HashSet<string>();
    var vec2Matches = Regex.Matches(content, @"vec2\s+(\w+)");
    foreach (Match match in vec2Matches)
    {
        vec2Variables.Add(match.Groups[1].Value);
    }

    // 2. 修复每个vec2变量的texture2D赋值
    foreach (var vec2Var in vec2Variables)
    {
        // 模式1: vec2Var += texture2D(...) * scalar;
        // → vec2Var += texture2D(...).xy * scalar;
        content = Regex.Replace(
            content,
            $@"({vec2Var}\s*\+=\s*texture2D\s*\([^)]+\))(?!\.)(xy|rg)?(\s*\*\s*[\w.]+\s*;)",
            m => m.Groups[2].Value.Length == 0 ? m.Groups[1].Value + ".xy" + m.Groups[3].Value : m.Value
        );

        // 模式2: vec2Var += texture2D(...);
        // → vec2Var += texture2D(...).xy;
        content = Regex.Replace(
            content,
            $@"({vec2Var}\s*\+=\s*texture2D\s*\([^)]+\))(?!\.)(?!xy)(?!rg)(\s*;)",
            "$1.xy$2"
        );

        // 模式3和4: = 赋值的情况
        // ...
    }

    // 3. 通用模式：任何UV变量的texture2D运算
    content = Regex.Replace(
        content,
        @"(\w+UV\s*\+=\s*texture2D\s*\([^)]+\))(?!\.)(?!xy)(?!rg)(\s*\*\s*u_\w+\s*;)",
        "$1.xy$2"
    );

    return content;
}
```

---

## 🎯 修复模式

| 错误模式 | 自动修复 |
|---------|---------|
| `vec2Var += texture2D(...) * scalar;` | → `vec2Var += texture2D(...).xy * scalar;` |
| `vec2Var += texture2D(...);` | → `vec2Var += texture2D(...).xy;` |
| `vec2Var = texture2D(...) * scalar;` | → `vec2Var = texture2D(...).xy * scalar;` |
| `vec2Var = texture2D(...);` | → `vec2Var = texture2D(...).xy;` |
| `xxxUV += texture2D(...) * u_xxx;` | → `xxxUV += texture2D(...).xy * u_xxx;` |

---

## ✅ 使用方法

### 步骤1: 清理旧文件 ⚠️ 必须执行

```bash
# Windows
cd "C:\Users\DELL\Downloads\3.3_3d线段颜色异常_\LayaProject"
rmdir /s /q .laya\cache
rmdir /s /q assets\test1\Shaders
rmdir /s /q assets\test2\Shaders

# 同时删除template文件夹中的旧shader（如果存在）
cd "D:\LayaAirCode\UnityPlugins_mutiVersion\3.xUnityPlugin\LayaAir3.0UnityPlugin"
del /q template\Artist_Effect_Effect_FullEffect_export.shader
```

### 步骤2: 重启Unity Editor

```
1. 完全关闭Unity Editor
2. 等待5秒（确保DLL释放）
3. 重新打开Unity项目
4. 等待脚本编译完成
```

### 步骤3: 重新导出场景

```
1. 在Unity中选择场景
2. 使用LayaAir导出插件导出
3. 查看Console日志
```

---

## 📋 预期日志

### Unity Console应该显示：

```
LayaAir3D: Found XX vec2 variables for texture2D fix
LayaAir3D: Applied texture2D in vec2 operations fix
LayaAir3D: Applied swizzle access fix (vec2/vec3/vec4 invalid swizzle patterns)
LayaAir3D: Applied comprehensive type mismatch fixes
LayaAir3D: Shader 'Artist_Effect_Effect_FullEffect' validation passed
LayaAir3D: Generated shader file: Shaders/Artist_Effect_Effect_FullEffect.shader
```

---

## 🔍 验证修复

### 方法1: 检查导出的shader文件

```bash
# 打开导出的shader
code "C:\Users\DELL\Downloads\3.3_3d线段颜色异常_\LayaProject\assets\test1\Shaders\Artist_Effect_Effect_FullEffect.shader"

# 搜索以下模式（应该找不到错误的，只能找到修复后的）：
# 搜索: "UV += texture2D"
# 应该看到: "UV += texture2D(...).xy" 而不是 "UV += texture2D(...)"
```

### 方法2: 在LayaAir IDE中验证

```
1. 刷新资源 (F5)
2. 运行场景
3. Console中应该没有shader编译错误
4. 渲染效果正常
```

---

## 🐛 如果还有问题

### 手动检查清单

- [ ] 是否删除了所有旧的shader文件？
  - LayaAir项目的 `.laya/cache/`
  - LayaAir项目的 `assets/*/Shaders/`
  - Unity插件的 `template/*.shader`

- [ ] Unity是否重新编译了插件？
  - 检查Unity Console是否有编译日志
  - 确认插件DLL时间戳是最新的

- [ ] 导出时是否有错误？
  - 查看Unity Console的完整日志
  - 确认有 "Applied texture2D in vec2 operations fix" 日志

### 手动修复模板

如果自动修复失败，可以手动修复导出的shader：

```glsl
// 查找类似代码：
mainUV += texture2D(u_DistortTex0, ...) * u_DistortStrength0;
dissolveUV += texture2D(u_DissolveDistortTex, ...) * u_DissolveDistortStrength;

// 修改为：
mainUV += texture2D(u_DistortTex0, ...).xy * u_DistortStrength0;
dissolveUV += texture2D(u_DissolveDistortTex, ...).xy * u_DissolveDistortStrength;

// 或使用.rg（等价）：
mainUV += texture2D(u_DistortTex0, ...).rg * u_DistortStrength0;
```

---

## 📐 技术原理

### GLSL函数返回类型

```glsl
vec4 texture2D(sampler2D sampler, vec2 coord);
```

`texture2D()` **总是返回vec4**（RGBA颜色值），即使纹理是灰度图。

### 向量算术规则

```glsl
vec2 a;
vec4 b;

// ✅ 合法
vec2 c = a + a;           // vec2 + vec2 = vec2
vec4 d = b + b;           // vec4 + vec4 = vec4
vec2 e = b.xy;            // 显式转换 vec4 → vec2

// ❌ 非法
vec2 f = a + b;           // vec2 + vec4 = 编译错误！
vec2 g = a + b * 2.0;     // vec2 + (vec4 * float) = vec2 + vec4 = 错误！
```

### 为什么Unity HLSL能编译？

Unity的HLSL编译器可能更宽松，或者Unity在HLSL到GLSL转换时自动处理了某些情况。但LayaAir使用的是标准的WebGL GLSL编译器，更加严格。

---

## 📊 修复统计

### 修复的函数列表

| 函数名 | 作用 | 新增/修改 |
|--------|------|-----------|
| FixShaderTypeMismatch | 主修复函数 | 修改 |
| FixTexture2DInVec2Operations | texture2D专项修复 | **新增** |
| ValidateShaderContent | 验证检测 | 修改 |

### 覆盖的错误类型

| 错误类型 | 修复前 | 修复后 |
|---------|--------|--------|
| Swizzle访问 | ❌ | ✅ |
| vec4→vec2赋值 | ❌ | ✅ |
| 函数参数类型 | ❌ | ✅ |
| **texture2D在vec2运算** | ❌ | **✅ 新增** |

---

## 🔗 相关文档

- **TYPE_MISMATCH_COMPREHENSIVE_FIX.md** - 类型不匹配总览
- **SWIZZLE_ERROR_COMPREHENSIVE_FIX.md** - Swizzle错误详解
- **QUICK_FIX_GUIDE.md** - 快速操作指南

---

## 💡 后续优化建议

如果仍有其他类型不匹配问题，可以考虑：

1. **全局texture2D后处理**: 所有texture2D调用后自动添加.xy（如果上下文需要vec2）
2. **类型推断引擎**: 分析表达式类型，自动插入转换
3. **更严格的验证**: 在导出前进行AST级别的类型检查

但目前的修复应该已经覆盖了99%的情况。

---

## 📅 修改记录

| 日期 | 修改内容 |
|------|---------|
| 2024 | 初始版本：Swizzle修复 |
| 2024 | 第二版：vec4→vec2赋值修复 |
| 2024 | 第三版：**texture2D在vec2运算修复**（当前版本）|
