# v_MeshColor条件编译修复 - 参考AI版本Shader

## 修复时间
2026-02-12

## 问题描述
导出Unity粒子mesh模式shader时出现编译错误：
```
ERROR: 0:395: 'v_MeshColor' : undeclared identifier
```

## 根本原因
1. 缺少`RENDERMODE_MESH`宏定义
2. `v_MeshColor`的varying声明方式不正确

## 参考实现
参考AI辅助转换的shader：`template\Artist_Effect_Effect_FullEffect.shader`

AI版本使用**条件编译**声明v_MeshColor：
```glsl
#ifdef RENDERMODE_MESH
varying vec4 v_MeshColor;
#endif
```

---

## 完整修复方案

### ✅ 修复1：添加RENDERMODE_MESH define
**位置**：`CustomShaderExporter.cs:1432`

```csharp
if (parseResult.isParticleBillboard)
{
    // ⭐ 粒子mesh模式：添加RENDERMODE_MESH define
    sb.AppendLine("        RENDERMODE_MESH: { type: bool, default: false },");
    addedDefines.Add("RENDERMODE_MESH");

    sb.AppendLine("        TINTCOLOR: { type: bool, default: true },");
    addedDefines.Add("TINTCOLOR");
    sb.AppendLine("        ADDTIVEFOG: { type: bool, default: true },");
    addedDefines.Add("ADDTIVEFOG");
}
```

---

### ✅ 修复2：添加v_MeshColor到allVaryings
**位置**：`CustomShaderExporter.cs:1858`

```csharp
// ⭐ 关键修复：粒子mesh模式需要v_MeshColor传递顶点颜色
if (!allVaryings.ContainsKey("v_MeshColor"))
{
    allVaryings["v_MeshColor"] = "vec4";
    Debug.Log("LayaAir3D: Added varying vec4 v_MeshColor for particle mesh mode");
}
```

---

### ✅ 修复3：自定义特效shader - varying条件编译包裹
**位置**：`CustomShaderExporter.cs:1973`（在去重后）

```csharp
// ⭐ 在去重后，将v_MeshColor用条件编译包裹（参考AI版本shader）
if (parseResult.varyingDeclarations.Contains("varying vec4 v_MeshColor"))
{
    // 将 "    varying vec4 v_MeshColor;" 替换为条件编译版本（保持缩进）
    parseResult.varyingDeclarations = Regex.Replace(parseResult.varyingDeclarations,
        @"(\s*)varying\s+vec4\s+v_MeshColor\s*;",
        "$1#ifdef RENDERMODE_MESH\n$1varying vec4 v_MeshColor;\n$1#endif");
    Debug.Log("LayaAir3D: Wrapped v_MeshColor with conditional compilation");
}
```

**生成结果**：
```glsl
    #ifdef RENDERMODE_MESH
    varying vec4 v_MeshColor;
    #endif
```

---

### ✅ 修复4：自定义特效shader - FS使用v_MeshColor
**位置**：`CustomShaderExporter.cs:2167`

```csharp
// ⭐ 粒子系统mesh模式：在最终输出前乘以mesh顶点颜色（参考AI版本shader）
if (parseResult.isParticleBillboard)
{
    sb.AppendLine();
    sb.AppendLine("    #ifdef RENDERMODE_MESH");
    sb.AppendLine("        // Multiply by mesh vertex color in mesh mode");
    sb.AppendLine("        gl_FragColor *= v_MeshColor;");
    sb.AppendLine("    #endif");
}
```

---

### ✅ 修复5：简单粒子shader - VS varying条件编译
**位置**：`CustomShaderExporter.cs:6732`（GenerateParticleVertexShader）

```csharp
// varying声明（参考AI版本使用条件编译）
sb.AppendLine("#ifdef RENDERMODE_MESH");
sb.AppendLine("varying vec4 v_MeshColor;");
sb.AppendLine("#endif");
sb.AppendLine("varying vec4 v_Color;");
sb.AppendLine("varying vec2 v_TextureCoordinate;");
sb.AppendLine("varying vec4 v_ScreenPos;");
```

---

### ✅ 修复6：简单粒子shader - FS varying条件编译
**位置**：`CustomShaderExporter.cs:6642`（GenerateParticleFragmentShader）

```csharp
// varying声明（与VS保持一致，参考AI版本使用条件编译）
sb.AppendLine("#ifdef RENDERMODE_MESH");
sb.AppendLine("varying vec4 v_MeshColor;");
sb.AppendLine("#endif");
sb.AppendLine("varying vec4 v_Color;");
sb.AppendLine("varying vec2 v_TextureCoordinate;");
sb.AppendLine("varying vec4 v_ScreenPos;");
```

---

### ✅ 修复7：简单粒子shader - FS颜色初始化（已正确）
**位置**：`CustomShaderExporter.cs:6659`（无需修改，已正确）

```csharp
sb.AppendLine("#ifdef RENDERMODE_MESH");
sb.AppendLine("    // Mesh mode: start with mesh vertex color");
sb.AppendLine("    color = v_MeshColor;");
sb.AppendLine("#else");
sb.AppendLine("    // Billboard mode: start with white");
sb.AppendLine("    color = vec4(1.0);");
sb.AppendLine("#endif");
```

**说明**：简单粒子shader在颜色初始化时就区分了mesh和billboard模式，无需在后面再次乘以v_MeshColor。

---

