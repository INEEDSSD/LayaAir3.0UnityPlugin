using System.Collections.Generic;

/// <summary>
/// Generates a default baseRender2D material (.lmat) for Mesh2DRender components.
/// Mirrors the engine-side Mesh2DRender.mesh2DDefaultMaterial configuration.
/// </summary>
internal class Mesh2DDefaultMaterialFile : JsonFile
{
    private static readonly string VIRTUAL_PATH = "Assets/Mesh2DRender_DefaultMaterial.lmat";

    public Mesh2DDefaultMaterialFile() : base(VIRTUAL_PATH, CreateMaterialJson())
    {
    }

    private static JSONObject CreateMaterialJson()
    {
        JSONObject jsonData = new JSONObject(JSONObject.Type.OBJECT);
        jsonData.AddField("version", "LAYAMATERIAL:04");

        JSONObject props = new JSONObject(JSONObject.Type.OBJECT);
        jsonData.AddField("props", props);

        JSONObject textures = new JSONObject(JSONObject.Type.ARRAY);
        props.AddField("textures", textures);

        props.AddField("type", "baseRender2D");

        // Sync with reference Mesh2DRender_DefaultMaterial.lmat
        props.AddField("renderQueue", 3000);
        props.AddField("materialRenderMode", 5);
        props.AddField("s_Cull", 2);           // RenderState.CULL_BACK
        props.AddField("s_Blend", 1);          // RenderState.BLEND_ENABLE_ALL
        props.AddField("s_BlendSrc", 1);       // RenderState.BLENDPARAM_ONE
        props.AddField("s_BlendDst", 7);       // RenderState.BLENDPARAM_ONE_MINUS_SRC_ALPHA
        props.AddField("s_BlendEquation", 0);  // RenderState.BLENDEQUATION_ADD
        props.AddField("s_DepthTest", 1);      // RenderState.DEPTHTEST_LESS
        props.AddField("s_DepthWrite", false);

        JSONObject defines = new JSONObject(JSONObject.Type.ARRAY);
        defines.Add("BASERENDER2D");
        props.AddField("defines", defines);

        return jsonData;
    }
}
