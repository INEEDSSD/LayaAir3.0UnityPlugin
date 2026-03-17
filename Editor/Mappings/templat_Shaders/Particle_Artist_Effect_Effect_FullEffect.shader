Shader3D Start
{
    type:Shader3D,
    name: "Artist_Effect_Effect_FullEffect",
    enableInstancing:true,
    supportReflectionProbe:false,
    shaderType:Effect,
    uniformMap:{
		LayerType: { type: "Int", default: 0, index: -1, alias: "层数量", catalog: "基本设置", catalogOrder: 0, inspector: "RadioGroup", options: { members: ["EFFECT_LAYER_ONE", "EFFECT_LAYER_TWO", "EFFECT_LAYER_THREE"] } },
        WrapMode: { type: "Int", default: 0, index: -0.5, alias: "贴图Wrap模式", catalog: "基本设置", inspector: "RadioGroup", options: { members: ["EFFECT_WRAPMODE_DEFAULT", "EFFECT_WRAPMODE_CLAMP", "EFFECT_WRAPMODE_REPEAT"] } },
        u_PolarControl: { type: "Vector4", default: [0.5, 0.5, 1, 1], index: -0.3, alias: "极坐标控制(中心XY,径向缩放,角度缩放)", catalog: "基本设置", hidden: "!data.EFFECT_POLAR" },
        u_texture: { type: "Texture2D", index: 0, alias: "第一层颜色贴图(rgb),透明度(a)", catalog: "基本设置" },
        u_TilingOffset: { type: "Vector4", default: [1, 1, 0, 0], index: 1, alias: "第一层TilingOffset", catalog: "基本设置" },
        u_Scroll0: { type: "Vector2", default: [0, 0], index: 2, alias: "第一层UV滚动速度(U,V)", catalog: "基本设置" },
        u_Layer0UVMode: { type: "Int", default: 0, index: 2.5, alias: "第一层UV模式", catalog: "基本设置", enumSource: [{name: "Model", value: 0}, {name: "Screen", value: 1}] },
        u_RotateCenter: { type: "Vector2", default: [0.5, 0.5], index: 4, alias: "第一层旋转中心(X,Y)", catalog: "基本设置", hidden: "!data.EFFECT_ROTATION" },
        u_RotateAngle: { type: "Float", default: 0, index: 5, alias: "第一层旋转角度", catalog: "基本设置", hidden: "!data.EFFECT_ROTATION" },
        u_Translation: { type: "Vector2", default: [0, 0], index: 6, alias: "第一层旋转位置(X,Y)", catalog: "基本设置", hidden: "!data.EFFECT_ROTATION" },
        u_DistortTex: { type: "Texture2D", default: "black", index: 8, alias: "第一层扰动贴图(rg)", catalog: "基本设置", hidden: "!data.EFFECT_DISTORT" },
        u_DistortTex_ST: { type: "Vector4", default: [1, 1, 0, 0], index: 9, alias: "扰动TilingOffset", catalog: "基本设置", hidden: "!data.EFFECT_DISTORT" },
        u_DistortScroll: { type: "Vector2", default: [0, 0], index: 10, alias: "扰动UV滚动速度(U,V)", catalog: "基本设置", hidden: "!data.EFFECT_DISTORT" },
        u_DistortStrength: { type: "Float", default: 0, index: 11, alias: "扰动强度", catalog: "基本设置", hidden: "!data.EFFECT_DISTORT" },
        u_Distort0UVMode: { type: "Int", default: 0, index: 11.5, alias: "扰动UV模式", catalog: "基本设置", hidden: "!data.EFFECT_DISTORT", enumSource: [{name: "Model", value: 0}, {name: "Screen", value: 1}] },
        u_GradientMapTex0: { type: "Texture2D", default: "white", index: 12, alias: "渐变映射贴图", catalog: "基本设置", hidden: "!data.EFFECT_GRADIENT_MAP" },
        u_GradientMapTex0_ST: { type: "Vector4", default: [1, 1, 0, 0], index: 12.5, alias: "渐变映射TilingOffset", catalog: "基本设置", hidden: "!data.EFFECT_GRADIENT_MAP" },
        u_DetailTex: { type: "Texture2D", default: "white", index: 30, alias: "第二层颜色贴图(rgb),透明度(a)", catalog: "基本设置", hidden: "!data.EFFECT_LAYER_TWO && !data.EFFECT_LAYER_THREE" },
        u_DetailTex_ST: { type: "Vector4", default: [1, 1, 0, 0], index: 31, alias: "第二层TilingOffset", catalog: "基本设置", hidden: "!data.EFFECT_LAYER_TWO && !data.EFFECT_LAYER_THREE" },
        u_Scroll1: { type: "Vector2", default: [0, 0], index: 32, alias: "第二层UV滚动速度(U,V)", catalog: "基本设置", hidden: "!data.EFFECT_LAYER_TWO && !data.EFFECT_LAYER_THREE" },
        u_Layer1UVMode: { type: "Int", default: 0, index: 32.5, alias: "第二层UV模式", catalog: "基本设置", hidden: "!data.EFFECT_LAYER_TWO && !data.EFFECT_LAYER_THREE", enumSource: [{name: "Model", value: 0}, {name: "Screen", value: 1}] },
        u_RotateCenter02: { type: "Vector2", default: [0.5, 0.5], index: 33, alias: "第二层旋转中心(X,Y)", catalog: "基本设置", hidden: "(!data.EFFECT_LAYER_TWO && !data.EFFECT_LAYER_THREE) || !data.EFFECT_ROTATION_TWO" },
        u_RotateAngle02: { type: "Float", default: 0, index: 34, alias: "第二层旋转角度", catalog: "基本设置", hidden: "(!data.EFFECT_LAYER_TWO && !data.EFFECT_LAYER_THREE) || !data.EFFECT_ROTATION_TWO" },
        u_Translation02: { type: "Vector2", default: [0, 0], index: 35, alias: "第二层旋转位置(X,Y)", catalog: "基本设置", hidden: "(!data.EFFECT_LAYER_TWO && !data.EFFECT_LAYER_THREE) || !data.EFFECT_ROTATION_TWO" },
        u_DetailTex2: { type: "Texture2D", default: "white", index: 40, alias: "第三层颜色贴图(rgb),透明度(a)", catalog: "基本设置", hidden: "!data.EFFECT_LAYER_THREE" },
        u_DetailTex2_ST: { type: "Vector4", default: [1, 1, 0, 0], index: 41, alias: "第三层TilingOffset", catalog: "基本设置", hidden: "!data.EFFECT_LAYER_THREE" },
        u_Scroll2: { type: "Vector2", default: [0, 0], index: 42, alias: "第三层UV滚动速度(U,V)", catalog: "基本设置", hidden: "!data.EFFECT_LAYER_THREE" },
        u_Layer2UVMode: { type: "Int", default: 0, index: 42.5, alias: "第三层UV模式", catalog: "基本设置", hidden: "!data.EFFECT_LAYER_THREE", enumSource: [{name: "Model", value: 0}, {name: "Screen", value: 1}] },
        u_RotateCenter03: { type: "Vector2", default: [0.5, 0.5], index: 43, alias: "第三层旋转中心(X,Y)", catalog: "基本设置", hidden: "!data.EFFECT_LAYER_THREE || !data.EFFECT_ROTATION_THREE" },
        u_RotateAngle03: { type: "Float", default: 0, index: 44, alias: "第三层旋转角度", catalog: "基本设置", hidden: "!data.EFFECT_LAYER_THREE || !data.EFFECT_ROTATION_THREE" },
        u_Translation03: { type: "Vector2", default: [0, 0], index: 45, alias: "第三层旋转位置(X,Y)", catalog: "基本设置", hidden: "!data.EFFECT_LAYER_THREE || !data.EFFECT_ROTATION_THREE" },
        u_LayerColor: { type: "Vector4", default: [1, 1, 1, 1], index: 50, alias: "叠加颜色(rgb),透明度(a)", catalog: "基本设置" },
        u_LayerMultiplier: { type: "Float", default: 1.0, index: 51, alias: "叠加因子", catalog: "基本设置" },
        u_Alpha: { type: "Float", default: 1.0, index: 52, alias: "全局透明度", catalog: "基本设置", range: [0, 1] },
        u_DissolveTex: { type: "Texture2D", default: "white", index: 100, alias: "溶解贴图", catalog: "溶解效果设置", catalogOrder: 1, hidden: "!data.EFFECT_DISSOLVE" },
        u_DissolveTex_ST: { type: "Vector4", default: [1, 1, 0, 0], index: 101, alias: "溶解TilingOffset", catalog: "溶解效果设置", hidden: "!data.EFFECT_DISSOLVE" },
        u_DissolveAmount: { type: "Float", default: 0, index: 102, alias: "溶解度", catalog: "溶解效果设置", hidden: "!data.EFFECT_DISSOLVE", range: [0, 1.001] },
        u_UseDissolveAmountMinus: { type: "Float", default: 0, index: 103, alias: "允许负溶解度", catalog: "溶解效果设置", hidden: "!data.EFFECT_DISSOLVE" },
        u_DissolveAmountTex: { type: "Texture2D", default: "black", index: 104, alias: "溶解度贴图", catalog: "溶解效果设置", hidden: "!data.EFFECT_DISSOLVE" },
        u_DissolveAmountTex_ST: { type: "Vector4", default: [1, 1, 0, 0], index: 105, alias: "溶解度TilingOffset", catalog: "溶解效果设置", hidden: "!data.EFFECT_DISSOLVE" },
        u_RotateCenter04: { type: "Vector2", default: [0.5, 0.5], index: 105.1, alias: "溶解度贴图旋转中心(X,Y)", catalog: "溶解效果设置", hidden: "!data.EFFECT_DISSOLVE || !data.EFFECT_ROTATION_FOUR" },
        u_RotateAngle04: { type: "Float", default: 0, index: 105.2, alias: "溶解度贴图旋转角度", catalog: "溶解效果设置", hidden: "!data.EFFECT_DISSOLVE || !data.EFFECT_ROTATION_FOUR" },
        u_Translation04: { type: "Vector2", default: [0, 0], index: 105.3, alias: "溶解度贴图旋转位置(X,Y)", catalog: "溶解效果设置", hidden: "!data.EFFECT_DISSOLVE || !data.EFFECT_ROTATION_FOUR" },
        u_DissolveFadeRange: { type: "Float", default: 0.1, index: 106, alias: "溶解范围", catalog: "溶解效果设置", hidden: "!data.EFFECT_DISSOLVE || data.EFFECT_FADE_EDGE", range: [0, 1] },
        u_DissolveDistortTex: { type: "Texture2D", default: "black", index: 107, alias: "溶解扭曲贴图", catalog: "溶解效果设置", hidden: "!data.EFFECT_DISSOLVE || !data.EFFECT_DISSOLVE_DISTORT" },
        u_DissolveDistortTex_ST: { type: "Vector4", default: [1, 1, 0, 0], index: 107.1, alias: "溶解扭曲TilingOffset", catalog: "溶解效果设置", hidden: "!data.EFFECT_DISSOLVE || !data.EFFECT_DISSOLVE_DISTORT" },
        u_DissolveDistortScroll: { type: "Vector2", default: [0, 0], index: 107.2, alias: "溶解扭曲UV滚动速度(U,V)", catalog: "溶解效果设置", hidden: "!data.EFFECT_DISSOLVE || !data.EFFECT_DISSOLVE_DISTORT" },
        u_DissolveDistortStrength: { type: "Float", default: 0, index: 107.3, alias: "溶解扭曲强度", catalog: "溶解效果设置", hidden: "!data.EFFECT_DISSOLVE || !data.EFFECT_DISSOLVE_DISTORT", range: [0, 10] },
        u_DissolveDistortUVMode: { type: "Int", default: 0, index: 107.4, alias: "溶解扭曲UV模式", catalog: "溶解效果设置", hidden: "!data.EFFECT_DISSOLVE || !data.EFFECT_DISSOLVE_DISTORT", enumSource: [{name: "Model", value: 0}, {name: "Screen", value: 1}] },
        u_FadeEdgeTex: { type: "Texture2D", default: "white", index: 110, alias: "溶解边缘贴图", catalog: "溶解效果设置", hidden: "!data.EFFECT_DISSOLVE || !data.EFFECT_FADE_EDGE" },
        u_FadeEdgeTex_ST: { type: "Vector4", default: [1, 1, 0, 0], index: 111, alias: "溶解边缘TilingOffset", catalog: "溶解效果设置", hidden: "!data.EFFECT_DISSOLVE || !data.EFFECT_FADE_EDGE" },
        u_FadeEdgeColor: { type: "Color", default: [1, 1, 1, 1], index: 112, alias: "溶解边缘颜色", catalog: "溶解效果设置", hidden: "!data.EFFECT_DISSOLVE || !data.EFFECT_FADE_EDGE" },
        u_FadeEdgeStrength: { type: "Float", default: 1.0, index: 113, alias: "溶解边缘强度", catalog: "溶解效果设置", hidden: "!data.EFFECT_DISSOLVE || !data.EFFECT_FADE_EDGE" },
        u_FadeEdgeType: { type: "Float", default: 0, index: 114, alias: "溶解边缘混合模式", catalog: "溶解效果设置", hidden: "!data.EFFECT_DISSOLVE || !data.EFFECT_FADE_EDGE", enumSource: [{name: "Blend", value: 0}, {name: "Add", value: 1}] },
        u_FadeEdgeRange1: { type: "Float", default: 0.1, index: 115, alias: "溶解边缘非过渡区范围", catalog: "溶解效果设置", hidden: "!data.EFFECT_DISSOLVE || !data.EFFECT_FADE_EDGE" },
        u_FadeEdgeRange2: { type: "Float", default: 0.1, index: 116, alias: "溶解边缘过渡区范围", catalog: "溶解效果设置", hidden: "!data.EFFECT_DISSOLVE || !data.EFFECT_FADE_EDGE" },
        u_AlphaTestValue: { type: "Float", default: 0.5, index: 201, alias: "Alpha测试阈值", catalog: "其他设置", range: [0, 1] }
    },
    attributeMap: {
        a_CornerTextureCoordinate: ["Vector4", 5],
        a_MeshPosition: ["Vector3", 1],
        a_MeshColor: ["Vector4", 2],
        a_MeshTextureCoordinate: ["Vector2", 3],
        a_ShapePositionStartLifeTime: ["Vector4", 4],
        a_DirectionTime: ["Vector4", 0],
        a_StartColor: ["Vector4", 6],
        a_StartSize: ["Vector3", 8],
        a_StartRotation0: ["Vector3", 9],
        a_StartSpeed: ["Float", 10],
        a_Random0: ["Vector4", 11],
        a_Random1: ["Vector4", 12],
        a_SimulationWorldPostion: ["Vector3", 13],
        a_SimulationWorldRotation: ["Vector4", 14],
        a_SimulationUV: ["Vector4", 15]
    },
    defines: {
        EFFECT_WRAPMODE_DEFAULT: { type: bool, default: true, index: 3, alias: "Default", catalog: "基本设置" },
        EFFECT_WRAPMODE_CLAMP: { type: bool, default: false, index: 4, alias: "Clamp", catalog: "基本设置" },
        EFFECT_WRAPMODE_REPEAT: { type: bool, default: false, index: 5, alias: "Repeat", catalog: "基本设置" },
        EFFECT_POLAR: { type: bool, default: false, alias: "极坐标开关", catalog: "基本设置", position: "after WrapMode" },
        EFFECT_ROTATION: { type: bool, default: false, alias: "第一层贴图旋转开关", catalog: "基本设置", position: "after u_Layer0UVMode" },
        EFFECT_DISTORT: { type: bool, default: false, alias: "第一层扰动效果开关", catalog: "基本设置", position: "after u_Translation" },
        EFFECT_GRADIENT_MAP: { type: bool, default: false, alias: "渐变映射开关", catalog: "基本设置", position: "after u_Distort0UVMode" },
        EFFECT_LAYER_ONE: { type: bool, default: true, index: 28, alias: "一层", catalog: "基本设置" },
        EFFECT_LAYER_TWO: { type: bool, default: false, index: 29, alias: "二层", catalog: "基本设置" },
        EFFECT_LAYER_THREE: { type: bool, default: false, index: 39, alias: "三层", catalog: "基本设置" },
        EFFECT_ROTATION_TWO: { type: bool, default: false, alias: "第二层贴图旋转开关", catalog: "基本设置", position: "after u_Layer1UVMode", hidden: "!data.EFFECT_LAYER_TWO && !data.EFFECT_LAYER_THREE" },
        EFFECT_ROTATION_THREE: { type: bool, default: false, alias: "第三层贴图旋转开关", catalog: "基本设置", position: "after u_Layer2UVMode", hidden: "!data.EFFECT_LAYER_THREE" },
        EFFECT_DISSOLVE: { type: bool, default: false, index: 99, alias: "溶解效果开关", catalog: "溶解效果设置", catalogOrder: 1 },
        EFFECT_ROTATION_FOUR: { type: bool, default: false, alias: "溶解度贴图旋转开关", catalog: "溶解效果设置", position: "after u_DissolveAmountTex_ST", hidden: "!data.EFFECT_DISSOLVE" },
        EFFECT_DISSOLVE_DISTORT: { type: bool, default: false, alias: "溶解扭曲开关", catalog: "溶解效果设置", position: "after u_DissolveFadeRange", hidden: "!data.EFFECT_DISSOLVE" },
        EFFECT_FADE_EDGE: { type: bool, default: false, index: 109, alias: "溶解边缘开关", catalog: "溶解效果设置", hidden: "!data.EFFECT_DISSOLVE" }
    },
    styles: {
        materialRenderMode: { default: 5 },
        s_Blend: { default: 2 },
        s_BlendDstRGB: { default: 7 },
        s_Cull: { default: 0 },
        s_DepthWrite: { default: false },
        EFFECT_WRAPMODE_DEFAULT: { inspector: null },
        EFFECT_WRAPMODE_CLAMP: { inspector: null },
        EFFECT_WRAPMODE_REPEAT: { inspector: null },
        EFFECT_LAYER_ONE: { inspector: null },
        EFFECT_LAYER_TWO: { inspector: null },
        EFFECT_LAYER_THREE: { inspector: null }
    },
    shaderPass:[
        {
            pipeline:"Forward",
            VS:Effect_FullEffectVS,
            FS:Effect_FullEffectFS,
            statefirst: true,
            renderState: {
                blend: "Seperate",
                blendEquationRGB: "Add",
                blendEquationAlpha: "Add",
                srcBlendRGB: "One",
                srcBlendAlpha: "One",
                dstBlendAlpha: "Zero"
            }
        }
    ]
}
Shader3D End

