using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LayaAir3Export
{
   
    public static void ExportScene()
    {
        try
        {
            // 显示初始化进度
            EditorUtility.DisplayProgressBar(LanguageConfig.str_LayaAirExport, LanguageConfig.str_ExportInit, 0f);
            
            GameObjectUitls.init();
            MetarialUitls.init();
            AnimationCurveGroup.init();
            UnsupportedFeatureCollector.Init();

            // 清除自定义Shader导出缓存
            CustomShaderExporter.ClearCache();

            var active = EditorSceneManager.GetActiveScene();
            var sceneCount = EditorSceneManager.sceneCount;
            
            for (int i = 0; i < sceneCount; i++)
            {
                Scene scene = EditorSceneManager.GetSceneAt(i);

                // 检查场景路径是否为空（未保存的场景）
                if (string.IsNullOrEmpty(scene.path))
                {
                    Debug.LogWarning($"场景 '{scene.name}' 未保存，跳过导出。请先保存场景。");
                    continue;
                }

                // 显示场景处理进度
                float sceneProgress = (float)i / sceneCount;
                EditorUtility.DisplayProgressBar(LanguageConfig.str_LayaAirExport,
                    string.Format(LanguageConfig.str_ExportScene, scene.name), sceneProgress * 0.3f);

                EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Additive);
                HierarchyFile hierachy = new HierarchyFile(scene);
                hierachy.saveAllFile(ExportConfig.FirstlevelMenu == 0);
            }

            if (sceneCount > 1 && !string.IsNullOrEmpty(active.path)) {
                EditorSceneManager.OpenScene(active.path, OpenSceneMode.Additive);
            }

            EditorUtility.ClearProgressBar();
            SceneView.lastActiveSceneView.ShowNotification(new GUIContent(LanguageConfig.str_Exported));
            ExportLogger.Log(LanguageConfig.str_Exported);

            // 导出完成后显示不支持功能的汇总提示
            UnsupportedFeatureCollector.ShowResultDialog();
        }
        finally
        {
            // 确保进度条被清除
            EditorUtility.ClearProgressBar();
        }
    }
}
