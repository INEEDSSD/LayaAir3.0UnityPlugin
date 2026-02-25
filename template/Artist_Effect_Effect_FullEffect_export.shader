Shader3D Start
{
    type:Shader3D,
    name:Artist_Effect_Effect_FullEffect,
    enableInstancing:true,
    supportReflectionProbe:false,
    shaderType:Effect,
    uniformMap:{
    // Basic
    u_AlphaTestValue: { type: Float, default: 0.5, range: [0.0, 1.0] },
    u_TilingOffset: { type: Vector4, default: [1, 1, 0, 0] },
    // Shader Properties
    u_LayerType: { type: Float, default: 0.0 },
    u_AlbedoTexture: { type: Texture2D, default: "white", options: { define: "ALBEDOTEXTURE" } },
    u_DetailTex: { type: Texture2D, default: "white", options: { define: "DETAILTEX" } },
    u_DetailTex2: { type: Texture2D, default: "white", options: { define: "DETAILTEX2MAP" } },
    u_Scroll0X: { type: Float, default: 0.0 },
    u_Scroll0Y: { type: Float, default: 0.0 },
    u_Scroll1X: { type: Float, default: 0.0 },
    u_Scroll1Y: { type: Float, default: 0.0 },
    u_Scroll2X: { type: Float, default: 0.0 },
    u_Scroll2Y: { type: Float, default: 0.0 },
    u_WrapMode: { type: Float, default: 0.0 },
    u_Layer0UVMode: { type: Float, default: 0.0 },
    u_Layer1UVMode: { type: Float, default: 0.0 },
    u_Layer2UVMode: { type: Float, default: 0.0 },
    u_LayerColor: { type: Color, default: [1, 1, 1, 1] },
    u_LayerMultiplier: { type: Float, default: 1.0, range: [0.0, 10.0] },
    u_ROTATIONTEX: { type: Float, default: 0.0 },
    u_RotateCenterX: { type: Float, default: 0.5 },
    u_RotateCenterY: { type: Float, default: 0.5 },
    u_TranslationX: { type: Float, default: 0.0 },
    u_TranslationY: { type: Float, default: 0.0 },
    u_RotateAngle: { type: Float, default: 0.0, range: [-360.0, 360.0] },
    u_ROTATIONTEXTWO: { type: Float, default: 0.0 },
    u_RotateCenterX02: { type: Float, default: 0.5 },
    u_RotateCenterY02: { type: Float, default: 0.5 },
    u_TranslationX02: { type: Float, default: 0.0 },
    u_TranslationY02: { type: Float, default: 0.0 },
    u_RotateAngle02: { type: Float, default: 0.0, range: [-360.0, 360.0] },
    u_ROTATIONTEXTHREE: { type: Float, default: 0.0 },
    u_RotateCenterX03: { type: Float, default: 0.5 },
    u_RotateCenterY03: { type: Float, default: 0.5 },
    u_TranslationX03: { type: Float, default: 0.0 },
    u_TranslationY03: { type: Float, default: 0.0 },
    u_RotateAngle03: { type: Float, default: 0.0, range: [-360.0, 360.0] },
    u_ROTATIONTEXFOUR: { type: Float, default: 0.0 },
    u_RotateCenterX04: { type: Float, default: 0.5 },
    u_RotateCenterY04: { type: Float, default: 0.5 },
    u_TranslationX04: { type: Float, default: 0.0 },
    u_TranslationY04: { type: Float, default: 0.0 },
    u_RotateAngle04: { type: Float, default: 0.0, range: [-360.0, 360.0] },
    u_UseDistort0: { type: Float, default: 0.0 },
    u_DistortTex0: { type: Texture2D, default: "black", options: { define: "DISTORTTEX0MAP" } },
    u_Distort0UVMode: { type: Float, default: 0.0 },
    u_Distort0X: { type: Float, default: 0.0 },
    u_Distort0Y: { type: Float, default: 0.0 },
    u_DistortStrength0: { type: Float, default: 0.0, range: [0.0, 10.0] },
    u_UseLighting: { type: Float, default: 0.0 },
    u_EffectMainLightDir: { type: Vector4, default: [-0.5, 0.5, 1.0, 0.0] },
    u_EffectMainLightColor: { type: Color, default: [1, 1, 1, 1] },
    u_EffectMainLightIntensity: { type: Float, default: 5.0, range: [0.0, 5.0] },
    u_EffectAmbientLightColor: { type: Color, default: [0.5, 0.5, 0.5, 1] },
    u_EffectSSSColor: { type: Color, default: [0, 0, 0, 1] },
    u_UseRim: { type: Float, default: 0.0 },
    u_UseRimMap: { type: Float, default: 0.0 },
    u_RimMap: { type: Texture2D, default: "gray", options: { define: "RIMMAP" } },
    u_RimMaskMap: { type: Texture2D, default: "white", options: { define: "RIMMASKMAP" } },
    u_RimLevel: { type: Float, default: 1.0, range: [0.0, 5.0] },
    u_RimColor: { type: Color, default: [0.6, 0.8, 1, 1] },
    u_RimSharp: { type: Float, default: 2.0, range: [0.0, 20.0] },
    u_RimMode: { type: Float, default: 0.0 },
    u_GlowStrength: { type: Float, default: 1.0, range: [0.0, 1.0] },
    u_UseVertexOffset: { type: Float, default: 0.0 },
    u_VertexOffsetMode: { type: Float, default: 0.0 },
    u_VertexAmplitude: { type: Float, default: 0.0, range: [0.0, 10.0] },
    u_VertexAmplitudeTex: { type: Texture2D, default: "white", options: { define: "VERTEXAMPLITUDETEX" } },
    u_VertexAmplitudeTexScroll0X: { type: Float, default: 0.0 },
    u_VertexAmplitudeTexScroll0Y: { type: Float, default: 0.0 },
    u_VertexAmplitudeMaskTex: { type: Texture2D, default: "white", options: { define: "VERTEXAMPLITUDEMASKTEX" } },
    u_UseDissolve: { type: Float, default: 0.0 },
    u_DissolveTexture: { type: Texture2D, default: "white", options: { define: "DISSOLVETEXTURE" } },
    u_DissolveAmount: { type: Float, default: 0.0, range: [0.0, 1.0] },
    u_DissolveFadeRange: { type: Float, default: 0.1, range: [0.0, 1.0] },
    u_DissolveAmountTexture: { type: Texture2D, default: "black", options: { define: "DISSOLVEAMOUNTTEXTURE" } },
    u_UseDissolveAmountMinus: { type: Float, default: 0.0 },
    u_UseFadeEdge: { type: Float, default: 0.0 },
    u_FadeEdgeTexture: { type: Texture2D, default: "white", options: { define: "FADEEDGETEXTURE" } },
    u_FadeEdgeColor: { type: Color, default: [1, 1, 1, 1] },
    u_FadeEdgeStrength: { type: Float, default: 1.0, range: [0.0, 2.0] },
    u_FadeEdgeRange1: { type: Float, default: 0.1, range: [0.0, 0.5] },
    u_FadeEdgeRange2: { type: Float, default: 0.1, range: [0.0, 0.5] },
    u_FadeEdgeType: { type: Float, default: 0.0 },
    u_FadeGlowStrength: { type: Float, default: 1.0, range: [0.0, 10.0] },
    u_UseDissolveDistort: { type: Float, default: 0.0 },
    u_DissolveDistortTex: { type: Texture2D, default: "black", options: { define: "DISSOLVEDISTORTTEX" } },
    u_DissolveDistortUVMode: { type: Float, default: 0.0 },
    u_DissolveDistortX: { type: Float, default: 0.0 },
    u_DissolveDistortY: { type: Float, default: 0.0 },
    u_DissolveDistortStrength: { type: Float, default: 0.0, range: [0.0, 10.0] },
    u_UseGradientMap0: { type: Float, default: 0.0 },
    u_GradientMapTex0: { type: Texture2D, default: "white", options: { define: "GRADIENTMAPTEX0MAP" } },
    u_UseNormalMapForRim: { type: Float, default: 0.0 },
    u_NormalTexture: { type: Texture2D, default: "bump", options: { define: "NORMALTEXTURE" } },
    u_NormalMapStrength: { type: Float, default: 1.0, range: [0.0, 5.0] },
    u_UsePolar: { type: Float, default: 0.0 },
    u_PolarControl: { type: Vector4, default: [0.5, 0.5, 1, 1] },
    u_Alpha: { type: Float, default: 1.0, range: [0.0, 1.0] },
    u_UseCustomData: { type: Float, default: 0.0 },
    u_MainTex_OffsetX_Custom: { type: Float, default: 0.0 },
    u_MainTex_OffsetY_Custom: { type: Float, default: 0.0 },
    u_DetailTex_OffsetX_Custom: { type: Float, default: 0.0 },
    u_DetailTex_OffsetY_Custom: { type: Float, default: 0.0 },
    u_DissolveTex_OffsetX_Custom: { type: Float, default: 0.0 },
    u_DissolveTex_OffsetY_Custom: { type: Float, default: 0.0 },
    u_DissolveAmount_Custom: { type: Float, default: 0.0 },
    u_VertexAmplitudeX_Custom: { type: Float, default: 0.0 },
    u_VertexAmplitudeY_Custom: { type: Float, default: 0.0 },
    u_VertexAmplitude_Custom: { type: Float, default: 0.0 },
    // Texture Tiling/Offset
    u_AlbedoTexture_ST: { type: Vector4, default: [1, 1, 0, 0] },
    u_DetailTex_ST: { type: Vector4, default: [1, 1, 0, 0] },
    u_DetailTex2_ST: { type: Vector4, default: [1, 1, 0, 0] },
    u_DistortTex0_ST: { type: Vector4, default: [1, 1, 0, 0] },
    u_RimMap_ST: { type: Vector4, default: [1, 1, 0, 0] },
    u_RimMaskMap_ST: { type: Vector4, default: [1, 1, 0, 0] },
    u_VertexAmplitudeTex_ST: { type: Vector4, default: [1, 1, 0, 0] },
    u_VertexAmplitudeMaskTex_ST: { type: Vector4, default: [1, 1, 0, 0] },
    u_DissolveTexture_ST: { type: Vector4, default: [1, 1, 0, 0] },
    u_DissolveAmountTexture_ST: { type: Vector4, default: [1, 1, 0, 0] },
    u_FadeEdgeTexture_ST: { type: Vector4, default: [1, 1, 0, 0] },
    u_DissolveDistortTex_ST: { type: Vector4, default: [1, 1, 0, 0] },
    u_GradientMapTex0_ST: { type: Vector4, default: [1, 1, 0, 0] },
    u_NormalTexture_ST: { type: Vector4, default: [1, 1, 0, 0] },
    u_MainTex_ST: { type: Vector4, default: [1, 1, 0, 0] },
},
defines: {
RENDERMODE_MESH: { type: bool, default: false },
TINTCOLOR: { type: bool, default: true },
ADDTIVEFOG: { type: bool, default: true },
LAYERTYPE_ONE: { type: bool, default: false },
USERIM: { type: bool, default: false },
USERIMMAP: { type: bool, default: false },
USELIGHTING: { type: bool, default: false },
USEVERTEXOFFSET: { type: bool, default: false },
USEDISSOLVE: { type: bool, default: false },
USEFADEEDGE: { type: bool, default: false },
USEDISTORT0: { type: bool, default: false },
USECUSTOMDATA: { type: bool, default: false },
USEGRADIENTMAP0: { type: bool, default: false },
USENORMALMAPFORRIM: { type: bool, default: false },
USEDISSOLVEDISTORT: { type: bool, default: false },
ROTATIONTEX: { type: bool, default: false },
ROTATIONTEXTWO: { type: bool, default: false },
ROTATIONTEXTHREE: { type: bool, default: false },
ROTATIONTEXFOUR: { type: bool, default: false },
USEPOLAR: { type: bool, default: false },
LAYERTYPE_THREE: { type: bool, default: false },
LAYERTYPE_TWO: { type: bool, default: false },
WRAPMODE_CLAMP: { type: bool, default: false },
WRAPMODE_REPEAT: { type: bool, default: false },
WRAPMODE_DEFAULT: { type: bool, default: false },
},
attributeMap: {
a_DirectionTime: Vector4,
a_MeshPosition: Vector3,
a_MeshColor: Vector4,
a_MeshTextureCoordinate: Vector2,
a_ShapePositionStartLifeTime: Vector4,
a_CornerTextureCoordinate: Vector4,
a_StartColor: Vector4,
a_EndColor: Vector4,
a_StartSize: Vector3,
a_StartRotation0: Vector3,
a_StartSpeed: Float,
a_Random0: Vector4,
a_Random1: Vector4,
a_SimulationWorldPostion: Vector3,
a_SimulationWorldRotation: Vector4,
a_SimulationUV: Vector4
},
shaderPass:[
{
    pipeline:Forward,
    VS:Artist_Effect_Effect_FullEffectVS,
    FS:Artist_Effect_Effect_FullEffectFS
}
]
}
Shader3D End

