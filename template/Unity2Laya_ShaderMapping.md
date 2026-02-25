# Unity Shader 到 LayaAir Shader 变量映射文档

---

## 转换概述

### 原始文件
- **Unity Shader**: `assets/test2/FishStandard_Base_ori.shader` (871行)
- **目标文件**: `assets/test1/FishStandard_Base.shader`

### 参考文件
| 文件 | 用途 |
|-----|------|
| `assets/test2/PBRShader.shader` | LayaAir PBR材质模板，作为转换基准框架 |
| `assets/test2/convert_LayaWebGL.txt` | Laya编译后的WebGL代码，用于查看实际变量名和结构体定义 |
| `assets/test2/glsls/Math.glsl` | 内置数学函数库，避免重复定义 |
| `assets/test2/glsls/BRDF.glsl` | 内置BRDF函数库 |
| `assets/test2/glsls/pbrCommon.glsl` | PBR通用定义，包含PixelParams结构体 |
| `assets/test2/glsls/pbrVertex.glsl` | PBR顶点着色器库 |
| `assets/test2/glsls/pbrFrag.glsl` | PBR片段着色器库 |
| `assets/test2/glsls/pbrGI.glsl` | 全局光照库 |

### 转换基准原则

1. **框架结构**: 以 `PBRShader.shader` 为基准框架
   - 使用相同的include文件
   - 使用相同的顶点着色器结构
   - 使用 `SurfaceInputs` + `PBR_Metallic_Flow` 模式

2. **变量命名**: 
   - Unity `_PropertyName` → Laya `u_PropertyName`
   - 矩阵变量按Laya规范命名（如 `u_View` 而非 `u_ViewMatrix`）

3. **内置函数**: 
   - 使用 `convert_LayaWebGL.txt` 确认实际可用的函数
   - 使用 `glsls/*.glsl` 确认已定义的函数，避免重复定义

4. **数据传递**:
   - 使用 `PixelParams` 结构体传递顶点数据
   - `viewDir` 需手动计算，不在 `PixelParams` 中

### Unity Shader 功能模块

原始Unity Shader包含以下功能模块：

| 模块 | 转换状态 | 说明 |
|-----|---------|------|
| PBR基础材质 | ✅ 已转换 | BaseMap, MAER贴图, 法线贴图 |
| NPR卡通光照 | ✅ 已转换 | 明暗阈值分离 (Med/Shadow/Reflect) |
| 风格化高光 | ✅ 已转换 | GGX + Stylized 混合 |
| IBL环境光 | ✅ 已转换 | Cubemap采样，支持旋转 |
| Matcap效果 | ✅ 已转换 | 两套Matcap系统 |
| Fresnel边缘光 | ✅ 已转换 | 方向性菲涅尔 |
| Effect Rim | ✅ 已转换 | 额外边缘光效果 |
| HSV调整 | ✅ 已转换 | 色相/饱和度/明度 |
| 对比度调整 | ✅ 已转换 | - |
| Tonemapping | ✅ 已转换 | Hell曲线色调映射 |
| 自发光 | ✅ 已转换 | - |
| Hit Color/Overlay | ⏳ 部分 | 简化实现 |
| Streamer流光 | ❌ 未转换 | 需要额外实现 |
| 阴影投射 | ❌ 未转换 | 需要ShadowCaster Pass |

---

## 1. 相机相关 (Camera)

| Unity变量名 | Laya变量名 | 说明 |
|------------|-----------|------|
| `_WorldSpaceCameraPos` | `u_CameraPos` | 相机世界坐标位置 |
| `UNITY_MATRIX_V` | `u_View` | 视图矩阵 |
| `UNITY_MATRIX_P` | `u_Projection` | 投影矩阵 |
| `UNITY_MATRIX_VP` | `u_ViewProjection` | 视图投影矩阵 |
| - | `u_CameraDirection` | 相机方向 |
| - | `u_CameraUp` | 相机上方向 |
| - | `u_Viewport` | 视口 (x, y, width, height) |
| - | `u_ProjectionParams` | 投影参数 |
| - | `u_ZBufferParams` | Z缓冲参数 |

## 2. 模型/世界矩阵 (Transform)

| Unity变量名 | Laya变量名 | 说明 |
|------------|-----------|------|
| `UNITY_MATRIX_M` / `unity_ObjectToWorld` | `u_WorldMat` | 世界矩阵 |
| - | `u_WorldInvertFront` | 世界反转前向 |

