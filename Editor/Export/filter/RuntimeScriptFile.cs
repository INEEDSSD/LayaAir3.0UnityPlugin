using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Copies a TypeScript runtime script from the plugin's RuntimeScripts directory
/// to the export output's _src_ directory.
/// The UUID is stable across exports (persisted via .meta file at the output path).
/// </summary>
internal class RuntimeScriptFile : FileData
{
    private string m_sourcePath;

    /// <param name="fileName">Script file name (e.g. "Animator2DSync.ts")</param>
    public RuntimeScriptFile(string fileName)
        : base("_src_/" + fileName)
    {
        m_sourcePath = Path.Combine(
            Application.dataPath,
            "LayaAir3.0UnityPlugin/Editor/Export/RuntimeScripts/" + fileName);
    }

    protected override string getOutFilePath(string path)
    {
        // path is already "_src_/filename.ts"
        return path;
    }

    public override void SaveFile(Dictionary<string, FileData> exportFiles)
    {
        if (!File.Exists(m_sourcePath))
        {
            Debug.LogError($"[LayaAir Export] RuntimeScript not found: {m_sourcePath}");
            return;
        }

        string filePath = outPath;
        string folder = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        File.Copy(m_sourcePath, filePath, true);
        base.saveMeta();
    }
}
