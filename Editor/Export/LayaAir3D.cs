using System;
using System.Collections;
using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using marijnz.EditorCoroutines;

public class LayaAir3D : EditorWindow
{
    private static Vector2 ScrollPosition;

    private static bool GameObjectSetting;
    public static bool MeshSetting;
    private static bool OtherSetting;
    private static bool CustomShaderSetting;
    private static bool ComponentScriptSetting; // 组件脚本UUID映射配置

    public static bool Scenes;
    public static int sceneIndex;

    private static GUIStyle g = new GUIStyle();

    private static bool PassNull = false;

    public static LayaAir3D layaWindow;

    private static Texture2D exporttu;
   
    

    [MenuItem("LayaAir3D 3.0/Export Tool", false, 1)]
    public static void initLayaExport()
    {
        LanguageConfig.configLanguage();
        layaWindow = (LayaAir3D)EditorWindow.GetWindow(typeof(LayaAir3D));
        exporttu = new Texture2D(52, 52);
        Util.FileUtil.FileStreamLoadTexture(Util.FileUtil.getPluginResUrl("LayaResouce/Export.png"), exporttu);
    }

    [MenuItem("LayaAir3D 3.0/Help/Study")]
    static void initLayaStudy()
    {
        ServeConfig.getInstance().openurl(URLType.StudyURL);
    }

    [MenuItem("LayaAir3D 3.0/Help/Answsers")]
    static void initLayaAsk()
    {
        ServeConfig.getInstance().openurl(URLType.LayaAskURL);
    }
    void OnGUI()
    {
        ExportConfig.initConfig();
        LanguageConfig.configLanguage();

        GUILayout.Space(10);

        
        GUILayout.Space(15);
        GUILayout.BeginHorizontal();
        GUILayout.Space(24);
        ExportConfig.FirstlevelMenu = GUILayout.Toolbar(ExportConfig.FirstlevelMenu, new string[] { LanguageConfig.str_Scene, LanguageConfig.str_Sprite3D }, GUILayout.Height(30), GUILayout.Width(position.width - 48));
        GUILayout.EndHorizontal();
        ScrollPosition = GUILayout.BeginScrollView(ScrollPosition);

        GUILayout.Space(25);
        //---------------------------------------GameObjectSetting------------------------------------------
        GUILayout.BeginHorizontal();
        GUILayout.Space(21);
        GameObjectSetting = EditorGUILayout.Foldout(GameObjectSetting, LanguageConfig.str_GameObjectSetting, true);
        GUILayout.EndHorizontal();
        if (GameObjectSetting)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(21);
            GUILayout.Label("", GUILayout.Width(15));
            GUILayout.BeginVertical(GUILayout.ExpandWidth(true));

            ExportConfig.IgnoreNotActiveGameObject = GUILayout.Toggle(ExportConfig.IgnoreNotActiveGameObject, LanguageConfig.str_IgnoreNotActiveGameObjects);

            if (ExportConfig.FirstlevelMenu == 1)
            {
                ExportConfig.BatchMade = GUILayout.Toggle(ExportConfig.BatchMade, LanguageConfig.str_BatchMakeTheFirstLevelGameObjects);
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

        }

        //??
        GUILayout.BeginHorizontal();
        GUILayout.Space(25);
        GUILayout.Box("", GUILayout.Height(1), GUILayout.Width(position.width - 50));
        GUILayout.EndHorizontal();

        //---------------------------------------GameObjectSetting------------------------------------------
        GUILayout.Space(10);
        //---------------------------------------MeshSetting------------------------------------------
        GUILayout.BeginHorizontal();
        GUILayout.Space(21);
        MeshSetting = EditorGUILayout.Foldout(MeshSetting, LanguageConfig.str_MeshSetting, true);
        GUILayout.EndHorizontal();
        if (MeshSetting)
        {
            GUILayout.BeginVertical(GUILayout.ExpandWidth(true));

            GUILayout.BeginHorizontal();
            GUILayout.Space(21);
            GUILayout.Label("", g, GUILayout.Width(15));

            ExportConfig.IgnoreVerticesUV = GUILayout.Toggle(ExportConfig.IgnoreVerticesUV, LanguageConfig.str_IgnoreVerticesUV);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(21);
            GUILayout.Label("", g, GUILayout.Width(15));

            ExportConfig.IgnoreVerticesColor = GUILayout.Toggle(ExportConfig.IgnoreVerticesColor, LanguageConfig.str_IgnoreVerticesColor);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(21);
            GUILayout.Label("", g, GUILayout.Width(15));

            ExportConfig.IgnoreVerticesNormal = GUILayout.Toggle(ExportConfig.IgnoreVerticesNormal, LanguageConfig.str_IgnoreVerticesNormal);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(21);
            GUILayout.Label("", g, GUILayout.Width(15));

            ExportConfig.IgnoreVerticesTangent = GUILayout.Toggle(ExportConfig.IgnoreVerticesTangent, LanguageConfig.str_IgnoreVerticesTangent);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(21);
            GUILayout.Label("", g, GUILayout.Width(15));

            ExportConfig.AutoVerticesUV1 = GUILayout.Toggle(ExportConfig.AutoVerticesUV1, LanguageConfig.str_AutoVerticesUV1);
            GUILayout.EndHorizontal();


            GUILayout.EndVertical();
        }
        //---------------------------------------OtherSetting------------------------------------------
        //??
        GUILayout.BeginHorizontal();
        GUILayout.Space(25);
        GUILayout.Box("", GUILayout.Height(1), GUILayout.Width(position.width - 50));
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        GUILayout.BeginHorizontal();
        GUILayout.Space(21);
        OtherSetting = EditorGUILayout.Foldout(OtherSetting, LanguageConfig.str_OtherSetting, true);
        GUILayout.EndHorizontal();
        if (OtherSetting)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(21);
            GUILayout.Label("", GUILayout.Width(15));
            GUILayout.BeginVertical(GUILayout.ExpandWidth(true));

            GUILayout.BeginHorizontal();

            ExportConfig.CustomizeDirectory = GUILayout.Toggle(ExportConfig.CustomizeDirectory, LanguageConfig.str_CustomizeExportRootDirectoryName, GUILayout.Width(250));
            if (ExportConfig.CustomizeDirectory)
                ExportConfig.CustomizeDirectoryName = GUILayout.TextField(ExportConfig.CustomizeDirectoryName);
            GUILayout.EndHorizontal();

            // 粒子系统导出模式
            GUILayout.Space(10);
            GUILayout.Label("粒子导出默认模式 (Particle Export Mode)", EditorStyles.boldLabel);
            ExportConfig.ParticleExportMode = GUILayout.Toolbar(
                ExportConfig.ParticleExportMode,
                new string[] { "Shuriken (GPU)", "CPU Particle" },
                GUILayout.Height(25)
            );
            EditorGUILayout.HelpBox(
                "全局默认值。单个粒子可挂 LayaParticleExportSetting 组件覆盖。\n" +
                "Global default. Individual particles can override via LayaParticleExportSetting component.",
                MessageType.Info
            );
            GUILayout.Space(5);

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }


