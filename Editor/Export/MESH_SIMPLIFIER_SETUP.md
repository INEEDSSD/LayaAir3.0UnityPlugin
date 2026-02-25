# Mesh简化方案使用指南

## 问题说明

自动简化算法（三角形抽取）对复杂mesh（圆柱体、球体等）效果不理想，容易破坏拓扑结构。

---

## 推荐方案对比

| 方案 | 质量 | 难度 | 适用场景 |
|------|------|------|---------|
| ✅ **方案1：手动准备LOD Mesh** | ⭐⭐⭐⭐⭐ | 简单 | 所有场景（最推荐） |
| ⭐ **方案2：UnityMeshSimplifier库** | ⭐⭐⭐⭐ | 中等 | 需要批量自动简化 |
| 🛠️ **方案3：Unity编辑器工具** | ⭐⭐⭐ | 简单 | 少量mesh手动处理 |
| ❌ **内置三角形抽取** | ⭐⭐ | 自动 | 简单mesh（不推荐） |

---

## 方案1：手动准备LOD Mesh（强烈推荐）⭐⭐⭐⭐⭐

### 为什么最推荐？
- **质量最高**：在3D软件中手动减面，完全可控
- **最简单**：不需要写代码或安装插件
- **无风险**：不依赖自动算法，结果可预测

### 操作步骤

#### 步骤1：在Blender中减面

1. **导入原始Mesh**
   ```
   File → Import → FBX
   ```

2. **添加Decimate修改器**
   - 选中mesh
   - 右侧面板：Modifiers → Add Modifier → Decimate
   - Collapse模式：设置Ratio（保留比例）
     - 原始66顶点 → 目标32顶点：Ratio = 0.48
   - 或Planar模式：设置Angle Limit（角度限制）

3. **预览并调整**
   - 实时查看简化效果
   - 调整参数直到满意
   - 确保mesh形状基本保持

4. **应用修改器并导出**
   ```
   Apply修改器
   File → Export → FBX
   文件名：guangzhu02_Low.fbx
   ```

#### 步骤2：在Unity中配置

1. **导入简化后的Mesh**
   - 将 `guangzhu02_Low.fbx` 拖入Unity

2. **配置粒子系统**
   - 选择粒子系统GameObject
   - Inspector → Particle System → Renderer模块
   - Mesh字段点击右侧小圆点

3. **添加多个Mesh**（Unity 2018.1+）
   ```
   Mesh:
     Size: 2
     Element 0: guangzhu02_High (66顶点) ← 编辑器预览用
     Element 1: guangzhu02_Low  (32顶点) ← 导出LayaAir用
   ```

4. **导出验证**
   - 导出场景
   - 查看日志：
   ```
   LayaAir3D: 检测到手动LOD Mesh (meshes[1]): guangzhu02_Low
     顶点数: 32, 总顶点: 32000
   LayaAir3D: ✓ 使用手动LOD Mesh
   ```

---

## 方案2：UnityMeshSimplifier库（高质量自动简化）⭐⭐⭐⭐

### 简介
- 开源库：https://github.com/Whinarn/UnityMeshSimplifier
- 算法：二次误差度量（QEM）
- 许可证：MIT（可商用）
- 效果：接近专业3D软件的减面质量

### 安装步骤

#### 方法1：通过Package Manager（推荐）

1. Unity菜单：`Window` → `Package Manager`
2. 点击左上角 `+` → `Add package from git URL`
3. 输入：
   ```
   https://github.com/Whinarn/UnityMeshSimplifier.git
   ```
4. 点击 `Add`

#### 方法2：手动下载

1. 访问：https://github.com/Whinarn/UnityMeshSimplifier/releases
2. 下载最新Release的 `.unitypackage`
3. 在Unity中：`Assets` → `Import Package` → `Custom Package`
4. 选择下载的文件并导入

### 启用集成

1. **添加脚本宏定义**
   - Unity菜单：`Edit` → `Project Settings` → `Player`
   - Other Settings → Scripting Define Symbols
   - 添加：`UNITY_MESH_SIMPLIFIER`
   - 点击 `Apply`

2. **重新编译**
   - Unity会自动重新编译脚本
   - 查看Console确认没有错误

### 使用效果

安装后，导出时会自动使用高质量算法：