## 3. 光照相关 (Lighting)

| Unity变量名 | Laya变量名 | 说明 |
|------------|-----------|------|
| `_WorldSpaceLightPos0` | 通过 `getDirectionLight(i, positionWS)` 获取 | 主光源方向 |
| `_LightColor0` | `DirectionLight.color` | 主光源颜色 |
| - | `u_DirationLightCount` | 方向光数量 |

## 4. 时间相关 (Time)

| Unity变量名 | Laya变量名 | 说明 |
|------------|-----------|------|
| `_Time` | `u_Time` | 时间 |

## 5. 雾效相关 (Fog)

| Unity变量名 | Laya变量名 | 说明 |
|------------|-----------|------|
| `unity_FogParams` | `u_FogParams` | 雾参数 |
| `unity_FogColor` | `u_FogColor` | 雾颜色 |

## 6. 环境光/GI相关 (Global Illumination)

| Unity变量名 | Laya变量名 | 说明 |
|------------|-----------|------|
| - | `u_AmbientColor` | 环境光颜色 |
| - | `u_AmbientIntensity` | 环境光强度 |
| - | `u_ReflectionIntensity` | 反射强度 |
| - | `u_IBLTex` | IBL纹理 (Cubemap) |
| - | `u_IBLRoughnessLevel` | IBL粗糙度级别 |
| - | `u_GIRotate` | GI旋转 |
| - | `u_IBLDFG` | IBL DFG LUT纹理 |

## 7. 材质属性命名规范

| Unity格式 | Laya格式 | 示例 |
|----------|---------|------|
| `_PropertyName` | `u_PropertyName` | `_BaseColor` → `u_AlbedoColor` |

## 8. 纹理类型

| Unity类型 | Laya类型 | 说明 |
|----------|---------|------|
| `sampler2D` | `Texture2D` | 2D纹理 |
| `samplerCUBE` | `TextureCube` | 立方体贴图 |

## 9. PixelParams 结构体字段

`PixelParams` 是Laya中从顶点着色器传递到片段着色器的数据结构：

| 字段名 | 类型 | 说明 |
|-------|-----|------|
| `positionWS` | `vec3` | 世界空间位置 |
| `normalWS` | `vec3` | 世界空间法线 |
| `tangentWS` | `vec3` | 世界空间切线 |
| `biNormalWS` | `vec3` | 世界空间副切线 |
| `TBN` | `mat3` | TBN矩阵 |
| `uv0` | `vec2` | UV坐标 (需要 `#ifdef UV`) |
| `vertexColor` | `vec4` | 顶点颜色 (需要 `#ifdef COLOR`) |

**注意**: `viewDir` 不在 `PixelParams` 中，需要手动计算：
```glsl
vec3 viewDir = normalize(u_CameraPos - pixel.positionWS);
```

## 10. SurfaceInputs 结构体字段

`SurfaceInputs` 用于传递材质表面属性：

| 字段名 | 类型 | 说明 |
|-------|-----|------|
| `diffuseColor` | `vec3` | 漫反射颜色 |
| `alpha` | `float` | 透明度 |
| `alphaTest` | `float` | Alpha测试阈值 |
| `metallic` | `float` | 金属度 |
| `smoothness` | `float` | 光滑度 |
| `occlusion` | `float` | 环境光遮蔽 |
| `emissionColor` | `vec3` | 自发光颜色 |
| `normalTS` | `vec3` | 切线空间法线 |

## 11. 常用Include文件

### 顶点着色器
```glsl
#include "Math.glsl";
#include "Scene.glsl";
#include "SceneFogInput.glsl";
#include "Camera.glsl";
#include "Sprite3DVertex.glsl";
#include "VertexCommon.glsl";
#include "PBRVertex.glsl";
```

### 片段着色器
```glsl
#include "Color.glsl";
#include "Scene.glsl";
#include "SceneFog.glsl";
#include "Camera.glsl";
#include "Sprite3DFrag.glsl";
#include "PBRMetallicFrag.glsl";
```

## 12. Math.glsl 中已定义的函数/常量

