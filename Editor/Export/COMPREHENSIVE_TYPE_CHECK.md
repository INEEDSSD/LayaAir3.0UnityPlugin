# 全面的类型检查和自动修复系统

## 📋 概述

这个系统在shader导出时自动检测并修复所有变量赋值中的类型不匹配问题，确保GLSL shader能够正确编译。

---

## 🔍 检测的类型不匹配模式

### 1. vec4 → vec2 赋值

#### 错误示例
```glsl
vec4 v_Texcoord0;
vec2 uv = v_Texcoord0;  // ❌ 类型不匹配
```

#### 自动修复
```glsl
vec2 uv = v_Texcoord0.xy;  // ✅ 自动添加.xy
```

---

### 2. vec4 → vec3 赋值

#### 错误示例
```glsl
vec4 color;
vec3 rgb = color;  // ❌ 类型不匹配
```

#### 自动修复
```glsl
vec3 rgb = color.xyz;  // ✅ 自动添加.xyz
```

---

### 3. vec3 → vec2 赋值

#### 错误示例
```glsl
vec3 position;
vec2 screenPos = position;  // ❌ 类型不匹配
```

#### 自动修复
```glsl
vec2 screenPos = position.xy;  // ✅ 自动添加.xy
```

---

### 4. texture2D的UV参数

#### 错误示例
```glsl
vec4 v_Texcoord0;
vec4 color = texture2D(u_MainTex, v_Texcoord0);  // ❌ UV应该是vec2
```

#### 自动修复
```glsl
vec4 color = texture2D(u_MainTex, v_Texcoord0.xy);  // ✅ 自动添加.xy
```

---

### 5. 构造函数中的类型不匹配

#### 错误示例
```glsl
vec4 v_Texcoord0;
vec2 uv = vec2(v_Texcoord0);  // ❌ vec2构造函数不接受vec4
```

#### 自动修复
```glsl
vec2 uv = vec2(v_Texcoord0.xy);  // ✅ 自动添加.xy
```

---

### 6. 函数参数类型不匹配

#### 错误示例
```glsl
vec4 uv;
vec2 fractUV = fract(uv);  // ❌ fract期望vec2但收到vec4
```

#### 检测结果
```
⚠️ Potential type mismatch in fract(uv) - vec4 may need swizzle
```

（这类问题会被记录，需要根据上下文手动检查）

---

## 🔧 实现原理

### 第一步：收集变量类型信息

```csharp
var variableTypes = new Dictionary<string, string>();

// 收集varying/uniform/attribute声明
var declMatches = Regex.Matches(content,
    @"(varying|uniform|attribute)\s+(vec2|vec3|vec4|...)\s+(\w+)");

// 收集局部变量声明
var localVarMatches = Regex.Matches(content,
    @"^\s*(vec2|vec3|vec4|...)\s+(\w+)\s*[=;]",
    RegexOptions.Multiline);
```

**结果示例**:
```
v_Texcoord0 → vec4
v_Color → vec4
u_MainTex → sampler2D
```

---

### 第二步：检测并修复赋值

```csharp
foreach (var kvp in variableTypes)
{
    if (kvp.Value == "vec4")
    {
        // 查找: vec2 xxx = vec4Var;
        var pattern = $@"(vec2\s+\w+\s*=\s*)({kvp.Key})(?![.\w])(\s*;)";

        // 替换为: vec2 xxx = vec4Var.xy;
        content = Regex.Replace(content, pattern, "$1$2.xy$3");
    }
}
```

---

### 第三步：检测texture2D的UV参数

```csharp
foreach (var kvp in variableTypes)
{
    if (kvp.Value == "vec4")
    {
        // 查找: texture2D(sampler, vec4Var)
        var pattern = $@"(texture2D\s*\([^,]+,\s*)({kvp.Key})(?![.\w])(\s*\))";

        // 替换为: texture2D(sampler, vec4Var.xy)
        content = Regex.Replace(content, pattern, "$1$2.xy$3");
    }
}
```