```
MeshSimplifier: 开始简化mesh 'guangzhu02'
  原始顶点数: 66
  理论最小目标: 28
  简化质量: 0.70
  实际目标顶点数: 40
  使用UnityMeshSimplifier库（高质量QEM算法）
  简化后顶点数: 38
```

**对比：**
- 内置算法：66 → 22顶点（减少66%，形状破坏严重）
- UnityMeshSimplifier：66 → 38顶点（减少42%，形状保持良好）

---

## 方案3：Unity编辑器工具（手动简化）🛠️

### 使用编辑器工具

1. **打开工具**
   - Unity菜单：`LayaAir3D` → `Mesh简化工具`

2. **简化Mesh**
   ```
   1. 拖入源Mesh到'源Mesh'字段
   2. 设置目标顶点数（例如：32）
   3. 调整简化质量（推荐0.7）
   4. 点击'生成预览'
   5. 在Scene视图中检查效果
   6. 满意后点击'保存为Asset'
   ```

3. **配置粒子系统**
   - 按照方案1的步骤2配置
   - 使用保存的简化Mesh

---

## 最佳实践建议

### 针对不同类型的Mesh

**圆柱体（guangzhu02）：**
```
原始：66顶点
推荐：
  - Blender手动减面：32顶点 ⭐⭐⭐⭐⭐
  - UnityMeshSimplifier：36-40顶点 ⭐⭐⭐⭐
  - 内置算法：避免使用 ❌
```

**球体：**
```
原始：80-100顶点
推荐：
  - Blender手动减面：32-48顶点
  - UnityMeshSimplifier：40-50顶点
```

**方块/简单形状：**
```
原始：24顶点
推荐：
  - 不需要简化，直接使用
```

### 配置建议

**如果不想手动减面：**
```xml
<!-- Configuration.xml -->
<EnableCustomShaderExport>True</EnableCustomShaderExport>
<AutoSimplifyParticleMesh>True</AutoSimplifyParticleMesh>
<ParticleMeshMaxVertices>65535</ParticleMeshMaxVertices>
<ParticleMeshSimplifyQuality>0.9</ParticleMeshSimplifyQuality>  ← 提高到0.9
<ShowParticleMeshWarning>True</ShowParticleMeshWarning>
```

安装UnityMeshSimplifier后，设置quality=0.9可以获得最佳效果。

**如果使用手动LOD：**
```xml
<AutoSimplifyParticleMesh>False</AutoSimplifyParticleMesh>  ← 关闭自动简化
<ShowParticleMeshWarning>True</ShowParticleMeshWarning>     ← 保持警告
```

只在Blender中准备好低面数mesh，插件会自动检测并使用。

---

## 故障排除

### Q: UnityMeshSimplifier安装后报错？
A: 检查：
1. 是否添加了宏定义 `UNITY_MESH_SIMPLIFIER`
2. Unity版本是否 ≥ 2018.1
3. 尝试重启Unity

### Q: 自动简化还是效果不好？
A: **强烈建议使用方案1手动准备**：
- Blender减面质量最高
- 完全可控，不依赖算法
- 适合所有类型的mesh

### Q: 没有Blender怎么办？
A: 可以使用：
- Maya：Mesh → Reduce
- 3DMax：ProOptimizer修改器
- MeshLab（免费）：Filters → Remeshing, Simplification
- 或使用方案3的Unity编辑器工具

---

## 总结

| 你的需求 | 推荐方案 |
|---------|---------|
| 最佳质量，少量mesh | **方案1：Blender手动减面** ⭐⭐⭐⭐⭐ |
| 批量处理，自动化 | **方案2：安装UnityMeshSimplifier** ⭐⭐⭐⭐ |
| 不想安装插件 | **方案1 或 方案3** |
| 临时测试 | **方案3：编辑器工具** |

**最终建议：使用方案1（Blender手动减面）+ 配置meshes[1]**

这是最简单、最可靠、质量最高的方案。

---

**相关文件：**
- UnityMeshSimplifierIntegration.cs - UnityMeshSimplifier集成
- MeshSimplifierTool.cs - Unity编辑器工具
- MeshSimplifier.cs - 内置简化算法
- PARTICLE_MESH_OPTIMIZATION.md - 粒子优化总文档
