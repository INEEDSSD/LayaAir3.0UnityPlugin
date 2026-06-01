using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

internal class JsonUtils
{
    // CPU 粒子组件类型名 → Laya 引擎 UUID 映射表 (来自 cupparticle 分支)
    private static Dictionary<string, string> registclassMap;

    static JsonUtils()
    {
        registclassMap = new Dictionary<string, string>();
        registclassMap.Add("ParticleSystem", "92eb166a-5945-4936-b7d7-b276c49a7a5d");
        registclassMap.Add("ParticleSystemRenderer", "75e22e26-8d5d-4d92-b088-231b22ce3c41");
        registclassMap.Add("MinMaxCurve", "b87b8fac-9e9e-4208-b771-9af1d197faff");
        registclassMap.Add("AnimationCurve", "98c3ef8f-a969-4a69-a9a2-aaa279c0af76");
        registclassMap.Add("CurveKeyframe", "e841b213-e04b-4d71-91d8-1f3da4c4d90b");
        registclassMap.Add("MinMaxGradient", "ea294660-23b9-4d09-957b-e522a1044d69");
        registclassMap.Add("PlusBurst", "6d8f5863-a67e-4bf7-a234-4c72ccfdced6");
        registclassMap.Add("PlusShape", "2dc73014-8f75-44a1-a180-7328a15ef786");
        registclassMap.Add("PlusSubmitterData", "83f308c7-3aa9-4337-a9cd-810e2a7b8239");
        registclassMap.Add("MainModule", "4e51be3f-6ce0-467d-badf-2c601f3c1940");
        registclassMap.Add("PlusEmission", "4f5e56f1-f217-45be-a510-e6b7a9f9501e");
        registclassMap.Add("PlusVelocityOverLife", "7c667757-559d-4b21-b3c8-7adb4f4d6487");
        registclassMap.Add("PlusSizeOverLife", "02fec0b5-fcd0-42e3-b708-1c6ea2c8a254");
        registclassMap.Add("PlusForceOverLife", "8fc8f82c-cd7b-4218-b2e6-800f15d9d091");
        registclassMap.Add("PlusRotationOverLife", "e5d4f071-b7e2-4ef5-8381-ed372fa1a022");
        registclassMap.Add("PlusLimtVelocityOverLife", "d20fef97-69e8-4368-bf9c-1fc6a7d4aea4");
        registclassMap.Add("PlusColorOverLife", "ad3ae3e8-1dd4-49db-bbce-58b7ac9f4969");
        registclassMap.Add("PlusColorBySpeed", "71b74bb3-558b-4dca-97cc-d444781d3caf");
        registclassMap.Add("PlusSizeBySpeed", "cdd7f2a1-0ba0-415d-a9c5-22406b15e2c1");
        registclassMap.Add("PlusRotationBySpeed", "172e8ace-f8e2-4df1-b5c2-d50fe17f2740");
        registclassMap.Add("PlusInheritVelocity", "343fc527-709b-4e99-a00f-71fcec2e77e0");
        registclassMap.Add("PlusNoise", "c79e0ae7-5cec-4e78-a41b-eafaacc52845");
        registclassMap.Add("PlusTextureSheetAnimation", "6d5e6217-7a72-4955-859e-148df1efeb8e");
        registclassMap.Add("PlusSubEmitters", "5fa562e6-998f-446c-b0ba-201a2cfa57f9");
        registclassMap.Add("PlusLifetimeByEmitterSpeed", "ea138eaa-6321-49a1-bd7d-fd8575cc11ee");
        registclassMap.Add("PlusExternalForces", "52d807f4-d341-4a01-ad93-0133ae4e0be6");
        registclassMap.Add("PlusCollision", "911a7d8f-ee26-415d-b402-7188e1bb87c0");
        registclassMap.Add("ParticleSystemForceField", "5cb29556-5d65-416d-b203-f7126bad146b");
        registclassMap.Add("PlusTrails", "c1c63215-5155-415b-9847-61741dc62b6c");
        registclassMap.Add("MeshItem", "17726b3d-5c87-416c-a078-c29b86064394");
        registclassMap.Add("PlusBoxShape", "cfdbb0bc-27ab-4d63-8b94-cdcc24a97d2f");
        registclassMap.Add("PlusSphereShape", "6c5f9470-d0c6-4dc4-8df6-c126bec7424d");
        registclassMap.Add("PlusHemisphereShape", "2d79ad98-bdc6-44e3-823b-8d1a87670741");
        registclassMap.Add("PlusConeShape", "01678383-2307-47a0-8c9d-cfaad5ad20ab");
        registclassMap.Add("PlusCircleShape", "447881cb-4443-43e9-99c3-027b5488a6f8");
        registclassMap.Add("PlusDountShape", "6f45c7c2-5327-45d2-be33-c3f1faeb9f8f");
        registclassMap.Add("PlusSideEdgeShape", "3c6ce9b5-f8cf-4e60-8cb9-dbb1e8acac1f");
        registclassMap.Add("PlusMeshShape", "af66f3c5-08f8-4999-bb2f-2f0044e76150");
        registclassMap.Add("PlusMeshRenderShape", "ba49942d-7aff-46dd-9ddb-e33e8da4ea01");
        registclassMap.Add("PlusRectangleShape", "f8006022-8285-4c9e-b193-00f22e704396");
    }