GLSL Start
#defineGLSL Effect_FullEffectVS

#define SHADER_NAME Effect_FullEffectVS

#include "Camera.glsl";
#include "particleShuriKenSpriteVS.glsl";
#include "Math.glsl";
#include "MathGradient.glsl";
#include "Color.glsl";
#include "Scene.glsl"
#include "SceneFogInput.glsl"

#ifdef RENDERMODE_MESH
varying vec4 v_MeshColor;
#endif

varying vec4 v_Color;
varying vec2 v_TextureCoordinate;
varying vec2 v_Texcoord0;

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
        outLifeVelocity = mix(u_VOLVelocityConst,
            u_VOLVelocityConstMax,
            vec3(a_Random1.y, a_Random1.z, a_Random1.w));
    #endif
    #ifdef VELOCITYOVERLIFETIMERANDOMCURVE
        outLifeVelocity = vec3(
        mix(getCurValueFromGradientFloat(u_VOLVelocityGradientX, normalizedAge),
            getCurValueFromGradientFloat(u_VOLVelocityGradientMaxX, normalizedAge),
            a_Random1.y),
        mix(getCurValueFromGradientFloat(u_VOLVelocityGradientY, normalizedAge),
            getCurValueFromGradientFloat(u_VOLVelocityGradientMaxY, normalizedAge),
            a_Random1.z),
        mix(getCurValueFromGradientFloat(u_VOLVelocityGradientZ, normalizedAge),
            getCurValueFromGradientFloat(u_VOLVelocityGradientMaxZ, normalizedAge),
            a_Random1.w));
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
        mix(getTotalValueFromGradientFloat(u_VOLVelocityGradientX, normalizedAge),
            getTotalValueFromGradientFloat(u_VOLVelocityGradientMaxX, normalizedAge),
            a_Random1.y),
        mix(getTotalValueFromGradientFloat(u_VOLVelocityGradientY, normalizedAge),
            getTotalValueFromGradientFloat(u_VOLVelocityGradientMaxY, normalizedAge),
            a_Random1.z),
        mix(getTotalValueFromGradientFloat(u_VOLVelocityGradientZ, normalizedAge),
            getTotalValueFromGradientFloat(u_VOLVelocityGradientMaxZ, normalizedAge),
            a_Random1.w));
    #endif

    vec3 finalPosition;
    if (u_VOLSpaceType == 0)
    {
        if (u_ScalingMode != 2)
            finalPosition = rotationByQuaternions(
                u_PositionScale * (a_ShapePositionStartLifeTime.xyz + startPosition + lifePosition),
                worldRotation);
        else
            finalPosition = rotationByQuaternions(
                u_PositionScale * a_ShapePositionStartLifeTime.xyz + startPosition + lifePosition,
                worldRotation);
    }
    else
    {
        if (u_ScalingMode != 2)
            finalPosition = rotationByQuaternions(
                u_PositionScale * (a_ShapePositionStartLifeTime.xyz + startPosition),
                worldRotation)
                + lifePosition;
        else
            finalPosition = rotationByQuaternions(
                u_PositionScale * a_ShapePositionStartLifeTime.xyz + startPosition,
                worldRotation)
                + lifePosition;
    }
