Shader3D Start
{
    type:Shader3D,
    name:FishStandard_Base,
    enableInstancing:true,
    supportReflectionProbe:true,
    shaderType:D3,
    uniformMap:{
        // Basic
        u_AlphaTestValue: { type: Float, default: 0.5, range: [0.0, 1.0] },
        u_TilingOffset: { type: Vector4, default: [1, 1, 0, 0] },
        u_Alpha: { type: Float, default: 1.0, range: [0.0, 1.0] },
        
        // Base Color
        u_AlbedoColor: { type: Color, default: [1, 1, 1, 1] },
        u_AlbedoTexture: { type: Texture2D, options: { define: "ALBEDOTEXTURE" } },
        u_ColorIntensity: { type: Float, default: 1.0, range: [1.0, 10.0] },
        
        // PBR - MAER Map (Metallic, AO, Emission, Roughness)
        u_MAER: { type: Texture2D, options: { define: "MAERMAP" } },
        u_Metallic: { type: Float, default: 0.0, range: [0.0, 1.0] },
        u_MetallicRemapMin: { type: Float, default: 0.0 },
        u_MetallicRemapMax: { type: Float, default: 1.0 },
        u_Smoothness: { type: Float, default: 1.0, range: [0.0, 1.0] },
        u_SmoothnessRemapMin: { type: Float, default: 0.0 },
        u_SmoothnessRemapMax: { type: Float, default: 1.0 },
        u_OcclusionStrength: { type: Float, default: 1.0, range: [0.0, 1.5] },
        
        // Normal Map
        u_NormalTexture: { type: Texture2D, options: { define: "NORMALTEXTURE" } },
        u_NormalScale: { type: Float, default: 1.0, range: [0.0, 2.0] },
        
        // Mask (R-IBL, G-Matcap, B-MatcapAdd, A-Fresnel)
        u_Mask: { type: Texture2D, options: { define: "MASKMAP" } },
        
        // IBL
        u_IBLMap: { type: TextureCube, options: { define: "IBLMAP" } },
        u_IBLMapColor: { type: Color, default: [1, 1, 1, 1] },
        u_IBLMapIntensity: { type: Float, default: 0.1, range: [0.0, 3.0] },
        u_IBLMapPower: { type: Float, default: 1.0, range: [0.0, 5.0] },
        u_IBLMapRotateX: { type: Float, default: 0.0, range: [0.0, 360.0] },
        u_IBLMapRotateY: { type: Float, default: 0.0, range: [0.0, 360.0] },
        u_IBLMapRotateZ: { type: Float, default: 0.0, range: [0.0, 360.0] },
        
        // Matcap
        u_MatcapMap: { type: Texture2D, options: { define: "MATCAPMAP" } },
        u_MatcapAngle: { type: Float, default: 0.0, range: [0.0, 360.0] },
        u_MatcapStrength: { type: Float, default: 0.0, range: [0.0, 10.0] },
        u_MatcapPow: { type: Float, default: 1.0, range: [0.01, 10.0] },
        u_MatcapColor: { type: Color, default: [1, 1, 1, 1] },
        
        // Matcap Add
        u_MatcapAddMap: { type: Texture2D, options: { define: "MATCAPADDMAP" } },
        u_MatcapAddAngle: { type: Float, default: 0.0, range: [0.0, 360.0] },
        u_MatcapAddStrength: { type: Float, default: 0.0, range: [0.0, 10.0] },
        u_MatcapAddPow: { type: Float, default: 1.0, range: [0.01, 10.0] },
        u_MatcapAddColor: { type: Color, default: [1, 1, 1, 1] },
        
        // Emission
        u_EmissionColor: { type: Color, default: [0, 0, 0, 0] },
        u_EmissionTexture: { type: Texture2D, options: { define: "EMISSIONTEXTURE" } },
        u_EmissionIntensity: { type: Float, default: 1.0 },
        
        // NPR Toon Shading
        u_MedColor: { type: Color, default: [1, 1, 1, 1] },
        u_MedThreshold: { type: Float, default: 1.0, range: [0.0, 1.0] },
        u_MedSmooth: { type: Float, default: 0.25, range: [0.0, 0.5] },
        u_ShadowColor: { type: Color, default: [0, 0, 0, 1] },
        u_ShadowThreshold: { type: Float, default: 0.7, range: [0.0, 1.0] },
        u_ShadowSmooth: { type: Float, default: 0.2, range: [0.0, 0.5] },
        u_ReflectColor: { type: Color, default: [0.02, 0.02, 0.02, 0] },
        u_ReflectThreshold: { type: Float, default: 0.4, range: [0.0, 1.0] },
        u_ReflectSmooth: { type: Float, default: 0.15, range: [0.0, 0.5] },
        u_GIIntensity: { type: Float, default: 0.0, range: [0.0, 2.0] },
        
        // Specular
        u_SpecularHighlights: { type: Float, default: 1.0, range: [0.0, 1.0] },
        u_GGXSpecular: { type: Float, default: 1.0, range: [0.0, 1.0] },
        u_SpecularColor: { type: Color, default: [1, 1, 1, 1] },
        u_SpecularIntensity: { type: Float, default: 1.0 },
        u_SpecularLightOffset: { type: Vector4, default: [0, 0, 0, 0] },
        u_SpecularThreshold: { type: Float, default: 0.5, range: [0.1, 2.0] },
        u_SpecularSmooth: { type: Float, default: 0.5, range: [0.0, 0.5] },
        
        // Fresnel
        u_DirectionalFresnel: { type: Float, default: 0.0 },
        u_FresnelColor: { type: Color, default: [1, 0, 0, 0] },
        u_fresnelOffset: { type: Vector4, default: [0, 0, 0, 0] },
        u_FresnelThreshold: { type: Float, default: 0.5, range: [0.0, 1.0] },
        u_FresnelSmooth: { type: Float, default: 0.5, range: [0.0, 0.5] },
        u_FresnelIntensity: { type: Float, default: 1.0 },
        u_FresnelMetallic: { type: Float, default: 1.0, range: [0.0, 1.0] },
        u_FresnelFit: { type: Float, default: 0.0, range: [0.0, 1.0] },
        
        // Rim Effect
        u_RimColor: { type: Color, default: [1, 0.5, 0, 1] },
        u_RimPower: { type: Float, default: 1.0, range: [0.01, 10.0] },
        u_RimIntensity: { type: Float, default: 0.0 },
        u_RimStart: { type: Float, default: 0.0, range: [0.0, 1.0] },
        u_RimEnd: { type: Float, default: 1.0, range: [0.0, 1.0] },
        u_RimOffset: { type: Vector4, default: [0, 0, 0, 0] },
        
        // HSV Adjust
        u_AdjustHSV: { type: Float, default: 0.0, range: [0.0, 1.0] },
        u_AdjustHue: { type: Float, default: 0.0, range: [0.0, 360.0] },
        u_AdjustSaturation: { type: Float, default: 1.0, range: [0.0, 1.5] },
        u_AdjustValue: { type: Float, default: 1.0, range: [0.0, 1.5] },
        
        // Contrast
        u_OriginalColor: { type: Float, default: 0.0, range: [0.0, 1.0] },
        u_Contrast: { type: Float, default: 0.0, range: [0.0, 1.0] },
        u_ContrastScale: { type: Float, default: 1.0, range: [0.0, 3.0] },
        
        // Tonemapping
        u_ToneWeight: { type: Float, default: 0.0, range: [0.0, 3.0] },
        u_WhitePoint: { type: Float, default: 1.0, range: [0.0, 3.0] },
        
        // Light Control
        u_SelfLight: { type: Float, default: 0.0, range: [0.0, 1.0] },
        u_SelfLightDir: { type: Vector4, default: [0, 1, 0, 1] },
    },
    defines: {
        EMISSION: { type: bool, default: false },
        ENABLEVERTEXCOLOR: { type: bool, default: true },
        USENPR: { type: bool, default: true }
    },
    shaderPass:[
        {
            pipeline:Forward,
            VS:FishStandardVS,
            FS:FishStandardFS
        }
    ]
}
Shader3D End