| 名称 | 类型 | 说明 |
|-----|-----|------|
| `PI` | `const float` | 圆周率 3.14159265359 |
| `INVERT_PI` | `const float` | 1/PI |
| `HALF_PI` | `const float` | PI/2 |
| `MEDIUMP_FLT_MAX` | `const float` | 65504.0 |
| `MEDIUMP_FLT_MIN` | `const float` | 0.00006103515625 |
| `saturate(x)` | `macro` | clamp(x, 0.0, 1.0) |
| `pow2(x)` | `function` | x * x |
| `pow5(x)` | `function` | x^5 |
| `SafeNormalize(vec3)` | `function` | 安全归一化 |
| `normalScale(normal, scale)` | `function` | 法线缩放 |
| `rotationByEuler(vec3, vec3)` | `function` | 欧拉角旋转 |
| `rotationByAxis(vec3, vec3, float)` | `function` | 轴角旋转 |
| `rotationByQuaternions(vec3, vec4)` | `function` | 四元数旋转 |
| `vecmax(vec)` | `function` | 向量最大分量 |
| `vecmin(vec)` | `function` | 向量最小分量 |

## 13. 常见问题

### Q: 如何获取视图方向 (View Direction)?
```glsl
vec3 viewDir = normalize(u_CameraPos - positionWS);
```

### Q: 如何将法线从切线空间转换到世界空间?
```glsl
vec3 normalWS = normalize(pixel.TBN * normalTS);
```

### Q: 如何获取Matcap UV?
```glsl
vec3 viewNormal = (u_View * vec4(normalWS, 0.0)).xyz;
vec2 matcapUV = viewNormal.xy * 0.5 + 0.5;
```

## 14. 转换过程遇到的问题与解决方案

### 问题1: SafeNormalize 重复定义
**错误**: `'SafeNormalize' : redefinition`

**原因**: `Math.glsl` 中已定义了 `SafeNormalize` 函数

**解决**: 删除shader中自定义的 `SafeNormalize`，直接使用内置函数

### 问题2: viewDir 字段不存在
**错误**: `'viewDir' : no such field in structure`

**原因**: `PixelParams` 结构体中没有 `viewDir` 字段

**解决**: 手动计算视图方向
```glsl
// 错误写法
vec3 viewDir = pixel.viewDir;

// 正确写法
vec3 viewDir = normalize(u_CameraPos - pixel.positionWS);
```

### 问题3: u_ViewMatrix 不存在
**错误**: `'u_ViewMatrix' : undeclared identifier`

**原因**: Laya中视图矩阵变量名为 `u_View`

**解决**: 
```glsl
// 错误写法
vec3 viewNormal = (u_ViewMatrix * vec4(normalWS, 0.0)).xyz;

// 正确写法
vec3 viewNormal = (u_View * vec4(normalWS, 0.0)).xyz;
```

### 问题4: PI 常量重复定义
**错误**: `'PI' : redefinition`

**原因**: `Math.glsl` 中已定义了 `PI` 常量

**解决**: 删除shader中自定义的 `PI`，直接使用内置常量

### 问题5: Include文件不存在
**错误**: 找不到 `Shadow.glsl`, `PBRLight.glsl` 等文件

**原因**: 这些是自己臆造的文件名，Laya中不存在

**解决**: 只使用 `PBRShader.shader` 模板中实际使用的include文件

## 15. LayaAir Shader 文件结构

```
Shader3D Start
{
    type: Shader3D,
    name: "ShaderName",
    enableInstancing: true,
    supportReflectionProbe: true,
    shaderType: D3,
    uniformMap: {
        // uniform变量定义
        u_PropertyName: { type: Type, default: value }
    },
    defines: {
        // 宏定义
        DEFINE_NAME: { type: bool, default: false }
    },
    shaderPass: [
        {
            pipeline: Forward,
            VS: VertexShaderName,
            FS: FragmentShaderName
        }
    ]
}
Shader3D End

GLSL Start
#defineGLSL VertexShaderName
    // 顶点着色器代码
#endGLSL

#defineGLSL FragmentShaderName
    // 片段着色器代码
#endGLSL
GLSL End
```

## 16. Uniform类型对照

| Laya类型 | GLSL类型 | 说明 |
|---------|---------|------|
| `Float` | `float` | 浮点数 |
| `Vector4` | `vec4` | 四维向量 |
| `Color` | `vec4` | 颜色（RGBA） |
| `Texture2D` | `sampler2D` | 2D纹理 |
| `TextureCube` | `samplerCube` | 立方体贴图 |
| `Matrix4x4` | `mat4` | 4x4矩阵 |

## 17. Define选项

