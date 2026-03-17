Shader3D Start
{
    type:Shader3D,
    name: "Mesh_FullEffect",
    enableInstancing:true,
    supportReflectionProbe:false,
    shaderType:D3,
    uniformMap:{
		LayerType: { type: "Int", default: 0, index: -1, alias: "层数量", catalog: "基本设置", catalogOrder: 0, serializable: false, inspector: "RadioGroup", options: { members: ["EFFECT_LAYER_ONE", "EFFECT_LAYER_TWO", "EFFECT_LAYER_THREE"] } },
        WrapMode: { type: "Int", default: 0, index: -0.5, alias: "贴图Wrap模式", catalog: "基本设置", serializable: false, inspector: "RadioGroup", options: { members: ["EFFECT_WRAPMODE_DEFAULT", "EFFECT_WRAPMODE_CLAMP", "EFFECT_WRAPMODE_REPEAT"] } },
        u_PolarControl: { type: "Vector4", default: [0.5, 0.5, 1, 1], index: -0.3, alias: "极坐标控制(中心XY,径向缩放,角度缩放)", catalog: "基本设置", hidden: "!data.EFFECT_POLAR" },
        u_texture: { type: "Texture2D", default: "white", index: 0, alias: "第一层颜色贴图(rgb),透明度(a)", catalog: "基本设置" },
        u_TilingOffset: { type: "Vector4", default: [1, 1, 0, 0], index: 1, alias: "第一层TilingOffset", catalog: "基本设置" },
        u_Scroll0: { type: "Vector2", default: [0, 0], index: 2, alias: "第一层UV滚动速度(U,V)", catalog: "基本设置" },
        u_Layer0UVMode: { type: "Int", default: 0, index: 2.5, alias: "第一层UV模式", catalog: "基本设置", enumSource: [{name: "Model", value: 0}, {name: "Screen", value: 1}] },
        u_RotateCenter: { type: "Vector2", default: [0.5, 0.5], index: 4, alias: "第一层旋转中心(X,Y)", catalog: "基本设置", hidden: "!data.EFFECT_ROTATION" },
        u_RotateAngle: { type: "Float", default: 0, index: 5, alias: "第一层旋转角度", catalog: "基本设置", hidden: "!data.EFFECT_ROTATION" },
        u_Translation: { type: "Vector2", default: [0, 0], index: 6, alias: "第一层旋转位置(X,Y)", catalog: "基本设置", hidden: "!data.EFFECT_ROTATION" },
        u_DistortTex: { type: "Texture2D", default: "black", index: 8, alias: "第一层扰动贴图(rg)", catalog: "基本设置", hidden: "!data.EFFECT_DISTORT" },
        u_DistortTex_ST: { type: "Vector4", default: [1, 1, 0, 0], index: 9, alias: "扰动TilingOffset", catalog: "基本设置", hidden: "!data.EFFECT_DISTORT" },
        u_DistortScroll: { type: "Vector2", default: [0, 0], index: 10, alias: "扰动UV滚动速度(U,V)", catalog: "基本设置", hidden: "!data.EFFECT_DISTORT" },
        u_DistortStrength: { type: "Float", default: 0, index: 11, alias: "扰动强度", catalog: "基本设置", hidden: "!data.EFFECT_DISTORT" },
        u_Distort0UVMode: { type: "Int", default: 0, index: 11.5, alias: "扰动UV模式", catalog: "基本设置", hidden: "!data.EFFECT_DISTORT", enumSource: [{name: "Model", value: 0}, {name: "Screen", value: 1}] },
        u_GradientMapTex0: { type: "Texture2D", default: "white", index: 12, alias: "渐变映射贴图", catalog: "基本设置", hidden: "!data.EFFECT_GRADIENT_MAP" },
        u_GradientMapTex0_ST: { type: "Vector4", default: [1, 1, 0, 0], index: 12.5, alias: "渐变映射TilingOffset", catalog: "基本设置", hidden: "!data.EFFECT_GRADIENT_MAP" },
        u_DetailTex: { type: "Texture2D", default: "white", index: 30, alias: "第二层颜色贴图(rgb),透明度(a)", catalog: "基本设置", hidden: "!data.EFFECT_LAYER_TWO && !data.EFFECT_LAYER_THREE" },
        u_DetailTex_ST: { type: "Vector4", default: [1, 1, 0, 0], index: 31, alias: "第二层TilingOffset", catalog: "基本设置", hidden: "!data.EFFECT_LAYER_TWO && !data.EFFECT_LAYER_THREE" },
        u_Scroll1: { type: "Vector2", default: [0, 0], index: 32, alias: "第二层UV滚动速度(U,V)", catalog: "基本设置", hidden: "!data.EFFECT_LAYER_TWO && !data.EFFECT_LAYER_THREE" },
        u_Layer1UVMode: { type: "Int", default: 0, index: 32.5, alias: "第二层UV模式", catalog: "基本设置", hidden: "!data.EFFECT_LAYER_TWO && !data.EFFECT_LAYER_THREE", enumSource: [{name: "Model", value: 0}, {name: "Screen", value: 1}] },
        u_RotateCenter02: { type: "Vector2", default: [0.5, 0.5], index: 33, alias: "第二层旋转中心(X,Y)", catalog: "基本设置", hidden: "(!data.EFFECT_LAYER_TWO && !data.EFFECT_LAYER_THREE) || !data.EFFECT_ROTATION_TWO" },
        u_RotateAngle02: { type: "Float", default: 0, index: 34, alias: "第二层旋转角度", catalog: "基本设置", hidden: "(!data.EFFECT_LAYER_TWO && !data.EFFECT_LAYER_THREE) || !data.EFFECT_ROTATION_TWO" },
        u_Translation02: { type: "Vector2", default: [0, 0], index: 35, alias: "第二层旋转位置(X,Y)", catalog: "基本设置", hidden: "(!data.EFFECT_LAYER_TWO && !data.EFFECT_LAYER_THREE) || !data.EFFECT_ROTATION_TWO" },
        u_DetailTex2: { type: "Texture2D", default: "white", index: 40, alias: "第三层颜色贴图(rgb),透明度(a)", catalog: "基本设置", hidden: "!data.EFFECT_LAYER_THREE" },
        u_DetailTex2_ST: { type: "Vector4", default: [1, 1, 0, 0], index: 41, alias: "第三层TilingOffset", catalog: "基本设置", hidden: "!data.EFFECT_LAYER_THREE" },
        u_Scroll2: { type: "Vector2", default: [0, 0], index: 42, alias: "第三层UV滚动速度(U,V)", catalog: "基本设置", hidden: "!data.EFFECT_LAYER_THREE" },
        u_Layer2UVMode: { type: "Int", default: 0, index: 42.5, alias: "第三层UV模式", catalog: "基本设置", hidden: "!data.EFFECT_LAYER_THREE", enumSource: [{name: "Model", value: 0}, {name: "Screen", value: 1}] },
        u_RotateCenter03: { type: "Vector2", default: [0.5, 0.5], index: 43, alias: "第三层旋转中心(X,Y)", catalog: "基本设置", hidden: "!data.EFFECT_LAYER_THREE || !data.EFFECT_ROTATION_THREE" },
        u_RotateAngle03: { type: "Float", default: 0, index: 44, alias: "第三层旋转角度", catalog: "基本设置", hidden: "!data.EFFECT_LAYER_THREE || !data.EFFECT_ROTATION_THREE" },
        u_Translation03: { type: "Vector2", default: [0, 0], index: 45, alias: "第三层旋转位置(X,Y)", catalog: "基本设置", hidden: "!data.EFFECT_LAYER_THREE || !data.EFFECT_ROTATION_THREE" },
        u_LayerColor: { type: "Vector4", default: [1, 1, 1, 1], index: 50, alias: "叠加颜色(rgb),透明度(a)", catalog: "基本设置" },
        u_LayerMultiplier: { type: "Float", default: 1.0, index: 51, alias: "叠加因子", catalog: "基本设置" },
        u_Alpha: { type: "Float", default: 1.0, index: 52, alias: "全局透明度", catalog: "基本设置", range: [0, 1] },
        u_DissolveTex: { type: "Texture2D", default: "white", index: 100, alias: "溶解贴图", catalog: "溶解效果设置", catalogOrder: 1, hidden: "!data.EFFECT_DISSOLVE" },
        u_DissolveTex_ST: { type: "Vector4", default: [1, 1, 0, 0], index: 101, alias: "溶解TilingOffset", catalog: "溶解效果设置", hidden: "!data.EFFECT_DISSOLVE" },
        u_DissolveAmount: { type: "Float", default: 0, index: 102, alias: "溶解度", catalog: "溶解效果设置", hidden: "!data.EFFECT_DISSOLVE", range: [0, 1.001] },
        u_UseDissolveAmountMinus: { type: "Float", default: 0, index: 103, alias: "允许负溶解度", catalog: "溶解效果设置", hidden: "!data.EFFECT_DISSOLVE" },
        u_DissolveAmountTex: { type: "Texture2D", default: "black", index: 104, alias: "溶解度贴图", catalog: "溶解效果设置", hidden: "!data.EFFECT_DISSOLVE" },
        u_DissolveAmountTex_ST: { type: "Vector4", default: [1, 1, 0, 0], index: 105, alias: "溶解度TilingOffset", catalog: "溶解效果设置", hidden: "!data.EFFECT_DISSOLVE" },
        u_RotateCenter04: { type: "Vector2", default: [0.5, 0.5], index: 105.1, alias: "溶解度贴图旋转中心(X,Y)", catalog: "溶解效果设置", hidden: "!data.EFFECT_DISSOLVE || !data.EFFECT_ROTATION_FOUR" },
        u_RotateAngle04: { type: "Float", default: 0, index: 105.2, alias: "溶解度贴图旋转角度", catalog: "溶解效果设置", hidden: "!data.EFFECT_DISSOLVE || !data.EFFECT_ROTATION_FOUR" },
        u_Translation04: { type: "Vector2", default: [0, 0], index: 105.3, alias: "溶解度贴图旋转位置(X,Y)", catalog: "溶解效果设置", hidden: "!data.EFFECT_DISSOLVE || !data.EFFECT_ROTATION_FOUR" },
        u_DissolveFadeRange: { type: "Float", default: 0.1, index: 106, alias: "溶解范围", catalog: "溶解效果设置", hidden: "!data.EFFECT_DISSOLVE || data.EFFECT_FADE_EDGE", range: [0, 1] },
        u_DissolveDistortTex: { type: "Texture2D", default: "black", index: 107, alias: "溶解扭曲贴图", catalog: "溶解效果设置", hidden: "!data.EFFECT_DISSOLVE || !data.EFFECT_DISSOLVE_DISTORT" },
        u_DissolveDistortTex_ST: { type: "Vector4", default: [1, 1, 0, 0], index: 107.1, alias: "溶解扭曲TilingOffset", catalog: "溶解效果设置", hidden: "!data.EFFECT_DISSOLVE || !data.EFFECT_DISSOLVE_DISTORT" },
        u_DissolveDistortScroll: { type: "Vector2", default: [0, 0], index: 107.2, alias: "溶解扭曲UV滚动速度(U,V)", catalog: "溶解效果设置", hidden: "!data.EFFECT_DISSOLVE || !data.EFFECT_DISSOLVE_DISTORT" },
        u_DissolveDistortStrength: { type: "Float", default: 0, index: 107.3, alias: "溶解扭曲强度", catalog: "溶解效果设置", hidden: "!data.EFFECT_DISSOLVE || !data.EFFECT_DISSOLVE_DISTORT", range: [0, 10] },
        u_DissolveDistortUVMode: { type: "Int", default: 0, index: 107.4, alias: "溶解扭曲UV模式", catalog: "溶解效果设置", hidden: "!data.EFFECT_DISSOLVE || !data.EFFECT_DISSOLVE_DISTORT", enumSource: [{name: "Model", value: 0}, {name: "Screen", value: 1}] },
        u_FadeEdgeTex: { type: "Texture2D", default: "white", index: 110, alias: "溶解边缘贴图", catalog: "溶解效果设置", hidden: "!data.EFFECT_DISSOLVE || !data.EFFECT_FADE_EDGE" },
        u_FadeEdgeTex_ST: { type: "Vector4", default: [1, 1, 0, 0], index: 111, alias: "溶解边缘TilingOffset", catalog: "溶解效果设置", hidden: "!data.EFFECT_DISSOLVE || !data.EFFECT_FADE_EDGE" },
        u_FadeEdgeColor: { type: "Color", default: [1, 1, 1, 1], index: 112, alias: "溶解边缘颜色", catalog: "溶解效果设置", hidden: "!data.EFFECT_DISSOLVE || !data.EFFECT_FADE_EDGE" },
        u_FadeEdgeStrength: { type: "Float", default: 1.0, index: 113, alias: "溶解边缘强度", catalog: "溶解效果设置", hidden: "!data.EFFECT_DISSOLVE || !data.EFFECT_FADE_EDGE" },
        u_FadeEdgeType: { type: "Float", default: 0, index: 114, alias: "溶解边缘混合模式", catalog: "溶解效果设置", hidden: "!data.EFFECT_DISSOLVE || !data.EFFECT_FADE_EDGE", enumSource: [{name: "Blend", value: 0}, {name: "Add", value: 1}] },
        u_FadeEdgeRange1: { type: "Float", default: 0.1, index: 115, alias: "溶解边缘非过渡区范围", catalog: "溶解效果设置", hidden: "!data.EFFECT_DISSOLVE || !data.EFFECT_FADE_EDGE" },
        u_FadeEdgeRange2: { type: "Float", default: 0.1, index: 116, alias: "溶解边缘过渡区范围", catalog: "溶解效果设置", hidden: "!data.EFFECT_DISSOLVE || !data.EFFECT_FADE_EDGE" },
        u_EffectMainLightDir: { type: "Vector4", default: [-0.5, 0.5, 1, 0], index: 150, alias: "主光方向(xyz),阈值偏移(w)", catalog: "光照设置", catalogOrder: 2, hidden: "!data.EFFECT_LIGHTING" },
        u_EffectMainLightColor: { type: "Color", default: [1, 1, 1, 1], index: 151, alias: "主光颜色", catalog: "光照设置", hidden: "!data.EFFECT_LIGHTING" },
        u_EffectMainLightIntensity: { type: "Float", default: 5.0, index: 152, alias: "主光强度", catalog: "光照设置", hidden: "!data.EFFECT_LIGHTING", range: [0, 5] },
        u_EffectAmbientLightColor: { type: "Color", default: [0.5, 0.5, 0.5, 1], index: 153, alias: "环境光颜色", catalog: "光照设置", hidden: "!data.EFFECT_LIGHTING" },
        u_EffectSSSColor: { type: "Color", default: [0, 0, 0, 1], index: 154, alias: "SSS散射颜色", catalog: "光照设置", hidden: "!data.EFFECT_LIGHTING" },
        u_RimMap: { type: "Texture2D", default: "gray", index: 160, alias: "Rim MatCap贴图", catalog: "Rim边缘光设置", catalogOrder: 3, hidden: "!data.EFFECT_RIM || !data.EFFECT_RIM_MAP" },
        u_RimMaskMap: { type: "Texture2D", default: "white", index: 161, alias: "Rim遮罩贴图", catalog: "Rim边缘光设置", hidden: "!data.EFFECT_RIM" },
        u_RimLevel: { type: "Float", default: 1.0, index: 162, alias: "Rim强度", catalog: "Rim边缘光设置", hidden: "!data.EFFECT_RIM", range: [0, 5] },
        u_RimColor: { type: "Color", default: [0.6, 0.8, 1, 1], index: 163, alias: "Rim颜色(HDR)", catalog: "Rim边缘光设置", hidden: "!data.EFFECT_RIM || data.EFFECT_RIM_MAP" },
        u_RimSharp: { type: "Float", default: 2.0, index: 164, alias: "Rim锐度", catalog: "Rim边缘光设置", hidden: "!data.EFFECT_RIM || data.EFFECT_RIM_MAP", range: [0, 20] },
        u_RimMode: { type: "Int", default: 0, index: 165, alias: "Rim混合模式", catalog: "Rim边缘光设置", hidden: "!data.EFFECT_RIM", enumSource: [{name: "Color Add", value: 0}, {name: "Color Multiply", value: 1}, {name: "Color And Alpha Multiply", value: 2}, {name: "Color Add Front Alpha", value: 3}] },
        u_NormalMap: { type: "Texture2D", default: "bump", index: 166, alias: "法线贴图", catalog: "Rim边缘光设置", hidden: "!data.EFFECT_RIM || !data.EFFECT_NORMAL_MAP_RIM" },
        u_NormalMap_ST: { type: "Vector4", default: [1, 1, 0, 0], index: 167, alias: "法线贴图TilingOffset", catalog: "Rim边缘光设置", hidden: "!data.EFFECT_RIM || !data.EFFECT_NORMAL_MAP_RIM" },
        u_NormalMapStrength: { type: "Float", default: 1.0, index: 168, alias: "法线贴图强度", catalog: "Rim边缘光设置", hidden: "!data.EFFECT_RIM || !data.EFFECT_NORMAL_MAP_RIM", range: [0, 5] },
        u_VertexOffsetMode: { type: "Int", default: 0, index: 170, alias: "顶点偏移模式", catalog: "顶点偏移设置", catalogOrder: 4, hidden: "!data.EFFECT_VERTEX_OFFSET", enumSource: [{name: "沿法线", value: 0}, {name: "沿轴向", value: 1}] },
        u_VertexAmplitude: { type: "Float", default: 0, index: 171, alias: "顶点偏移幅度", catalog: "顶点偏移设置", hidden: "!data.EFFECT_VERTEX_OFFSET", range: [0, 10] },
        u_VertexAmplitudeTex: { type: "Texture2D", default: "white", index: 172, alias: "顶点偏移贴图", catalog: "顶点偏移设置", hidden: "!data.EFFECT_VERTEX_OFFSET" },
        u_VertexAmplitudeTex_ST: { type: "Vector4", default: [1, 1, 0, 0], index: 173, alias: "顶点偏移贴图TilingOffset", catalog: "顶点偏移设置", hidden: "!data.EFFECT_VERTEX_OFFSET" },
        u_VertexAmplitudeTexScroll: { type: "Vector2", default: [0, 0], index: 174, alias: "顶点偏移贴图UV滚动速度", catalog: "顶点偏移设置", hidden: "!data.EFFECT_VERTEX_OFFSET" },
        u_VertexAmplitudeMaskTex: { type: "Texture2D", default: "white", index: 175, alias: "顶点偏移遮罩贴图", catalog: "顶点偏移设置", hidden: "!data.EFFECT_VERTEX_OFFSET" },
        u_VertexAmplitudeMaskTex_ST: { type: "Vector4", default: [1, 1, 0, 0], index: 176, alias: "顶点偏移遮罩TilingOffset", catalog: "顶点偏移设置", hidden: "!data.EFFECT_VERTEX_OFFSET" },
        u_AlphaTestValue: { type: "Float", default: 0.5, index: 201, alias: "Alpha测试阈值", catalog: "其他设置", range: [0, 1] }
    },
    defines: {
        EFFECT_WRAPMODE_DEFAULT: { type: bool, default: true, index: 3, alias: "Default", catalog: "基本设置" },
        EFFECT_WRAPMODE_CLAMP: { type: bool, default: false, index: 4, alias: "Clamp", catalog: "基本设置" },
        EFFECT_WRAPMODE_REPEAT: { type: bool, default: false, index: 5, alias: "Repeat", catalog: "基本设置" },
        EFFECT_POLAR: { type: bool, default: false, alias: "极坐标开关", catalog: "基本设置", position: "after WrapMode" },
        EFFECT_ROTATION: { type: bool, default: false, alias: "第一层贴图旋转开关", catalog: "基本设置", position: "after u_Layer0UVMode" },
        EFFECT_DISTORT: { type: bool, default: false, alias: "第一层扰动效果开关", catalog: "基本设置", position: "after u_Translation" },
        EFFECT_GRADIENT_MAP: { type: bool, default: false, alias: "渐变映射开关", catalog: "基本设置", position: "after u_Distort0UVMode" },
        EFFECT_LAYER_ONE: { type: bool, default: true, index: 28, alias: "一层", catalog: "基本设置" },
        EFFECT_LAYER_TWO: { type: bool, default: false, index: 29, alias: "二层", catalog: "基本设置" },
        EFFECT_LAYER_THREE: { type: bool, default: false, index: 39, alias: "三层", catalog: "基本设置" },
        EFFECT_ROTATION_TWO: { type: bool, default: false, alias: "第二层贴图旋转开关", catalog: "基本设置", position: "after u_Layer1UVMode", hidden: "!data.EFFECT_LAYER_TWO && !data.EFFECT_LAYER_THREE" },
        EFFECT_ROTATION_THREE: { type: bool, default: false, alias: "第三层贴图旋转开关", catalog: "基本设置", position: "after u_Layer2UVMode", hidden: "!data.EFFECT_LAYER_THREE" },
        EFFECT_DISSOLVE: { type: bool, default: false, index: 99, alias: "溶解效果开关", catalog: "溶解效果设置", catalogOrder: 1 },
        EFFECT_ROTATION_FOUR: { type: bool, default: false, alias: "溶解度贴图旋转开关", catalog: "溶解效果设置", position: "after u_DissolveAmountTex_ST", hidden: "!data.EFFECT_DISSOLVE" },
        EFFECT_DISSOLVE_DISTORT: { type: bool, default: false, alias: "溶解扭曲开关", catalog: "溶解效果设置", position: "after u_DissolveFadeRange", hidden: "!data.EFFECT_DISSOLVE" },
        EFFECT_FADE_EDGE: { type: bool, default: false, index: 109, alias: "溶解边缘开关", catalog: "溶解效果设置", hidden: "!data.EFFECT_DISSOLVE" },
        EFFECT_LIGHTING: { type: bool, default: false, alias: "简易光照开关", catalog: "光照设置", catalogOrder: 2 },
        EFFECT_RIM: { type: bool, default: false, alias: "Rim边缘光开关", catalog: "Rim边缘光设置", catalogOrder: 3 },
        EFFECT_RIM_MAP: { type: bool, default: false, alias: "Rim MatCap贴图开关", catalog: "Rim边缘光设置", hidden: "!data.EFFECT_RIM" },
        EFFECT_NORMAL_MAP_RIM: { type: bool, default: false, alias: "法线贴图影响Rim", catalog: "Rim边缘光设置", hidden: "!data.EFFECT_RIM" },
        EFFECT_VERTEX_OFFSET: { type: bool, default: false, alias: "顶点偏移开关", catalog: "顶点偏移设置", catalogOrder: 4 }
    },
    styles: {
        materialRenderMode: { default: 5 },
        s_Blend: { default: 2 },
        s_BlendDstRGB: { default: 7 },
        s_Cull: { default: 0 },
        s_DepthWrite: { default: false },
        EFFECT_WRAPMODE_DEFAULT: { inspector: null },
        EFFECT_WRAPMODE_CLAMP: { inspector: null },
        EFFECT_WRAPMODE_REPEAT: { inspector: null },
        EFFECT_LAYER_ONE: { inspector: null },
        EFFECT_LAYER_TWO: { inspector: null },
        EFFECT_LAYER_THREE: { inspector: null }
    },
    shaderPass:[
        {
            pipeline:"Forward",
            VS:Mesh_FullEffectVS,
            FS:Mesh_FullEffectFS,
            statefirst: true,
            renderState: {
                blend: "Seperate",
                blendEquationRGB: "Add",
                blendEquationAlpha: "Add",
                srcBlendRGB: "One",
                srcBlendAlpha: "One",
                dstBlendAlpha: "Zero"
            }
        }
    ]
}
Shader3D End

