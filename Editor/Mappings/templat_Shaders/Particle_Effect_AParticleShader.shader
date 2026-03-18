Shader3D Start
{
    type: Shader3D,
    name: Particle_Effect_AParticleShader,
    enableInstancing: true,
    supportReflectionProbe: false,
    shaderType: Effect,
    uniformMap: {
        // === 基础 ===
        u_AlphaTestValue: { type: Float, default: 0.5, range: [0.0, 1.0] },
        u_TilingOffset: { type: Vector4, default: [1, 1, 0, 0] },

        // === 主纹理 ===
        u_MainColor: { type: Vector4, default: [1, 1, 1, 1] },
        u_AlbedoTexture: { type: Texture2D, default: "white", options: { define: "ALBEDOTEXTURE" } },
        u_U_MainTex_Speed: { type: Float, default: 0.0, range: [-20.0, 20.0] },
        u_V_MainTex_Speed: { type: Float, default: 0.0, range: [-20.0, 20.0] },

        // === Mask ===
        u_MaskColor: { type: Float, default: 0.0, range: [0.0, 1.0] },
        u_MaskTex: { type: Texture2D, default: "white", options: { define: "MASKTEX" } },
        u_U_MaskTex_Speed: { type: Float, default: 0.0, range: [-10.0, 10.0] },
        u_V_MaskTex_Speed: { type: Float, default: 0.0, range: [-10.0, 10.0] },

        // === 边缘光(Fresnel) ===
        u_FresnelColor: { type: Color, default: [0, 0, 0, 0] },
        u_FresnelScale: { type: Float, default: 1.0, range: [0.0, 10.0] },
        u_FresnelPower: { type: Float, default: -2.0, range: [-5.0, 0.0] },

        // === 扰动(Noise) ===
        u_NoiseTex: { type: Texture2D, default: "white", options: { define: "NOISETEX" } },
        u_NoiseIntensity: { type: Float, default: 0.5 },
        u_U_NoiseTex_Speed: { type: Float, default: 0.0, range: [-10.0, 10.0] },
        u_V_NoiseTex_Speed: { type: Float, default: 0.0, range: [-10.0, 10.0] },

        // === 溶解(Dissolve) ===
        u_DissolveColor: { type: Color, default: [0, 0, 0, 0] },
        u_DissolveTex: { type: Texture2D, default: "white", options: { define: "DISSOLVETEX" } },
        u_DissolveIntensity: { type: Float, default: 0.0, range: [0.0, 1.0] },
        u_DissolveIntensity02: { type: Float, default: -1.0, range: [-1.0, 1.0] },
        u_HardAndSoft: { type: Float, default: 1.0, range: [0.5, 1.0] },
        u_SoftScale: { type: Float, default: 0.0, range: [0.0, 1.0] },
        u_U_DissolveTex_Speed: { type: Float, default: 0.0, range: [-10.0, 10.0] },
        u_V_DissolveTex_Speed: { type: Float, default: 0.0, range: [-10.0, 10.0] },

        // === 极坐标 ===
        u_Speed: { type: Float, default: 0.0, range: [-1.0, 1.0] },

        // === 旋涡 ===
        u_Angle: { type: Float, default: 0.0 },

        // === 顶点偏移 ===
        u_OffsetInt: { type: Float, default: 0.0 },
        u_VO_tillingU: { type: Float, default: 0.0 },
        u_VO_tillingV: { type: Float, default: 0.0 },
        u_VO_PannerSpeedU: { type: Float, default: 0.0 },
        u_VO_PannerSpeedV: { type: Float, default: 0.0 },
        u_XYZPower: { type: Vector4, default: [0, 0, 0, 0] },

        // === 其他 ===
        u_Alpha: { type: Float, default: 1.0, range: [0.0, 1.0] },

        // === 纹理Tiling/Offset ===
        u_AlbedoTexture_ST: { type: Vector4, default: [1, 1, 0, 0] },
        u_MaskTex_ST: { type: Vector4, default: [1, 1, 0, 0] },
        u_NoiseTex_ST: { type: Vector4, default: [1, 1, 0, 0] },
        u_DissolveTex_ST: { type: Vector4, default: [1, 1, 0, 0] },
    },
    defines: {
        RENDERMODE_MESH: { type: bool, default: false },
        TINTCOLOR: { type: bool, default: true },
        ADDTIVEFOG: { type: bool, default: true },
        USE_MASK: { type: bool, default: false },
        RIMLIGHT: { type: bool, default: false },
        NOISE: { type: bool, default: false },
        DISSOLVE: { type: bool, default: false },
        POLAR: { type: bool, default: false },
        SWIRL: { type: bool, default: false },
        VERTEX_OFFSET: { type: bool, default: false },
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
    shaderPass: [
        {
            pipeline: Forward,
            VS: Effect_AParticleShaderVS,
            FS: Effect_AParticleShaderFS,
            renderState: {
            }
        }
    ]
}
Shader3D End

GLSL Start
#defineGLSL Effect_AParticleShaderVS

    #define SHADER_NAME Effect_AParticleShaderVS

    #include "Camera.glsl";
    #include "particleShuriKenSpriteVS.glsl";
    #include "Math.glsl";
    #include "MathGradient.glsl";
    #include "Color.glsl";
    #include "Scene.glsl";
    #include "SceneFogInput.glsl";

    varying vec4 v_Color;
    varying vec3 v_NormalWS;
    varying vec4 v_Texcoord0;
    varying vec3 v_Texcoord3;
    varying vec2 v_TextureCoordinate;
    #ifdef RENDERMODE_MESH
    varying vec4 v_MeshColor;
    #endif

    // UV变换：Laya粒子系统的UV需要特殊处理Y轴翻转
    vec2 TransformUV(vec2 texcoord, vec4 tilingOffset)
    {
        vec2 transTexcoord = vec2(texcoord.x, texcoord.y - 1.0) * tilingOffset.xy + vec2(tilingOffset.z, -tilingOffset.w);
        transTexcoord.y += 1.0;
        return transTexcoord;
    }

    // ==================== 粒子系统核心函数 ====================

    #if defined(VELOCITYOVERLIFETIMECONSTANT) || defined(VELOCITYOVERLIFETIMECURVE) || defined(VELOCITYOVERLIFETIMERANDOMCONSTANT) || defined(VELOCITYOVERLIFETIMERANDOMCURVE)
    vec3 computeParticleLifeVelocity(in float normalizedAge)
    {
        vec3 outLifeVelocity;
        #ifdef VELOCITYOVERLIFETIMECONSTANT
            outLifeVelocity = u_VOLVelocityConst;
        #endif
        #ifdef VELOCITYOVERLIFETIMECURVE
            outLifeVelocity = vec3(
                getCurValueFromGradientFloat(u_VOLVelocityGradientX, normalizedAge),
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

    // 阻力处理
    vec3 getStartPosition(vec3 startVelocity, float age, vec3 dragData)
    {
        vec3 startPosition;
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
                lifePosition = vec3(
                    getTotalValueFromGradientFloat(u_VOLVelocityGradientX, normalizedAge),
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
            color *= mix(
                getColorFromGradient(u_ColorOverLifeGradientAlphas, u_ColorOverLifeGradientColors, normalizedAge, u_ColorOverLifeGradientRanges),
                getColorFromGradient(u_MaxColorOverLifeGradientAlphas, u_MaxColorOverLifeGradientColors, normalizedAge, u_MaxColorOverLifeGradientRanges),
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
            size *= mix(getCurValueFromGradientFloat(u_SOLSizeGradient, normalizedAge), getCurValueFromGradientFloat(u_SOLSizeGradientMax, normalizedAge), a_Random0.z);
        #endif
        #ifdef SIZEOVERLIFETIMECURVESEPERATE
            size *= vec2(getCurValueFromGradientFloat(u_SOLSizeGradientX, normalizedAge), getCurValueFromGradientFloat(u_SOLSizeGradientY, normalizedAge));
        #endif
        #ifdef SIZEOVERLIFETIMERANDOMCURVESSEPERATE
            size *= vec2(
                mix(getCurValueFromGradientFloat(u_SOLSizeGradientX, normalizedAge), getCurValueFromGradientFloat(u_SOLSizeGradientMaxX, normalizedAge), a_Random0.z),
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
            size *= vec3(
                getCurValueFromGradientFloat(u_SOLSizeGradientX, normalizedAge),
                getCurValueFromGradientFloat(u_SOLSizeGradientY, normalizedAge),
                getCurValueFromGradientFloat(u_SOLSizeGradientZ, normalizedAge));
        #endif
        #ifdef SIZEOVERLIFETIMERANDOMCURVESSEPERATE
            size *= vec3(
                mix(getCurValueFromGradientFloat(u_SOLSizeGradientX, normalizedAge), getCurValueFromGradientFloat(u_SOLSizeGradientMaxX, normalizedAge), a_Random0.z),
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
                rotation += mix(
                    getTotalValueFromGradientFloat(u_ROLAngularVelocityGradient, normalizedAge),
                    getTotalValueFromGradientFloat(u_ROLAngularVelocityGradientMax, normalizedAge),
                    a_Random0.w);
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
                rotation += mix(
                    getTotalValueFromGradientFloat(u_ROLAngularVelocityGradientZ, normalizedAge),
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
                rotation += vec3(
                    getTotalValueFromGradientFloat(u_ROLAngularVelocityGradientX, normalizedAge),
                    getTotalValueFromGradientFloat(u_ROLAngularVelocityGradientY, normalizedAge),
                    getTotalValueFromGradientFloat(u_ROLAngularVelocityGradientZ, normalizedAge));
            #endif
            #ifdef ROTATIONOVERLIFETIMERANDOMCONSTANTS
                vec3 ageRot = mix(u_ROLAngularVelocityConstSeprarate, u_ROLAngularVelocityConstMaxSeprarate, a_Random0.w) * age;
                rotation += ageRot;
            #endif
            #ifdef ROTATIONOVERLIFETIMERANDOMCURVES
                rotation += vec3(
                    mix(getTotalValueFromGradientFloat(u_ROLAngularVelocityGradientX, normalizedAge), getTotalValueFromGradientFloat(u_ROLAngularVelocityGradientMaxX, normalizedAge), a_Random0.w),
                    mix(getTotalValueFromGradientFloat(u_ROLAngularVelocityGradientY, normalizedAge), getTotalValueFromGradientFloat(u_ROLAngularVelocityGradientMaxY, normalizedAge), a_Random0.w),
                    mix(getTotalValueFromGradientFloat(u_ROLAngularVelocityGradientZ, normalizedAge), getTotalValueFromGradientFloat(u_ROLAngularVelocityGradientMaxZ, normalizedAge), a_Random0.w));
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
            float frame = floor(mix(
                getFrameFromGradient(u_TSAGradientUVs, uvNormalizedAge),
                getFrameFromGradient(u_TSAMaxGradientUVs, uvNormalizedAge),
                a_Random1.x));
            float totalULength = frame * u_TSASubUVLength.x;
            float floorTotalULength = floor(totalULength);
            uv.x += totalULength - floorTotalULength;
            uv.y += floorTotalULength * u_TSASubUVLength.y;
        #endif
        return uv;
    }

    // ==================== Perlin噪声（用于顶点偏移） ====================

    vec2 randomVec(vec2 noiseuv)
    {
        float v = dot(noiseuv, vec2(127.1, 311.7));
        return vec2(-1.0 + 2.0 * fract(sin(v) * 43758.5453123));
    }

    float perlinNoise(vec2 noiseuv)
    {
        vec2 pi = floor(noiseuv);
        vec2 pf = noiseuv - pi;
        vec2 w = pf * pf * (3.0 - 2.0 * pf);
        float lerp1 = mix(
            dot(randomVec(pi + vec2(0.0, 0.0)), pf - vec2(0.0, 0.0)),
            dot(randomVec(pi + vec2(1.0, 0.0)), pf - vec2(1.0, 0.0)), w.x);
        float lerp2 = mix(
            dot(randomVec(pi + vec2(0.0, 1.0)), pf - vec2(0.0, 1.0)),
            dot(randomVec(pi + vec2(1.0, 1.0)), pf - vec2(1.0, 1.0)), w.x);
        return mix(lerp1, lerp2, w.y);
    }

    // ==================== 主函数 ====================

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

            #ifdef RENDERMODE_MESH
                // ===== Mesh渲染模式 =====
                vec3 size = computeParticleSizeMesh(a_StartSize, normalizedAge);
                vec3 meshPos = a_MeshPosition * size;

                // 顶点偏移（Mesh模式下可用）
                #ifdef VERTEX_OFFSET
                    vec2 voUV = a_MeshTextureCoordinate * vec2(u_VO_tillingU, u_VO_tillingV) + u_Time * vec2(u_VO_PannerSpeedU, u_VO_PannerSpeedV);
                    float vNoise = perlinNoise(voUV);
                    // 使用归一化的网格位置作为偏移方向（近似法线）
                    vec3 offsetDir = normalize(a_MeshPosition + vec3(0.001));
                    meshPos += offsetDir * vNoise * u_OffsetInt * u_XYZPower.xyz;
                #endif

                #if defined(ROTATIONOVERLIFETIME) || defined(ROTATIONOVERLIFETIMESEPERATE)
                    if (u_ThreeDStartRotation != 0)
                    {
                        vec3 rotation = vec3(a_StartRotation0.xy, computeParticleRotationFloat(a_StartRotation0.z, age, normalizedAge));
                        center += rotationByQuaternions(u_SizeScale * rotationByEuler(meshPos, rotation), worldRotation);
                    }
                    else
                    {
                        #ifdef ROTATIONOVERLIFETIME
                            float angle = computeParticleRotationFloat(a_StartRotation0.x, age, normalizedAge);
                            if (a_ShapePositionStartLifeTime.x != 0.0 || a_ShapePositionStartLifeTime.y != 0.0)
                            {
                                center += rotationByQuaternions(
                                    rotationByAxis(u_SizeScale * meshPos,
                                        normalize(cross(vec3(0.0, 0.0, 1.0), vec3(a_ShapePositionStartLifeTime.xy, 0.0))),
                                        angle),
                                    worldRotation);
                            }
                            else
                            {
                                vec3 axis = mix(vec3(0.0, 0.0, -1.0), vec3(0.0, -1.0, 0.0), float(u_Shape));
                                #ifdef SHAPE
                                    center += u_SizeScale.xzy * rotationByQuaternions(rotationByAxis(meshPos, axis, angle), worldRotation);
                                #else
                                    if (u_SimulationSpace == 0)
                                        center += rotationByAxis(u_SizeScale * meshPos, axis, angle);
                                    else if (u_SimulationSpace == 1)
                                        center += rotationByQuaternions(u_SizeScale * rotationByAxis(meshPos, axis, angle), worldRotation);
                                #endif
                            }
                        #endif
                        #ifdef ROTATIONOVERLIFETIMESEPERATE
                            vec3 angle = computeParticleRotationVec3(vec3(0.0, 0.0, -a_StartRotation0.x), age, normalizedAge);
                            center += rotationByQuaternions(rotationByEuler(u_SizeScale * meshPos, vec3(angle.x, angle.y, angle.z)), worldRotation);
                        #endif
                    }
                #else
                    if (u_ThreeDStartRotation != 0)
                    {
                        center += rotationByQuaternions(u_SizeScale * rotationByEuler(meshPos, a_StartRotation0), worldRotation);
                    }
                    else
                    {
                        #ifdef SHAPE
                            if (u_SimulationSpace == 0)
                                center += u_SizeScale * rotationByAxis(meshPos, vec3(0.0, -1.0, 0.0), a_StartRotation0.x);
                            else if (u_SimulationSpace == 1)
                                center += rotationByQuaternions(u_SizeScale * rotationByAxis(meshPos, vec3(0.0, -1.0, 0.0), a_StartRotation0.x), worldRotation);
                        #else
                            if (a_ShapePositionStartLifeTime.x != 0.0 || a_ShapePositionStartLifeTime.y != 0.0)
                            {
                                if (u_SimulationSpace == 0)
                                    center += rotationByAxis(u_SizeScale * meshPos,
                                        normalize(cross(vec3(0.0, 0.0, 1.0), vec3(a_ShapePositionStartLifeTime.xy, 0.0))),
                                        a_StartRotation0.x);
                                else if (u_SimulationSpace == 1)
                                    center += rotationByQuaternions(
                                        u_SizeScale * rotationByAxis(meshPos,
                                            normalize(cross(vec3(0.0, 0.0, 1.0), vec3(a_ShapePositionStartLifeTime.xy, 0.0))),
                                            a_StartRotation0.x),
                                        worldRotation);
                            }
                            else
                            {
                                vec3 axis = mix(vec3(0.0, 0.0, -1.0), vec3(0.0, -1.0, 0.0), float(u_Shape));
                                if (u_SimulationSpace == 0)
                                    center += u_SizeScale * rotationByAxis(meshPos, axis, a_StartRotation0.x);
                                else if (u_SimulationSpace == 1)
                                    center += rotationByQuaternions(u_SizeScale * rotationByAxis(meshPos, axis, a_StartRotation0.x), worldRotation);
                            }
                        #endif
                    }
                #endif

                v_MeshColor = a_MeshColor;
                gl_Position = u_Projection * u_View * vec4(center, 1.0);

                // 粒子颜色（预乘u_MainColor，匹配Unity VS中 vertexColor *= _MainColor）
                // 手动gamma→linear转换，不依赖引擎Color.glsl的gammaToLinear（可能在gamma空间下是空操作）
                vec4 startcolor = vec4(pow(a_StartColor.rgb, vec3(2.2)), a_StartColor.a);
                v_Color = computeParticleColor(startcolor, normalizedAge) * u_MainColor;

                // UV计算（Mesh模式）
                vec2 simulateUV = a_SimulationUV.xy + a_MeshTextureCoordinate * a_SimulationUV.zw;
                v_TextureCoordinate = computeParticleUV(simulateUV, normalizedAge);

            #elif defined(STRETCHEDBILLBOARD)
                // ===== StretchedBillboard渲染模式 =====
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

                gl_Position = u_Projection * u_View * vec4(center, 1.0);

                vec4 startcolor = vec4(pow(a_StartColor.rgb, vec3(2.2)), a_StartColor.a);
                v_Color = computeParticleColor(startcolor, normalizedAge) * u_MainColor;

                vec2 simulateUV = a_SimulationUV.xy + a_CornerTextureCoordinate.zw * a_SimulationUV.zw;
                v_TextureCoordinate = computeParticleUV(simulateUV, normalizedAge);

            #elif defined(HORIZONTALBILLBOARD)
                // ===== HorizontalBillboard渲染模式（XZ平面） =====
                vec2 corner = a_CornerTextureCoordinate.xy;
                const vec3 cameraUpVector = vec3(0.0, 0.0, 1.0);
                const vec3 sideVector = vec3(-1.0, 0.0, 0.0);
                vec2 hBbSize = computeParticleSizeBillbard(a_StartSize.xy, normalizedAge);

                float rot = computeParticleRotationFloat(a_StartRotation0.x, age, normalizedAge);
                float c = cos(rot);
                float s = sin(rot);
                mat2 rotation = mat2(c, -s, s, c);
                corner = rotation * corner * cos(0.78539816339744830961566084581988);
                corner *= hBbSize;

                center += u_SizeScale.xzy * (corner.x * sideVector + corner.y * cameraUpVector);

                gl_Position = u_Projection * u_View * vec4(center, 1.0);

                vec4 startcolor = vec4(pow(a_StartColor.rgb, vec3(2.2)), a_StartColor.a);
                v_Color = computeParticleColor(startcolor, normalizedAge) * u_MainColor;

                vec2 simulateUV = a_SimulationUV.xy + a_CornerTextureCoordinate.zw * a_SimulationUV.zw;
                v_TextureCoordinate = computeParticleUV(simulateUV, normalizedAge);

            #elif defined(VERTICALBILLBOARD)
                // ===== VerticalBillboard渲染模式（Y轴竖直，面向相机） =====
                vec2 corner = a_CornerTextureCoordinate.xy;
                const vec3 cameraUpVector = vec3(0.0, 1.0, 0.0);
                vec3 sideVector = normalize(cross(u_CameraDirection, cameraUpVector));
                vec2 vBbSize = computeParticleSizeBillbard(a_StartSize.xy, normalizedAge);

                float rot = computeParticleRotationFloat(a_StartRotation0.x, age, normalizedAge);
                float c = cos(rot);
                float s = sin(rot);
                mat2 rotation = mat2(c, -s, s, c);
                corner = rotation * corner * cos(0.78539816339744830961566084581988);
                corner *= vBbSize;

                center += u_SizeScale.xzy * (corner.x * sideVector + corner.y * cameraUpVector);

                gl_Position = u_Projection * u_View * vec4(center, 1.0);

                vec4 startcolor = vec4(pow(a_StartColor.rgb, vec3(2.2)), a_StartColor.a);
                v_Color = computeParticleColor(startcolor, normalizedAge) * u_MainColor;

                vec2 simulateUV = a_SimulationUV.xy + a_CornerTextureCoordinate.zw * a_SimulationUV.zw;
                v_TextureCoordinate = computeParticleUV(simulateUV, normalizedAge);

            #else
                // ===== SphereBillboard渲染模式（默认，面向相机） =====
                vec2 corner = a_CornerTextureCoordinate.xy;
                vec3 cameraUpVector = normalize(u_CameraUp);
                vec3 sideVector = normalize(cross(u_CameraDirection, cameraUpVector));
                vec3 upVector = normalize(cross(sideVector, u_CameraDirection));
                corner *= computeParticleSizeBillbard(a_StartSize.xy, normalizedAge);

                #if defined(ROTATIONOVERLIFETIME) || defined(ROTATIONOVERLIFETIMESEPERATE)
                    if (u_ThreeDStartRotation != 0)
                    {
                        vec3 rotation = vec3(a_StartRotation0.xy, computeParticleRotationFloat(a_StartRotation0.z, age, normalizedAge));
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

                gl_Position = u_Projection * u_View * vec4(center, 1.0);

                // 粒子颜色（预乘u_MainColor，匹配Unity VS中 vertexColor *= _MainColor）
                // 手动gamma→linear转换，不依赖引擎Color.glsl的gammaToLinear（可能在gamma空间下是空操作）
                vec4 startcolor = vec4(pow(a_StartColor.rgb, vec3(2.2)), a_StartColor.a);
                v_Color = computeParticleColor(startcolor, normalizedAge) * u_MainColor;

                // UV计算（Billboard模式）
                vec2 simulateUV = a_SimulationUV.xy + a_CornerTextureCoordinate.zw * a_SimulationUV.zw;
                v_TextureCoordinate = computeParticleUV(simulateUV, normalizedAge);
            #endif
        }
        else
        {
            // 粒子已死亡，移出视野
            gl_Position = vec4(2.0, 2.0, 2.0, 1.0);
        }

        gl_Position = remapPositionZ(gl_Position);

        #ifdef FOG
            FogHandle(gl_Position.z);
        #endif

        // 特效相关varying赋值
        v_Texcoord0.xy = TransformUV(v_TextureCoordinate, u_TilingOffset);
        v_Texcoord0.zw = v_TextureCoordinate; // 保留原始UV（未经tiling的）

        // 边缘光：Billboard模式下法线近似为相机方向
        v_NormalWS = -normalize(u_CameraDirection);
        v_Texcoord3 = -u_CameraDirection; // 视线方向（用于Fresnel计算）
    }

#endGLSL

#defineGLSL Effect_AParticleShaderFS

    #define SHADER_NAME Effect_AParticleShaderFS

    #include "Scene.glsl";
    #include "SceneFog.glsl";
    #include "Color.glsl";
    #include "Camera.glsl";

    varying vec4 v_Color;
    varying vec3 v_NormalWS;
    varying vec4 v_Texcoord0;
    varying vec3 v_Texcoord3;
    varying vec2 v_TextureCoordinate;
    #ifdef RENDERMODE_MESH
    varying vec4 v_MeshColor;
    #endif

    // ==================== 噪声函数（FS中也需要用到） ====================

    vec2 randomVec(vec2 noiseuv)
    {
        float v = dot(noiseuv, vec2(127.1, 311.7));
        return vec2(-1.0 + 2.0 * fract(sin(v) * 43758.5453123));
    }

    float perlinNoise(vec2 noiseuv)
    {
        vec2 pi = floor(noiseuv);
        vec2 pf = noiseuv - pi;
        vec2 w = pf * pf * (3.0 - 2.0 * pf);
        float lerp1 = mix(
            dot(randomVec(pi + vec2(0.0, 0.0)), pf - vec2(0.0, 0.0)),
            dot(randomVec(pi + vec2(1.0, 0.0)), pf - vec2(1.0, 0.0)), w.x);
        float lerp2 = mix(
            dot(randomVec(pi + vec2(0.0, 1.0)), pf - vec2(0.0, 1.0)),
            dot(randomVec(pi + vec2(1.0, 1.0)), pf - vec2(1.0, 1.0)), w.x);
        return mix(lerp1, lerp2, w.y);
    }

    // ==================== 主函数 ====================

    void main()
    {
        vec4 uv = v_Texcoord0;

        // uvBase: 基础UV，POLAR开启时变为极坐标UV（匹配Unity中uv.xy的变化流程）
        vec2 uvBase = uv.zw;

        // ===== 极坐标UV（匹配Unity: 先计算polar UV，后续mainUVSpeed基于polar UV） =====
        #ifdef POLAR
            vec2 polarInput = uv.zw * 2.0 - 1.0;
            uvBase = vec2(
                (u_Time * u_Speed + length(polarInput)),
                ((atan(polarInput.x, polarInput.y) / 6.3) + 0.5));
        #endif

        vec2 mainUVSpeed = vec2(u_U_MainTex_Speed, u_V_MainTex_Speed) * u_Time + uvBase;
        vec2 maskUVSpeed = vec2(u_U_MaskTex_Speed, u_V_MaskTex_Speed) * u_Time + uv.zw; // mask始终使用原始UV（匹配Unity的i.uv.xy）

        // ===== 旋涡UV =====
        #ifdef SWIRL
            vec2 Swirluv = vec2(uv.z - 0.5, uv.w - 0.5);
            float f = distance(Swirluv, vec2(0.0, 0.0));
            float s = sin(mix(0.0, u_Angle, f));
            float c = cos(mix(0.0, u_Angle, f));
            Swirluv = vec2(-Swirluv.x * c + Swirluv.y * s, Swirluv.x * s + Swirluv.y * c);
            mainUVSpeed = vec2(Swirluv.x + 0.5, Swirluv.y + 0.5);
        #endif

        // ===== 扰动UV（使用uvBase，POLAR开启时基于极坐标UV，匹配Unity） =====
        #ifdef NOISE
        {
            vec2 noiseUVSpeed = vec2(u_U_NoiseTex_Speed, u_V_NoiseTex_Speed) * u_Time + uvBase;
            vec4 noiseTex = texture2D(u_NoiseTex, noiseUVSpeed * u_NoiseTex_ST.xy + u_NoiseTex_ST.zw) * u_NoiseIntensity;
            mainUVSpeed = vec2(u_U_MainTex_Speed, u_V_MainTex_Speed) * u_Time + uvBase + noiseTex.r;
        }
        #endif

        // ===== 主纹理采样（使用Unity相同的TRANSFORM_TEX公式: uv * ST.xy + ST.zw） =====
        vec4 mainTex = texture2D(u_AlbedoTexture, mainUVSpeed * u_TilingOffset.xy + u_TilingOffset.zw);
        vec3 col = mainTex.rgb * u_MainColor.rgb * v_Color.rgb;
        vec3 finalColor = col;
        float alpha = 1.0;

        // ===== Mask =====
        #ifdef USE_MASK
            vec3 mask = texture2D(u_MaskTex, maskUVSpeed * u_MaskTex_ST.xy + u_MaskTex_ST.zw).rgb;
            mask = mix(vec3(mask.r, mask.r, mask.r), mask.rgb, u_MaskColor);
            finalColor = mix(vec3(0.0), col, mask);
        #endif

        // ===== 边缘光(Fresnel) =====
        #ifdef RIMLIGHT
            float ndotv = dot(normalize(v_NormalWS), normalize(v_Texcoord3));
            vec3 fresnel = clamp(u_FresnelScale * pow(max(1.0 - ndotv, 0.0001), u_FresnelPower * 5.0), 0.0, 1.0) * u_FresnelColor.rgb;
            finalColor.rgb += fresnel;
        #endif

        // ===== 溶解 =====
        #ifdef DISSOLVE
        {
            vec2 dissolveUV = vec2(u_U_DissolveTex_Speed, u_V_DissolveTex_Speed) * u_Time + uvBase;
            float dissolveTex = texture2D(u_DissolveTex, dissolveUV * u_DissolveTex_ST.xy + u_DissolveTex_ST.zw).r - u_DissolveIntensity;

            // clip: 相当于Unity的clip(dissolveTex-0.001)
            if (dissolveTex - 0.001 < 0.0) { discard; }

            // 溶解边缘发光
            float dissolveStrength = smoothstep(u_DissolveIntensity * u_SoftScale * v_Color.a, -0.001, dissolveTex);
            vec3 dissolveColor = u_DissolveColor.rgb * dissolveStrength;
            finalColor.rgb = finalColor.rgb + dissolveColor;

            // 软硬溶解
            vec2 uvDissTex = dissolveUV * u_DissolveTex_ST.xy + u_DissolveTex_ST.zw;
            alpha = smoothstep(1.0 - u_HardAndSoft, u_HardAndSoft, clamp(texture2D(u_DissolveTex, uvDissTex).r - u_DissolveIntensity02, 0.0, 1.0));

            #ifdef USE_MASK
                finalColor.rgb = mix(vec3(0.0), finalColor.rgb, mask);
            #endif
        }
        #endif

        // ===== 极坐标叠加 =====
        #ifdef POLAR
            finalColor.rgb += col;
            #ifdef USE_MASK
                finalColor.rgb += mix(vec3(0.0), col, mask);
            #endif
        #endif

        // ===== 旋涡叠加 =====
        #ifdef SWIRL
            finalColor.rgb += mix(vec3(0.0), finalColor.rgb, texture2D(u_MaskTex, mainUVSpeed * u_MaskTex_ST.xy + u_MaskTex_ST.zw).a);
            #ifdef USE_MASK
                finalColor.rgb += mix(vec3(0.0), finalColor.rgb, texture2D(u_MaskTex, mainUVSpeed * u_MaskTex_ST.xy + u_MaskTex_ST.zw).a);
            #endif
        #endif

        // ===== Alpha计算 =====
        alpha *= mainTex.a * v_Color.a;

        #ifdef USE_MASK
            alpha *= mask.r;
        #endif

        alpha = min(1.0, alpha) * u_Alpha;

        // 预乘alpha到颜色中：WebGL/GLES2.0会在blending前将fragment输出clamp到[0,1]
        // 如果不预乘，HDR颜色(>1)全部被clamp为(1,1,1)，blend后变成均匀灰色
        // 预乘后较小的通道(R)有机会<1，保留颜色比例
        // 配合blend模式改为(One, One)，等效于原来的(SrcAlpha, One)
        gl_FragColor = vec4(finalColor * alpha, alpha);

        // === DEBUG: 全部关闭，正常渲染 ===
        // === END DEBUG ===

        // 注意：Unity的粒子shader中不会额外乘以mesh顶点色
        // 粒子系统的颜色已经通过a_StartColor传入，不需要再乘v_MeshColor
        // #ifdef RENDERMODE_MESH
        //     gl_FragColor *= v_MeshColor;
        // #endif

        #ifdef FOG
            gl_FragColor.rgb = scenUnlitFog(gl_FragColor.rgb);
        #endif

        // 在预乘alpha之后做outputTransform（linear→sRGB）
        // 预乘后HDR值已被alpha缩小，sRGB转换不会全部clamp为白色
        // 这样匹配Unity的管线：blend(linear) → sRGB framebuffer
        gl_FragColor = outputTransform(gl_FragColor);
    }

#endGLSL

GLSL End
