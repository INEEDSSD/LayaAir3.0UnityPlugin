//1.0版本
//特效shader汇总 
//包含：边缘光 扰动 溶解（软/硬） 极坐标 旋涡 顶点偏移 
//可设置：混合模式 剔除模式 深度写入模式 

Shader "Effect/AParticleShader"
{
    Properties
    {
		//主纹理
		[HDR]_MainColor("主纹理颜色", Color) = (1,1,1,1)
		_MainTex("主纹理贴图", 2D) = "white" {}
		_U_MainTex_Speed("U_MainTex Speed",Range(-20,20)) = 0
		_V_MainTex_Speed("U_MainTex Speed",Range(-20,20)) = 0
		//mask
		[Toggle(_USE_MASK)]_USE_MASK("Mask 开关",int) = 0
		_MaskColor("Maks Color",Range(0,1)) = 0
		_MaskTex("Mask(R)", 2D) = "white" {}
		_U_MaskTex_Speed("U_MaskTex Speed",Range(-10,10)) = 0
		_V_MaskTex_Speed("U_MaskTex Speed",Range(-10,10)) = 0
		//边缘光
		[Space(15)]
		[Toggle(_RIMLIGHT_ON)]_RIMLIGHT_ON("外发光 开关",int) = 0
		[HDR]_FresnelColor("边缘光颜色",Color) = (0,0,0,0)
		_FresnelScale("边缘光亮度",Range(0,10)) = 1
		_FresnelPower("边缘光宽度",Range(-5,0)) = -2
		//扰动
		[Space(15)]
        [Toggle(_NOISE_ON)]_NOISE_ON("扭曲 开关",int) = 0
        _NoiseTex("NoiseTex",2D) = "white"{}
        _NoiseIntensity("NoiseIntensity",float) = 0.5
		_U_NoiseTex_Speed("U_NoiseTex Speed",Range(-10,10)) = 0
        _V_NoiseTex_Speed("V_NoiseTex Speed",Range(-10,10)) = 0
        //溶解
		[Space(15)]
		[Toggle(_DISSOLVE)]_DISSOLVE("溶解 开关",int) = 0
		[HDR]_DissolveColor("DissolveColor",Color) = (0,0,0,0)
		_DissolveTex("DissolveTex", 2D) = "white" {}
		_DissolveIntensity("DissolveIntensity",Range(0,1)) = 0
		_DissolveIntensity02("DissolveIntensity02",Range(-1,1)) = -1
		_HardAndSoft("Hard or Soft", Range(0.5 , 1)) = 1
		_SoftScale("SoftScale",Range(0,1)) = 0
		_U_DissolveTex_Speed("U DissolveTex Speed",Range(-10,10)) = 0
		_V_DissolveTex_Speed("V DissolveTex Speed",Range(-10,10)) = 0
		//极坐标
		[Space(15)]
		[Toggle(_POLAR)] _POLAR("极坐标", Int) = 0
		_Speed("Speed", Range(-1, 1)) = 0
		//旋涡
		[Space(15)]
		[Toggle(_SWIRL)] _SWIRL("旋涡效果", Int) = 0
		_Angle("Angle", Float) = 0
		//顶点偏移
		[Space(15)]
        [Toggle(_VERTEX_OFFSET_ON)]_VERTEX_OFFSET_ON("顶点偏移开关",int) = 0
        _OffsetInt("OffsetInt",Float) = 0
        _VO_tillingU("VO_tillingU",float) = 0
        _VO_tillingV("VO_tillingV",float) = 0
        _VO_PannerSpeedU("VO_PannerSpeedU",float) = 0
        _VO_PannerSpeedV("VO_PannerSpeedV",float) = 0
        _XYZPower("XYZ_Power(X Y Z)",Vector) = (0,0,0,0)

		[HideInInspector]_Alpha("Alpha",Range(0,1)) = 1	//	technology will use to set fish alpha

		[HideInInspector] _BlendMode ("__bmode", Float) = 0.0
		[HideInInspector] _RenderMode ("__rmode", Float) = 0.0
		[HideInInspector] _SrcBlend("__src", Float) = 5.0
		[HideInInspector] _DstBlend("__dst", Float) = 10.0
		[HideInInspector] _CullMode("__cull", Float) = 0.0
		[HideInInspector] _PZWrite("__zw", Float) = 0.0
		[HideInInspector] _BlendOp("__op", Float) = -1.0

		[HideInInspector]_Stencil ("Stencil ID", Float) = 0
		[HideInInspector]_StencilComp("Stencil Comparison", Float) = 8
		[HideInInspector]_StencilOp ("Stencil Operation", Float) = 0
		[HideInInspector]_StencilReadMask ("Stencil Read Mask", Float) = 255
		[HideInInspector]_StencilWriteMask("Stencil Write Mask", Float) = 255

		[HideInInspector]_ColorMask("Color Mask", Float) = 15

    }
    SubShader
    {
		Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent"}
		Blend[_SrcBlend][_DstBlend]
		BlendOp[_BlendOp]
		Cull[_CullMode]
		ZWrite[_PZWrite]
		Lighting Off
		ColorMask[_ColorMask]
		Stencil
		{
			Ref[_Stencil]
			Comp[_StencilComp]
			Pass[_StencilOp]
			ReadMask[_StencilReadMask]
			WriteMask[_StencilWriteMask]
		}

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			#pragma target 3.0
			#pragma shader_feature  _USE_MASK 
			#pragma shader_feature _RIMLIGHT_ON
			#pragma shader_feature _POLAR
			#pragma shader_feature _SWIRL
			#pragma shader_feature _NOISE_ON
			#pragma shader_feature _DISSOLVE 
			#pragma shader_feature _VERTEX_OFFSET_ON
			//#pragma multi_compile_instancing

            #include "UnityCG.cginc"
			#include "UnityUI.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 uv : TEXCOORD0;
				float4 vertexColor : COLOR;
				float3 vertexNormal : NORMAL;
				//UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
				float4 uv : TEXCOORD0;
				float4 uv2 : TEXCOORD1;
				float4 pos : SV_POSITION;
				float4 vertexColor : COLOR;
				float3 worldPos : TEXCOORD2;
				#if defined(_RIMLIGHT_ON)
					float3 worldEye : TEXCOORD3;
				#endif
				float3 worldNormal : NORMAL;
				//UNITY_VERTEX_INPUT_INSTANCE_ID
            };

			//主纹理
			uniform float4 _MainColor;
			uniform float _U_MainTex_Speed;
			uniform float _V_MainTex_Speed;

			uniform float _MaskColor;
			uniform float _U_MaskTex_Speed;
			uniform float _V_MaskTex_Speed;
			uniform float _MainCustom;
			//边缘光
			uniform float4 _FresnelColor;
			uniform float _FresnelScale;
			uniform float _FresnelPower;
			//极坐标
			float   _Speed;
			//旋涡
			float  _Angle;
			//扰动
			uniform float _NoiseIntensity;
			uniform float _U_NoiseTex_Speed;
			uniform float _V_NoiseTex_Speed;
			uniform float _NoiseCustom;

			//溶解
			uniform float4 _DissolveColor;
			uniform float4  _EdgeColor;
			uniform float _SoftScale;
			uniform float _HardAndSoft;
			uniform float _DissolveIntensity;
			uniform float _DissolveIntensity02;
			uniform float _U_DissolveTex_Speed;
			uniform float _V_DissolveTex_Speed;
			//顶点偏移
			uniform float _OffsetInt;
			uniform float _VO_tillingU;
			uniform float _VO_tillingV;
			uniform float _VO_PannerSpeedU;
			uniform float _VO_PannerSpeedV;
			uniform float4 _XYZPower;
			//UI裁剪
			uniform float4 _ClipRect;
			//纹理
			uniform	sampler2D _MainTex;				uniform float4 _MainTex_ST;
			uniform sampler2D _MaskTex;				uniform float4 _MaskTex_ST;
			uniform sampler2D _NoiseTex;			uniform float4 _NoiseTex_ST;
			uniform sampler2D _DissolveTex;			uniform float4 _DissolveTex_ST;

			half _Alpha;

			float2 randomVec(float2 noiseuv)
			{
				float vec = dot(noiseuv, float2(127.1, 311.7));
				return -1.0 + 2.0 * frac(sin(vec) * 43758.5453123);
			}

			float perlinNoise(float2 noiseuv)
			{
				float2 pi = floor(noiseuv);
				float2 pf = noiseuv - pi;
				float2 w = pf * pf * (3.0 - 2.0 *  pf);

				float2 lerp1 = lerp(
					dot(randomVec(pi + float2(0.0, 0.0)), pf - float2(0.0, 0.0)),
					dot(randomVec(pi + float2(1.0, 0.0)), pf - float2(1.0, 0.0)), w.x);

				float2 lerp2 = lerp(
					dot(randomVec(pi + float2(0.0, 1.0)), pf - float2(0.0, 1.0)),
					dot(randomVec(pi + float2(1.0, 1.0)), pf - float2(1.0, 1.0)), w.x);
				return lerp(lerp1, lerp2, w.y);
			}

            v2f vert (appdata v)
            {
                v2f o;
				UNITY_INITIALIZE_OUTPUT(v2f, o);
				UNITY_SETUP_INSTANCE_ID(v); 
				UNITY_TRANSFER_INSTANCE_ID(v, o); 
				o.uv = v.uv;
				o.uv2 = v.uv;
				
				o.worldNormal = UnityObjectToWorldNormal(v.vertexNormal);

				#if defined(_VERTEX_OFFSET_ON)
					float2 uv5 = v.uv * float2(_VO_tillingU,_VO_tillingV) + _Time.y * float2(_VO_PannerSpeedU,_VO_PannerSpeedV);
					float Vnoise = perlinNoise(uv5);
					float3 vertexValue = v.vertexNormal * Vnoise * _OffsetInt * _XYZPower.xyz;
					v.vertex.xyz += vertexValue;
				#endif
				o.worldPos = v.vertex;
				o.pos = UnityObjectToClipPos(v.vertex);// +float4 (vertexValue, 0); 
				#if defined(_RIMLIGHT_ON)
					float3 posWorld = mul(unity_ObjectToWorld, v.vertex).xyz;
					o.worldEye = posWorld - _WorldSpaceCameraPos;
				#endif
				o.vertexColor = v.vertexColor * _MainColor ;
                return o;
            }

			float4 frag (v2f i) : SV_Target
            {		
				 UNITY_SETUP_INSTANCE_ID(i);
				// UV
				float4 uv = i.uv;
				//极坐标UV
				#if defined(_POLAR) 
					uv.xy = i.uv.xy * 2 - 1;
					uv.xy = float2((_Time.y*_Speed + length(uv)), ((atan2(uv.r, uv.g) / 6.3) + 0.5));
				#endif

				float2 mainUVSpeed = float2(_U_MainTex_Speed, _V_MainTex_Speed) * _Time.y + uv.xy;
				float2 maskUVSpeed = float2(_U_MaskTex_Speed, _V_MaskTex_Speed) * _Time.y + i.uv.xy;
				//旋涡UV
				#if defined(_SWIRL)
					float2 Swirluv = float2(i.uv2.x - 0.5,i.uv2.y - 0.5);
					float f = distance(Swirluv, float2(0, 0));
					float s = sin(lerp(0, _Angle, f));
					float c = cos(lerp(0, _Angle, f));	
					Swirluv = float2(-Swirluv.x*c + Swirluv.y*s, Swirluv.x*s + Swirluv.y*c);
					mainUVSpeed = float2(Swirluv.x + 0.5, Swirluv.y + 0.5);
				#endif
				//扰动UV
				#if defined(_NOISE_ON)
				{					
					float2 noiseUVSpeed = float2(_U_NoiseTex_Speed, _V_NoiseTex_Speed) * _Time.y + uv.xy;
					float4 noiseTex = tex2D(_NoiseTex, TRANSFORM_TEX(noiseUVSpeed, _NoiseTex))*_NoiseIntensity;
					mainUVSpeed = float2(_U_MainTex_Speed, _V_MainTex_Speed) * _Time.y + uv.xy + noiseTex.r; 
				}
				#endif	
				//主纹理
				float4 mainTex = tex2D(_MainTex, TRANSFORM_TEX(mainUVSpeed, _MainTex));
				float3 col = mainTex.rgb * _MainColor.rgb * i.vertexColor.rgb ;
				
				float3 finalColor = col ;
				half alpha = 1;

				#if defined(_USE_MASK) 
					float3 mask = tex2D(_MaskTex, TRANSFORM_TEX(maskUVSpeed, _MaskTex));
					mask  = lerp(float3(mask.r, mask.r, mask.r) , mask.rgb, _MaskColor);
					finalColor = lerp(0, col, mask);
				#endif	
				//边缘光
				#if defined(_RIMLIGHT_ON)
					float ndotv = dot(normalize(i.worldNormal), normalize(i.worldEye));
					float3 fresnel = saturate(_FresnelScale * pow(max(1.0 - ndotv, 0.0001), _FresnelPower *5 )) * _FresnelColor.rgb;
					finalColor.rgb += fresnel;
				#endif		
				//溶解
                #if defined(_DISSOLVE) 
                {					
					uv.xy = float2(_U_DissolveTex_Speed, _V_DissolveTex_Speed) * _Time.y + uv.xy;
					float dissolveTex = tex2D(_DissolveTex, TRANSFORM_TEX(uv.xy, _DissolveTex)).r - (_DissolveIntensity);
					clip(dissolveTex-0.001);

					float dissolveStrength = smoothstep(_DissolveIntensity * _SoftScale * i.vertexColor.a, -0.001, dissolveTex);
					float3 dissolveColor = _DissolveColor.rgb * dissolveStrength ;
					finalColor.rgb = finalColor.rgb + dissolveColor ;

					float2 uvDissTex = uv.xy * _DissolveTex_ST.xy + _DissolveTex_ST.zw;
					alpha = smoothstep(1.0 - _HardAndSoft, _HardAndSoft, saturate(tex2D(_DissolveTex, uvDissTex).r - _DissolveIntensity02));
					
					#if defined(_USE_MASK)
						finalColor.rgb = lerp(0, finalColor.rgb , mask);						
					#endif
                }
                #endif				
				//极坐标
				#if defined(_POLAR)	
					finalColor.rgb += col ;
					#if defined(_USE_MASK)
						finalColor.rgb += lerp(0, col, mask);
					#endif
				#endif
				//旋涡
				#if defined(_SWIRL)
					finalColor.rgb += lerp(0, finalColor.rgb, tex2D(_MaskTex, TRANSFORM_TEX(mainUVSpeed, _MaskTex)).a);
					#if defined(_USE_MASK)
						finalColor.rgb += lerp(0, finalColor.rgb, tex2D(_MaskTex, TRANSFORM_TEX(mainUVSpeed, _MaskTex)).a);
					#endif
				#endif
				//alpha
				alpha *= mainTex.a * i.vertexColor.a ;
				#ifdef UNITY_UI_CLIP_RECT
					alpha *= UnityGet2DClipping(i.worldPos.xy, _ClipRect);
				#endif
				#ifdef UNITY_UI_ALPHACLIP
					clip(color.a - 0.001);
				#endif
				#if defined(_USE_MASK)
					alpha *=  mask;
				#endif
				alpha = min(1, alpha)* _Alpha;
                return float4(finalColor , alpha);
            }
            ENDCG
        }
    }
	CustomEditor "TY_Effect_ParticleShaderGUI"
}
