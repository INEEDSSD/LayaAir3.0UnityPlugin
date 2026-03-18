using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// A 2D animator controller file (.mcc) containing the full state machine data
/// (layers, states, transitions, parameters) for Animator2D.
/// Generated during SpriteRenderer color animation export.
/// Only states with SpriteRenderer m_Color curves get .mc clip references;
/// states without color curves are included to preserve state machine structure.
/// </summary>
internal class AnimatorController2DFile : FileData
{
    private JSONObject m_data;

    /// <param name="targetPath">binding.path filter — only include clips with color curves matching this path</param>
    public AnimatorController2DFile(string virtualPath, AnimatorController controller,
                                     GameObject gameObject, ResoureMap resoureMap,
                                     string targetPath = "")
        : base(virtualPath)
    {
        m_data = BuildControllerData(controller, gameObject, resoureMap, targetPath);
    }

    protected override string getOutFilePath(string path)
    {
        return path;
    }

    private JSONObject BuildControllerData(AnimatorController controller,
                                            GameObject gameObject, ResoureMap resoureMap,
                                            string targetPath = "")
    {
        JSONObject root = new JSONObject(JSONObject.Type.OBJECT);

        // controllerLayers
        JSONObject layersArray = new JSONObject(JSONObject.Type.ARRAY);
        AnimatorControllerLayer[] layers = controller.layers;
        for (int i = 0; i < layers.Length; i++)
        {
            layersArray.Add(BuildLayerData(layers[i], controller, gameObject, resoureMap, i == 0, targetPath));
        }
        root.AddField("controllerLayers", layersArray);

        // animatorParams
        AnimatorControllerParameter[] parameters = controller.parameters;
        if (parameters.Length > 0)
        {
            JSONObject paramsArray = new JSONObject(JSONObject.Type.ARRAY);
            for (int i = 0; i < parameters.Length; i++)
            {
                JSONObject param = new JSONObject(JSONObject.Type.OBJECT);
                param.AddField("id", i);
                param.AddField("name", parameters[i].name);

                // Map Unity types to engine AniParmType: Float=0, Bool=1, Trigger=2
                int paramType;
                switch (parameters[i].type)
                {
                    case AnimatorControllerParameterType.Float:
                        paramType = 0;
                        param.AddField("val", parameters[i].defaultFloat);
                        break;
                    case AnimatorControllerParameterType.Int:
                        paramType = 0; // Map int to float in engine
                        param.AddField("val", (float)parameters[i].defaultInt);
                        break;
                    case AnimatorControllerParameterType.Bool:
                        paramType = 1;
                        param.AddField("val", parameters[i].defaultBool);
                        break;
                    case AnimatorControllerParameterType.Trigger:
                        paramType = 2;
                        param.AddField("val", false);
                        break;
                    default:
                        paramType = 0;
                        param.AddField("val", 0);
                        break;
                }
                param.AddField("type", paramType);
                paramsArray.Add(param);
            }
            root.AddField("animatorParams", paramsArray);
        }

        return root;
    }

    private JSONObject BuildLayerData(AnimatorControllerLayer layer, AnimatorController controller,
                                       GameObject gameObject, ResoureMap resoureMap, bool isBaseLayer,
                                       string targetPath = "")
    {
        JSONObject layerNode = new JSONObject(JSONObject.Type.OBJECT);
        layerNode.AddField("name", layer.name);
        layerNode.AddField("blendingMode",
            layer.blendingMode == AnimatorLayerBlendingMode.Additive ? 1 : 0);
        layerNode.AddField("playOnWake", true);
        layerNode.AddField("defaultWeight", isBaseLayer ? 1f : layer.defaultWeight);

        AnimatorStateMachine stateMachine = layer.stateMachine;
        ChildAnimatorState[] states = stateMachine.states;
        JSONObject statesArray = new JSONObject(JSONObject.Type.ARRAY);
        layerNode.AddField("states", statesArray);

        // Build state name → id map
        Dictionary<string, int> stateMap = new Dictionary<string, int>();
        for (int i = 0; i < states.Length; i++)
        {
            stateMap[states[i].state.name] = i;
        }

        // Build parameter name → index map (for conditions)
        Dictionary<string, int> paramMap = new Dictionary<string, int>();
        AnimatorControllerParameter[] parameters = controller.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            paramMap[parameters[i].name] = i;
        }