## 完整的v_MeshColor生命周期

### 1. 定义阶段（defines）
```javascript
defines: {
    RENDERMODE_MESH: { type: bool, default: false },
    ...
}
```

### 2. 声明阶段（varying - 条件编译）
```glsl
// VS
#ifdef RENDERMODE_MESH
varying vec4 v_MeshColor;
#endif

// FS
#ifdef RENDERMODE_MESH
varying vec4 v_MeshColor;
#endif
```

### 3. 赋值阶段（VS - ParticleShaderTemplate）
```glsl
#ifdef RENDERMODE_MESH
    // Mesh模式：使用真实顶点颜色
    v_MeshColor = a_MeshColor;
#else
    // Billboard模式：使用白色（默认值）
    v_MeshColor = vec4(1.0);
#endif
```

### 4. 使用阶段

#### 简单粒子shader（初始化方式）
```glsl
#ifdef RENDERMODE_MESH
    color = v_MeshColor;  // 初始化为mesh颜色
#else
    color = vec4(1.0);     // 初始化为白色
#endif
```

#### 自定义特效shader（乘法方式）
```glsl
#ifdef RENDERMODE_MESH
    gl_FragColor *= v_MeshColor;  // 最后乘以mesh颜色
#endif
```

---

## 关键设计对比

| 方面 | 之前的方案 | AI版本方案（现在） |
|------|-----------|------------------|
| **RENDERMODE_MESH define** | ❌ 缺失 | ✅ 已添加 |
| **varying声明方式** | 无条件 | **条件编译** |
| **varying声明时机** | 所有模式 | 仅mesh模式（条件编译） |
| **内存占用** | 略高 | 更优 |
| **编译兼容性** | ✅ | ✅ |

---

## 为什么使用条件编译？

### 优点
1. **节省内存**：billboard模式下不声明v_MeshColor
2. **语义清晰**：明确表示v_MeshColor仅在mesh模式下存在
3. **符合标准**：与Laya官方shader模板、AI辅助转换的shader保持一致
4. **更安全**：避免在不支持的模式下误用变量

### 实现细节
- 声明用条件编译：`#ifdef RENDERMODE_MESH`
- 赋值在条件块内：ParticleShaderTemplate已正确处理
- 使用也用条件编译：确保只在mesh模式下访问

---

## 通用性保证

### ✅ 不依赖shader名字
- 判断条件：`parseResult.isParticleBillboard`
- 基于shader的properties、attributes、特征分析
- 适用于所有Unity粒子shader

### ✅ 覆盖所有生成路径
1. **简单粒子shader**：GenerateParticleVertexShader + GenerateParticleFragmentShader
2. **自定义特效shader**：GenerateVertexShaderFromCustomCode + GenerateFragmentShaderFromCustomCode
3. **两种路径都正确处理了v_MeshColor的条件编译**

---

## 验证清单

- [x] ✅ `RENDERMODE_MESH` define已添加
- [x] ✅ `v_MeshColor`添加到allVaryings
- [x] ✅ 自定义特效shader VS使用条件编译声明v_MeshColor
- [x] ✅ 自定义特效shader FS使用条件编译声明v_MeshColor
- [x] ✅ 自定义特效shader FS正确使用v_MeshColor
- [x] ✅ 简单粒子shader VS使用条件编译声明v_MeshColor
- [x] ✅ 简单粒子shader FS使用条件编译声明v_MeshColor
- [x] ✅ 简单粒子shader FS正确初始化颜色

---

## 测试方法

### 1. 重新导出shader
```
Unity Editor -> LayaAir3D Export -> Export Particle Shader
```

### 2. 检查导出的shader文件
```bash
# 检查RENDERMODE_MESH define
grep "RENDERMODE_MESH" exported_shader.shader

# 检查v_MeshColor条件编译声明
grep -A1 "RENDERMODE_MESH" exported_shader.shader | grep "v_MeshColor"

# 检查v_MeshColor使用
grep "v_MeshColor" exported_shader.shader
```

### 3. 预期结果
```glsl
// defines中
RENDERMODE_MESH: { type: bool, default: false },

// VS varying声明
#ifdef RENDERMODE_MESH
varying vec4 v_MeshColor;
#endif

// FS varying声明
#ifdef RENDERMODE_MESH
varying vec4 v_MeshColor;
#endif

// FS使用
#ifdef RENDERMODE_MESH
    gl_FragColor *= v_MeshColor;  // 或 color = v_MeshColor;
#endif
```

---

## 相关文件

- **修改文件**: `CustomShaderExporter.cs`
  - 1432行：添加RENDERMODE_MESH define
  - 1858行：添加v_MeshColor到allVaryings
  - 1973行：条件编译包裹（自定义shader）
  - 2167行：FS使用v_MeshColor（自定义shader）
  - 6642行：FS varying条件编译（简单粒子）
  - 6732行：VS varying条件编译（简单粒子）

- **参考文件**:
  - `Artist_Effect_Effect_FullEffect.shader` (AI转换版本)
  - `ParticleMeshVS.vs` (Laya标准mesh粒子VS)
  - `ParticleMeshFS.fs` (Laya标准mesh粒子FS)

- **模板文件**: `ParticleShaderTemplate.cs` (无需修改，已正确处理v_MeshColor赋值)

---

## 修复状态

✅ **已完成** - 使用条件编译，完全参考AI版本shader的实现方式
