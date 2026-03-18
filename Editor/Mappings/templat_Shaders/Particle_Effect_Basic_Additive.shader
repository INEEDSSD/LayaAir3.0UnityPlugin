Shader3D Start
{
    type:Shader3D,
    name:Effect_Basic_Additive,
    enableInstancing:true,
    supportReflectionProbe:false,
    shaderType:Effect,
    uniformMap:{
    // Basic
    u_AlphaTestValue: { type: Float, default: 0.5, range: [0.0, 1.0] },
    u_TilingOffset: { type: Vector4, default: [1, 1, 0, 0] },
    // Shader Properties
    u_TintColor: { type: Vector4, default: [0.5, 0.5, 0.5, 0.5] },
    u_AlbedoTexture: { type: Texture2D, default: "white", options: { define: "ALBEDOTEXTURE" } },
    u_UVScroll: { type: Vector4, default: [0, 0, 0, 0] },
    u_UseUIAlphaClip: { type: Float, default: 0 },
    u_ClipRect: { type: Vector4, default: [0, 0, 0, 0] },
    // Texture Tiling/Offset
    u_AlbedoTexture_ST: { type: Vector4, default: [1, 1, 0, 0] },
},
defines: {
RENDERMODE_MESH: { type: bool, default: false },
TINTCOLOR: { type: bool, default: true },
ADDTIVEFOG: { type: bool, default: true },
UNITY_UI_CLIP_RECT: { type: bool, default: false },
UNITY_UI_ALPHACLIP: { type: bool, default: false },
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
    VS:Effect_Basic_AdditiveVS,
    FS:Effect_Basic_AdditiveFS,
    renderState: {
        blend: "Seperate",
        blendEquationRGB: "Add",
        blendEquationAlpha: "Add",
        srcBlendRGB: "SrcAlpha",
        dstBlendRGB: "One",
        srcBlendAlpha: "SrcAlpha",
        dstBlendAlpha: "One"
    }
}
]
}
Shader3D End

GLSL Start
#defineGLSL Effect_Basic_AdditiveVS