GLSL Start
#defineGLSL Artist_Effect_Effect_FullEffectVS

#define SHADER_NAME Artist_Effect_Effect_FullEffect

#include "Camera.glsl";
#include "particleShuriKenSpriteVS.glsl";

#include "Math.glsl";
#include "MathGradient.glsl";
#include "Color.glsl";
#include "Scene.glsl";
#include "SceneFogInput.glsl";

varying vec4 v_Color;
varying vec4 v_ScreenPos;
varying vec4 v_Texcoord0;
varying vec3 v_Texcoord2;
varying vec3 v_Texcoord3;
varying vec4 v_Texcoord4;
varying vec4 v_Texcoord5;
varying vec4 v_Texcoord6;
varying vec4 v_Texcoord7;
varying vec3 v_Texcoord8;
varying vec4 v_Texcoord9;
varying vec2 v_TextureCoordinate;
#ifdef RENDERMODE_MESH
varying vec4 v_MeshColor;
#endif

vec2 TransformUV(vec2 texcoord, vec4 tilingOffset)
{
    vec2 transTexcoord = vec2(texcoord.x, texcoord.y - 1.0) * tilingOffset.xy + vec2(tilingOffset.z, -tilingOffset.w);
    transTexcoord.y += 1.0;
    return transTexcoord;
}

