using System.Text;

/// <summary>
/// 粒子Shader完整模板 - 包含80%Unity粒子系统兼容性
/// 基于 Artist_Effect_Effect_FullEffect.shader 修复后的版本
/// </summary>
public static class ParticleShaderTemplate
{
    /// <summary>
    /// 获取完整的粒子顶点着色器函数库代码
    /// </summary>
    public static string GetParticleVertexFunctions()
    {
        StringBuilder sb = new StringBuilder();

        // TransformUV函数
        sb.AppendLine("vec2 TransformUV(vec2 texcoord, vec4 tilingOffset)");
        sb.AppendLine("{");
        sb.AppendLine("    vec2 transTexcoord = vec2(texcoord.x, texcoord.y - 1.0) * tilingOffset.xy + vec2(tilingOffset.z, -tilingOffset.w);");
        sb.AppendLine("    transTexcoord.y += 1.0;");
        sb.AppendLine("    return transTexcoord;");
        sb.AppendLine("}");
        sb.AppendLine();

        // computeParticleLifeVelocity函数
        sb.AppendLine("#if defined(VELOCITYOVERLIFETIMECONSTANT) || defined(VELOCITYOVERLIFETIMECURVE) || defined(VELOCITYOVERLIFETIMERANDOMCONSTANT) || defined(VELOCITYOVERLIFETIMERANDOMCURVE)");
        sb.AppendLine("vec3 computeParticleLifeVelocity(in float normalizedAge)");
        sb.AppendLine("{");
        sb.AppendLine("    vec3 outLifeVelocity;");
        sb.AppendLine("    #ifdef VELOCITYOVERLIFETIMECONSTANT");
        sb.AppendLine("        outLifeVelocity = u_VOLVelocityConst;");
        sb.AppendLine("    #endif");
        sb.AppendLine("    #ifdef VELOCITYOVERLIFETIMECURVE");
        sb.AppendLine("        outLifeVelocity = vec3(getCurValueFromGradientFloat(u_VOLVelocityGradientX, normalizedAge),");
        sb.AppendLine("        getCurValueFromGradientFloat(u_VOLVelocityGradientY, normalizedAge),");
        sb.AppendLine("        getCurValueFromGradientFloat(u_VOLVelocityGradientZ, normalizedAge));");
        sb.AppendLine("    #endif");
        sb.AppendLine("    #ifdef VELOCITYOVERLIFETIMERANDOMCONSTANT");
        sb.AppendLine("        outLifeVelocity = mix(u_VOLVelocityConst, u_VOLVelocityConstMax, vec3(a_Random1.y, a_Random1.z, a_Random1.w));");
        sb.AppendLine("    #endif");
        sb.AppendLine("    #ifdef VELOCITYOVERLIFETIMERANDOMCURVE");
        sb.AppendLine("        outLifeVelocity = vec3(");
        sb.AppendLine("        mix(getCurValueFromGradientFloat(u_VOLVelocityGradientX, normalizedAge), getCurValueFromGradientFloat(u_VOLVelocityGradientMaxX, normalizedAge), a_Random1.y),");
        sb.AppendLine("        mix(getCurValueFromGradientFloat(u_VOLVelocityGradientY, normalizedAge), getCurValueFromGradientFloat(u_VOLVelocityGradientMaxY, normalizedAge), a_Random1.z),");
        sb.AppendLine("        mix(getCurValueFromGradientFloat(u_VOLVelocityGradientZ, normalizedAge), getCurValueFromGradientFloat(u_VOLVelocityGradientMaxZ, normalizedAge), a_Random1.w));");
        sb.AppendLine("    #endif");
        sb.AppendLine("    return outLifeVelocity;");
        sb.AppendLine("}");
        sb.AppendLine("#endif");
        sb.AppendLine();

        // getStartPosition函数
        sb.AppendLine("vec3 getStartPosition(vec3 startVelocity, float age, vec3 dragData)");
        sb.AppendLine("{");
        sb.AppendLine("    vec3 startPosition;");
        sb.AppendLine("    float lasttime = min(startVelocity.x / dragData.x, age);");
        sb.AppendLine("    startPosition = lasttime * (startVelocity - 0.5 * dragData * lasttime);");
        sb.AppendLine("    return startPosition;");
        sb.AppendLine("}");
        sb.AppendLine();

        // computeParticlePosition函数 - 完整版
        sb.AppendLine("vec3 computeParticlePosition(in vec3 startVelocity, in vec3 lifeVelocity, in float age, in float normalizedAge, vec3 gravityVelocity, vec4 worldRotation, vec3 dragData)");
        sb.AppendLine("{");
        sb.AppendLine("    vec3 startPosition = getStartPosition(startVelocity, age, dragData);");
        sb.AppendLine("    vec3 lifePosition;");
        sb.AppendLine("#if defined(VELOCITYOVERLIFETIMECONSTANT) || defined(VELOCITYOVERLIFETIMECURVE) || defined(VELOCITYOVERLIFETIMERANDOMCONSTANT) || defined(VELOCITYOVERLIFETIMERANDOMCURVE)");
        sb.AppendLine("    #ifdef VELOCITYOVERLIFETIMECONSTANT");
        sb.AppendLine("        lifePosition = lifeVelocity * age;");
        sb.AppendLine("    #endif");
        sb.AppendLine("    #ifdef VELOCITYOVERLIFETIMECURVE");
        sb.AppendLine("        lifePosition = vec3(getTotalValueFromGradientFloat(u_VOLVelocityGradientX, normalizedAge),");
        sb.AppendLine("        getTotalValueFromGradientFloat(u_VOLVelocityGradientY, normalizedAge),");
        sb.AppendLine("        getTotalValueFromGradientFloat(u_VOLVelocityGradientZ, normalizedAge));");
        sb.AppendLine("    #endif");
        sb.AppendLine("    #ifdef VELOCITYOVERLIFETIMERANDOMCONSTANT");
        sb.AppendLine("        lifePosition = lifeVelocity * age;");
        sb.AppendLine("    #endif");
        sb.AppendLine("    #ifdef VELOCITYOVERLIFETIMERANDOMCURVE");
        sb.AppendLine("        lifePosition = vec3(");
        sb.AppendLine("        mix(getTotalValueFromGradientFloat(u_VOLVelocityGradientX, normalizedAge), getTotalValueFromGradientFloat(u_VOLVelocityGradientMaxX, normalizedAge), a_Random1.y),");
        sb.AppendLine("        mix(getTotalValueFromGradientFloat(u_VOLVelocityGradientY, normalizedAge), getTotalValueFromGradientFloat(u_VOLVelocityGradientMaxY, normalizedAge), a_Random1.z),");
        sb.AppendLine("        mix(getTotalValueFromGradientFloat(u_VOLVelocityGradientZ, normalizedAge), getTotalValueFromGradientFloat(u_VOLVelocityGradientMaxZ, normalizedAge), a_Random1.w));");
        sb.AppendLine("    #endif");
        sb.AppendLine();
        sb.AppendLine("    vec3 finalPosition;");
        sb.AppendLine("    if (u_VOLSpaceType == 0) {");
        sb.AppendLine("        if (u_ScalingMode != 2)");
        sb.AppendLine("            finalPosition = rotationByQuaternions(u_PositionScale * (a_ShapePositionStartLifeTime.xyz + startPosition + lifePosition), worldRotation);");
        sb.AppendLine("        else");
        sb.AppendLine("            finalPosition = rotationByQuaternions(u_PositionScale * a_ShapePositionStartLifeTime.xyz + startPosition + lifePosition, worldRotation);");
        sb.AppendLine("    } else {");
        sb.AppendLine("        if (u_ScalingMode != 2)");
        sb.AppendLine("            finalPosition = rotationByQuaternions(u_PositionScale * (a_ShapePositionStartLifeTime.xyz + startPosition), worldRotation) + lifePosition;");
        sb.AppendLine("        else");
        sb.AppendLine("            finalPosition = rotationByQuaternions(u_PositionScale * a_ShapePositionStartLifeTime.xyz + startPosition, worldRotation) + lifePosition;");
        sb.AppendLine("    }");
        sb.AppendLine("#else");
        sb.AppendLine("    vec3 finalPosition;");
        sb.AppendLine("    if (u_ScalingMode != 2)");
        sb.AppendLine("        finalPosition = rotationByQuaternions(u_PositionScale * (a_ShapePositionStartLifeTime.xyz + startPosition), worldRotation);");
        sb.AppendLine("    else");
        sb.AppendLine("        finalPosition = rotationByQuaternions(u_PositionScale * a_ShapePositionStartLifeTime.xyz + startPosition, worldRotation);");
        sb.AppendLine("#endif");
        sb.AppendLine();
        sb.AppendLine("    if (u_SimulationSpace == 0)");
        sb.AppendLine("        finalPosition = finalPosition + a_SimulationWorldPostion;");
        sb.AppendLine("    else if (u_SimulationSpace == 1)");
        sb.AppendLine("        finalPosition = finalPosition + u_WorldPosition;");
        sb.AppendLine();
        sb.AppendLine("    finalPosition += 0.5 * gravityVelocity * age;");
        sb.AppendLine("    return finalPosition;");
        sb.AppendLine("}");
        sb.AppendLine();

        // computeParticleColor函数
        sb.AppendLine("vec4 computeParticleColor(in vec4 color, in float normalizedAge)");
        sb.AppendLine("{");
        sb.AppendLine("#ifdef COLOROVERLIFETIME");
        sb.AppendLine("    color *= getColorFromGradient(u_ColorOverLifeGradientAlphas, u_ColorOverLifeGradientColors, normalizedAge, u_ColorOverLifeGradientRanges);");
        sb.AppendLine("#endif");
        sb.AppendLine("#ifdef RANDOMCOLOROVERLIFETIME");
        sb.AppendLine("    color *= mix(getColorFromGradient(u_ColorOverLifeGradientAlphas, u_ColorOverLifeGradientColors, normalizedAge, u_ColorOverLifeGradientRanges),");
        sb.AppendLine("        getColorFromGradient(u_MaxColorOverLifeGradientAlphas, u_MaxColorOverLifeGradientColors, normalizedAge, u_MaxColorOverLifeGradientRanges), a_Random0.y);");
        sb.AppendLine("#endif");
        sb.AppendLine("    return color;");
        sb.AppendLine("}");
        sb.AppendLine();

        // computeParticleSizeBillbard函数
        sb.AppendLine("vec2 computeParticleSizeBillbard(in vec2 size, in float normalizedAge)");
        sb.AppendLine("{");
        sb.AppendLine("#ifdef SIZEOVERLIFETIMECURVE");
        sb.AppendLine("    size *= getCurValueFromGradientFloat(u_SOLSizeGradient, normalizedAge);");
        sb.AppendLine("#endif");
        sb.AppendLine("#ifdef SIZEOVERLIFETIMERANDOMCURVES");
        sb.AppendLine("    size *= mix(getCurValueFromGradientFloat(u_SOLSizeGradient, normalizedAge), getCurValueFromGradientFloat(u_SOLSizeGradientMax, normalizedAge), a_Random0.z);");
        sb.AppendLine("#endif");
        sb.AppendLine("#ifdef SIZEOVERLIFETIMECURVESEPERATE");
        sb.AppendLine("    size *= vec2(getCurValueFromGradientFloat(u_SOLSizeGradientX, normalizedAge), getCurValueFromGradientFloat(u_SOLSizeGradientY, normalizedAge));");
        sb.AppendLine("#endif");
        sb.AppendLine("#ifdef SIZEOVERLIFETIMERANDOMCURVESSEPERATE");
        sb.AppendLine("    size *= vec2(mix(getCurValueFromGradientFloat(u_SOLSizeGradientX, normalizedAge), getCurValueFromGradientFloat(u_SOLSizeGradientMaxX, normalizedAge), a_Random0.z),");
        sb.AppendLine("        mix(getCurValueFromGradientFloat(u_SOLSizeGradientY, normalizedAge), getCurValueFromGradientFloat(u_SOLSizeGradientMaxY, normalizedAge), a_Random0.z));");
        sb.AppendLine("#endif");
        sb.AppendLine("    return size;");
        sb.AppendLine("}");
        sb.AppendLine();

        // computeParticleSizeMesh函数
        sb.AppendLine("#ifdef RENDERMODE_MESH");
        sb.AppendLine("vec3 computeParticleSizeMesh(in vec3 size, in float normalizedAge)");
        sb.AppendLine("{");
        sb.AppendLine("    #ifdef SIZEOVERLIFETIMECURVE");
        sb.AppendLine("        size *= getCurValueFromGradientFloat(u_SOLSizeGradient, normalizedAge);");
        sb.AppendLine("    #endif");
        sb.AppendLine("    #ifdef SIZEOVERLIFETIMERANDOMCURVES");
        sb.AppendLine("        size *= mix(getCurValueFromGradientFloat(u_SOLSizeGradient, normalizedAge), getCurValueFromGradientFloat(u_SOLSizeGradientMax, normalizedAge), a_Random0.z);");
        sb.AppendLine("    #endif");
        sb.AppendLine("    #ifdef SIZEOVERLIFETIMECURVESEPERATE");
        sb.AppendLine("        size *= vec3(getCurValueFromGradientFloat(u_SOLSizeGradientX, normalizedAge), getCurValueFromGradientFloat(u_SOLSizeGradientY, normalizedAge), getCurValueFromGradientFloat(u_SOLSizeGradientZ, normalizedAge));");
        sb.AppendLine("    #endif");
        sb.AppendLine("    #ifdef SIZEOVERLIFETIMERANDOMCURVESSEPERATE");
        sb.AppendLine("        size *= vec3(mix(getCurValueFromGradientFloat(u_SOLSizeGradientX, normalizedAge), getCurValueFromGradientFloat(u_SOLSizeGradientMaxX, normalizedAge), a_Random0.z),");
        sb.AppendLine("            mix(getCurValueFromGradientFloat(u_SOLSizeGradientY, normalizedAge), getCurValueFromGradientFloat(u_SOLSizeGradientMaxY, normalizedAge), a_Random0.z),");
        sb.AppendLine("            mix(getCurValueFromGradientFloat(u_SOLSizeGradientZ, normalizedAge), getCurValueFromGradientFloat(u_SOLSizeGradientMaxZ, normalizedAge), a_Random0.z));");
        sb.AppendLine("    #endif");
        sb.AppendLine("    return size;");
        sb.AppendLine("}");
        sb.AppendLine("#endif");
        sb.AppendLine();

        // computeParticleRotationFloat函数
        sb.AppendLine("float computeParticleRotationFloat(in float rotation, in float age, in float normalizedAge)");
        sb.AppendLine("{");
        sb.AppendLine("#ifdef ROTATIONOVERLIFETIME");
        sb.AppendLine("    #ifdef ROTATIONOVERLIFETIMECONSTANT");
        sb.AppendLine("        rotation += u_ROLAngularVelocityConst * age;");
        sb.AppendLine("    #endif");
        sb.AppendLine("    #ifdef ROTATIONOVERLIFETIMECURVE");
        sb.AppendLine("        rotation += getTotalValueFromGradientFloat(u_ROLAngularVelocityGradient, normalizedAge);");
        sb.AppendLine("    #endif");
        sb.AppendLine("    #ifdef ROTATIONOVERLIFETIMERANDOMCONSTANTS");
        sb.AppendLine("        rotation += mix(u_ROLAngularVelocityConst, u_ROLAngularVelocityConstMax, a_Random0.w) * age;");
        sb.AppendLine("    #endif");
        sb.AppendLine("    #ifdef ROTATIONOVERLIFETIMERANDOMCURVES");
        sb.AppendLine("        rotation += mix(getTotalValueFromGradientFloat(u_ROLAngularVelocityGradient, normalizedAge), getTotalValueFromGradientFloat(u_ROLAngularVelocityGradientMax, normalizedAge), a_Random0.w);");
        sb.AppendLine("    #endif");
        sb.AppendLine("#endif");
        sb.AppendLine("#ifdef ROTATIONOVERLIFETIMESEPERATE");
        sb.AppendLine("    #ifdef ROTATIONOVERLIFETIMECONSTANT");
        sb.AppendLine("        rotation += u_ROLAngularVelocityConstSeprarate.z * age;");
        sb.AppendLine("    #endif");
        sb.AppendLine("    #ifdef ROTATIONOVERLIFETIMECURVE");
        sb.AppendLine("        rotation += getTotalValueFromGradientFloat(u_ROLAngularVelocityGradientZ, normalizedAge);");
        sb.AppendLine("    #endif");
        sb.AppendLine("    #ifdef ROTATIONOVERLIFETIMERANDOMCONSTANTS");
        sb.AppendLine("        rotation += mix(u_ROLAngularVelocityConstSeprarate.z, u_ROLAngularVelocityConstMaxSeprarate.z, a_Random0.w) * age;");
        sb.AppendLine("    #endif");
        sb.AppendLine("    #ifdef ROTATIONOVERLIFETIMERANDOMCURVES");
        sb.AppendLine("        rotation += mix(getTotalValueFromGradientFloat(u_ROLAngularVelocityGradientZ, normalizedAge), getTotalValueFromGradientFloat(u_ROLAngularVelocityGradientMaxZ, normalizedAge), a_Random0.w);");
        sb.AppendLine("    #endif");
        sb.AppendLine("#endif");
        sb.AppendLine("    return rotation;");
        sb.AppendLine("}");
        sb.AppendLine();

        // computeParticleUV函数
        sb.AppendLine("vec2 computeParticleUV(in vec2 uv, in float normalizedAge)");
        sb.AppendLine("{");
        sb.AppendLine("#ifdef TEXTURESHEETANIMATIONCURVE");
        sb.AppendLine("    float cycleNormalizedAge = normalizedAge * u_TSACycles;");
        sb.AppendLine("    float frame = getFrameFromGradient(u_TSAGradientUVs, cycleNormalizedAge - floor(cycleNormalizedAge));");
        sb.AppendLine("    float totalULength = frame * u_TSASubUVLength.x;");
        sb.AppendLine("    float floorTotalULength = floor(totalULength);");
        sb.AppendLine("    uv.x += totalULength - floorTotalULength;");
        sb.AppendLine("    uv.y += floorTotalULength * u_TSASubUVLength.y;");
        sb.AppendLine("#endif");
        sb.AppendLine("#ifdef TEXTURESHEETANIMATIONRANDOMCURVE");
        sb.AppendLine("    float cycleNormalizedAge = normalizedAge * u_TSACycles;");
        sb.AppendLine("    float uvNormalizedAge = cycleNormalizedAge - floor(cycleNormalizedAge);");
        sb.AppendLine("    float frame = floor(mix(getFrameFromGradient(u_TSAGradientUVs, uvNormalizedAge), getFrameFromGradient(u_TSAMaxGradientUVs, uvNormalizedAge), a_Random1.x));");
        sb.AppendLine("    float totalULength = frame * u_TSASubUVLength.x;");
        sb.AppendLine("    float floorTotalULength = floor(totalULength);");
        sb.AppendLine("    uv.x += totalULength - floorTotalULength;");
        sb.AppendLine("    uv.y += floorTotalULength * u_TSASubUVLength.y;");
        sb.AppendLine("#endif");
        sb.AppendLine("    return uv;");
        sb.AppendLine("}");
        sb.AppendLine();

        return sb.ToString();
    }

