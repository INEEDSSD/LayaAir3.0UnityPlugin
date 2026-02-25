# RENDERMODE_MESH 修复诊断指南

## ⚠️ 诊断步骤

### 步骤1: 检查Unity编译状态

**在Unity Console中执行以下检查**：

1. 打开Unity Editor
2. 查看Console，确认没有编译错误
3. 检查插件DLL的修改时间：
   ```
   Assets/LayaAir3D/Editor/LayaAir3Export.dll
   ```
   应该是最新的时间戳

### 步骤2: 强制重新编译插件

```bash
# 方法1：删除DLL让Unity重新编译
1. 关闭Unity
2. 删除所有.dll和.dll.meta文件
3. 重新打开Unity，等待编译完成

# 方法2：右键Reimport
1. 在Unity Project窗口找到LayaAir3Export.dll
2. 右键 → Reimport
3. 等待编译完成
```

### 步骤3: 检查粒子系统设置

**在Unity Inspector中**：

1. 选择你的ParticleSystem GameObject
2. 找到 **Particle System Renderer** 组件
3. 检查 **Render Mode** 设置：
   ```
   ✅ 应该设置为: Mesh
   ❌ 如果是: Billboard / Stretched Billboard / 其他 → 不需要RENDERMODE_MESH
   ```

4. 如果Render Mode是Mesh，检查是否指定了Mesh：
   ```
   Mesh: 应该有一个mesh资源（如Quad, Cube等）
   ```

### 步骤4: 检查材质类型

**你的材质属于哪种类型？**

#### 类型A: 内置粒子材质
```
材质路径: Resources/unity_builtin_extra 或类似内置路径
Shader: Particles/... 或 Artist_Effect/...
```

**检查代码路径**：MaterialFile.cs → WriteBuiltinParticleMaterial()

**预期日志**：
```
LayaAir3D: Added RENDERMODE_MESH define for built-in particle material in mesh mode
```

#### 类型B: 自定义Shader材质（推荐）
```
材质路径: Assets/Materials/xxx.mat
Shader: Artist_Effect_Effect_FullEffect 或其他自定义shader
```

**检查设置**：
```
LayaAir → Export Config → Enable Custom Shader Export: ✅ 必须勾选
```

**预期日志**：
```
LayaAir3D: Particle system 'ParticleSystem' uses MESH render mode
LayaAir3D: Added RENDERMODE_MESH define for particle mesh rendering mode
```

### 步骤5: 清理缓存 ⚠️ 必须执行

```bash
# Windows - 在LayaAir项目目录
cd "C:\Users\DELL\Downloads\3.3_3d线段颜色异常_\LayaProject"

# 删除所有缓存
rmdir /s /q .laya\cache
rmdir /s /q assets\test1\Shaders
rmdir /s /q assets\test1\Material
rmdir /s /q assets\test2\Shaders
rmdir /s /q assets\test2\Material

# 删除Unity插件template文件夹中的旧shader
cd "D:\LayaAirCode\UnityPlugins_mutiVersion\3.xUnityPlugin\LayaAir3.0UnityPlugin"
del /q template\*.shader
del /q template\*.lmat
```

```bash
# Mac/Linux
rm -rf .laya/cache/
rm -rf assets/*/Shaders/
rm -rf assets/*/Material/
rm -rf template/*.shader
rm -rf template/*.lmat
```

### 步骤6: 完全重启Unity

```
1. 保存所有修改
2. 完全关闭Unity Editor（不是切换场景）
3. 等待10秒
4. 重新打开Unity项目
5. 等待脚本编译完成（看Console）
```

### 步骤7: 导出并检查日志

```
1. 在Unity中选择场景
2. LayaAir → Export Scene
3. 仔细查看Unity Console的完整日志
```

**必须看到的日志（如果是mesh模式）**：
```
✅ LayaAir3D: Particle system 'XXX' uses MESH render mode
```

**如果没有看到这个日志**：
- 说明ParticleSystemRenderer的render mode不是Mesh
- 或者renderer没有正确传递给MaterialFile
- 或者Unity没有重新编译插件

### 步骤8: 检查导出的材质文件

**打开导出的.lmat文件**：
```bash
code "C:\Users\DELL\Downloads\3.3_3d线段颜色异常_\LayaProject\assets\test1\Material\fx_mat_xxx.lmat"
```

**搜索defines数组**：
```json
"defines": [
    "LAYERTYPE_THREE",
    "USEDISTORT0",
    "RENDERMODE_MESH"  ← 这个必须存在！
]
```

**如果RENDERMODE_MESH不存在**：
- 说明MaterialFile.IsParticleMeshMode()返回false
- 或者材质导出代码没有执行到检测逻辑

---

## 🔍 具体错误诊断

### 错误1: Unity Console没有任何关于RENDERMODE_MESH的日志

**可能原因**：
1. Unity没有重新编译插件DLL
2. 粒子系统的Render Mode不是Mesh
3. MaterialFile创建时没有传递renderer参数

**解决方案**：
```csharp
// 添加临时调试日志
// 在MaterialFile.cs构造函数中添加：
Debug.Log($"MaterialFile: material={material.name}, renderer={(renderer != null ? renderer.GetType().Name : "null")}");

// 在MaterialFile.cs第31行之后添加：
if (particleRenderer != null)
{
    Debug.Log($"MaterialFile: ParticleRenderer detected, renderMode={renderMode}");
}
```

