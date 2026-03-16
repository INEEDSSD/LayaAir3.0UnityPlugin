using UnityEngine;

/// <summary>
/// 导出日志工具类 - 根据配置控制调试日志输出
/// </summary>
public static class ExportLogger
{
    /// <summary>
    /// 输出调试日志（受EnableDebugLog配置控制）
    /// </summary>
    public static void Log(string message)
    {
        if (ExportConfig.EnableDebugLog)
        {
            Debug.Log(message);
        }
    }

    /// <summary>
    /// 输出警告日志（始终显示）
    /// </summary>
    public static void Warning(string message)
    {
        Debug.LogWarning(message);
    }

    /// <summary>
    /// 输出错误日志（始终显示）
    /// </summary>
    public static void Error(string message)
    {
        Debug.LogError(message);
    }
}