#if defined(VELOCITYOVERLIFETIMECONSTANT) || defined(VELOCITYOVERLIFETIMECURVE) || defined(VELOCITYOVERLIFETIMERANDOMCONSTANT) || defined(VELOCITYOVERLIFETIMERANDOMCURVE)
vec3 computeParticleLifeVelocity(in float normalizedAge)
{
    vec3 outLifeVelocity;
#ifdef VELOCITYOVERLIFETIMECONSTANT
    outLifeVelocity = u_VOLVelocityConst;
#endif
#ifdef VELOCITYOVERLIFETIMECURVE
    outLifeVelocity = vec3(getCurValueFromGradientFloat(u_VOLVelocityGradientX, normalizedAge),
    getCurValueFromGradientFloat(u_VOLVelocityGradientY, normalizedAge),
    getCurValueFromGradientFloat(u_VOLVelocityGradientZ, normalizedAge));
#endif
#ifdef VELOCITYOVERLIFETIMERANDOMCONSTANT
    outLifeVelocity = mix(u_VOLVelocityConst, u_VOLVelocityConstMax, vec3(a_Random1.y, a_Random1.z, a_Random1.w));
#endif
#ifdef VELOCITYOVERLIFETIMERANDOMCURVE
    outLifeVelocity = vec3(
    mix(getCurValueFromGradientFloat(u_VOLVelocityGradientX, normalizedAge), getCurValueFromGradientFloat(u_VOLVelocityGradientMaxX, normalizedAge), a_Random1.y),
    mix(getCurValueFromGradientFloat(u_VOLVelocityGradientY, normalizedAge), getCurValueFromGradientFloat(u_VOLVelocityGradientMaxY, normalizedAge), a_Random1.z),
    mix(getCurValueFromGradientFloat(u_VOLVelocityGradientZ, normalizedAge), getCurValueFromGradientFloat(u_VOLVelocityGradientMaxZ, normalizedAge), a_Random1.w));
#endif
    return outLifeVelocity;
}
#endif

vec3 getStartPosition(vec3 startVelocity, float age, vec3 dragData)
{
    vec3 startPosition;
    float lasttime = min(startVelocity.x / dragData.x, age);
    startPosition = lasttime * (startVelocity - 0.5 * dragData * lasttime);
    return startPosition;
}

