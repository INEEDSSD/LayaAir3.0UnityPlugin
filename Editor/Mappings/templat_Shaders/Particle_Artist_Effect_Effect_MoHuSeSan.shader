Shader3D Start
{
    type:Shader3D,
    name:Particle_Artist_Effect_Effect_MoHuSeSan,
    enableInstancing:true,
    supportReflectionProbe:false,
    shaderType:Effect,
    uniformMap:{
    // Basic
    u_AlphaTestValue: { type: Float, default: 0.5, range: [0.0, 1.0] },
    u_TilingOffset: { type: Vector4, default: [1, 1, 0, 0] },
    // Shader Properties
    u_Float8: { type: Float, default: 0 },
    u_Numberofsamples: { type: Float, default: 0 },
    u_Switch: { type: Float, default: 1 },
    u_U: { type: Float, default: 0.5, range: [0, 1] },
    u_V: { type: Float, default: 0.5, range: [0, 1] },
    u_blur: { type: Float, default: 0 },
    u_mask: { type: Texture2D, default: "white", options: { define: "MASKMAP" } },
    u_mask_Scale: { type: Float, default: 0, range: [0, 1] },
    u_Dispersion_Scale: { type: Float, default: 0.5, range: [0, 1] },
    u_Dispersion: { type: Float, default: 1 },
    // Texture Tiling/Offset
    u_mask_ST: { type: Vector4, default: [1, 1, 0, 0] },
    u_Scroll0X: { type: Float, default: 0.0 },
    u_Scroll0Y: { type: Float, default: 0.0 },
},
defines: {
RENDERMODE_MESH: { type: bool, default: false },
TINTCOLOR: { type: bool, default: true },
ADDTIVEFOG: { type: bool, default: true },
NUMBEROFSAMPLES_X8: { type: bool, default: false },
NUMBEROFSAMPLES_X16: { type: bool, default: false },
NUMBEROFSAMPLES_X21: { type: bool, default: false },
NUMBEROFSAMPLES_X26: { type: bool, default: false },
_NUMBEROFSAMPLES_X8: { type: bool, default: false },
_NUMBEROFSAMPLES_X16: { type: bool, default: false },
_NUMBEROFSAMPLES_X21: { type: bool, default: false },
_NUMBEROFSAMPLES_X26: { type: bool, default: false },
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
    VS:Artist_Effect_Effect_MoHuSeSanVS,
    FS:Artist_Effect_Effect_MoHuSeSanFS
}
]
}
Shader3D End

GLSL Start
#defineGLSL Artist_Effect_Effect_MoHuSeSanVS

#define SHADER_NAME Artist_Effect_Effect_MoHuSeSan

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
varying vec4 v_y;
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

        vec4 BuYu_ComputeGrabScreenPos( vec4 pos )
        {
            float scale = 1.0;
            vec4 o = pos;
            o.y = pos.w * 0.5;
            // Fix: Laya u_ProjectionParams.z = invertY flag (Unity uses .x)
            o.y = ( pos.y - o.y ) * u_ProjectionParams.z * scale + o.y;
            return o;
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
            v_Texcoord0.xy = TransformUV(v_TextureCoordinate, u_TilingOffset);
            v_Texcoord0.zw = v_TextureCoordinate;
            v_ScreenPos.xy = (gl_Position.xy + gl_Position.w) * 0.5;
            v_ScreenPos.zw = gl_Position.zw;
            v_y = vec4(0.0);

        }
#endGLSL

#defineGLSL Artist_Effect_Effect_MoHuSeSanFS

#define SHADER_NAME Artist_Effect_Effect_MoHuSeSan

#include "Scene.glsl";
#include "SceneFog.glsl";
#include "Color.glsl";
#include "Camera.glsl";

        varying vec4 v_Color;
        varying vec4 v_ScreenPos;
        varying vec4 v_Texcoord0;
        varying vec2 v_TextureCoordinate;
        varying vec4 v_y;
#ifdef RENDERMODE_MESH
        varying vec4 v_MeshColor;