        //??
        GUILayout.BeginHorizontal();
        GUILayout.Space(21);
        GUILayout.Box("", GUILayout.Height(1), GUILayout.Width(position.width - 60));
        GUILayout.EndHorizontal();

        //---------------------------------------CustomShaderSetting------------------------------------------
        GUILayout.Space(10);

        GUILayout.BeginHorizontal();
        GUILayout.Space(21);
        CustomShaderSetting = EditorGUILayout.Foldout(CustomShaderSetting, LanguageConfig.str_CustomShaderSetting, true);
        GUILayout.EndHorizontal();
        if (CustomShaderSetting)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(21);
            GUILayout.Label("", GUILayout.Width(15));
            GUILayout.BeginVertical(GUILayout.ExpandWidth(true));

            // 启用自定义Shader导出（自动转换未配置的Shader）
            ExportConfig.EnableCustomShaderExport = GUILayout.Toggle(ExportConfig.EnableCustomShaderExport, LanguageConfig.str_EnableCustomShaderExport);
            
            if (ExportConfig.EnableCustomShaderExport)
            {
                GUILayout.Space(5);

                // 提示信息
                EditorGUILayout.HelpBox(LanguageConfig.str_CustomShaderTips, MessageType.Info);
            }

#if ENABLE_PARTICLE_MESH_OPTIMIZATION
            // ========== 粒子系统Mesh优化设置 ==========
            GUILayout.Space(10);
            GUILayout.Label("粒子系统Mesh优化", EditorStyles.boldLabel);
            GUILayout.Space(5);

            // 显示警告开关
            ExportConfig.ShowParticleMeshWarning = GUILayout.Toggle(
                ExportConfig.ShowParticleMeshWarning,
                "显示粒子Mesh顶点数警告"
            );

            // 自动简化Mesh开关
            ExportConfig.AutoSimplifyParticleMesh = GUILayout.Toggle(
                ExportConfig.AutoSimplifyParticleMesh,
                "自动简化超限的粒子Mesh"
            );

