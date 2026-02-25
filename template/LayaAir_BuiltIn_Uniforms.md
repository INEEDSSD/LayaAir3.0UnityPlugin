# LayaAir 引擎内置 Uniform 变量文档

本文档列出了 LayaAir 引擎中所有内置的 Uniform 变量，这些变量可以在自定义着色器中直接使用，无需在材质中额外定义。

---

## 目录

1. [场景相关 (Scene3D)](#1-场景相关-scene3d)
2. [相机相关 (BaseCamera)](#2-相机相关-basecamera)
3. [精灵/物体相关 (Sprite3D)](#3-精灵物体相关-sprite3d)
4. [光照相关 (Lighting)](#4-光照相关-lighting)
5. [阴影相关 (Shadow)](#5-阴影相关-shadow)
6. [全局光照相关 (Global Illumination)](#6-全局光照相关-global-illumination)
7. [体积光照探针 (Volumetric GI)](#7-体积光照探针-volumetric-gi)
8. [2D 渲染相关](#8-2d-渲染相关)
9. [粒子系统相关](#9-粒子系统相关)
10. [拖尾系统相关](#10-拖尾系统相关)

---

## 1. 场景相关 (Scene3D)

这些变量定义在 `SceneCommon.glsl` 中，需要引入该文件使用。

| 变量名 | 类型 | 说明 |
|--------|------|------|
| `u_Time` | `float` | 场景运行时间（秒），可用于动画效果 |
| `u_FogParams` | `vec4` | 雾效参数：x=起始距离, y=范围, z=密度 |
| `u_FogColor` | `vec4` | 雾效颜色 |
| `u_GIRotate` | `float` | 全局光照旋转角度（弧度） |
| `u_DirationLightCount` | `int` | 方向光数量 |

**使用示例：**
```glsl
#include "SceneCommon.glsl";

void main() {
    // 使用时间创建动画效果
    float wave = sin(u_Time * 2.0);
    
    // 应用雾效
    float fogFactor = (distance - u_FogParams.x) / u_FogParams.y;
}
```

---

## 2. 相机相关 (BaseCamera)

这些变量定义在 `CameraCommon.glsl` 中。

| 变量名 | 类型 | 说明 |
|--------|------|------|
| `u_CameraPos` | `vec3` | 相机世界空间位置 |
| `u_View` | `mat4` | 视图矩阵 |
| `u_Projection` | `mat4` | 投影矩阵 |
| `u_ViewProjection` | `mat4` | 视图投影矩阵 (VP) |
| `u_CameraDirection` | `vec3` | 相机朝向方向（归一化） |
| `u_CameraUp` | `vec3` | 相机上方向（归一化） |
| `u_Viewport` | `vec4` | 视口参数：x, y, width, height |
| `u_ProjectionParams` | `vec4` | 投影参数：x=1或-1(翻转), y=近平面, z=远平面, w=1/远平面 |
| `u_OpaqueTextureParams` | `vec4` | 不透明纹理参数 |
| `u_ZBufferParams` | `vec4` | 深度缓冲参数 |
| `u_CameraDepthTexture` | `sampler2D` | 相机深度纹理 |
| `u_CameraDepthNormalsTexture` | `sampler2D` | 相机深度法线纹理 |
| `u_CameraOpaqueTexture` | `sampler2D` | 相机不透明纹理（用于折射等效果） |

**使用示例：**
```glsl
#include "CameraCommon.glsl";

void main() {
    // 计算视图方向
    vec3 viewDir = normalize(u_CameraPos - worldPosition);
    
    // 世界空间转裁剪空间
    vec4 clipPos = u_ViewProjection * vec4(worldPosition, 1.0);
    
    // 采样深度纹理
    float depth = texture2D(u_CameraDepthTexture, screenUV).r;
}
```

---

## 3. 精灵/物体相关 (Sprite3D)

这些变量定义在 `Sprite3DCommon.glsl` 中。

| 变量名 | 类型 | 说明 |
|--------|------|------|
| `u_WorldMat` | `mat4` | 物体的世界变换矩阵 |
| `u_WorldInvertFront` | `vec4` | x=正面翻转标志, yzw=节点自定义数据 |

**宏定义快捷访问：**
| 宏名称 | 说明 |
|--------|------|
| `WorldInvertFront` | 正面翻转标志 |
| `NodeCustomData0` | 节点自定义数据0 |
| `NodeCustomData1` | 节点自定义数据1 |
| `NodeCustomData2` | 节点自定义数据2 |

**使用示例：**
```glsl
#include "Sprite3DCommon.glsl";

void main() {
    // 将顶点从模型空间变换到世界空间
    vec4 worldPos = u_WorldMat * vec4(a_Position.xyz, 1.0);
    
    // 获取节点自定义数据
    float customValue = NodeCustomData0;
}
```

---

## 4. 光照相关 (Lighting)

这些变量定义在 `Lighting.glsl` 中。

### 4.1 传统单光源模式 (LEGACYSINGLELIGHTING)

**方向光：**
| 变量名 | 类型 | 说明 |
|--------|------|------|
| `u_DirLightColor` | `vec3` | 方向光颜色 |
| `u_DirLightDirection` | `vec3` | 方向光方向 |
| `u_DirLightMode` | `int` | 方向光模式 |

**点光源：**
| 变量名 | 类型 | 说明 |
|--------|------|------|
| `u_PointLightColor` | `vec3` | 点光源颜色 |
| `u_PointLightPos` | `vec3` | 点光源位置 |
| `u_PointLightRange` | `float` | 点光源范围 |
| `u_PointLightMode` | `int` | 点光源模式 |

**聚光灯：**
| 变量名 | 类型 | 说明 |
|--------|------|------|
| `u_SpotLightColor` | `vec3` | 聚光灯颜色 |
| `u_SpotLightPos` | `vec3` | 聚光灯位置 |
| `u_SpotLightDirection` | `vec3` | 聚光灯方向 |
| `u_SpotLightRange` | `float` | 聚光灯范围 |
| `u_SpotLightSpot` | `float` | 聚光灯角度 |
| `u_SpotLightMode` | `int` | 聚光灯模式 |

### 4.2 集群光照模式

| 变量名 | 类型 | 说明 |
|--------|------|------|
| `u_LightBuffer` | `sampler2D` | 光源数据缓冲纹理 |
| `u_LightClusterBuffer` | `sampler2D` | 光源集群缓冲纹理 |

---

## 5. 阴影相关 (Shadow)

这些变量定义在 `ShadowCommon.glsl` 中。

| 变量名 | 类型 | 说明 |
|--------|------|------|
| `u_ShadowLightDirection` | `vec3` | 阴影光源方向 |
| `u_ShadowBias` | `vec4` | 阴影偏移参数 |
| `u_ShadowSplitSpheres[4]` | `vec4[4]` | 级联阴影分割球 |
| `u_ShadowMatrices[4]` | `mat4[4]` | 级联阴影矩阵 |
| `u_ShadowMapSize` | `vec4` | 阴影贴图尺寸 |
| `u_ShadowParams` | `vec4` | 阴影参数 |
| `u_SpotShadowMapSize` | `vec4` | 聚光灯阴影贴图尺寸 |
| `u_SpotViewProjectMatrix` | `mat4` | 聚光灯视图投影矩阵 |
| `u_ShadowMap` | `sampler2D/sampler2DShadow` | 阴影贴图 |
| `u_SpotShadowMap` | `sampler2D/sampler2DShadow` | 聚光灯阴影贴图 |

---

## 6. 全局光照相关 (Global Illumination)

这些变量定义在 `globalIllumination.glsl` 中。

### 6.1 IBL (基于图像的光照)

| 变量名 | 类型 | 说明 |
|--------|------|------|
| `u_AmbientColor` | `vec4` | 环境光颜色 |
| `u_IblSH[9]` | `vec3[9]` | 球谐光照系数（9个） |
| `u_IBLTex` | `samplerCube` | IBL 环境贴图 |
| `u_IBLRoughnessLevel` | `float` | IBL 粗糙度级别（mipmap级数） |
| `u_AmbientIntensity` | `float` | 环境光强度 |
| `u_ReflectionIntensity` | `float` | 反射强度 |

### 6.2 传统 IBL (GI_LEGACYIBL)

| 变量名 | 类型 | 说明 |
|--------|------|------|
| `u_AmbientSHAr` | `vec4` | 球谐系数 Ar |
| `u_AmbientSHAg` | `vec4` | 球谐系数 Ag |
| `u_AmbientSHAb` | `vec4` | 球谐系数 Ab |
| `u_AmbientSHBr` | `vec4` | 球谐系数 Br |
| `u_AmbientSHBg` | `vec4` | 球谐系数 Bg |
| `u_AmbientSHBb` | `vec4` | 球谐系数 Bb |
| `u_AmbientSHC` | `vec4` | 球谐系数 C |
| `u_ReflectTexture` | `samplerCube` | 反射立方体贴图 |
| `u_ReflectCubeHDRParams` | `vec4` | 反射贴图 HDR 参数 |

### 6.3 光照贴图 (Lightmap)

| 变量名 | 类型 | 说明 |
|--------|------|------|
| `u_LightMap` | `sampler2D` | 光照贴图 |
| `u_LightMapDirection` | `sampler2D` | 方向光照贴图（用于方向性光照贴图） |
| `u_LightmapScaleOffset` | `vec4` | 光照贴图缩放偏移 |

### 6.4 反射探针盒投影 (SPECCUBE_BOX_PROJECTION)

| 变量名 | 类型 | 说明 |
|--------|------|------|
| `u_SpecCubeProbePosition` | `vec3` | 反射探针位置 |
| `u_SpecCubeBoxMax` | `vec3` | 反射探针盒最大边界 |
| `u_SpecCubeBoxMin` | `vec3` | 反射探针盒最小边界 |

---

## 7. 体积光照探针 (Volumetric GI)

这些变量定义在 `VolumetricGI.glsl` 中，需要启用 `VOLUMETRICGI` 宏。

| 变量名 | 类型 | 说明 |
|--------|------|------|
| `u_VolGIProbeCounts` | `vec3` | 探针网格数量 (x, y, z) |
| `u_VolGIProbeStep` | `vec3` | 探针间距 |
| `u_VolGIProbeStartPosition` | `vec3` | 探针起始位置 |
| `u_VolGIProbeParams` | `vec4` | 探针参数：x=辐照度纹素, y=距离纹素, z=法线偏移, w=视角偏移 |
| `u_ProbeIrradiance` | `sampler2D` | 探针辐照度纹理 |
| `u_ProbeDistance` | `sampler2D` | 探针距离纹理 |

---

## 8. 2D 渲染相关

这些变量用于 2D 精灵渲染，定义在 `Sprite2DVertex.glsl` 和 `Sprite2DFrag.glsl` 中。

### 8.1 变换相关

| 变量名 | 类型 | 说明 |
|--------|------|------|
| `u_view2D` | `mat3` | 2D 视图矩阵（需要 CAMERA2D 宏） |
| `u_NMatrix_0` | `vec3` | 节点变换矩阵第一行 |
| `u_NMatrix_1` | `vec3` | 节点变换矩阵第二行 |
| `u_size` | `vec2` | 画布尺寸 |
| `u_InvertMat_0` | `vec3` | 翻转矩阵第一行（需要 RENDERTEXTURE 宏） |
| `u_InvertMat_1` | `vec3` | 翻转矩阵第二行（需要 RENDERTEXTURE 宏） |

### 8.2 裁剪相关

| 变量名 | 类型 | 说明 |
|--------|------|------|
| `u_clipMatDir` | `vec4` | 全局裁剪方向 |
| `u_clipMatPos` | `vec4` | 全局裁剪位置 |
| `u_mClipMatDir` | `vec4` | 材质裁剪方向（需要 MATERIALCLIP 宏） |
| `u_mClipMatPos` | `vec4` | 材质裁剪位置（需要 MATERIALCLIP 宏） |

### 8.3 纹理相关

| 变量名 | 类型 | 说明 |
|--------|------|------|
| `u_spriteTexture` | `sampler2D` | 精灵纹理 |
| `u_spriteTextureArray` | `sampler2DArray` | 精灵纹理数组（需要 USE_TEX_ARRAY 宏） |
| `u_TexRange` | `vec4` | 纹理范围：startU, startV, uRange, vRange |
| `u_baseRenderColor` | `vec4` | 基础渲染颜色 |
| `u_baseRender2DTexture` | `sampler2D` | 基础渲染纹理 |
| `u_tilingOffset` | `vec4` | 纹理平铺偏移 |

### 8.4 2D 光照相关 (LIGHT2D_ENABLE)

| 变量名 | 类型 | 说明 |
|--------|------|------|
| `u_LightDirection` | `vec3` | 2D 光照方向 |
| `u_LightAndShadow2DParam` | `vec4` | 光影图参数（尺寸和位置） |
| `u_LightAndShadow2DAmbient` | `vec4` | 2D 环境光 |
| `u_LightAndShadow2D` | `sampler2D` | 2D 光影纹理 |
| `u_LightAndShadow2D_AddMode` | `sampler2D` | 叠加模式光影纹理 |
| `u_LightAndShadow2D_SubMode` | `sampler2D` | 减法模式光影纹理 |
| `u_normal2DTexture` | `sampler2D` | 2D 法线纹理 |
| `u_normal2DStrength` | `float` | 2D 法线强度 |

### 8.5 其他

| 变量名 | 类型 | 说明 |
|--------|------|------|
| `u_VertAlpha` | `float` | 顶点透明度 |
| `u_vertexSize` | `vec4` | 顶点尺寸（需要 VERTEX_SIZE 宏） |

---

## 9. 粒子系统相关

### 9.1 3D 粒子 (ShuriKen)

这些变量定义在 `particleShuriKenSpriteVS.glsl` 中。

#### Billboard 模式顶点属性

在 Billboard 模式下，每个粒子使用 `a_CornerTextureCoordinate` 属性：

| 属性名 | 类型 | 说明 |
|--------|------|------|
| `a_CornerTextureCoordinate` | `vec4` | 粒子角落和纹理坐标 |

**`a_CornerTextureCoordinate` 分量说明：**

| 分量 | 说明 | 取值范围 |
|------|------|----------|
| `xy` | 角落位置（corner），用于构建 Billboard 四边形 | -0.5 到 0.5 |
| `zw` | 纹理坐标（UV），用于采样纹理 | 0.0 到 1.0 |

**使用示例：**
```glsl
#ifdef BILLBOARD
attribute vec4 a_CornerTextureCoordinate;
#endif

void main() {
    #ifdef BILLBOARD
    // 获取角落位置和UV
    vec2 corner = a_CornerTextureCoordinate.xy;  // 用于构建四边形
    vec2 uv = a_CornerTextureCoordinate.zw;      // 用于纹理采样
    
    // 使用corner构建Billboard
    vec3 cameraRight = vec3(u_View[0][0], u_View[1][0], u_View[2][0]);
    vec3 cameraUp = vec3(u_View[0][1], u_View[1][1], u_View[2][1]);
    vec3 positionWS = centerWS + cameraRight * corner.x + cameraUp * corner.y;
    #endif
}
```

#### 基础参数

| 变量名 | 类型 | 说明 |
|--------|------|------|
| `u_CurrentTime` | `float` | 当前时间 |
| `u_Gravity` | `vec3` | 重力 |
| `u_DragConstanct` | `vec2` | 阻力常数 |
| `u_WorldPosition` | `vec3` | 发射器世界位置 |
| `u_WorldRotation` | `vec4` | 发射器世界旋转（四元数） |
| `u_ThreeDStartRotation` | `int` | 是否使用3D起始旋转 |
| `u_Shape` | `int` | 发射形状 |
| `u_ScalingMode` | `int` | 缩放模式 |
| `u_PositionScale` | `vec3` | 位置缩放 |
| `u_SizeScale` | `vec3` | 尺寸缩放 |
| `u_StretchedBillboardLengthScale` | `float` | 拉伸广告牌长度缩放 |
| `u_StretchedBillboardSpeedScale` | `float` | 拉伸广告牌速度缩放 |
| `u_SimulationSpace` | `int` | 模拟空间 |

**生命周期速度 (VELOCITYOVERLIFETIMECURVE)：**
| 变量名 | 类型 | 说明 |
|--------|------|------|
| `u_VOLSpaceType` | `int` | 速度空间类型 |
| `u_VOLVelocityConst` | `vec3` | 恒定速度 |
| `u_VOLVelocityGradientX[2]` | `vec4[2]` | X轴速度曲线 |
| `u_VOLVelocityGradientY[2]` | `vec4[2]` | Y轴速度曲线 |
| `u_VOLVelocityGradientZ[2]` | `vec4[2]` | Z轴速度曲线 |

**生命周期颜色 (COLOROVERLIFETIME)：**
| 变量名 | 类型 | 说明 |
|--------|------|------|
| `u_ColorOverLifeGradientColors[]` | `vec4[]` | 颜色渐变 |
| `u_ColorOverLifeGradientAlphas[]` | `vec4[]` | 透明度渐变 |
| `u_ColorOverLifeGradientRanges` | `vec4` | 渐变范围 |

**生命周期尺寸 (SIZEOVERLIFETIMECURVE)：**
| 变量名 | 类型 | 说明 |
|--------|------|------|
| `u_SOLSizeGradient[2]` | `vec4[2]` | 尺寸曲线 |
| `u_SOLSizeGradientX[2]` | `vec4[2]` | X轴尺寸曲线（分离模式） |
| `u_SOLSizeGradientY[2]` | `vec4[2]` | Y轴尺寸曲线（分离模式） |
| `u_SOLSizeGradientZ[2]` | `vec4[2]` | Z轴尺寸曲线（分离模式） |

**生命周期旋转 (ROTATIONOVERLIFETIME)：**
| 变量名 | 类型 | 说明 |
|--------|------|------|
| `u_ROLAngularVelocityConst` | `float` | 恒定角速度 |
| `u_ROLAngularVelocityGradient[2]` | `vec4[2]` | 角速度曲线 |

**纹理动画 (TEXTURESHEETANIMATIONCURVE)：**
| 变量名 | 类型 | 说明 |
|--------|------|------|
| `u_TSACycles` | `float` | 动画循环次数 |
| `u_TSASubUVLength` | `vec2` | 子UV长度 |
| `u_TSAGradientUVs[2]` | `vec4[2]` | UV渐变 |

### 9.2 2D 粒子

这些变量定义在 `Particle2DCommon.glsl` 中。

| 变量名 | 类型 | 说明 |
|--------|------|------|
| `u_CurrentTime` | `float` | 当前时间 |
| `u_UnitPixels` | `float` | 单位像素 |
| `u_SpriteRotAndScale` | `vec4` | 精灵旋转和缩放 |

---

## 10. 拖尾系统相关

这些变量定义在 `TrailVertexUtil.glsl` 中。

| 变量名 | 类型 | 说明 |
|--------|------|------|
| `u_CurTime` | `float` | 当前时间 |
| `u_LifeTime` | `float` | 生命周期 |
| `u_WidthCurve[10]` | `vec4[10]` | 宽度曲线 |
| `u_WidthCurveKeyLength` | `int` | 宽度曲线关键帧数量 |

---

## 简单动画着色器 (SimpleAnimator)

用于骨骼动画的 GPU 蒙皮。

| 变量名 | 类型 | 说明 |
|--------|------|------|
| `u_SimpleAnimatorParams` | `vec4` | 动画参数 |
| `u_SimpleAnimatorTexture` | `sampler2D` | 动画纹理 |

---

## 使用注意事项

1. **引入对应的 GLSL 文件**：使用这些内置变量前，需要在着色器中引入对应的 glsl 文件：
   ```glsl
   #include "SceneCommon.glsl";
   #include "CameraCommon.glsl";
   #include "Sprite3DCommon.glsl";
   ```

2. **宏定义依赖**：某些变量需要特定的宏定义才能使用，例如：
   - `DIRECTIONLIGHT` - 方向光相关变量
   - `POINTLIGHT` - 点光源相关变量
   - `SPOTLIGHT` - 聚光灯相关变量
   - `SHADOW` - 阴影相关变量
   - `GI_IBL` - IBL 相关变量
   - `LIGHTMAP` - 光照贴图相关变量
   - `VOLUMETRICGI` - 体积光照探针相关变量

3. **Uniform Block**：在支持 Uniform Block 的平台上（WebGL2/WebGPU），这些变量会被打包到 Uniform Block 中以提高性能。

4. **常用时间动画示例**：
   ```glsl
   #include "SceneCommon.glsl";
   
   void main() {
       // 正弦波动画
       float wave = sin(u_Time * 3.14159);
       
       // 循环动画 (0-1)
       float loop = fract(u_Time * 0.5);
       
       // 脉冲效果
       float pulse = abs(sin(u_Time * 2.0));
   }
   ```

---

## 版本信息

- 引擎版本：LayaAir 3.0
- 文档更新日期：2026-02-06