#else
    vec3 finalPosition;
    if (u_ScalingMode != 2)
        finalPosition = rotationByQuaternions(
            u_PositionScale * (a_ShapePositionStartLifeTime.xyz + startPosition),
            worldRotation);
    else
        finalPosition = rotationByQuaternions(
            u_PositionScale * a_ShapePositionStartLifeTime.xyz + startPosition,
            worldRotation);
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
    color *= getColorFromGradient(u_ColorOverLifeGradientAlphas,
        u_ColorOverLifeGradientColors,
        normalizedAge, u_ColorOverLifeGradientRanges);
#endif
#ifdef RANDOMCOLOROVERLIFETIME
    color *= mix(getColorFromGradient(u_ColorOverLifeGradientAlphas,
            u_ColorOverLifeGradientColors,
            normalizedAge, u_ColorOverLifeGradientRanges),
        getColorFromGradient(u_MaxColorOverLifeGradientAlphas,
            u_MaxColorOverLifeGradientColors,
            normalizedAge, u_MaxColorOverLifeGradientRanges),
        a_Random0.y);
#endif
    return color;
}

vec2 computeParticleSizeBillbard(in vec2 size, in float normalizedAge)
{
#ifdef SIZEOVERLIFETIMECURVE
    size *= getCurValueFromGradientFloat(u_SOLSizeGradient, normalizedAge);
#endif
#ifdef SIZEOVERLIFETIMERANDOMCURVES
    size *= mix(getCurValueFromGradientFloat(u_SOLSizeGradient, normalizedAge),
        getCurValueFromGradientFloat(u_SOLSizeGradientMax, normalizedAge),
        a_Random0.z);
#endif
#ifdef SIZEOVERLIFETIMECURVESEPERATE
    size *= vec2(getCurValueFromGradientFloat(u_SOLSizeGradientX, normalizedAge),
        getCurValueFromGradientFloat(u_SOLSizeGradientY, normalizedAge));
#endif
#ifdef SIZEOVERLIFETIMERANDOMCURVESSEPERATE
    size *= vec2(mix(getCurValueFromGradientFloat(u_SOLSizeGradientX, normalizedAge),
            getCurValueFromGradientFloat(u_SOLSizeGradientMaxX, normalizedAge),
            a_Random0.z),
        mix(getCurValueFromGradientFloat(u_SOLSizeGradientY, normalizedAge),
            getCurValueFromGradientFloat(u_SOLSizeGradientMaxY, normalizedAge),
            a_Random0.z));
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
        size *= mix(getCurValueFromGradientFloat(u_SOLSizeGradient, normalizedAge),
            getCurValueFromGradientFloat(u_SOLSizeGradientMax, normalizedAge),
            a_Random0.z);
    #endif
    #ifdef SIZEOVERLIFETIMECURVESEPERATE
        size *= vec3(getCurValueFromGradientFloat(u_SOLSizeGradientX, normalizedAge),
            getCurValueFromGradientFloat(u_SOLSizeGradientY, normalizedAge),
            getCurValueFromGradientFloat(u_SOLSizeGradientZ, normalizedAge));
    #endif
    #ifdef SIZEOVERLIFETIMERANDOMCURVESSEPERATE
        size *= vec3(mix(getCurValueFromGradientFloat(u_SOLSizeGradientX, normalizedAge),
                getCurValueFromGradientFloat(u_SOLSizeGradientMaxX, normalizedAge),
                a_Random0.z),
            mix(getCurValueFromGradientFloat(u_SOLSizeGradientY, normalizedAge),
                getCurValueFromGradientFloat(u_SOLSizeGradientMaxY, normalizedAge),
                a_Random0.z),
            mix(getCurValueFromGradientFloat(u_SOLSizeGradientZ, normalizedAge),
                getCurValueFromGradientFloat(u_SOLSizeGradientMaxZ, normalizedAge),
                a_Random0.z));
    #endif
    return size;
}
#endif