            if (ExportConfig.AutoSimplifyParticleMesh)
            {
                GUILayout.Space(5);

                EditorGUI.indentLevel++;

                // 简化质量滑块
                GUILayout.BeginHorizontal();
                GUILayout.Label("简化质量:", GUILayout.Width(80));
                ExportConfig.ParticleMeshSimplifyQuality = EditorGUILayout.Slider(
                    ExportConfig.ParticleMeshSimplifyQuality,
                    0.1f,
                    1.0f
                );
                GUILayout.Label(string.Format("{0:P0}", ExportConfig.ParticleMeshSimplifyQuality), GUILayout.Width(40));
                GUILayout.EndHorizontal();

                // 最大顶点数设置
                GUILayout.BeginHorizontal();
                GUILayout.Label("顶点数限制:", GUILayout.Width(80));
                ExportConfig.ParticleMeshMaxVertices = EditorGUILayout.IntSlider(
                    ExportConfig.ParticleMeshMaxVertices,
                    10000,
                    100000
                );
                GUILayout.Label(ExportConfig.ParticleMeshMaxVertices.ToString(), GUILayout.Width(60));
                GUILayout.EndHorizontal();

                EditorGUI.indentLevel--;

                GUILayout.Space(5);
                EditorGUILayout.HelpBox(
                    "自动简化功能将在导出时检测粒子系统mesh顶点数，" +
                    "如果超过限制会自动简化mesh到安全范围。\n" +
                    "简化质量越高，保留的细节越多，但简化程度越低。\n" +
                    "建议质量: 0.7",
                    MessageType.Info
                );
            }
#endif

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        //??
        GUILayout.BeginHorizontal();
        GUILayout.Space(21);
        GUILayout.Box("", GUILayout.Height(1), GUILayout.Width(position.width - 60));
        GUILayout.EndHorizontal();

        //---------------------------------------ComponentScriptSetting------------------------------------------
        GUILayout.Space(10);
        GUILayout.BeginHorizontal();
        GUILayout.Space(21);
        ComponentScriptSetting = EditorGUILayout.Foldout(ComponentScriptSetting, "组件脚本导出配置", true);
        GUILayout.EndHorizontal();

        if (ComponentScriptSetting)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(21);
            GUILayout.BeginVertical();

            EditorGUILayout.HelpBox(
                "当Unity动画控制自定义组件属性时，需要配置组件类型名到LayaAir脚本UUID的映射。\n" +
                "UUID可以在LayaAir项目的 src/xxx.ts.meta 文件中找到。",
                MessageType.Info
            );

            GUILayout.Space(5);

            if (GUILayout.Button("打开组件UUID映射配置面板", GUILayout.Height(30)))
            {
                // 通过反射安全地打开窗口，避免编译依赖
                OpenComponentMappingWindow();
            }

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        //??
        GUILayout.BeginHorizontal();
        GUILayout.Space(21);
        GUILayout.Box("", GUILayout.Height(1), GUILayout.Width(position.width - 60));
        GUILayout.EndHorizontal();

        GUILayout.EndScrollView();
        GUILayout.BeginHorizontal();
        if (PassNull)
        {
            GUIStyle g = new GUIStyle();
            g.normal.textColor = Color.red;

            GUILayout.Label(LanguageConfig.str_SavePathcannotbeempty, g);
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Space(21);
        GUILayout.Label(LanguageConfig.str_SavePath, GUILayout.Width(69), GUILayout.ExpandWidth(false));
        string savePath = ExportConfig.SAVEPATH;
        savePath = GUILayout.TextField(savePath, GUILayout.Height(21));

        if (savePath.Length <= 0)
        {
            savePath = "Assets";
        }
        if (GUILayout.Button(LanguageConfig.str_Browse, GUILayout.MaxWidth(100), GUILayout.Height(22)))
        {
            savePath = EditorUtility.SaveFolderPanel("LayaUnityPlugin", savePath, "");   
        }
        if (savePath.Length > 0)
        {
            ExportConfig.SAVEPATH = savePath;
            PassNull = false;
            this.Repaint();
        }
        GUILayout.Space(21);
        GUILayout.EndHorizontal();
        GUILayout.Space(21);
        
        GUILayout.BeginHorizontal();
        GUILayout.Space(21);
        GUIContent c22 = new GUIContent(LanguageConfig.str_LayaAirExport, exporttu);
        if (GUILayout.Button(c22, GUILayout.Height(30), GUILayout.Width(position.width - 45)))
        {
            try {
                LayaAir3Export.ExportScene();
            } catch(Exception e) {
                Debug.LogError(LanguageConfig.str_ExportFailed + "\n" + e.Message + "\n" + e.StackTrace);
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(30);
        ExportConfig.saveConfiguration();

    }

    /// <summary>
    /// 打开组件UUID映射配置窗口
    /// </summary>
    private static void OpenComponentMappingWindow()
    {
        // 直接调用Unity菜单命令（最可靠的方式）
        bool success = EditorApplication.ExecuteMenuItem("LayaAir/组件脚本导出配置");

        if (!success)
        {
            Debug.LogError("无法打开配置面板。请确认Unity Console中是否有编译错误。");
        }
    }

}
