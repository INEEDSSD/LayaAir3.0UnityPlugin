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
            
            // 清除自定义Shader导出缓存
            CustomShaderExporter.ClearCache();

            var active = EditorSceneManager.GetActiveScene();
            var sceneCount = EditorSceneManager.sceneCount;
            
            for (int i = 0; i < sceneCount; i++)
            {
                Scene scene = EditorSceneManager.GetSceneAt(i);
                
                // 显示场景处理进度
                float sceneProgress = (float)i / sceneCount;
                EditorUtility.DisplayProgressBar(LanguageConfig.str_LayaAirExport, 
                    string.Format(LanguageConfig.str_ExportScene, scene.name), sceneProgress * 0.3f);
                
                EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Additive);
                HierarchyFile hierachy = new HierarchyFile(scene);
                hierachy.saveAllFile(ExportConfig.FirstlevelMenu == 0);
            }
            
            if (sceneCount > 1) {
                EditorSceneManager.OpenScene(active.path, OpenSceneMode.Additive);
            }

            EditorUtility.ClearProgressBar();
            SceneView.lastActiveSceneView.ShowNotification(new GUIContent(LanguageConfig.str_Exported));
            Debug.Log(LanguageConfig.str_Exported);
        }
        finally
        {
            // 确保进度条被清除
            EditorUtility.ClearProgressBar();
        }
    }
}
