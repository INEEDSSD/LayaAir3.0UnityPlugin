Shader3D Start
{
    type:Shader3D,
    name: "Mesh_Effect_Basic_AlphaBlend",
    enableInstancing:true,
    supportReflectionProbe:false,
    shaderType:D3,
    uniformMap:{
        u_TintColor: { type: "Vector4", default: [1, 1, 1, 1], alias: "Tint Color" },
        u_AlbedoTexture: { type: "Texture2D", default: "white", alias: "Main Texture" },
        u_UVScroll: { type: "Vector4", default: [0, 0, 0, 0], alias: "UV Scroll" },
        u_TilingOffset: { type: "Vector4", default: [1, 1, 0, 0], alias: "Tiling Offset" },
        u_AlphaTestValue: { type: "Float", default: 0.5, range: [0.0, 1.0] },
    },
    defines: {
    },
    styles: {
        materialRenderMode: { default: 5 },
        s_Blend: { default: 2 },
        s_BlendDstRGB: { default: 7 },
        s_Cull: { default: 0 },
        s_DepthWrite: { default: false },
    },
    shaderPass:[
        {
            pipeline:"Forward",
            VS:Mesh_Effect_Basic_AlphaBlendVS,
            FS:Mesh_Effect_Basic_AlphaBlendFS,
            statefirst: true,
                renderState: {
                blend: "Enable",
                blendEquation: "Add",
                srcBlend: "SrcAlpha",
                dstBlend: "OneMinusSrcAlpha"
            }
        }
    ]
}
Shader3D End

GLSL Start
#defineGLSL Mesh_Effect_Basic_AlphaBlendVS

#define SHADER_NAME Mesh_Effect_Basic_AlphaBlendVS

#include "Math.glsl";

#include "Scene.glsl"
#include "SceneFogInput.glsl";

#include "Camera.glsl";

#include "Sprite3DVertex.glsl";

#include "VertexCommon.glsl";

varying vec4 v_Color;
varying vec2 v_TextureCoordinate;
varying vec2 v_Texcoord0;

vec2 TransformUV(vec2 texcoord, vec4 tilingOffset)
{
    return texcoord * tilingOffset.xy + tilingOffset.zw;
}

void main()
{
    Vertex vertex;
    getVertexParams(vertex);

    mat4 worldMat = getWorldMatrix();

    vec4 pos = (worldMat * vec4(vertex.positionOS, 1.0));
    vec3 positionWS = pos.xyz / pos.w;

    gl_Position = getPositionCS(positionWS);

    #ifdef COLOR
        v_Color = vertex.vertexColor;
    #else
        v_Color = vec4(1.0);
    #endif

    v_Texcoord0 = vertex.texCoord0;
    v_TextureCoordinate = TransformUV(vertex.texCoord0, u_TilingOffset);

    gl_Position = remapPositionZ(gl_Position);
    #ifdef FOG
        FogHandle(gl_Position.z);
    #endif
}

#endGLSL

#defineGLSL Mesh_Effect_Basic_AlphaBlendFS

#define SHADER_NAME Mesh_Effect_Basic_AlphaBlendFS

#include "Color.glsl";

#include "Scene.glsl";
#include "SceneFog.glsl";

#include "Camera.glsl";

#include "Sprite3DFrag.glsl";

varying vec4 v_Color;
varying vec2 v_TextureCoordinate;
varying vec2 v_Texcoord0;

void main()
{
    // DEBUG STEP4: texColor * TintColor * vertexColor
    vec2 mainUV = v_TextureCoordinate;
    vec4 texColor = texture2D(u_AlbedoTexture, mainUV);
    gl_FragColor = texColor * u_TintColor * v_Color;
}

#endGLSL
GLSL End