#define SHADER_NAME Effect_Basic_Additive

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
varying vec2 v_TextureCoordinate;
varying vec4 v_WorldPosition;
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
    // 避免除以零：阻力为零时直接使用 v*t
    if (abs(dragData.x) < 0.0001) {
        startPosition = startVelocity * age;
    } else {
        float lasttime = min(startVelocity.x / dragData.x, age);
        startPosition = lasttime * (startVelocity - 0.5 * dragData * lasttime);
    }
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
            rotation += vec3(
                getTotalValueFromGradientFloat(u_ROLAngularVelocityGradientX, normalizedAge),
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
            rotation += vec3(
                mix(getTotalValueFromGradientFloat(u_ROLAngularVelocityGradientX, normalizedAge),
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
            float age = u_CurrentTime - a_DirectionTime.w;
            float normalizedAge = age / a_ShapePositionStartLifeTime.w;
            vec3 lifeVelocity;

            if (normalizedAge < 1.0)
            {
                // ===== Common setup =====
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

                // ===== SPHERHBILLBOARD =====
#ifdef SPHERHBILLBOARD
                vec2 corner = a_CornerTextureCoordinate.xy;
                vec3 cameraUpVector = normalize(u_CameraUp);
                vec3 sideVector = normalize(cross(u_CameraDirection, cameraUpVector));
                vec3 upVector = normalize(cross(sideVector, u_CameraDirection));
                corner *= computeParticleSizeBillbard(a_StartSize.xy, normalizedAge);
    #if defined(ROTATIONOVERLIFETIME) || defined(ROTATIONOVERLIFETIMESEPERATE)
                if (u_ThreeDStartRotation != 0)
                {
                    vec3 rotation = vec3(
                        a_StartRotation0.xy,
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

                // ===== STRETCHEDBILLBOARD =====
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

                // ===== HORIZONTALBILLBOARD =====
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

                // ===== VERTICALBILLBOARD =====
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

                // ===== RENDERMODE_MESH =====
#ifdef RENDERMODE_MESH
                vec3 size = computeParticleSizeMesh(a_StartSize, normalizedAge);
    #if defined(ROTATIONOVERLIFETIME) || defined(ROTATIONOVERLIFETIMESEPERATE)
                if (u_ThreeDStartRotation != 0)
                {
                    vec3 rotation = vec3(
                        a_StartRotation0.xy,
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
                        center += rotationByQuaternions(
                            rotationByAxis(
                                u_SizeScale * a_MeshPosition * size,
                                normalize(cross(vec3(0.0, 0.0, 1.0),
                                    vec3(a_ShapePositionStartLifeTime.xy, 0.0))),
                                angle),
                            worldRotation);
                    }
                    else
                    {
                        vec3 axis = mix(vec3(0.0, 0.0, -1.0), vec3(0.0, -1.0, 0.0), float(u_Shape));
            #ifdef SHAPE
                        center += u_SizeScale.xzy * rotationByQuaternions(rotationByAxis(a_MeshPosition * size, axis, angle), worldRotation);
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
                    center += rotationByQuaternions(
                        rotationByEuler(u_SizeScale * a_MeshPosition * size,
                            vec3(angle.x, angle.y, angle.z)),
                        worldRotation);
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
                            center += rotationByQuaternions(
                                u_SizeScale * rotationByAxis(a_MeshPosition * size,
                                    normalize(cross(vec3(0.0, 0.0, 1.0),
                                        vec3(a_ShapePositionStartLifeTime.xy, 0.0))),
                                    a_StartRotation0.x),
                                worldRotation);
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

                // ===== Common post-processing =====
                gl_Position = u_Projection * u_View * vec4(center, 1.0);
                v_WorldPosition = vec4(center, 1.0);

                vec4 startcolor = gammaToLinear(a_StartColor);
                v_Color = computeParticleColor(startcolor, normalizedAge);

                // UV calculation
                vec2 simulateUV;
    #if defined(SPHERHBILLBOARD) || defined(STRETCHEDBILLBOARD) || defined(HORIZONTALBILLBOARD) || defined(VERTICALBILLBOARD)
                simulateUV = a_SimulationUV.xy + a_CornerTextureCoordinate.zw * a_SimulationUV.zw;
                v_TextureCoordinate = computeParticleUV(simulateUV, normalizedAge);
    #endif
    #ifdef RENDERMODE_MESH
                simulateUV = a_SimulationUV.xy + a_MeshTextureCoordinate * a_SimulationUV.zw;
                v_TextureCoordinate = computeParticleUV(simulateUV, normalizedAge);
    #endif
            }
            else
            {
                gl_Position = vec4(2.0, 2.0, 2.0, 1.0);
                v_WorldPosition = vec4(0.0, 0.0, 0.0, 0.0);
            }

            gl_Position = remapPositionZ(gl_Position);

#ifdef FOG
            FogHandle(gl_Position.z);
#endif

            v_ScreenPos.xy = (gl_Position.xy + gl_Position.w) * 0.5;
            v_ScreenPos.zw = gl_Position.zw;
            v_Texcoord0.xy = TransformUV(v_TextureCoordinate, u_TilingOffset) + u_Time * u_UVScroll.xy;
            v_Texcoord0.zw = v_TextureCoordinate;
        }
#endGLSL

#defineGLSL Effect_Basic_AdditiveFS

#define SHADER_NAME Effect_Basic_Additive

#include "Scene.glsl";
#include "SceneFog.glsl";
#include "Color.glsl";
#include "Camera.glsl";

        varying vec4 v_Color;
        varying vec4 v_ScreenPos;
        varying vec4 v_Texcoord0;
        varying vec2 v_TextureCoordinate;
        varying vec4 v_WorldPosition;
#ifdef RENDERMODE_MESH
        varying vec4 v_MeshColor;
#endif

        void main()
        {
            vec4 col = 2.0 * v_Color * texture2D(u_AlbedoTexture, v_Texcoord0.xy) * u_TintColor;

#ifdef RENDERMODE_MESH
            col *= v_MeshColor;
#endif

#ifdef UNITY_UI_CLIP_RECT
            vec2 inside = step(u_ClipRect.xy, v_WorldPosition.xy) * step(v_WorldPosition.xy, u_ClipRect.zw);
            col.a *= inside.x * inside.y;
#endif

#ifdef UNITY_UI_ALPHACLIP
            if (col.a < 0.001) discard;
#endif

            gl_FragColor = col;

#ifdef FOG
            gl_FragColor.rgb = scenUnlitFog(gl_FragColor.rgb);
#endif

            gl_FragColor = outputTransform(gl_FragColor);
        }
#endGLSL

GLSL End