    public static JSONObject GetColorObject(Color color)
    {
        JSONObject colorData = new JSONObject(JSONObject.Type.OBJECT);
        colorData.AddField("_$type", "Color");
        colorData.AddField("r", color.r);
        colorData.AddField("g", color.g);
        colorData.AddField("b", color.b);
        colorData.AddField("a", color.a);
        return colorData;
    }
    public static JSONObject GetVector3Object(Vector3 value)
    {
        JSONObject postionData = new JSONObject(JSONObject.Type.OBJECT);
        postionData.AddField("_$type", "Vector3");
        postionData.AddField("x", value.x);
        postionData.AddField("y", value.y);
        postionData.AddField("z", value.z);
        return postionData;
    }

    public static JSONObject GetQuaternionObject(Quaternion quaternion)
    {
        JSONObject postionData = new JSONObject(JSONObject.Type.OBJECT);
        postionData.AddField("_$type", "Quaternion");
        postionData.AddField("x", quaternion.x);
        postionData.AddField("y", quaternion.y);
        postionData.AddField("z", quaternion.z);
        postionData.AddField("w", quaternion.w);
        return postionData;
    }
    public static JSONObject GetTransfrom(GameObject gObject)
    {
        JSONObject transfrom = new JSONObject(JSONObject.Type.OBJECT);
        Vector3 position = gObject.transform.localPosition;

        // 检查父节点是否是相机/灯光（它们有额外的Y180旋转需要补偿）
        bool parentIsCameraOrLight = gObject.transform.parent != null &&
            GameObjectUitls.isCameraOrLight(gObject.transform.parent.gameObject);

        if (parentIsCameraOrLight)
        {
            // 父级相机/灯光有额外Y180旋转，子节点取反Z而非X
            SpaceUtils.changePostionForCameraChild(ref position);
        }
        else
        {
            SpaceUtils.changePostion(ref position);
        }
        transfrom.AddField("localPosition", GetVector3Object(position));

        Quaternion rotation = gObject.transform.localRotation;
        bool isRotate = GameObjectUitls.isCameraOrLight(gObject);
        SpaceUtils.changeRotate(ref rotation, isRotate);
        if (parentIsCameraOrLight)
        {
            // 左乘Y180补偿父级的额外旋转
            SpaceUtils.compensateCameraParentRotation(ref rotation);
        }
        transfrom.AddField("localRotation", GetQuaternionObject(rotation));
        transfrom.AddField("localScale", GetVector3Object(gObject.transform.localScale));
        return transfrom;
    }

