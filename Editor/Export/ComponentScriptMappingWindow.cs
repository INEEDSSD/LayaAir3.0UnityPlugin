using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class ComponentScriptMappingWindow : EditorWindow
{
    private List<MappingItem> mappings = new List<MappingItem>();
    private Vector2 scrollPosition;
    private string configFilePath;
    private string newComponentName = "";
    private string newUUID = "";

    [System.Serializable]
    private class MappingItem
    {
        public string componentName;
        public string uuid;

        public MappingItem(string name, string uuid)
        {
            this.componentName = name;
            this.uuid = uuid;
        }
    }

    [MenuItem("LayaAir/组件脚本导出配置")]
    public static void ShowWindow()
    {
        ComponentScriptMappingWindow window = GetWindow<ComponentScriptMappingWindow>("组件脚本UUID映射");
        window.minSize = new Vector2(600, 400);
        window.Show();
    }

    private void OnEnable()
    {
        configFilePath = Path.Combine(Application.dataPath, "LayaAir3.0UnityPlugin/Editor/Mappings/component_script_uuid_mapping.json");
        LoadMappings();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);

        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
        titleStyle.fontSize = 16;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        EditorGUILayout.LabelField("LayaAir 组件脚本UUID映射配置", titleStyle);

        EditorGUILayout.Space(5);

        EditorGUILayout.HelpBox(
            "当Unity动画控制自定义组件属性时，需要配置组件类型名到LayaAir脚本UUID的映射。\n" +
            "UUID可以在LayaAir项目的 src/xxx.ts.meta 文件中找到。",
            MessageType.Info
        );

        EditorGUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("保存配置", GUILayout.Width(100), GUILayout.Height(25)))
        {
            SaveMappings();
        }
        if (GUILayout.Button("重新加载", GUILayout.Width(100), GUILayout.Height(25)))
        {
            LoadMappings();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        DrawAddNewMappingSection();

        EditorGUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("组件类型名", EditorStyles.boldLabel, GUILayout.Width(200));
        EditorGUILayout.LabelField("LayaAir脚本UUID", EditorStyles.boldLabel);
        GUILayout.Space(80);
        EditorGUILayout.EndHorizontal();

        DrawSeparator();

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        if (mappings.Count == 0)
        {
            EditorGUILayout.Space(20);
            GUIStyle emptyStyle = new GUIStyle(EditorStyles.label);
            emptyStyle.alignment = TextAnchor.MiddleCenter;
            emptyStyle.fontStyle = FontStyle.Italic;
            EditorGUILayout.LabelField("暂无映射配置，请点击上方添加", emptyStyle);
        }
        else
        {
            for (int i = 0; i < mappings.Count; i++)
            {
                DrawMappingRow(i);
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawAddNewMappingSection()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("添加新映射", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField("组件类型名:", GUILayout.Width(90));
        newComponentName = EditorGUILayout.TextField(newComponentName, GUILayout.Width(200));
        GUILayout.Space(10);
        EditorGUILayout.LabelField("UUID:", GUILayout.Width(50));
        newUUID = EditorGUILayout.TextField(newUUID);
        GUILayout.Space(10);

        GUI.enabled = !string.IsNullOrEmpty(newComponentName) && !string.IsNullOrEmpty(newUUID);
        if (GUILayout.Button("添加", GUILayout.Width(60)))
        {
            AddMapping();
        }
        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    private void DrawMappingRow(int index)
    {
        MappingItem mapping = mappings[index];
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        mapping.componentName = EditorGUILayout.TextField(mapping.componentName, GUILayout.Width(200));
        mapping.uuid = EditorGUILayout.TextField(mapping.uuid);
        if (GUILayout.Button("删除", GUILayout.Width(60)))
        {
            if (EditorUtility.DisplayDialog("确认删除", $"确定要删除映射 '{mapping.componentName}' 吗？", "删除", "取消"))
            {
                mappings.RemoveAt(index);
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    private void AddMapping()
    {
        if (mappings.Exists(m => m.componentName == newComponentName))
        {
            EditorUtility.DisplayDialog("添加失败", $"组件类型 '{newComponentName}' 已存在。", "确定");
            return;
        }

        mappings.Add(new MappingItem(newComponentName, newUUID));
        newComponentName = "";
        newUUID = "";
        GUI.FocusControl(null);
        EditorUtility.DisplayDialog("添加成功", "映射已添加，请点击'保存配置'保存。", "确定");
    }

    private void LoadMappings()
    {
        mappings.Clear();

        if (!File.Exists(configFilePath))
        {
            return;
        }

        try
        {
            string jsonContent = File.ReadAllText(configFilePath);
            JSONObject jsonObj = new JSONObject(jsonContent);
            JSONObject mappingsObj = jsonObj.GetField("mappings");

            if (mappingsObj != null && mappingsObj.keys != null)
            {
                foreach (string key in mappingsObj.keys)
                {
                    JSONObject uuidField = mappingsObj.GetField(key);
                    if (uuidField != null && uuidField.str != null)
                    {
                        mappings.Add(new MappingItem(key, uuidField.str));
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"加载配置失败: {e.Message}");
        }
    }

    private void SaveMappings()
    {
        try
        {
            string directory = Path.GetDirectoryName(configFilePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            JSONObject jsonObj = new JSONObject(JSONObject.Type.OBJECT);
            jsonObj.AddField("_comment", "Unity组件类型到LayaAir脚本UUID的映射配置");

            JSONObject instructionsArray = new JSONObject(JSONObject.Type.ARRAY);
            instructionsArray.Add("如何获取LayaAir脚本的UUID：");
            instructionsArray.Add("1. 在LayaAir项目的src目录中找到对应的TypeScript脚本文件");
            instructionsArray.Add("2. 找到同名的.meta文件");
            instructionsArray.Add("3. 打开.meta文件，复制其中的uuid值");
            instructionsArray.Add("4. 使用菜单 LayaAir > 组件脚本导出配置 打开配置面板添加映射");
            jsonObj.AddField("_instructions", instructionsArray);

            JSONObject mappingsObj = new JSONObject(JSONObject.Type.OBJECT);
            foreach (MappingItem mapping in mappings)
            {
                if (!string.IsNullOrEmpty(mapping.componentName) && !string.IsNullOrEmpty(mapping.uuid))
                {
                    mappingsObj.AddField(mapping.componentName, mapping.uuid);
                }
            }
            jsonObj.AddField("mappings", mappingsObj);

            File.WriteAllText(configFilePath, jsonObj.Print(true));

            EditorUtility.DisplayDialog("保存成功", $"配置已保存，共 {mappings.Count} 个映射", "确定");
            AssetDatabase.Refresh();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"保存配置失败: {e.Message}");
        }
    }

    private void DrawSeparator()
    {
        EditorGUILayout.Space(2);
        Rect rect = EditorGUILayout.GetControlRect(false, 1);
        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
        EditorGUILayout.Space(2);
    }
}
