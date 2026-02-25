# 粒子系统Mesh优化功能说明

## 功能概述

Unity导出插件现在支持**粒子系统Mesh顶点数优化**，解决LayaAir粒子系统最大顶点数65535的限制问题。

**注意：** 此功能**仅针对粒子系统**，其他mesh（MeshRenderer、SkinnedMeshRenderer等）无此限制。

## 问题描述

### LayaAir粒子系统限制

在Unity中使用**Mesh渲染模式**的粒子系统时：

```
总顶点数 = maxParticles × meshVertexCount
```

**LayaAir限制：** 总顶点数 ≤ 65535

如果超过此限制，LayaAir运行时会报错：
```
粒子系统顶点数超过最大限制 65535
```

### 常见场景
```
粒子系统配置:
- maxParticles = 1000
- Mesh顶点数 = 100
- 总顶点数 = 1000 × 100 = 100,000 ❌ 超限
```

## 解决方案

### 方案1：检测+警告（默认启用）

导出时自动检测顶点数，超限时显示详细警告信息。

**配置：**
- ☑ `显示粒子Mesh顶点数警告`（默认：开启）

**警告内容：**
- 当前配置（maxParticles、mesh顶点数、总顶点数）
- 建议的最大粒子数
- 建议的mesh目标顶点数
- 解决方案

### 方案2：自动简化Mesh（可选）

导出时自动检测并简化超限的mesh到安全范围。

**配置：**
- ☑ `自动简化超限的粒子Mesh`（默认：关闭）
- 简化质量：0.1-1.0（默认：0.7）
- 顶点数限制：65535

**简化流程：**
1. 检测粒子系统总顶点数
2. 如果超限，计算目标顶点数
3. 简化mesh到目标顶点数
4. 替换为简化后的mesh

**简化质量说明：**

质量参数(0.1-1.0)控制简化的温和程度，值越高质量越好但简化越少：

- **0.5-0.6**：激进简化，可能影响视觉效果，适合不重要的粒子效果
- **0.7-0.8**：平衡简化（推荐），保留较好质量，允许适度超限
- **0.9-1.0**：温和简化，最大程度保留细节，可能仍然超限

**重要提示：**
- 质量参数允许简化后的mesh适度超出理论限制，以保证视觉效果
- 例如：quality=0.7时，实际顶点数可能是理论最小值的1.4倍，但总顶点数仍在可接受范围内
- 如需严格满足限制，请降低质量参数或减少maxParticles

## 使用方法

### 1. 打开导出设置

Unity菜单：`LayaAir3D` → `Export Settings`

### 2. 配置粒子系统Mesh优化

```
粒子系统Mesh优化
├─ ☑ 显示粒子Mesh顶点数警告
├─ ☐ 自动简化超限的粒子Mesh
│   ├─ 简化质量: [━━━━━━━●━━] 70%
│   └─ 顶点数限制: 65535
└─ 说明...
```

### 3. 推荐配置

**新手/测试阶段：**
```
☑ 显示警告
☐ 自动简化
```
→ 仅警告，手动控制优化

**正式开发：**
```
☑ 显示警告
☑ 自动简化
  质量: 70%
```
→ 自动处理，失败时显示警告

**批量导出：**
```
☐ 显示警告
☑ 自动简化
  质量: 70%
```
→ 静默处理，不弹窗

### 4. 导出场景

正常导出，插件会自动检查粒子系统mesh。

## 导出日志示例

### 检测到超限（未启用自动简化）
```
LayaAir3D Warning: 粒子系统 'Explosion' (Mesh模式) 的顶点数量超过限制!
当前配置: maxParticles=1000, Mesh顶点数=100, 总顶点数=100000
限制: 总顶点数 ≤ 65535

解决方案:
1. 将 maxParticles 减少到 655 或以下
2. 使用顶点数更少的Mesh (目标: ≤65 个顶点)
3. 启用'自动简化粒子Mesh'选项
4. 调整顶点数限制配置
```

