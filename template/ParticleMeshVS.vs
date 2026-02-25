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
#define attribute in
#define varying out
#define textureCube texture
#define texture2D texture
attribute vec4 a_CornerTextureCoordinate;
attribute vec3 a_MeshPosition;
attribute vec4 a_MeshColor;
attribute vec2 a_MeshTextureCoordinate;
attribute vec4 a_ShapePositionStartLifeTime;
attribute vec4 a_DirectionTime;
attribute vec4 a_StartColor;
attribute vec3 a_StartSize;
attribute vec3 a_StartRotation0;
attribute float a_StartSpeed;
attribute vec4 a_Random0;
attribute vec4 a_Random1;
attribute vec3 a_SimulationWorldPostion;
attribute vec4 a_SimulationWorldRotation;
attribute vec4 a_SimulationUV;
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
#define SHADER_NAME ParticleVS
#if !defined(CameraCommon_lib)
    #define CameraCommon_lib
    uniform BaseCamera {
        vec3 u_CameraPos;
        mat4 u_View;
        mat4 u_Projection;
        mat4 u_ViewProjection;
        vec3 u_CameraDirection;
        vec3 u_CameraUp;
        vec4 u_Viewport;
        vec4 u_ProjectionParams;
        vec4 u_OpaqueTextureParams;
        vec4 u_ZBufferParams;
    };
    uniform sampler2D u_CameraDepthTexture;
    uniform sampler2D u_CameraDepthNormalsTexture;
    uniform sampler2D u_CameraOpaqueTexture;
    vec4 getPositionCS(in vec3 positionWS) {
        return u_ViewProjection*vec4(positionWS, 1.0);
    }
    vec3 getViewDirection(in vec3 positionWS) {
        return normalize(u_CameraPos-positionWS);
    }
    vec4 remapPositionZ(vec4 position) {
        position.z = position.z*2.0-position.w;
        return position;
    }
#endif
uniform float u_CurrentTime;
uniform vec3 u_Gravity;
uniform vec2 u_DragConstanct;
uniform vec3 u_WorldPosition;
uniform vec4 u_WorldRotation;
uniform int u_ThreeDStartRotation;
uniform int u_Shape;
uniform int u_ScalingMode;
uniform vec3 u_PositionScale;
uniform vec3 u_SizeScale;
uniform float u_StretchedBillboardLengthScale;
uniform float u_StretchedBillboardSpeedScale;
uniform int u_SimulationSpace;
#if defined(VELOCITYOVERLIFETIMECONSTANT) || defined(VELOCITYOVERLIFETIMECURVE) || defined(VELOCITYOVERLIFETIMERANDOMCONSTANT) || defined(VELOCITYOVERLIFETIMERANDOMCURVE)
    uniform int u_VOLSpaceType;
#endif
#if defined(VELOCITYOVERLIFETIMECONSTANT) || defined(VELOCITYOVERLIFETIMERANDOMCONSTANT)
    uniform vec3 u_VOLVelocityConst;
#endif
#if defined(VELOCITYOVERLIFETIMECURVE) || defined(VELOCITYOVERLIFETIMERANDOMCURVE)
    uniform vec4 u_VOLVelocityGradientX[2];
    uniform vec4 u_VOLVelocityGradientY[2];
    uniform vec4 u_VOLVelocityGradientZ[2];
#endif
#define COLORCOUNT 4
#define COLORCOUNT_HALF 2
#if defined(SIZEOVERLIFETIMECURVE) || defined(SIZEOVERLIFETIMERANDOMCURVES)
    uniform vec4 u_SOLSizeGradient[2];
#endif
#if defined(SIZEOVERLIFETIMECURVESEPERATE) || defined(SIZEOVERLIFETIMERANDOMCURVESSEPERATE)
    uniform vec4 u_SOLSizeGradientX[2];
    uniform vec4 u_SOLSizeGradientY[2];
    uniform vec4 u_SOLSizeGradientZ[2];
#endif
#if defined(TEXTURESHEETANIMATIONCURVE) || defined(TEXTURESHEETANIMATIONRANDOMCURVE)
    uniform float u_TSACycles;
    uniform vec2 u_TSASubUVLength;
    uniform vec4 u_TSAGradientUVs[2];