        // Export each state
        for (int i = 0; i < states.Length; i++)
        {
            Vector3 position = states[i].position;
            AnimatorState state = states[i].state;

            JSONObject stateNode = new JSONObject(JSONObject.Type.OBJECT);
            statesArray.Add(stateNode);
            stateNode.AddField("id", i.ToString());
            stateNode.AddField("name", state.name);
            stateNode.AddField("speed", state.speed);
            stateNode.AddField("clipStart", 0);
            stateNode.AddField("clipEnd", 1);
            stateNode.AddField("x", position.x);
            stateNode.AddField("y", position.y);

            AnimationClip clip = state.motion as AnimationClip;
            if (clip != null)
            {
                stateNode.AddField("loop", clip.isLooping ? -1 : 0);

                // Only export .mc for clips with SpriteRenderer color curves matching targetPath
                if (HasSpriteColorCurves(clip, targetPath))
                {
                    string clipName = GameObjectUitls.cleanIllegalChar(clip.name, true);
                    string clipVirtualPath = this.filePath.Replace(".mcc", "_" + clipName + ".mc");

                    if (!resoureMap.HaveFileData(clipVirtualPath))
                    {
                        resoureMap.AddExportFile(new AnimClip2DFile(clipVirtualPath, clip, gameObject, resoureMap, targetPath));
                    }
                    AnimClip2DFile clipFile = resoureMap.GetFileData(clipVirtualPath) as AnimClip2DFile;
                    if (clipFile != null)
                    {
                        JSONObject clipRef = new JSONObject(JSONObject.Type.OBJECT);
                        clipRef.AddField("_$uuid", clipFile.uuid);
                        clipRef.AddField("_$type", "AnimationClip2D");
                        stateNode.AddField("clip", clipRef);
                    }
                }
            }

            // Transitions
            AnimatorStateTransition[] transitions = state.transitions;
            if (transitions.Length > 0)
            {
                JSONObject transArray = new JSONObject(JSONObject.Type.ARRAY);
                stateNode.AddField("soloTransitions", transArray);
                for (int j = 0; j < transitions.Length; j++)
                {
                    AnimatorStateTransition transition = transitions[j];
                    if (transition.destinationState == null)
                    {
                        Debug.LogWarning($"[LayaAir Export 2D] State '{state.name}' has a transition with null destinationState, skipping");
                        continue;
                    }
                    if (!stateMap.ContainsKey(transition.destinationState.name))
                    {
                        Debug.LogWarning($"[LayaAir Export 2D] State '{state.name}' has a transition to unknown state '{transition.destinationState.name}', skipping");
                        continue;
                    }

                    JSONObject transNode = new JSONObject(JSONObject.Type.OBJECT);
                    transArray.Add(transNode);
                    transNode.AddField("id", stateMap[transition.destinationState.name].ToString());
                    transNode.AddField("exitByTime", transition.hasExitTime);
                    transNode.AddField("exitTime", transition.exitTime);
                    transNode.AddField("transduration", transition.duration);
                    transNode.AddField("transstartoffset", transition.offset);
                    if (transition.solo) transNode.AddField("solo", true);
                    if (transition.mute) transNode.AddField("mute", true);

                    // Conditions
                    if (transition.conditions.Length > 0)
                    {
                        JSONObject condsArray = BuildConditions(transition.conditions, paramMap, parameters);
                        if (condsArray != null && condsArray.Count > 0)
                            transNode.AddField("conditions", condsArray);
                    }
                }
            }
        }

        // Entry state (id = -1)
        Vector3 entryPos = stateMachine.entryPosition;
        JSONObject entryNode = new JSONObject(JSONObject.Type.OBJECT);
        statesArray.Add(entryNode);
        entryNode.AddField("id", "-1");
        entryNode.AddField("name", "entry");
        entryNode.AddField("speed", 1);
        entryNode.AddField("clipEnd", 1);
        entryNode.AddField("x", entryPos.x);
        entryNode.AddField("y", entryPos.y);

        JSONObject entrySoloTransitions = new JSONObject(JSONObject.Type.ARRAY);
        if (stateMachine.entryTransitions.Length > 0)
        {
            for (int j = 0; j < stateMachine.entryTransitions.Length; j++)
            {
                AnimatorTransition transition = stateMachine.entryTransitions[j];
                if (transition.destinationState == null)
                {
                    Debug.LogWarning("[LayaAir Export 2D] Entry transition has null destinationState, skipping");
                    continue;
                }
                if (!stateMap.ContainsKey(transition.destinationState.name))
                {
                    Debug.LogWarning($"[LayaAir Export 2D] Entry transition points to unknown state '{transition.destinationState.name}', skipping");
                    continue;
                }
                JSONObject soloTransition = new JSONObject(JSONObject.Type.OBJECT);
                soloTransition.AddField("id", stateMap[transition.destinationState.name].ToString());
                entrySoloTransitions.Add(soloTransition);
            }
        }
        else if (stateMachine.defaultState != null && stateMap.ContainsKey(stateMachine.defaultState.name))
        {
            JSONObject soloTransition = new JSONObject(JSONObject.Type.OBJECT);
            soloTransition.AddField("id", stateMap[stateMachine.defaultState.name].ToString());
            entrySoloTransitions.Add(soloTransition);
        }
        entryNode.AddField("soloTransitions", entrySoloTransitions);