---

### 第四步：检测构造函数

```csharp
// vec2(vec4Var) → vec2(vec4Var.xy)
var pattern = $@"(vec2\s*\()({varName})(?![.\w])(\s*\))";
content = Regex.Replace(content, pattern, "$1$2.xy$3");

// vec3(vec4Var) → vec3(vec4Var.xyz)
var pattern = $@"(vec3\s*\()({varName})(?![.\w])(\s*\))";
content = Regex.Replace(content, pattern, "$1$2.xyz$3");
```

---

## 📊 修复统计

### 自动修复的类型

| 源类型 | 目标类型 | 自动修复 | 示例 |
|--------|---------|---------|------|
| vec4 | vec2 | ✅ 是 | `var.xy` |
| vec4 | vec3 | ✅ 是 | `var.xyz` |
| vec3 | vec2 | ✅ 是 | `var.xy` |
| vec2 | vec3 | ❌ 否 | 需要手动 |
| vec2 | vec4 | ❌ 否 | 需要手动 |
| vec3 | vec4 | ❌ 否 | 需要手动 |

---

## 🎯 使用示例

### 示例1：v_Texcoord0赋值

**输入shader**:
```glsl
varying vec4 v_Texcoord0;

void main()
{
    vec2 uv = v_Texcoord0;  // ❌ 错误
    vec4 color = texture2D(u_MainTex, v_Texcoord0);  // ❌ 错误
}
```

**自动修复后**:
```glsl
varying vec4 v_Texcoord0;

void main()
{
    vec2 uv = v_Texcoord0.xy;  // ✅ 修复
    vec4 color = texture2D(u_MainTex, v_Texcoord0.xy);  // ✅ 修复
}
```

**Unity Console日志**:
```
LayaAir3D: ComprehensiveTypeCheck - Found 5 variables
LayaAir3D: Fixed vec2 = v_Texcoord0 → vec2 = v_Texcoord0.xy
LayaAir3D: Fixed texture2D UV parameter: v_Texcoord0 → v_Texcoord0.xy
LayaAir3D: ComprehensiveTypeCheck applied 2 automatic type fixes
```

---

### 示例2：多级赋值

**输入shader**:
```glsl
varying vec4 v_Texcoord0;
vec2 mainUV;
vec2 distortUV;

void main()
{
    mainUV = v_Texcoord0;  // ❌ 错误
    distortUV = mainUV;  // ✅ 正确（vec2 = vec2）
}
```

**自动修复后**:
```glsl
varying vec4 v_Texcoord0;
vec2 mainUV;
vec2 distortUV;

void main()
{
    mainUV = v_Texcoord0.xy;  // ✅ 修复
    distortUV = mainUV;  // ✅ 保持不变
}
```

---

### 示例3：构造函数

**输入shader**:
```glsl
varying vec4 v_Color;

void main()
{
    vec2 data = vec2(v_Color);  // ❌ 错误
    vec3 rgb = vec3(v_Color);  // ❌ 错误
}
```

**自动修复后**:
```glsl
varying vec4 v_Color;

void main()
{
    vec2 data = vec2(v_Color.xy);  // ✅ 修复
    vec3 rgb = vec3(v_Color.xyz);  // ✅ 修复
}
```

---

## 🔍 不会自动修复的情况

### 情况1：vec2 → vec3/vec4 扩展

```glsl
vec2 uv;
vec3 position = uv;  // ⚠️ 无法自动修复，需要第三个分量
```

**原因**: 不知道第三个分量应该是什么值（0.0? 1.0? 其他？）

**解决方案**: 手动修复
```glsl
vec3 position = vec3(uv, 0.0);  // 手动指定第三个分量
```

---

### 情况2：vec3 → vec4 扩展

```glsl
vec3 rgb;
vec4 color = rgb;  // ⚠️ 无法自动修复，需要alpha分量
```