float computeParticleRotationFloat(in float rotation, in float age, in float normalizedAge)
{
#ifdef ROTATIONOVERLIFETIME
    #ifdef ROTATIONOVERLIFETIMECONSTANT
        float ageRot = u_ROLAngularVelocityConst * age;
        rotation += ageRot;
    #endif
    #ifdef ROTATIONOVERLIFETIMECURVE
        rotation += getTotalValueFromGradientFloat(u_ROLAngularVelocityGradient, normalizedAge);
    #endif
    #ifdef ROTATIONOVERLIFETIMERANDOMCONSTANTS
        float ageRot = mix(u_ROLAngularVelocityConst, u_ROLAngularVelocityConstMax, a_Random0.w) * age;
        rotation += ageRot;
    #endif
    #ifdef ROTATIONOVERLIFETIMERANDOMCURVES
        rotation += mix(
            getTotalValueFromGradientFloat(u_ROLAngularVelocityGradient, normalizedAge),
            getTotalValueFromGradientFloat(u_ROLAngularVelocityGradientMax, normalizedAge),
            a_Random0.w);
    #endif
#endif
#ifdef ROTATIONOVERLIFETIMESEPERATE
    #ifdef ROTATIONOVERLIFETIMECONSTANT
        float ageRot = u_ROLAngularVelocityConstSeprarate.z * age;
        rotation += ageRot;
    #endif
    #ifdef ROTATIONOVERLIFETIMECURVE
        rotation += getTotalValueFromGradientFloat(u_ROLAngularVelocityGradientZ, normalizedAge);
    #endif
    #ifdef ROTATIONOVERLIFETIMERANDOMCONSTANTS
        float ageRot = mix(u_ROLAngularVelocityConstSeprarate.z,
            u_ROLAngularVelocityConstMaxSeprarate.z,
            a_Random0.w) * age;
        rotation += ageRot;
    #endif
    #ifdef ROTATIONOVERLIFETIMERANDOMCURVES
        rotation += mix(getTotalValueFromGradientFloat(u_ROLAngularVelocityGradientZ, normalizedAge),
            getTotalValueFromGradientFloat(u_ROLAngularVelocityGradientMaxZ, normalizedAge),
            a_Random0.w);
    #endif
#endif
    return rotation;
}

