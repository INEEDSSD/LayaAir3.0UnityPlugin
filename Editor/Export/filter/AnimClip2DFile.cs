using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// A 2D animation clip file (.mc) containing LAYAANIMATION2D:01 binary data.
/// Generated during SpriteRenderer color animation export and referenced by Animator2D component.
/// </summary>
internal class AnimClip2DFile : FileData
{
    private AnimationClip m_clip;
    private GameObject m_root;
    private ResoureMap m_resoureMap;
    private string m_targetPath;

    public AnimClip2DFile(string virtualPath, AnimationClip clip, GameObject root, ResoureMap resoureMap,
                          string targetPath = "")
        : base(virtualPath)
    {
        this.m_clip = clip;
        this.m_root = root;
        this.m_resoureMap = resoureMap;
        this.m_targetPath = targetPath;
    }

    protected override string getOutFilePath(string path)
    {
        return path;
    }

    public override void SaveFile(Dictionary<string, FileData> exportFiles)
    {
        string filePath = outPath;
        string folder = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        GameObjectUitls.writeClip2D(m_clip, fs, m_root, m_targetPath);
        // fs is closed inside writeClip2D

        base.saveMeta();
    }
}