vec3 computeParticlePosition(in vec3 startVelocity, in vec3 lifeVelocity, in float age, in float normalizedAge, vec3 gravityVelocity, vec4 worldRotation, vec3 dragData)
{
    vec3 startPosition = getStartPosition(startVelocity, age, dragData);
    vec3 lifePosition;
#if defined(VELOCITYOVERLIFETIMECONSTANT) || defined(VELOCITYOVERLIFETIMECURVE) || defined(VELOCITYOVERLIFETIMERANDOMCONSTANT) || defined(VELOCITYOVERLIFETIMERANDOMCURVE)
#ifdef VELOCITYOVERLIFETIMECONSTANT
    lifePosition = lifeVelocity * age;
#endif
#ifdef VELOCITYOVERLIFETIMECURVE
    lifePosition = vec3(getTotalValueFromGradientFloat(u_VOLVelocityGradientX, normalizedAge),
    getTotalValueFromGradientFloat(u_VOLVelocityGradientY, normalizedAge),
    getTotalValueFromGradientFloat(u_VOLVelocityGradientZ, normalizedAge));
#endif
#ifdef VELOCITYOVERLIFETIMERANDOMCONSTANT
    lifePosition = lifeVelocity * age;
#endif
#ifdef VELOCITYOVERLIFETIMERANDOMCURVE
    lifePosition = vec3(
    mix(getTotalValueFromGradientFloat(u_VOLVelocityGradientX, normalizedAge), getTotalValueFromGradientFloat(u_VOLVelocityGradientMaxX, normalizedAge), a_Random1.y),
    mix(getTotalValueFromGradientFloat(u_VOLVelocityGradientY, normalizedAge), getTotalValueFromGradientFloat(u_VOLVelocityGradientMaxY, normalizedAge), a_Random1.z),
    mix(getTotalValueFromGradientFloat(u_VOLVelocityGradientZ, normalizedAge), getTotalValueFromGradientFloat(u_VOLVelocityGradientMaxZ, normalizedAge), a_Random1.w));
#endif

    vec3 finalPosition;
    if (u_VOLSpaceType == 0) {
        if (u_ScalingMode != 2)
        finalPosition = rotationByQuaternions(u_PositionScale * (a_ShapePositionStartLifeTime.xyz + startPosition + lifePosition), worldRotation);
        else
        finalPosition = rotationByQuaternions(u_PositionScale * a_ShapePositionStartLifeTime.xyz + startPosition + lifePosition, worldRotation);
        } else {
            if (u_ScalingMode != 2)
            finalPosition = rotationByQuaternions(u_PositionScale * (a_ShapePositionStartLifeTime.xyz + startPosition), worldRotation) + lifePosition;
            else
            finalPosition = rotationByQuaternions(u_PositionScale * a_ShapePositionStartLifeTime.xyz + startPosition, worldRotation) + lifePosition;
        }
#else
        vec3 finalPosition;
        if (u_ScalingMode != 2)
        finalPosition = rotationByQuaternions(u_PositionScale * (a_ShapePositionStartLifeTime.xyz + startPosition), worldRotation);
        else
        finalPosition = rotationByQuaternions(u_PositionScale * a_ShapePositionStartLifeTime.xyz + startPosition, worldRotation);
#endif

        if (u_SimulationSpace == 0)
        finalPosition = finalPosition + a_SimulationWorldPostion;
        else if (u_SimulationSpace == 1)
        finalPosition = finalPosition + u_WorldPosition;

        finalPosition += 0.5 * gravityVelocity * age;
        return finalPosition;
    }

    vec4 computeParticleColor(in vec4 color, in float normalizedAge)
    {
#ifdef COLOROVERLIFETIME
        color *= getColorFromGradient(u_ColorOverLifeGradientAlphas, u_ColorOverLifeGradientColors, normalizedAge, u_ColorOverLifeGradientRanges);
#endif
#ifdef RANDOMCOLOROVERLIFETIME
        color *= mix(getColorFromGradient(u_ColorOverLifeGradientAlphas, u_ColorOverLifeGradientColors, normalizedAge, u_ColorOverLifeGradientRanges),
        getColorFromGradient(u_MaxColorOverLifeGradientAlphas, u_MaxColorOverLifeGradientColors, normalizedAge, u_MaxColorOverLifeGradientRanges), a_Random0.y);
#endif
        return color;
    }

    vec2 computeParticleSizeBillbard(in vec2 size, in float normalizedAge)
    {
#ifdef SIZEOVERLIFETIMECURVE
        size *= getCurValueFromGradientFloat(u_SOLSizeGradient, normalizedAge);
#endif
#ifdef SIZEOVERLIFETIMERANDOMCURVES
        size *= mix(getCurValueFromGradientFloat(u_SOLSizeGradient, normalizedAge), getCurValueFromGradientFloat(u_SOLSizeGradientMax, normalizedAge), a_Random0.z);
#endif
#ifdef SIZEOVERLIFETIMECURVESEPERATE
        size *= vec2(getCurValueFromGradientFloat(u_SOLSizeGradientX, normalizedAge), getCurValueFromGradientFloat(u_SOLSizeGradientY, normalizedAge));
#endif
#ifdef SIZEOVERLIFETIMERANDOMCURVESSEPERATE
        size *= vec2(mix(getCurValueFromGradientFloat(u_SOLSizeGradientX, normalizedAge), getCurValueFromGradientFloat(u_SOLSizeGradientMaxX, normalizedAge), a_Random0.z),
        mix(getCurValueFromGradientFloat(u_SOLSizeGradientY, normalizedAge), getCurValueFromGradientFloat(u_SOLSizeGradientMaxY, normalizedAge), a_Random0.z));
#endif
        return size;
    }

#ifdef RENDERMODE_MESH
    vec3 computeParticleSizeMesh(in vec3 size, in float normalizedAge)
    {
#ifdef SIZEOVERLIFETIMECURVE
        size *= getCurValueFromGradientFloat(u_SOLSizeGradient, normalizedAge);
#endif
#ifdef SIZEOVERLIFETIMERANDOMCURVES
        size *= mix(getCurValueFromGradientFloat(u_SOLSizeGradient, normalizedAge), getCurValueFromGradientFloat(u_SOLSizeGradientMax, normalizedAge), a_Random0.z);
#endif
#ifdef SIZEOVERLIFETIMECURVESEPERATE
        size *= vec3(getCurValueFromGradientFloat(u_SOLSizeGradientX, normalizedAge), getCurValueFromGradientFloat(u_SOLSizeGradientY, normalizedAge), getCurValueFromGradientFloat(u_SOLSizeGradientZ, normalizedAge));
#endif
#ifdef SIZEOVERLIFETIMERANDOMCURVESSEPERATE
        size *= vec3(mix(getCurValueFromGradientFloat(u_SOLSizeGradientX, normalizedAge), getCurValueFromGradientFloat(u_SOLSizeGradientMaxX, normalizedAge), a_Random0.z),
        mix(getCurValueFromGradientFloat(u_SOLSizeGradientY, normalizedAge), getCurValueFromGradientFloat(u_SOLSizeGradientMaxY, normalizedAge), a_Random0.z),
        mix(getCurValueFromGradientFloat(u_SOLSizeGradientZ, normalizedAge), getCurValueFromGradientFloat(u_SOLSizeGradientMaxZ, normalizedAge), a_Random0.z));
#endif
        return size;
    }