GLSL Start
#defineGLSL Mesh_FullEffectVS

#define SHADER_NAME Mesh_FullEffectVS

#include "Math.glsl";

#include "Scene.glsl"
#include "SceneFogInput.glsl";

#include "Camera.glsl";

#include "Sprite3DVertex.glsl";

#include "VertexCommon.glsl";

varying vec4 v_Color;
varying vec2 v_TextureCoordinate;
varying vec2 v_Texcoord0;

#ifdef EFFECT_LIGHTING
varying vec3 v_NormalWS;
#endif

#ifdef EFFECT_RIM
    #ifdef EFFECT_NORMAL_MAP_RIM
    varying vec3 v_NormalWS_Rim;
    varying vec3 v_TangentWS;
    varying vec3 v_BinormalWS;
    #else
    varying vec3 v_NormalView;
    #endif
#endif

vec2 TransformUV(vec2 texcoord, vec4 tilingOffset)
{
    vec2 transTexcoord = vec2(texcoord.x, texcoord.y - 1.0) * tilingOffset.xy + vec2(tilingOffset.z, -tilingOffset.w);
    transTexcoord.y += 1.0;
    return transTexcoord;
}

void main()
{
    Vertex vertex;
    getVertexParams(vertex);

    mat4 worldMat = getWorldMatrix();

    // Vertex Offset
    vec3 posOS = vertex.positionOS;
    #ifdef EFFECT_VERTEX_OFFSET
        vec2 vtxAmpUV = vertex.texCoord0 * u_VertexAmplitudeTex_ST.xy + u_VertexAmplitudeTex_ST.zw
            + fract(vec2(u_VertexAmplitudeTexScroll.x, -u_VertexAmplitudeTexScroll.y) * u_Time);
        vec4 vertexAmpTex = texture2DLodEXT(u_VertexAmplitudeTex, vtxAmpUV, 0.0);
        vec2 vtxMaskUV = vertex.texCoord0 * u_VertexAmplitudeMaskTex_ST.xy + u_VertexAmplitudeMaskTex_ST.zw;
        vec4 vertexMaskTex = texture2DLodEXT(u_VertexAmplitudeMaskTex, vtxMaskUV, 0.0);
        if (u_VertexOffsetMode == 1) {
            posOS += u_VertexAmplitude * (2.0 * vertexAmpTex.rgb - 1.0) * vertexMaskTex.r;
        } else {
            posOS += vertex.normalOS * u_VertexAmplitude * vertexAmpTex.r * vertexMaskTex.r;
        }
    #endif

    vec4 pos = (worldMat * vec4(posOS, 1.0));
    vec3 positionWS = pos.xyz / pos.w;

    gl_Position = getPositionCS(positionWS);

    #ifdef COLOR
        v_Color = vertex.vertexColor;
    #else
        v_Color = vec4(1.0);
    #endif
    v_Texcoord0 = vertex.texCoord0;
    v_TextureCoordinate = TransformUV(vertex.texCoord0, u_TilingOffset);

    // Normal for Lighting
    #ifdef EFFECT_LIGHTING
        mat3 normalMat = mat3(worldMat);
        v_NormalWS = normalize(normalMat * vertex.normalOS);
    #endif

    // Normal for Rim
    #ifdef EFFECT_RIM
        #ifdef EFFECT_NORMAL_MAP_RIM
            mat3 nMat = mat3(worldMat);
            v_NormalWS_Rim = normalize(nMat * vertex.normalOS);
            v_TangentWS = normalize(nMat * vertex.tangentOS.xyz);
            float sign = vertex.tangentOS.w;
            v_BinormalWS = cross(v_NormalWS_Rim, v_TangentWS) * sign;
        #else
            // normalView = (MV_IT) * normal, for non-uniform scale use inverse transpose
            mat3 nMat3 = mat3(worldMat);
            vec3 normalWS = normalize(nMat3 * vertex.normalOS);
            v_NormalView = normalize(mat3(u_View) * normalWS);
        #endif
    #endif

    gl_Position = remapPositionZ(gl_Position);
    #ifdef FOG
        FogHandle(gl_Position.z);
    #endif
}

