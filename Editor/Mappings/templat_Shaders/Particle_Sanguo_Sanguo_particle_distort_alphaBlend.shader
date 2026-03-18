Shader3D Start
{
    type:Shader3D,
    name: "Particle_Sanguo_Sanguo_particle_distort_alphaBlend",
    enableInstancing:true,
    supportReflectionProbe:false,
    shaderType:Effect,
    uniformMap:{
        u_texture: { type: "Texture2D", index: 0, alias: "主贴图", catalog: "基本设置", catalogOrder: 0 },
        u_TilingOffset: { type: "Vector4", default: [1, 1, 0, 0], index: 1, alias: "主贴图TilingOffset", catalog: "基本设置" },
        u_U_MainTex: { type: "Float", default: 0, index: 2, alias: "主贴图UV滚动U", catalog: "基本设置" },
        u_V_MainTex: { type: "Float", default: 0, index: 3, alias: "主贴图UV滚动V", catalog: "基本设置" },
        u_Glow: { type: "Float", default: 5, index: 4, alias: "发光强度", catalog: "基本设置" },
        u_Color: { type: "Color", default: [0.5, 0.5, 0.5, 1], index: 5, alias: "颜色", catalog: "基本设置" },
        u_distort_tex: { type: "Texture2D", default: "white", index: 10, alias: "扭曲贴图", catalog: "扭曲设置", catalogOrder: 1 },
        u_distort_tex_ST: { type: "Vector4", default: [1, 1, 0, 0], index: 11, alias: "扭曲TilingOffset", catalog: "扭曲设置" },
        u_U: { type: "Float", default: 0.2, index: 12, alias: "扭曲UV滚动U", catalog: "扭曲设置" },
        u_V: { type: "Float", default: 0.1, index: 13, alias: "扭曲UV滚动V", catalog: "扭曲设置" },
        u_QD: { type: "Float", default: 0.1, index: 14, alias: "扭曲强度", catalog: "扭曲设置" },
        u_AlphaTestValue: { type: "Float", default: 0.5, index: 201, alias: "Alpha测试阈值", catalog: "其他设置", catalogOrder: 3, range: [0, 1] }
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
    },
    styles: {
        materialRenderMode: { default: 5 },
        s_Cull: { default: 0 },
        s_DepthWrite: { default: false }
    },
    shaderPass:[
        {
            pipeline:"Forward",
            VS:Sanguo_particle_distort_alphaBlendVS,
            FS:Sanguo_particle_distort_alphaBlendFS,
            statefirst: true,
            renderState: {
                blend: "Enable",
                srcBlend: "SrcColor",
                dstBlend: "OneMinusSrcColor",
                blendEquation: "Add"
            }
        }
    ]
}
Shader3D End

GLSL Start
#defineGLSL Sanguo_particle_distort_alphaBlendVS

#define SHADER_NAME Sanguo_particle_distort_alphaBlendVS

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

#defineGLSL Sanguo_particle_distort_alphaBlendFS

#define SHADER_NAME Sanguo_particle_distort_alphaBlendFS

#include "Scene.glsl";
#include "SceneFog.glsl";
#include "Color.glsl";

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

void main()
{
    float time = u_Time;
    vec2 baseUV = v_Texcoord0;

    // Distortion UV: scroll is applied BEFORE TilingOffset (matches Unity TRANSFORM_TEX order)
    vec2 distScrolledUV = baseUV + fract(vec2(u_U, -u_V) * time);
    vec2 distUV = TransformUV(distScrolledUV, u_distort_tex_ST);
    vec4 distortCol = texture2D(u_distort_tex, distUV);

    // Main UV: distortion offset + scroll, then TilingOffset
    float distortOffset = distortCol.r * u_QD;
    vec2 mainScrolledUV = vec2(distortOffset) + baseUV + fract(vec2(u_U_MainTex, -u_V_MainTex) * time);
    vec2 mainUV = TransformUV(mainScrolledUV, u_TilingOffset);
    vec4 mainCol = texture2D(u_texture, mainUV);

    // Gamma to linear
    #ifdef GAMMATEXTURE
        mainCol = gammaToLinear(mainCol);
        distortCol = gammaToLinear(distortCol);
    #endif

    // Color mixing (matches Unity formula exactly)
    // emission = (vColor.rgb * color.rgb * mainCol.rgb * glow * distortCol.rgb)
    //          * (vColor.a * color.a * mainCol.a * glow) * mainCol.a
    vec3 baseRGB = v_Color.rgb * u_Color.rgb * mainCol.rgb * u_Glow * distortCol.rgb;
    float alphaFactor = v_Color.a * u_Color.a * mainCol.a * u_Glow;
    vec3 emission = baseRGB * alphaFactor * mainCol.a;

    vec4 finalColor;
    #ifdef RENDERMODE_MESH
        finalColor = v_MeshColor;
    #else
        finalColor = vec4(1.0);
    #endif

    finalColor.rgb *= emission;

    #ifdef ALPHATEST
        if (length(finalColor.rgb) < u_AlphaTestValue) discard;
    #endif

    gl_FragColor = finalColor;

    #ifdef FOG
        gl_FragColor = scenUnlitFog(gl_FragColor);
    #endif

    gl_FragColor = outputTransform(gl_FragColor);
}

#endGLSL
GLSL End
