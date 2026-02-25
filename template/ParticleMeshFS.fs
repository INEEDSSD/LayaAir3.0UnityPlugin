#version 300 es
#if defined(GL_FRAGMENT_PRECISION_HIGH)
    precision highp float;
    precision highp int;
    precision highp sampler2DArray;
    precision highp sampler3D;
#else
    precision mediump float;
    precision mediump int;
    precision mediump sampler2DArray;
    precision mediump sampler3D;
#endif
layout(std140, column_major) uniform;
#define varying in
out highp vec4 pc_fragColor;
#define gl_FragColor pc_fragColor
#define gl_FragDepthEXT gl_FragDepth
#define texture2D texture
#define textureCube texture
#define texture2DProj textureProj
#define texture2DLodEXT textureLod
#define texture2DProjLodEXT textureProjLod
#define textureCubeLodEXT textureLod
#define texture2DGradEXT textureGrad
#define texture2DProjGradEXT textureProjGrad
#define textureCubeGradEXT textureGrad

uniform Material {
    vec4 u_Tintcolor;
    vec4 u_TilingOffset;
    float u_AlphaTestValue;
};
uniform sampler2D u_texture;
#define MAX_LIGHT_COUNT 32
#define MAX_LIGHT_COUNT_PER_CLUSTER 32
#define CLUSTER_X_COUNT 12
#define CLUSTER_Y_COUNT 12
#define CLUSTER_Z_COUNT 12
#define MORPH_MAX_COUNT 32
#define SHADER_CAPAILITY_LEVEL 35
#define REMAP_Z
#define ENUNIFORMBLOCK
#define RENDERMODE_MESH
#define SHAPE
#define TINTCOLOR
#define GRAPHICS_API_GLES3
#define SHADER_NAME ParticleFS
#if !defined(SceneCommon_lib)
    #define SceneCommon_lib
    uniform Scene3D {
        float u_Time;
        vec4 u_FogParams;
        vec4 u_FogColor;
        float u_GIRotate;
        int u_DirationLightCount;
    };
#endif
#if !defined(SceneFog_lib)
    #define SceneFog_lib