#endif

    float computeParticleRotationFloat(in float rotation, in float age, in float normalizedAge)
    {
#ifdef ROTATIONOVERLIFETIME
#ifdef ROTATIONOVERLIFETIMECONSTANT
        rotation += u_ROLAngularVelocityConst * age;
#endif
#ifdef ROTATIONOVERLIFETIMECURVE
        rotation += getTotalValueFromGradientFloat(u_ROLAngularVelocityGradient, normalizedAge);
#endif
#ifdef ROTATIONOVERLIFETIMERANDOMCONSTANTS
        rotation += mix(u_ROLAngularVelocityConst, u_ROLAngularVelocityConstMax, a_Random0.w) * age;
#endif
#ifdef ROTATIONOVERLIFETIMERANDOMCURVES
        rotation += mix(getTotalValueFromGradientFloat(u_ROLAngularVelocityGradient, normalizedAge), getTotalValueFromGradientFloat(u_ROLAngularVelocityGradientMax, normalizedAge), a_Random0.w);
#endif
#endif
#ifdef ROTATIONOVERLIFETIMESEPERATE
#ifdef ROTATIONOVERLIFETIMECONSTANT
        rotation += u_ROLAngularVelocityConstSeprarate.z * age;
#endif
#ifdef ROTATIONOVERLIFETIMECURVE
        rotation += getTotalValueFromGradientFloat(u_ROLAngularVelocityGradientZ, normalizedAge);
#endif
#ifdef ROTATIONOVERLIFETIMERANDOMCONSTANTS
        rotation += mix(u_ROLAngularVelocityConstSeprarate.z, u_ROLAngularVelocityConstMaxSeprarate.z, a_Random0.w) * age;
#endif
#ifdef ROTATIONOVERLIFETIMERANDOMCURVES
        rotation += mix(getTotalValueFromGradientFloat(u_ROLAngularVelocityGradientZ, normalizedAge), getTotalValueFromGradientFloat(u_ROLAngularVelocityGradientMaxZ, normalizedAge), a_Random0.w);
#endif
#endif
        return rotation;
    }

    vec2 computeParticleUV(in vec2 uv, in float normalizedAge)
    {
#ifdef TEXTURESHEETANIMATIONCURVE
        float cycleNormalizedAge = normalizedAge * u_TSACycles;
        float frame = getFrameFromGradient(u_TSAGradientUVs, cycleNormalizedAge - floor(cycleNormalizedAge));
        float totalULength = frame * u_TSASubUVLength.x;
        float floorTotalULength = floor(totalULength);
        uv.x += totalULength - floorTotalULength;
        uv.y += floorTotalULength * u_TSASubUVLength.y;
#endif
#ifdef TEXTURESHEETANIMATIONRANDOMCURVE
        float cycleNormalizedAge = normalizedAge * u_TSACycles;
        float uvNormalizedAge = cycleNormalizedAge - floor(cycleNormalizedAge);
        float frame = floor(mix(getFrameFromGradient(u_TSAGradientUVs, uvNormalizedAge), getFrameFromGradient(u_TSAMaxGradientUVs, uvNormalizedAge), a_Random1.x));
        float totalULength = frame * u_TSASubUVLength.x;
        float floorTotalULength = floor(totalULength);
        uv.x += totalULength - floorTotalULength;
        uv.y += floorTotalULength * u_TSASubUVLength.y;
#endif
        return uv;
    }

    void main()
    {

        // Particle system logic - calculate particle center position
        float age = u_CurrentTime - a_DirectionTime.w;
        float normalizedAge = age / a_ShapePositionStartLifeTime.w;
        vec3 lifeVelocity;

        if (normalizedAge < 1.0)
        {
#ifdef RENDERMODE_MESH
            // ===== Mesh Rendering Mode =====
            // Calculate particle center position first
            vec3 startVelocity = a_DirectionTime.xyz * a_StartSpeed;

#if defined(VELOCITYOVERLIFETIMECONSTANT) || defined(VELOCITYOVERLIFETIMECURVE) || defined(VELOCITYOVERLIFETIMERANDOMCONSTANT) || defined(VELOCITYOVERLIFETIMERANDOMCURVE)
            lifeVelocity = computeParticleLifeVelocity(normalizedAge);
#endif

            vec3 gravityVelocity = u_Gravity * age;
            vec4 worldRotation;

            if (u_SimulationSpace == 0)
            worldRotation = a_SimulationWorldRotation;
            else
            worldRotation = u_WorldRotation;

            vec3 dragData = a_DirectionTime.xyz * mix(u_DragConstanct.x, u_DragConstanct.y, a_Random0.x);
            vec3 center = computeParticlePosition(startVelocity, lifeVelocity, age, normalizedAge, gravityVelocity, worldRotation, dragData);

            // Apply mesh vertex with size scaling and rotation
            vec3 size = computeParticleSizeMesh(a_StartSize, normalizedAge);

            // Apply rotation based on particle system settings
            // Use rotationByAxis for 2D rotation (simpler, compatible with most use cases)
            if (u_SimulationSpace == 0)
            center += u_SizeScale * rotationByAxis(a_MeshPosition * size, vec3(0.0, -1.0, 0.0), a_StartRotation0.x);
            else if (u_SimulationSpace == 1)
            center += rotationByQuaternions(u_SizeScale * rotationByAxis(a_MeshPosition * size, vec3(0.0, -1.0, 0.0), a_StartRotation0.x), worldRotation);

            // Transform to clip space
            gl_Position = u_Projection * u_View * vec4(center, 1.0);

            // Pass mesh color through separate varying
            v_MeshColor = a_MeshColor;

            // Particle color for additional effects
            vec4 startcolor = gammaToLinear(a_StartColor);
            v_Color = computeParticleColor(startcolor, normalizedAge);

            // UV calculation for mesh mode
            vec2 simulateUV = a_SimulationUV.xy + a_MeshTextureCoordinate * a_SimulationUV.zw;
            v_TextureCoordinate = computeParticleUV(simulateUV, normalizedAge);

            // Screen position
            v_ScreenPos.xy = (gl_Position.xy + gl_Position.w) * 0.5;
            v_ScreenPos.zw = gl_Position.zw;

#else
            // ===== Billboard Particle Mode =====
            vec3 startVelocity = a_DirectionTime.xyz * a_StartSpeed;

#if defined(VELOCITYOVERLIFETIMECONSTANT) || defined(VELOCITYOVERLIFETIMECURVE) || defined(VELOCITYOVERLIFETIMERANDOMCONSTANT) || defined(VELOCITYOVERLIFETIMERANDOMCURVE)
            lifeVelocity = computeParticleLifeVelocity(normalizedAge);
#endif

            vec3 gravityVelocity = u_Gravity * age;
            vec4 worldRotation;

            if (u_SimulationSpace == 0)
            worldRotation = a_SimulationWorldRotation;
            else
            worldRotation = u_WorldRotation;

            vec3 dragData = a_DirectionTime.xyz * mix(u_DragConstanct.x, u_DragConstanct.y, a_Random0.x);
            vec3 center = computeParticlePosition(startVelocity, lifeVelocity, age, normalizedAge, gravityVelocity, worldRotation, dragData);

            // Default Billboard mode
            vec2 corner = a_CornerTextureCoordinate.xy;
            vec3 cameraUpVector = normalize(u_CameraUp);
            vec3 sideVector = normalize(cross(u_CameraDirection, cameraUpVector));
            vec3 upVector = normalize(cross(sideVector, u_CameraDirection));
            corner *= computeParticleSizeBillbard(a_StartSize.xy, normalizedAge);
            float c = cos(a_StartRotation0.x);
            float s = sin(a_StartRotation0.x);
            mat2 rotation = mat2(c, -s, s, c);
            corner = rotation * corner;
            center += u_SizeScale.xzy * (corner.x * sideVector + corner.y * upVector);

            // Final position
            gl_Position = u_Projection * u_View * vec4(center, 1.0);

            // Screen position
            v_ScreenPos.xy = (gl_Position.xy + gl_Position.w) * 0.5;
            v_ScreenPos.zw = gl_Position.zw;

            // Particle color
            vec4 startcolor = gammaToLinear(a_StartColor);
            v_Color = computeParticleColor(startcolor, normalizedAge);

            // UV calculation
            vec2 simulateUV = a_SimulationUV.xy + a_CornerTextureCoordinate.zw * a_SimulationUV.zw;
            v_TextureCoordinate = computeParticleUV(simulateUV, normalizedAge);
#endif
        }
        else
        {
            // Particle is dead, move it out of view
            gl_Position = vec4(2.0, 2.0, 2.0, 1.0);
        }

        gl_Position = remapPositionZ(gl_Position);

#ifdef FOG
        FogHandle(gl_Position.z);
#endif

        // 特效相关的varying赋值
        v_Texcoord0.xy = v_TextureCoordinate;
        v_Texcoord0.zw = gl_Position.xy; // 默认屏幕坐标
        v_ScreenPos.xy = (gl_Position.xy + gl_Position.w) * 0.5;
        v_ScreenPos.zw = gl_Position.zw;
        v_Texcoord0.xy = v_TextureCoordinate;
        v_Texcoord0.zw = fract(vec2(u_Scroll0X, u_Scroll0Y) * u_Time);
#if defined(LAYERTYPE_TWO) || defined(LAYERTYPE_THREE)
        v_Texcoord4.xy = fract(vec2(u_Scroll1X, u_Scroll1Y) * u_Time);
#endif
#ifdef LAYERTYPE_THREE
        v_Texcoord4.zw = fract(vec2(u_Scroll2X, u_Scroll2Y) * u_Time);
#endif
#ifdef USEDISTORT0
        v_Texcoord6.xy = fract(vec2(u_Distort0X, u_Distort0Y) * u_Time);
#endif
#ifdef USEDISSOLVEDISTORT
        v_Texcoord6.zw = fract(vec2(u_DissolveDistortX, u_DissolveDistortY) * u_Time);
#endif
#ifdef ROTATIONTEX
        float rad1 = u_RotateAngle * 0.01745329;
        v_Texcoord9.x = cos(rad1);
        v_Texcoord9.y = sin(rad1);
#endif
#ifdef ROTATIONTEXTWO
        float rad2 = u_RotateAngle02 * 0.01745329;
        v_Texcoord9.z = cos(rad2);
        v_Texcoord9.w = sin(rad2);
#endif

    }
