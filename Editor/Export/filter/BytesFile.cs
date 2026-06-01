using System.Collections.Generic;
using System.IO;

/// <summary>
/// 写入预编码字节数组的最小 FileData。
/// 用于 Cubemap 面纹理等运行时生成的资源。
/// </summary>
internal class BytesFile : FileData
{
    private byte[] m_bytes;

    public BytesFile(string path, byte[] bytes) : base(path)
    {
        this.m_bytes = bytes;
    }

    public override void SaveFile(Dictionary<string, FileData> exportFiles)
    {
        base.saveMeta();
        string folder = Path.GetDirectoryName(outPath);
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);
        File.WriteAllBytes(outPath, m_bytes);
    }
}
