# Shader Export Improvements Summary

## 修复时间
2026-02-12

## 修复的问题

### 1. **mat2旋转矩阵构造 - GLSL列优先顺序** ✅

**问题描述:**
- 导出的shader使用了错误的mat2构造参数顺序
- Unity HLSL使用行优先矩阵，而GLSL使用列优先矩阵
- 导致Billboard粒子旋转和UV旋转功能错误

**错误代码 (导出的):**
```glsl
mat2 rotation = mat2(c, -s, s, c);
```

**正确代码 (AI辅助的):**
```glsl
// GLSL是列优先，需要调整参数顺序
mat2 rotation = mat2(c, s, -s, c);
```

**修复位置:**
- CustomShaderExporter.cs ~5481-5495行
- 自动检测并修复rotation和rotMat变量的mat2构造

**影响范围:**
- Billboard粒子模式的旋转
- RotateUV函数
- LowCostRotate函数

---

### 2. **矩阵乘法顺序** ✅

**问题描述:**
- 向量与矩阵相乘的顺序错误
- GLSL中应该是 `mat * vec`，不是 `vec * mat`

**错误代码 (导出的):**
```glsl
vec2 rotated = (delta * rotMat);
```

**正确代码 (AI辅助的):**
```glsl
vec2 rotated = rotMat * delta;
```

**修复位置:**
- CustomShaderExporter.cs ~5497-5503行
- 自动修复 `(变量 * 矩阵)` 为 `(矩阵 * 变量)`

**影响范围:**
- 所有使用旋转矩阵的UV计算
- RotateUV和LowCostRotate函数

---

### 3. **颜色乘法顺序** ✅

**问题描述:**
- v_MeshColor和v_Color的应用顺序错误
- 应该先应用mesh顶点颜色，再应用粒子颜色

**错误代码 (导出的):**
```glsl
color *= v_Color;
color.rgb *= v_Color.a;
color.a = clamp(color.a, 0.0, 1.0);
gl_FragColor = color;

#ifdef RENDERMODE_MESH
    // Multiply by mesh vertex color in mesh mode
    gl_FragColor *= v_MeshColor;
#endif
```

**正确代码 (AI辅助的):**
```glsl
#ifdef RENDERMODE_MESH
    // In Mesh mode, use mesh vertex color
    color *= v_MeshColor;
#endif
// Multiply by particle color (for both modes)
color *= v_Color;
color.rgb *= v_Color.a;
color.a = clamp(color.a, 0.0, 1.0);
gl_FragColor = color;
```

**修复位置:**
- CustomShaderExporter.cs ~5506-5533行
- 自动检测并重新排序颜色乘法代码块

**影响范围:**
- Mesh粒子模式的颜色混合
- 最终渲染颜色的正确性

---

### 4. **mat3/transpose/inverse 语法错误** ✅ (增强)

**问题描述:**
- 复杂的mat3嵌套函数调用中出现分号断开语法
- 缺少闭合括号

**错误代码 (导出的):**
```glsl
vec3 normalView = normalize((mat3(transpose); (inverse(u_View)) * normalWorldRim));
```

**正确代码 (AI辅助的):**
```glsl
vec3 normalView = normalize(mat3(transpose(inverse(u_View))) * normalWorldRim);
```

**修复位置:**
- CustomShaderExporter.cs ~5424-5463行
- 增加了6个步骤的渐进式修复
- STEP 1: 直接匹配最复杂的完整错误模式
- STEP 2-6: 逐步修复各种变体

**影响范围:**
- 法线贴图用于边缘光时的法线变换
- 任何使用view矩阵的变换

---

### 5. **mix函数类型精确性** ✅ (已存在)

**问题描述:**
- vec3赋值时mix函数使用float参数而不是vec3参数
- GLSL虽然允许隐式转换但不够精确

**错误代码 (导出的):**
```glsl
vec3 spec = mix(0.0, 1.0, smoothstep(...));
```

**正确代码 (AI辅助的):**
```glsl
vec3 spec = mix(vec3(0.0), vec3(1.0), smoothstep(...));
```

**修复位置:**
- CustomShaderExporter.cs ~5457-5478行
- 已存在的修复，本次无需修改

**影响范围:**
- 光照计算
- 任何vec3类型的mix操作

---

## 测试建议

### 1. 清理缓存
```bash
rmdir /s /q .laya\cache
rmdir /s /q assets\test1
rmdir /s /q assets\test2
del /q template\*.*
```

### 2. 重启Unity Editor
- 确保所有修改被重新编译
- Unity需要重新加载插件DLL

### 3. 重新导出测试场景
- 导出粒子特效场景
- 特别注意使用Mesh渲染模式的粒子
- 检查使用UV旋转的特效

### 4. 在LayaAir IDE中测试
- 检查shader编译是否成功（无错误）
- 检查粒子旋转是否正确
- 检查粒子颜色混合是否正确
- 检查边缘光效果是否正确

---

## 技术细节

### GLSL列优先矩阵说明

GLSL中矩阵是列优先存储的。对于2D旋转矩阵：

**数学定义（行优先思维）:**
```
| cos(θ)  -sin(θ) |
| sin(θ)   cos(θ) |
```

**GLSL构造（列优先）:**
```glsl
mat2(cos(θ), sin(θ), -sin(θ), cos(θ))
//   第1列↑  第1列↑   第2列↑    第2列↑
```

**错误构造（按行写入）:**
```glsl
mat2(cos(θ), -sin(θ), sin(θ), cos(θ))  // ❌ 错误！
```

### 矩阵乘法顺序

GLSL中向量与矩阵相乘：
- 正确: `mat * vec` - 矩阵左乘向量
- 错误: `vec * mat` - 在GLSL中语义不同

### 颜色混合顺序

正确的颜色混合顺序应该是：
1. **先应用mesh顶点颜色** (v_MeshColor) - 这是3D模型自身的颜色信息
2. **再应用粒子颜色** (v_Color) - 这是粒子系统的动态颜色
3. **最后预乘alpha** (color.rgb *= color.a)

这样可以确保颜色叠加的逻辑正确。

---

## 相关文件

### 修改的文件
- `Editor/Export/CustomShaderExporter.cs` - 主要修复文件

### 参考文件
- `template/Artist_Effect_Effect_FullEffect.shader` - AI辅助转换的正确shader
- `template/Artist_Effect_Effect_FullEffect_export.shader` - 导出的shader（修复前）

---

## 修复日志

| 日期 | 问题 | 状态 |
|------|------|------|
| 2026-02-12 | mat2构造顺序错误 | ✅ 已修复 |
| 2026-02-12 | 矩阵乘法顺序错误 | ✅ 已修复 |
| 2026-02-12 | 颜色乘法顺序错误 | ✅ 已修复 |
| 2026-02-12 | transpose/inverse语法错误 | ✅ 增强修复 |
| 2026-02-12 | mix函数类型精确性 | ✅ 已存在 |

---

## 后续建议

1. **回归测试**: 使用多个不同的粒子特效测试，确保所有修复都有效
2. **性能测试**: 确认修复后的shader性能没有下降
3. **文档更新**: 更新用户文档，说明GLSL和HLSL的差异
4. **单元测试**: 考虑为shader转换添加自动化测试