GLSL Start
#defineGLSL FishStandardVS
    #define SHADER_NAME FishStandard_Base

    #include "Math.glsl";

    #include "Scene.glsl";
    #include "SceneFogInput.glsl";

    #include "Camera.glsl";
    #include "Sprite3DVertex.glsl";

    #include "VertexCommon.glsl";

    #include "PBRVertex.glsl";

    void main()
    {
        Vertex vertex;
        getVertexParams(vertex);

        PixelParams pixel;
        initPixelParams(pixel, vertex);

        gl_Position = getPositionCS(pixel.positionWS);
        gl_Position = remapPositionZ(gl_Position);

    #ifdef FOG
        FogHandle(gl_Position.z);
    #endif // FOG
    }
#endGLSL

#defineGLSL FishStandardFS
    #define SHADER_NAME FishStandard_Base

    #include "Color.glsl";

    #include "Scene.glsl";
    #include "SceneFog.glsl";

    #include "Camera.glsl";
    #include "Sprite3DFrag.glsl";

    #include "PBRMetallicFrag.glsl";
    
    //========================================
    // NPR Utility Functions (not in Laya libs)
    //========================================
    
    float LinearStep(float minVal, float maxVal, float value)
    {
        return clamp((value - minVal) / (maxVal - minVal + 0.0001), 0.0, 1.0);
    }
    
    float remapValue(float x, float t1, float t2, float s1, float s2)
    {
        return (x - t1) / (t2 - t1) * (s2 - s1) + s1;
    }

    vec3 Gamma22ToLinear(vec3 color)
    {
        return pow(color, vec3(2.2));
    }

    //========================================
    // Rotation Functions for IBL
    //========================================
    
    vec3 rotateVectorXYZ(float rx, float ry, float rz, vec3 v)
    {
        // Using rotationByEuler from Math.glsl
        vec3 rot = vec3(radians(rx), radians(ry), radians(rz));
        return rotationByEuler(v, rot);
    }

    //========================================
    // HSV Functions
    //========================================
    
    vec3 RGBtoHSV(vec3 rgb)
    {
        float cmax = max(rgb.r, max(rgb.g, rgb.b));
        float cmin = min(rgb.r, min(rgb.g, rgb.b));
        float delta = cmax - cmin;
        
        vec3 hsv = vec3(0.0, 0.0, cmax);
        
        if (delta > 0.0001)
        {
            hsv.y = delta / cmax;
            
            if (rgb.r == cmax)
                hsv.x = (rgb.g - rgb.b) / delta;
            else if (rgb.g == cmax)
                hsv.x = 2.0 + (rgb.b - rgb.r) / delta;
            else
                hsv.x = 4.0 + (rgb.r - rgb.g) / delta;
            
            hsv.x /= 6.0;
            if (hsv.x < 0.0) hsv.x += 1.0;
        }
        
        return hsv;
    }
    
    vec3 HSVtoRGB(vec3 hsv)
    {
        float h = hsv.x * 6.0;
        float s = hsv.y;
        float v = hsv.z;
        
        float c = v * s;
        float x = c * (1.0 - abs(mod(h, 2.0) - 1.0));
        float m = v - c;
        
        vec3 rgb;
        if (h < 1.0)      rgb = vec3(c, x, 0.0);
        else if (h < 2.0) rgb = vec3(x, c, 0.0);
        else if (h < 3.0) rgb = vec3(0.0, c, x);
        else if (h < 4.0) rgb = vec3(0.0, x, c);
        else if (h < 5.0) rgb = vec3(x, 0.0, c);
        else              rgb = vec3(c, 0.0, x);
        
        return rgb + vec3(m);
    }
    
    vec3 AdjustHSVColor(vec3 color, float hueShift, float satMult, float valMult)
    {
        vec3 hsv = RGBtoHSV(color);
        hsv.x = mod(hsv.x + hueShift / 360.0, 1.0);
        hsv.y *= satMult;
        hsv.z *= valMult;
        return HSVtoRGB(hsv);
    }

    //========================================
    // Fresnel Function
    //========================================
    
    vec3 FresnelCore(vec3 normal, vec3 viewDir, vec3 rimColor, float power, float intensity, float start, float end, vec3 offset)
    {
        vec3 N = normalize(normal);
        vec3 V = normalize(viewDir) + offset;
        float NdotV = 1.0 - clamp(dot(N, V), 0.0, 1.0);
        
        float range = smoothstep(start, end, NdotV);
        float fresnel = intensity * pow(range, power);
        return clamp(rimColor * fresnel, 0.0, 1.0);
    }

    //========================================
    // NPR Radiance Calculation
    //========================================
    
    vec3 CalculateNPRRadiance(vec3 lightDir, vec3 lightColor, vec3 normalWS)
    {
        float NdotL = dot(normalWS, lightDir);
        float halfLambert = NdotL * 0.5 + 0.5;
        
        float smoothMedTone = LinearStep(u_MedThreshold - u_MedSmooth, u_MedThreshold + u_MedSmooth, halfLambert);
        vec3 MedToneColor = mix(u_MedColor.rgb, vec3(1.0), smoothMedTone);
        
        float smoothShadow = LinearStep(u_ShadowThreshold - u_ShadowSmooth, u_ShadowThreshold + u_ShadowSmooth, halfLambert);
        vec3 ShadowColor = mix(u_ShadowColor.rgb, MedToneColor, smoothShadow);
        
        float smoothReflect = LinearStep(u_ReflectThreshold - u_ReflectSmooth, u_ReflectThreshold + u_ReflectSmooth, halfLambert);
        vec3 ReflectColor = mix(u_ReflectColor.rgb, ShadowColor, smoothReflect);
        
        return lightColor * ReflectColor;
    }

    //========================================
    // Stylized Specular (NPR)
    //========================================
    
    vec3 StylizedSpecular(float perceptualRoughness, vec3 specularColor, vec3 normalWS, vec3 lightDir, vec3 viewDir, vec3 specularLightOffset)
    {
        vec3 halfDir = SafeNormalize(normalize(lightDir + specularLightOffset) + viewDir);
        float NoH = clamp(dot(normalWS, halfDir), 0.0, 1.0);
        float LoH = clamp(dot(lightDir, halfDir), 0.0, 1.0);
        
        float roughness = perceptualRoughness * perceptualRoughness;
        float roughness2 = roughness * roughness;
        float roughness2MinusOne = roughness2 - 1.0;
        float normalizationTerm = roughness * 4.0 + 2.0;
        
        float d = NoH * NoH * roughness2MinusOne + 1.00001;
        float LoH2 = LoH * LoH;
        float specularTerm = roughness2 / ((d * d) * max(0.1, LoH2) * normalizationTerm);
        
        specularTerm = max(0.0, specularTerm - MEDIUMP_FLT_MIN);
        specularTerm = min(100.0, specularTerm);
        
        specularTerm = pow(specularTerm, u_SpecularThreshold + u_SpecularSmooth);
        float specularStylize = LinearStep(u_SpecularThreshold - u_SpecularSmooth, u_SpecularThreshold + u_SpecularSmooth, specularTerm);
        specularTerm = mix(specularStylize, specularTerm, u_GGXSpecular);
        
        return specularTerm * max(vec3(0.0), u_SpecularIntensity * u_SpecularColor.rgb) * specularColor;
    }

    //========================================
    // Initialize Surface Inputs
    //========================================

    void initSurfaceInputs(inout SurfaceInputs inputs, inout PixelParams pixel)
    {
        inputs.alphaTest = u_AlphaTestValue;

    #ifdef UV
        vec2 uv = transformUV(pixel.uv0, u_TilingOffset);
    #else
        vec2 uv = vec2(0.0);
    #endif

        // Diffuse Color
        inputs.diffuseColor = u_AlbedoColor.rgb * u_ColorIntensity;
        inputs.alpha = u_AlbedoColor.a * u_Alpha;

    #ifdef COLOR
        #ifdef ENABLEVERTEXCOLOR
        inputs.diffuseColor *= pixel.vertexColor.xyz;
        inputs.alpha *= pixel.vertexColor.a;
        #endif
    #endif

    #ifdef ALBEDOTEXTURE
        vec4 albedoSampler = texture2D(u_AlbedoTexture, uv);
        #ifdef Gamma_u_AlbedoTexture
        albedoSampler = gammaToLinear(albedoSampler);
        #endif
        inputs.diffuseColor *= albedoSampler.rgb;
        inputs.alpha *= albedoSampler.a;
    #endif

        // Normal
        inputs.normalTS = vec3(0.0, 0.0, 1.0);
    #ifdef NORMALTEXTURE
        vec3 normalSampler = texture2D(u_NormalTexture, uv).rgb;
        normalSampler = normalize(normalSampler * 2.0 - 1.0);
        normalSampler.y *= -1.0;
        inputs.normalTS = normalScale(normalSampler, u_NormalScale);
    #endif

        // MAER Map (Metallic, AO, Emission, Smoothness)
        inputs.metallic = u_Metallic;
        inputs.smoothness = u_Smoothness;
        inputs.occlusion = 1.0;

    #ifdef MAERMAP
        vec4 maerSampler = texture2D(u_MAER, uv);
        inputs.metallic = remapValue(maerSampler.r, 0.0, 1.0, u_MetallicRemapMin, u_MetallicRemapMax);
        inputs.occlusion = mix(1.0, maerSampler.g, u_OcclusionStrength);
        // Unity用反向remap: lerp(Max, Min, texA)，等价于 mix(Max, Min, texA)
        float remappedSmooth = mix(u_SmoothnessRemapMax, u_SmoothnessRemapMin, maerSampler.a);
        inputs.smoothness = remappedSmooth * u_Smoothness;
    #endif

        // Emission
        // Unity公式: emission = EmissionColor * EmissionTexture.rgb * EmissionScale
        // 注意: Unity没有使用MAER.b作为Emission遮罩
        inputs.emissionColor = vec3(0.0);
    #ifdef EMISSION
        inputs.emissionColor = u_EmissionColor.rgb * u_EmissionIntensity;
        #ifdef EMISSIONTEXTURE
        vec4 emissionSampler = texture2D(u_EmissionTexture, uv);
        #ifdef Gamma_u_EmissionTexture
        emissionSampler = gammaToLinear(emissionSampler);
        #endif
        inputs.emissionColor *= emissionSampler.rgb;
        #endif
    #endif
    }

    //========================================
    // Main Fragment Shader
    //========================================

    void main()
    {
        PixelParams pixel;
        getPixelParams(pixel);

        SurfaceInputs inputs;
        initSurfaceInputs(inputs, pixel);

    #ifdef UV
        vec2 uv = transformUV(pixel.uv0, u_TilingOffset);
    #else
        vec2 uv = vec2(0.0);
    #endif

        // Get view direction and normal
        vec3 positionWS = pixel.positionWS;
        vec3 viewDir = normalize(u_CameraPos - positionWS);
        
        // Calculate normalWS from normalTS
        vec3 normalWS = pixel.normalWS;
    #ifdef NORMALTEXTURE
        normalWS = normalize(pixel.TBN * inputs.normalTS);
    #endif

        // Light direction - use self light or scene default
        vec3 selfLightDir = u_SelfLightDir.xyz;
        // 如果SelfLightDir是零向量，使用默认方向 (0, 1, 0)
        float lightDirLen = length(selfLightDir);
        vec3 lightDir = lightDirLen > 0.001 ? normalize(selfLightDir) : normalize(vec3(0.0, 1.0, 0.0));
        vec3 lightColor = vec3(1.0);

        // Sample Mask
        vec4 maskTex = vec4(1.0);
    #ifdef MASKMAP
        maskTex = texture2D(u_Mask, uv);
    #endif

        //========================================
        // Calculate Surface Color
        //========================================
        
        vec4 surfaceColor;

    #ifdef USENPR
        // NPR Toon Shading
        vec3 specularLightOffset = mix(u_SpecularLightOffset.xyz, u_SelfLightDir.xyz, u_SelfLight);
        vec3 radiance = CalculateNPRRadiance(lightDir, lightColor, normalWS);
        float ndotl = LinearStep(u_ShadowThreshold - u_ShadowSmooth, u_ShadowThreshold + u_ShadowSmooth, dot(lightDir, normalWS) * 0.5 + 0.5);
        
        // Calculate specular color (simplified)
        float oneMinusReflectivity = (1.0 - inputs.metallic) * 0.96;
        vec3 diffuseColor = inputs.diffuseColor * oneMinusReflectivity;
        vec3 specularColor = mix(vec3(0.04), inputs.diffuseColor, inputs.metallic);
        float perceptualRoughness = 1.0 - inputs.smoothness;
        
        // Diffuse + GI
        vec3 finalDiffuse = diffuseColor * radiance * inputs.occlusion;
        vec3 gi = diffuseColor * inputs.occlusion * u_GIIntensity;
        
        // Stylized Specular
        vec3 specular = StylizedSpecular(perceptualRoughness, specularColor, normalWS, lightDir, viewDir, specularLightOffset) * radiance;
        specular = mix(vec3(0.0), specular, u_SpecularHighlights);
        
        surfaceColor = vec4(finalDiffuse + gi + specular + inputs.emissionColor, inputs.alpha);
    #else
        // Standard PBR Flow
        surfaceColor = PBR_Metallic_Flow(inputs, pixel);
    #endif

        //========================================
        // IBL Reflection
        //========================================
        
        // IBL Reflection
        vec3 iblColor = vec3(0.0);
    #ifdef IBLMAP
        vec3 reflectVector = reflect(-viewDir, normalWS);
        vec3 rotatedReflect = rotateVectorXYZ(u_IBLMapRotateX, u_IBLMapRotateY, u_IBLMapRotateZ, reflectVector);
        
        float iblPerceptualRoughness = 1.0 - inputs.smoothness;
        float mip = (1.7 - iblPerceptualRoughness * 0.7) * iblPerceptualRoughness * 8.0;
        vec4 iblSampler = textureCube(u_IBLMap, rotatedReflect, mip);
        iblColor = pow(iblSampler.rgb, vec3(u_IBLMapPower));
        iblColor *= u_IBLMapColor.rgb * u_IBLMapIntensity * inputs.metallic;
        
        surfaceColor.rgb = mix(surfaceColor.rgb, surfaceColor.rgb + iblColor, maskTex.r);
    #endif

        //========================================
        // Matcap
        //========================================
        
        // Matcap
        vec3 matcapColorFinal = vec3(0.0);
    #ifdef MATCAPMAP
        float cosmc = cos(radians(u_MatcapAngle));
        float sinmc = sin(radians(u_MatcapAngle));
        vec3 viewNormal = (u_View * vec4(normalWS, 0.0)).xyz;
        vec2 uv_Matcap = viewNormal.xy * 0.5 + 0.5;
        uv_Matcap = mat2(cosmc, -sinmc, sinmc, cosmc) * (uv_Matcap - 0.5) + 0.5;
        
        vec4 matcapSampler = texture2D(u_MatcapMap, uv_Matcap);
        vec3 matcapColor = matcapSampler.rgb * u_MatcapColor.rgb * u_MatcapStrength * 5.0;
        float matcapIntensity = clamp(pow(abs(matcapSampler.r), u_MatcapPow), 0.0, 1.0);
        matcapColor *= matcapIntensity;
        matcapColorFinal = matcapColor * maskTex.g;
        surfaceColor.rgb = mix(surfaceColor.rgb, surfaceColor.rgb + surfaceColor.rgb * matcapColor, maskTex.g);
    #endif

    #ifdef MATCAPADDMAP
        float cosmcAdd = cos(radians(u_MatcapAddAngle));
        float sinmcAdd = sin(radians(u_MatcapAddAngle));
        vec3 viewNormalAdd = (u_View * vec4(normalWS, 0.0)).xyz;
        vec2 uv_MatcapAdd = viewNormalAdd.xy * 0.5 + 0.5;
        uv_MatcapAdd = mat2(cosmcAdd, -sinmcAdd, sinmcAdd, cosmcAdd) * (uv_MatcapAdd - 0.5) + 0.5;
        
        vec4 matcapAddSampler = texture2D(u_MatcapAddMap, uv_MatcapAdd);
        float matcapAlAdd = clamp(pow(abs(matcapAddSampler.r), u_MatcapAddPow), 0.0, 1.0) * u_MatcapAddStrength;
        matcapAlAdd *= maskTex.b;
        
        surfaceColor.rgb = mix(surfaceColor.rgb, surfaceColor.rgb + matcapAddSampler.rgb * u_MatcapAddColor.rgb, matcapAlAdd);
    #endif

        //========================================
        // Fresnel (按照Unity StylizedGlobalIlluminationFresnel流程)
        //========================================
        
        vec3 fresnelSpecular = vec3(0.0);
        {
            // Unity: FresnelIntensity = lerp(0, _FresnelIntensity, texAlpha)
            float FresnelIntensity = mix(0.0, u_FresnelIntensity, maskTex.a);
            
            // Unity: viewDirectionWS已经在调用时加了offset
            vec3 viewDirWithOffset = normalize(viewDir + u_fresnelOffset.xyz);
            
            // Unity: GetViewForwardDir - 获取相机前向方向
            vec3 cameraForward = -vec3(u_View[0][2], u_View[1][2], u_View[2][2]);
            vec3 viewForwardDir = normalize(cameraForward + u_fresnelOffset.xyz);
            
            // dot值
            float dotNormal = dot(normalWS, viewDirWithOffset);
            float dotFit = dot(normalWS, viewForwardDir);
            
            // LinearStep输入值 (1.0 - dot)
            float linearStepInputNormal = 1.0 - clamp(dotNormal, 0.0, 1.0);
            float linearStepInputFit = 1.0 - clamp(dotFit, 0.0, 1.0);
            
            // LinearStep输出
            float linearStepNormal = LinearStep(u_FresnelThreshold - u_FresnelSmooth, u_FresnelThreshold + u_FresnelSmooth, linearStepInputNormal);
            float linearStepFit = LinearStep(u_FresnelThreshold - u_FresnelSmooth, u_FresnelThreshold + u_FresnelSmooth, linearStepInputFit);
            
            // Unity: ndotl计算
            float ndotl = LinearStep(u_ShadowThreshold - u_ShadowSmooth, u_ShadowThreshold + u_ShadowSmooth, 1.0);
            ndotl = mix(1.0, ndotl, u_DirectionalFresnel);
            
            // 顶点颜色系数
            float vertexColorR = 1.0;
        #ifdef COLOR
            vertexColorR = pixel.vertexColor.r;
        #endif
            
            // Unity: fresnelTerm = LinearStep(...) * max(0, FresnelIntensity * vertexcolor.r) * ndotl
            float fresnelTermNormal = linearStepNormal * max(0.0, FresnelIntensity * vertexColorR) * ndotl;
            float fresnelTermFit = linearStepFit * max(0.0, FresnelIntensity * vertexColorR) * ndotl;
            
            // Unity: fresnelTerm = lerp(fresnelTerm, fresnelFit, _FresnelFit)
            float fresnelTermFinal = mix(fresnelTermNormal, fresnelTermFit, u_FresnelFit);
            
            // Unity的EnvironmentBRDFCustom模拟:
            // Unity在UniversalFragmentStylizedPBRFresnel中传入:
            // - albedo=0, metallic=0, specular=0, smoothness=0
            // 这导致brdfData.diffuse≈0, brdfData.grazingTerm≈0.04, roughness=1
            // 最终公式: surfaceReduction * indirectSpecular * lerp(specular*radiance, grazingTerm, fresnelTerm) * fresnelTerm
            // 由于radiance=0, 结果 ≈ surfaceReduction * indirectSpecular * grazingTerm * fresnelTerm * fresnelTerm
            
            float roughness = 1.0;  // smoothness=0时，roughness=1
            float surfaceReduction = 1.0 / (roughness * roughness + 1.0);  // = 0.5
            float grazingTerm = 0.04;  // 默认dielectric reflectivity
            
            // indirectSpecular在没有IBL时接近0或很小的环境光
            vec3 indirectSpecular = vec3(0.1) * mix(1.0, inputs.metallic, u_FresnelMetallic);
            
            // Unity的BRDF公式: surfaceReduction * indirectSpecular * grazingTerm * fresnelTerm
            fresnelSpecular = surfaceReduction * indirectSpecular * grazingTerm * fresnelTermFinal * u_FresnelColor.rgb;
            
            surfaceColor.rgb += fresnelSpecular;
        }

        //========================================
        // Rim Effect
        //========================================
        
        vec3 effectRim = vec3(0.0);
        if (u_RimIntensity > 0.001)
        {
            effectRim = FresnelCore(normalWS, viewDir, u_RimColor.rgb, u_RimPower, u_RimIntensity, u_RimStart, u_RimEnd, u_RimOffset.xyz);
            surfaceColor.rgb += effectRim;
        }

        //========================================
        // Contrast
        //========================================
        
        vec3 avgColor = vec3(0.5) * u_ContrastScale;
        surfaceColor.rgb = mix(mix(vec3(0.5), surfaceColor.rgb, 1.1), surfaceColor.rgb, u_OriginalColor);
        surfaceColor.rgb = mix(surfaceColor.rgb, mix(avgColor, surfaceColor.rgb, 1.1), u_Contrast);

        //========================================
        // HSV Adjustment
        //========================================
        
        if (u_AdjustHSV > 0.5)
        {
            surfaceColor.rgb = AdjustHSVColor(surfaceColor.rgb, u_AdjustHue, u_AdjustSaturation, u_AdjustValue);
        }

        //========================================
        // Tonemapping
        //========================================
        
        if (u_ToneWeight > 0.001)
        {
            vec3 numerator = surfaceColor.rgb * (6.2 * surfaceColor.rgb + 0.5);
            vec3 denominator = surfaceColor.rgb * (6.2 * surfaceColor.rgb + 1.2) + 0.06;
            vec3 tonemapped = numerator / denominator;
            tonemapped *= u_WhitePoint;
            tonemapped = Gamma22ToLinear(tonemapped);
            surfaceColor.rgb = mix(surfaceColor.rgb, tonemapped, u_ToneWeight);
        }

        //========================================
        // Fog
        //========================================
        
    #ifdef FOG
        surfaceColor.rgb = sceneLitFog(surfaceColor.rgb);
    #endif

        //========================================
        // Alpha Test
        //========================================
        
    #ifdef ALPHATEST
        if (surfaceColor.a < u_AlphaTestValue)
            discard;
    #endif

        //========================================
        // Output
        //========================================
        
        surfaceColor.a = clamp(surfaceColor.a, 0.0, 1.0);
        
        gl_FragColor = surfaceColor;
        gl_FragColor = outputTransform(gl_FragColor);
    }
#endGLSL

GLSL End