#endif
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
vec2 getVec2ValueByIndexFromeVec4Array(in vec4 gradientNumbers[2], in int vec2Index) {
    int v4Index = int(floor(float(vec2Index)/2.0));
    int offset = (vec2Index-v4Index*2)*2;
    return vec2(gradientNumbers[v4Index][offset], gradientNumbers[v4Index][offset+1]);
}
vec2 getVec2ValueByIndexFromeVec4Array_COLORCOUNT(in vec4 gradientNumbers[COLORCOUNT_HALF], in int vec2Index) {
    int v4Index = int(floor(float(vec2Index)/2.0));
    int offset = (vec2Index-v4Index*2)*2;
    vec4 v4Value = gradientNumbers[v4Index];
    return vec2(v4Value[offset], v4Value[offset+1]);
}
float getCurValueFromGradientFloat(in vec4 gradientNumbers[2], in float normalizedAge) {
    float curValue;
    for(int i = 1;i<4;i++) {
        vec2 gradientNumber;
        gradientNumber = getVec2ValueByIndexFromeVec4Array(gradientNumbers, i);
        float key = gradientNumber.x;
        curValue = gradientNumber.y;
        if(key >= normalizedAge) {
            vec2 lastGradientNumber;
            lastGradientNumber = getVec2ValueByIndexFromeVec4Array(gradientNumbers, i-1);
            float lastKey = lastGradientNumber.x;
            float age = max((normalizedAge-lastKey), 0.0)/(key-lastKey);
            curValue = mix(lastGradientNumber.y, gradientNumber.y, age);
            break;
        }

    }
    return curValue;
}
float getTotalValueFromGradientFloat(in vec4 gradientNumbers[2], in float normalizedAge) {
    vec2 val = getVec2ValueByIndexFromeVec4Array(gradientNumbers, 0);
    float keyTime = min(normalizedAge, val.x);
    float totalValue = keyTime*val.y;
    float lastSpeed = 0.;
    for(int i = 1;i<4;i++) {
        vec2 gradientNumber = getVec2ValueByIndexFromeVec4Array(gradientNumbers, i);
        vec2 lastGradientNumber = getVec2ValueByIndexFromeVec4Array(gradientNumbers, i-1);
        float key = gradientNumber.x;
        float lastValue = lastGradientNumber.y;
        if(key >= normalizedAge) {
            float lastKey = lastGradientNumber.x;
            float time = max((normalizedAge-lastKey), 0.);
            float age = time/(key-lastKey);
            lastSpeed = mix(lastValue, gradientNumber.y, age);
            totalValue += (lastValue+mix(lastValue, gradientNumber.y, age))/2.0*a_ShapePositionStartLifeTime.w*time;
            keyTime = normalizedAge;
            break;
        }
        else if(key>keyTime) {
            totalValue += (lastValue+gradientNumber.y)/2.0*a_ShapePositionStartLifeTime.w*(key-lastGradientNumber.x);
            keyTime = key;
            lastSpeed = gradientNumber.y;
        }

    }
    return totalValue+max(normalizedAge-keyTime, 0.)*lastSpeed*a_ShapePositionStartLifeTime.w;
}
vec4 getColorFromGradient(in vec4 gradientAlphas[COLORCOUNT_HALF], in vec4 gradientColors[COLORCOUNT], in float normalizedAge, in vec4 keyRanges) {
    float alphaAge = clamp(normalizedAge, keyRanges.z, keyRanges.w);
    vec4 overTimeColor;
    for(int i = 1;i<COLORCOUNT;i++) {
        vec2 gradientAlpha = getVec2ValueByIndexFromeVec4Array_COLORCOUNT(gradientAlphas, i);
        float alphaKey = gradientAlpha.x;
        if(alphaKey >= alphaAge) {
            vec2 lastGradientAlpha = getVec2ValueByIndexFromeVec4Array_COLORCOUNT(gradientAlphas, i-1);
            float lastAlphaKey = lastGradientAlpha.x;
            float age = clamp((alphaAge-lastAlphaKey)/(alphaKey-lastAlphaKey), 0.0, 1.0);
            overTimeColor.a = mix(lastGradientAlpha.y, gradientAlpha.y, age);
            break;
        }

    }
    float colorAge = clamp(normalizedAge, keyRanges.x, keyRanges.y);
    for(int i = 1;i<COLORCOUNT;i++) {
        vec4 gradientColor = gradientColors[i];
        float colorKey = gradientColor.x;
        if(colorKey >= colorAge) {
            vec4 lastGradientColor = gradientColors[i-1];
            float lastColorKey = lastGradientColor.x;
            float age = (colorAge-lastColorKey)/(colorKey-lastColorKey);
            overTimeColor.rgb = mix(gradientColors[i-1].yzw, gradientColor.yzw, age);
            break;
        }

    }
    return overTimeColor;
}
float getFrameFromGradient(in vec4 gradientFrames[2], in float normalizedAge) {
    float overTimeFrame;
    for(int i = 1;i<4;i++) {
        vec2 gradientFrame = getVec2ValueByIndexFromeVec4Array(gradientFrames, i);
        float key = gradientFrame.x;
        overTimeFrame = gradientFrame.y;
        if(key >= normalizedAge) {
            vec2 lastGradientFrame = getVec2ValueByIndexFromeVec4Array(gradientFrames, i-1);
            float lastKey = lastGradientFrame.x;
            float age = max((normalizedAge-lastKey), 0.)/(key-lastKey);
            overTimeFrame = mix(lastGradientFrame.y, gradientFrame.y, age);
            break;
        }

    }
    return floor(overTimeFrame);
}
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
varying vec4 v_MeshColor;
varying vec4 v_Color;
varying vec2 v_TextureCoordinate;
vec2 TransformUV(vec2 texcoord, vec4 tilingOffset) {
    vec2 transTexcoord = vec2(texcoord.x, texcoord.y-1.0)*tilingOffset.xy+vec2(tilingOffset.z, -tilingOffset.w);
    transTexcoord.y += 1.0;
    return transTexcoord;
}
#if defined(VELOCITYOVERLIFETIMECONSTANT) || defined(VELOCITYOVERLIFETIMECURVE) || defined(VELOCITYOVERLIFETIMERANDOMCONSTANT) || defined(VELOCITYOVERLIFETIMERANDOMCURVE)
    vec3 computeParticleLifeVelocity(in float normalizedAge) {
        vec3 outLifeVelocity;
        return outLifeVelocity;
    }