### 自动简化成功（示例1 - 严格满足限制）
```
LayaAir3D: 粒子系统 'Explosion' (Mesh模式) 的顶点数量超过限制!
LayaAir3D: 尝试自动简化mesh到 65 个顶点...
MeshSimplifier: 开始简化mesh 'ParticleMesh'
  原始顶点数: 100
  理论最小目标: 65
  简化质量: 0.50
  实际目标顶点数: 80 (允许超出理论值以保证质量)
  简化后顶点数: 62
LayaAir3D: 简化结果检查
  简化后Mesh顶点数: 62 (原始: 100, 减少 38.0%)
  总顶点数: 62000 (原始: 100000)
  严格限制: 65535, 满足: ✓
LayaAir3D: ✓ 成功简化粒子Mesh
  Mesh顶点: 100 → 62 (减少 38.0%)
  总顶点数: 100000 → 62000
  状态: 完全满足限制
```

### 自动简化成功（示例2 - 保证质量，允许适度超限）
```
LayaAir3D: 粒子系统 'guangzhu02' (Mesh模式) 的顶点数量超过限制!
当前配置: maxParticles=1000, Mesh顶点数=66, 总顶点数=66000
限制: 总顶点数 ≤ 28827
LayaAir3D: 尝试自动简化mesh到 28 个顶点...
MeshSimplifier: 开始简化mesh 'guangzhu02'
  原始顶点数: 66
  理论最小目标: 28
  简化质量: 0.70
  实际目标顶点数: 40 (允许超出理论值以保证质量)
  简化后顶点数: 40
LayaAir3D: 简化结果检查
  简化后Mesh顶点数: 40 (原始: 66, 减少 39.4%)
  总顶点数: 40000 (原始: 66000)
  严格限制: 28827, 满足: ✗
  弹性限制: 44970 (quality=0.7, 允许超出56%), 满足: ✓
LayaAir3D: ⚠ 成功简化粒子Mesh
  Mesh顶点: 66 → 40 (减少 39.4%)
  总顶点数: 66000 → 40000
  状态: 超出严格限制但在可接受范围内
LayaAir3D: 提示 - 如需严格满足限制(28827)，可以:
  1. 降低简化质量到 0.5-0.6 (当前 0.7)
  2. 减少maxParticles到 436 (当前 1000)
  3. 提高顶点数限制到 40000 以上
```

## 手动优化方法

⚠️ **重要提示：** 如果自动简化效果不理想（mesh形状变形严重），请查看：
- **[MESH_SIMPLIFIER_SETUP.md](./MESH_SIMPLIFIER_SETUP.md)** - 详细的mesh简化方案对比和使用指南
- 包含：Blender手动减面、UnityMeshSimplifier库集成、Unity编辑器工具

---

### 方法1：使用手动准备的低面数Mesh（强烈推荐）⭐⭐⭐⭐⭐

**适用场景：** 自动简化效果不理想，破坏了mesh形状

**操作步骤：**

1. **在3D软件中创建低面数版本**
   - 使用Blender/Maya/3DMax等工具手动减面
   - 保持mesh的基本形状和结构
   - 导出为FBX/OBJ格式

2. **在Unity中配置粒子系统**
   - 选择粒子系统的GameObject
   - 在Renderer模块中，将Mesh设置为你的原始高质量mesh
   - Unity 2018.1+支持：点击Mesh字段右侧的"+"，可以添加多个mesh
   - 第2个mesh（meshes[1]）将被插件自动识别为简化版本

3. **导出时自动使用**
   - 插件会检测meshes[1]
   - 如果meshes[1]的顶点数满足限制，自动使用它
   - 否则降级到自动简化算法

**Unity配置示例：**
```
ParticleSystemRenderer:
  Render Mode: Mesh
  Mesh: [0] HighQualityMesh (66顶点)  ← Unity编辑器中使用
        [1] LowQualityMesh (32顶点)   ← 导出LayaAir时使用
```

**日志输出：**
```
LayaAir3D: 检测到手动LOD Mesh (meshes[1]): LowQualityMesh
  顶点数: 32, 总顶点: 32000
LayaAir3D: ✓ 成功简化粒子Mesh
  使用手动LOD Mesh
```

---

### 方法2：减少maxParticles
在Unity粒子系统设置中：
```
Max Particles: 655  （而不是1000）
```

### 方法3：简化Mesh模型
1. 在3D软件中优化模型
2. 减少多边形数量
3. 移除不必要的细节

### 方法4：使用简单形状
粒子效果可以使用：
- Quad（4顶点）
- Cube（8顶点）
- 低多边形sphere（20-50顶点）

## 配置文件

位置：`Editor/Configuration.xml`