#endGLSL

#defineGLSL Artist_Effect_Effect_FullEffectFS

#define SHADER_NAME Artist_Effect_Effect_FullEffect

#include "Scene.glsl";
#include "SceneFog.glsl";
#include "Color.glsl";
#include "Camera.glsl";

    varying vec4 v_Color;
    varying vec4 v_ScreenPos;
    varying vec4 v_Texcoord0;
    varying vec3 v_Texcoord2;
    varying vec3 v_Texcoord3;
    varying vec4 v_Texcoord4;
    varying vec4 v_Texcoord5;
    varying vec4 v_Texcoord6;
    varying vec4 v_Texcoord7;
    varying vec3 v_Texcoord8;
    varying vec4 v_Texcoord9;
    varying vec2 v_TextureCoordinate;
#ifdef RENDERMODE_MESH
    varying vec4 v_MeshColor;
#endif

    vec2 PolarCoordinates(vec2 UV, vec2 Center, float RadialScale, float LengthScale)
    {
        // 将UV坐标平移到以Center为中心
        vec2 delta = UV - Center;
        // 计算半径（距离）
        float radius = length(delta) * 2.0 * RadialScale;
        // 计算角度（使用atan2函数，范围[-π, π]）
        // 将角度映射到[0.0, 1.0]范围，并应用缩放
        float angle = atan(delta.x, delta.y) * (1.0 / 6.28318530718) * LengthScale;
        return vec2(radius, angle);
    }

    vec2 RotateUV(vec2 uv, float centerX, float centerY, float angle, float transX, float transY)
    {
        // 1. 角度转弧度
        float rad = angle * 3.1415926 / 180.0;
        float s, c;
        s = sin(rad); c = cos(rad); // 同时计算 sin 和 cos，性能更好

        //  去中心化 + 平移
        vec2 center = vec2(centerX, centerY);
        vec2 delta = uv - center + vec2(transX, transY);
        mat2 rotMat = mat2(c, -s, s, c);
        vec2 rotated = (delta * rotMat);
        return rotated + center;
    }

    vec2 LowCostRotate(vec2 uv, float centerX, float centerY, float c, float s, float transX, float transY) {
        vec2 center = vec2(centerX, centerY);
        vec2 delta = uv - center + vec2(transX, transY);

        // 仅仅是 4 次乘法和 2 次加法
        mat2 rotMat = mat2(c, -s, s, c);
        vec2 rotated = (delta * rotMat);
        return rotated + center;
    }

    void main()
    {

        vec2 screenUV = v_ScreenPos.xy / v_ScreenPos.w;

#ifdef USEPOLAR
        v_Texcoord0.xy = PolarCoordinates(v_Texcoord0.xy,u_PolarControl.xy,u_PolarControl.z,u_PolarControl.w);
#endif

        // 1. Distortion
#ifdef USEDISSOLVE
        vec2 dissolveUV = vec2(u_DissolveTexture_ST.xy * v_Texcoord0.xy);
#ifdef USEDISSOLVEDISTORT
        dissolveUV += texture2D(u_DissolveDistortTex, v_Texcoord6.zw + ((u_DissolveDistortUVMode == 0.0 ? v_Texcoord0.xy : screenUV).xy * u_DissolveDistortTex_ST.xy + u_DissolveDistortTex_ST.zw)).xy * u_DissolveDistortStrength;
#endif
#ifdef USECUSTOMDATA
        float dissolveValue = texture2D(u_DissolveTexture, (dissolveUV + vec2(u_DissolveTex_OffsetX_Custom < 1.0 ? u_DissolveTexture_ST.z : v_Texcoord7[u_DissolveTex_OffsetX_Custom - 1.0],
        u_DissolveTex_OffsetY_Custom < 1.0 ? u_DissolveTexture_ST.w : v_Texcoord7[u_DissolveTex_OffsetY_Custom - 1.0]))).r;
        float dissoveAmount = u_DissolveAmount_Custom < 1.0 ? u_DissolveAmount : v_Texcoord7[u_DissolveAmount_Custom - 1.0];
#else
        float dissolveValue = texture2D(u_DissolveTexture, (dissolveUV + u_DissolveTexture_ST.zw)).r;
        float dissoveAmount = u_DissolveAmount;
#endif
#ifdef ROTATIONTEXFOUR
        vec2 uvFour = RotateUV(v_Texcoord0.xy, u_RotateCenterX04, u_RotateCenterY04, u_RotateAngle04, u_TranslationX04, u_TranslationY04);
        dissoveAmount += texture2D(u_DissolveAmountTexture, (u_DissolveAmountTexture_ST.xy * uvFour + u_DissolveAmountTexture_ST.zw)).r;
#else
        dissoveAmount += texture2D(u_DissolveAmountTexture, (u_DissolveAmountTexture_ST.xy * v_Texcoord0.xy + u_DissolveAmountTexture_ST.zw)).r;
#endif
        dissoveAmount = min(1.001, dissoveAmount);

#ifdef USEFADEEDGE
        float fadeValue = dissoveAmount;
        float fadeFactor = dissolveValue;
        if ((fadeFactor - fadeValue) < 0.0) { discard; };
        float fadeAlpha = 0.0;
        vec4 fadeEdgeTex = texture2D(u_FadeEdgeTexture, (v_Texcoord0.xy * u_FadeEdgeTexture_ST.xy + u_FadeEdgeTexture_ST.zw));
        vec3 fadeEdgeColor = fadeEdgeTex.rgb * u_FadeEdgeColor * u_FadeEdgeStrength;
        float fadeEdgeValue1 = u_FadeEdgeRange1 + fadeValue;
        float fadeEdgeValue2 = fadeEdgeValue1 + u_FadeEdgeRange2;
        float range = fadeEdgeValue2 - fadeEdgeValue1;
        fadeAlpha = clamp((fadeEdgeValue2 - fadeFactor) / max(0.0001, range), 0.0, 1.0);
#else
        float dissoveFactor = 1.0;
        if (dissoveAmount > 0.0 || u_UseDissolveAmountMinus > 0.0)
        {
            if (u_DissolveFadeRange == 0.0)
            if ((dissolveValue - dissoveAmount) < 0.0) { discard; };
            else
            {
                dissoveFactor = (1.0 - step(dissolveValue, dissoveAmount)) * smoothstep(dissoveAmount, dissoveAmount + u_DissolveFadeRange , dissolveValue);
                //dissoveFactor = (1.0 - step(dissolveValue, dissoveAmount)) * ((dissolveValue - dissoveAmount) / u_DissolveFadeRange);
            }
        }
#endif
#endif

        // Layer
        // sample the texture
#ifdef USECUSTOMDATA
        vec2 mainUV = vec2(v_Texcoord0.zw + ((u_Layer0UVMode == 0.0 ? v_Texcoord0.xy : screenUV) * u_TilingOffset.xy + vec2(u_MainTex_OffsetX_Custom < 1.0 ? u_TilingOffset.z : v_Texcoord7[u_MainTex_OffsetX_Custom - 1.0],
        u_MainTex_OffsetY_Custom < 1.0 ? u_TilingOffset.w : v_Texcoord7[u_MainTex_OffsetY_Custom - 1.0])));
#else
        vec2 mainUV = vec2(v_Texcoord0.zw + (u_TilingOffset.xy * (u_Layer0UVMode == 0.0 ? v_Texcoord0.xy : screenUV) + u_TilingOffset.zw));
#endif

#ifdef USEDISTORT0
        mainUV += texture2D(u_DistortTex0, v_Texcoord6.xy + ((u_Distort0UVMode == 0.0 ? v_Texcoord0.xy : screenUV).xy * u_DistortTex0_ST.xy + u_DistortTex0_ST.zw)).xy * u_DistortStrength0;
#endif
        //图片重复方式的算法
#ifdef WRAPMODE_DEFAULT
        mainUV = mainUV;
#elif defined(WRAPMODE_CLAMP)
        mainUV = clamp(mainUV, 0.0, 1.0);
#elif defined(WRAPMODE_REPEAT)
        mainUV = fract(mainUV);
#endif

#ifdef ROTATIONTEX
        mainUV = LowCostRotate(mainUV, u_RotateCenterX, u_RotateCenterY,
        v_Texcoord9.x, v_Texcoord9.y,
        u_TranslationX, u_TranslationY);
#endif

        vec4 color = texture2D(u_AlbedoTexture, mainUV);

#ifdef USEGRADIENTMAP0
        color = texture2D(u_GradientMapTex0, (vec2(color.r, 0.5) * u_GradientMapTex0_ST.xy + u_GradientMapTex0_ST.zw));
#endif

#if defined(LAYERTYPE_TWO) || defined(LAYERTYPE_THREE)
#ifdef USECUSTOMDATA
        vec2 detailUV = vec2(v_Texcoord4.xy + (u_DetailTex_ST.xy * (u_Layer1UVMode == 0.0 ? v_Texcoord0.xy : screenUV) + vec2(u_DetailTex_OffsetX_Custom < 0.9 ? u_DetailTex_ST.z : v_Texcoord7[u_DetailTex_OffsetX_Custom - 1.0],
        u_DetailTex_OffsetY_Custom < 0.001 ? u_DetailTex_ST.w : v_Texcoord7[u_DetailTex_OffsetY_Custom - 1.0])));
#else
        vec2 detailUV = vec2(v_Texcoord4.xy + (u_DetailTex_ST.xy * (u_Layer1UVMode == 0.0 ? v_Texcoord0.xy : screenUV) + u_DetailTex_ST.zw));
#endif

#ifdef ROTATIONTEXTWO
        detailUV = LowCostRotate(detailUV, u_RotateCenterX02, u_RotateCenterY02, v_Texcoord9.z, v_Texcoord9.w, u_TranslationX02, u_TranslationY02);
#endif
        vec4 detailTex = texture2D(u_DetailTex, detailUV);
        color *= detailTex;
#endif

#ifdef LAYERTYPE_THREE
#ifdef ROTATIONTEXTHREE
        v_Texcoord4.zw = RotateUV(v_Texcoord4.zw, u_RotateCenterX03, u_RotateCenterY03, u_RotateAngle03, u_TranslationX03, u_TranslationY03);
#endif
        vec4 detailTex2 = texture2D(u_DetailTex2, v_Texcoord4.zw + ((u_Layer2UVMode == 0.0 ? v_Texcoord0.xy : screenUV) * u_DetailTex2_ST.xy + u_DetailTex2_ST.zw));
        color *= detailTex2;
#endif
        color *= u_LayerColor * u_LayerMultiplier * u_Alpha;

#ifdef USEDISSOLVE
#ifdef USEFADEEDGE
        if (u_FadeEdgeType == 0.0)
        color.xyz = mix(color.xyz, fadeEdgeColor, fadeAlpha);
        else
        color.xyz = color.xyz + fadeAlpha * fadeEdgeColor;
#else
        color.a *= dissoveFactor;
#endif
#endif
        color.rgb *= color.a;

        //3.0 light
#ifdef USELIGHTING
        vec3 normalWorld = normalize(v_Texcoord3);
        vec3 effectMainLightDir = normalize(u_EffectMainLightDir);
        float snl = dot(normalWorld, effectMainLightDir);
        float w = fwidth(snl) * 2.0;
        vec3 spec = mix(0.0, 1.0, smoothstep(-w, w * 2.0, snl + u_EffectMainLightDir.w - 1.0)) * step(0.0001, u_EffectMainLightDir.w);
        color.rgb *= spec * u_EffectMainLightIntensity * u_EffectMainLightColor * max((u_EffectSSSColor + snl) / (u_EffectSSSColor + 1.0), 0.0) + u_EffectAmbientLightColor;

#endif

        // Rim
#ifdef USERIM
#ifdef USENORMALMAPFORRIM
        vec3 tangent = v_Texcoord8.xyz;
        vec3 binormal = v_Texcoord2.xyz;
        vec3 normal = v_Texcoord3.xyz;
        vec3 normalMapVal = texture2D(u_NormalTexture, v_Texcoord0.xy * u_NormalTexture_ST.xy + u_NormalTexture_ST.zw).xyz;
        normalMapVal.xy = (normalMapVal * 2.0 - 1.0) * u_NormalMapStrength;
        normalMapVal.z = sqrt(1.0 - clamp(dot(normalMapVal.xy, normalMapVal.xy), 0.0, 1.0));
        vec3 normalWorldRim = normalize(normalMapVal.x * tangent + normalMapVal.y * binormal + normalMapVal.z * normal);
        vec3 normalView = normalize((mat3(transpose); (inverse(u_View)) * normalWorldRim));
#else
        vec3 normalView = normalize(v_Texcoord2);
#endif

        float rimMask = texture2D(u_RimMaskMap, v_Texcoord0.xy).r;
#ifdef USERIMMAP
        vec2 rimUV = normalView * 0.5 + 0.5;
        vec4 rimColor = texture2D(u_RimMap, rimUV);
        float rimFactor = (rimColor.x + rimColor.y + rimColor.z) * rimColor.w * u_RimLevel * rimMask;
        vec3 rimCol = rimColor.rgb;
#else
        float rimFactor = pow(length(normalView.xy), u_RimSharp) * 2.0 * u_RimLevel * rimMask;
        vec3 rimCol = u_RimColor.rgb;
#endif

        if (u_RimMode == 1.0)
        color.xyz *= rimCol * rimFactor;
        else if (u_RimMode == 2.0)
        {
            color.xyz *= rimCol * rimFactor;
            color.w *= rimFactor;
        }
        else if (u_RimMode == 3.0)
        color.xyz += rimCol * rimFactor * color.w;
        else
        color.xyz += rimCol * rimFactor;
#endif
        color *= v_Color;
        color.rgb *= v_Color.a;
        color.a = clamp(color.a, 0.0, 1.0);
        gl_FragColor = color;

#ifdef RENDERMODE_MESH
        // Multiply by mesh vertex color in mesh mode
        gl_FragColor *= v_MeshColor;
#endif

#ifdef FOG
        gl_FragColor.rgb = scenUnlitFog(gl_FragColor.rgb);
#endif

        gl_FragColor = outputTransform(gl_FragColor);
    }
#endGLSL

GLSL End
