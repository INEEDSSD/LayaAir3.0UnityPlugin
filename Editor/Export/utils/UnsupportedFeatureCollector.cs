using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// 收集导出过程中发现的 LayaAir 不支持的 Unity 组件，导出完成后汇总提示。
/// </summary>
public static class UnsupportedFeatureCollector
{
    // 不支持的组件类型 → 出现的 GameObject 路径列表
    private static Dictionary<string, List<string>> unsupportedComponents;

    // 已检查过的 GameObject 实例ID集合，避免重复检查
    private static HashSet<int> checkedObjects;

    // 不支持的组件类型定义（Type → 提示文案）
    private static readonly Dictionary<Type, string> UnsupportedComponentTypes = new Dictionary<Type, string>
    {
        { typeof(TrailRenderer), "TrailRenderer（拖尾渲染器）" },
        { typeof(LineRenderer), "LineRenderer（线条渲染器）" },
        { typeof(Cloth), "Cloth（布料模拟）" },
        { typeof(WindZone), "WindZone（风力区域）" },
        { typeof(Terrain), "Terrain（地形） - 仅部分支持" },
        { typeof(AudioSource), "AudioSource（音频源）" },
        { typeof(AudioListener), "AudioListener（音频监听器）" },
        { typeof(Canvas), "Canvas（UI画布）" },
        { typeof(VideoPlayer), "VideoPlayer（视频播放器）" },
        // 物理组件
        { typeof(Rigidbody), "Rigidbody（刚体）" },
        { typeof(BoxCollider), "BoxCollider（盒碰撞器）" },
        { typeof(SphereCollider), "SphereCollider（球碰撞器）" },
        { typeof(CapsuleCollider), "CapsuleCollider（胶囊碰撞器）" },
        { typeof(MeshCollider), "MeshCollider（网格碰撞器）" },
        { typeof(CharacterController), "CharacterController（角色控制器）" },
        { typeof(Joint), "Joint（关节） - 包括所有关节类型" },
    };

    /// <summary>
    /// 导出开始时调用，清空之前的记录
    /// </summary>
    public static void Init()
    {
        unsupportedComponents = new Dictionary<string, List<string>>();
        checkedObjects = new HashSet<int>();
    }

    /// <summary>
    /// 检查一个 GameObject 上是否挂载了不支持的组件
    /// </summary>
    public static void CheckGameObject(GameObject go)
    {
        if (go == null) return;

        int instanceId = go.GetInstanceID();
        if (checkedObjects.Contains(instanceId)) return;
        checkedObjects.Add(instanceId);

        string goPath = GetGameObjectPath(go);

        foreach (var kvp in UnsupportedComponentTypes)
        {
            Type compType = kvp.Key;
            string displayName = kvp.Value;

            // Joint 是基类，用 GetComponent 会匹配所有派生类型
            Component comp = go.GetComponent(compType);
            if (comp != null)
            {
                if (!unsupportedComponents.ContainsKey(displayName))
                {
                    unsupportedComponents[displayName] = new List<string>();
                }
                unsupportedComponents[displayName].Add(goPath);
            }
        }
    }

    /// <summary>
    /// 手动添加一条不支持功能的警告（供外部模块调用，如粒子导出）
    /// </summary>
    /// <param name="featureName">功能名称（作为分类 key）</param>
    /// <param name="goPath">GameObject 路径</param>
    /// <param name="hint">额外提示信息（会附加到路径后面）</param>
    public static void AddWarning(string featureName, string goPath, string hint = null)
    {
        if (unsupportedComponents == null)
            unsupportedComponents = new Dictionary<string, List<string>>();

        if (!unsupportedComponents.ContainsKey(featureName))
        {
            unsupportedComponents[featureName] = new List<string>();
        }

        string entry = string.IsNullOrEmpty(hint) ? goPath : goPath + "  (" + hint + ")";
        unsupportedComponents[featureName].Add(entry);
    }

    /// <summary>
    /// 手动添加一条不支持功能的警告（传入 GameObject，自动获取路径）
    /// </summary>
    public static void AddWarning(string featureName, GameObject go, string hint = null)
    {
        if (go == null) return;
        AddWarning(featureName, GetGameObjectPath(go), hint);
    }

    /// <summary>
    /// 是否有任何不支持的组件被检测到
    /// </summary>
    public static bool HasWarnings()
    {
        return unsupportedComponents != null && unsupportedComponents.Count > 0;
    }

    /// <summary>
    /// 获取汇总文本（用于弹窗显示）
    /// </summary>
    public static string GetSummary()
    {
        if (!HasWarnings()) return string.Empty;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("场景中发现以下 LayaAir 不支持的组件，相关功能将不会被导出：\n");

        foreach (var kvp in unsupportedComponents)
        {
            sb.AppendFormat("  ● {0} — {1} 处\n", kvp.Key, kvp.Value.Count);
        }

        sb.AppendLine("\n详细的 GameObject 列表已输出到 Console 窗口。");
        return sb.ToString();
    }

    /// <summary>
    /// 导出完成后调用：在 Console 输出详细列表，并弹窗显示汇总
    /// </summary>
    public static void ShowResultDialog()
    {
        if (!HasWarnings()) return;

        // 在 Console 中输出详细信息
        foreach (var kvp in unsupportedComponents)
        {
            StringBuilder detail = new StringBuilder();
            detail.AppendFormat("[LayaAir Export] 不支持的组件: {0} ({1} 处)\n", kvp.Key, kvp.Value.Count);
            foreach (string path in kvp.Value)
            {
                detail.AppendFormat("  - {0}\n", path);
            }
            ExportLogger.Warning(detail.ToString());
        }

        // 弹窗显示汇总
        EditorUtility.DisplayDialog(
            "LayaAir 导出完成 - 兼容性提示",
            GetSummary(),
            "确定"
        );
    }

    /// <summary>
    /// 获取 GameObject 在 Hierarchy 中的完整路径
    /// </summary>
    private static string GetGameObjectPath(GameObject go)
    {
        Transform t = go.transform;
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}
