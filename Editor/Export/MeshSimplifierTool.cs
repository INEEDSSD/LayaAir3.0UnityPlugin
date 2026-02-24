using UnityEngine;
using UnityEditor;
using System.IO;

namespace LayaExport
{
    /// <summary>
    /// Mesh简化编辑器工具
    /// 允许用户在Unity中手动简化mesh并保存
    /// </summary>
    public class MeshSimplifierTool : EditorWindow
    {
        private Mesh sourceMesh;
        private int targetVertexCount = 32;
        private float quality = 0.7f;
        private Mesh previewMesh;
        private string savePath = "Assets/SimplifiedMeshes/";
        private Vector2 scrollPos;

        [MenuItem("LayaAir3D/Mesh简化工具")]
        public static void ShowWindow()
        {
            GetWindow<MeshSimplifierTool>("Mesh简化工具");
        }

        void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            GUILayout.Label("Mesh简化工具", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "用于粒子系统Mesh顶点优化\n" +
                "1. 拖入要简化的Mesh\n" +
                "2. 设置目标顶点数\n" +
                "3. 点击预览查看效果\n" +
                "4. 满意后保存为新Mesh",
                MessageType.Info);

            GUILayout.Space(10);

            // 源Mesh选择
            GUILayout.Label("1. 选择源Mesh", EditorStyles.boldLabel);
            sourceMesh = (Mesh)EditorGUILayout.ObjectField("源Mesh", sourceMesh, typeof(Mesh), false);

            if (sourceMesh != null)
            {
                EditorGUILayout.LabelField("原始顶点数", sourceMesh.vertexCount.ToString());
                EditorGUILayout.LabelField("原始三角形数", (sourceMesh.triangles.Length / 3).ToString());
            }

            GUILayout.Space(10);

            // 简化参数
            GUILayout.Label("2. 简化参数", EditorStyles.boldLabel);
            targetVertexCount = EditorGUILayout.IntSlider("目标顶点数", targetVertexCount, 4, 200);
            quality = EditorGUILayout.Slider("简化质量", quality, 0.3f, 1.0f);

            EditorGUILayout.HelpBox(
                "质量说明：\n" +
                "• 0.3-0.5：激进简化，可能严重变形\n" +
                "• 0.6-0.7：平衡简化（推荐）\n" +
                "• 0.8-1.0：温和简化，保留更多细节",
                MessageType.None);

            GUILayout.Space(10);

            // 预览按钮
            GUILayout.Label("3. 预览简化效果", EditorStyles.boldLabel);
            GUI.enabled = sourceMesh != null;

            if (GUILayout.Button("生成预览", GUILayout.Height(30)))
            {
                GeneratePreview();
            }

            GUI.enabled = true;

            if (previewMesh != null)
            {
                EditorGUILayout.LabelField("预览顶点数", previewMesh.vertexCount.ToString());
                EditorGUILayout.LabelField("预览三角形数", (previewMesh.triangles.Length / 3).ToString());

                float reductionPercent = (1.0f - (float)previewMesh.vertexCount / sourceMesh.vertexCount) * 100;
                EditorGUILayout.LabelField("顶点减少", $"{reductionPercent:F1}%");

                EditorGUILayout.HelpBox(
                    "在Scene视图中查看预览效果\n" +
                    "如果效果不满意，调整参数后重新预览",
                    MessageType.Info);
            }

            GUILayout.Space(10);

            // 保存
            GUILayout.Label("4. 保存简化后的Mesh", EditorStyles.boldLabel);
            savePath = EditorGUILayout.TextField("保存路径", savePath);

            GUI.enabled = previewMesh != null;

            if (GUILayout.Button("保存为Asset", GUILayout.Height(30)))
            {
                SaveSimplifiedMesh();
            }

            GUI.enabled = true;

            GUILayout.Space(10);

            // 使用说明
            if (GUILayout.Button("查看详细说明"))
            {
                ShowHelp();
            }

            EditorGUILayout.EndScrollView();
        }

        void GeneratePreview()
        {
            if (sourceMesh == null)
                return;

            try
            {
                previewMesh = MeshSimplifier.SimplifyMesh(sourceMesh, targetVertexCount, quality);
                EditorUtility.DisplayDialog("预览生成成功",
                    $"简化后顶点数: {previewMesh.vertexCount}\n" +
                    $"原始顶点数: {sourceMesh.vertexCount}\n" +
                    $"减少: {(1.0f - (float)previewMesh.vertexCount / sourceMesh.vertexCount) * 100:F1}%\n\n" +
                    "在Scene视图中查看效果",
                    "确定");
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("简化失败", e.Message, "确定");
            }
        }

        void SaveSimplifiedMesh()
        {
            if (previewMesh == null)
                return;

            // 确保目录存在
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }

            // 生成文件名
            string fileName = $"{sourceMesh.name}_Simplified_{previewMesh.vertexCount}v.asset";
            string fullPath = Path.Combine(savePath, fileName);

            // 保存为Asset
            AssetDatabase.CreateAsset(previewMesh, fullPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("保存成功",
                $"简化后的Mesh已保存到:\n{fullPath}\n\n" +
                "现在可以在粒子系统中使用此Mesh：\n" +
                "1. 选择粒子系统\n" +
                "2. 在Renderer模块中，点击Mesh字段右侧的'+'\n" +
                "3. 添加此简化Mesh作为meshes[1]",
                "确定");

            // 选中保存的Asset
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<Mesh>(fullPath);
            EditorGUIUtility.PingObject(Selection.activeObject);
        }

        void ShowHelp()
        {
            EditorUtility.DisplayDialog("Mesh简化工具使用说明",
                "用途：为粒子系统创建低面数版本的Mesh\n\n" +
                "使用流程：\n" +
                "1. 将高面数Mesh拖入'源Mesh'字段\n" +
                "2. 根据需求设置目标顶点数\n" +
                "   - 粒子Mesh建议：20-50顶点\n" +
                "   - 圆柱体：24-32顶点\n" +
                "   - 球体：32-48顶点\n" +
                "3. 调整质量参数（0.7为推荐值）\n" +
                "4. 点击'生成预览'查看效果\n" +
                "5. 在Scene视图中检查形状\n" +
                "6. 满意后点击'保存为Asset'\n\n" +
                "配置粒子系统：\n" +
                "1. 选择粒子系统GameObject\n" +
                "2. Particle System → Renderer模块\n" +
                "3. Mesh字段：点击右侧'+'\n" +
                "4. meshes[0]: 原始高质量Mesh（编辑器使用）\n" +
                "5. meshes[1]: 简化Mesh（导出LayaAir使用）\n\n" +
                "导出时，插件会自动使用meshes[1]",
                "明白了");
        }

        void OnDestroy()
        {
            if (previewMesh != null)
            {
                DestroyImmediate(previewMesh);
            }
        }
    }
}