#endif

        vec4 BuYu_ComputeGrabScreenPos( vec4 pos )
        {
            float scale = 1.0;
            vec4 o = pos;
            o.y = pos.w * 0.5;
            // Fix: Laya u_ProjectionParams.z = invertY flag (Unity uses .x)
            o.y = ( pos.y - o.y ) * u_ProjectionParams.z * scale + o.y;
            return o;
        }

        void main()
        {

            vec4 finalColor;
            // Fix: use v_ScreenPos (screen-space position) instead of v_Texcoord0 (UV tiling data)
            vec4 screenPos = v_ScreenPos;
            vec4 grabScreenPos = BuYu_ComputeGrabScreenPos(screenPos);
            vec4 grabScreenPosNorm = grabScreenPos / grabScreenPos.w;
            vec2 grabScreenPosNormResult = (vec2(grabScreenPosNorm.r , grabScreenPosNorm.g));
            // Fix: sample u_CameraOpaqueTexture (Laya equivalent of Unity GrabPass _GrabScreen_1)
            vec4 screenColor01 = texture2D(u_CameraOpaqueTexture, grabScreenPosNormResult);
            vec4 screenColor02 = texture2D(u_CameraOpaqueTexture, grabScreenPosNormResult);
            vec2 uandv = (vec2(u_U , u_V));
            // Fix: use v_y (UV2 data) instead of v_Color (particle color)
            vec4 uv1 = v_y;
            uv1.xy = v_y.xy * vec2( 1.0,1.0 ) + vec2( 0.0,0.0 );
            float lerpuv1 = mix( uv1.x , 0.0 , u_Switch);
            vec2 temp_output01 = ( (grabScreenPosNormResult - uandv) * vec2( 0.01,0.01 ) * ( u_blur + lerpuv1) );
            vec2 temp_output02 = (grabScreenPosNormResult - temp_output01 );
            vec4 screenColor03 = texture2D(u_CameraOpaqueTexture, temp_output02);
            vec2 temp_output03 = ( temp_output02 - temp_output01 );
            vec4 screenColor04 = texture2D(u_CameraOpaqueTexture, temp_output03);
            vec3 screenColorFinal = (vec3(screenColor02.r , screenColor03.g , screenColor04.b));
            vec4 screenColor05 = texture2D(u_CameraOpaqueTexture, grabScreenPosNormResult);
            vec4 screenColor06 = texture2D(u_CameraOpaqueTexture, temp_output02);
            vec4 screenColor07 = texture2D(u_CameraOpaqueTexture, temp_output03);
            vec2 temp_output_41_0 = ( temp_output03 - temp_output01 );
            vec4 screenColor08 = texture2D(u_CameraOpaqueTexture, temp_output_41_0);
            vec2 temp_output_42_0 = ( temp_output_41_0 - temp_output01 );
            vec4 screenColor09 = texture2D(u_CameraOpaqueTexture, temp_output_42_0);
            vec2 temp_output_43_0 = ( temp_output_42_0 - temp_output01 );
            vec4 screenColor10 = texture2D(u_CameraOpaqueTexture, temp_output_43_0);
            vec2 temp_output_45_0 = ( temp_output_43_0 - temp_output01 );
            vec4 screenColor11 = texture2D(u_CameraOpaqueTexture, temp_output_45_0);
            vec2 temp_output_47_0 = ( temp_output_45_0 - temp_output01 );
            vec4 screenColor12 = texture2D(u_CameraOpaqueTexture, temp_output_47_0);
            vec4 temp_output_58_0 = ( screenColor05 + screenColor06 + screenColor07 + screenColor08 + screenColor09 + screenColor10 + screenColor11 + screenColor12 );
            vec2 temp_output_44_0 = ( temp_output_47_0 - temp_output01 );
            vec2 myVarName123 = temp_output01;
            vec2 temp_output_46_0 = ( temp_output_44_0 - myVarName123 );
            vec2 temp_output_129_0 = ( temp_output_46_0 - myVarName123 );
            vec2 temp_output_130_0 = ( temp_output_129_0 - myVarName123 );
            vec2 temp_output_131_0 = ( temp_output_130_0 - myVarName123 );
            vec2 temp_output_132_0 = ( temp_output_131_0 - myVarName123 );
            vec2 temp_output_144_0 = ( temp_output_132_0 - myVarName123 );
            vec2 temp_output_145_0 = ( temp_output_144_0 - myVarName123 );
            vec4 screenColor15 = texture2D(u_CameraOpaqueTexture, temp_output_145_0);
            vec4 screenColor13 = texture2D(u_CameraOpaqueTexture, temp_output_44_0);
            vec4 screenColor14 = texture2D(u_CameraOpaqueTexture, temp_output_46_0);
            vec4 screenColor16 = texture2D(u_CameraOpaqueTexture, temp_output_129_0);
            vec4 screenColor17 = texture2D(u_CameraOpaqueTexture, temp_output_130_0);
            vec4 screenColor18 = texture2D(u_CameraOpaqueTexture, temp_output_131_0);
            vec4 screenColor19 = texture2D(u_CameraOpaqueTexture, temp_output_132_0);
            vec4 screenColor20 = texture2D(u_CameraOpaqueTexture, temp_output_144_0);
            vec4 temp_output_134_0 = ( temp_output_58_0 + ( screenColor15 + screenColor13 + screenColor14 + screenColor16 + screenColor17 + screenColor18 + screenColor19 + screenColor20 ) );
            vec2 temp_output_152_0 = ( temp_output_145_0 - myVarName123 );
            vec4 screenColor21 = texture2D(u_CameraOpaqueTexture, temp_output_152_0);
            vec2 temp_output_153_0 = ( temp_output_152_0 - myVarName123 );
            vec4 screenColor22 = texture2D(u_CameraOpaqueTexture, temp_output_153_0);
            vec2 temp_output_154_0 = ( temp_output_153_0 - myVarName123 );
            vec4 screenColor23 = texture2D(u_CameraOpaqueTexture, temp_output_154_0);
            vec2 temp_output_155_0 = ( temp_output_154_0 - myVarName123 );
            vec4 screenColor25 = texture2D(u_CameraOpaqueTexture, temp_output_155_0);
            vec2 temp_output_156_0 = ( temp_output_155_0 - myVarName123 );
            vec4 screenColor24 = texture2D(u_CameraOpaqueTexture, temp_output_156_0);
            vec4 temp_output_157_0 = ( temp_output_134_0 + ( screenColor21 + screenColor22 + screenColor23 + screenColor25 + screenColor24 ) );
            vec2 temp_output_173_0 = ( temp_output_156_0 - myVarName123 );
            vec4 screenColor26 = texture2D(u_CameraOpaqueTexture, temp_output_173_0);
            vec2 temp_output_172_0 = ( temp_output_173_0 - myVarName123 );
            vec4 screenColor30 = texture2D(u_CameraOpaqueTexture, temp_output_172_0);
            vec2 temp_output_171_0 = ( temp_output_172_0 - myVarName123 );
            vec4 screenColor29 = texture2D(u_CameraOpaqueTexture, temp_output_171_0);
            vec2 temp_output_170_0 = ( temp_output_171_0 - myVarName123 );
            vec4 screenColor28 = texture2D(u_CameraOpaqueTexture, temp_output_170_0);
            // Fix: add missing screenColor27 (was incomplete in exported shader)
            vec4 screenColor27 = texture2D(u_CameraOpaqueTexture, ( temp_output_170_0 - myVarName123 ));

#ifdef NUMBEROFSAMPLES_X8
            vec4 staticSwitch = ( temp_output_58_0 / 8.0 );
#elif defined(NUMBEROFSAMPLES_X16)
            vec4 staticSwitch = ( temp_output_134_0 / 16.0 );
#elif defined(NUMBEROFSAMPLES_X21)
            vec4 staticSwitch = ( temp_output_157_0 / 21.0 );
#elif defined(NUMBEROFSAMPLES_X26)
            vec4 staticSwitch = ( ( temp_output_157_0 + ( screenColor26 + screenColor30 + screenColor29 + screenColor28 + screenColor27 ) ) / 26.0 );
#else
            vec4 staticSwitch = ( temp_output_58_0 / 8.0 );
#endif

            // Fix: use v_y (UV2 data) instead of v_Color (particle color)
            vec4 uv = v_y;
            float lerpResultScale = mix( uv.y , 0.0 , u_Dispersion);
            vec4 lerpScreenColor = mix( vec4( screenColorFinal , 0.0 ) , staticSwitch , ( u_Dispersion_Scale + lerpResultScale));
            finalColor = mix( screenColor01 , lerpScreenColor, ( texture2D(u_mask, grabScreenPosNormResult).r * u_mask_Scale ));
#ifdef RENDERMODE_MESH
            finalColor *= v_MeshColor;
#endif
            gl_FragColor = finalColor;

#ifdef FOG
            gl_FragColor.rgb = scenUnlitFog(gl_FragColor.rgb);
#endif

            gl_FragColor = outputTransform(gl_FragColor);
        }
#endGLSL

GLSL End