        // AnyState (id = -2)
        Vector3 anyPos = stateMachine.anyStatePosition;
        JSONObject anyNode = new JSONObject(JSONObject.Type.OBJECT);
        statesArray.Add(anyNode);
        anyNode.AddField("id", "-2");
        anyNode.AddField("name", "anyState");
        anyNode.AddField("speed", 1);
        anyNode.AddField("clipEnd", 1);
        anyNode.AddField("x", anyPos.x);
        anyNode.AddField("y", anyPos.y);

        if (stateMachine.anyStateTransitions.Length > 0)
        {
            JSONObject anySoloTransitions = new JSONObject(JSONObject.Type.ARRAY);
            for (int j = 0; j < stateMachine.anyStateTransitions.Length; j++)
            {
                AnimatorStateTransition anyTransition = stateMachine.anyStateTransitions[j];
                if (anyTransition.destinationState == null) continue;
                if (!stateMap.ContainsKey(anyTransition.destinationState.name)) continue;

                JSONObject soloTransition = new JSONObject(JSONObject.Type.OBJECT);
                soloTransition.AddField("id", stateMap[anyTransition.destinationState.name].ToString());
                soloTransition.AddField("exitByTime", anyTransition.hasExitTime);
                soloTransition.AddField("exitTime", anyTransition.exitTime);
                soloTransition.AddField("transduration", anyTransition.duration);
                anySoloTransitions.Add(soloTransition);

                // Conditions for anyState transitions
                if (anyTransition.conditions.Length > 0)
                {
                    JSONObject condsArray = BuildConditions(anyTransition.conditions, paramMap, parameters);
                    if (condsArray != null && condsArray.Count > 0)
                        soloTransition.AddField("conditions", condsArray);
                }
            }
            anyNode.AddField("soloTransitions", anySoloTransitions);
        }

        return layerNode;
    }

    /// <summary>
    /// Build conditions array for a transition.
    /// Maps Unity AnimatorCondition to engine TypeAnimatorConditions format.
    /// </summary>
    private static JSONObject BuildConditions(AnimatorCondition[] conditions,
                                               Dictionary<string, int> paramMap,
                                               AnimatorControllerParameter[] parameters)
    {
        JSONObject condsArray = new JSONObject(JSONObject.Type.ARRAY);
        foreach (AnimatorCondition condition in conditions)
        {
            if (!paramMap.ContainsKey(condition.parameter))
            {
                Debug.LogWarning($"[LayaAir Export 2D] Condition references unknown parameter '{condition.parameter}', skipping");
                continue;
            }

            int paramIndex = paramMap[condition.parameter];
            AnimatorControllerParameter param = parameters[paramIndex];

            JSONObject condNode = new JSONObject(JSONObject.Type.OBJECT);
            condNode.AddField("id", paramIndex);

            if (param.type == AnimatorControllerParameterType.Float ||
                param.type == AnimatorControllerParameterType.Int)
            {
                // AniStateConditionNumberCompressType: Less=0, Greater=1
                int condType = condition.mode == AnimatorConditionMode.Less ? 0 : 1;
                condNode.AddField("type", condType);
                condNode.AddField("checkValue", condition.threshold);
            }
            else if (param.type == AnimatorControllerParameterType.Bool)
            {
                condNode.AddField("checkValue", condition.mode == AnimatorConditionMode.If);
            }
            else if (param.type == AnimatorControllerParameterType.Trigger)
            {
                condNode.AddField("checkValue", condition.mode == AnimatorConditionMode.If);
            }

            condsArray.Add(condNode);
        }
        return condsArray;
    }

    /// <summary>
    /// Check if an AnimationClip has SpriteRenderer m_Color curves matching targetPath.
    /// </summary>
    internal static bool HasSpriteColorCurves(AnimationClip clip, string targetPath = "")
    {
        EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
        foreach (EditorCurveBinding binding in bindings)
        {
            if (binding.type == typeof(SpriteRenderer)
                && binding.propertyName.StartsWith("m_Color")
                && binding.path == targetPath)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Check if any clip in the controller has SpriteRenderer m_Color curves matching targetPath.
    /// </summary>
    internal static bool HasAnySpriteColorCurves(AnimatorController controller, string targetPath = "")
    {
        foreach (AnimationClip clip in controller.animationClips)
        {
            if (HasSpriteColorCurves(clip, targetPath))
                return true;
        }
        return false;
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
