Shader "Artist_Effect/Effect_FullEffect"
{
	Properties
	{
		[KeywordEnum(One, Two, Three)] _LayerType("Layer Type", Int) = 0
		_MainTex("Texture", 2D) = "white" {}
		_DetailTex("2nd layer", 2D) = "white" {}
		_DetailTex2("3rd layer", 2D) = "white" {}
		_Scroll0X("Base layer Scroll speed X", Float) = 0.0
		_Scroll0Y("Base layer Scroll speed Y", Float) = 0.0
		_Scroll1X("2nd layer Scroll speed X", Float) = 0.0
		_Scroll1Y("2nd layer Scroll speed Y", Float) = 0.0
		_Scroll2X("3rd layer Scroll speed X", Float) = 0.0
		_Scroll2Y("3rd layer Scroll speed Y", Float) = 0.0
		[KeywordEnum(Default,Clamp, Repeat)]_WrapMode("WrapMode", Float) = 0
		[Enum(model, 0, screen, 1)] _Layer0UVMode("Layer0 UV Mode", Float) = 0
		[Enum(model, 0, screen, 1)] _Layer1UVMode("Layer1 UV Mode", Float) = 0
		[Enum(model, 0, screen, 1)] _Layer2UVMode("Layer2 UV Mode", Float) = 0
		[HDR]_LayerColor("Color", Color) = (1,1,1,1)
		_LayerMultiplier("Layer Multiplier", Range(0.0, 10.0)) = 1.0

		[Toggle] _ROTATIONTEX("Rotation", float) = 0.0
		_RotateCenterX("RotateCenterX",Float) = 0.5
		_RotateCenterY("RotateCenterY",Float) = 0.5
		_TranslationX("TranslationX",float) = 0
		_TranslationY("TranslationY",float) = 0
		_RotateAngle("RotateAngle",Range(-360,360)) = 0

		[Toggle] _ROTATIONTEXTWO("Rotation Two", float) = 0.0
		_RotateCenterX02("RotateCenterX02",Float) = 0.5
		_RotateCenterY02("RotateCenterY02",Float) = 0.5
		_TranslationX02("TranslationX02",float) = 0
		_TranslationY02("TranslationY02",float) = 0
		_RotateAngle02("RotateAngle02",Range(-360,360)) = 0

		[Toggle] _ROTATIONTEXTHREE("Rotation Three", float) = 0.0
		_RotateCenterX03("RotateCenterX03",Float) = 0.5
		_RotateCenterY03("RotateCenterY03",Float) = 0.5
		_TranslationX03("TranslationX03",float) = 0
		_TranslationY03("TranslationY03",float) = 0
		_RotateAngle03("RotateAngle03",Range(-360,360)) = 0

		[Toggle] _ROTATIONTEXFOUR("Rotation Four", float) = 0.0
		_RotateCenterX04("RotateCenterX04",Float) = 0.5
		_RotateCenterY04("RotateCenterY04",Float) = 0.5
		_TranslationX04("TranslationX04",float) = 0
		_TranslationY04("TranslationY04",float) = 0
		_RotateAngle04("RotateAngle04",Range(-360,360)) = 0

		[Toggle] _UseDistort0("Distort Toggle 0", Float) = 0
		_DistortTex0("DistortTex0", 2D) = "black" {}
		[Enum(model, 0, screen, 1)] _Distort0UVMode("DistortTex0 UV Mode", Float) = 0
		_Distort0X("_Distort0X", Float) = 0
		_Distort0Y("_Distort0Y", Float) = 0
		_DistortStrength0("Distort Strength 0", Range(0, 10)) = 0

		[Toggle] _UseLighting("Lighting Toggle", Float) = 0
		_EffectMainLightDir("MainLight Direction", Vector) = (-0.5, 0.5, 1.0, 0.0)
		_EffectMainLightColor("MainLight Color", Color) = (1,1,1,1)
		_EffectMainLightIntensity("MainLight Intensity", Range(0.0, 5.0)) = 5.0
		_EffectAmbientLightColor("AmbientLight Color", Color) = (0.5,0.5,0.5,1)
		_EffectSSSColor("SSS Scatter Color", Color) = (0,0,0,1)

		[Toggle] _UseRim("Rim Toggle", Float) = 0
		[Toggle] _UseRimMap("Rim Map Toggle", Float) = 0
		[NoScaleOffset]_RimMap("Rim Map", 2D) = "gray" {}
		[NoScaleOffset]_RimMaskMap("Rim Mask Map", 2D) = "white" {}
		_RimLevel("Rim Level", Range(0.0, 5.0)) = 1.0
		[HDR]_RimColor("Rim Color", Color) = (0.6,0.8,1,1)
		_RimSharp("Rim Sharp",Range(0.0,20.0)) = 2.0
		[Enum(Color Add, 0, Color Multiply, 1, Color And Alpha Multiply, 2, Color Add Front Alpha, 3)] _RimMode("Rim Mode", Float) = 0

		_GlowStrength("Glow Strength", Range(0, 1)) = 1

		[Toggle] _UseVertexOffset("Vertex Offset Toggle", Float) = 0
		[Enum(normal, 0, axis, 1)] _VertexOffsetMode("Vertex Offset Mode", Float) = 0
		_VertexAmplitude("Vertex Amplitude", Range(0, 10)) = 0
		_VertexAmplitudeTex("Vertex Amplitude Texture", 2D) = "white" {}
		_VertexAmplitudeTexScroll0X("Vertex Amplitude Tex Scroll speed X", Float) = 0.0
		_VertexAmplitudeTexScroll0Y("Vertex Amplitude Tex Scroll Scroll speed Y", Float) = 0.0
		_VertexAmplitudeMaskTex("Vertex Amplitude Mask Texture", 2D) = "white" {}


		[MaterialToggle] _UseDissolve("Use Dissolve", Float) = 0
		_DissolveTexture("Dissolve", 2D) = "white" {}
		_DissolveAmount("Dissolve Amount", Range(0, 1.001)) = 0
		_DissolveFadeRange("Dissolve Fade Range", Range(0, 1.0)) = 0.1
		_DissolveAmountTexture("Dissolve Amount Texture", 2D) = "black" {}
		[MaterialToggle] _UseDissolveAmountMinus("Use Dissolve Minus", Float) = 0

		[Toggle] _UseFadeEdge("Use Fade Edge", Float) = 0
		_FadeEdgeTexture("Fade Edge Texture", 2D) = "white" {}
		[HDR]_FadeEdgeColor("Fade Edge Color", Color) = (1, 1, 1, 1)
		_FadeEdgeStrength("Fade Edge Strength", Range(0, 2)) = 1.0
		_FadeEdgeRange1("Fade Edge Range 1", Range(0, 0.5)) = 0.1
		_FadeEdgeRange2("Fade Edge Range 2", Range(0, 0.5)) = 0.1
		[Enum(Blend, 0, Add, 1)] _FadeEdgeType("Fade Edge Type", Float) = 0
		_FadeGlowStrength("Fade Glow Strength", Range(0, 10)) = 0.0

		[Toggle] _UseDissolveDistort("Dissolve Distort Toggle", Float) = 0
		_DissolveDistortTex("Dissolve DistortTex", 2D) = "black" {}
		[Enum(model, 0, screen, 1)] _DissolveDistortUVMode("Dissolve DistortTex UV Mode", Float) = 0
		_DissolveDistortX("Dissolve DistortX", Float) = 0
		_DissolveDistortY("Dissolve DistortY", Float) = 0
		_DissolveDistortStrength("Dissolve Distort Strength", Range(0, 10)) = 0


		[Toggle] _UseGradientMap0("Use Gradient Map 0", Float) = 0
		_GradientMapTex0("Gradient Map Tex 0", 2D) = "white" {}

		[Toggle] _UseNormalMapForRim("Normal Map For Rim Toggle", Float) = 0
		_NormalMap("Normal Tex", 2D) = "bump" {}
		_NormalMapStrength("Normal Map Strength", Range(0, 5)) = 1.0


		[Toggle] _UsePolar("UsePolar", Float) = 0
		_PolarControl("PolarControl", Vector) = (0.5,0.5,1,1)

		[HideInInspector]_Alpha("Alpha",Range(0,1)) = 1

		[MaterialToggle] _UseCustomData("Use CustomData", Int) = 0
		[HideInInspector][Enum(None, 0, Custom1X, 1, Custom1Y, 2, Custom1Z, 3, Custom1W, 4)] _MainTex_OffsetX_Custom("_MainTex_OffsetX_Custom", Int) = 0
		[HideInInspector][Enum(None, 0, Custom1X, 1, Custom1Y, 2, Custom1Z, 3, Custom1W, 4)]_MainTex_OffsetY_Custom("_MainTex_OffsetY_Custom", Int) = 0
		[HideInInspector][Enum(None, 0, Custom1X, 1, Custom1Y, 2, Custom1Z, 3, Custom1W, 4)] _DetailTex_OffsetX_Custom("_DetailTex_OffsetX_Custom", Int) = 0
		[HideInInspector][Enum(None, 0, Custom1X, 1, Custom1Y, 2, Custom1Z, 3, Custom1W, 4)] _DetailTex_OffsetY_Custom("_DetailTex_OffsetY_Custom", Int) = 0
		[HideInInspector][Enum(None, 0, Custom1X, 1, Custom1Y, 2, Custom1Z, 3, Custom1W, 4)] _DissolveTex_OffsetX_Custom("_Dissolve_OffsetX_Custom", Int) = 0
		[HideInInspector][Enum(None, 0, Custom1X, 1, Custom1Y, 2, Custom1Z, 3, Custom1W, 4)] _DissolveTex_OffsetY_Custom("_Dissolve_OffsetY_Custom", Int) = 0
		[HideInInspector][Enum(None, 0, Custom1X, 1, Custom1Y, 2, Custom1Z, 3, Custom1W, 4)] _DissolveAmount_Custom("_DissolveAmount_Custom", Int) = 0
		[HideInInspector][Enum(None, 0, Custom1X, 1, Custom1Y, 2, Custom1Z, 3, Custom1W, 4)] _VertexAmplitudeX_Custom("_VertexAmplitudeX_Custom", Int) = 0
		[HideInInspector][Enum(None, 0, Custom1X, 1, Custom1Y, 2, Custom1Z, 3, Custom1W, 4)] _VertexAmplitudeY_Custom("_VertexAmplitudeY_Custom", Int) = 0
		[HideInInspector][Enum(None, 0, Custom1X, 1, Custom1Y, 2, Custom1Z, 3, Custom1W, 4)] _VertexAmplitude_Custom("_VertexAmplitude_Custom", Int) = 0
		[HideInInspector] _Mode("__mode", Int) = 0.0
		[HideInInspector] _Cull("__cull", Int) = 2.0
		[HideInInspector] _ZWrite("__zw", Int) = 0.0
		[HideInInspector] _DstBlend("__dst", Int) = 0.0

		_Stencil("Stencil ID", Float) = 0
		[Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp("模板比较函数", Float) = 8 // Always
		[HideInInspector]_StencilOp("Stencil Operation", Float) = 0
		[HideInInspector]_StencilReadMask("Stencil Read Mask", Float) = 255
		[HideInInspector]_StencilWriteMask("Stencil Write Mask", Float) = 255

		[HideInInspector]_ColorMask("Color Mask", Float) = 15
	}
		SubShader
		{
			Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "IgnoreProjector" = "True"}
			LOD 150

			Pass
			{
				Name "Normal"

				BlendOp Add , Add
				Blend One[_DstBlend] ,One Zero
				Cull[_Cull]
				ZWrite[_ZWrite]
				ColorMask[_ColorMask]
				Stencil
				{
					Ref[_Stencil]
					Comp[_StencilComp]
					Pass[_StencilOp]
					ReadMask[_StencilReadMask]
					WriteMask[_StencilWriteMask]
				}

				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag

				#pragma shader_feature _LAYERTYPE_ONE _LAYERTYPE_TWO _LAYERTYPE_THREE
				#pragma shader_feature _ _USERIM_ON
				#pragma shader_feature _ _USERIMMAP_ON
				#pragma shader_feature _ _USELIGHTING_ON
				#pragma shader_feature _ _USEVERTEXOFFSET_ON
				#pragma shader_feature _ _USEDISSOLVE_ON
				#pragma shader_feature _ _USEFADEEDGE_ON
				#pragma shader_feature _ _USEDISTORT0_ON
				#pragma shader_feature _ _USECUSTOMDATA_ON
				#pragma shader_feature _ _USEGRADIENTMAP0_ON
				#pragma shader_feature _ _USENORMALMAPFORRIM_ON
				#pragma shader_feature _ _USEDISSOLVEDISTORT_ON
				#pragma shader_feature _ _WRAPMODE_DEFAULT _WRAPMODE_CLAMP _WRAPMODE_REPEAT  
				#pragma shader_feature _ROTATIONTEX_ON 
				#pragma shader_feature _ROTATIONTEXTWO_ON 
				#pragma shader_feature _ROTATIONTEXTHREE_ON 
				#pragma shader_feature _ROTATIONTEXFOUR_ON
				#pragma shader_feature _USEPOLAR_ON

				#include "UnityCG.cginc"

			//  Polar Coordinates
			float2 PolarCoordinates(float2 UV, float2 Center, float RadialScale, float LengthScale)
			{
			// 将UV坐标平移到以Center为中心
			float2 delta = UV - Center;
			// 计算半径（距离）
			float radius = length(delta) * 2.0 * RadialScale;
			// 计算角度（使用atan2函数，范围[-π, π]）
			// 将角度映射到[0, 1]范围，并应用缩放
			float angle = atan2(delta.x, delta.y) * (1.0 / 6.28318530718) * LengthScale;
			return float2(radius, angle);
		}

		//    Generic UV Rotation
		float2 RotateUV(float2 uv, float centerX, float centerY, float angle, float transX, float transY)
		{
			// 1. 角度转弧度
			float rad = angle * 3.1415926 / 180.0;
			float s, c;
			sincos(rad, s, c); // 同时计算 sin 和 cos，性能更好

			//  去中心化 + 平移
			float2 center = float2(centerX, centerY);
			float2 delta = uv - center + float2(transX, transY);

			float2x2 rotMat = float2x2(c, -s, s, c);
			float2 rotated = mul(delta, rotMat);
			return rotated + center;
		}

		float2 LowCostRotate(float2 uv, float centerX, float centerY, float c, float s, float transX, float transY) {
			float2 center = float2(centerX, centerY);
			float2 delta = uv - center + float2(transX, transY);

			// 仅仅是 4 次乘法和 2 次加法
			float2x2 rotMat = float2x2(c, -s, s, c);
			float2 rotated = mul(delta, rotMat);
			return rotated + center;
		}

		struct appdata
		{
			float4 vertex : POSITION;
			float4 uv0 : TEXCOORD0;
			half4 vcolor : COLOR;
			half3 normal : NORMAL;
			#if defined(_USECUSTOMDATA_ON )
				float4 uv1 : TEXCOORD1;
			#endif
				//#if defined(_USENORMALMAPFORRIM_ON)
					half4 tangent   : TANGENT;
					//#endif
				};

				struct v2f
				{
					float4 uv : TEXCOORD0;
					float4 vertex : SV_POSITION;
					half4 vcolor : TEXCOORD1;

					#if defined(_USERIM_ON)
						#if defined(_USENORMALMAPFORRIM_ON)
							half3 binormalWorld : TEXCOORD2;
						#else
							half3 normalView : TEXCOORD2;
						#endif
					#endif
					#if defined(_USELIGHTING_ON) || defined(_USENORMALMAPFORRIM_ON)
						half3 normalWorld : TEXCOORD3;
					#endif
					#if defined(_LAYERTYPE_TWO) || defined(_LAYERTYPE_THREE)
						float4 uv2 : TEXCOORD4;
					#endif
					half4 screenPos : TEXCOORD5;
					#if defined(_USEDISTORT0_ON) || defined(_USEDISSOLVEDISTORT_ON)
						float4 uv3 : TEXCOORD6;
					#endif
					#if defined(_USECUSTOMDATA_ON )
						float4 data : TEXCOORD7;
					#endif
					#if defined(_USENORMALMAPFORRIM_ON)
						float3 tangentWorld : TEXCOORD8;
					#endif
					#if defined(_ROTATIONTEX_ON) || defined(_ROTATIONTEXTWO_ON)
						float4 rotSinCos01 : TEXCOORD9;
					#endif
				};

				sampler2D _MainTex;
				float4 _MainTex_ST;
				float _Scroll0X;
				float _Scroll0Y;
				int _Layer0UVMode;

				float4 _PolarControl;
				float _UsePolar;

				#if defined(_USEDISTORT0_ON)
					sampler2D _DistortTex0;
					float4 _DistortTex0_ST;
					int _Distort0UVMode;
					float _Distort0X;
					float _Distort0Y;
					half _DistortStrength0;
				#endif

				#if defined(_LAYERTYPE_TWO) || defined(_LAYERTYPE_THREE)
					sampler2D _DetailTex;
					float4 _DetailTex_ST;
					float _Scroll1X;
					float _Scroll1Y;
					int _Layer1UVMode;
				#endif

				#if defined(_LAYERTYPE_THREE)
					sampler2D _DetailTex2;
					float4 _DetailTex2_ST;
					float _Scroll2X;
					float _Scroll2Y;
					int _Layer2UVMode;
				#endif
					half4 _LayerColor;
					half _LayerMultiplier;
					half _Alpha;
				#if defined(_USELIGHTING_ON)
					half4 _EffectMainLightDir;
					half4 _EffectMainLightColor;
					half _EffectMainLightIntensity;
					half4 _EffectSSSColor;
					half4 _EffectAmbientLightColor;
				#endif
				#if defined(_USERIM_ON)
					int _UseRim;
					sampler2D _RimMaskMap;
					half _RimLevel;
					int _RimMode;
				#if defined(_USERIMMAP_ON)
					sampler2D _RimMap;
					#else
						half4 _RimColor;
						half _RimSharp;
					#endif
				#endif

				#if defined(_USEVERTEXOFFSET_ON)
					sampler2D _VertexAmplitudeTex;
					float4 _VertexAmplitudeTex_ST;
					half _VertexAmplitudeTexScroll0X;
					half _VertexAmplitudeTexScroll0Y;
					half _VertexAmplitude;
					sampler2D _VertexAmplitudeMaskTex;
					float4 _VertexAmplitudeMaskTex_ST;
					int _VertexOffsetMode;
				#endif

				#if defined(_USEDISSOLVE_ON)
					sampler2D _DissolveTexture;
					half4 _DissolveTexture_ST;
					half _DissolveAmount;
					//int _HardDissolve;
					half _DissolveFadeRange;
					sampler2D _DissolveAmountTexture;
					half4 _DissolveAmountTexture_ST;
					half _UseDissolveAmountMinus;

					#if defined(_USEFADEEDGE_ON)
						sampler2D _FadeEdgeTexture;
						half4 _FadeEdgeTexture_ST;
						half4  _FadeEdgeColor;
						half _FadeEdgeStrength;
						half _FadeEdgeRange1;
						half _FadeEdgeRange2;
						int _FadeEdgeType;
					#endif

					#if defined(_USEDISSOLVEDISTORT_ON)
						sampler2D _DissolveDistortTex;
						float4 _DissolveDistortTex_ST;
						int _DissolveDistortUVMode;
						float _DissolveDistortX;
						float _DissolveDistortY;
						half _DissolveDistortStrength;
					#endif
				#endif

				#if defined(_USECUSTOMDATA_ON )
					int _MainTex_OffsetX_Custom;
					int _MainTex_OffsetY_Custom;
					int _DetailTex_OffsetX_Custom;
					int _DetailTex_OffsetY_Custom;
					int _DissolveTex_OffsetX_Custom;
					int _DissolveTex_OffsetY_Custom;
					int _DissolveAmount_Custom;
					int _VertexAmplitudeX_Custom;
					int _VertexAmplitudeY_Custom;
					//int _VertexAmplitude_Custom;
				#endif

				#if defined(_USEGRADIENTMAP0_ON)
					sampler2D _GradientMapTex0;
					float4 _GradientMapTex0_ST;
				#endif
				#if defined(_USENORMALMAPFORRIM_ON)
					sampler2D _NormalMap;
					half4 _NormalMap_ST;
					half _NormalMapStrength;
				#endif
				#if defined(_ROTATIONTEX_ON)
					float _RotateCenterX;
					float _RotateCenterY;
					float _RotateAngle;
					float _TranslationX;
					float _TranslationY;
				#endif				
				#if defined(_ROTATIONTEXTWO_ON)
					float _RotateCenterX02;
					float _RotateCenterY02;
					float _RotateAngle02;
					float _TranslationX02;
					float _TranslationY02;
				#endif				
				#if defined(_ROTATIONTEXTHREE_ON)
					float _RotateCenterX03;
					float _RotateCenterY03;
					float _RotateAngle03;
					float _TranslationX03;
					float _TranslationY03;
				#endif				
				#if defined(_ROTATIONTEXFOUR_ON)
					float _RotateCenterX04;
					float _RotateCenterY04;
					float _RotateAngle04;
					float _TranslationX04;
					float _TranslationY04;
				#endif


			v2f vert(appdata v)
			{
				v2f o;

				#if defined(_USECUSTOMDATA_ON )
					o.data.xy = v.uv0.zw;
					o.data.zw = v.uv1.xy;
				#endif
				#if defined(_USEVERTEXOFFSET_ON)
					half4 vertexAmplitudeTex = tex2Dlod(_VertexAmplitudeTex, float4(TRANSFORM_TEX(v.uv0.xy, _VertexAmplitudeTex) + frac(float2(_VertexAmplitudeTexScroll0X, _VertexAmplitudeTexScroll0Y) * _Time.g), 0, 0));
					half4 vertexAmplitudeMaskTex = tex2Dlod(_VertexAmplitudeMaskTex, float4(TRANSFORM_TEX(v.uv0.xy, _VertexAmplitudeMaskTex), 0, 0));
					float3 posInMS = v.vertex.xyz;
					#if defined(_USECUSTOMDATA_ON )
						half2 vertexAmplitudeUV = v.uv0.zw + (_VertexAmplitudeTex_ST.xy * v.uv0.xy + half2(_VertexAmplitudeX_Custom < 1 ? _VertexAmplitudeTex_ST.z : o.data[_VertexAmplitudeX_Custom - 1],
							_VertexAmplitudeY_Custom < 1 ? _VertexAmplitudeTex_ST.w : o.data[_VertexAmplitudeY_Custom - 1]));
						vertexAmplitudeTex = tex2Dlod(_VertexAmplitudeTex, float4(TRANSFORM_TEX(vertexAmplitudeUV , _VertexAmplitudeMaskTex) + frac(float2(_VertexAmplitudeTexScroll0X, _VertexAmplitudeTexScroll0Y) * _Time.g), 0, 0));
					#endif
					if (_VertexOffsetMode == 1)
					{
						posInMS += _VertexAmplitude * (2 * vertexAmplitudeTex.rgb - 1) * vertexAmplitudeMaskTex.r;
					}
					else
					{
						posInMS += v.normal * _VertexAmplitude * vertexAmplitudeTex.r * vertexAmplitudeMaskTex.r;
					}
					o.vertex = UnityObjectToClipPos(float4(posInMS, 1));
				#else
					o.vertex = UnityObjectToClipPos(v.vertex);
				#endif

				o.screenPos = ComputeScreenPos(o.vertex);
				o.uv.xy = v.uv0.xy;
				o.uv.zw = frac(float2(_Scroll0X, _Scroll0Y) * _Time.g);
				o.vcolor = v.vcolor;

				#if defined(_USENORMALMAPFORRIM_ON)
					half3 normalWorld = UnityObjectToWorldNormal(v.normal);
					half3 tangentWorld = UnityObjectToWorldDir(v.tangent.xyz);
					half sign = v.tangent.w * unity_WorldTransformParams.w;
					half3 binormalWorld = cross(normalWorld, tangentWorld) * sign;
					o.normalWorld = normalWorld;
					o.tangentWorld = tangentWorld;
				#endif

				#if defined(_USERIM_ON)
					#if defined(_USENORMALMAPFORRIM_ON)
						o.binormalWorld = binormalWorld;
					#else
						o.normalView = normalize(mul((half3x3)UNITY_MATRIX_IT_MV, v.normal));
					#endif
				#endif

				#if defined(_USELIGHTING_ON)
					o.normalWorld = UnityObjectToWorldNormal(v.normal);
				#endif

				#if defined(_LAYERTYPE_TWO) || defined(_LAYERTYPE_THREE)
					o.uv2.xy = frac(float2(_Scroll1X, _Scroll1Y) * _Time.g);
				#endif

				#if defined(_LAYERTYPE_THREE)
					o.uv2.zw = frac(float2(_Scroll2X, _Scroll2Y) * _Time.g);
				#endif
				#if defined(_USEDISTORT0_ON)
					o.uv3.xy = frac(float2(_Distort0X, _Distort0Y) * _Time.g);
				#endif
				#if defined(_USEDISSOLVEDISTORT_ON)
					o.uv3.zw = frac(float2(_DissolveDistortX, _DissolveDistortY) * _Time.g);
				#endif

			   #if defined(_ROTATIONTEX_ON)
					float rad1 = _RotateAngle * 0.01745329; // pi / 180
					sincos(rad1, o.rotSinCos01.y, o.rotSinCos01.x); // y=sin, x=cos
			   #endif

			   #if defined(_ROTATIONTEXTWO_ON)
					float rad2 = _RotateAngle02 * 0.01745329;
					sincos(rad2, o.rotSinCos01.w, o.rotSinCos01.z);
			   #endif
				return o;
			}

			half4 frag(v2f i) : SV_Target
			{
				half2 screenUV = i.screenPos.xy / i.screenPos.w;
				#if defined(_USEPOLAR_ON)
					i.uv.xy = PolarCoordinates(i.uv.xy,_PolarControl.xy,_PolarControl.z,_PolarControl.w);
				#endif	

					// 1. Distortion
					#if defined(_USEDISSOLVE_ON)
						half2 dissolveUV = _DissolveTexture_ST.xy * i.uv.xy;
						#if defined(_USEDISSOLVEDISTORT_ON)
							dissolveUV += tex2D(_DissolveDistortTex, i.uv3.zw + TRANSFORM_TEX((_DissolveDistortUVMode == 0 ? i.uv.xy : screenUV), _DissolveDistortTex)) * _DissolveDistortStrength;
						#endif
						#if defined(_USECUSTOMDATA_ON )
							half dissolveValue = tex2D(_DissolveTexture, (dissolveUV + half2(_DissolveTex_OffsetX_Custom < 1 ? _DissolveTexture_ST.z : i.data[_DissolveTex_OffsetX_Custom - 1],
												_DissolveTex_OffsetY_Custom < 1 ? _DissolveTexture_ST.w : i.data[_DissolveTex_OffsetY_Custom - 1]))).r;
							half dissoveAmount = _DissolveAmount_Custom < 1 ? _DissolveAmount : i.data[_DissolveAmount_Custom - 1];
						#else
							half dissolveValue = tex2D(_DissolveTexture, (dissolveUV + _DissolveTexture_ST.zw)).r;
							half dissoveAmount = _DissolveAmount;
						#endif
						#if defined(_ROTATIONTEXFOUR_ON)
							half2 uvFour = RotateUV(i.uv.xy, _RotateCenterX04, _RotateCenterY04, _RotateAngle04, _TranslationX04, _TranslationY04);
							dissoveAmount += tex2D(_DissolveAmountTexture, (_DissolveAmountTexture_ST.xy * uvFour + _DissolveAmountTexture_ST.zw)).r;
						#else
							dissoveAmount += tex2D(_DissolveAmountTexture, (_DissolveAmountTexture_ST.xy * i.uv.xy + _DissolveAmountTexture_ST.zw)).r;
						#endif
						dissoveAmount = min(1.001, dissoveAmount);


						#if defined(_USEFADEEDGE_ON)
							half fadeValue = dissoveAmount;
							half fadeFactor = dissolveValue;
							clip(fadeFactor - fadeValue);

							half fadeAlpha = 0;
							half4 fadeEdgeTex = tex2D(_FadeEdgeTexture, TRANSFORM_TEX(i.uv.xy, _FadeEdgeTexture));
							half3 fadeEdgeColor = fadeEdgeTex.rgb * _FadeEdgeColor * _FadeEdgeStrength;

							half fadeEdgeValue1 = _FadeEdgeRange1 + fadeValue;
							half fadeEdgeValue2 = fadeEdgeValue1 + _FadeEdgeRange2;
							float range = fadeEdgeValue2 - fadeEdgeValue1;
							fadeAlpha = saturate((fadeEdgeValue2 - fadeFactor) / max(0.0001, range));
						#else
							half dissoveFactor = 1;
							if (dissoveAmount > 0 || _UseDissolveAmountMinus > 0)
							{
								if (_DissolveFadeRange == 0)
									clip(dissolveValue - dissoveAmount);
								else
								{
									dissoveFactor = (1 - step(dissolveValue, dissoveAmount)) * smoothstep(dissoveAmount, dissoveAmount + _DissolveFadeRange , dissolveValue);
									//dissoveFactor = (1 - step(dissolveValue, dissoveAmount)) * ((dissolveValue - dissoveAmount) / _DissolveFadeRange);
								}
							}
						#endif
					#endif

							// Layer 
							// sample the texture
							#if defined(_USECUSTOMDATA_ON )
								float2 mainUV = i.uv.zw + ((_Layer0UVMode == 0 ? i.uv.xy : screenUV) * _MainTex_ST.xy + half2(_MainTex_OffsetX_Custom < 1 ? _MainTex_ST.z : i.data[_MainTex_OffsetX_Custom - 1],
												_MainTex_OffsetY_Custom < 1 ? _MainTex_ST.w : i.data[_MainTex_OffsetY_Custom - 1]));
							#else
								float2 mainUV = i.uv.zw + (_MainTex_ST.xy * (_Layer0UVMode == 0 ? i.uv.xy : screenUV) + _MainTex_ST.zw);
							#endif

							#if defined(_USEDISTORT0_ON)
								mainUV += tex2D(_DistortTex0, i.uv3.xy + TRANSFORM_TEX((_Distort0UVMode == 0 ? i.uv.xy : screenUV), _DistortTex0)) * _DistortStrength0;
							#endif
								//图片重复方式的算法
								#ifdef _WRAPMODE_DEFAULT
									mainUV = mainUV;
								#elif _WRAPMODE_CLAMP
									mainUV = saturate(mainUV);
								#elif _WRAPMODE_REPEAT
									mainUV = frac(mainUV);
								#endif

								#if defined(_ROTATIONTEX_ON)
									mainUV = LowCostRotate(mainUV, _RotateCenterX, _RotateCenterY,
										i.rotSinCos01.x, i.rotSinCos01.y,
										_TranslationX, _TranslationY);
								#endif

								half4 color = tex2D(_MainTex, mainUV);

								#if defined(_USEGRADIENTMAP0_ON)
									color = tex2D(_GradientMapTex0, TRANSFORM_TEX(half2(color.r, 0.5), _GradientMapTex0));
								#endif

								#if defined(_LAYERTYPE_TWO) || defined(_LAYERTYPE_THREE)
									#if defined(_USECUSTOMDATA_ON )
										float2 detailUV = i.uv2.xy + (_DetailTex_ST.xy * (_Layer1UVMode == 0 ? i.uv.xy : screenUV) + half2(_DetailTex_OffsetX_Custom < 0.9f ? _DetailTex_ST.z : i.data[_DetailTex_OffsetX_Custom - 1],
															_DetailTex_OffsetY_Custom < 0.001f ? _DetailTex_ST.w : i.data[_DetailTex_OffsetY_Custom - 1]));
									#else
										float2 detailUV = i.uv2.xy + (_DetailTex_ST.xy * (_Layer1UVMode == 0 ? i.uv.xy : screenUV) + _DetailTex_ST.zw);
									#endif

									#if defined(_ROTATIONTEXTWO_ON) 
										detailUV = LowCostRotate(detailUV, _RotateCenterX02, _RotateCenterY02, i.rotSinCos01.z, i.rotSinCos01.w, _TranslationX02, _TranslationY02);
									 #endif
									half4 detailTex = tex2D(_DetailTex, detailUV);
									color *= detailTex;
								#endif

								#if defined(_LAYERTYPE_THREE)
									#if defined(_ROTATIONTEXTHREE_ON) 
										i.uv2.zw = RotateUV(i.uv2.zw, _RotateCenterX03, _RotateCenterY03, _RotateAngle03, _TranslationX03, _TranslationY03);
									#endif
									half4 detailTex2 = tex2D(_DetailTex2, i.uv2.zw + TRANSFORM_TEX((_Layer2UVMode == 0 ? i.uv.xy : screenUV), _DetailTex2));
									color *= detailTex2;
								#endif
									color *= _LayerColor * _LayerMultiplier * _Alpha;;

								#if defined(_USEDISSOLVE_ON)
									#if defined(_USEFADEEDGE_ON)
										if (_FadeEdgeType == 0)
											color.xyz = lerp(color.xyz, fadeEdgeColor, fadeAlpha);
										else
											color.xyz = color.xyz + fadeAlpha * fadeEdgeColor;
									#else
										color.a *= dissoveFactor;
									#endif
								#endif
								color.rgb *= color.a;

								//3 light
								#if defined(_USELIGHTING_ON)
									half3 normalWorld = normalize(i.normalWorld);
									half3 effectMainLightDir = normalize(_EffectMainLightDir);
									half snl = dot(normalWorld, effectMainLightDir);
									half w = fwidth(snl) * 2.0;
									half3 spec = lerp(0, 1, smoothstep(-w, w * 2.0, snl + _EffectMainLightDir.w - 1)) * step(0.0001, _EffectMainLightDir.w);
									color.rgb *= spec * _EffectMainLightIntensity * _EffectMainLightColor * max((_EffectSSSColor + snl) / (_EffectSSSColor + 1), 0) + _EffectAmbientLightColor;

								#endif

									// Rim
									#if defined(_USERIM_ON)
										#if defined(_USENORMALMAPFORRIM_ON)
											half3 tangent = i.tangentWorld.xyz;
											half3 binormal = i.binormalWorld.xyz;
											half3 normal = i.normalWorld.xyz;
											half3 normalMapVal = tex2D(_NormalMap, i.uv.xy * _NormalMap_ST.xy + _NormalMap_ST.zw).xyz;
											normalMapVal.xy = (normalMapVal * 2 - 1) * _NormalMapStrength;
											normalMapVal.z = sqrt(1 - saturate(dot(normalMapVal.xy, normalMapVal.xy)));
											half3 normalWorldRim = normalize(normalMapVal.x * tangent + normalMapVal.y * binormal + normalMapVal.z * normal);
											half3 normalView = normalize(mul((half3x3)transpose(unity_MatrixInvV), normalWorldRim));
										#else
											half3 normalView = normalize(i.normalView);
										#endif

										half rimMask = tex2D(_RimMaskMap, i.uv.xy).r;
										#if defined(_USERIMMAP_ON)
											half2 rimUV = normalView * 0.5 + 0.5;
											half4 rimColor = tex2D(_RimMap, rimUV);
											half rimFactor = (rimColor.x + rimColor.y + rimColor.z) * rimColor.w * _RimLevel * rimMask;
											half3 rimCol = rimColor.rgb;
										#else
											half rimFactor = pow(length(normalView.xy), _RimSharp) * 2 * _RimLevel * rimMask;
											half3 rimCol = _RimColor.rgb;
										#endif

										if (_RimMode == 1)
											color.xyz *= rimCol * rimFactor;
										else if (_RimMode == 2)
										{
											color.xyz *= rimCol * rimFactor;
											color.w *= rimFactor;
										}
										else if (_RimMode == 3)
											color.xyz += rimCol * rimFactor * color.w;
										else
											color.xyz += rimCol * rimFactor;
									#endif
									color *= i.vcolor;
									color.rgb *= i.vcolor.a;
									color.a = saturate(color.a);
									return color;
								}
								ENDCG
							}
		}
			CustomEditor "SEFullEffectInShowShaderGUI"
}