纹理uniform可以添加define选项，当纹理被赋值时自动启用宏：

```javascript
u_AlbedoTexture: { type: Texture2D, options: { define: "ALBEDOTEXTURE" } }
```

在shader中使用：
```glsl
#ifdef ALBEDOTEXTURE
    vec4 texColor = texture2D(u_AlbedoTexture, uv);
#endif
```

---

## 18. 功能模块排查结果与修复记录

### 排查方法
通过添加Debug调试开关，逐个模块对比Unity和Laya的输出效果：
- **Unity**: 在材质中设置 `_DEBUG_Enable[模块名]` 参数
- **Laya**: 在材质中设置 `u_DEBUG_Mode` 参数（0=最终输出, 1-25=各模块单独输出）

### 模块排查总结

| 模块 | 状态 | 问题 | 修复方案 |
|------|------|------|----------|
| BaseColor | ✅ | - | - |
| Normal | ✅ | - | - |
| Metallic | ✅ | Laya显示全白 | 材质defines添加`MAERMAP`宏 |
| Smoothness | ✅ | 颜色深浅不一致 | 修复Remap逻辑，使用reverse remap |
| AO | ✅ | - | - |
| NPR Radiance | ✅ | Laya显示贴图色/全白 | 1. 材质defines添加`USENPR`宏<br>2. 修复光方向为零向量的问题 |
| Specular | ✅ | - | - |
| IBL | ⏭️ 跳过 | Laya缺少CubeMap资源 | 需要导出IBL贴图到Laya |
| Matcap | ✅ | - | - |
| Fresnel | ✅ | Laya显示绿色，Unity显示黑色 | 1. 添加`u_FresnelFit`参数<br>2. 模拟Unity的BRDF处理 |
| Rim | ⏭️ 跳过 | 两边强度都为0 | 无需修复 |
| Emission | ✅ | Laya颜色区域少于Unity | 1. `u_EmissionIntensity`改为7<br>2. 移除MAER.b遮罩乘法 |
| HSV | ⏭️ 跳过 | 两边都禁用 | 无需修复 |
| Contrast | ⏭️ 跳过 | 两边都禁用 | 无需修复 |
| Tonemapping | ⏭️ 跳过 | 两边都禁用 | 无需修复 |

---

## 19. 关键修复详解

### 19.1 Metallic/Smoothness 宏定义问题
**问题**: Laya的MAER贴图未正确加载，显示默认白色

**原因**: 材质文件的`defines`数组中缺少必要的宏定义

**解决**: 在Laya材质(`.lmat`)的`defines`数组中添加：
```json
"defines": [
  "MAERMAP",    // MAER贴图宏
  "USENPR",     // NPR渲染宏
  "EMISSION",   // 自发光宏
  // ... 其他宏
]
```

### 19.2 Smoothness Remap 逻辑差异
**问题**: Smoothness颜色深浅与Unity不一致

**Unity代码**:
```hlsl
float sm = lerp(_SmoothnessRemapMax, _SmoothnessRemapMin, maer.a) * _Smoothness;
```
这是**反向remap**（从Max到Min）

**Laya错误实现**:
```glsl
float remappedSmooth = remapValue(maerSampler.a, 0.0, 1.0, u_SmoothnessRemapMin, u_SmoothnessRemapMax);
```

**Laya正确实现**:
```glsl
float remappedSmooth = mix(u_SmoothnessRemapMax, u_SmoothnessRemapMin, maerSampler.a);
```

### 19.3 NPR 光方向问题
**问题**: NPR Radiance显示全白

**原因**: `u_SelfLightDir`在材质中为`[0,0,0,1]`（零向量），归一化后产生NaN

**解决**: 
1. Shader中添加零向量检测，使用默认光方向：
```glsl
vec3 selfLightDir = u_SelfLightDir.xyz;
if (length(selfLightDir) < 0.001) {
    selfLightDir = vec3(0.0, 1.0, 0.0);  // 默认向上
}
lightDir = normalize(selfLightDir);
```
2. 材质中设置非零的光方向值

### 19.4 Fresnel BRDF 处理差异
**问题**: Laya显示绿色（FresnelColor），Unity显示黑色

**原因**: Unity的Fresnel经过`EnvironmentBRDFCustom`函数处理，由于传入的BRDF参数(smoothness=0, metallic=0)导致输出接近黑色