#if defined(RENDERMODE_MESH) && (defined(ROTATIONOVERLIFETIME) || defined(ROTATIONOVERLIFETIMESEPERATE))
vec3 computeParticleRotationVec3(in vec3 rotation, in float age, in float normalizedAge)
{
    #ifdef ROTATIONOVERLIFETIME
        #ifdef ROTATIONOVERLIFETIMECONSTANT
            float ageRot = u_ROLAngularVelocityConst * age;
            rotation += ageRot;
        #endif
        #ifdef ROTATIONOVERLIFETIMECURVE
            rotation += getTotalValueFromGradientFloat(u_ROLAngularVelocityGradient, normalizedAge);
        #endif
        #ifdef ROTATIONOVERLIFETIMERANDOMCONSTANTS
            float ageRot = mix(u_ROLAngularVelocityConst, u_ROLAngularVelocityConstMax, a_Random0.w) * age;
            rotation += ageRot;
        #endif
        #ifdef ROTATIONOVERLIFETIMERANDOMCURVES
            rotation += mix(
                getTotalValueFromGradientFloat(u_ROLAngularVelocityGradient, normalizedAge),
                getTotalValueFromGradientFloat(u_ROLAngularVelocityGradientMax, normalizedAge),
                a_Random0.w);
        #endif
    #endif
    #ifdef ROTATIONOVERLIFETIMESEPERATE
        #ifdef ROTATIONOVERLIFETIMECONSTANT
            vec3 ageRot = u_ROLAngularVelocityConstSeprarate * age;
            rotation += ageRot;
        #endif
        #ifdef ROTATIONOVERLIFETIMECURVE
            rotation += vec3(getTotalValueFromGradientFloat(u_ROLAngularVelocityGradientX, normalizedAge),
                getTotalValueFromGradientFloat(u_ROLAngularVelocityGradientY, normalizedAge),
                getTotalValueFromGradientFloat(u_ROLAngularVelocityGradientZ, normalizedAge));
        #endif
        #ifdef ROTATIONOVERLIFETIMERANDOMCONSTANTS
            vec3 ageRot = mix(u_ROLAngularVelocityConstSeprarate,
                u_ROLAngularVelocityConstMaxSeprarate,
                a_Random0.w) * age;
            rotation += ageRot;
        #endif
        #ifdef ROTATIONOVERLIFETIMERANDOMCURVES
            rotation += vec3(mix(getTotalValueFromGradientFloat(u_ROLAngularVelocityGradientX, normalizedAge),
                    getTotalValueFromGradientFloat(u_ROLAngularVelocityGradientMaxX, normalizedAge),
                    a_Random0.w),
                mix(getTotalValueFromGradientFloat(u_ROLAngularVelocityGradientY, normalizedAge),
                    getTotalValueFromGradientFloat(u_ROLAngularVelocityGradientMaxY, normalizedAge),
                    a_Random0.w),
                mix(getTotalValueFromGradientFloat(u_ROLAngularVelocityGradientZ, normalizedAge),
                    getTotalValueFromGradientFloat(u_ROLAngularVelocityGradientMaxZ, normalizedAge),
                    a_Random0.w));
        #endif
    #endif
    return rotation;
}
#endif

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
    float frame = floor(mix(getFrameFromGradient(u_TSAGradientUVs, uvNormalizedAge),
        getFrameFromGradient(u_TSAMaxGradientUVs, uvNormalizedAge),
        a_Random1.x));
    float totalULength = frame * u_TSASubUVLength.x;
    float floorTotalULength = floor(totalULength);
    uv.x += totalULength - floorTotalULength;
    uv.y += floorTotalULength * u_TSASubUVLength.y;
#endif
    return uv;
}

