Shader "Sanguo/Sanguo_particle_distort_add" {
    Properties {
        [Header(Main Config)]
        _MainTex ("MainTex", 2D) = "white" {}
        _Glow("Glow", Float) = 5
        _Color("Color", Color) = (0.5,0.5,0.5,1)
        _Alpha("Alpha",Range(0,1)) = 1
        [Header(Distortion)]
        _distort_tex ("distort_tex", 2D) = "white" {}
        _QD ("QD", Float ) = 0.1
        [Header(Animation Speed)]
        _U ("U", Float ) = 0.2
        _V ("V", Float ) = 0.1
        _U_MainTex ("U_MainTex", Float ) = 0
        _V_MainTex ("V_MainTex", Float ) = 0
        [Header(Clipping Area(Local Space))]
        _MinX("Min X", Float) = -2000
        _MaxX("Max X", Float) = 2000
        _MinY("Min Y", Float) = -2000
        _MaxY("Max Y", Float) = 2000
    }
    SubShader {
        Tags {
            "IgnoreProjector"="True"
            "Queue"="Transparent+1"
            "RenderType"="Transparent"
        }
        Pass {
            Name "FORWARD"
            Tags {
                "LightMode"="ForwardBase"
            }
            Blend One One
            Cull Off
            ZWrite Off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            //#define UNITY_PASS_FORWARDBASE
            #include "UnityCG.cginc"
            #pragma multi_compile_fwdbase
            #pragma exclude_renderers xbox360 xboxone ps3 ps4 psp2 
            #pragma target 3.0
            uniform float4 _TimeEditor;
            uniform sampler2D _MainTex; uniform float4 _MainTex_ST;
            uniform sampler2D _distort_tex; uniform float4 _distort_tex_ST;
            uniform float _QD;
            uniform float _U;
            uniform float _V;
            uniform float _U_MainTex;
            uniform float _V_MainTex;
            uniform float _Glow;
            uniform float4 _Color;
            uniform float _Alpha;

            //-------------------add----------------------
            float _MinX;
            float _MaxX;
            float _MinY;
            float _MaxY;
            //-------------------add----------------------

            struct VertexInput {
                float4 vertex : POSITION;
                float2 texcoord0 : TEXCOORD0;
                float4 vertexColor : COLOR;
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float4 vertexColor : COLOR;
                //-------------------add----------------------
                float3 vpos : TEXCOORD2;
                //-------------------add----------------------
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                o.uv0 = v.texcoord0;
                o.vertexColor = v.vertexColor;
                o.pos = UnityObjectToClipPos(v.vertex);
                //-------------------add----------------------
                o.vpos = v.vertex.xyz;
                //-------------------add----------------------
                return o;
            }
            float4 frag(VertexOutput i) : COLOR {
                // 计算时间偏移
                float4 timeScrol = _Time + _TimeEditor;
                float2 timeScrollDistort = float2(_U, _V) * timeScrol.y +i.uv0;
                float2 timeScrollMain = float2(_U_MainTex, _V_MainTex) * timeScrol.y + i.uv0;

                //采样扭曲贴图 
                float2 distUV = TRANSFORM_TEX(timeScrollDistort, _distort_tex);
                float4 distortCol = tex2D(_distort_tex, distUV);
                // 计算主贴图 UV
                float2 distortOffset = distortCol.r * _QD;
                float2 mainUV = TRANSFORM_TEX(distortOffset + timeScrollMain, _MainTex);
                float4 mainCol = tex2D(_MainTex,mainUV);

                //  颜色混合计算
                half3 baseRGB = i.vertexColor.rgb * _Color.rgb * mainCol.rgb * _Glow * distortCol.rgb;
                half alphaFactor = i.vertexColor.a * _Color.a * mainCol.a * _Glow;
                half3 emission = baseRGB * alphaFactor * mainCol.a;
                float4 c = fixed4(emission, 1);

                //矩形区域裁切
				c.a *= step(_MinX , i.vpos.x);
				c.a *= step(i.vpos.x ,  _MaxX);
				c.a *= step(_MinY , i.vpos.y);
				c.a *= step(i.vpos.y  , _MaxY);

                c.rgb *= c.a * _Alpha;
                return c;
                //-------------------add----------------------
            }
            ENDCG
        }
    }
}
