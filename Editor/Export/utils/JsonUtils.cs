using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

internal class JsonUtils 
{
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

    public static JSONObject SetComponentsType(JSONObject compData, string componentsname, bool isOverride)
    {
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