void main()
{
    float age = u_CurrentTime - a_DirectionTime.w;
    float normalizedAge = age / a_ShapePositionStartLifeTime.w;
    vec3 lifeVelocity;
    if (normalizedAge < 1.0)
    {
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

#ifdef SPHERHBILLBOARD
        vec2 corner = a_CornerTextureCoordinate.xy;
        vec3 cameraUpVector = normalize(u_CameraUp);
        vec3 sideVector = normalize(cross(u_CameraDirection, cameraUpVector));
        vec3 upVector = normalize(cross(sideVector, u_CameraDirection));
        corner *= computeParticleSizeBillbard(a_StartSize.xy, normalizedAge);
    #if defined(ROTATIONOVERLIFETIME) || defined(ROTATIONOVERLIFETIMESEPERATE)
        if (u_ThreeDStartRotation != 0)
        {
            vec3 rotation = vec3(a_StartRotation0.xy,
                computeParticleRotationFloat(a_StartRotation0.z, age, normalizedAge));
            center += u_SizeScale.xzy * rotationByEuler(corner.x * sideVector + corner.y * upVector, rotation);
        }
        else
        {
            float rot = computeParticleRotationFloat(a_StartRotation0.x, age, normalizedAge);
            float c = cos(rot);
            float s = sin(rot);
            mat2 rotation = mat2(c, -s, s, c);
            corner = rotation * corner;
            center += u_SizeScale.xzy * (corner.x * sideVector + corner.y * upVector);
        }
    #else
        if (u_ThreeDStartRotation != 0)
        {
            center += u_SizeScale.xzy * rotationByEuler(corner.x * sideVector + corner.y * upVector, a_StartRotation0);
        }
        else
        {
            float c = cos(a_StartRotation0.x);
            float s = sin(a_StartRotation0.x);
            mat2 rotation = mat2(c, -s, s, c);
            corner = rotation * corner;
            center += u_SizeScale.xzy * (corner.x * sideVector + corner.y * upVector);
        }
    #endif
#endif

#ifdef STRETCHEDBILLBOARD
        vec2 corner = a_CornerTextureCoordinate.xy;
        vec3 velocity;
    #if defined(VELOCITYOVERLIFETIMECONSTANT) || defined(VELOCITYOVERLIFETIMECURVE) || defined(VELOCITYOVERLIFETIMERANDOMCONSTANT) || defined(VELOCITYOVERLIFETIMERANDOMCURVE)
        if (u_VOLSpaceType == 0)
            velocity = rotationByQuaternions(u_SizeScale * (startVelocity + lifeVelocity), worldRotation) + gravityVelocity;
        else
            velocity = rotationByQuaternions(u_SizeScale * startVelocity, worldRotation) + lifeVelocity + gravityVelocity;
    #else
        velocity = rotationByQuaternions(u_SizeScale * startVelocity, worldRotation) + gravityVelocity;
    #endif
        vec3 cameraUpVector = normalize(velocity);
        vec3 direction = normalize(center - u_CameraPos);
        vec3 sideVector = normalize(cross(direction, normalize(velocity)));
        sideVector = u_SizeScale.xzy * sideVector;
        cameraUpVector = length(vec3(u_SizeScale.x, 0.0, 0.0)) * cameraUpVector;
        vec2 size = computeParticleSizeBillbard(a_StartSize.xy, normalizedAge);
        const mat2 rotaionZHalfPI = mat2(0.0, -1.0, 1.0, 0.0);
        corner = rotaionZHalfPI * corner;
        corner.y = corner.y - abs(corner.y);
        float speed = length(velocity);
        center += sign(u_SizeScale.x) * (sign(u_StretchedBillboardLengthScale) * size.x * corner.x * sideVector + (speed * u_StretchedBillboardSpeedScale + size.y * u_StretchedBillboardLengthScale) * corner.y * cameraUpVector);
#endif

#ifdef HORIZONTALBILLBOARD
        vec2 corner = a_CornerTextureCoordinate.xy;
        const vec3 cameraUpVector = vec3(0.0, 0.0, 1.0);
        const vec3 sideVector = vec3(-1.0, 0.0, 0.0);
        float rot = computeParticleRotationFloat(a_StartRotation0.x, age, normalizedAge);
        float c = cos(rot);
        float s = sin(rot);
        mat2 rotation = mat2(c, -s, s, c);
        corner = rotation * corner * cos(0.78539816339744830961566084581988);
        corner *= computeParticleSizeBillbard(a_StartSize.xy, normalizedAge);
        center += u_SizeScale.xzy * (corner.x * sideVector + corner.y * cameraUpVector);
#endif

#ifdef VERTICALBILLBOARD
        vec2 corner = a_CornerTextureCoordinate.xy;
        const vec3 cameraUpVector = vec3(0.0, 1.0, 0.0);
        vec3 sideVector = normalize(cross(u_CameraDirection, cameraUpVector));
        float rot = computeParticleRotationFloat(a_StartRotation0.x, age, normalizedAge);
        float c = cos(rot);
        float s = sin(rot);
        mat2 rotation = mat2(c, -s, s, c);
        corner = rotation * corner * cos(0.78539816339744830961566084581988);
        corner *= computeParticleSizeBillbard(a_StartSize.xy, normalizedAge);
        center += u_SizeScale.xzy * (corner.x * sideVector + corner.y * cameraUpVector);
#endif

#ifdef RENDERMODE_MESH
        vec3 size = computeParticleSizeMesh(a_StartSize, normalizedAge);
    #if defined(ROTATIONOVERLIFETIME) || defined(ROTATIONOVERLIFETIMESEPERATE)
        if (u_ThreeDStartRotation != 0)
        {
            vec3 rotation = vec3(a_StartRotation0.xy,
                computeParticleRotationFloat(a_StartRotation0.z, age, normalizedAge));
            center += rotationByQuaternions(
                u_SizeScale * rotationByEuler(a_MeshPosition * size, rotation),
                worldRotation);
        }
        else
        {
            #ifdef ROTATIONOVERLIFETIME
                float angle = computeParticleRotationFloat(a_StartRotation0.x, age, normalizedAge);
                if (a_ShapePositionStartLifeTime.x != 0.0 || a_ShapePositionStartLifeTime.y != 0.0)
                {
                    center += (rotationByQuaternions(
                        rotationByAxis(
                            u_SizeScale * a_MeshPosition * size,
                            normalize(cross(vec3(0.0, 0.0, 1.0),
                                vec3(a_ShapePositionStartLifeTime.xy, 0.0))),
                            angle),
                        worldRotation));
                }
                else
                {
                    vec3 axis = mix(vec3(0.0, 0.0, -1.0), vec3(0.0, -1.0, 0.0), float(u_Shape));
                    #ifdef SHAPE
                        center += u_SizeScale.xzy * (rotationByQuaternions(rotationByAxis(a_MeshPosition * size, axis, angle), worldRotation));
                    #else
                        if (u_SimulationSpace == 0)
                            center += rotationByAxis(u_SizeScale * a_MeshPosition * size, axis, angle);
                        else if (u_SimulationSpace == 1)
                            center += rotationByQuaternions(u_SizeScale * rotationByAxis(a_MeshPosition * size, axis, angle), worldRotation);
                    #endif
                }
            #endif
            #ifdef ROTATIONOVERLIFETIMESEPERATE
                vec3 angle = computeParticleRotationVec3(
                    vec3(0.0, 0.0, -a_StartRotation0.x), age, normalizedAge);
                center += (rotationByQuaternions(
                    rotationByEuler(u_SizeScale * a_MeshPosition * size,
                        vec3(angle.x, angle.y, angle.z)),
                    worldRotation));
            #endif
        }
    #else
        if (u_ThreeDStartRotation != 0)
        {
            center += rotationByQuaternions(
                u_SizeScale * rotationByEuler(a_MeshPosition * size, a_StartRotation0),
                worldRotation);
        }
        else
        {
            #ifdef SHAPE
                if (u_SimulationSpace == 0)
                    center += u_SizeScale * rotationByAxis(a_MeshPosition * size, vec3(0.0, -1.0, 0.0), a_StartRotation0.x);
                else if (u_SimulationSpace == 1)
                    center += rotationByQuaternions(
                        u_SizeScale * rotationByAxis(a_MeshPosition * size, vec3(0.0, -1.0, 0.0), a_StartRotation0.x),
                        worldRotation);
            #else
                if (a_ShapePositionStartLifeTime.x != 0.0 || a_ShapePositionStartLifeTime.y != 0.0)
                {
                    if (u_SimulationSpace == 0)
                        center += rotationByAxis(
                            u_SizeScale * a_MeshPosition * size,
                            normalize(cross(vec3(0.0, 0.0, 1.0),
                                vec3(a_ShapePositionStartLifeTime.xy, 0.0))),
                            a_StartRotation0.x);
                    else if (u_SimulationSpace == 1)
                        center += (rotationByQuaternions(
                            u_SizeScale * rotationByAxis(a_MeshPosition * size, normalize(cross(vec3(0.0, 0.0, 1.0), vec3(a_ShapePositionStartLifeTime.xy, 0.0))), a_StartRotation0.x),
                            worldRotation));
                }
                else
                {
                    vec3 axis = mix(vec3(0.0, 0.0, -1.0), vec3(0.0, -1.0, 0.0), float(u_Shape));
                    if (u_SimulationSpace == 0)
                        center += u_SizeScale * rotationByAxis(a_MeshPosition * size, axis, a_StartRotation0.x);
                    else if (u_SimulationSpace == 1)
                        center += rotationByQuaternions(
                            u_SizeScale * rotationByAxis(a_MeshPosition * size, axis, a_StartRotation0.x),
                            worldRotation);
                }
            #endif
        }
    #endif
        v_MeshColor = a_MeshColor;
#endif

        gl_Position = u_Projection * u_View * vec4(center, 1.0);
        vec4 startcolor = gammaToLinear(a_StartColor);
        v_Color = computeParticleColor(startcolor, normalizedAge);

        vec2 simulateUV;
    #if defined(SPHERHBILLBOARD) || defined(STRETCHEDBILLBOARD) || defined(HORIZONTALBILLBOARD) || defined(VERTICALBILLBOARD)
        simulateUV = a_SimulationUV.xy + a_CornerTextureCoordinate.zw * a_SimulationUV.zw;
        v_TextureCoordinate = computeParticleUV(simulateUV, normalizedAge);
    #endif
    #ifdef RENDERMODE_MESH
        simulateUV = a_SimulationUV.xy + a_MeshTextureCoordinate * a_SimulationUV.zw;
        v_TextureCoordinate = computeParticleUV(simulateUV, normalizedAge);
    #endif
        v_Texcoord0 = v_TextureCoordinate;
        v_TextureCoordinate = TransformUV(v_TextureCoordinate, u_TilingOffset);
    }
    else
    {
        gl_Position = vec4(2.0, 2.0, 2.0, 1.0);
    }
    gl_Position = remapPositionZ(gl_Position);
    #ifdef FOG
        FogHandle(gl_Position.z);
    #endif
}