**Unity计算流程**:
```hlsl
// 传入参数都是0
InitializeBRDFData(float4(0,0,0,0), 0, 0, 0, alpha, brdfData);
// 最终通过BRDF处理，结果接近0
return EnvironmentBRDFCustom(brdfData, radiance, indirectDiffuse, indirectSpecular, fresnelTerm);
```

**Laya解决方案**: 模拟Unity的BRDF处理
```glsl
float roughness = 1.0;  // smoothness=0时
float surfaceReduction = 1.0 / (roughness * roughness + 1.0);  // = 0.5
float grazingTerm = 0.04;  // 默认电介质反射率
vec3 indirectSpecular = vec3(0.1) * mix(1.0, inputs.metallic, u_FresnelMetallic);
fresnelSpecular = surfaceReduction * indirectSpecular * grazingTerm * fresnelTermFinal * u_FresnelColor.rgb;
```

### 19.5 Emission 公式差异
**问题**: Laya自发光区域少于Unity

**Unity公式** (LightingCustomBase.cginc 第337行):
```hlsl
float3 emColor = _EmissionColor * emTex * _EmissionScale;
// 注意: 没有使用MAER.b作为遮罩（第338行被注释）
```

**Laya错误实现**:
```glsl
inputs.emissionColor = u_EmissionColor.rgb * u_EmissionIntensity;
inputs.emissionColor *= maerEmission.b;  // 错误：额外乘以MAER.b
inputs.emissionColor *= emissionSampler.rgb;
```

**Laya正确实现**:
```glsl
inputs.emissionColor = u_EmissionColor.rgb * u_EmissionIntensity;
// 不乘以MAER.b
inputs.emissionColor *= emissionSampler.rgb;
```

**参数差异**:
- Unity: `_EmissionScale = 7`
- Laya: `u_EmissionIntensity` 需要设置为 `7`

---

## 20. Unity与Laya参数对照表

### 关键参数差异

| 功能 | Unity参数 | Laya参数 | 注意事项 |
|------|----------|---------|----------|
| 基础颜色 | `_BaseColor` | `u_AlbedoColor` | RGBA格式一致 |
| 自发光强度 | `_EmissionScale` | `u_EmissionIntensity` | **需手动同步** |
| Fresnel适配 | `_FresnelFit` | `u_FresnelFit` | 默认1，使用相机前向 |
| 光方向 | `_SelfLightDir` | `u_SelfLightDir` | **不能为零向量** |

### 材质宏定义检查清单

确保Laya材质的`defines`数组包含以下必要宏：

| 宏名称 | 用途 | 何时需要 |
|--------|------|----------|
| `ALBEDOTEXTURE` | 基础贴图 | 有BaseMap时 |
| `NORMALTEXTURE` | 法线贴图 | 有NormalMap时 |
| `MAERMAP` | MAER贴图 | 有MAER贴图时 |
| `USENPR` | NPR渲染 | 使用NPR卡通渲染时 |
| `EMISSION` | 自发光 | 需要自发光效果时 |
| `EMISSIONTEXTURE` | 自发光贴图 | 有EmissionTexture时 |
| `MATCAPMAP` | Matcap贴图 | 有Matcap贴图时 |
| `MATCAPADDMAP` | Matcap叠加贴图 | 有MatcapAdd贴图时 |
| `IBLMAP` | IBL环境贴图 | 有IBL CubeMap时 |
| `MASKMAP` | 遮罩贴图 | 有Mask贴图时 |
| `COLOR` | 顶点颜色 | 使用顶点颜色时 |

---

## 21. Debug调试模式参考

### Laya Debug模式 (u_DEBUG_Mode)

| 值 | 输出内容 |
|----|----------|
| 0 | 最终渲染结果 |
| 1 | BaseColor |
| 2 | Normal |
| 3 | Metallic |
| 4 | Smoothness |
| 5 | AO |
| 6 | NPR Radiance |
| 7 | Specular |
| 8 | IBL |
| 9 | Matcap |
| 10 | Fresnel (简化版) |
| 11 | Rim |
| 12 | Emission |
| 20-25 | Fresnel分步骤 |
| 30-32 | Emission分步骤 |

### Fresnel分步骤Debug

| Laya Mode | Unity Mode | 内容 |
|-----------|------------|------|
| 20 | 3 | Mask.a通道 |
| 21 | 4 | dot(N,V)值 |
| 22 | 5 | LinearStep结果 |
| 23 | 6 | FresnelIntensity |
| 24 | 7 | fresnelTerm最终值 |
| 25 | 2 | 完整Fresnel输出 |