### 错误2: Unity Console显示"uses MESH render mode"但导出的.lmat没有RENDERMODE_MESH

**可能原因**：
1. 材质导出走的是WriteBuiltinParticleMaterial路径，但我的最新修复没有生效
2. 或者材质导出走的是MetarialUitls.WriteMetarial的其他分支

**检查方法**：
```csharp
// 在MaterialFile.cs第47行添加调试日志：
Debug.Log($"MaterialFile: Using WriteBuiltinParticleMaterial, isParticleMeshMode={m_isParticleMeshMode}");

// 在MaterialFile.cs第45行添加调试日志：
Debug.Log($"MaterialFile: Using MetarialUitls.WriteMetarial");
```

### 错误3: 导出后LayaAir IDE仍然报v_MeshColor错误

**检查shader文件**：
```bash
# 打开导出的shader
code "C:\Users\DELL\Downloads\3.3_3d线段颜色异常_\LayaProject\assets\test1\Shaders\Artist_Effect_Effect_FullEffect.shader"

# 搜索v_MeshColor
# 应该看到条件编译包裹：
#ifdef RENDERMODE_MESH
varying vec4 v_MeshColor;
#endif
```

**如果shader中v_MeshColor没有条件编译**：
- 说明shader导出代码没有正确处理
- 参考文档：v_MeshColor_CONDITIONAL_COMPILATION_FIX.md

---

## 🐛 快速测试脚本

**创建测试脚本帮助诊断**：

```csharp
// 在Unity中创建 Assets/Editor/DiagnosticRENDERMODE_MESH.cs
using UnityEngine;
using UnityEditor;

public class DiagnosticRENDERMODE_MESH
{
    [MenuItem("LayaAir/Diagnostic/Check ParticleSystem Render Mode")]
    static void CheckParticleSystemRenderMode()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            Debug.LogError("Please select a GameObject with ParticleSystem");
            return;
        }

        ParticleSystemRenderer psr = selected.GetComponent<ParticleSystemRenderer>();
        if (psr == null)
        {
            Debug.LogError("Selected GameObject does not have ParticleSystemRenderer");
            return;
        }

        Debug.Log($"=== ParticleSystem Diagnostic ===");
        Debug.Log($"GameObject: {selected.name}");
        Debug.Log($"Render Mode: {psr.renderMode}");
        Debug.Log($"Is Mesh Mode: {psr.renderMode == ParticleSystemRenderMode.Mesh}");

        if (psr.renderMode == ParticleSystemRenderMode.Mesh)
        {
            Debug.Log($"Mesh: {(psr.mesh != null ? psr.mesh.name : "null")}");
        }

        Material[] materials = psr.sharedMaterials;
        Debug.Log($"Materials Count: {materials.Length}");
        for (int i = 0; i < materials.Length; i++)
        {
            if (materials[i] != null)
            {
                Debug.Log($"  Material[{i}]: {materials[i].name}");
                Debug.Log($"  Shader: {materials[i].shader.name}");
                string matPath = AssetDatabase.GetAssetPath(materials[i]);
                Debug.Log($"  Path: {matPath}");
                Debug.Log($"  Is Built-in: {string.IsNullOrEmpty(matPath) || matPath.Contains("unity_builtin_extra") || matPath.Contains("Library/")}");
            }
        }
    }
}
```

**使用方法**：
1. 在Unity中选择你的ParticleSystem GameObject
2. 菜单：LayaAir → Diagnostic → Check ParticleSystem Render Mode
3. 查看Console输出的完整信息
4. 把输出贴给我，我可以进一步诊断

---

## 📋 完整检查清单

导出前确认：
- [ ] Unity没有编译错误
- [ ] 插件DLL是最新的
- [ ] ParticleSystem Render Mode = Mesh
- [ ] LayaAir Export Config → Enable Custom Shader Export = ✅
- [ ] 已清理.laya/cache和assets/*/Shaders
- [ ] 已删除template/*.shader
- [ ] 已重启Unity Editor

导出时检查：
- [ ] Unity Console显示"Particle system uses MESH render mode"
- [ ] Unity Console显示"Added RENDERMODE_MESH define"
- [ ] 没有其他错误或警告

导出后检查：
- [ ] .lmat文件的defines包含"RENDERMODE_MESH"
- [ ] .shader文件的v_MeshColor有#ifdef包裹
- [ ] LayaAir IDE没有v_MeshColor编译错误

---

## 💡 如果仍然无效

请提供以下信息：

1. **Unity Console完整日志**（从开始导出到结束的所有日志）
2. **导出的.lmat文件内容**（完整的JSON）
3. **导出的.shader文件**（至少包含varying声明部分）
4. **诊断脚本的输出**（上面的DiagnosticRENDERMODE_MESH脚本）
5. **Unity版本和LayaAir版本**
6. **Particle System Inspector截图**（显示Render Mode设置）

有了这些信息，我可以精确定位问题所在。

---

## 🔗 相关文档

- **v_MeshColor_CONDITIONAL_COMPILATION_FIX.md** - Shader层面的条件编译修复
- **ALL_FIXES_SUMMARY.md** - 所有修复的汇总
- **QUICK_FIX_GUIDE.md** - 快速修复指南

---

**最后更新**: 2024
**版本**: v1.1 - 添加内置粒子材质路径修复
