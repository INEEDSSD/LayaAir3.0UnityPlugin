# Unity Shader 转 LayaAir Shader 转换指南

## 目录
1. [概述](#1-概述)
2. [文件格式对比](#2-文件格式对比)
3. [数据类型映射](#3-数据类型映射)
4. [内置函数映射](#4-内置函数映射)
5. [内置变量映射](#5-内置变量映射)
6. [顶点属性映射](#6-顶点属性映射)
7. [渲染状态映射](#7-渲染状态映射)
8. [Include文件系统](#8-include文件系统)
9. [宏定义系统](#9-宏定义系统)
10. [光照系统差异](#10-光照系统差异)
11. [转换流程](#11-转换流程)
12. [完整示例](#12-完整示例)
13. [常见问题](#13-常见问题)

---

## 1. 概述

### 1.1 核心差异

| 项目 | Unity | LayaAir |
|------|-------|---------|
| 着色器语言 | HLSL/Cg | GLSL ES 3.0 |
| 文件格式 | `.shader` | `.shader` (JSON+GLSL) |
| 渲染后端 | DirectX/OpenGL/Metal/Vulkan | WebGL/WebGPU |
| Surface Shader | 支持 | 不支持（需手动实现） |
| Pass系统 | 多Pass叠加光照 | Cluster-based单Pass光照 |

### 1.2 转换可行性

| Shader类型 | 可转换性 | 备注 |
|------------|----------|------|
| Unlit Shader | ✅ 简单 | 直接语法转换 |
| PBR/Standard | ✅ 简单 | 参数一一对应 |
| 顶点动画 | ✅ 简单 | 语法转换 |
| UV/颜色动画 | ✅ 简单 | 语法转换 |
| 自定义光照模型 | ⚠️ 中等 | 需理解两边光照系统 |
| 后处理效果 | ⚠️ 中等 | 核心算法可复用 |
| Surface Shader | ⚠️ 中等 | 需展开为Vert/Frag |
| GrabPass/透明折射 | ⚠️ 中等 | 需用CommandBuffer |
| Tessellation | ❌ 困难 | Web平台支持有限 |
| Geometry Shader | ❌ 困难 | Web平台不支持 |

---

## 2. 文件格式对比

### 2.1 Unity Shader 结构

```hlsl
Shader "Custom/MyShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200
        
        Pass
        {
            Name "ForwardBase"
            Tags { "LightMode" = "ForwardBase" }
            
            Cull Back
            ZWrite On
            ZTest LEqual
            Blend Off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            
            // ... shader code ...
            ENDCG
        }
        
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            // ...
        }
    }
    FallBack "Diffuse"
}
```

### 2.2 LayaAir Shader 结构

```glsl
Shader3D Start
{
    name: "CustomMyShader",
    enableInstancing: true,
    supportReflectionProbe: true,
    shaderType: "D3",
    uniformMap: {
        u_MainTexture: { type: Texture2D, default: "white" },
        u_Color: { type: Color, default: [1, 1, 1, 1] },
        u_Metallic: { type: Float, default: 0.0, range: [0.0, 1.0] }
    },
    defines: {
        ALBEDOTEXTURE: { type: bool, default: false }
    },
    shaderPass: [
        {
            pipeline: "Forward",
            VS: "MainVS",
            FS: "MainFS",
            renderState: {
                Cull: "Back",
                DepthWrite: true,
                DepthTest: "LEqual",
                Blend: "Disable"
            }
        },
        {
            pipeline: "ShadowCaster",
            VS: "ShadowVS",
            FS: "ShadowFS"
        }
    ]
}
Shader3D End

GLSL Start
#defineGLSL MainVS
    // 顶点着色器代码
#endGLSL

#defineGLSL MainFS
    // 片元着色器代码
#endGLSL

#defineGLSL ShadowVS
    // 阴影顶点着色器
#endGLSL

#defineGLSL ShadowFS
    // 阴影片元着色器
#endGLSL
GLSL End
```

---

## 3. 数据类型映射

### 3.1 基础类型

| Unity HLSL | LayaAir GLSL | 说明 |
|------------|--------------|------|
| `float` | `float` | 单精度浮点 |
| `float2` | `vec2` | 二维向量 |
| `float3` | `vec3` | 三维向量 |
| `float4` | `vec4` | 四维向量 |
| `half` | `float` | GLSL无half，用float |
| `half2` | `vec2` | - |
| `half3` | `vec3` | - |
| `half4` | `vec4` | - |
| `fixed` | `float` | GLSL无fixed |
| `fixed4` | `vec4` | - |
| `int` | `int` | 整数 |
| `int2` | `ivec2` | 整数向量 |
| `int3` | `ivec3` | - |
| `int4` | `ivec4` | - |
| `uint` | `uint` | 无符号整数 |
| `bool` | `bool` | 布尔值 |

### 3.2 矩阵类型

| Unity HLSL | LayaAir GLSL | 说明 |
|------------|--------------|------|
| `float2x2` | `mat2` | 2x2矩阵 |
| `float3x3` | `mat3` | 3x3矩阵 |
| `float4x4` | `mat4` | 4x4矩阵 |
| `matrix` | `mat4` | 默认4x4 |

### 3.3 纹理类型

| Unity HLSL | LayaAir GLSL | uniformMap类型 |
|------------|--------------|----------------|
| `sampler2D` | `sampler2D` | `Texture2D` |
| `samplerCUBE` | `samplerCube` | `TextureCube` |
| `sampler3D` | `sampler3D` | `Texture3D` |
| `sampler2DArray` | `sampler2DArray` | `Texture2DArray` |

### 3.4 uniformMap 支持的类型

```javascript
// 完整类型列表
{
    "Color": ShaderDataType.Color,        // vec4, RGBA颜色
    "Int": ShaderDataType.Int,            // int
    "Bool": ShaderDataType.Bool,          // bool
    "Float": ShaderDataType.Float,        // float
    "Vector2": ShaderDataType.Vector2,    // vec2
    "Vector3": ShaderDataType.Vector3,    // vec3
    "Vector4": ShaderDataType.Vector4,    // vec4
    "Matrix4x4": ShaderDataType.Matrix4x4,// mat4
    "Matrix3x3": ShaderDataType.Matrix3x3,// mat3
    "Texture2D": ShaderDataType.Texture2D,
    "TextureCube": ShaderDataType.TextureCube,
    "Texture2DArray": ShaderDataType.Texture2DArray,
    "Texture3D": ShaderDataType.Texture3D,
    "StorageTexture2D": ShaderDataType.StorageTexture2D,  // Compute用
    "StorageBuffer": ShaderDataType.DeviceBuffer,          // Compute用
}
```

---

## 4. 内置函数映射

### 4.1 数学函数

| Unity HLSL | LayaAir GLSL | 说明 |
|------------|--------------|------|
| `mul(a, b)` | `a * b` | 矩阵/向量乘法 |
| `lerp(a, b, t)` | `mix(a, b, t)` | 线性插值 |
| `saturate(x)` | `clamp(x, 0.0, 1.0)` | 钳制到[0,1] |
| `frac(x)` | `fract(x)` | 取小数部分 |
| `rsqrt(x)` | `inversesqrt(x)` | 平方根倒数 |
| `atan2(y, x)` | `atan(y, x)` | 反正切 |
| `ddx(x)` | `dFdx(x)` | x方向偏导数 |
| `ddy(x)` | `dFdy(x)` | y方向偏导数 |
| `clip(x)` | `if(x < 0.0) discard;` | 丢弃像素 |

### 4.2 纹理采样

| Unity HLSL | LayaAir GLSL | 说明 |
|------------|--------------|------|
| `tex2D(tex, uv)` | `texture2D(tex, uv)` | 2D纹理采样 |
| `texCUBE(tex, dir)` | `textureCube(tex, dir)` | 立方体纹理 |
| `tex2Dlod(tex, uv)` | `textureLod(tex, uv.xy, uv.w)` | 指定LOD采样 |
| `tex2Dproj(tex, uv)` | `textureProj(tex, uv)` | 投影纹理采样 |
| `tex2Dbias(tex, uv)` | `texture(tex, uv.xy, uv.w)` | 带bias采样 |
| `tex2Dgrad(tex, uv, ddx, ddy)` | `textureGrad(tex, uv, ddx, ddy)` | 指定梯度采样 |

### 4.3 向量/矩阵操作

| Unity HLSL | LayaAir GLSL | 说明 |
|------------|--------------|------|
| `dot(a, b)` | `dot(a, b)` | 点积（相同） |
| `cross(a, b)` | `cross(a, b)` | 叉积（相同） |
| `normalize(v)` | `normalize(v)` | 归一化（相同） |
| `length(v)` | `length(v)` | 长度（相同） |
| `distance(a, b)` | `distance(a, b)` | 距离（相同） |
| `reflect(i, n)` | `reflect(i, n)` | 反射（相同） |
| `refract(i, n, eta)` | `refract(i, n, eta)` | 折射（相同） |
| `transpose(m)` | `transpose(m)` | 转置（相同） |
| `determinant(m)` | `determinant(m)` | 行列式（相同） |

---

## 5. 内置变量映射

### 5.1 变换矩阵

| Unity | LayaAir | 来源 | 说明 |
|-------|---------|------|------|
| `UNITY_MATRIX_M` | `u_WorldMat` | Sprite3DCommon.glsl | 世界矩阵 |
| `UNITY_MATRIX_V` | `u_View` | CameraCommon.glsl | 视图矩阵 |
| `UNITY_MATRIX_P` | `u_Projection` | CameraCommon.glsl | 投影矩阵 |
| `UNITY_MATRIX_VP` | `u_ViewProjection` | CameraCommon.glsl | VP矩阵 |
| `UNITY_MATRIX_MVP` | `u_ViewProjection * u_WorldMat` | 需手动计算 | MVP矩阵 |
| `UNITY_MATRIX_MV` | `u_View * u_WorldMat` | 需手动计算 | MV矩阵 |
| `UNITY_MATRIX_I_V` | `inverse(u_View)` | 需手动计算 | 视图逆矩阵 |
| `unity_ObjectToWorld` | `u_WorldMat` | Sprite3DCommon.glsl | 同MATRIX_M |
| `unity_WorldToObject` | `inverse(u_WorldMat)` | 需手动计算 | 世界到对象 |

### 5.2 相机相关

| Unity | LayaAir | 来源 | 说明 |
|-------|---------|------|------|
| `_WorldSpaceCameraPos` | `u_CameraPos` | CameraCommon.glsl | 相机世界位置 |
| `_CameraDirection` | `u_CameraDirection` | CameraCommon.glsl | 相机方向 |
| `_CameraUp` | `u_CameraUp` | CameraCommon.glsl | 相机上方向 |
| `_ScreenParams` | `u_Viewport` | CameraCommon.glsl | 屏幕参数 |
| `_ProjectionParams` | `u_ProjectionParams` | CameraCommon.glsl | 投影参数 |
| `_ZBufferParams` | `u_ZBufferParams` | CameraCommon.glsl | 深度缓冲参数 |
| `_CameraDepthTexture` | `u_CameraDepthTexture` | CameraCommon.glsl | 深度纹理 |
| `_CameraOpaqueTexture` | `u_CameraOpaqueTexture` | CameraCommon.glsl | 不透明纹理 |

### 5.3 场景相关

| Unity | LayaAir | 来源 | 说明 |
|-------|---------|------|------|
| `_Time` | `u_Time` | SceneCommon.glsl | 时间 |
| `unity_FogParams` | `u_FogParams` | SceneCommon.glsl | 雾参数 |
| `unity_FogColor` | `u_FogColor` | SceneCommon.glsl | 雾颜色 |

### 5.4 光照相关

| Unity | LayaAir | 说明 |
|-------|---------|------|
| `_LightColor0` | 通过`DirectionLight.color`获取 | 主光源颜色 |
| `_WorldSpaceLightPos0` | 通过`DirectionLight.direction`获取 | 主光源方向 |
| `unity_LightAtten` | 通过`Light.attenuation`获取 | 光照衰减 |
| `SHADOW_ATTENUATION(i)` | `sampleShadowmap(shadowCoord)` | 阴影衰减 |

---

## 6. 顶点属性映射

### 6.1 顶点输入

| Unity语义 | LayaAir属性 | Location | 类型 | 说明 |
|-----------|-------------|----------|------|------|
| `POSITION` | `a_Position` | 0 | vec4 | 顶点位置 |
| `COLOR` | `a_Color` | 1 | vec4 | 顶点颜色 |
| `TEXCOORD0` | `a_Texcoord0` | 2 | vec2 | UV0 |
| `NORMAL` | `a_Normal` | 3 | vec3 | 法线 |
| `TANGENT` | `a_Tangent0` | 4 | vec4 | 切线 |
| `BLENDINDICES` | `a_BoneIndices` | 5 | vec4 | 骨骼索引 |
| `BLENDWEIGHT` | `a_BoneWeights` | 6 | vec4 | 骨骼权重 |
| `TEXCOORD1` | `a_Texcoord1` | 7 | vec2 | UV1 |

### 6.2 Instance属性

| LayaAir属性 | Location | 类型 | 说明 |
|-------------|----------|------|------|
| `a_WorldMat` | 8-11 | mat4 | 世界矩阵(Instance) |
| `a_SimpleTextureParams` | 12 | vec4 | 简单动画参数 |
| `a_LightmapScaleOffset` | 13 | vec4 | 光照贴图偏移 |
| `a_WorldInvertFront` | 14 | vec4 | 翻转+自定义数据 |

### 6.3 默认AttributeMap

```javascript
// LayaAir SubShader.DefaultAttributeMap
{
    'a_Position': [0, ShaderDataType.Vector4],
    'a_Normal': [3, ShaderDataType.Vector3],
    'a_Tangent0': [4, ShaderDataType.Vector4],
    'a_Texcoord0': [2, ShaderDataType.Vector2],
    'a_Texcoord1': [7, ShaderDataType.Vector2],
    'a_Color': [1, ShaderDataType.Vector4],
    'a_BoneWeights': [6, ShaderDataType.Vector4],
    'a_BoneIndices': [5, ShaderDataType.Vector4],
    'a_WorldMat': [8, ShaderDataType.Matrix4x4],
    'a_SimpleTextureParams': [12, ShaderDataType.Vector4],
    'a_LightmapScaleOffset': [13, ShaderDataType.Vector4],
    'a_WorldInvertFront': [14, ShaderDataType.Vector4],
}
```

---

## 7. 渲染状态映射

### 7.1 Cull (剔除)

| Unity | LayaAir renderState | 说明 |
|-------|---------------------|------|
| `Cull Back` | `Cull: "Back"` | 剔除背面 |
| `Cull Front` | `Cull: "Front"` | 剔除正面 |
| `Cull Off` | `Cull: "Off"` | 不剔除 |

### 7.2 ZWrite/ZTest (深度)

| Unity | LayaAir renderState | 说明 |
|-------|---------------------|------|
| `ZWrite On` | `DepthWrite: true` | 深度写入 |
| `ZWrite Off` | `DepthWrite: false` | 禁用深度写入 |
| `ZTest Less` | `DepthTest: "Less"` | 小于时通过 |
| `ZTest LEqual` | `DepthTest: "LEqual"` | 小于等于 |
| `ZTest Greater` | `DepthTest: "Greater"` | 大于 |
| `ZTest GEqual` | `DepthTest: "GreaterEqual"` | 大于等于 |
| `ZTest Equal` | `DepthTest: "Equal"` | 等于 |
| `ZTest NotEqual` | `DepthTest: "NotEqual"` | 不等于 |
| `ZTest Always` | `DepthTest: "Always"` | 总是通过 |
| `ZTest Never` | `DepthTest: "Never"` | 从不通过 |
| `ZTest Off` | `DepthTest: "Off"` | 关闭 |

### 7.3 Blend (混合)

| Unity | LayaAir renderState | 说明 |
|-------|---------------------|------|
| `Blend Off` | `Blend: "Disable"` | 禁用混合 |
| `Blend SrcAlpha OneMinusSrcAlpha` | `Blend: "All", SrcBlend: "SourceAlpha", DstBlend: "OneMinusSourceAlpha"` | 标准透明 |

#### 混合因子对照表

| Unity | LayaAir |
|-------|---------|
| `Zero` | `"Zero"` |
| `One` | `"One"` |
| `SrcColor` | `"SourceColor"` |
| `OneMinusSrcColor` | `"OneMinusSourceColor"` |
| `DstColor` | `"DestinationColor"` |
| `OneMinusDstColor` | `"OneMinusDestinationColor"` |
| `SrcAlpha` | `"SourceAlpha"` |
| `OneMinusSrcAlpha` | `"OneMinusSourceAlpha"` |
| `DstAlpha` | `"DestinationAlpha"` |
| `OneMinusDstAlpha` | `"OneMinusDestinationAlpha"` |
| `SrcAlphaSaturate` | `"SourceAlphaSaturate"` |

#### 混合方程对照表

| Unity | LayaAir |
|-------|---------|
| `BlendOp Add` | `BlendEquation: "Add"` |
| `BlendOp Sub` | `BlendEquation: "Subtract"` |
| `BlendOp RevSub` | `BlendEquation: "Reverse_substract"` |
| `BlendOp Min` | `BlendEquation: "Min"` |
| `BlendOp Max` | `BlendEquation: "Max"` |

### 7.4 Stencil (模板)

| Unity | LayaAir renderState |
|-------|---------------------|
| `Stencil { Ref 1 }` | `StencilRef: 1` |
| `Stencil { ReadMask 255 }` | `StencilReadMask: 255` |
| `Stencil { WriteMask 255 }` | `StencilWriteMask: 255` |
| `Stencil { Comp Always }` | `StencilTest: "Always"` |
| `Stencil { Pass Replace }` | `StencilOp: ["Keep", "Keep", "Replace"]` |

#### 模板操作对照表

| Unity | LayaAir |
|-------|---------|
| `Keep` | `"Keep"` |
| `Zero` | `"Zero"` |
| `Replace` | `"Replace"` |
| `IncrSat` | `"IncrementSaturate"` |
| `DecrSat` | `"DecrementSaturate"` |
| `Invert` | `"Invert"` |
| `IncrWrap` | `"IncrementWrap"` |
| `DecrWrap` | `"DecrementWrap"` |

### 7.5 完整renderState示例

```json
renderState: {
    Cull: "Back",
    DepthWrite: true,
    DepthTest: "LEqual",
    Blend: "All",
    SrcBlend: "SourceAlpha",
    DstBlend: "OneMinusSourceAlpha",
    BlendEquation: "Add",
    StencilTest: "Always",
    StencilRef: 1,
    StencilReadMask: 255,
    StencilWriteMask: 255,
    StencilOp: ["Keep", "Keep", "Replace"]
}
```

---

## 8. Include文件系统

### 8.1 核心Include文件

| 文件名 | 用途 | 主要内容 |
|--------|------|----------|
| `Math.glsl` | 数学工具 | 常用数学函数 |
| `Color.glsl` | 颜色处理 | Gamma/Linear转换等 |
| `Scene.glsl` | 场景数据 | u_Time, u_FogParams等 |
| `Camera.glsl` | 相机数据 | u_CameraPos, u_View, u_Projection等 |
| `Sprite3DVertex.glsl` | 3D顶点 | getWorldMatrix(), 骨骼动画 |
| `Sprite3DFrag.glsl` | 3D片元 | 基础片元工具 |
| `VertexCommon.glsl` | 顶点通用 | getVertexParams(), Morph Target |
| `ShadingVertex.glsl` | 着色顶点 | shadingPixelParams() |
| `ShadingFrag.glsl` | 着色片元 | getPixelParams() |
| `Lighting.glsl` | 光照系统 | 光源获取、衰减计算 |
| `ShadowSampler.glsl` | 阴影采样 | sampleShadowmap() |
| `globalIllumination.glsl` | 全局光照 | IBL, Lightmap |

### 8.2 PBR相关Include

| 文件名 | 用途 |
|--------|------|
| `PBRVertex.glsl` | PBR顶点处理 |
| `PBRMetallicFrag.glsl` | PBR金属流程片元 |
| `PBRLighting.glsl` | PBR光照计算 |
| `BRDF.glsl` | BRDF函数 |
| `PBRGI.glsl` | PBR全局光照 |

### 8.3 Include使用方式

```glsl
#include "Math.glsl";
#include "Scene.glsl";
#include "Camera.glsl";
#include "Sprite3DVertex.glsl";
#include "VertexCommon.glsl";
#include "Lighting.glsl";
```

---

## 9. 宏定义系统

### 9.1 Unity vs LayaAir 宏定义

| Unity | LayaAir | 说明 |
|-------|---------|------|
| `#pragma multi_compile _A _B` | `defines: { A: {...}, B: {...} }` | 变体定义 |
| `#pragma shader_feature _X` | 在uniformMap中用options.define | 功能开关 |
| `#if defined(X)` | `#ifdef X` 或 `#if defined(X)` | 条件编译 |

### 9.2 LayaAir常用内置宏

| 宏名 | 说明 | 触发条件 |
|------|------|----------|
| `UV` | 有UV0数据 | 模型含UV0 |
| `UV1` | 有UV1数据 | 模型含UV1 |
| `COLOR` | 有顶点色 | 模型含顶点色 |
| `NORMAL` | 有法线 | 模型含法线 |
| `TANGENT` | 有切线 | 模型含切线 |
| `BONE` | 骨骼动画 | 使用骨骼 |
| `GPU_INSTANCE` | GPU实例化 | 开启Instance |
| `LIGHTMAP` | 光照贴图 | 使用Lightmap |
| `DIRECTIONLIGHT` | 方向光 | 场景有方向光 |
| `POINTLIGHT` | 点光源 | 场景有点光 |
| `SPOTLIGHT` | 聚光灯 | 场景有聚光灯 |
| `CALCULATE_SHADOWS` | 计算阴影 | 开启阴影 |
| `FOG` | 雾效 | 开启雾效 |
| `GRAPHICS_API_GLES3` | GLES3环境 | WebGL2 |

### 9.3 纹理相关宏

```glsl
// 纹理存在时自动定义
#ifdef ALBEDOTEXTURE
    // u_AlbedoTexture 存在
#endif

// Gamma校正宏（自动生成）
#ifdef Gamma_u_AlbedoTexture
    albedoColor = gammaToLinear(albedoColor);
#endif
```

---

## 10. 光照系统差异

### 10.1 架构差异

| 特性 | Unity | LayaAir |
|------|-------|---------|
| 多光源处理 | ForwardAdd多Pass叠加 | Cluster-based单Pass |
| 光源数据 | 内置uniform | u_LightBuffer纹理 |
| 阴影 | 内置宏SHADOW_ATTENUATION | sampleShadowmap()函数 |
| GI | 内置SH + ReflectionProbe | VolumetricGI / Lightmap |

### 10.2 LayaAir光照使用

```glsl
#include "Lighting.glsl";

// 获取方向光
#ifdef DIRECTIONLIGHT
for (int i = 0; i < CalculateLightCount; i++) {
    if (i >= DirectionCount) break;
    DirectionLight dirLight = getDirectionLight(i, positionWS);
    Light light = getLight(dirLight);
    // 使用 light.color, light.dir, light.attenuation
}
#endif

// 获取点光源/聚光灯需要先获取cluster信息
#if defined(POINTLIGHT) || defined(SPOTLIGHT)
ivec4 clusterInfo = getClusterInfo(u_View, u_Viewport, positionWS, gl_FragCoord, u_ProjectionParams);
#endif

#ifdef POINTLIGHT
for (int i = 0; i < CalculateLightCount; i++) {
    if (i >= clusterInfo.x) break;
    PointLight pointLight = getPointLight(i, clusterInfo, positionWS);
    Light light = getLight(pointLight, normalWS, positionWS);
    // ...
}
#endif
```

### 10.3 阴影使用

```glsl
#include "ShadowSampler.glsl";

#ifdef CALCULATE_SHADOWS
vec4 shadowCoord = getShadowCoord(positionWS);
float shadowAtten = sampleShadowmap(shadowCoord);
// shadowAtten: 0=全阴影, 1=无阴影
#endif
```

---

## 11. 转换流程

### 11.1 步骤概览

```
1. 分析Unity Shader
   ├── 识别Shader类型（Unlit/Lit/PBR/Custom）
   ├── 提取Properties
   ├── 分析Pass结构
   └── 识别依赖的Unity功能

2. 创建LayaAir框架
   ├── 创建.shader文件
   ├── 编写Shader3D配置块
   ├── 定义uniformMap
   └── 定义shaderPass

3. 转换着色器代码
   ├── HLSL → GLSL语法
   ├── 替换内置变量
   ├── 替换内置函数
   └── 添加必要的include

4. 处理特殊功能
   ├── 光照计算
   ├── 阴影接收
   ├── 实例化支持
   └── 宏定义适配

5. 测试和调试
   ├── 编译错误修复
   ├── 视觉效果对比
   └── 性能测试
```

### 11.2 详细转换步骤

#### 步骤1：分析Unity Shader

```
检查项：
□ 是Surface Shader还是Vert/Frag Shader?
□ 有哪些Properties?（类型、默认值、范围）
□ 有几个Pass?各是什么LightMode?
□ 使用了哪些Unity内置功能?
□ 有哪些multi_compile/shader_feature?
```

#### 步骤2：创建uniformMap

```javascript
// Unity Properties 转换规则
_MainTex ("Texture", 2D) = "white" {}
// → u_MainTexture: { type: Texture2D, default: "white" }

_Color ("Color", Color) = (1,1,1,1)
// → u_Color: { type: Color, default: [1, 1, 1, 1] }

_Metallic ("Metallic", Range(0,1)) = 0.0
// → u_Metallic: { type: Float, default: 0.0, range: [0.0, 1.0] }

_BumpMap ("Normal Map", 2D) = "bump" {}
// → u_NormalTexture: { type: Texture2D, default: "normal" }
```

#### 步骤3：代码转换模板

**顶点着色器模板：**
```glsl
#define SHADER_NAME YourShaderName

#include "Math.glsl";
#include "Scene.glsl";
#include "Camera.glsl";
#include "Sprite3DVertex.glsl";
#include "VertexCommon.glsl";

// 如果是PBR/Lit shader
#include "PBRVertex.glsl";
// 或自定义varying
varying vec3 v_PositionWS;
varying vec3 v_NormalWS;
// ...

void main()
{
    Vertex vertex;
    getVertexParams(vertex);
    
    // 自定义顶点处理
    mat4 worldMat = getWorldMatrix();
    vec4 positionWS = worldMat * vec4(vertex.positionOS, 1.0);
    
    gl_Position = u_ViewProjection * positionWS;
    gl_Position = remapPositionZ(gl_Position);
    
    // 传递varying
    v_PositionWS = positionWS.xyz;
    // ...
}
```

**片元着色器模板：**
```glsl
#define SHADER_NAME YourShaderName

#include "Color.glsl";
#include "Scene.glsl";
#include "Camera.glsl";
#include "Sprite3DFrag.glsl";

// 声明uniform（来自uniformMap）
uniform sampler2D u_MainTexture;
uniform vec4 u_Color;
// ...

// 接收varying
varying vec3 v_PositionWS;
varying vec3 v_NormalWS;
// ...

void main()
{
    // 纹理采样
    vec4 texColor = texture2D(u_MainTexture, v_Texcoord0);
    
    // 自定义着色逻辑
    vec4 finalColor = texColor * u_Color;
    
    // 输出
    gl_FragColor = finalColor;
}
```

---

## 12. 完整示例

### 12.1 Unlit Shader 转换

**Unity版本：**
```hlsl
Shader "Custom/SimpleUnlit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 vertex : SV_POSITION; };
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                return col;
            }
            ENDCG
        }
    }
}
```

**LayaAir版本：**
```glsl
Shader3D Start
{
    name: "CustomSimpleUnlit",
    enableInstancing: true,
    shaderType: "D3",
    uniformMap: {
        u_MainTexture: { type: Texture2D, default: "white" },
        u_Color: { type: Color, default: [1, 1, 1, 1] },
        u_TilingOffset: { type: Vector4, default: [1, 1, 0, 0] }
    },
    shaderPass: [
        {
            pipeline: "Forward",
            VS: "UnlitVS",
            FS: "UnlitFS",
            renderState: {
                Cull: "Back",
                DepthWrite: true,
                DepthTest: "LEqual",
                Blend: "Disable"
            }
        }
    ]
}
Shader3D End

GLSL Start
#defineGLSL UnlitVS
    #define SHADER_NAME CustomSimpleUnlit
    
    #include "Camera.glsl";
    #include "Sprite3DVertex.glsl";
    #include "Sprite3DCommon.glsl";
    
    attribute vec4 a_Position;
    attribute vec2 a_Texcoord0;
    
    uniform vec4 u_TilingOffset;
    
    varying vec2 v_Texcoord0;
    
    void main()
    {
        mat4 worldMat = getWorldMatrix();
        vec4 positionWS = worldMat * a_Position;
        
        gl_Position = u_ViewProjection * positionWS;
        gl_Position = remapPositionZ(gl_Position);
        
        v_Texcoord0 = transformUV(a_Texcoord0, u_TilingOffset);
    }
#endGLSL

#defineGLSL UnlitFS
    #define SHADER_NAME CustomSimpleUnlit
    
    #include "Color.glsl";
    
    uniform sampler2D u_MainTexture;
    uniform vec4 u_Color;
    
    varying vec2 v_Texcoord0;
    
    void main()
    {
        vec4 texColor = texture2D(u_MainTexture, v_Texcoord0);
        
        #ifdef Gamma_u_MainTexture
        texColor = gammaToLinear(texColor);
        #endif
        
        gl_FragColor = texColor * u_Color;
    }
#endGLSL
GLSL End
```

### 12.2 PBR Shader 参数对照

| Unity Standard | LayaAir PBR | 类型 |
|----------------|-------------|------|
| `_Color` | `u_AlbedoColor` | Color |
| `_MainTex` | `u_AlbedoTexture` | Texture2D |
| `_Metallic` | `u_Metallic` | Float |
| `_Glossiness` | `u_Smoothness` | Float |
| `_MetallicGlossMap` | `u_MetallicGlossTexture` | Texture2D |
| `_BumpMap` | `u_NormalTexture` | Texture2D |
| `_BumpScale` | `u_NormalScale` | Float |
| `_OcclusionMap` | `u_OcclusionTexture` | Texture2D |
| `_OcclusionStrength` | `u_OcclusionStrength` | Float |
| `_EmissionColor` | `u_EmissionColor` | Color |
| `_EmissionMap` | `u_EmissionTexture` | Texture2D |
| `_Cutoff` | `u_AlphaTestValue` | Float |

---

## 13. 常见问题

### 13.1 编译错误

| 错误 | 原因 | 解决方案 |
|------|------|----------|
| `undefined variable` | 变量未声明 | 检查include或添加uniform声明 |
| `type mismatch` | 类型不匹配 | 检查HLSL→GLSL类型转换 |
| `undeclared identifier` | 函数未定义 | 检查是否缺少include |

### 13.2 视觉差异

| 问题 | 可能原因 | 解决方案 |
|------|----------|----------|
| 颜色偏暗 | Gamma校正 | 检查Gamma_xxx宏 |
| 法线反转 | 坐标系差异 | 法线Y分量取反 |
| UV错误 | UV坐标系差异 | 使用transformUV函数 |
| 光照错误 | 光照系统差异 | 使用LayaAir光照系统 |

### 13.3 不支持的功能

| Unity功能 | LayaAir替代方案 |
|-----------|-----------------|
| Surface Shader | 手动实现Vert/Frag |
| GrabPass | CommandBuffer |
| Geometry Shader | 无（Web不支持） |
| Tessellation | 有限支持 |
| ForwardAdd多Pass | Cluster光照单Pass |

---

## 附录：快速检查清单

### 转换前检查
- [ ] Unity Shader是否使用Surface Shader？
- [ ] 是否依赖GrabPass？
- [ ] 是否使用Geometry/Tessellation Shader？
- [ ] 有几个Pass？各是什么用途？

### 转换中检查
- [ ] 所有Properties已转换为uniformMap？
- [ ] 所有HLSL类型已转换为GLSL？
- [ ] 所有内置函数已替换？
- [ ] 所有内置变量已映射？
- [ ] 必要的include已添加？

### 转换后检查
- [ ] 编译无错误？
- [ ] 视觉效果正确？
- [ ] 光照正常？
- [ ] 阴影正常？
- [ ] Instance支持正常？

---

**文档版本**: 1.0  
**适用引擎**: LayaAir 3.x  
**最后更新**: 2024

---