---

## 更新日志

| 日期 | 更新内容 |
|-----|---------|
| 2026-02-05 | 初始文档创建，完成基础映射表 |
| 2026-02-05 | 添加转换概述、参考文件列表 |
| 2026-02-05 | 添加问题解决方案、文件结构说明 |
| 2026-02-05 | 添加功能模块逐一排查流程，Unity shader添加DEBUG调试开关 |
| 2026-02-05 | **完成全模块排查**，记录所有修复方案：<br>- Metallic/Smoothness宏定义修复<br>- Smoothness Remap逻辑修复<br>- NPR光方向零向量修复<br>- Fresnel BRDF处理模拟<br>- Emission公式修正（移除MAER.b） |

---

## 22. LayaAir Shader类型与材质类型

### 22.1 ShaderFeatureType (shaderType)

LayaAir引擎中的Shader有不同的渲染类型，需要在导出时正确设置 `shaderType` 字段：

| 枚举值 | 名称 | 说明 |
|-------|------|------|
| -1 | None | 无类型 |
| 0 | Default | 默认类型 |
| 1 | D3 | 3D渲染Shader（PBR、BlinnPhong、Unlit等3D物体材质） |
| 2 | D2_primitive | 2D图元Shader |
| 3 | D2_TextureSV | 2D纹理Shader |
| 4 | D2_BaseRenderNode2D | 2D基础渲染节点Shader |
| 5 | PostProcess | 后处理Shader（Bloom、Blur、Tonemapping等） |
| 6 | Sky | 天空盒Shader |
| 7 | Effect | 特效Shader（粒子系统材质、Trail拖尾等） |

### 22.2 材质类型 (MaterialType)

转换器会根据Unity Shader名称检测材质类型，然后决定：
1. 使用哪种Shader模板生成代码
2. 设置正确的shaderType

