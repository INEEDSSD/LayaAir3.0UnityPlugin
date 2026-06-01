#if UNITY_MESH_SIMPLIFIER
using UnityEngine;
using UnityMeshSimplifier;

namespace LayaExport
{
    /// <summary>
    /// UnityMeshSimplifier库集成（可选）
    /// GitHub: https://github.com/Whinarn/UnityMeshSimplifier
    /// 安装方法：
    /// 1. Unity Package Manager → Add package from git URL
    /// 2. 输入：https://github.com/Whinarn/UnityMeshSimplifier.git
    /// 3. 或下载Release版本放入Assets/Plugins文件夹
    /// </summary>
    public static class UnityMeshSimplifierIntegration
    {
        /// <summary>
        /// 使用UnityMeshSimplifier简化mesh
        /// 基于二次误差度量（QEM）算法，质量远超简单采样
        /// </summary>
        public static Mesh SimplifyMeshAdvanced(Mesh originalMesh, int targetVertexCount, float quality = 0.7f)
        {
            var meshSimplifier = new UnityMeshSimplifier.MeshSimplifier();
            meshSimplifier.Initialize(originalMesh);

            // quality参数影响简化质量
            // 0.5 = 激进简化
            // 0.7 = 平衡（推荐）
            // 0.9 = 保守简化
            meshSimplifier.SimplificationOptions = new SimplificationOptions
            {
                PreserveBorderEdges = true,      // 保留边缘
                PreserveUVSeamEdges = true,      // 保留UV接缝
                PreserveUVFoldoverEdges = true,  // 保留UV折叠边
                EnableSmartLink = true,          // 智能链接
                VertexLinkDistance = 0.0001f,    // 顶点链接距离
                MaxIterationCount = 100,         // 最大迭代次数
                Agressiveness = 7.0              // 激进度（7为推荐值）
            };

            // 计算目标质量（保留比例）
            float targetQuality = (float)targetVertexCount / originalMesh.vertexCount;
            targetQuality = Mathf.Clamp(targetQuality * quality, 0.1f, 1.0f);

            meshSimplifier.SimplifyMesh(targetQuality);

            Mesh simplifiedMesh = meshSimplifier.ToMesh();
            simplifiedMesh.name = originalMesh.name + "_Simplified";

            return simplifiedMesh;
        }
    }
}
#endif
