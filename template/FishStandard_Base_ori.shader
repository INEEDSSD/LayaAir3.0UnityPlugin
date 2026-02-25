Shader "FishStandard_Base"
{
    Properties
    {

        [Foldout(_FoldoutSurfaceEnd,Small,true)] _FoldoutSurface("Surface Options", Float) = 0.0
        [HideInInspector] _SurfaceType ("Surface Type", Float) = 0.0
        [HideInInspector] _BlendMode ("__mode", Float) = 0.0
        [HideInInspector] _SrcBlend ("__src", Float) = 1.0
        [HideInInspector] _DstBlend ("__dst", Float) = 0.0
        [HideInInspector] _ZWrite ("__zw", Float) = 1.0
		[HideInInspector] _ZWriteAnimatable("ZWrite Animatable", Range(0, 1)) = 1  // 新增：用于动画K帧
        [HideInInspector] _CullMode("__cull", Float) = 2.0
		[Header(Render Queue Offset)]
		[Toggle] _RenderQueueOffset("RenderQueue +1", Float) = 0

		//Light
		[HideInInspector]_SelfLight("SelfLight" ,Range(0,1)) = 0
		[HideInInspector]_SelfLightDir("SelfLightDir",Vector) = (0,0,0,1)
		[HideInInspector]_IBLRotate("IBLRotate",Vector) = (0,0,0,1)
		[HideInInspector]_MatCapRotate("MatCapRotate",Range(0,360)) = 0
		[HideInInspector]_isIBLRotateActive("isIBLRotateActive" ,Range(0,1)) = 0
		[HideInInspector]_isMatCapRotateActive("isMatCapRotateActive" ,Range(0,1)) = 0

        _AlphaClip("Alpha Clip", Float) = 0.0
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        _Alpha("Alpha",Range(0,1)) =1
		_ModelScale("ModelScale",Range(-1,5)) =0

        [SplitLine(3,18)]
        [Space(10)]
        _BaseMap("BaseMap", 2D) = "white" {}
        [HDR] _BaseColor("Color", Color) = (1,1,1,1)
		_ColorIntensity("Color Instensity" , Range(1,10)) = 1
        _MAER("MaerMap M-Metallic A-Ao E-Emission R-Smoothness ", 2D) = "white" {} 
       
	    _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
		_MetallicRemapMin("Metallic RemapMin", Float) = 0.0
		_MetallicRemapMax("Metallic RemapMax", Float) = 1.0
		
		_Smoothness("Smoothness", Range(0.0, 1.0)) = 1.0
		_SmoothnessRemapMin("Smoothness RemapMin", Float) = 0.0
        _SmoothnessRemapMax("Smoothness RemapMax", Float) = 1.0
       
	    _OcclusionStrength("AOStrength", Range(0.0, 1.5)) = 1.0      
        [HideInInspector]_GlossMapScale("Smoothness Scale", Range(0.0, 1.0)) = 1.0
        // _SmoothnessTextureChannel("Smoothness texture channel", Float) = 0
        [HideInInspector][Tex(true,_SpecColor)]_SpecGlossMap("Specular", 2D) = "white" {}
        [HideInInspector]_SpecColor("Specular", Color) = (0.2, 0.2, 0.2)
        _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Scale",  Float) = 1.0

		//TotalMask
		_Mask("Mask", 2D) = "white" {}
		//IBL
        [HDR]_IBLMap("IBL Map", Cube) = "black" {}
        _IBLMapColor("IBL Map Color", Color) = (1,1,1,1) 
		_IBLMapIntensity("IBL Map Intensity", Range(0.0, 3.0)) = 0.1
		_IBLMapPower("IBL Map Power",  Range(0,5)) = 1.0
        _IBLMapRotateX("IBL Map Rotate", Range(0,360)) = 0
		_IBLMapRotateY("IBL Map Rotate", Range(0,360)) = 0 
		_IBLMapRotateZ("IBL Map Rotate", Range(0,360)) = 0   
   
        
		//Matcap
		_MatcapMap("MatcapMap", 2D) = "black" {}
		_MatcapAngle("Matcap Angle", Range(0,360)) = 0
		_MatcapStrength("Matcap Strength", Range(0,10)) = 0  
		_MatcapPow("Matcap Pow", Range(0.01,10)) = 1  
		_MatcapColor("Matcap Color", Color) = (1,1,1,1)


		//MatcapAdd
		_MatcapAddMap("MatcapMapAdd", 2D) = "black" {}
		_MatcapAddAngle("Matcap AngleAdd", Range(0,360)) = 0
		_MatcapAddStrength("Matcap Strength Add", Range(0,10)) = 0  
		_MatcapAddPow("Matcap Pow Add", Range(0.01,10)) = 1  
		_MatcapAddColor("Matcap Color Add", Color) = (1,1,1,1) 
		

		//Streamer
        [Toggle] _Streamer("Streamer",Int) = 0
        _StreamerTex("Texture",2D) = "white"{}
        _StreamerMask("Mask",2D) = "white"{}
        _StreamerNoise("Noise",2D) = "white"{}
        _StreamerNoiseSpeed("NoiseSpeed",Float) = 1.0
        _StreamerColor("Color",Color) = (1,1,1,1)
        _StreamerAlpha("Alpha",Float) = 1
		_StreamerModexyz("_StreamerModexyz", Vector) = (1,1,1,0)
        _StreamerScrollX("speed X", Float) = 1.0
        _StreamerScrollY("speed Y", Float) = 0.0
        [KeywordEnum(UVPos,ScreenPos)] _StreamerChannel("StreamerType",Float) = 0
		
		
		
		[Toggle]_SpecularHighlights("Specular Highlights", Float) = 1.0
        [Toggle]_EnvironmentReflections("Environment Reflections", Float) = 1.0    
        [HDR]_EmissionColor("EmissionColor", Color) = (0,0,0)
        _EmissionTexture("EmissionTex", 2D) = "white" {}
        _EmissionScale("Emission Scale", Float) = 1
        _FoldoutSurfaceEnd("_FoldoutSurfaceEnd", Float) = 0.0
        [Space(10)]
        [SplitLine(6,18)][Space(10)]
        [Header(NPR)]
        _FoldoutAdvance("Advanced", Float) = 0.0
        [Space(30)]
        _SpecularIntensity2("CelMask Specular Intensity", Float) = 1.1
        _MedBrushStrength("Med Tone Brush Strength", Range(0,1)) = 0
        _ShadowBrushStrength("Shadow Brush Strength", Range(0,1)) = 0
        _ReflBrushStrength("Reflect Brush Strength", Range(0,1)) = 0
        [Space(10)]
        [HDR]_MedColor("Med Tone Color", Color) = (1,1,1,1)
        _MedThreshold("Med Tone Threshold", Range(0,1)) = 1
        _MedSmooth("Med Tone Smooth", Range(0,0.5)) = 0.25
        [Space(10)]
        [HDR]_ShadowColor("Shadow Color", Color) = (0,0,0,1)
        _ShadowThreshold("Shadow Threshold", Range(0,1)) = 0.7
        _ShadowSmooth("Shadow Smooth", Range(0,0.5)) = 0.2
        [Space(10)]
        [HDR]_ReflectColor("Reflect Color", Color) = (0.02,0.02,0.02,0)
        _ReflectThreshold("Reflect Threshold", Range(0,1)) = 0.4
        _ReflectSmooth("Reflect Smooth", Range(0,0.5)) = 0.15
		[Space(10)]
        _GIIntensity("GI Intensity", Range(0,2)) = 0
        _GGXSpecular("GGX Specular", float) = 1
		
		_SpecularColor("Specular Color", Color) = (1, 1, 1, 1)
		_SpecularIntensity("Specular Intensity", float) = 1
        _SpecularLightOffset("Specular Light Offset", Vector) = (0,0,0,0)
        _SpecularThreshold("Specular Threshold", Range(0.1,2)) = 0.5
        _SpecularSmooth("Specular Smooth", Range(0,0.5)) = 0.5
		[Space(10)]

		[Toggle]_FastSSS("FastSSS", float) = 0.0

		[Toggle]_SpecularSecond("SpecularSecond", float) = 0.0
		_SpecularMaskMap("SpecularMaskMap(R)RimMask(G)", 2D) = "white" {}
		_SpecularSecondColor("SpecularSecond Color", Color) = (1, 1, 1, 1)
		_SpecularSecondIntensity("SpecularSecond Intensity", float) = 1
		_SpecularSecondThreshold("SpecularSecond Threshold", Range(0.1, 50)) = 0.5
        _SpecularSecondLightOffset("SpecularSecond Light Offset", Vector) = (1.95,-1.95,0,0) 
        [Space(10)]
        _DirectionalFresnel("Directional Fresnel", float) = 0
        [HDR]_FresnelColor("Fresnel Color", Color) = (1,0,0,0)
        _fresnelOffset("Fresnel Light Offset", Vector) = (0,0,0,0)
        _FresnelThreshold("Fresnel Threshold", Range(0,1)) = 0.5
        _FresnelSmooth("Fresnel Smooth", Range(0,0.5)) = 0.5
        _FresnelIntensity("Fresnel Intensity", float) = 1
		[Toggle]_FresnelFit("FresnelFit",Float) = 1.0
		[Toggle]_FresnelMetallic("FresnelMetallic",Float) = 1.0


		[Space(10)]
		[Toggle] _EffectRim("EffectRim", Float) = 0
		[Toggle] _EffectRimAlpha("EffectRimAlpha", Float) = 0

		[Toggle]_EffectRimUV("_EffectRimUV", Float) = 0
		_EffectMap("EffectMap", 2D) = "white" {}
		[HDR]_EffectColor ("EffectColor", Color) = (1,0.5,0,1)
		_EffectIntensity("EffectIntensity",Float) = 0.0
		_EffectMap_Uspeed("EffectMap_Uspeed",Float) = 0.0
		_EffectMap_Vspeed("EffectMap_Vspeed",Float) = 0.0

		_EffectMapNoise("EffectMapNoise", 2D) = "white" {}
		_EffectMapNoiseIntensity("EffectMapNoiseIntensity",Float) = 0.0
		_EffectMapNoise_Uspeed("EffectMapNoise_Uspeed",Float) = 0.0
		_EffectMapNoise_Vspeed("EffectMapNoise_Vspeed",Float) = 0.0


		[HDR]_RimColor ("RimColor", Color) = (1,0.5,0,1)
        _RimPower ("RimPower", Range(0.01,10)) = 1.0
        _RimIntensity ("RimIntensity", Float) = 0.0
        _RimStart ("RimStart", Range(0,1)) = 0.0
        _RimEnd ("RimEnd", Range(0,1)) = 1.0
		_RimMask("RimMask", 2D) = "white" {}
		_RimMask_Uspeed("EffectMap_Uspeed",Float) = 0.0
		_RimMask_Vspeed("EffectMap_Vspeed",Float) = 0.0

		[Toggle] _EffectRimIsAdd("EffectRimIsAdd", Float) = 1
		

		[Space(10)]
		_RimColor ("RimColor", Color) = (1,0.5,0,1)
        _RimPower ("RimPower", Range(1,10)) = 1.0
        _RimIntensity ("RimIntensity", Float) = 0.0
        _RimStart ("RimStart", Range(0,1)) = 0.0
        _RimEnd ("RimEnd", Range(0,1)) = 1.0
		_RimOffset("Rim Light Offset", Vector) = (0,0,0,0)
        

		[Toggle] _Overlay("Hit Color",Int) = 0
		[KeywordEnum(Rim,Albedo)] _HitColorChannel("HitColorType",Float) = 0
        [HDR]_HitColor("Color",Color) = (1,1,1,1)
        _HitMultiple("Multiple",Float) = 1
        _HitRimPower("Rim Power", Range(0.01, 10)) = 0.01
        _HitRimSpread("Rim Spread", Range(-15, 4.99)) = 0.01
		_HitRimOffset("Rim Offset", Vector) = (-1,0,0,0)
		_HitRimSmooth("Rim Smooth", Range(0,0.5)) = 0.3
		_OverlayColor("Color",Color) = (1,1,1,1)
		_OverlayMultiple("Multiple",Float) = 1
        _OverlayRimPower("Rim Power", Range(0.01, 10)) = 0.01
        _OverlayRimSpread("Rim Spread", Range(0, 4.99)) = 0.01

        [Toggle]_AdjustHSV("Adjust HSV", Float) = 0.0
		_AdjustHue("Hue", Range(0,360)) = 0.0
		_AdjustSaturation("Saturation", Range(0,1.5)) = 1.0
		_AdjustValue("Value", Range(0,1.5)) = 1.0

		[Toggle] _OriginalColor("OriginalColor",Float) = 0.0
		[Toggle] _Contrast("AdjustContrast", Float) = 0.0
		_ContrastScale("ContrastSacle",Range(0,3)) = 1

		_ShadowScale("Shadow Scale",Range(0,1.35)) = 0
		_ShadowMaskMap("ShadowMaskMap(R)", 2D) = "white" {}
    	[Toggle]_UseFacing("UseFacing",Float) = 0.0
		
		_u_ToneWeight("u_ToneWeight", Range(0, 3)) = 0
		_u_WhitePoint("u_WhitePoint", Range(0, 3)) = 1

		[Space(10)]
        // Editmode props
        [HideInInspector] _QueueOffset("Queue offset", Float) = 0.0
        // ObsoleteProperties
        [HideInInspector] _MainTex("BaseMap", 2D) = "white" {}
        [HideInInspector] _Color("Base Color", Color) = (1, 1, 1, 1)
        [HideInInspector] _GlossMapScale("Smoothness", Float) = 0.0
        [HideInInspector] _Glossiness("Smoothness", Float) = 0.0
        [HideInInspector] _GlossyReflections("EnvironmentReflections", Float) = 0.0
    }
	
    SubShader
    {
        
		Tags { "RenderType" = "Opaque"  "Queue"= "Geometry" }
		LOD 150
	
        //ForwardBase
		
        Pass
        {
		    Name "FORWARD"
			Tags {"LightMode" = "ForwardBase"}
			Blend[_SrcBlend][_DstBlend]
		    ZWrite [_ZWrite]	
		    Cull [_CullMode]
			//Lighting On
            CGPROGRAM
            
			#pragma target 3.0
			
			//#pragma shader_feature S_BOOL
			#include "UnityCG.cginc"
			
			#include "AutoLight.cginc"
			#include "FishCgincBase.cginc"
			#include "LightingCustomBase.cginc"
            #include "UnityShaderVariables.cginc"
            		
            // Material Keywords
            #pragma shader_feature _NORMALMAP
            #pragma shader_feature _ALPHATEST_ON
            #pragma shader_feature _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _HITCOLORCHANNEL_RIM _HITCOLORCHANNEL_ALBEDO
			#pragma shader_feature _OVERLAY_ON
			#pragma shader_feature _STREAMER_ON
      
            #pragma shader_feature _ENVIRONMENTREFLECTIONS_OFF         
			#pragma shader_feature _EFFECTRIM_ON 
			
            #pragma shader_feature _MAER_ON

			//#pragma multi_compile _ALPHATEST_ON  _ALPHAPREMULTIPLY_ON     
            #pragma multi_compile DIRECTIONAL POINT
            #pragma multi_compile SHADOWS_OFF SHADOWS_SCREEN /*SHADOWS_DEPTH*/
            // #pragma multi_compile LIGHTPROBE_SH
            //#pragma multi_compile_fwdbase

			#pragma vertex vertForwardBase
            #pragma fragment frag

			float _RimPower,_RimIntensity,_RimStart,_RimEnd;
			float4 _RimColor;
			float3 _RimOffset;
			
			

			// Gamma��sRGB��ת���Կռ�
			half3 Gamma22ToLinear(half3 color) {
				//return pow(color, 2.2);
				return color <= 0.04045? color / 12.92 : pow((color + 0.055) / 1.055, 2.4);
			}

			// fresnel
			float3 FresnelCore(float3 normal, float3 viewDir, float3 rimColor, float power, float intensity, float start, float end,float3 offset)
            {
                float3 N = normalize(normal);
                float3 V = normalize(viewDir)+offset;
                float NdotV = 1 - saturate(dot(N, V));
                
                float range = smoothstep(start, end, NdotV);
                float fresnel = intensity * pow(range, power);
                return saturate(rimColor * fresnel);
            }
			
			float3 AdjustNormalDirection(float3 normalDirection,float facing)
			{
				return (facing<0.0)?-normalDirection:normalDirection;
			}
    
			float3 DirectStylizedBDRF(BRDFData brdfData,  float occlusion,float3 normalWS, float3 lightDirectionWS, float3 viewDirectionWS, float2 uv)
            {
            	//#ifndef _SPECULARHIGHLIGHTS_OFF    
                 
                float3 halfDir = SafeNormalize(float3(lightDirectionWS) + float3(viewDirectionWS));                  				
                float NoH = saturate(dot(normalWS, halfDir));
                float LoH = saturate(dot(lightDirectionWS, halfDir));
                float d = NoH * NoH * brdfData.roughness2MinusOne + 1.00001f;
                float LoH2 = LoH * LoH;                
                float specularTerm = brdfData.roughness2 / ((d * d)* max(0.1h, LoH2) * brdfData.normalizationTerm);     
                //half specularTerm = brdfData.roughness2 / ((d * d)* max(0.1h, LoH2) * 1);   
                 //specularTerm = pow(specularTerm,_SpecularThreshold);  
                //return half3(specularTerm.xxx);      
                #if defined (SHADER_API_MOBILE) || defined (SHADER_API_SWITCH)
                    specularTerm = specularTerm - HALF_MIN;
                    specularTerm = clamp(specularTerm, 0.0, 100.0); // Prevent FP16 overflow on mobiles               
                #endif	
                	
                //half4 celTex = SampleCelMap(uv);
                float3 color = float3(1,1,1);
                float specularStylize = 0; 
                specularTerm =  pow(specularTerm ,_SpecularThreshold+ _SpecularSmooth);
                //return specularTerm;
                specularStylize = LinearStep( _SpecularThreshold - _SpecularSmooth, _SpecularThreshold + _SpecularSmooth, specularTerm );
                    
                specularTerm = lerp(specularStylize, specularTerm, _GGXSpecular);
                    
                color = specularTerm* max(0,_SpecularIntensity*_SpecularColor.rgb)* brdfData.specular;
                  

                //return color;                  
               //color += (brdfData.diffuse*occlusion);   
			   

                return lerp(brdfData.diffuse* occlusion,color += (brdfData.diffuse*occlusion),_SpecularHighlights);
            }


			float DirectBRDFSpecular(BRDFData brdfData, float3 normalWS, float3 lightDirectionWS, float3 viewDirectionWS)
			{
				float3 lightDirectionWSFloat3 = float3(lightDirectionWS);
				float3 halfDir = SafeNormalize(lightDirectionWSFloat3 + float3(viewDirectionWS));

				float NoH = saturate(dot(float3(normalWS), halfDir));
				float LoH = float(saturate(dot(lightDirectionWSFloat3, halfDir)));

				// GGX Distribution multiplied by combined approximation of Visibility and Fresnel
				// BRDFspec = (D * V * F) / 4.0
				// D = roughness^2 / ( NoH^2 * (roughness^2 - 1) + 1 )^2
				// V * F = 1.0 / ( LoH^2 * (roughness + 0.5) )
				// See "Optimizing PBR for Mobile" from Siggraph 2015 moving mobile graphics course
				// https://community.arm.com/events/1155

				// Final BRDFspec = roughness^2 / ( NoH^2 * (roughness^2 - 1) + 1 )^2 * (LoH^2 * (roughness + 0.5) * 4.0)
				// We further optimize a few light invariant terms
				// brdfData.normalizationTerm = (roughness + 0.5) * 4.0 rewritten as roughness * 4.0 + 2.0 to a fit a MAD.
				float d = NoH * NoH * brdfData.roughness2MinusOne + 1.00001f;
				float d2 = float(d * d);

				float LoH2 = LoH * LoH;
				float specularTerm = brdfData.roughness2 / (d2 * max(float(0.1), LoH2) * brdfData.normalizationTerm);

				// On platforms where half actually means something, the denominator has a risk of overflow
				// clamp below was added specifically to "fix" that, but dx compiler (we convert bytecode to metal/gles)
				// sees that specularTerm have only non-negative terms, so it skips max(0,..) in clamp (leaving only min(100,...))
				#if defined (SHADER_API_MOBILE) || defined (SHADER_API_SWITCH)
					specularTerm = specularTerm - HALF_MIN;
					specularTerm = clamp(specularTerm, 0.0, 100.0); // Prevent FP16 overflow on mobiles
				#endif

				return specularTerm;
			}


		    half3 LightingStylizedPhysicallyBased(BRDFData brdfData, float3 radiance, float3 lightColor,  float3 lightDirectionWS, float occlusion, float3 normalWS, float3 viewDirectionWS,float2 uv)
            {	
				float3 SelfLightDirAdd = _SelfLightDir.xyz;
				//float3 SpecularLightOffset = _SelfLight > 0 ? SelfLightDirAdd.xyz : _SpecularLightOffset.xyz;
				float3 SpecularLightOffset = lerp(_SpecularLightOffset.xyz, SelfLightDirAdd.xyz, saturate(_SelfLight));
                return  DirectStylizedBDRF(brdfData,occlusion, normalWS, normalize(lightDirectionWS + SpecularLightOffset), viewDirectionWS,uv) * radiance ;
            }
			float remap(float x, float t1, float t2, float s1, float s2)
			{
				return (x - t1) / (t2 - t1) * (s2 - s1) + s1;
			}

            float3 LightingStylizedPhysicallyBased(BRDFData brdfData, float3 radiance, Light light, float occlusion, float3 normalWS, float3 viewDirectionWS,float2 uv)
            {			
                return LightingStylizedPhysicallyBased(brdfData, radiance, light.color, light.direction, occlusion, normalWS, viewDirectionWS,uv);
            }
				
			float3 EnvironmentBRDFSpecular(BRDFData brdfData, float fresnelTerm)
			{
				float surfaceReduction = 1.0 / (brdfData.roughness2 + 1.0);
				return float3(surfaceReduction * lerp(brdfData.specular, brdfData.grazingTerm, fresnelTerm));
			}
			
			float3 EnvironmentBRDFCustom(BRDFData brdfData, float3 radiance, float3 indirectDiffuse, float3 indirectSpecular, float fresnelTerm)  
            {
                float3 c = indirectDiffuse * brdfData.diffuse * _GIIntensity;
                //return brdfData.diffuse;
                float surfaceReduction = 1.0 / (brdfData.roughness2 + 1.0);

				
                //return c += surfaceReduction *indirectSpecular *lerp(brdfData.specular * radiance, brdfData.grazingTerm, fresnelTerm)*fresnelTerm;
                c += surfaceReduction * indirectSpecular * lerp(brdfData.specular * radiance, brdfData.grazingTerm, fresnelTerm)*fresnelTerm;   
				
				
							
                return c;
				
            }
			float3 StylizedGlobalIllumination(BRDFData brdfData, float3 radiance,  float occlusion, float3 normalWS, float3 viewDirectionWS, float metallic, float ndotl,float4 vertexcolor,float2 uv)
            {
                            
                float3 reflectVector = reflect(-viewDirectionWS, normalWS);	  
                float3 indirectDiffuse =  occlusion;			
                float3 indirectSpecular = GlossyEnvironmentReflection(reflectVector, brdfData.perceptualRoughness, occlusion) * metallic;  			        
                return EnvironmentBRDFCustom(brdfData, radiance, indirectDiffuse, indirectSpecular, 0);
            }
			float3 StylizedGlobalIlluminationFresnel(BRDFData brdfData, float3 radiance,  float occlusion, float3 normalWS, float3 viewDirectionWS, float metallic, float ndotl,float4 vertexcolor,float2 uv,float texAlpha)
            {
                
                float3 reflectVector = reflect(-viewDirectionWS, normalWS);	
                //reflectVector = rotateVectorAboutY(_IBLMapRotate, reflectVector);	
				float FresnelIntensity = lerp(0,_FresnelIntensity,texAlpha);
                float fresnelTerm = LinearStep( _FresnelThreshold - _FresnelSmooth, _FresnelThreshold + _FresnelSmooth, 1.0 - saturate(dot(normalWS, viewDirectionWS))) * max(0,FresnelIntensity*vertexcolor.r) * ndotl;
				float fresnelFit = LinearStep( _FresnelThreshold - _FresnelSmooth, _FresnelThreshold + _FresnelSmooth, 1.0 - saturate(dot(normalWS, GetViewForwardDir(_fresnelOffset)))) * max(0,FresnelIntensity*vertexcolor.r) * ndotl;
				fresnelTerm = lerp(fresnelTerm,fresnelFit,_FresnelFit);
				//fresnelTerm = lerp(fresnelTerm,fresnelFit,_FresnelFit)*tex2D(_Mask,uv).a;        
                float3 indirectDiffuse =  occlusion;		
                //float3 indirectDiffuse = _OcclusionStrength;		
                float3 indirectSpecular = GlossyEnvironmentReflection(reflectVector, brdfData.perceptualRoughness, occlusion) * lerp(1,metallic,_FresnelMetallic); 
                //return indirectDiffuse; 				        
                return EnvironmentBRDFCustom(brdfData, radiance, indirectDiffuse, indirectSpecular, fresnelTerm);
            }
			float3 CalculateRadiance(Light light, float3 normalWS, float3 brush, float3 brushStrengthRGB,float2 uv)
            {
                float NdotL = dot(normalWS, light.direction);
                float halfLambertMed = NdotL * 0.5 + 0.5;
                float halfLambertShadow = halfLambertMed;
                float halfLambertRefl = halfLambertMed;
                float smoothMedTone = LinearStep( _MedThreshold - _MedSmooth, _MedThreshold + _MedSmooth, halfLambertMed);
                float3 MedToneColor = lerp(_MedColor.rgb , 1 , smoothMedTone);
                float smoothShadow = LinearStep ( _ShadowThreshold - _ShadowSmooth, _ShadowThreshold + _ShadowSmooth, halfLambertShadow );
                float3 ShadowColor = lerp(_ShadowColor.rgb, MedToneColor, smoothShadow );   //
                float smoothReflect = LinearStep( _ReflectThreshold - _ReflectSmooth, _ReflectThreshold + _ReflectSmooth, halfLambertRefl);
                float3 ReflectColor = lerp(_ReflectColor.rgb , ShadowColor , smoothReflect);
                float3 radiance = _LightColor0.rgb * ReflectColor;//lightColor * (lightAttenuation * NdotL);
               // return smoothMedTone.xxx;
                return radiance;
            }
			float4 UniversalFragmentStylizedPBR(InputData inputData, float3 albedo, float metallic, float3 specular, float smoothness, float occlusion, float3 emission, float alpha, float2 uv,float4 vertexcolor,float isFront,float _UseFacing )      //uv 
            {
                BRDFData brdfData; 
				InitializeBRDFData(albedo, metallic, specular, smoothness, alpha, brdfData); 
				           
				Light mainLight = GetMainLight();
				float3 normalWS = normalize(inputData.normalWS);
				// if(_UseFacing)
            	// {
            	// 	 normalWS = AdjustNormalDirection(inputData.normalWS,isFront);
            	// }
				normalWS = lerp(normalWS,AdjustNormalDirection(normalize(inputData.normalWS),isFront),_UseFacing);
				
                float3 radiance =CalculateRadiance(mainLight, normalWS, 0.5, float3(0, 0, 0),uv);
				
				
                float ndotl = LinearStep( _ShadowThreshold - _ShadowSmooth, _ShadowThreshold + _ShadowSmooth, dot(mainLight.direction, normalWS) * 0.5 + 0.5);
				
                float3 color = StylizedGlobalIllumination(brdfData,radiance, occlusion, normalWS, inputData.viewDirectionWS + _fresnelOffset.xyz, metallic, lerp(1,ndotl, _DirectionalFresnel),vertexcolor,uv);
				 
                color += LightingStylizedPhysicallyBased(brdfData, radiance, mainLight,occlusion, normalWS, inputData.viewDirectionWS,uv);               
				//float3 color2 = LightingStylizedPhysicallyBased(brdfData, radiance, mainLight,occlusion, inputData.normalWS, inputData.viewDirectionWS,uv);                    
                //return float4(color2, 1);
                color.xyz = color.xyz + emission;
                float nd = dot(mainLight.direction, normalWS);
                return float4(color, alpha);
            }
			float4 UniversalFragmentStylizedPBRFresnel(InputData inputData, float3 albedo, float metallic, float3 specular, float smoothness, float occlusion, float alpha, float2 uv,float4 vertexcolor,float texAlpha) 
            {
                BRDFData brdfData;    
				InitializeBRDFData(float4(0,0,0,0), 0, 0, 0, alpha, brdfData); 
				
				
                float ndotl = LinearStep( _ShadowThreshold - _ShadowSmooth, _ShadowThreshold + _ShadowSmooth, 1);
				
                float3 color = StylizedGlobalIlluminationFresnel(brdfData,float3(0,0,0), occlusion, inputData.normalWS, inputData.viewDirectionWS + _fresnelOffset.xyz, metallic, lerp(1,ndotl, _DirectionalFresnel),vertexcolor,uv,texAlpha);
                return float4(color, 0);
            }
			float4 CustomGetIndirectSpec(float perceptualRoughness,float3 reflUVW)
			{
				float mip = (1.7 - perceptualRoughness*0.7)*perceptualRoughness*8;
				float4 R = float4(reflUVW, mip);
				
				
				float4 reflectColor = texCUBElod(_IBLMap, R);
				reflectColor = pow(reflectColor,_IBLMapPower);
				

				//reflectColor.xyz = pow(reflectColor, float3(2.2,2.2,2.2));//decodeHDR
			// reflectColor.xyz = reflectColor.xyz*reflectColor.w;

				//We need this for shadow receving
				//UNITY_TRANSFER_LIGHTING(o, v.uv1);
				return reflectColor;
			}

	
			float4 frag (v2f i) : SV_Target
            {
               
				float3 worldPos = float3(i.TtoW0.w, i.TtoW1.w, i.TtoW2.w);
				
				float3 viewDir = normalize(_WorldSpaceCameraPos - worldPos);
				#ifdef USING_DIRECTIONAL_LIGHT
					float3 lightDir = normalize(_WorldSpaceLightPos0);// _WorldSpaceLightPos0;
					//float3 lightDir = normalize( UnityWorldSpaceLightDir(worldPos));
				#else
					float3 lightDir = _WorldSpaceLightPos0 - worldPos;
					//float3 lightDir =normalize(i.lightPos - i.worldPos);
				#endif
				
				
				//Mask
				float4 TotalMask = tex2D(_Mask, i.uv);
				half2 specularMaskMap = tex2D(_SpecularMaskMap, i.uv).rg;

				UNITY_LIGHT_ATTENUATION(atten, i, i.vertex.xyz);				
				float3 lightColor = _LightColor0.rgb*atten;
				float3 halfDir = normalize(lightDir + viewDir);			
				float3 packedNormal = UnpackNormal(tex2D(_BumpMap, i.uv));
				packedNormal =normalize(float3(dot(i.TtoW0.xyz, packedNormal), dot(i.TtoW1.xyz, packedNormal), dot(i.TtoW2.xyz, packedNormal)));			
				float3 worldNormal = normalize(float3(i.TtoW0.z, i.TtoW1.z, i.TtoW2.z));
				SurfaceData surfaceData;
				InitializeStandardLitSurfaceData(i.uv, surfaceData);				
				float3 specColor = lerp (kDieletricSpec.rgb, surfaceData.albedo, surfaceData.metallic);
                //half oneMinusReflectivity = (1 - surfaceData.metallic)*kDieletricSpec.a;
                //albedo * oneMinusReflectivity;
            	
				#ifdef _NORMALMAP     
                //half3 packedNormal =  UnpackNormal(tex2D(_BumpMap, i.uv)).xyz;
                float3 normalWS = packedNormal; 
                normalWS = UnpackNormalScale(normalWS,_BumpScale);   
				    
                #else               
                float3 normalWS = normalize(float3(i.TtoW0.z, i.TtoW1.z, i.TtoW2.z));

                #endif
            	//AdjustNormalDirection
            	

            	 if(_UseFacing)
            	{
            		normalWS=AdjustNormalDirection(normalWS.xyz,i.facing);
            	}
            	
            	
				
                float3 viewWS = viewDir;
                float ndv = saturate(dot(normalWS, viewWS));
                float ndotl = saturate(dot(normalWS,lightDir));
            	
            	
            	
                float3 viewDirO = lerp(-viewWS,float3(0,0,1),UNITY_MATRIX_P[3][3]);
				//half3 viewDirO = -viewWS;
                //return ndv.xxxx;
                float3 reflectVector = reflect(viewDirO, normalWS);

				half2 shadowScale = tex2D(_ShadowMaskMap, i.uv).rg;
				
				//BlinnPhong
				float3 lDir = _SpecularSecondLightOffset;
				float3 vDir = normalize(_WorldSpaceCameraPos - worldPos);
				float3 rDir = normalize(reflect(-lDir, normalWS));
				float4 BlinnPhongSpecular = max(0.0,dot(vDir,rDir));
				BlinnPhongSpecular = pow(BlinnPhongSpecular,_SpecularSecondThreshold*_Smoothness);
				BlinnPhongSpecular = BlinnPhongSpecular*_SpecularSecondColor*_SpecularSecondIntensity * specularMaskMap.r;


                InputData inputData;
                InitializeInputData(i, normalize(surfaceData.normalTS), inputData);
				float3 reflectVectorIBL = rotateVectorMatrix(_IBLMapRotateX, _IBLMapRotateY,_IBLMapRotateZ,reflectVector);
				float3 reflectVectorSelfDir = rotateVectorMatrix(_IBLRotate.x, _IBLRotate.y,_IBLRotate.z,reflectVector);
				//reflectVector = _isIBLRotateActive > 0 ? reflectVectorSelfDir : reflectVectorIBL;
				reflectVector = lerp(reflectVectorIBL, reflectVectorSelfDir, saturate(_isIBLRotateActive));
				//reflectVector = rotateVectorMatrix(_IBLMapRotateX, _IBLMapRotateY,_IBLMapRotateZ,reflectVector);
            	


				//float4 cubemap = texCUBE(_IBLMap, reflectVector);

				
	            float3 reflectColor = CustomGetIndirectSpec((1 - perceptualRoughness),reflectVector); 
                //half3 reflectColor = GlossyEnvironmentReflection(reflectVector, (1-surfaceData.smoothness), surfaceData.occlusion);            
                float3 envBRDF = EnvBRDFApprox(specColor, perceptualRoughness, saturate(ndv));



				float3 cubeMapColor = reflectColor.xyz*1*_IBLMapColor.xyz*_IBLMapIntensity*surfaceData.metallic;
				
				float4 baseTex = tex2D(_BaseMap, i.uv);
            	

								
                //cubeMapColor = clamp(0,1,cubeMapColor);
                //return half4(cubeMapColor.xyz,1);
				
                
				float4 color = UniversalFragmentStylizedPBR(inputData, surfaceData.albedo, surfaceData.metallic, surfaceData.specular, 
				surfaceData.smoothness, surfaceData.occlusion, surfaceData.emission, surfaceData.alpha, i.uv,i.color,i.facing,_UseFacing);
            	//return color;



				float3 iblcolor = color.rgb + cubeMapColor;
				color.rgb = lerp(color.rgb ,iblcolor ,TotalMask.r);
				//color.rgb+=cubeMapColor;

				
				#ifdef _OVERLAY_ON
					
					#if _HITCOLORCHANNEL_RIM
						float3 normalWSOffset = float3(normalWS.x + _HitRimOffset.x, normalWS.y + _HitRimOffset.y, normalWS.z + _HitRimOffset.z);
						float ndvOffset = saturate(dot(normalWSOffset, normalWS));
						ndvOffset= saturate(1- ndvOffset);
						float rimHit = ndvOffset;
						float hitAlpha = saturate(pow(rimHit, 5.0 - _HitRimSpread) * _HitRimPower * _HitMultiple * i.color.r);
						//hitAlpha += LinearStep( 0.5 - _HitRimSmooth ,0.5 + _HitRimSmooth , ndvOffset) * max(0,hitRimPower * i.color.r)* ndotl ;
						//hitAlpha += LinearStep( _HitRimSpread - _HitRimSmooth , _HitRimSpread + _HitRimSmooth , 1.0 - ndvOffset * max(0,_HitRimPower * i.color.r)*ndotl);
						//half fresnelTerm = LinearStep( _FresnelThreshold - _FresnelSmooth, _FresnelThreshold + _FresnelSmooth, 1.0 - saturate(dot(normalWS, viewDirectionWS))) * max(0,_FresnelIntensity*vertexcolor.r) * ndotl;
						color.rgb = lerp(color.rgb, _HitColor.rgb* _HitMultiple, hitAlpha);
					
					#endif
					

					#if _HITCOLORCHANNEL_ALBEDO
						color.rgb *=  _HitColor.rgb*_HitMultiple;
					#endif
				#else 
					#if _HITCOLORCHANNEL_RIM
						float3 normalWSOffset = float3(normalWS.x + _HitRimOffset.x, normalWS.y + _HitRimOffset.y, normalWS.z + _HitRimOffset.z);
						float ndvOffset = saturate(dot(normalWSOffset, normalWS));
						ndvOffset= saturate(1- ndvOffset);
						float rimOverlay = ndvOffset;
						float OverlayhitAlpha = saturate(pow(rimOverlay, 5.0 - _HitRimSpread) * _HitRimPower * _OverlayMultiple*i.color.r);
						//OverlayhitAlpha += LinearStep( 0.5 - _HitRimSmooth ,0.5 + _HitRimSmooth , ndvOffset) * max(0,hitRimPower * i.color.r)* ndotl ;
						color.rgb = lerp(color.rgb, _OverlayColor.rgb* _OverlayMultiple,OverlayhitAlpha );
					
					#endif

					#if _HITCOLORCHANNEL_ALBEDO
						color.rgb*= _OverlayColor.rgb *_OverlayMultiple;
					#endif
				#endif

				
				color.rgb = lerp(color, color * baseTex.rgb * _BaseColor.rgb * _ColorIntensity, surfaceData.metallic);
				
				
				//Matcap
				float cosmc = cos(radians(_MatcapAngle));
				float cosmr = cos(radians(_MatCapRotate));
				//cosmc = _isMatCapRotateActive > 0 ? cosmr : cosmc;
				cosmc = lerp(cosmc, cosmr, saturate(_isMatCapRotateActive));
				float sinmc= sin(radians(_MatcapAngle));
				float sinmr = sin(radians(_MatCapRotate));
				//sinmc = _isMatCapRotateActive > 0 ? sinmr : sinmc;
				cosmc = lerp(sinmc, sinmr, saturate(_isMatCapRotateActive));
				float2 uv_Matcap = mul((mul(UNITY_MATRIX_V, float4(normalWS, 0.0)).xyz + 0.5).xy - float2(0.5, 0.5), 
								float2x2(cosmc, -sinmc, sinmc, sinmc)) + float2(0.5, 0.5);
				float4 matcapColor =tex2D(_MatcapMap, uv_Matcap) * _MatcapColor * _MatcapStrength*5;
				//mask
				//float matcapMask = TotalMask.g;

				float matcapIntensity = clamp(pow(abs(matcapColor.x), _MatcapPow),0,1);
				matcapColor *= matcapIntensity;
				//final blend
				color.rgb = lerp(color.rgb, color.rgb + color.rgb * matcapColor ,TotalMask.g);
				




				//MatcapAdd
				float cosmcAdd = cos(radians(_MatcapAddAngle));
				float sinmcAdd= sin(radians(_MatcapAddAngle));

				//mask
				//float matcapMaskAdd = TotalMask.b;

				//float matcapMaskAdd = tex2D(_MatcapMask, i.uv).g;

				float2 uv_MatcapAdd = float2(0,0);
				//color.rgb = albedoCol;
					
		        float4x4 viewMatrix = UNITY_MATRIX_V;
            	float3 cameraForward = -viewMatrix[2].xyz;
				cameraForward = normalize(cameraForward);

				float3 viewUpDir = mul(UNITY_MATRIX_I_V,float4(0,1,0,0)).xyz;
				float3 cameraRight = normalize(cross(viewUpDir,cameraForward));
				float3 cameraUP = normalize(cross(cameraForward,cameraRight));

				uv_MatcapAdd = mul(float3x3(cameraRight,cameraUP,cameraForward),normalWS).xy;
				uv_MatcapAdd = mul(float2x2(cosmcAdd, -sinmcAdd, sinmcAdd, cosmcAdd),uv_MatcapAdd* float2(0.495, 0.495));
				uv_MatcapAdd += float2(0.5, 0.5);
					
				float4 matcapColorAdd =tex2D(_MatcapAddMap, uv_MatcapAdd) * _MatcapAddColor; //* _MatcapStrength*5;
					
				float matcapAlAdd = clamp(pow(abs(matcapColorAdd.x), _MatcapAddPow),0,1)*_MatcapAddStrength;
				matcapAlAdd *= TotalMask.b;
				float3 matcapFinalAdd = lerp(color.rgb, color.rgb+matcapColorAdd.rgb,matcapAlAdd);
					
				color.rgb = matcapFinalAdd;



				
				//Contrast
				float3 avgColor = float3(0.5, 0.5, 0.5)*_ContrastScale;
				color.rgb = lerp(lerp(float3(0.5, 0.5, 0.5), color.rgb, 1.1),color.rgb,_OriginalColor);
				// color.rgb = lerp(float3(0.5, 0.5, 0.5), color.rgb, 1.1);
				color.rgb = lerp(color.rgb,lerp(avgColor, color.rgb, 1.1),_Contrast);
	   			//color.rgb = lerp(avgColor, color.rgb, 1.1);

                //return baseTex;
                float outAlpha = Alpha(baseTex, _BaseColor, _Cutoff);             	           
               // color.rgb = MixFog(color.rgb, inputData.fogCoord);
                //color.a = OutputAlpha(color.a);               
                color.a = outAlpha*color.a*_Alpha;
				

				//SpecularSecond
				color = lerp(color,color +(color*BlinnPhongSpecular),_SpecularSecond);


				//return shadow.xxxx;			
				float shadow = SHADOW_ATTENUATION(i);
				// shadow 
				float4 colorShadow = (color + color * shadow) * 0.5 ;
				color = lerp(color, max(0.001,colorShadow), shadowScale.r*_ShadowScale);
				//color = lerp(color, colorShadow, _ShadowScale);
				

				//HSV
				color.rgb = lerp(color.rgb,HsvFunFinal(color.rgb),_AdjustHSV);



				color += UniversalFragmentStylizedPBRFresnel(inputData, surfaceData.albedo, surfaceData.metallic, surfaceData.specular, 
				surfaceData.smoothness, surfaceData.occlusion,surfaceData.alpha, i.uv,i.color,TotalMask.a);



				float4 screenPos = i.screnuv;
                float4 screenPosNorm = screenPos / screenPos.w;

				#ifdef _EFFECTRIM_ON

				float2 EffectUV = lerp(i.uv,screenPos,_EffectRimUV);
				
				float2 EffectMap_offset = float2(_EffectMap_Uspeed, _EffectMap_Vspeed) * _Time.y;

				float2 EffectMapNoise_offset = float2(_EffectMapNoise_Uspeed, _EffectMapNoise_Vspeed) * _Time.y;

				float EffectNoiseTex = tex2D(_EffectMapNoise, EffectUV*_EffectMapNoise_ST.xy+(_EffectMapNoise_ST.zw+EffectMapNoise_offset)).r;

				float4 EffectMap = tex2D(_EffectMap, EffectUV*_EffectMap_ST.xy+(_EffectMap_ST.zw+EffectMap_offset+EffectNoiseTex*_EffectMapNoiseIntensity))*_EffectColor*_EffectIntensity;
				
				float2 RimMask_offset = float2(_RimMask_Uspeed, _RimMask_Vspeed) * _Time.y;

				float RimMask = tex2D(_RimMask, i.uv*_RimMask_ST.xy+_RimMask_ST.zw+RimMask_offset).r;
	
				float3 effectRim =FresnelCore(normalWS,viewDir,_RimColor,_RimPower,_RimIntensity,_RimStart,_RimEnd,_RimOffset);

				
				
				float4 EffectMapAlpha = tex2D(_EffectMap, EffectUV*_EffectMap_ST.xy+(_EffectMap_ST.zw+EffectMap_offset+EffectNoiseTex*_EffectMapNoiseIntensity))*_EffectIntensity;

				float effectRimAlpah =FresnelCore(normalWS,viewDir,1,_RimPower,_RimIntensity,_RimStart,_RimEnd,_RimOffset);;

				color.rgb +=lerp(effectRim*EffectMap,effectRim+EffectMap,_EffectRimIsAdd)*RimMask;
				

				color.a  = lerp(saturate(color.a),lerp(effectRimAlpah*EffectMapAlpha,effectRimAlpah+EffectMapAlpha,_EffectRimIsAdd),_EffectRimAlpha)*RimMask;
				
				


				#endif


				#if _STREAMER_ON
				half4 streamerNoise1 = tex2D(_StreamerNoise, i.streamerUV.yx);
				half4 streamerNoise2 = tex2D(_StreamerNoise, i.streamerUV.zw);
				half2 streamerNoise = streamerNoise1.r * streamerNoise2.g * _StreamerNoiseSpeed;
				half4 streamerAlbedo;
				streamerAlbedo = tex2D(_StreamerTex, i.streamerUV.xy + streamerNoise);
				half4 streamerMask = tex2D(_StreamerMask, i.uv);
				color.rgb = streamerAlbedo * _StreamerColor * streamerMask * _StreamerAlpha * streamerAlbedo.a + color.rgb;
				#endif
				

				color.a  = saturate(color.a);
				
				// Hell核心曲线(动态参数)
				half3 numerator = color.rgb * (6.2 *color.rgb + 0.5);
				//half3 denominator = color.rgb * (6.2 *color.rgb + 1.7) + 0.06; 
				half3 denominator = color.rgb * (6.2 *color.rgb + 1.2) + 0.06;
				half3 tonemapped = numerator / denominator;
				// 白点缩放
				tonemapped *= _u_WhitePoint;
				// 饱和度调整
				half3 finalColor = tonemapped;
				finalColor = Gamma22ToLinear(finalColor);				
				color.rgb = lerp(color.rgb, finalColor, _u_ToneWeight);

				//float3 effectRim =FresnelCore(normalWS,viewDir,_RimColor,_RimPower,_RimIntensity,_RimStart,_RimEnd,_RimOffset);
				//color.rgb += effectRim*specularMaskMap.g;

				

                return color;
            }
            ENDCG
        }


		



		Pass
		{
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }

			ZWrite On ZTest LEqual

			CGPROGRAM
			#pragma target 3.0

			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON 
			//#pragma shader_feature _METALLICGLOSSMAP
			//#pragma shader_feature _PARALLAXMAP
			#pragma multi_compile_shadowcaster

			#pragma vertex vertShadowCaster
			#pragma fragment fragShadowCaster

			#include "UnityStandardShadow.cginc"

			ENDCG
		}
    }
	
	//Fallback "Diffuse"
	CustomEditor "CustomShaderGUI_Base"
}