#endif
#if !defined(Color_lib)
    #define Color_lib
    #if !defined(Math_lib)
        #define Math_lib
        #define PI 3.14159265359
        #define INVERT_PI 0.31830988618
        #define HALF_PI 1.570796327
        #define MEDIUMP_FLT_MAX 65504.0
        #define MEDIUMP_FLT_MIN 0.00006103515625
        #if defined(GL_FRAGMENT_PRECISION_HIGH)
            #define FLT_EPS 1e-5
            #define saturateMediump(x) x
        #else
            #define FLT_EPS MEDIUMP_FLT_MIN
            #define saturateMediump(x) min(x, MEDIUMP_FLT_MAX)
        #endif
        #define saturate(x) clamp(x, 0.0, 1.0)
        float pow2(float x) {
            return x*x;
        }
        vec3 pow2(vec3 x) {
            return x*x;
        }
        float pow5(float x) {
            float x2 = x*x;
            return x2*x2*x;
        }
        const float INVERT_LOG10 = 0.43429448190325176;
        float log10(float x) {
            return log(x)*INVERT_LOG10;
        }
        float vecmax(const vec2 v) {
            return max(v.x, v.y);
        }
        float vecmax(const vec3 v) {
            return max(v.x, max(v.y, v.z));
        }
        float vecmax(const vec4 v) {
            return max(max(v.x, v.y), max(v.z, v.w));
        }
        float vecmin(const vec2 v) {
            return min(v.x, v.y);
        }
        float vecmin(const vec3 v) {
            return min(v.x, min(v.y, v.z));
        }
        float vecmin(const vec4 v) {
            return min(min(v.x, v.y), min(v.z, v.w));
        }
        vec3 SafeNormalize(in vec3 inVec) {
            float dp3 = max(0.001, dot(inVec, inVec));
            return inVec*inversesqrt(dp3);
        }
        vec3 normalScale(in vec3 normal, in float scale) {
            normal *= vec3(scale, scale, 1.0);
            return normalize(normal);
        }
        float acosFast(float x) {
            float y = abs(x);
            float p = -0.1565827*y+1.570796;
            p *= sqrt(1.0-y);
            return x >= 0.0 ? p : PI-p;
        }
        float acosFastPositive(float x) {
            float p = -0.1565827*x+1.570796;
            return p*sqrt(1.0-x);
        }
        float interleavedGradientNoise(const highp vec2 w) {
            const vec3 m = vec3(0.06711056, 0.00583715, 52.9829189);
            return fract(m.z*fract(dot(w, m.xy)));
        }
        vec3 rotationByEuler(in vec3 vector, in vec3 rot) {
            float halfRoll = rot.z*0.5;
            float halfPitch = rot.x*0.5;
            float halfYaw = rot.y*0.5;
            float sinRoll = sin(halfRoll);
            float cosRoll = cos(halfRoll);
            float sinPitch = sin(halfPitch);
            float cosPitch = cos(halfPitch);
            float sinYaw = sin(halfYaw);
            float cosYaw = cos(halfYaw);
            float quaX = (cosYaw*sinPitch*cosRoll)+(sinYaw*cosPitch*sinRoll);
            float quaY = (sinYaw*cosPitch*cosRoll)-(cosYaw*sinPitch*sinRoll);
            float quaZ = (cosYaw*cosPitch*sinRoll)-(sinYaw*sinPitch*cosRoll);
            float quaW = (cosYaw*cosPitch*cosRoll)+(sinYaw*sinPitch*sinRoll);
            float x = quaX+quaX;
            float y = quaY+quaY;
            float z = quaZ+quaZ;
            float wx = quaW*x;
            float wy = quaW*y;
            float wz = quaW*z;
            float xx = quaX*x;
            float xy = quaX*y;
            float xz = quaX*z;
            float yy = quaY*y;
            float yz = quaY*z;
            float zz = quaZ*z;
            return vec3(((vector.x*((1.0-yy)-zz))+(vector.y*(xy-wz)))+(vector.z*(xz+wy)), ((vector.x*(xy+wz))+(vector.y*((1.0-xx)-zz)))+(vector.z*(yz-wx)), ((vector.x*(xz-wy))+(vector.y*(yz+wx)))+(vector.z*((1.0-xx)-yy)));
        }
        vec3 rotationByAxis(in vec3 vector, in vec3 axis, in float angle) {
            float halfAngle = angle*0.5;
            float sinf = sin(halfAngle);
            float quaX = axis.x*sinf;
            float quaY = axis.y*sinf;
            float quaZ = axis.z*sinf;
            float quaW = cos(halfAngle);
            float x = quaX+quaX;
            float y = quaY+quaY;
            float z = quaZ+quaZ;
            float wx = quaW*x;
            float wy = quaW*y;
            float wz = quaW*z;
            float xx = quaX*x;
            float xy = quaX*y;
            float xz = quaX*z;
            float yy = quaY*y;
            float yz = quaY*z;
            float zz = quaZ*z;
            return vec3(((vector.x*((1.0-yy)-zz))+(vector.y*(xy-wz)))+(vector.z*(xz+wy)), ((vector.x*(xy+wz))+(vector.y*((1.0-xx)-zz)))+(vector.z*(yz-wx)), ((vector.x*(xz-wy))+(vector.y*(yz+wx)))+(vector.z*((1.0-xx)-yy)));
        }
        vec3 rotationByQuaternions(in vec3 v, in vec4 q) {
            return v+2.0*cross(q.xyz, cross(q.xyz, v)+q.w*v);
        }
    #endif
    vec3 linearToGamma(in vec3 value) {
        return pow(value, vec3(1.0/2.2));
    }
    vec4 linearToGamma(in vec4 value) {
        return vec4(linearToGamma(value.rgb), value.a);
    }
    vec3 gammaToLinear(in vec3 value) {
        return pow(value, vec3(2.2));
    }
    vec4 gammaToLinear(in vec4 value) {
        return vec4(gammaToLinear(value.rgb), value.a);
    }
    const float c_RGBDMaxRange = 255.0;
    vec4 encodeRGBD(in vec3 color) {
        float maxRGB = max(vecmax(color), FLT_EPS);
        float d = max(1.0, c_RGBDMaxRange/maxRGB);
        d = saturate(d/255.0);
        vec3 rgb = color.rgb*d;
        rgb = saturate(rgb);
        return vec4(rgb, d);
    }
    vec3 decodeRGBD(in vec4 rgbd) {
        vec3 color = rgbd.rgb*(1.0/rgbd.a);
        return color;
    }
    vec4 encodeRGBM(in vec3 color, float range) {
        color *= 1.0/range;
        float maxRGB = max(vecmax(color), FLT_EPS);
        float m = ceil(maxRGB*255.0)/255.0;
        vec3 rgb = color.rgb*1.0/m;
        vec4 rgbm = vec4(rgb, m);
        return rgbm;
    }
    vec3 decodeRGBM(in vec4 rgbm, float range) {
        return range*rgbm.rgb*rgbm.a;
    }
    #if !defined(OutputTransform_lib)
        #define OutputTransform_lib
        vec3 gammaCorrect(in vec3 color, float gammaValue) {
            return pow(color, vec3(gammaValue));
        }
        vec4 gammaCorrect(in vec4 color) {
            float gammaValue = 1.0/2.2;
            return vec4(gammaCorrect(color.rgb, gammaValue), color.a);
        }
        vec4 outputTransform(in vec4 color) {
            return color;
        }
    #endif
#endif
const vec4 c_ColorSpace = vec4(4.59479380, 4.59479380, 4.59479380, 2.0);
varying vec4 v_Color;
varying vec2 v_TextureCoordinate;
varying vec4 v_MeshColor;
void main() {
    vec4 color;
    color = v_MeshColor;
    color *= u_Tintcolor*c_ColorSpace*v_Color;
    gl_FragColor = color;
    gl_FragColor = outputTransform(gl_FragColor);
}