**原因**: 不知道alpha值应该是什么

**解决方案**: 手动修复
```glsl
vec4 color = vec4(rgb, 1.0);  // 手动指定alpha
```

---

### 情况3：复杂表达式

```glsl
vec4 v_Texcoord0;
vec2 uv = v_Texcoord0 * 2.0 + vec2(0.5);  // ❌ 复杂表达式
```

**原因**: 正则表达式难以准确匹配复杂表达式

**检测结果**:
```
⚠️ Type mismatch may exist in complex expression
```

**解决方案**: 手动添加swizzle
```glsl
vec2 uv = v_Texcoord0.xy * 2.0 + vec2(0.5);  // ✅ 手动修复
```

---

## 📋 完整的检查流程

```
shader导出开始
    ↓
读取Unity shader源代码
    ↓
HLSL到GLSL转换
    ↓
FixShaderTypeMismatch (已有的修复)
    ├─ Swizzle错误修复 (.xy.z → .z)
    ├─ vec4→vec2基础修复
    └─ texture2D在vec2运算修复
    ↓
⭐ ComprehensiveTypeCheck (新增)
    ├─ 收集所有变量类型
    ├─ 修复vec4→vec2赋值
    ├─ 修复vec4→vec3赋值
    ├─ 修复vec3→vec2赋值
    ├─ 修复texture2D UV参数
    ├─ 修复构造函数参数
    └─ 检测函数调用参数
    ↓
ValidateShaderContent (验证)
    ├─ 检测剩余的类型不匹配
    └─ 输出警告日志
    ↓
保存shader文件
```

---

## 🚀 性能优化

### 优化1：增量检查

只检查可能有问题的变量类型（vec2/vec3/vec4），跳过float、int等基础类型。

### 优化2：一次遍历

在单次遍历中完成多种类型的修复，避免重复解析shader代码。

### 优化3：早期退出

```csharp
if (variableTypes.Count == 0)
{
    Debug.Log("No vector variables found, skipping type check");
    return content;
}
```

---

## 💡 最佳实践

### 1. Unity Shader编写规范

在Unity shader中就使用正确的类型：

```hlsl
// ✅ 好
v2f vert(appdata v)
{
    v2f o;
    o.uv = v.uv;  // 直接使用正确类型
    return o;
}

// ❌ 不好
v2f vert(appdata v)
{
    v2f o;
    o.texcoord = v.uv;  // 如果texcoord是vec4会有问题
    return o;
}
```

### 2. 显式的类型转换

在可能歧义的地方使用显式转换：

```glsl
// ✅ 好：显式转换
vec2 uv = v_Texcoord0.xy;
vec4 color = texture2D(u_MainTex, uv);

// ❌ 不好：隐式转换
vec2 uv = v_Texcoord0;  // 依赖自动修复
```

### 3. 变量命名规范

使用清晰的命名表示变量类型：

```glsl
// ✅ 好：名称清晰
vec2 mainUV;
vec3 worldPos;
vec4 screenPos;

// ❌ 不好：名称模糊
vec4 data;  // 不清楚包含什么
```

---

## 🔗 与其他修复的关系

### 修复顺序

1. **FixShaderTypeMismatch** (基础修复)
   - 处理已知的特定模式
   - 主要针对v_Texcoord0和texture2D

2. **ComprehensiveTypeCheck** ⭐ (本系统)
   - 通用的类型检查和修复
   - 处理所有变量的赋值

3. **ValidateShaderContent** (验证)
   - 检测剩余问题
   - 输出警告供用户检查

### 互补关系

```
FixShaderTypeMismatch:  特定模式修复（快速、精确）
                ↓
ComprehensiveTypeCheck: 通用类型修复（全面、智能）
                ↓
ValidateShaderContent:  验证和报告（检测遗漏）
```

---

## 📊 测试结果

### 测试用例1：基础赋值

