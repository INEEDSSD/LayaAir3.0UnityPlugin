using UnityEngine;

public static class ExportLogger
{
    public static void Log(string message) { }

    public static void Warning(string message)
    {
        Debug.LogWarning(message);
    }

    public static void Error(string message)
    {
        Debug.LogError(message);
    }
}
