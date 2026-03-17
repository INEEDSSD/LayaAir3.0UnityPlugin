Shader3D Start
{
    type:Shader3D,
    name:UI_Particles_Additive,
    enableInstancing:true,
    supportReflectionProbe:false,
    shaderType:D2_BaseRenderNode2D,
    uniformMap:{
        u_TintColor: { type: Vector4, default: [1, 1, 1, 1] },
    },
    attributeMap: {
        a_position: Vector4,
        a_color: Vector4,
        a_uv: Vector2,
    },
    defines: {
        BASERENDER2D: { type: bool, default: true }
    }
    shaderPass:[
        {
            pipeline:Forward,
            VS:UI_Particles_AdditiveVS,
            FS:UI_Particles_AdditiveFS,
            statefirst: true,
            renderState: {
                blend: "Seperate",
                blendEquationRGB: "Add",
                blendEquationAlpha: "Add",
                srcBlendRGB: "SourceAlpha",
                dstBlendRGB: "One",
                srcBlendAlpha: "SourceAlpha",
                dstBlendAlpha: "One"
            }
        }
    ]
}
Shader3D End

GLSL Start
#defineGLSL UI_Particles_AdditiveVS

    #define SHADER_NAME UI_Particles_Additive

    #include "Sprite2DVertex.glsl";

    void main() {
        vertexInfo info;
        getVertexInfo(info);

        v_texcoord = info.uv;
        v_color = info.color;

        #ifdef LIGHT2D_ENABLE
            lightAndShadow(info);
        #endif

        gl_Position = getPosition(info.pos);
    }

#endGLSL

#defineGLSL UI_Particles_AdditiveFS
    #define SHADER_NAME UI_Particles_Additive
    #if defined(GL_FRAGMENT_PRECISION_HIGH)
    precision highp float;
    #else
    precision mediump float;
    #endif

    #include "Sprite2DFrag.glsl";

    void main()
    {
        clip();
        vec4 textureColor = texture2D(u_baseRender2DTexture, v_texcoord);
        // Fix Laya linear space: convert texture and TintColor back to gamma
        textureColor = vec4(linearToGamma(textureColor.rgb), linearToGamma(vec3(textureColor.a, 0.0, 0.0)).r);
        vec4 tint = vec4(linearToGamma(u_TintColor.rgb), linearToGamma(vec3(u_TintColor.a, 0.0, 0.0)).r);
        gl_FragColor = v_color * tint * textureColor;
    }

#endGLSL
GLSL End
