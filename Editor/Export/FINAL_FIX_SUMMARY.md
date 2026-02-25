# Unity粒子Shader导出最终修复总结

## 修复完成时间
2026-02-12

---

## 核心问题

导出Unity粒子mesh模式shader时出现编译错误：
```
ERROR: 0:399: 'v_MeshColor' : undeclared identifier
```

**根本原因**：插件导出的shader缺少`RENDERMODE_MESH`宏定义

---

## 修复方案（通用且不特殊匹配shader名字）

### ✅ 修复：添加RENDERMODE_MESH define

**文件**：`CustomShaderExporter.cs`
**位置**：第1432行
**修改**：在粒子shader的defines生成中添加RENDERMODE_MESH

```csharp
// 粒子shader使用TINTCOLOR/ADDTIVEFOG/RENDERMODE_MESH（参考Particle.shader模板）
if (parseResult.isParticleBillboard)
{
    // ⭐ 粒子mesh模式：添加RENDERMODE_MESH define（用于区分mesh和billboard模式）
    sb.AppendLine("        RENDERMODE_MESH: { type: bool, default: false },");
    addedDefines.Add("RENDERMODE_MESH");

    sb.AppendLine("        TINTCOLOR: { type: bool, default: true },");
    addedDefines.Add("TINTCOLOR");
    sb.AppendLine("        ADDTIVEFOG: { type: bool, default: true },");
    addedDefines.Add("ADDTIVEFOG");
}
```

**判断条件**：`parseResult.isParticleBillboard`（基于功能特征，不依赖shader名字）

---

## 完整的shader结构对比

### 1. defines定义

#### AI转换版本 ✅
```javascript
defines: {
    RENDERMODE_MESH: { type: bool, default: false },  // ← 存在
    TINTCOLOR: { type: bool, default: true },
    ADDTIVEFOG: { type: bool, default: true },
    ...
}
```

#### 插件导出版本（修复前）❌
```javascript
defines: {
    TINTCOLOR: { type: bool, default: true },  // ← 缺少RENDERMODE_MESH
    ADDTIVEFOG: { type: bool, default: true },
    ...
}
```

#### 插件导出版本（修复后）✅
```javascript
defines: {
    RENDERMODE_MESH: { type: bool, default: false },  // ← 已添加
    TINTCOLOR: { type: bool, default: true },
    ADDTIVEFOG: { type: bool, default: true },
    ...
}
```

---

### 2. varying v_MeshColor处理

#### AI版本：条件编译声明
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

#### 插件导出版本：无条件声明（保持不变）✅
```glsl
// VS
varying vec4 v_MeshColor;  // 无条件声明

// FS
varying vec4 v_MeshColor;  // 无条件声明
```

**设计理由**：
- ✅ 更安全：变量在所有模式下都存在
- ✅ 更简单：减少条件编译复杂度
- ✅ 正确赋值：mesh模式用真实值，billboard/dead用白色
- ✅ 条件使用：只在mesh模式下实际使用

---

### 3. v_MeshColor赋值（VS）

#### 两个版本都正确 ✅

```glsl
#ifdef RENDERMODE_MESH
    // Mesh模式
    v_MeshColor = a_MeshColor;
#else
    // Billboard模式
    v_MeshColor = vec4(1.0);  // 白色，不影响颜色
#endif

// Dead粒子
v_MeshColor = vec4(1.0);
```

---

### 4. v_MeshColor使用（FS）

#### 两个版本都正确 ✅

```glsl
#ifdef RENDERMODE_MESH
    // 只在mesh模式下乘以顶点颜色
    gl_FragColor *= v_MeshColor;
#endif
```

---

## 通用性保证

### ✅ 不依赖shader名字
- 判断条件：`parseResult.isParticleBillboard`
- 基于：shader的properties、attributes、特征分析
- 适用：所有Unity粒子shader（内置、自定义、特效shader）

### ✅ 自动化判断
```
Unity粒子shader → 插件分析 → isParticleBillboard=true → 自动添加RENDERMODE_MESH
```

---

## 技术设计对比

| 方面 | AI版本 | 插件导出版本 | 结论 |
|------|--------|------------|------|
| **varying声明** | 条件编译 | 无条件 | 两种都可行，导出版本更安全 |
| **RENDERMODE_MESH** | ✅ 有 | ❌→✅ 修复后有 | 修复完成 |
| **函数完整性** | ✅ 完整 | ✅ 完整 | 一致 |
| **条件编译逻辑** | ✅ 正确 | ✅ 正确 | 一致 |

---

## 预期效果

修复后的shader导出应该：
1. ✅ `RENDERMODE_MESH`宏定义正确添加到defines
2. ✅ 在Laya引擎中成功编译，无undeclared identifier错误
3. ✅ Mesh模式正确显示顶点颜色
4. ✅ Billboard模式正常工作
5. ✅ 适用于所有粒子shader（不限于特定名字）

---

## 测试验证

### 验证步骤
1. 重新导出包含mesh模式粒子的shader
2. 检查导出的.shader文件：
   - 搜索`RENDERMODE_MESH`，应该在defines中找到
   - 搜索`varying vec4 v_MeshColor`，应该有声明
   - 搜索`gl_FragColor *= v_MeshColor`，应该有条件使用
3. 导入Laya引擎，验证编译无错误
4. 运行时测试mesh模式和billboard模式

### 验证命令
```bash
# 检查RENDERMODE_MESH
grep "RENDERMODE_MESH" exported_shader.shader

# 检查v_MeshColor声明
grep "varying vec4 v_MeshColor" exported_shader.shader

# 检查v_MeshColor使用
grep "v_MeshColor" exported_shader.shader
```

---

## 相关文件

- **修改文件**: `CustomShaderExporter.cs` (第1432行)
- **参考文件**:
  - `Artist_Effect_Effect_FullEffect.shader` (AI转换版本)
  - `Artist_Effect_Effect_FullEffect_export.shader` (插件导出版本)
- **模板文件**: `ParticleShaderTemplate.cs` (无需修改)

---

## 后续建议

1. **测试覆盖**：
   - 测试不同类型的粒子shader（简单粒子、复杂特效）
   - 测试mesh模式和billboard模式切换
   - 测试不同的渲染效果（溶解、边缘发光等）

2. **代码审查**：
   - 确认所有粒子shader都正确生成RENDERMODE_MESH
   - 检查是否有其他粒子特性define缺失

3. **文档更新**：
   - 更新shader导出文档，说明RENDERMODE_MESH的作用
   - 添加粒子mesh模式的使用说明

---

## 修复状态

✅ **已完成** - 通用方案，基于功能特征，不依赖shader名字
