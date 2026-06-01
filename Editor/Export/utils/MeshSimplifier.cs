using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace LayaExport
{
    /// <summary>
    /// 简单的Mesh简化工具
    /// 用于粒子系统Mesh顶点数优化
    /// </summary>
    public static class MeshSimplifier
    {
        /// <summary>
        /// 简化mesh到目标顶点数
        /// </summary>
        /// <param name="originalMesh">原始mesh</param>
        /// <param name="targetVertexCount">目标顶点数（理论最小值）</param>
        /// <param name="quality">简化质量 (0.1-1.0)
        ///   - 0.5-0.6: 激进简化，可能影响视觉效果较大
        ///   - 0.7-0.8: 平衡简化（推荐）
        ///   - 0.9-1.0: 温和简化，保留更多细节，但可能仍超限
        /// quality参数允许简化后的顶点数适度超过理论目标，以保证质量</param>
        /// <returns>简化后的mesh</returns>
        public static Mesh SimplifyMesh(Mesh originalMesh, int targetVertexCount, float quality = 0.7f)
        {
            if (originalMesh == null)
                return null;

            int originalVertexCount = originalMesh.vertexCount;

            // 如果已经符合要求，直接返回副本
            if (originalVertexCount <= targetVertexCount)
            {
                return Object.Instantiate(originalMesh);
            }

            // ⭐ 关键改进：quality参数控制实际目标顶点数
            // quality越高，允许保留的顶点越多（简化越温和）
            // 例如：targetVertexCount=28, quality=0.7 → actualTarget=28/0.7=40
            //      targetVertexCount=28, quality=0.5 → actualTarget=28/0.5=56
            quality = Mathf.Clamp(quality, 0.3f, 1.0f);
            int actualTargetCount = Mathf.CeilToInt(targetVertexCount / quality);

            // 确保不超过原始顶点数的80%（避免简化太少）
            int maxAllowedCount = Mathf.CeilToInt(originalVertexCount * 0.8f);
            actualTargetCount = Mathf.Min(actualTargetCount, maxAllowedCount);

            ExportLogger.Log($"MeshSimplifier: 开始简化mesh '{originalMesh.name}'");
            ExportLogger.Log($"  原始顶点数: {originalVertexCount}");
            ExportLogger.Log($"  理论最小目标: {targetVertexCount}");
            ExportLogger.Log($"  简化质量: {quality:F2}");
            ExportLogger.Log($"  实际目标顶点数: {actualTargetCount} (允许超出理论值以保证质量)");

            #if UNITY_MESH_SIMPLIFIER
            // ⭐ 优先使用UnityMeshSimplifier（如果已安装）
            try
            {
                ExportLogger.Log($"  使用UnityMeshSimplifier库（高质量QEM算法）");
                Mesh advancedMesh = UnityMeshSimplifierIntegration.SimplifyMeshAdvanced(
                    originalMesh, actualTargetCount, quality);
                ExportLogger.Log($"  简化后顶点数: {advancedMesh.vertexCount}");
                return advancedMesh;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"  UnityMeshSimplifier失败: {e.Message}，降级到内置算法");
            }
            #endif

            // 使用内置三角形抽取简化方法（备用）
            ExportLogger.Log($"  使用内置三角形抽取算法");
            Mesh sampledMesh = SimplifyByVertexSampling(originalMesh, actualTargetCount);
            ExportLogger.Log($"  简化后顶点数: {sampledMesh.vertexCount}");

            return sampledMesh;
        }


        /// <summary>
        /// 通过三角形抽取简化mesh
        /// 保持完整的三角形，避免破坏拓扑结构
        /// </summary>
        private static Mesh SimplifyByVertexSampling(Mesh originalMesh, int targetVertexCount)
        {
            // ⭐ 新策略：不是采样顶点，而是抽取三角形
            // 这样可以保持mesh的拓扑完整性

            Vector3[] vertices = originalMesh.vertices;
            Vector3[] normals = originalMesh.normals;
            Vector4[] tangents = originalMesh.tangents;
            Vector2[] uv = originalMesh.uv;
            Vector2[] uv2 = originalMesh.uv2;
            Color[] colors = originalMesh.colors;
            int[] triangles = originalMesh.triangles;

            int originalVertexCount = vertices.Length;
            int triangleCount = triangles.Length / 3;

            // 计算需要保留的三角形比例
            float vertexReductionRatio = (float)targetVertexCount / originalVertexCount;
            // 假设顶点共享，三角形比例大约是顶点比例
            int targetTriangleCount = Mathf.Max(1, Mathf.CeilToInt(triangleCount * vertexReductionRatio));

            ExportLogger.Log($"  简化策略: 抽取三角形");
            ExportLogger.Log($"  原始三角形数: {triangleCount}, 目标三角形数: {targetTriangleCount}");

            // 均匀抽取三角形
            float step = (float)triangleCount / targetTriangleCount;
            HashSet<int> selectedTriangles = new HashSet<int>();

            for (int i = 0; i < targetTriangleCount; i++)
            {
                int triangleIndex = Mathf.Min((int)(i * step), triangleCount - 1);
                selectedTriangles.Add(triangleIndex);
            }

            // 收集被选中的三角形使用的所有顶点
            HashSet<int> usedVertices = new HashSet<int>();
            foreach (int triIdx in selectedTriangles)
            {
                int idx = triIdx * 3;
                usedVertices.Add(triangles[idx]);
                usedVertices.Add(triangles[idx + 1]);
                usedVertices.Add(triangles[idx + 2]);
            }

            // 如果顶点数还是太多，尝试进一步减少三角形
            if (usedVertices.Count > targetVertexCount * 1.5f)
            {
                Debug.LogWarning($"  第一次抽取顶点数({usedVertices.Count})过多，进行二次简化");
                targetTriangleCount = Mathf.CeilToInt(targetTriangleCount * 0.7f);
                selectedTriangles.Clear();
                usedVertices.Clear();

                step = (float)triangleCount / targetTriangleCount;
                for (int i = 0; i < targetTriangleCount; i++)
                {
                    int triangleIndex = Mathf.Min((int)(i * step), triangleCount - 1);
                    selectedTriangles.Add(triangleIndex);
                }

                foreach (int triIdx in selectedTriangles)
                {
                    int idx = triIdx * 3;
                    usedVertices.Add(triangles[idx]);
                    usedVertices.Add(triangles[idx + 1]);
                    usedVertices.Add(triangles[idx + 2]);
                }
            }

            // 创建顶点映射
            Dictionary<int, int> vertexMap = new Dictionary<int, int>();
            List<Vector3> newVertices = new List<Vector3>();
            List<Vector3> newNormals = new List<Vector3>();
            List<Vector4> newTangents = new List<Vector4>();
            List<Vector2> newUV = new List<Vector2>();
            List<Vector2> newUV2 = new List<Vector2>();
            List<Color> newColors = new List<Color>();

            foreach (int oldIdx in usedVertices.OrderBy(x => x))
            {
                int newIdx = newVertices.Count;
                vertexMap[oldIdx] = newIdx;

                newVertices.Add(vertices[oldIdx]);
                if (normals.Length > oldIdx) newNormals.Add(normals[oldIdx]);
                if (tangents.Length > oldIdx) newTangents.Add(tangents[oldIdx]);
                if (uv.Length > oldIdx) newUV.Add(uv[oldIdx]);
                if (uv2.Length > oldIdx) newUV2.Add(uv2[oldIdx]);
                if (colors.Length > oldIdx) newColors.Add(colors[oldIdx]);
            }

            // 重建三角形索引
            List<int> newTriangles = new List<int>();
            foreach (int triIdx in selectedTriangles.OrderBy(x => x))
            {
                int idx = triIdx * 3;
                newTriangles.Add(vertexMap[triangles[idx]]);
                newTriangles.Add(vertexMap[triangles[idx + 1]]);
                newTriangles.Add(vertexMap[triangles[idx + 2]]);
            }

            // 构建新mesh
            Mesh newMesh = new Mesh();
            newMesh.name = originalMesh.name + "_Simplified";
            newMesh.vertices = newVertices.ToArray();

            if (newNormals.Count > 0) newMesh.normals = newNormals.ToArray();
            if (newTangents.Count > 0) newMesh.tangents = newTangents.ToArray();
            if (newUV.Count > 0) newMesh.uv = newUV.ToArray();
            if (newUV2.Count > 0) newMesh.uv2 = newUV2.ToArray();
            if (newColors.Count > 0) newMesh.colors = newColors.ToArray();

            newMesh.triangles = newTriangles.ToArray();

            if (newNormals.Count == 0)
                newMesh.RecalculateNormals();

            newMesh.RecalculateBounds();

            ExportLogger.Log($"  最终: 顶点数={newMesh.vertexCount}, 三角形数={newMesh.triangles.Length / 3}");

            return newMesh;
        }

        /// <summary>
        /// 找到距离目标索引最近的已映射顶点
        /// </summary>
        private static int FindNearestMappedVertex(int targetIndex, int maxIndex, Dictionary<int, int> vertexMap)
        {
            // 先检查目标索引本身
            if (vertexMap.ContainsKey(targetIndex))
                return targetIndex;

            // 向前后搜索最近的映射顶点
            int searchRadius = Mathf.Max(maxIndex / 10, 5); // 动态搜索半径
            for (int offset = 1; offset <= searchRadius; offset++)
            {
                int prevIndex = targetIndex - offset;
                if (prevIndex >= 0 && vertexMap.ContainsKey(prevIndex))
                    return prevIndex;

                int nextIndex = targetIndex + offset;
                if (nextIndex < maxIndex && vertexMap.ContainsKey(nextIndex))
                    return nextIndex;
            }

            // 如果找不到，返回映射表中最接近的顶点
            int nearestKey = vertexMap.Keys.OrderBy(k => Mathf.Abs(k - targetIndex)).FirstOrDefault();
            return nearestKey;
        }

        /// <summary>
        /// 计算建议的最大粒子数
        /// </summary>
        public static int CalculateSuggestedMaxParticles(int meshVertexCount, int maxTotalVertices = 65535)
        {
            if (meshVertexCount <= 0)
                return 0;

            return maxTotalVertices / meshVertexCount;
        }

        /// <summary>
        /// 计算简化后mesh需要的目标顶点数
        /// </summary>
        public static int CalculateTargetVertexCount(int maxParticles, int maxTotalVertices = 65535)
        {
            if (maxParticles <= 0)
                return maxTotalVertices;

            return maxTotalVertices / maxParticles;
        }
    }
}