    /// <summary>
    /// 构建 2D UI Image 节点 JSON（不含 3D transform，使用 RectTransform 的 2D 坐标）
    /// </summary>
    public static JSONObject GetImageNode(GameObject go)
    {
        JSONObject node = new JSONObject(JSONObject.Type.OBJECT);
        node.AddField("name", go.name);
        node.AddField("active", go.activeSelf);

        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt != null)
        {
            node.AddField("x", rt.anchoredPosition.x);
            node.AddField("y", -rt.anchoredPosition.y); // Unity Y↑ → Laya Y↓
            node.AddField("width", rt.sizeDelta.x);
            node.AddField("height", rt.sizeDelta.y);
        }
        return node;
    }

    public static JSONObject GetGameObject(GameObject gObject,bool isperfabRoot = false, JSONObject nodeData = null)
    {
        if(nodeData == null)
        {
            nodeData = new JSONObject(JSONObject.Type.OBJECT);
        }
        if (isperfabRoot)
        {
            nodeData.AddField("_$ver", 1);
        }
        nodeData.AddField("name", gObject.name);
        nodeData.AddField("active", gObject.activeSelf);
        StaticEditorFlags staticEditorFlags = GameObjectUtility.GetStaticEditorFlags(gObject);
        nodeData.AddField("isStatic", ((int)staticEditorFlags & (int)StaticEditorFlags.BatchingStatic) > 0);
        nodeData.AddField("layer", gObject.layer);
        if (!gObject.tag.Equals("Untagged")) {
            nodeData.AddField("tag", gObject.tag);
        }
        nodeData.AddField("transform", JsonUtils.GetTransfrom(gObject));
        return nodeData;
    }
    public static JSONObject GetDirectionalLightComponentData(Light light, bool isOverride)
    {
        JSONObject lightData = JsonUtils.SetComponentsType(new JSONObject(JSONObject.Type.OBJECT), "DirectionLightCom",isOverride);
        SetLightData(light, lightData);
        return lightData;
    }

    public static JSONObject GetPointLightComponentData(Light light, bool isOverride)
    {
        JSONObject lightData = JsonUtils.SetComponentsType(new JSONObject(JSONObject.Type.OBJECT), "PointLightCom", isOverride);
        SetLightData(light, lightData);
        lightData.AddField("range", light.range);

        return lightData;
    }

    public static JSONObject GetSpotLightComponentData(Light light, bool isOverride)
    {
        JSONObject lightData = JsonUtils.SetComponentsType(new JSONObject(JSONObject.Type.OBJECT), "SpotLightCom", isOverride);
        SetLightData(light, lightData);
        lightData.AddField("range", light.range);
        lightData.AddField("spotAngle", light.spotAngle);

        return lightData;
    }

    private static void SetLightData(Light light, JSONObject lightData)
    {
        lightData.AddField("intensity", light.intensity);
        switch (light.lightmapBakeType)
        {
            case LightmapBakeType.Realtime:
                lightData.AddField("lightmapBakedType", 1);
                break;
            case LightmapBakeType.Mixed:
                lightData.AddField("lightmapBakedType", 0);
                break;
            case LightmapBakeType.Baked:
                lightData.AddField("lightmapBakedType", 2);
                break;
            default:
                lightData.AddField("lightmapBakedType", 1);
                break;
        }
        lightData.AddField("color", GetColorObject(light.color));
        switch (light.shadows)
        {
            case LightShadows.Hard:
                lightData.AddField("shadowMode", 1);
                break;
            case LightShadows.Soft:
                lightData.AddField("shadowMode", 2);
                break;
            default:
                lightData.AddField("shadowMode", 0);
                break;
        }
        lightData.AddField("shadowStrength", light.shadowStrength);
        lightData.AddField("shadowDepthBias", light.shadowBias + 1);
        lightData.AddField("shadowNormalBias", light.shadowNormalBias + 0.6f);
        lightData.AddField("shadowNearPlane", light.shadowNearPlane);
    }

    public static JSONObject GetVector2Object(Vector2 value)
    {
        JSONObject data = new JSONObject(JSONObject.Type.OBJECT);
        data.AddField("_$type", "Vector2");
        data.AddField("x", value.x);
        data.AddField("y", value.y);
        return data;
    }

    public static JSONObject GetVector2Object(float x, float y)
    {
        JSONObject data = new JSONObject(JSONObject.Type.OBJECT);
        data.AddField("_$type", "Vector2");
        data.AddField("x", x);
        data.AddField("y", y);
        return data;
    }

    public static JSONObject SetComponentsType(JSONObject compData, string componentsname)
    {
        if (registclassMap.ContainsKey(componentsname))
        {
            componentsname = registclassMap[componentsname];
        }
        compData.AddField("_$type", componentsname);
        return compData;
    }

    public static JSONObject SetComponentsType(JSONObject compData, string componentsname, bool isOverride)
    {
        if (registclassMap.ContainsKey(componentsname))
        {
            componentsname = registclassMap[componentsname];
        }
        if (isOverride)
        {
            compData.AddField("_$override", componentsname);
        }
        else
        {
            compData.AddField("_$type", componentsname);
        }
        return compData;
    }

    public static void getCameraComponentData(GameObject gameObject, JSONObject props)
    {
        Camera camera = gameObject.GetComponent<Camera>();

        try
        {

        if (camera.clearFlags == CameraClearFlags.Skybox)
        {
            props.AddField("clearFlag", 1);
        }
        else if (camera.clearFlags == CameraClearFlags.SolidColor || camera.clearFlags == CameraClearFlags.Color)
        {
            props.AddField("clearFlag", 0);
        }
        else if (camera.clearFlags == CameraClearFlags.Depth)
        {
            props.AddField("clearFlag", 2);
        }
        else
        {
            props.AddField("clearFlag", 3);
        }

        props.AddField("orthographic", camera.orthographic);
        props.AddField("orthographicVerticalSize", camera.orthographicSize * 2);

        // FOV适配：Unity和LayaAir的aspect可能不同
        // Unity: 使用Game窗口的实际aspect
        // LayaAir: 使用canvas的实际aspect（保高模式下会动态变化）
        //
        // 如果aspect不同，需要调整FOV以保持相同的视野范围
        // 策略：保持视野的"高度"一致（在相同距离下，顶部和底部的位置相同）
        //
        // 计算公式：
        // 垂直视野高度 = 2 * distance * tan(vFOV/2)
        // 水平视野宽度 = 垂直视野高度 * aspect
        //
        // 如果Unity aspect ≠ LayaAir aspect，有两种策略：
        // 1. 保持垂直FOV不变（当前默认）→ 水平视野会不同
        // 2. 调整垂直FOV使水平视野保持一致
        //
        // 用户反馈"底部被裁剪"，说明需要更大的垂直FOV
        // 这里提供一个可选的调整系数

        // FOV自适应调整（安全版本，如果计算失败则使用原始FOV）
        float adjustedFOV = camera.fieldOfView;

        try
        {
            float unityAspect = camera.aspect;
            float targetAspect = 1334.0f / 750.0f;  // LayaAir设计分辨率aspect

            // 如果Unity窗口比LayaAir目标更窄（aspect更小），增加FOV以补偿
            if (unityAspect < targetAspect && unityAspect > 0 && targetAspect > 0)
            {
                // 计算水平FOV（角度）
                float unityHFOV = 2.0f * Mathf.Atan(Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f) * unityAspect) * Mathf.Rad2Deg;
                // 从水平FOV反推LayaAir的垂直FOV
                float newFOV = 2.0f * Mathf.Atan(Mathf.Tan(unityHFOV * Mathf.Deg2Rad * 0.5f) / targetAspect) * Mathf.Rad2Deg;

                // 验证计算结果合理性
                if (!float.IsNaN(newFOV) && !float.IsInfinity(newFOV) && newFOV > 0 && newFOV < 180)
                {
                    adjustedFOV = newFOV;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[LayaAir Export] FOV adjustment failed for camera '{camera.gameObject.name}', using original FOV: {e.Message}");
        }

        props.AddField("fieldOfView", adjustedFOV);
        props.AddField("enableHDR", camera.allowHDR);
        props.AddField("nearPlane", camera.nearClipPlane);
        props.AddField("farPlane", camera.farClipPlane);

        JSONObject viewPort = new JSONObject(JSONObject.Type.OBJECT);
        viewPort.AddField("_$type", "Viewport");
        Rect rect = camera.rect;
        viewPort.AddField("x", rect.x);
        viewPort.AddField("y", 1.0f - rect.y - rect.height);
        viewPort.AddField("width", rect.width);
        viewPort.AddField("height", rect.height);
        props.AddField("normalizedViewport", viewPort);

        props.AddField("clearColor", GetColorObject(camera.backgroundColor));

        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LayaAir Export] Error exporting camera '{gameObject.name}':\n{e.ToString()}");
            throw;
        }
    }
}