#endGLSL

#defineGLSL Effect_FullEffectFS

#define SHADER_NAME Effect_FullEffectFS

#include "Scene.glsl";
#include "SceneFog.glsl";
#include "Color.glsl";
#include "Camera.glsl";

const vec4 c_ColorSpace = vec4(4.59479380, 4.59479380, 4.59479380, 2.0);

varying vec4 v_Color;
varying vec2 v_TextureCoordinate;
varying vec2 v_Texcoord0;

#ifdef RENDERMODE_MESH
varying vec4 v_MeshColor;
#endif

vec2 TransformUV(vec2 texcoord, vec4 tilingOffset)
{
    vec2 transTexcoord = vec2(texcoord.x, texcoord.y - 1.0) * tilingOffset.xy + vec2(tilingOffset.z, -tilingOffset.w);
    transTexcoord.y += 1.0;
    return transTexcoord;
}

vec2 RotateUV(vec2 uv, vec2 center, float angleDeg, vec2 trans)
{
    float rad = angleDeg * 0.01745329;
    float s = sin(rad);
    float c = cos(rad);
    vec2 delta = uv - center + trans;
    return vec2(delta.x * c - delta.y * s, delta.x * s + delta.y * c) + center;
}

vec2 PolarCoordinates(vec2 UV, vec2 Center, float RadialScale, float LengthScale)
{
    vec2 delta = UV - Center;
    float radius = length(delta) * 2.0 * RadialScale;
    // atan(x, y) matches Unity's atan2(delta.x, delta.y) argument order
    float angle = atan(delta.x, delta.y) * (1.0 / 6.28318530718) * LengthScale;
    return vec2(radius, angle);
}

