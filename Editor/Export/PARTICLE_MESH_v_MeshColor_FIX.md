# 修复Unity粒子Mesh模式Shader导出v_MeshColor未声明错误

⚠️ **状态：已屏蔽（2026-02-12）**
经用户测试发现不是插件导出的问题，相关修改已全部注释屏蔽。

## 问题描述

导出Unity粒子系统mesh模式的shader时出现编译错误：
```
ERROR: 0:399: 'v_MeshColor' : undeclared identifier
ERROR: 0:399: 'assign' : l-value required (can't modify a const)
ERROR: 0:399: '=' : dimension mismatch
ERROR: 0:415: 'v_MeshColor' : undeclared identifier
```

**根本原因**：
1. ParticleShaderTemplate在mesh模式下会使用`v_MeshColor`传递顶点颜色
2. 但导出插件在varying提取阶段遗漏了`v_MeshColor`的声明
3. 导致VS和FS中都没有声明这个varying变量

## 修复方案

### 修复1：在varying提取阶段添加v_MeshColor声明
**位置**：`CustomShaderExporter.cs:1849-1860`

在粒子shader的varying处理中添加：
```csharp
// ⭐ 关键修复：粒子mesh模式需要v_MeshColor传递顶点颜色
if (!allVaryings.ContainsKey("v_MeshColor"))
{
    allVaryings["v_MeshColor"] = "vec4";
    Debug.Log("LayaAir3D: Added varying vec4 v_MeshColor for particle mesh mode");
}
```

**作用**：确保v_MeshColor在VS和FS中都被声明为`varying vec4 v_MeshColor;`

### 修复2：在简单粒子FS中使用v_MeshColor
**位置**：`CustomShaderExporter.cs:6648-6655`

在GenerateParticleFragmentShader的颜色处理中添加：
```csharp
sb.AppendLine("#ifdef RENDERMODE_MESH");
sb.AppendLine("    // Mesh mode: multiply by mesh vertex color");
sb.AppendLine("    color *= v_MeshColor;");
sb.AppendLine("#endif");
```

**作用**：简单粒子shader在mesh模式下正确使用mesh顶点颜色

### 修复3：在自定义特效FS中使用v_MeshColor
**位置**：`CustomShaderExporter.cs:2150-2160`

在FS main函数中，convertedFragCode之后添加：
```csharp
// ⭐ 粒子系统mesh模式：在最终输出前乘以mesh顶点颜色
if (parseResult.isParticleBillboard)
{
    sb.AppendLine();
    sb.AppendLine("    #ifdef RENDERMODE_MESH");
    sb.AppendLine("        // Multiply by mesh vertex color in mesh mode");
    sb.AppendLine("        gl_FragColor *= v_MeshColor;");
    sb.AppendLine("    #endif");
}
```

**作用**：复杂自定义特效shader（如Artist_Effect_Effect_FullEffect）在mesh模式下也能正确使用mesh顶点颜色

## 技术细节

### v_MeshColor的完整流程

1. **声明**（修复1）：
   - VS: `varying vec4 v_MeshColor;`
   - FS: `varying vec4 v_MeshColor;`

2. **赋值**（ParticleShaderTemplate.cs已实现）：
   - Mesh模式：`v_MeshColor = a_MeshColor;`（第287行）
   - Billboard模式：`v_MeshColor = vec4(1.0);`（第350行）
   - Dead particles：`v_MeshColor = vec4(1.0);`（第358行）

3. **使用**（修复2+修复3）：
   - 简单粒子：在颜色计算中乘以v_MeshColor
   - 复杂特效：在gl_FragColor最终输出前乘以v_MeshColor

### 条件编译说明

使用`#ifdef RENDERMODE_MESH`包裹v_MeshColor的使用（不包裹声明），因为：
- **声明**：无条件声明，保证VS和FS中变量存在
- **赋值**：无论哪种模式都会赋值（mesh模式用顶点颜色，billboard模式用白色）
- **使用**：只在mesh模式下才需要乘以顶点颜色

## 验证方法

1. 重新导出包含mesh模式粒子的shader
2. 检查导出的shader文件：
   - VS部分应有：`varying vec4 v_MeshColor;`
   - FS部分应有：`varying vec4 v_MeshColor;`
   - FS部分应有：`#ifdef RENDERMODE_MESH ... gl_FragColor *= v_MeshColor; ... #endif`
3. 导入Laya引擎测试，确保shader编译无错误且渲染正确

## 相关文件

- `CustomShaderExporter.cs`: 主要修复文件
- `ParticleShaderTemplate.cs`: 粒子shader模板（已正确实现v_MeshColor赋值）
- `ParticleMeshVS.vs`: Laya标准粒子mesh模式VS参考
- `ParticleMeshFS.fs`: Laya标准粒子mesh模式FS参考

## 修复日期

2026-02-12