#endif
vec3 getStartPosition(vec3 startVelocity, float age, vec3 dragData) {
    vec3 startPosition;
    float lasttime = min(startVelocity.x/dragData.x, age);
    startPosition = lasttime*(startVelocity-0.5*dragData*lasttime);
    return startPosition;
}
vec3 computeParticlePosition(in vec3 startVelocity, in vec3 lifeVelocity, in float age, in float normalizedAge, vec3 gravityVelocity, vec4 worldRotation, vec3 dragData) {
    vec3 startPosition = getStartPosition(startVelocity, age, dragData);
    vec3 lifePosition;
    #if defined(VELOCITYOVERLIFETIMECONSTANT) || defined(VELOCITYOVERLIFETIMECURVE) || defined(VELOCITYOVERLIFETIMERANDOMCONSTANT) || defined(VELOCITYOVERLIFETIMERANDOMCURVE)
        vec3 finalPosition;
        if(u_VOLSpaceType == 0) {
            if(u_ScalingMode! = 2)finalPosition = rotationByQuaternions(u_PositionScale*(a_ShapePositionStartLifeTime.xyz+startPosition+lifePosition), worldRotation);
            else finalPosition = rotationByQuaternions(u_PositionScale*a_ShapePositionStartLifeTime.xyz+startPosition+lifePosition, worldRotation);
        }
        else {
            if(u_ScalingMode! = 2)finalPosition = rotationByQuaternions(u_PositionScale*(a_ShapePositionStartLifeTime.xyz+startPosition), worldRotation)+lifePosition;
            else finalPosition = rotationByQuaternions(u_PositionScale*a_ShapePositionStartLifeTime.xyz+startPosition, worldRotation)+lifePosition;
        }
    #else
        vec3 finalPosition;
        if(u_ScalingMode! = 2)finalPosition = rotationByQuaternions(u_PositionScale*(a_ShapePositionStartLifeTime.xyz+startPosition), worldRotation);
        else finalPosition = rotationByQuaternions(u_PositionScale*a_ShapePositionStartLifeTime.xyz+startPosition, worldRotation);
    #endif
    if(u_SimulationSpace == 0)finalPosition = finalPosition+a_SimulationWorldPostion;
    else if(u_SimulationSpace == 1)finalPosition = finalPosition+u_WorldPosition;
    finalPosition += 0.5*gravityVelocity*age;
    return finalPosition;
}
vec4 computeParticleColor(in vec4 color, in float normalizedAge) {
    return color;
}
vec2 computeParticleSizeBillbard(in vec2 size, in float normalizedAge) {
    return size;
}
vec3 computeParticleSizeMesh(in vec3 size, in float normalizedAge) {
    return size;
}
float computeParticleRotationFloat(in float rotation, in float age, in float normalizedAge) {
    return rotation;
}
#if defined(RENDERMODE_MESH) && (defined(ROTATIONOVERLIFETIME) || defined(ROTATIONOVERLIFETIMESEPERATE))
    vec3 computeParticleRotationVec3(in vec3 rotation, in float age, in float normalizedAge) {
        return rotation;
    }