| 材质类型 | 对应shaderType | 说明 |
|---------|---------------|------|
| PBR | D3 | 标准PBR材质（Standard、URP/Lit等） |
| BLINNPHONG | D3 | BlinnPhong光照材质 |
| Unlit | D3 | 无光照材质 |
| PARTICLESHURIKEN | Effect | 粒子材质（Particles/*、Laya/Legacy/Particle等） |
| SkyBox | Sky | 天空盒材质 |
| Custom | D3 | 自定义材质（生成完整PBR Shader） |

### 22.3 自动检测规则

转换器会根据Unity Shader名称自动检测材质类型：

| Unity Shader名称关键字 | 检测为材质类型 | shaderType |
|----------------------|--------------|------------|
| `particle`, `additive`, `alpha blended`, `multiply` | PARTICLESHURIKEN | Effect |
| `laya/legacy/particle`, `laya/legacy/effect`, `laya/legacy/trail` | PARTICLESHURIKEN | Effect |
| `skybox/procedural` | SkyProcedural | Sky |
| `skybox/panoramic` | SkyPanoramic | Sky |
| `skybox` | SkyBox | Sky |
| `unlit`, `laya/unlit` | Unlit | D3 |
| `blinnphong`, `diffuse`, `bumped`, `specular`, `vertexlit` | BLINNPHONG | D3 |
| `standard`, `/lit`, `pbr` | PBR | D3 |
| 其他 | Custom | D3 |

### 22.4 不同材质类型的Shader结构

#### Effect类型（粒子）
- 简单的Unlit渲染
- 支持顶点颜色（默认启用ENABLEVERTEXCOLOR）
- 支持纹理和颜色混合
- 不需要光照计算

```
shaderType:Effect,
uniformMap:{
    u_AlbedoColor: { type: Color, default: [1, 1, 1, 1] },
    u_AlbedoTexture: { type: Texture2D, options: { define: "ALBEDOTEXTURE" } },
    u_AlbedoIntensity: { type: Float, default: 1.0 },
}
```

#### D3类型（PBR）
- 完整的PBR光照模型
- 支持NPR、IBL、Matcap等扩展特性
- 支持法线贴图、MAER贴图等

```
shaderType:D3,
uniformMap:{
    // 完整的PBR属性...
}
```

### 22.5 示例

```
// 3D PBR材质
shaderType:D3,

// 天空盒
shaderType:Sky,

// 后处理
shaderType:PostProcess,

// 粒子特效
shaderType:Effect,
```

---

## 23. 自动转换器实现 (CustomShaderExporter.cs)

### 23.1 转换器功能概述

`CustomShaderExporter.cs` 实现了Unity自定义Shader到LayaAir Shader的自动转换功能：

1. **Shader文件生成**: 自动生成 `.shader` 文件，包含完整的GLSL代码
2. **材质文件导出**: 导出 `.lmat` 材质文件，包含所有属性值和宏定义
3. **特性检测**: 自动检测Shader特性（NPR、IBL、Matcap、Fresnel等）
4. **属性映射**: Unity属性名自动转换为Laya属性名

### 23.2 属性名映射规则

| Unity格式 | Laya格式 | 示例 |
|----------|---------|------|
| `_PropertyName` | `u_PropertyName` | `_BaseColor` → `u_AlbedoColor` |
| `_XXX_ST` | `u_TilingOffset` | `_MainTex_ST` → `u_TilingOffset` |

### 23.3 纹理宏定义映射

| Unity纹理属性 | Laya宏定义 |
|--------------|-----------|
| `_MainTex` / `_BaseMap` | `ALBEDOTEXTURE` |
| `_BumpMap` / `_NormalMap` | `NORMALTEXTURE` |
| `_MAER` | `MAERMAP` |
| `_Mask` | `MASKMAP` |
| `_IBLMap` | `IBLMAP` |
| `_MatcapMap` | `MATCAPMAP` |
| `_MatcapAddMap` | `MATCAPADDMAP` |
| `_EmissionTexture` | `EMISSIONTEXTURE` |

### 23.4 特性检测关键字

| 特性 | 检测关键字 |
|-----|-----------|
| NPR卡通渲染 | `Med`, `Shadow`, `Reflect`, `GI`, `Stylized`, `Toon` |
| IBL环境光 | `IBL` |
| Matcap | `Matcap` |
| Fresnel | `Fresnel`, `fresnel` |
| Rim边缘光 | `Rim` (排除`Remap`) |
| HSV调整 | `HSV`, `Hue`, `Saturation` |
| Tonemapping | `Tone`, `WhitePoint` |

### 23.5 生成的Shader结构

```
Shader3D Start
{
    type: Shader3D,
    name: ShaderName,
    uniformMap: {
        // 按类别分组的属性
        // Basic, Base Color, PBR, Normal, Mask, IBL, Matcap, Emission, NPR, Specular, Fresnel, Rim, HSV, Contrast, Tonemapping, Light Control
    },
    defines: {
        EMISSION, ENABLEVERTEXCOLOR, USENPR
    },
    shaderPass: [...]
}
Shader3D End

GLSL Start
#defineGLSL ShaderNameVS
    // 顶点着色器 - 使用PBRVertex.glsl
#endGLSL

#defineGLSL ShaderNameFS
    // 片元着色器
    // 1. 工具函数 (LinearStep, remapValue, HSV, Fresnel等)
    // 2. initSurfaceInputs函数
    // 3. main函数 (NPR/PBR渲染流程)
#endGLSL
GLSL End
```

### 23.6 材质文件结构

```json
{
    "version": "LAYAMATERIAL:04",
    "props": {
        "type": "ShaderName",
        "s_Cull": 2,
        "s_Blend": 0,
        "s_BlendSrc": 1,
        "s_BlendDst": 0,
        "alphaTest": false,
        "renderQueue": 2000,
        "u_AlbedoColor": [1, 1, 1, 1],
        "u_Metallic": 0.0,
        // ... 其他属性
        "textures": [
            { "name": "u_AlbedoTexture", "path": "texture.png" }
        ],
        "defines": ["ALBEDOTEXTURE", "NORMALTEXTURE", "MAERMAP", "USENPR", "EMISSION"]
    }
}
```

### 23.7 使用方法

1. 在Unity中启用自定义Shader导出功能
2. 导出场景时，自动检测使用自定义Shader的材质
3. 为每个自定义Shader生成对应的 `.shader` 文件
4. 为每个材质生成对应的 `.lmat` 文件

### 23.8 注意事项

1. **Cubemap纹理**: 目前Cubemap导出需要手动处理
2. **光方向**: `u_SelfLightDir` 不能为零向量，否则会导致NaN
3. **宏定义**: 确保材质的 `defines` 数组包含所有必要的宏
4. **Smoothness Remap**: Unity使用反向remap（从Max到Min）

---
*最后更新: 2026-02-06*



Unity 粒子 Shader 导出到 Laya 的对应标准
一、Uniform 变量对应
Unity	Laya	说明
_MainTex_ST	u_TilingOffset	主纹理 Tiling/Offset，Laya 用统一 TilingOffset
_Time / _Time.g	u_CurrentTime	时间，用于 UV 滚动、动画等
_LayerColor	u_LayerColor	层颜色
_LayerMultiplier	u_LayerMultiplier	层乘数
_Alpha	u_Alpha	透明度
二、顶点属性对应
Unity	Laya	说明
v.uv0	a_CornerTextureCoordinate.xy / a_SimulationUV	基础 UV
v.vcolor	v_Color（由 computeParticleColor 计算）	粒子颜色
v.uv0.zw	a_CornerTextureCoordinate.zw / a_SimulationUV.zw	CustomData 通道
v.uv1	a_SimulationUV	CustomData 扩展
不要用通用的 Vertex 结构，使用 Laya 的粒子属性。
三、Varying 传递对应
Unity v2f	Laya varying	说明
i.uv.xy	v_Texcoord0.xy	基础 UV
i.uv.zw	v_Texcoord0.zw	主纹理滚动偏移
i.uv2.xy	v_Texcoord4.xy	第二层滚动
i.uv2.zw	v_Texcoord4.zw	第三层滚动
i.uv3.xy	v_Texcoord6.xy	扭曲纹理滚动
i.uv3.zw	v_Texcoord6.zw	溶解扭曲滚动
i.vcolor	v_Color	粒子颜色
i.screenPos	v_ScreenPos	屏幕坐标
i.data	v_Texcoord7	CustomData
四、Shader Define 对应
Unity	Laya	说明
_LAYERTYPE_TWO / _LAYERTYPE_THREE	DETAILTEX / DETAILTEX2MAP	多纹理层
_USEDISTORT0_ON	DISTORTTEX0MAP	扭曲
_USEDISSOLVE_ON	USEDISSOLVE	溶解
_ROTATIONTEX_ON	ROTATIONTEX	UV 旋转
_USERIM_ON	USERIM	边缘光
_USELIGHTING_ON	USELIGHTING	光照
_USECUSTOMDATA_ON	USECUSTOMDATA	CustomData
五、时间与 UV 计算
Unity：用 frac(float2(_Scroll0X, _Scroll0Y) * _Time.g) 做 UV 滚动
Laya：用 fract(vec2(u_Scroll0X, u_Scroll0Y) * u_CurrentTime)
时间统一用 u_CurrentTime，不要用 u_Time
六、Tiling/Offset 应用
Unity：mainUV = uv * _MainTex_ST.xy + _MainTex_ST.zw
Laya 主纹理：mainUV = uv * u_TilingOffset.xy + u_TilingOffset.zw
其他纹理：使用各自 u_xxx_ST（如 u_DetailTex_ST、u_DissolveTexture_ST）
七、粒子计算流程
不使用 Unity 的顶点变换，改用 Laya 的粒子函数：
computeParticlePosition：位置
computeParticleColor：颜色
computeParticleSizeBillbard / computeParticleSizeMesh：大小
computeParticleRotationFloat / computeParticleRotationVec3：旋转
computeParticleUV：UV
参考 Laya 的 Particle.shader 模板实现。
八、混合与透明度
Unity 常用预乘 Alpha：color.rgb *= color.a
Laya 边缘透明可能表现为黑色，与混合模式（OPAQUE、TRANSPARENT、CUTOUT）有关
溶解的 clip() 在 CUTOUT 下更符合预期，TRANSPARENT 下需结合混合模式调整
九、矩阵与数学
HLSL 的 mul(向量, 矩阵) 在 GLSL 中为 矩阵 * 向量
UV 旋转：GLSL 用 mat2，注意行列顺序与 Unity 一致
十、导出检查清单
主纹理 Tiling/Offset 使用 u_TilingOffset，而不是 u_MainTex_ST
所有时间相关计算使用 u_CurrentTime
顶点处理使用 Laya 粒子函数，不沿用 Unity 顶点逻辑
用 a_CornerTextureCoordinate、a_SimulationUV 等粒子属性传递 UV
Shader define 名称按上表映射
溶解、边缘透明等效果需结合渲染模式（CUTOUT/TRANSPARENT）再做调优

新增的内容