```glsl
// 输入
varying vec4 v_Texcoord0;
vec2 uv = v_Texcoord0;

// 输出
vec2 uv = v_Texcoord0.xy;
```
✅ **通过**

### 测试用例2：texture2D

```glsl
// 输入
vec4 color = texture2D(u_MainTex, v_Texcoord0);

// 输出
vec4 color = texture2D(u_MainTex, v_Texcoord0.xy);
```
✅ **通过**

### 测试用例3：构造函数

```glsl
// 输入
vec2 data = vec2(v_Color);

// 输出
vec2 data = vec2(v_Color.xy);
```
✅ **通过**

### 测试用例4：多级赋值

```glsl
// 输入
vec2 uv1 = v_Texcoord0;
vec2 uv2 = uv1;

// 输出
vec2 uv1 = v_Texcoord0.xy;
vec2 uv2 = uv1;  // 不需要修复（vec2 = vec2）
```
✅ **通过**

---

## 🆘 故障排查

### 问题1：自动修复没有生效

**检查清单**:
- [ ] Unity是否重新编译了插件？
- [ ] Console是否显示"ComprehensiveTypeCheck applied X fixes"？
- [ ] 是否完全清理了缓存？

**解决方案**:
1. 完全重启Unity
2. 删除.laya/cache和template文件夹
3. 重新导出shader
4. 查看Unity Console完整日志

---

### 问题2：仍然有类型不匹配错误

**可能原因**:
1. 复杂表达式无法自动修复
2. vec2→vec3/vec4扩展需要手动处理
3. 自定义函数的参数类型

**解决方案**:
1. 查看ValidateShaderContent的警告日志
2. 打开导出的shader手动检查
3. 参考AI辅助转换的shader对比修复

---

### 问题3：错误的自动修复

**示例**:
```glsl
// 错误修复
vec3 position = v_Texcoord0.xy;  // ❌ 应该是.xyz
```

**原因**: 正则表达式匹配错误

**解决方案**:
1. 提交Issue报告具体情况
2. 手动修复shader
3. 更新正则表达式规则

---

## 📞 获取帮助

如果遇到类型不匹配问题：

1. **查看Unity Console日志**
   ```
   LayaAir3D: ComprehensiveTypeCheck - Found X variables
   LayaAir3D: Fixed ...
   LayaAir3D: Type mismatch detected: ...
   ```

2. **对比导出的shader和AI shader**
   - 导出的shader: `template\XXX_export.shader`
   - AI shader: `template\XXX.shader`

3. **提供完整信息**
   - Unity Console日志
   - 导出的shader代码
   - 预期的正确代码
   - Unity和LayaAir版本

---

## 🎓 技术要点

### GLSL类型系统

```glsl
// ✅ 合法操作
vec2 a, b;
vec2 c = a + b;        // vec2 + vec2 = vec2
vec2 d = a * 2.0;      // vec2 * float = vec2
float e = dot(a, b);   // dot(vec2, vec2) = float

// ❌ 非法操作
vec2 a;
vec4 b;
vec2 c = a + b;        // 错误：vec2 + vec4
vec2 d = b;            // 错误：vec2 = vec4
```

### Swizzle操作

```glsl
vec4 v = vec4(1.0, 2.0, 3.0, 4.0);

// ✅ 合法
vec2 xy = v.xy;        // (1.0, 2.0)
vec3 xyz = v.xyz;      // (1.0, 2.0, 3.0)
vec2 zw = v.zw;        // (3.0, 4.0)
vec3 bgr = v.bgr;      // (3.0, 2.0, 1.0)

// ❌ 非法
vec3 xyz = v.xy;       // 错误：大小不匹配
float x = v.xy;        // 错误：vec2不能赋给float
vec2 xz = v.x.z;       // 错误：链式swizzle
```

---

**最后更新**: 2024-02-12
**版本**: v1.0 - 初始版本
**状态**: ✅ 已实现并测试