#endif
vec2 computeParticleUV(in vec2 uv, in float normalizedAge) {
    return uv;
}
void main() {
    float age = u_CurrentTime-a_DirectionTime.w;
    float normalizedAge = age/a_ShapePositionStartLifeTime.w;
    vec3 lifeVelocity;
    if(normalizedAge<1.0) {
        vec3 startVelocity = a_DirectionTime.xyz*a_StartSpeed;
        #if defined(VELOCITYOVERLIFETIMECONSTANT) || defined(VELOCITYOVERLIFETIMECURVE) || defined(VELOCITYOVERLIFETIMERANDOMCONSTANT) || defined(VELOCITYOVERLIFETIMERANDOMCURVE)
            lifeVelocity = computeParticleLifeVelocity(normalizedAge);
        #endif
        vec3 gravityVelocity = u_Gravity*age;
        vec4 worldRotation;
        if(u_SimulationSpace == 0)worldRotation = a_SimulationWorldRotation;
        else worldRotation = u_WorldRotation;
        vec3 dragData = a_DirectionTime.xyz*mix(u_DragConstanct.x, u_DragConstanct.y, a_Random0.x);
        vec3 center = computeParticlePosition(startVelocity, lifeVelocity, age, normalizedAge, gravityVelocity, worldRotation, dragData);
        vec3 size = computeParticleSizeMesh(a_StartSize, normalizedAge);
        #if defined(ROTATIONOVERLIFETIME) || defined(ROTATIONOVERLIFETIMESEPERATE)
            if(u_ThreeDStartRotation! = 0) {
                vec3 rotation = vec3(a_StartRotation0.xy, computeParticleRotationFloat(a_StartRotation0.z, age, normalizedAge));
                center += rotationByQuaternions(u_SizeScale*rotationByEuler(a_MeshPosition*size, rotation), worldRotation);
            }
            else {
    
            }
        #else
            if(u_ThreeDStartRotation! = 0) {
                center += rotationByQuaternions(u_SizeScale*rotationByEuler(a_MeshPosition*size, a_StartRotation0), worldRotation);
            }
            else {
                if(u_SimulationSpace == 0)center += u_SizeScale*rotationByAxis(a_MeshPosition*size, vec3(0.0, -1.0, 0.0), a_StartRotation0.x);
                else if(u_SimulationSpace == 1)center += rotationByQuaternions(u_SizeScale*rotationByAxis(a_MeshPosition*size, vec3(0.0, -1.0, 0.0), a_StartRotation0.x), worldRotation);
            }
        #endif
        v_MeshColor = a_MeshColor;
        gl_Position = u_Projection*u_View*vec4(center, 1.0);
        vec4 startcolor = gammaToLinear(a_StartColor);
        v_Color = computeParticleColor(startcolor, normalizedAge);
    }
    else {
        gl_Position = vec4(2.0, 2.0, 2.0, 1.0);
    }
    gl_Position = remapPositionZ(gl_Position);
}
