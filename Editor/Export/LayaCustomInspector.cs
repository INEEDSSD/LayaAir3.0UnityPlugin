using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

internal class LayaCustomInspector : Editor
{
    /// <summary>
    /// Editor 程序集
    /// </summary>
    private static Assembly _EditorAssembly = Assembly.GetAssembly(typeof(Editor));

    /// <summary>
    /// 反射 Editor 类型
    /// </summary>
    private System.Type _EditorType;

    /// <summary>
    /// Editor 编辑对象类型
    /// </summary>
    private System.Type _EditorObjectType;


    private Editor _editorInstance;

    /// <summary>
    /// 反射 Editor 类型 对应实例
    /// </summary>
    protected Editor EditorInstance
    {
        get
        {
            if (_editorInstance == null && targets != null && targets.Length > 0)
            {
                _editorInstance = CreateEditor(targets, _EditorType);
            }
            if (_editorInstance == null)
            {
                throw new System.ArgumentException(string.Format("Create {0} failed", _EditorType));
            }

            return _editorInstance;
        }
    }

    /// <summary>
    /// 缓存 MethodInfo maps
    /// </summary>
    private static Dictionary<string, MethodInfo> MethodMap = new Dictionary<string, MethodInfo>();
    /// <summary>
    /// 缓存 PropertyInfo maps
    /// </summary>
    private static Dictionary<string, PropertyInfo> PropertyMap = new Dictionary<string, PropertyInfo>();

    /// <summary>
    /// 缓存 FieldInfo maps
    /// </summary>
    private static Dictionary<string, FieldInfo> FieldMap = new Dictionary<string, FieldInfo>();

    public LayaCustomInspector(string unity_particleInspector)
    {
        _EditorType = _EditorAssembly.GetTypes().Where(t => t.Name == unity_particleInspector).FirstOrDefault();

        // 此 CustomEditor 脚本 指定 EditorObject type
        _EditorObjectType = GetCustomEditorType(GetType());

        // 反射类 原始 EditorObject type
        var oriEditorObjectType = GetCustomEditorType(_EditorType);

        // 检查 Editor 编辑类型是否相符
        if (_EditorObjectType != oriEditorObjectType)
        {
            throw new System.ArgumentException(
                    string.Format("Type {0} does not match the editor {1} type {2}",
                              _EditorObjectType, unity_particleInspector, oriEditorObjectType));
        }
    }

    public Editor customEditor { get { return EditorInstance; } }

    /// <summary>
    /// 获取 Editor 编辑对象类型
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    private System.Type GetCustomEditorType(System.Type type)
    {
        var flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;

        var customAttributes = type.GetCustomAttributes(typeof(CustomEditor), true) as CustomEditor[];
        var customField = customAttributes.Select(editor => editor.GetType().GetField("m_InspectedType", flags)).FirstOrDefault();
        return customField.GetValue(customAttributes[0]) as System.Type;
    }

    /// <summary>
    /// 获取 MethodInfo
    /// </summary>
    /// <param name="methodName"></param>
    /// <returns></returns>
    protected MethodInfo GetMethod(string methodName)
    {
        if (MethodMap.ContainsKey(methodName))
        {
            return MethodMap[methodName];
        }

        var flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
        MethodInfo methodInfo = _EditorType.GetMethod(methodName, flags);
        if (methodInfo != null)
        {
            MethodMap.Add(methodName, methodInfo);
        }
        return methodInfo;
    }

    /// <summary>
    /// 调用 反射函数
    /// </summary>
    /// <param name="methodName"></param>
    /// <param name="parameters"></param>
    /// <returns></returns>
    protected object InvokeMethodInfo(string methodName, object[] parameters)
    {
        MethodInfo methodInfo = GetMethod(methodName);
        if (methodInfo != null)
        {
            return methodInfo.Invoke(EditorInstance, parameters);
        }
        return null;
    }

    /// <summary>
    /// 获取 PropertyInfo
    /// </summary>
    /// <param name="propertyName"></param>
    /// <returns></returns>
    protected PropertyInfo GetProperty(string propertyName)
    {
        if (PropertyMap.ContainsKey(propertyName))
        {
            return PropertyMap[propertyName];
        }

        var flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
        PropertyInfo propertyInfo = _EditorType.GetProperty(propertyName, flags);

        if (propertyInfo != null)
        {
            PropertyMap.Add(propertyName, propertyInfo);
        }

        return propertyInfo;
    }

    protected FieldInfo GetField(string fieldName)
    {
        if (FieldMap.ContainsKey(fieldName)) { 
            return FieldMap[fieldName];
        }
        var flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
        FieldInfo fieldInfo = _EditorType.GetField(fieldName, flags);

        if (fieldInfo != null)
        {
            FieldMap.Add(fieldName, fieldInfo);
        }

        return fieldInfo;
    }

    void OnDisable()
    {
        if (_editorInstance != null)
        {
            DestroyImmediate(_editorInstance);
        }
    }

    public void OnSceneGUI()
    {
        InvokeMethodInfo("OnSceneGUI", null);
    }
    public void OnSceneViewGUI(SceneView sceneView)
    {
        InvokeMethodInfo("OnSceneViewGUI", new object[] { sceneView });
    }

    protected override void OnHeaderGUI()
    {
        InvokeMethodInfo("OnHeaderGUI", null);
    }

    public override void OnInspectorGUI()
    {
        EditorInstance.OnInspectorGUI();
    }

    public override bool UseDefaultMargins()
    {
        return EditorInstance.UseDefaultMargins();
    }

    public override bool RequiresConstantRepaint()
    {
        return EditorInstance.RequiresConstantRepaint();
    }

    public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height)
    {
        return EditorInstance.RenderStaticPreview(assetPath, subAssets, width, height);
    }

    public override void ReloadPreviewInstances()
    {
        EditorInstance.ReloadPreviewInstances();
    }

    public override void OnPreviewSettings()
    {
        EditorInstance.OnPreviewSettings();
    }

    public override void OnPreviewGUI(Rect r, GUIStyle background)
    {
        EditorInstance.OnPreviewGUI(r, background);
    }

    public override void OnInteractivePreviewGUI(Rect r, GUIStyle background)
    {
        EditorInstance.OnInteractivePreviewGUI(r, background);
    }

    public override bool HasPreviewGUI()
    {
        return EditorInstance.HasPreviewGUI();
    }

    public override GUIContent GetPreviewTitle()
    {
        return EditorInstance.GetPreviewTitle();
    }

    public override string GetInfoString()
    {
        return EditorInstance.GetInfoString();
    }

    public override void DrawPreview(Rect previewArea)
    {
        EditorInstance.DrawPreview(previewArea);
    }

}