    /// <summary>
    /// 获取完整的粒子顶点着色器main函数代码（支持Billboard和Mesh模式）
    /// </summary>
    public static string GetParticleVertexMainFunction()
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("void main()");
        sb.AppendLine("{");
        sb.AppendLine("    // Particle system logic - calculate particle center position");
        sb.AppendLine("    float age = u_CurrentTime - a_DirectionTime.w;");
        sb.AppendLine("    float normalizedAge = age / a_ShapePositionStartLifeTime.w;");
        sb.AppendLine("    vec3 lifeVelocity;");
        sb.AppendLine();
        sb.AppendLine("    if (normalizedAge < 1.0)");
        sb.AppendLine("    {");

        // ===== Mesh Rendering Mode =====
        sb.AppendLine("#ifdef RENDERMODE_MESH");
        sb.AppendLine("        // ===== Mesh Rendering Mode =====");
        sb.AppendLine("        // Calculate particle center position first");
        sb.AppendLine("        vec3 startVelocity = a_DirectionTime.xyz * a_StartSpeed;");
        sb.AppendLine();
        sb.AppendLine("        #if defined(VELOCITYOVERLIFETIMECONSTANT) || defined(VELOCITYOVERLIFETIMECURVE) || defined(VELOCITYOVERLIFETIMERANDOMCONSTANT) || defined(VELOCITYOVERLIFETIMERANDOMCURVE)");
        sb.AppendLine("            lifeVelocity = computeParticleLifeVelocity(normalizedAge);");
        sb.AppendLine("        #endif");
        sb.AppendLine();
        sb.AppendLine("        vec3 gravityVelocity = u_Gravity * age;");
        sb.AppendLine("        vec4 worldRotation;");
        sb.AppendLine();
        sb.AppendLine("        if (u_SimulationSpace == 0)");
        sb.AppendLine("            worldRotation = a_SimulationWorldRotation;");
        sb.AppendLine("        else");
        sb.AppendLine("            worldRotation = u_WorldRotation;");
        sb.AppendLine();
        sb.AppendLine("        vec3 dragData = a_DirectionTime.xyz * mix(u_DragConstanct.x, u_DragConstanct.y, a_Random0.x);");
        sb.AppendLine("        vec3 center = computeParticlePosition(startVelocity, lifeVelocity, age, normalizedAge, gravityVelocity, worldRotation, dragData);");
        sb.AppendLine();
        sb.AppendLine("        // Apply mesh vertex with size scaling and rotation");
        sb.AppendLine("        vec3 size = computeParticleSizeMesh(a_StartSize, normalizedAge);");
        sb.AppendLine();
        sb.AppendLine("        // Apply rotation based on particle system settings");
        sb.AppendLine("        // Use rotationByAxis for 2D rotation (simpler, compatible with most use cases)");
        sb.AppendLine("        if (u_SimulationSpace == 0)");
        sb.AppendLine("            center += u_SizeScale * rotationByAxis(a_MeshPosition * size, vec3(0.0, -1.0, 0.0), a_StartRotation0.x);");
        sb.AppendLine("        else if (u_SimulationSpace == 1)");
        sb.AppendLine("            center += rotationByQuaternions(u_SizeScale * rotationByAxis(a_MeshPosition * size, vec3(0.0, -1.0, 0.0), a_StartRotation0.x), worldRotation);");
        sb.AppendLine();
        sb.AppendLine("        // Transform to clip space");
        sb.AppendLine("        gl_Position = u_Projection * u_View * vec4(center, 1.0);");
        sb.AppendLine();
        sb.AppendLine("        // Pass mesh color through separate varying");
        sb.AppendLine("        v_MeshColor = a_MeshColor;");
        sb.AppendLine();
        sb.AppendLine("        // Particle color for additional effects");
        sb.AppendLine("        vec4 startcolor = gammaToLinear(a_StartColor);");
        sb.AppendLine("        v_Color = computeParticleColor(startcolor, normalizedAge);");
        sb.AppendLine();
        sb.AppendLine("        // UV calculation for mesh mode");
        sb.AppendLine("        vec2 simulateUV = a_SimulationUV.xy + a_MeshTextureCoordinate * a_SimulationUV.zw;");
        sb.AppendLine("        v_TextureCoordinate = computeParticleUV(simulateUV, normalizedAge);");
        sb.AppendLine();
        sb.AppendLine("        // Screen position");
        sb.AppendLine("        v_ScreenPos.xy = (gl_Position.xy + gl_Position.w) * 0.5;");
        sb.AppendLine("        v_ScreenPos.zw = gl_Position.zw;");
        sb.AppendLine();

