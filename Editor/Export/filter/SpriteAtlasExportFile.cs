using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Exports a LayaAir .atlas file for sprites that are sub-regions of a texture.
/// The atlas maps sprite names to their pixel rects within the parent texture.
/// Used when a Unity Sprite occupies only part of its source texture (spritesheet/atlas).
/// </summary>
internal class SpriteAtlasExportFile : FileData
{
    private string m_textureFileName;  // relative image filename (e.g. "texture.png")
    private Dictionary<string, SpriteFrameData> m_frames = new Dictionary<string, SpriteFrameData>();

    private struct SpriteFrameData
    {
        public int x, y, w, h;
        public int sourceW, sourceH;
        public int offsetX, offsetY;
    }

    /// <param name="virtualPath">Path for the .atlas file (e.g. "Assets/Textures/myTex.atlas")</param>
    /// <param name="textureFileName">The texture image filename relative to atlas directory (e.g. "myTex.png")</param>
    public SpriteAtlasExportFile(string virtualPath, string textureFileName)
        : base(virtualPath)
    {
        m_textureFileName = textureFileName;
    }

    /// <summary>
    /// Add a sprite frame to this atlas.
    /// </summary>
    /// <param name="spriteName">The sprite name (used as frame key and sub-texture URL suffix)</param>
    /// <param name="rect">The sprite's pixel rect within the texture (Unity coordinates: origin bottom-left)</param>
    /// <param name="textureHeight">Full texture height, used to flip Y from Unity to LayaAir coordinates</param>
    /// <param name="sourceSize">Original sprite size (before trimming)</param>
    /// <param name="spriteSourceOffset">Offset from trimmed to original (usually 0,0)</param>
    public void AddFrame(string spriteName, Rect rect, int textureHeight,
                         Vector2Int sourceSize, Vector2Int spriteSourceOffset)
    {
        // Unity sprite rects have origin at bottom-left; LayaAir atlas expects top-left origin.
        // Flip Y: atlasY = textureHeight - unityY - spriteHeight
        int flippedY = textureHeight - Mathf.RoundToInt(rect.y) - Mathf.RoundToInt(rect.height);

        m_frames[spriteName + ".png"] = new SpriteFrameData
        {
            x = Mathf.RoundToInt(rect.x),
            y = flippedY,
            w = Mathf.RoundToInt(rect.width),
            h = Mathf.RoundToInt(rect.height),
            sourceW = sourceSize.x,
            sourceH = sourceSize.y,
            offsetX = spriteSourceOffset.x,
            offsetY = spriteSourceOffset.y
        };
    }

    /// <summary>
    /// Check if a frame with the given sprite name already exists in this atlas.
    /// </summary>
    public bool HasFrame(string spriteName)
    {
        return m_frames.ContainsKey(spriteName + ".png");
    }

    /// <summary>
    /// Get the sub-texture reference for use in _$uuid fields of .lh etc.
    /// Format: "atlasUUID@spriteName" — engine resolves this to the cached sub-texture.
    /// </summary>
    public string GetSubTextureRef(string spriteName)
    {
        return this.uuid + "@" + spriteName;
    }

    public IEnumerable<string> frameNames { get { return m_frames.Keys; } }

    protected override string getOutFilePath(string path)
    {
        return path;
    }

    public override void SaveFile(Dictionary<string, FileData> exportFiles)
    {
        JSONObject root = new JSONObject(JSONObject.Type.OBJECT);

        // frames
        JSONObject frames = new JSONObject(JSONObject.Type.OBJECT);
        foreach (var kv in m_frames)
        {
            JSONObject frameObj = new JSONObject(JSONObject.Type.OBJECT);

            JSONObject frame = new JSONObject(JSONObject.Type.OBJECT);
            frame.AddField("x", kv.Value.x);
            frame.AddField("y", kv.Value.y);
            frame.AddField("w", kv.Value.w);
            frame.AddField("h", kv.Value.h);
            frame.AddField("idx", 0);
            frameObj.AddField("frame", frame);

            JSONObject sss = new JSONObject(JSONObject.Type.OBJECT);
            sss.AddField("x", kv.Value.offsetX);
            sss.AddField("y", kv.Value.offsetY);
            frameObj.AddField("spriteSourceSize", sss);

            JSONObject ss = new JSONObject(JSONObject.Type.OBJECT);
            ss.AddField("w", kv.Value.sourceW);
            ss.AddField("h", kv.Value.sourceH);
            frameObj.AddField("sourceSize", ss);

            frames.AddField(kv.Key, frameObj);
        }
        root.AddField("frames", frames);

        // meta
        JSONObject meta = new JSONObject(JSONObject.Type.OBJECT);
        // image: relative filename — AtlasLoader joins with folderPath to load the texture
        meta.AddField("image", m_textureFileName);
        root.AddField("meta", meta);

        // Write file
        string filePath = outPath;
        string folder = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        StreamWriter writer = new StreamWriter(fs);
        writer.Write(root.Print(true));
        writer.Close();

        base.saveMeta();
    }
}