void main()
{
    vec2 baseUV = v_Texcoord0;
    float time = u_Time;

    // Screen-Space UV (for layers that support model/screen UV switching)
    vec2 screenUV = gl_FragCoord.xy / u_Viewport.zw;

    // Polar Coordinates (global UV transform, affects all layers)
    #ifdef EFFECT_POLAR
        baseUV = PolarCoordinates(baseUV, u_PolarControl.xy, u_PolarControl.z, u_PolarControl.w);
    #endif

    // Dissolve (computed early, may discard)
    #ifdef EFFECT_DISSOLVE
        vec2 dissolveUV = TransformUV(baseUV, u_DissolveTex_ST);

        // Dissolve Distortion
        #ifdef EFFECT_DISSOLVE_DISTORT
            vec2 dissolveDistortBase = u_DissolveDistortUVMode == 0 ? baseUV : screenUV;
            vec2 dissolveDistortUV = TransformUV(dissolveDistortBase, u_DissolveDistortTex_ST) + fract(vec2(u_DissolveDistortScroll.x, -u_DissolveDistortScroll.y) * time);
            dissolveUV += texture2D(u_DissolveDistortTex, dissolveDistortUV).xy * u_DissolveDistortStrength;
        #endif

        float dissolveValue = texture2D(u_DissolveTex, dissolveUV).r;
        float dissolveAmount = u_DissolveAmount;

        // Dissolve Amount Texture (with optional UV rotation)
        #ifdef EFFECT_ROTATION_FOUR
            vec2 dissolveAmtBaseUV = RotateUV(baseUV, u_RotateCenter04, u_RotateAngle04, u_Translation04);
        #else
            vec2 dissolveAmtBaseUV = baseUV;
        #endif
        vec2 dissolveAmtUV = TransformUV(dissolveAmtBaseUV, u_DissolveAmountTex_ST);
        dissolveAmount += texture2D(u_DissolveAmountTex, dissolveAmtUV).r;
        dissolveAmount = min(1.001, dissolveAmount);

        #ifdef EFFECT_FADE_EDGE
            // Fade Edge mode: hard clip + edge color
            float fadeFactor = dissolveValue;
            float fadeValue = dissolveAmount;
            if (fadeFactor < fadeValue) discard;

            vec2 fadeEdgeUV = TransformUV(baseUV, u_FadeEdgeTex_ST);
            vec4 fadeEdgeTex = texture2D(u_FadeEdgeTex, fadeEdgeUV);
            vec3 fadeEdgeColor = fadeEdgeTex.rgb * u_FadeEdgeColor.rgb * u_FadeEdgeStrength;

            float fadeEdgeValue1 = u_FadeEdgeRange1 + fadeValue;
            float fadeEdgeValue2 = fadeEdgeValue1 + u_FadeEdgeRange2;
            float fadeEdgeRange = fadeEdgeValue2 - fadeEdgeValue1;
            float fadeAlpha = clamp((fadeEdgeValue2 - fadeFactor) / max(0.0001, fadeEdgeRange), 0.0, 1.0);
        #else
            // Standard dissolve: soft or hard edge
            float dissolveFactor = 1.0;
            if (dissolveAmount > 0.0 || u_UseDissolveAmountMinus > 0.0)
            {
                if (u_DissolveFadeRange <= 0.001) {
                    if (dissolveValue < dissolveAmount) discard;
                } else {
                    dissolveFactor = (1.0 - step(dissolveValue, dissolveAmount)) * smoothstep(dissolveAmount, dissolveAmount + u_DissolveFadeRange, dissolveValue);
                }
            }
        #endif
    #endif

    // Main texture UV (support model/screen UV mode + polar coordinates)
    vec2 mainUV;
    if (u_Layer0UVMode == 1) {
        mainUV = TransformUV(screenUV, u_TilingOffset);
    } else {
        #ifdef EFFECT_POLAR
            mainUV = TransformUV(baseUV, u_TilingOffset);
        #else
            mainUV = v_TextureCoordinate;
        #endif
    }
    mainUV += fract(vec2(u_Scroll0.x, -u_Scroll0.y) * time);

    // Distortion (support model/screen UV mode)
    #ifdef EFFECT_DISTORT
        vec2 distortBase = u_Distort0UVMode == 0 ? baseUV : screenUV;
        vec2 distortUV = TransformUV(distortBase, u_DistortTex_ST) + fract(vec2(u_DistortScroll.x, -u_DistortScroll.y) * time);
        mainUV += texture2D(u_DistortTex, distortUV).xy * u_DistortStrength;
    #endif

    // WrapMode
    #ifdef EFFECT_WRAPMODE_CLAMP
        mainUV = clamp(mainUV, 0.0, 1.0);
    #endif
    #ifdef EFFECT_WRAPMODE_REPEAT
        mainUV = fract(mainUV);
    #endif

    // UV Rotation (layer 1)
    #ifdef EFFECT_ROTATION
        mainUV = RotateUV(mainUV, u_RotateCenter, u_RotateAngle, u_Translation);
    #endif

    // Sample main texture
    vec4 texColor = texture2D(u_texture, mainUV);
    #ifdef Gamma_u_texture
        texColor = gammaToLinear(texColor);
    #endif

    // Gradient Map (R channel lookup, replaces all RGBA, applied before layer mixing)
    #ifdef EFFECT_GRADIENT_MAP
        vec2 gradientUV = u_GradientMapTex0_ST.xy * vec2(texColor.r, 0.5) + u_GradientMapTex0_ST.zw;
        texColor = texture2D(u_GradientMapTex0, gradientUV);
    #endif

    // 2nd Layer (support UV mode + rotation)
    #if defined(EFFECT_LAYER_TWO) || defined(EFFECT_LAYER_THREE)
        vec2 layer1Base = u_Layer1UVMode == 0 ? baseUV : screenUV;
        vec2 detailUV = TransformUV(layer1Base, u_DetailTex_ST) + fract(vec2(u_Scroll1.x, -u_Scroll1.y) * time);
        #ifdef EFFECT_ROTATION_TWO
            detailUV = RotateUV(detailUV, u_RotateCenter02, u_RotateAngle02, u_Translation02);
        #endif
        texColor *= texture2D(u_DetailTex, detailUV);
    #endif

    // 3rd Layer (support UV mode + rotation)
    #ifdef EFFECT_LAYER_THREE
        vec2 layer2Base = u_Layer2UVMode == 0 ? baseUV : screenUV;
        vec2 detail2UV = TransformUV(layer2Base, u_DetailTex2_ST) + fract(vec2(u_Scroll2.x, -u_Scroll2.y) * time);
        #ifdef EFFECT_ROTATION_THREE
            detail2UV = RotateUV(detail2UV, u_RotateCenter03, u_RotateAngle03, u_Translation03);
        #endif
        texColor *= texture2D(u_DetailTex2, detail2UV);
    #endif

    // Layer color tint and multiplier
    texColor *= u_LayerColor * u_LayerMultiplier * u_Alpha;

    // Premultiply alpha (matching Unity: color.rgb *= color.a)
    texColor.rgb *= texColor.a;

    // Apply dissolve to color
    #ifdef EFFECT_DISSOLVE
        #ifdef EFFECT_FADE_EDGE
            if (u_FadeEdgeType < 0.5) {
                // Blend mode: lerp edge color
                texColor.rgb = mix(texColor.rgb, fadeEdgeColor, fadeAlpha);
            } else {
                // Add mode: additive edge color
                texColor.rgb += fadeAlpha * fadeEdgeColor;
            }
        #else
            texColor.a *= dissolveFactor;
        #endif
    #endif

    // Final color composition (matching standard particle FS pattern)
    vec4 finalColor;
    #ifdef RENDERMODE_MESH
        finalColor = v_MeshColor;
    #else
        finalColor = vec4(1.0);
    #endif

    finalColor *= texColor * v_Color;

    #ifdef ALPHATEST
        if (finalColor.a < u_AlphaTestValue) discard;
    #endif

    #ifdef FOG
        finalColor.rgb = scenUnlitFog(finalColor.rgb);
    #endif

    gl_FragColor = finalColor;
    gl_FragColor = outputTransform(gl_FragColor);
}

#endGLSL
GLSL End