#endGLSL

#defineGLSL Mesh_FullEffectFS

#define SHADER_NAME Mesh_FullEffectFS

#include "Color.glsl";

#include "Scene.glsl";
#include "SceneFog.glsl";

#include "Camera.glsl";

#include "Sprite3DFrag.glsl";

varying vec4 v_Color;
varying vec2 v_TextureCoordinate;
varying vec2 v_Texcoord0;

#ifdef EFFECT_LIGHTING
varying vec3 v_NormalWS;
#endif

#ifdef EFFECT_RIM
    #ifdef EFFECT_NORMAL_MAP_RIM
    varying vec3 v_NormalWS_Rim;
    varying vec3 v_TangentWS;
    varying vec3 v_BinormalWS;
    #else
    varying vec3 v_NormalView;
    #endif
#endif

vec2 TransformUV(vec2 texcoord, vec4 tilingOffset)
{
    vec2 transTexcoord = vec2(texcoord.x, texcoord.y - 1.0) * tilingOffset.xy + vec2(tilingOffset.z, -tilingOffset.w);
    transTexcoord.y += 1.0;
    return transTexcoord;
}

vec2 RotateUV(vec2 uv, vec2 center, float angleDeg, vec2 trans)
{
    float rad = angleDeg * 0.01745329;
    float s = sin(rad);
    float c = cos(rad);
    vec2 delta = uv - center + trans;
    return vec2(delta.x * c - delta.y * s, delta.x * s + delta.y * c) + center;
}

