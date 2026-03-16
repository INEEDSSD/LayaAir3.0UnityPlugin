
using System.Collections.Generic;
using System.IO;
using UnityEngine;

internal class MeshFile : FileData
{
    private Mesh m_mesh;
    private Renderer render;

    public MeshFile(Mesh mesh,Renderer render) :base(null)
    {
        string path = AssetsUtil.GetMeshPath(mesh);
        this.updatePath(path);
        this.m_mesh = mesh;
        this.render = render;
    }

    /// <summary>
    /// 构造函数（支持指定自定义路径）
    /// 用于程序生成的mesh（如简化后的粒子mesh），需要使用原始mesh的路径
    /// </summary>
    public MeshFile(Mesh mesh, Renderer render, string customPath) :base(null)
    {
        this.updatePath(customPath);
        this.m_mesh = mesh;
        this.render = render;
    }

    public override void SaveFile(Dictionary<string, FileData> exportFiles)
    {
        if (this.m_mesh.uv2.Length > 0 && ExportConfig.AutoVerticesUV1)
        {
            JSONObject autouv1 = new JSONObject(JSONObject.Type.OBJECT);
            autouv1.AddField("generateLightmapUVs", true);
            this.metaData().AddField("importer", autouv1);
        }
        base.saveMeta();
        FileStream fs = Util.FileUtil.saveFile(this.outPath);
        string meshName = GameObjectUitls.cleanIllegalChar(this.m_mesh.name, true);
        if (this.render!=null&&this.render is SkinnedMeshRenderer)
        {
            MeshUitls.writeSkinnerMesh(this.render as SkinnedMeshRenderer, meshName, fs);
        }
        else
        {
            MeshUitls.writeMesh(this.m_mesh, meshName, fs);
        }
    }


   
}