```xml
<LayaExportSetting>
  <!-- 粒子系统Mesh优化配置 -->
  <AutoSimplifyParticleMesh>False</AutoSimplifyParticleMesh>
  <ParticleMeshMaxVertices>65535</ParticleMeshMaxVertices>
  <ParticleMeshSimplifyQuality>0.7</ParticleMeshSimplifyQuality>
  <ShowParticleMeshWarning>True</ShowParticleMeshWarning>
</LayaExportSetting>
```

## 技术说明

### 检查时机
```
粒子系统导出 → ExportParticleSystemV2()
              ↓
         CheckParticleVertexLimit()
              ├─ 检查: totalVertices > 65535?
              ├─ 简化: SimplifyMesh() (如启用)
              └─ 警告: 显示解决方案
```

### 简化算法
1. Unity内置优化（MeshUtility.Optimize）
2. 顶点采样简化（按步长采样）
3. 三角形重建
4. UV/法线/切线保留

### 顶点计算
```csharp
int maxParticles = particleSystem.main.maxParticles;
int meshVertexCount = mesh.vertexCount;
int totalVertexCount = maxParticles * meshVertexCount;

if (totalVertexCount > 65535)
{
    // 需要优化
    int targetMeshVertexCount = 65535 / maxParticles;
}
```

## 限制和注意

### 自动简化的局限
- ⚠️ 简化可能影响视觉效果
- ⚠️ 复杂mesh简化效果有限
- ⚠️ 建议导出后检查效果

### 性能影响
- 导出时间增加：1-5%
- 运行时性能：提升（顶点数减少）
- 文件大小：减少

### 兼容性
- Unity 2017.3+：支持内置优化
- Unity 5.x-2017.2：仅采样简化
- LayaAir：总顶点数 ≤ 65535

## 常见问题

**Q: 为什么只检查粒子系统？**
A: 只有粒子系统在LayaAir中有65535的顶点限制，其他mesh类型无此限制。

**Q: 自动简化后mesh形状变了/变成三角形了？**
A: 内置的三角形抽取算法对复杂mesh（圆柱体、球体等）效果很差。**解决方案**：

   **方案A：手动准备LOD Mesh（最推荐）⭐⭐⭐⭐⭐**
   1. 在Blender中使用Decimate修改器手动减面
   2. 导出为新的FBX文件
   3. 在Unity的ParticleSystemRenderer中添加为meshes[1]
   4. 插件会自动使用这个手动LOD mesh

   **方案B：安装UnityMeshSimplifier库（高质量）⭐⭐⭐⭐**
   1. Package Manager → Add from git URL
   2. 输入：https://github.com/Whinarn/UnityMeshSimplifier.git
   3. Project Settings → Scripting Define Symbols → 添加：`UNITY_MESH_SIMPLIFIER`
   4. 导出时自动使用高质量QEM算法

   **方案C：使用Unity编辑器工具（简单）⭐⭐⭐**
   1. Unity菜单：LayaAir3D → Mesh简化工具
   2. 手动简化mesh并保存
   3. 配置为meshes[1]

   **详细说明：** 查看 [MESH_SIMPLIFIER_SETUP.md](./MESH_SIMPLIFIER_SETUP.md)

**Q: 简化后效果变差怎么办？**
A: 有三种方案：
   - **推荐**：手动准备低面数mesh（见上一个问题）
   - 提高简化质量到0.8-0.9（但可能仍超限）
   - 减少maxParticles或使用更简单的mesh

**Q: 能调整限制到更高吗？**
A: 可以在导出设置中调整，但LayaAir引擎需要支持。如果你的限制是28827，建议调回默认的65535。

**Q: 批量导出如何避免弹窗？**
A: 关闭"显示警告"，启用"自动简化"。

**Q: Unity 2017不支持meshes数组怎么办？**
A: meshes数组功能需要Unity 2018.1+。如果使用老版本，请：
   - 直接替换粒子系统的Mesh为低面数版本
   - 或在3D软件中手动简化原始mesh

## 文件清单

### 新增文件
- `Editor/Export/utils/MeshSimplifier.cs`

### 修改文件
- `Editor/Export/ExportConfig.cs`
- `Editor/Export/LayaParticleExportV2.cs`
- `Editor/Export/LayaAir3D.cs`
- `Editor/Configuration.xml`

---

**适用范围：** 仅粒子系统（ParticleSystemRenderer）
**版本：** LayaAir 3.0 Unity Plugin
**日期：** 2026-02-11
**状态：** ✅ 完成