vec2 PolarCoordinates(vec2 UV, vec2 Center, float RadialScale, float LengthScale)
{
    vec2 delta = UV - Center;
    float radius = length(delta) * 2.0 * RadialScale;
    float angle = atan(delta.x, delta.y) * (1.0 / 6.28318530718) * LengthScale;
    return vec2(radius, angle);
}

void main()
{
    vec2 baseUV = v_Texcoord0;
    float time = u_Time;

    // Screen-Space UV
    vec2 screenUV = gl_FragCoord.xy / u_Viewport.zw;

    // Polar Coordinates
    #ifdef EFFECT_POLAR
        baseUV = PolarCoordinates(baseUV, u_PolarControl.xy, u_PolarControl.z, u_PolarControl.w);
    #endif

    // Dissolve
    #ifdef EFFECT_DISSOLVE
        vec2 dissolveUV = TransformUV(baseUV, u_DissolveTex_ST);

        #ifdef EFFECT_DISSOLVE_DISTORT
            vec2 dissolveDistortBase = u_DissolveDistortUVMode == 0 ? baseUV : screenUV;
            vec2 dissolveDistortUV = TransformUV(dissolveDistortBase, u_DissolveDistortTex_ST) + fract(vec2(u_DissolveDistortScroll.x, -u_DissolveDistortScroll.y) * time);
            dissolveUV += texture2D(u_DissolveDistortTex, dissolveDistortUV).xy * u_DissolveDistortStrength;
        #endif

        float dissolveValue = texture2D(u_DissolveTex, dissolveUV).r;
        float dissolveAmount = u_DissolveAmount;

        #ifdef EFFECT_ROTATION_FOUR
            vec2 dissolveAmtBaseUV = RotateUV(baseUV, u_RotateCenter04, u_RotateAngle04, u_Translation04);
        #else
            vec2 dissolveAmtBaseUV = baseUV;
        #endif
        vec2 dissolveAmtUV = TransformUV(dissolveAmtBaseUV, u_DissolveAmountTex_ST);
        dissolveAmount += texture2D(u_DissolveAmountTex, dissolveAmtUV).r;
        dissolveAmount = min(1.001, dissolveAmount);

        #ifdef EFFECT_FADE_EDGE
            float fadeFactor = dissolveValue;
            float fadeValue = dissolveAmount;
            if (fadeFactor < fadeValue) discard;

            vec2 fadeEdgeUV = TransformUV(baseUV, u_FadeEdgeTex_ST);
            vec4 fadeEdgeTex = texture2D(u_FadeEdgeTex, fadeEdgeUV);
            vec3 fadeEdgeColor = fadeEdgeTex.rgb * u_FadeEdgeColor.rgb * u_FadeEdgeStrength;

            float fadeEdgeValue1 = u_FadeEdgeRange1 + fadeValue;
            float fadeEdgeValue2 = fadeEdgeValue1 + u_FadeEdgeRange2;
            float fadeEdgeRange = fadeEdgeValue2 - fadeEdgeValue1;
            float fadeAlpha = clamp((fadeEdgeValue2 - fadeFactor) / max(0.0001, fadeEdgeRange), 0.0, 1.0);
        #else
            float dissolveFactor = 1.0;
            if (dissolveAmount > 0.0 || u_UseDissolveAmountMinus > 0.0)
            {
                if (u_DissolveFadeRange <= 0.001) {
                    if (dissolveValue < dissolveAmount) discard;
                } else {
                    dissolveFactor = (1.0 - step(dissolveValue, dissolveAmount)) * smoothstep(dissolveAmount, dissolveAmount + u_DissolveFadeRange, dissolveValue);
                }
            }
        #endif
    #endif

    // Main texture UV
    vec2 mainUV;
    if (u_Layer0UVMode == 1) {
        mainUV = TransformUV(screenUV, u_TilingOffset);
    } else {
        #ifdef EFFECT_POLAR
            mainUV = TransformUV(baseUV, u_TilingOffset);
        #else
            mainUV = v_TextureCoordinate;
        #endif
    }
    mainUV += fract(vec2(u_Scroll0.x, -u_Scroll0.y) * time);

    // Distortion
    #ifdef EFFECT_DISTORT
        vec2 distortBase = u_Distort0UVMode == 0 ? baseUV : screenUV;
        vec2 distortUV = TransformUV(distortBase, u_DistortTex_ST) + fract(vec2(u_DistortScroll.x, -u_DistortScroll.y) * time);
        mainUV += texture2D(u_DistortTex, distortUV).xy * u_DistortStrength;
    #endif

    // WrapMode
    #ifdef EFFECT_WRAPMODE_CLAMP
        mainUV = clamp(mainUV, 0.0, 1.0);
    #endif
    #ifdef EFFECT_WRAPMODE_REPEAT
        mainUV = fract(mainUV);
    #endif

    // UV Rotation (layer 1)
    #ifdef EFFECT_ROTATION
        mainUV = RotateUV(mainUV, u_RotateCenter, u_RotateAngle, u_Translation);
    #endif

    // Sample main texture
    vec4 texColor = texture2D(u_texture, mainUV);
    #ifdef Gamma_u_texture
        texColor = gammaToLinear(texColor);
    #endif

    // Gradient Map
    #ifdef EFFECT_GRADIENT_MAP
        vec2 gradientUV = u_GradientMapTex0_ST.xy * vec2(texColor.r, 0.5) + u_GradientMapTex0_ST.zw;
        texColor = texture2D(u_GradientMapTex0, gradientUV);
    #endif

    // 2nd Layer
    #if defined(EFFECT_LAYER_TWO) || defined(EFFECT_LAYER_THREE)
        vec2 layer1Base = u_Layer1UVMode == 0 ? baseUV : screenUV;
        vec2 detailUV = TransformUV(layer1Base, u_DetailTex_ST) + fract(vec2(u_Scroll1.x, -u_Scroll1.y) * time);
        #ifdef EFFECT_ROTATION_TWO
            detailUV = RotateUV(detailUV, u_RotateCenter02, u_RotateAngle02, u_Translation02);
        #endif
        texColor *= texture2D(u_DetailTex, detailUV);
    #endif

    // 3rd Layer
    #ifdef EFFECT_LAYER_THREE
        vec2 layer2Base = u_Layer2UVMode == 0 ? baseUV : screenUV;
        vec2 detail2UV = TransformUV(layer2Base, u_DetailTex2_ST) + fract(vec2(u_Scroll2.x, -u_Scroll2.y) * time);
        #ifdef EFFECT_ROTATION_THREE
            detail2UV = RotateUV(detail2UV, u_RotateCenter03, u_RotateAngle03, u_Translation03);
        #endif
        texColor *= texture2D(u_DetailTex2, detail2UV);
    #endif

    // Layer color tint and multiplier
    texColor *= u_LayerColor * u_LayerMultiplier * u_Alpha;

    // Premultiply alpha
    texColor.rgb *= texColor.a;

    // Simple Lighting (direction light + ambient + SSS)
    #ifdef EFFECT_LIGHTING
        vec3 normalWorld = normalize(v_NormalWS);
        vec3 effectMainLightDir = normalize(u_EffectMainLightDir.xyz);
        float snl = dot(normalWorld, effectMainLightDir);
        float w = fwidth(snl) * 2.0;
        vec3 spec = vec3(mix(0.0, 1.0, smoothstep(-w, w * 2.0, snl + u_EffectMainLightDir.w - 1.0)) * step(0.0001, u_EffectMainLightDir.w));
        texColor.rgb *= spec * u_EffectMainLightIntensity * u_EffectMainLightColor.rgb * max((u_EffectSSSColor.rgb + snl) / (u_EffectSSSColor.rgb + 1.0), 0.0) + u_EffectAmbientLightColor.rgb;
    #endif

    // Rim Edge Light
    #ifdef EFFECT_RIM
        #ifdef EFFECT_NORMAL_MAP_RIM
            vec3 tangent = v_TangentWS;
            vec3 binormal = v_BinormalWS;
            vec3 normal = v_NormalWS_Rim;
            vec3 normalMapVal = texture2D(u_NormalMap, baseUV * u_NormalMap_ST.xy + u_NormalMap_ST.zw).xyz;
            normalMapVal.xy = (normalMapVal.xy * 2.0 - 1.0) * u_NormalMapStrength;
            normalMapVal.z = sqrt(1.0 - clamp(dot(normalMapVal.xy, normalMapVal.xy), 0.0, 1.0));
            vec3 normalWorldRim = normalize(normalMapVal.x * tangent + normalMapVal.y * binormal + normalMapVal.z * normal);
            vec3 normalView = normalize(mat3(u_View) * normalWorldRim);
        #else
            vec3 normalView = normalize(v_NormalView);
        #endif

        float rimMask = texture2D(u_RimMaskMap, baseUV).r;
        #ifdef EFFECT_RIM_MAP
            vec2 rimUV = normalView.xy * 0.5 + 0.5;
            vec4 rimColor = texture2D(u_RimMap, rimUV);
            float rimFactor = (rimColor.x + rimColor.y + rimColor.z) * rimColor.w * u_RimLevel * rimMask;
            vec3 rimCol = rimColor.rgb;
        #else
            float rimFactor = pow(length(normalView.xy), u_RimSharp) * 2.0 * u_RimLevel * rimMask;
            vec3 rimCol = u_RimColor.rgb;
        #endif

        if (u_RimMode == 1) {
            texColor.rgb *= rimCol * rimFactor;
        } else if (u_RimMode == 2) {
            texColor.rgb *= rimCol * rimFactor;
            texColor.a *= rimFactor;
        } else if (u_RimMode == 3) {
            texColor.rgb += rimCol * rimFactor * texColor.a;
        } else {
            texColor.rgb += rimCol * rimFactor;
        }
    #endif

    // Apply dissolve
    #ifdef EFFECT_DISSOLVE
        #ifdef EFFECT_FADE_EDGE
            if (u_FadeEdgeType < 0.5) {
                texColor.rgb = mix(texColor.rgb, fadeEdgeColor, fadeAlpha);
            } else {
                texColor.rgb += fadeAlpha * fadeEdgeColor;
            }
        #else
            texColor.a *= dissolveFactor;
        #endif
    #endif

    // Final color composition
    vec4 finalColor = texColor * v_Color;

    #ifdef ALPHATEST
        if (finalColor.a < u_AlphaTestValue) discard;
    #endif

    #ifdef FOG
        finalColor.rgb = scenUnlitFog(finalColor.rgb);
    #endif

    gl_FragColor = finalColor;
    gl_FragColor = outputTransform(gl_FragColor);
}

#endGLSL
GLSL End
