using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// A 2D Laya UI prefab file (.lh) containing a Sprite with Mesh2DRender component.
/// Generated during SpriteRenderer export and referenced by the UI3D component as its prefab.
///
/// Two modes:
/// - Full texture mode: texture referenced by UUID (when sprite covers the entire texture)
/// - Atlas sub-texture mode: texture referenced by sub-texture URL path, with atlas preload
/// </summary>
internal class UI2DPrefabFile : FileData
{
    private JSONObject m_data;

    /// <summary>
    /// Full-texture constructor: the sprite covers the entire texture, referenced by UUID.
    /// </summary>
    /// <param name="virtualPath">Virtual path ending in .lh, used as registry key and output path</param>
    /// <param name="textureFile">The exported texture file whose UUID to embed as the Mesh2DRender texture</param>
    /// <param name="spriteName">Display name for the Sprite node</param>
    /// <param name="pixelWidth">Sprite pixel width</param>
    /// <param name="pixelHeight">Sprite pixel height</param>
    /// <param name="spriteColor">SpriteRenderer color to apply to Mesh2DRender</param>
    /// <param name="materialUUID">UUID of the baseRender2D default material for sharedMaterial</param>
    /// <param name="animationData">Optional Animator2D component JSON to append to _$comp array</param>
    public UI2DPrefabFile(string virtualPath, TextureFile textureFile,
                          string spriteName, int pixelWidth, int pixelHeight,
                          Color spriteColor, string materialUUID,
                          JSONObject animationData = null)
        : base(virtualPath)
    {
        m_data = BuildData(textureFile.uuid, spriteName, pixelWidth, pixelHeight,
                           spriteColor, null, materialUUID, animationData);
    }

    /// <summary>
    /// Atlas sub-texture constructor: the sprite is a sub-region, referenced by "uuid@spriteName".
    /// The atlas is preloaded via _$preloads both here and at the .ls/.lh root level.
    /// </summary>
    /// <param name="virtualPath">Virtual path ending in .lh</param>
    /// <param name="subTextureRef">Sub-texture reference (e.g. "atlasUUID@spriteName")</param>
    /// <param name="atlasUUID">Atlas file UUID for preloading via _$preloads</param>
    /// <param name="spriteName">Display name for the Sprite node</param>
    /// <param name="pixelWidth">Sprite pixel width</param>
    /// <param name="pixelHeight">Sprite pixel height</param>
    /// <param name="spriteColor">SpriteRenderer color to apply to Mesh2DRender</param>
    /// <param name="materialUUID">UUID of the baseRender2D default material for sharedMaterial</param>
    /// <param name="animationData">Optional Animator2D component JSON to append to _$comp array</param>
    public UI2DPrefabFile(string virtualPath, string subTextureRef, string atlasUUID,
                          string spriteName, int pixelWidth, int pixelHeight,
                          Color spriteColor, string materialUUID,
                          JSONObject animationData = null)
        : base(virtualPath)
    {
        // subTextureRef is "atlasUUID@spriteName" — engine's isUUID returns true,
        // prepends "res://" to form "res://atlasUUID@spriteName".
        m_data = BuildData(subTextureRef, spriteName, pixelWidth, pixelHeight,
                           spriteColor, atlasUUID, materialUUID, animationData);
    }

    protected override string getOutFilePath(string path)
    {
        // path is already the complete relative output path
        return path;
    }

    /// <param name="textureRef">UUID (full-texture mode) or "atlasUUID@spriteName" (atlas mode)</param>
    /// <param name="atlasUUID">If non-null, atlas file UUID to add to _$preloads</param>
    /// <param name="materialUUID">UUID of the baseRender2D default material</param>
    private static JSONObject BuildData(string textureRef, string spriteName,
                                        int pixelWidth, int pixelHeight,
                                        Color spriteColor,
                                        string atlasUUID,
                                        string materialUUID,
                                        JSONObject animationData)
    {
        JSONObject root = new JSONObject(JSONObject.Type.OBJECT);
        root.AddField("_$ver", 1);
        root.AddField("_$id", "root");
        root.AddField("_$type", "Sprite");
        root.AddField("name", spriteName);
        root.AddField("width", pixelWidth);
        root.AddField("height", pixelHeight);

        // Atlas preloads: ensure the atlas is loaded before the sub-texture _$uuid is resolved.
        // Also declared at the .ls scene level (HierarchyFile.getSceneNode) for early loading.
        if (atlasUUID != null)
        {
            JSONObject preloads = new JSONObject(JSONObject.Type.ARRAY);
            preloads.Add(atlasUUID);
            root.AddField("_$preloads", preloads);

            JSONObject preloadTypes = new JSONObject(JSONObject.Type.ARRAY);
            preloadTypes.Add("Atlas");
            root.AddField("_$preloadTypes", preloadTypes);
        }

        // Components array
        JSONObject compArray = new JSONObject(JSONObject.Type.ARRAY);

        // Mesh2DRender component
        JSONObject mesh2DComp = new JSONObject(JSONObject.Type.OBJECT);
        mesh2DComp.AddField("_$type", "Mesh2DRender");
        mesh2DComp.AddField("useUnitQuad", true);

        // texture reference
        // Full-texture mode: _$uuid is a texture UUID → engine resolves via "res://uuid"
        // Atlas mode: _$uuid is "atlasUUID@spriteName" → engine's isUUID returns true,
        // resolves to "res://atlasUUID@spriteName".
        JSONObject texRef = new JSONObject(JSONObject.Type.OBJECT);
        texRef.AddField("_$uuid", textureRef);
        // Full-texture: Texture2D (raw texture asset); Atlas sub-texture: Texture (atlas切出的小图)
        texRef.AddField("_$type", atlasUUID != null ? "Texture" : "Texture2D");
        mesh2DComp.AddField("texture", texRef);

        // color
        JSONObject colorObj = new JSONObject(JSONObject.Type.OBJECT);
        colorObj.AddField("_$type", "Color");
        colorObj.AddField("r", spriteColor.r);
        colorObj.AddField("g", spriteColor.g);
        colorObj.AddField("b", spriteColor.b);
        colorObj.AddField("a", spriteColor.a);
        mesh2DComp.AddField("color", colorObj);

        // size
        JSONObject sizeObj = new JSONObject(JSONObject.Type.OBJECT);
        sizeObj.AddField("_$type", "Vector2");
        sizeObj.AddField("x", (float)pixelWidth);
        sizeObj.AddField("y", (float)pixelHeight);
        mesh2DComp.AddField("size", sizeObj);

        // sharedMaterial (baseRender2D default material)
        if (!string.IsNullOrEmpty(materialUUID))
        {
            JSONObject matRef = new JSONObject(JSONObject.Type.OBJECT);
            matRef.AddField("_$uuid", materialUUID);
            matRef.AddField("_$type", "Material");
            mesh2DComp.AddField("sharedMaterial", matRef);
        }

        compArray.Add(mesh2DComp);

        // Animator2D component (if animation data provided)
        if (animationData != null)
        {
            compArray.Add(animationData);
        }

        root.AddField("_$comp", compArray);

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
