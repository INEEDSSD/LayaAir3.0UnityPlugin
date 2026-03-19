using System.Collections.Generic;
using System.IO;

/// <summary>
/// Generates a fileconfig.json that maps atlas sub-texture URLs to their parent atlas files.
/// AtlasInfoManager reads this at app startup so that requesting a sub-texture URL
/// automatically triggers loading the corresponding .atlas file first.
///
/// Format:
/// {
///   "path/to/texture.atlas": ["spriteName1", "spriteName2"]
/// }
/// </summary>
internal class FileConfigFile : FileData
{
    private const string FILE_CONFIG_PATH = "fileconfig.json";

    public FileConfigFile() : base(FILE_CONFIG_PATH)
    {
    }

    protected override string getOutFilePath(string path)
    {
        return path;
    }

    public override void SaveFile(Dictionary<string, FileData> exportFiles)
    {
        JSONObject root = new JSONObject(JSONObject.Type.OBJECT);
        bool hasEntries = false;

        // Scan all export files for SpriteAtlasExportFile instances
        foreach (var kv in exportFiles)
        {
            SpriteAtlasExportFile atlasFile = kv.Value as SpriteAtlasExportFile;
            if (atlasFile == null) continue;

            // Build the entry: [frameName1, frameName2, ...]
            JSONObject frameNames = new JSONObject(JSONObject.Type.ARRAY);
            foreach (string name in atlasFile.frameNames)
            {
                frameNames.Add(name);
            }

            // Key is the atlas file path
            root.AddField(atlasFile.filePath, frameNames);
            hasEntries = true;
        }

        // Only write the file if there are atlas entries
        if (!hasEntries) return;

        string filePath = outPath;
        string folder = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        StreamWriter writer = new StreamWriter(fs);
        writer.Write(root.Print(true));
        writer.Close();

        // fileconfig.json does not need a .meta file
    }
}
