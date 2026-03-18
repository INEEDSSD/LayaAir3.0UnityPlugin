using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// A 2D Laya UI prefab file (.lh) containing an Image component with a sprite texture.
/// Generated during SpriteRenderer / Image export and referenced by the UI3D component as its prefab.
/// Optionally includes a custom material reference for non-default shaders.
/// </summary>
internal class UI2DPrefabFile : FileData
{
    private JSONObject m_data;

    /// <param name="virtualPath">Virtual path ending in .lh, used as registry key and output path</param>
    /// <param name="textureFile">The exported texture file whose UUID to embed as the Image skin</param>
    /// <param name="spriteName">Display name for the Image node</param>
    /// <param name="pixelWidth">Sprite pixel width</param>
    /// <param name="pixelHeight">Sprite pixel height</param>
    /// <param name="materialFile">Optional custom material file (null = use default material)</param>
    public UI2DPrefabFile(string virtualPath, TextureFile textureFile,
                          string spriteName, int pixelWidth, int pixelHeight,
                          MaterialFile materialFile = null)
        : base(virtualPath)
    {
        m_data = BuildData(textureFile.uuid, spriteName, pixelWidth, pixelHeight,
                          materialFile != null ? materialFile.uuid : null);
    }

    protected override string getOutFilePath(string path)
    {
        // path is already the complete relative output path
        return path;
    }

    private static JSONObject BuildData(string textureUuid, string spriteName,
                                        int pixelWidth, int pixelHeight,
                                        string materialUuid)
    {
        JSONObject root = new JSONObject(JSONObject.Type.OBJECT);
        root.AddField("_$ver", 1);
        root.AddField("_$id", "root");
        root.AddField("_$type", "Image");
        root.AddField("name", spriteName);
        root.AddField("width", pixelWidth);
        root.AddField("height", pixelHeight);
        root.AddField("skin", "res://" + textureUuid);

        // 自定义材质引用（使用非默认shader时导出）
        if (!string.IsNullOrEmpty(materialUuid))
        {
            JSONObject matRef = new JSONObject(JSONObject.Type.OBJECT);
            matRef.AddField("_$uuid", materialUuid);
            matRef.AddField("_$type", "Material");
            root.AddField("material", matRef);
        }

        return root;
    }

    public override void SaveFile(Dictionary<string, FileData> exportFiles)
    {
        string filePath = outPath;
        string folder = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        StreamWriter writer = new StreamWriter(fs);
        writer.Write(m_data.Print(true));
        writer.Close();

        base.saveMeta();
    }
}