        // ===== Billboard Particle Mode =====
        sb.AppendLine("#else");
        sb.AppendLine("        // ===== Billboard Particle Mode =====");
        sb.AppendLine("        vec3 startVelocity = a_DirectionTime.xyz * a_StartSpeed;");
        sb.AppendLine();
        sb.AppendLine("        #if defined(VELOCITYOVERLIFETIMECONSTANT) || defined(VELOCITYOVERLIFETIMECURVE) || defined(VELOCITYOVERLIFETIMERANDOMCONSTANT) || defined(VELOCITYOVERLIFETIMERANDOMCURVE)");
        sb.AppendLine("            lifeVelocity = computeParticleLifeVelocity(normalizedAge);");
        sb.AppendLine("        #endif");
        sb.AppendLine();
        sb.AppendLine("        vec3 gravityVelocity = u_Gravity * age;");
        sb.AppendLine("        vec4 worldRotation;");
        sb.AppendLine();
        sb.AppendLine("        if (u_SimulationSpace == 0)");
        sb.AppendLine("            worldRotation = a_SimulationWorldRotation;");
        sb.AppendLine("        else");
        sb.AppendLine("            worldRotation = u_WorldRotation;");
        sb.AppendLine();
        sb.AppendLine("        vec3 dragData = a_DirectionTime.xyz * mix(u_DragConstanct.x, u_DragConstanct.y, a_Random0.x);");
        sb.AppendLine("        vec3 center = computeParticlePosition(startVelocity, lifeVelocity, age, normalizedAge, gravityVelocity, worldRotation, dragData);");
        sb.AppendLine();
        sb.AppendLine("        // Default Billboard mode");
        sb.AppendLine("        vec2 corner = a_CornerTextureCoordinate.xy;");
        sb.AppendLine("        vec3 cameraUpVector = normalize(u_CameraUp);");
        sb.AppendLine("        vec3 sideVector = normalize(cross(u_CameraDirection, cameraUpVector));");
        sb.AppendLine("        vec3 upVector = normalize(cross(sideVector, u_CameraDirection));");
        sb.AppendLine("        corner *= computeParticleSizeBillbard(a_StartSize.xy, normalizedAge);");
        sb.AppendLine("        float c = cos(a_StartRotation0.x);");
        sb.AppendLine("        float s = sin(a_StartRotation0.x);");
        sb.AppendLine("        mat2 rotation = mat2(c, -s, s, c);");
        sb.AppendLine("        corner = rotation * corner;");
        sb.AppendLine("        center += u_SizeScale.xzy * (corner.x * sideVector + corner.y * upVector);");
        sb.AppendLine();
        sb.AppendLine("        // Final position");
        sb.AppendLine("        gl_Position = u_Projection * u_View * vec4(center, 1.0);");
        sb.AppendLine();
        sb.AppendLine("        // Screen position");
        sb.AppendLine("        v_ScreenPos.xy = (gl_Position.xy + gl_Position.w) * 0.5;");
        sb.AppendLine("        v_ScreenPos.zw = gl_Position.zw;");
        sb.AppendLine();
        sb.AppendLine("        // Particle color");
        sb.AppendLine("        vec4 startcolor = gammaToLinear(a_StartColor);");
        sb.AppendLine("        v_Color = computeParticleColor(startcolor, normalizedAge);");
        sb.AppendLine();
        sb.AppendLine("        // UV calculation");
        sb.AppendLine("        vec2 simulateUV = a_SimulationUV.xy + a_CornerTextureCoordinate.zw * a_SimulationUV.zw;");
        sb.AppendLine("        v_TextureCoordinate = computeParticleUV(simulateUV, normalizedAge);");
        sb.AppendLine("#endif");
        sb.AppendLine("    }");
        sb.AppendLine("    else");
        sb.AppendLine("    {");
        sb.AppendLine("        // Particle is dead, move it out of view");
        sb.AppendLine("        gl_Position = vec4(2.0, 2.0, 2.0, 1.0);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    gl_Position = remapPositionZ(gl_Position);");
        sb.AppendLine();
        sb.AppendLine("#ifdef FOG");
        sb.AppendLine("    FogHandle(gl_Position.z);");
        sb.AppendLine("#endif");
        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>
    /// 获取粒子片段着色器常量定义
    /// </summary>
    public static string GetParticleFragmentConstants()
    {
        return "const vec4 c_ColorSpace = vec4(4.59479380, 4.59479380, 4.59479380, 2.0);";
    }
}
