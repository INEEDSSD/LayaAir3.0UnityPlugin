using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEditorInternal;
using UnityEngine;


[CustomEditor(typeof(ParticleSystem))]
[CanEditMultipleObjects]
internal class LayaParticleSystemEditor : LayaCustomInspector
{
    enum InspectorType
    {
        UnityInspector,
        LayaAirInspector
    }

    private static InspectorType inspectorType = InspectorType.UnityInspector;

    public LayaParticleSystemEditor() : base("ParticleSystemInspector")
    {
        InspectorAnimator();
    }

    public override void OnInspectorGUI()
    {
        IEnumerable<ParticleSystem> systems = from p in targets.OfType<ParticleSystem>() where (p != null) select p;

        ParticleSystem[] allSystems = systems.ToArray();
        bool usingMultiEdit = (allSystems.Length > 1);

        EditorGUILayout.BeginHorizontal();

        GUILayout.FlexibleSpace();

        inspectorType = (InspectorType)EditorGUILayout.EnumPopup(inspectorType);
        
        EditorGUILayout.EndHorizontal();

        switch (inspectorType)
        {
            case InspectorType.UnityInspector:
                base.OnInspectorGUI();
                break;
            case InspectorType.LayaAirInspector:
                OnLayaInspectorGUI();
                break;
            default:
                base.OnInspectorGUI();
                break;
        }
    }

    private void OnLayaInspectorGUI()
    {
        serializedObject.Update();

        MainModulePropInit();
        DoAnimat(main, ref showmain, "Main");
        if (EditorGUILayout.BeginFadeGroup(main.faded))
            MainModule();
        EditorGUILayout.EndFadeGroup();

        EmissionModulePropInit();
        DoAnimat(Emission, ref showEmission, "Emission");
        if (EditorGUILayout.BeginFadeGroup(Emission.faded))
            EmissionModule();
        EditorGUILayout.EndFadeGroup();

        ShapeModulePropInit();
        DoAnimat(LayaShape, ref showLayaShape, "Shape");
        if (EditorGUILayout.BeginFadeGroup(LayaShape.faded))
            ShapeModule();
        EditorGUILayout.EndFadeGroup();

        VelocityModulePropInit();
        DoAnimat(VelocityOverLifeTime, ref showVelocityOverLifeTime, "Velocity over Lifetime");
        if (EditorGUILayout.BeginFadeGroup(VelocityOverLifeTime.faded))
            VelocityModule();
        EditorGUILayout.EndFadeGroup();

        ColorModulePropInit();
        DoAnimat(ColorOverLifeTime, ref showColorOverLifeTime, "Color over Lifetime");
        if (EditorGUILayout.BeginFadeGroup(ColorOverLifeTime.faded))
            ColorModule();
        EditorGUILayout.EndFadeGroup();

        SizeModulePropInit();
        DoAnimat(SizeOverLifeTime, ref showSizeOverLifeTime, "Size over Lifetime");
        if (EditorGUILayout.BeginFadeGroup(SizeOverLifeTime.faded))
            SizeModule();
        EditorGUILayout.EndFadeGroup();

        RotationModulePropInit();
        DoAnimat(RotationOverLifeTime, ref showRotationOverLifeTime, "Rotation over Lifetime");
        if (EditorGUILayout.BeginFadeGroup(RotationOverLifeTime.faded))
            RotationModule();
        EditorGUILayout.EndFadeGroup();

        UVModulePropInit();
        DoAnimat(TextureSheetAnimation, ref showTextureSheetAnimation, "Texture Sheet Animation");
        if (EditorGUILayout.BeginFadeGroup(TextureSheetAnimation.faded))
            UVModule();
        EditorGUILayout.EndFadeGroup();

        RendererModulePropInit();
        DoAnimat(Render, ref showRender, "Renderer");
        if (EditorGUILayout.BeginFadeGroup(Render.faded))
            RendererModule();
        EditorGUILayout.EndFadeGroup();

        serializedObject.ApplyModifiedProperties();
    }


    #region Main Module 
    // LayaAir 3.0 支持的Main模块属性:
    // duration, looping, playOnAwake
    // startDelayType (Constant=0, TwoConstants=1), startDelay, startDelayMin, startDelayMax
    // startLifetimeType (Constant=0, TwoConstants=2), startLifetimeConstant, startLifetimeConstantMin, startLifetimeConstantMax
    // startSpeedType (Constant=0, TwoConstants=2), startSpeedConstant, startSpeedConstantMin, startSpeedConstantMax
    // startSizeType (Constant=0, TwoConstants=2), threeDStartSize, startSizeConstant/Separate
    // startRotationType (Constant=0, TwoConstants=2), threeDStartRotation, startRotationConstant/Separate
    // randomizeRotationDirection
    // startColorType (Color=0, TwoColors=2), startColorConstant, startColorConstantMin, startColorConstantMax
    // gravityModifier, simulationSpace, simulationSpeed, scaleMode
    // maxParticles, autoRandomSeed, randomSeed
    
    SerializedProperty duration;
    SerializedProperty looping;
    SerializedProperty startDelay;
    SerializedProperty startLifetime;
    SerializedProperty startSpeed;
    SerializedProperty size3D;
    SerializedProperty startSize;
    SerializedProperty startSizeY;
    SerializedProperty startSizeZ;
    SerializedProperty rotation3D;
    SerializedProperty startRotationX;
    SerializedProperty startRotationY;
    SerializedProperty startRotation;
    SerializedProperty randomizeRotationDirection;
    SerializedProperty startColor;
    SerializedProperty gravityModifier;
    SerializedProperty moveWithTransform;
    SerializedProperty simulationSpeed;
    SerializedProperty scalingMode;
    SerializedProperty playOnAwake;
    SerializedProperty maxNumParticles;
    SerializedProperty autoRandomSeed;
    SerializedProperty randomSeed;
    
    private void MainModulePropInit()
    {
        duration = serializedObject.FindProperty("lengthInSec");
        looping = serializedObject.FindProperty("looping");
        startDelay = serializedObject.FindProperty("startDelay");
        startLifetime = serializedObject.FindProperty("InitialModule.startLifetime");
        startSpeed = serializedObject.FindProperty("InitialModule.startSpeed");

        size3D = serializedObject.FindProperty("InitialModule.size3D");
        startSize = serializedObject.FindProperty("InitialModule.startSize");
        startSizeY = serializedObject.FindProperty("InitialModule.startSizeY");
        startSizeZ = serializedObject.FindProperty("InitialModule.startSizeZ");

        rotation3D = serializedObject.FindProperty("InitialModule.rotation3D");
        startRotationX = serializedObject.FindProperty("InitialModule.startRotationX");
        startRotationY = serializedObject.FindProperty("InitialModule.startRotationY");
        startRotation = serializedObject.FindProperty("InitialModule.startRotation");

        randomizeRotationDirection = serializedObject.FindProperty("InitialModule.randomizeRotationDirection");

        startColor = serializedObject.FindProperty("InitialModule.startColor");

        gravityModifier = serializedObject.FindProperty("InitialModule.gravityModifier");
        moveWithTransform = serializedObject.FindProperty("moveWithTransform");
        simulationSpeed = serializedObject.FindProperty("simulationSpeed");
        scalingMode = serializedObject.FindProperty("scalingMode");
        playOnAwake = serializedObject.FindProperty("playOnAwake");
        maxNumParticles = serializedObject.FindProperty("InitialModule.maxNumParticles");
        autoRandomSeed = serializedObject.FindProperty("autoRandomSeed");
        randomSeed = serializedObject.FindProperty("randomSeed");
    }

    private void MainModule()
    {
        // duration
        duration.floatValue = EditorGUILayout.FloatField(new GUIContent("Duration"), duration.floatValue);

        // looping
        looping.boolValue = EditorGUILayout.Toggle("Looping", looping.boolValue);

        // Play On Awake
        playOnAwake.boolValue = EditorGUILayout.Toggle("Play On Awake", playOnAwake.boolValue);

        // start delay (支持 Constant 和 TwoConstants)
        EditorGUILayout.BeginHorizontal();
        CurveEditor(ref startDelay, "Start Delay");
        CurveModeEditor(ref startDelay, (CurveMode_Constant | CurveMode_TwoConstants), 2);
        EditorGUILayout.EndHorizontal();

        // start lifetime (支持 Constant 和 TwoConstants)
        EditorGUILayout.BeginHorizontal();
        CurveEditor(ref startLifetime, "Start Lifetime");
        CurveModeEditor(ref startLifetime, (CurveMode_Constant | CurveMode_TwoConstants), 2);
        EditorGUILayout.EndHorizontal();

        // start speed (支持 Constant 和 TwoConstants)
        EditorGUILayout.BeginHorizontal();
        CurveEditor(ref startSpeed, "Start Speed");
        CurveModeEditor(ref startSpeed, (CurveMode_Constant | CurveMode_TwoConstants), 2);
        EditorGUILayout.EndHorizontal();

        // 3d start size
        size3D.boolValue = EditorGUILayout.Toggle("3D Start Size", size3D.boolValue);
        // start size (支持 Constant 和 TwoConstants)
        if (size3D.boolValue)
        {
            // X
            EditorGUILayout.BeginHorizontal();
            CurveEditor(ref startSize, "X");
            CurveModeEditor(ref startSize, (CurveMode_Constant | CurveMode_TwoConstants), 2);
            EditorGUILayout.EndHorizontal();

            // Y
            EditorGUILayout.BeginHorizontal();
            CurveEditor(ref startSizeY, "Y");
            startSizeY.FindPropertyRelative("minMaxState").intValue = startSize.FindPropertyRelative("minMaxState").intValue;
            EditorGUILayout.EndHorizontal();

            // Z
            EditorGUILayout.BeginHorizontal();
            CurveEditor(ref startSizeZ, "Z");
            startSizeZ.FindPropertyRelative("minMaxState").intValue = startSize.FindPropertyRelative("minMaxState").intValue;
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            EditorGUILayout.BeginHorizontal();
            CurveEditor(ref startSize, "Start Size");
            CurveModeEditor(ref startSize, (CurveMode_Constant | CurveMode_TwoConstants), 2);
            EditorGUILayout.EndHorizontal();
        }

        // 3D start rotation
        rotation3D.boolValue = EditorGUILayout.Toggle("3D Start Rotation", rotation3D.boolValue);
        // start rotation (支持 Constant 和 TwoConstants)
        if (rotation3D.boolValue)
        {
            // X
            EditorGUILayout.BeginHorizontal();
            CurveEditor(ref startRotationX, "X");
            CurveModeEditor(ref startRotationX, (CurveMode_Constant | CurveMode_TwoConstants), 2);
            EditorGUILayout.EndHorizontal();
            // Y
            EditorGUILayout.BeginHorizontal();
            CurveEditor(ref startRotationY, "Y");
            startRotationY.FindPropertyRelative("minMaxState").intValue = startRotationX.FindPropertyRelative("minMaxState").intValue;
            EditorGUILayout.EndHorizontal();
            // Z
            EditorGUILayout.BeginHorizontal();
            CurveEditor(ref startRotation, "Z");
            startRotation.FindPropertyRelative("minMaxState").intValue = startRotationX.FindPropertyRelative("minMaxState").intValue;
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            EditorGUILayout.BeginHorizontal();
            CurveEditor(ref startRotation, "Start Rotation");
            CurveModeEditor(ref startRotation, (CurveMode_Constant | CurveMode_TwoConstants), 2);
            EditorGUILayout.EndHorizontal();
        }

        // Randomize Direction (flip rotation)
        randomizeRotationDirection.floatValue = EditorGUILayout.Slider("Randomize Direction", randomizeRotationDirection.floatValue, 0f, 1f);

        // start color (支持 Color 和 TwoColors)
        EditorGUILayout.BeginHorizontal();
        GradientEditor(ref startColor, "Start Color");
        GradientModeEditor(ref startColor, (GradientMode_Color | GradientMode_TwoColors), 2);
        EditorGUILayout.EndHorizontal();

        // gravity Modifier (只支持 Constant)
        EditorGUILayout.BeginHorizontal();
        CurveEditor(ref gravityModifier, "Gravity Modifier");
        CurveModeEditor(ref gravityModifier, (CurveMode_Constant), 1);
        EditorGUILayout.EndHorizontal();

        // simulationSpace (LayaAir: 0=world, 1=local)
        moveWithTransform.intValue = EditorGUILayout.IntPopup("Simulation Space", moveWithTransform.intValue, new[] { "Local", "World" }, new[] { (int)ParticleSystemSimulationSpace.Local, (int)ParticleSystemSimulationSpace.World });

        // simulationSpeed
        simulationSpeed.floatValue = EditorGUILayout.FloatField("Simulation Speed", simulationSpeed.floatValue);

        // Scaling mode (LayaAir: 0=hierarchy, 1=local, 2=shape)
        scalingMode.intValue = EditorGUILayout.IntPopup("Scaling Mode", scalingMode.intValue, new[] { "Hierarchy", "Local", "Shape" }, new[] { (int)ParticleSystemScalingMode.Hierarchy, (int)ParticleSystemScalingMode.Local, (int)ParticleSystemScalingMode.Shape });

        // Max Particles
        maxNumParticles.intValue = EditorGUILayout.IntField("Max Particles", maxNumParticles.intValue);

        // Auto Random Seed
        autoRandomSeed.boolValue = EditorGUILayout.Toggle("Auto Random Seed", autoRandomSeed.boolValue);
        if (!autoRandomSeed.boolValue)
        {
            randomSeed.intValue = EditorGUILayout.IntField("Random Seed", randomSeed.intValue);
        }
    }

    #endregion

    #region Emission Module
    // LayaAir 3.0 支持的Emission模块属性:
    // enable, emissionRate (Rate over Time), emissionRateOverDistance (Rate over Distance)
    // bursts: time, minCount, maxCount

    SerializedProperty emissionEnable;
    SerializedProperty rateOverTime;
    SerializedProperty rateOverDistance;
    SerializedProperty m_BurstCount;
    SerializedProperty m_Bursts;

    ReorderableList burstsList = null;
    private void EmissionModulePropInit()
    {
        emissionEnable = serializedObject.FindProperty("EmissionModule.enabled");
        rateOverTime = serializedObject.FindProperty("EmissionModule.rateOverTime");
        rateOverDistance = serializedObject.FindProperty("EmissionModule.rateOverDistance");
        m_BurstCount = serializedObject.FindProperty("EmissionModule.m_BurstCount");
        m_Bursts = serializedObject.FindProperty("EmissionModule.m_Bursts");

        if (burstsList == null)
        {
            burstsList = new ReorderableList(serializedObject, m_Bursts, true, true, true, true)
            {
                drawElementCallback = DrawBurstsListItems,
                drawHeaderCallback = DrawBurstsHeader,
                onAddCallback = OnAddBurstCallback,
                onRemoveCallback = OnRemoveBurstCallback
            };
        }
    }

    private void EmissionModule()
    {
        emissionEnable.boolValue = EditorGUILayout.BeginToggleGroup("Emission", emissionEnable.boolValue);
        EditorGUILayout.Space();
        if (emissionEnable.boolValue)
        {
            // rate over time (只支持 Constant)
            EditorGUILayout.BeginHorizontal();
            CurveEditor(ref rateOverTime, "Rate over Time");
            CurveModeEditor(ref rateOverTime, (CurveMode_Constant), 1);
            EditorGUILayout.EndHorizontal();

            // rate over distance (只支持 Constant)
            EditorGUILayout.BeginHorizontal();
            CurveEditor(ref rateOverDistance, "Rate over Distance");
            CurveModeEditor(ref rateOverDistance, (CurveMode_Constant), 1);
            EditorGUILayout.EndHorizontal();

            // Bursts
            burstsList.DoLayoutList();
        }
        EditorGUILayout.EndToggleGroup();
    }

    void DrawBurstsListItems(Rect rect, int index, bool isActive, bool isFocused)
    {
        SerializedProperty burst = burstsList.serializedProperty.GetArrayElementAtIndex(index);
        SerializedProperty time = burst.FindPropertyRelative("time");
        SerializedProperty countCurve = burst.FindPropertyRelative("countCurve");

        // Time
        time.floatValue = EditorGUI.FloatField(new Rect(rect.x, rect.y, 80, EditorGUIUtility.singleLineHeight), time.floatValue);

        // LayaAir 3.0 Burst只支持 minCount 和 maxCount (对应 TwoConstants 模式)
        ParticleSystemCurveMode mode = (ParticleSystemCurveMode)countCurve.FindPropertyRelative("minMaxState").intValue;
        
        // Min Count
        float minCount = countCurve.FindPropertyRelative("minScalar").floatValue;
        float maxCount = countCurve.FindPropertyRelative("scalar").floatValue;
        
        if (mode == ParticleSystemCurveMode.Constant)
        {
            // 单值模式，min和max相同
            maxCount = EditorGUI.FloatField(new Rect(rect.x + 90, rect.y, 60, EditorGUIUtility.singleLineHeight), maxCount);
            countCurve.FindPropertyRelative("scalar").floatValue = maxCount;
            countCurve.FindPropertyRelative("minScalar").floatValue = maxCount;
        }
        else
        {
            // TwoConstants模式
            minCount = EditorGUI.FloatField(new Rect(rect.x + 90, rect.y, 50, EditorGUIUtility.singleLineHeight), minCount);
            maxCount = EditorGUI.FloatField(new Rect(rect.x + 150, rect.y, 50, EditorGUIUtility.singleLineHeight), maxCount);
            countCurve.FindPropertyRelative("minScalar").floatValue = minCount;
            countCurve.FindPropertyRelative("scalar").floatValue = maxCount;
        }
        
        // Mode选择 (只支持 Constant 和 TwoConstants)
        string[] names = { "Constant", "Random" };
        int[] flags = { (int)ParticleSystemCurveMode.Constant, (int)ParticleSystemCurveMode.TwoConstants };
        countCurve.FindPropertyRelative("minMaxState").intValue = EditorGUI.IntPopup(new Rect(rect.x + 210, rect.y, 80, EditorGUIUtility.singleLineHeight), countCurve.FindPropertyRelative("minMaxState").intValue, names, flags);

        if (m_BurstCount.intValue == 8)
        {
            burstsList.displayAdd = false;
        }
        else
        {
            burstsList.displayAdd = true;
        }
    }

    void DrawBurstsHeader(Rect rect)
    {
        EditorGUI.PrefixLabel(new Rect(rect.x + 20, rect.y, 200, EditorGUIUtility.singleLineHeight), new GUIContent("Time          Count"));
    }

    void OnAddBurstCallback(ReorderableList list)
    {
        int index = list.serializedProperty.arraySize;

        if (index == 8)
            return;

        m_BurstCount.intValue++;
        list.serializedProperty.arraySize = m_BurstCount.intValue;
        list.index = index;

        SerializedProperty burst = list.serializedProperty.GetArrayElementAtIndex(index);
        SerializedProperty time = burst.FindPropertyRelative("time");
        SerializedProperty countCurve = burst.FindPropertyRelative("countCurve");
        SerializedProperty cycleCount = burst.FindPropertyRelative("cycleCount");
        SerializedProperty repeatInterval = burst.FindPropertyRelative("repeatInterval");
        SerializedProperty probability = burst.FindPropertyRelative("probability");

        time.floatValue = 0.0f;
        countCurve.FindPropertyRelative("minMaxState").intValue = 0;
        countCurve.FindPropertyRelative("scalar").floatValue = 30;
        countCurve.FindPropertyRelative("minScalar").floatValue = 30;
        cycleCount.intValue = 1;
        repeatInterval.floatValue = 0.010f;
        probability.floatValue = 1.0f;
    }

    void OnRemoveBurstCallback(ReorderableList list)
    {
        int index = list.index;
        int delIndex = list.serializedProperty.arraySize - 1;
        list.serializedProperty.MoveArrayElement(index, delIndex);
        list.serializedProperty.DeleteArrayElementAtIndex(delIndex);
        m_BurstCount.intValue--;
        list.index = delIndex - 1;
    }

    #endregion

    #region Shape Module
    // LayaAir 3.0 支持的Shape类型:
    // BoxShape: x, y, z (length), randomDirection
    // CircleShape: radius, emitFromEdge, arcDEG, randomDirection
    // ConeShape: angleDEG, radius, length, emitType (Base=0, BaseShell=1, Volume=2, VolumeShell=3), randomDirection
    // HemisphereShape: radius, emitFromShell, randomDirection
    // SphereShape: radius, emitFromShell, randomDirection

    SerializedProperty shapeEnable;
    SerializedProperty shapeType;
    SerializedProperty shapeRadius;
    SerializedProperty radiusThickness;
    SerializedProperty randomDirectionAmount;
    SerializedProperty angle;
    SerializedProperty shapeLength;
    SerializedProperty shapeScale;
    SerializedProperty shapeArcValue;
    
    private void ShapeModulePropInit()
    {
        SerializedProperty shape = serializedObject.FindProperty("ShapeModule");
        shapeEnable = shape.FindPropertyRelative("enabled");
        shapeType = shape.FindPropertyRelative("type");
        shapeRadius = shape.FindPropertyRelative("radius.value");
        radiusThickness = shape.FindPropertyRelative("radiusThickness");
        randomDirectionAmount = shape.FindPropertyRelative("randomDirectionAmount");
        angle = shape.FindPropertyRelative("angle");
        shapeLength = shape.FindPropertyRelative("length");
        shapeScale = shape.FindPropertyRelative("m_Scale");
        shapeArcValue = shape.FindPropertyRelative("arc.value");
    }

    private void ShapeModule()
    {
        shapeEnable.boolValue = EditorGUILayout.BeginToggleGroup("Shape", shapeEnable.boolValue);
        EditorGUILayout.Space();
        if (shapeEnable.boolValue)
        {
            // type (LayaAir 3.0 支持: Sphere, Hemisphere, Cone, ConeVolume, Box, Circle)
            shapeType.intValue = EditorGUILayout.IntPopup("Shape", shapeType.intValue, 
                new[] { "Sphere", "Hemisphere", "Cone", "Box", "Circle" }, 
                new[] { (int)ParticleSystemShapeType.Sphere, (int)ParticleSystemShapeType.Hemisphere, (int)ParticleSystemShapeType.Cone, (int)ParticleSystemShapeType.Box, (int)ParticleSystemShapeType.Circle });

            ParticleSystemShapeType st = (ParticleSystemShapeType)shapeType.intValue;
            switch (st)
            {
                case ParticleSystemShapeType.Sphere:
                    // SphereShape: radius, emitFromShell
                    shapeRadius.floatValue = EditorGUILayout.FloatField("Radius", shapeRadius.floatValue);
                    bool emitFromShellSphere = radiusThickness.floatValue == 0;
                    emitFromShellSphere = EditorGUILayout.Toggle("Emit From Shell", emitFromShellSphere);
                    radiusThickness.floatValue = emitFromShellSphere ? 0 : 1;
                    break;
                    
                case ParticleSystemShapeType.Hemisphere:
                    // HemisphereShape: radius, emitFromShell
                    shapeRadius.floatValue = EditorGUILayout.FloatField("Radius", shapeRadius.floatValue);
                    bool emitFromShellHemi = radiusThickness.floatValue == 0;
                    emitFromShellHemi = EditorGUILayout.Toggle("Emit From Shell", emitFromShellHemi);
                    radiusThickness.floatValue = emitFromShellHemi ? 0 : 1;
                    break;
                    
                case ParticleSystemShapeType.Cone:
                case ParticleSystemShapeType.ConeVolume:
                    // ConeShape: angleDEG, radius, length, emitType
                    angle.floatValue = EditorGUILayout.Slider("Angle", angle.floatValue, 0f, 90f);
                    shapeRadius.floatValue = EditorGUILayout.FloatField("Radius", shapeRadius.floatValue);
                    shapeLength.floatValue = EditorGUILayout.FloatField("Length", shapeLength.floatValue);
                    
                    // emitType: Base=0, BaseShell=1, Volume=2, VolumeShell=3
                    int emitType = 0;
                    bool isVolume = shapeType.intValue == (int)ParticleSystemShapeType.ConeVolume;
                    bool isShell = radiusThickness.floatValue == 0;
                    if (!isVolume && !isShell) emitType = 0; // Base
                    else if (!isVolume && isShell) emitType = 1; // BaseShell
                    else if (isVolume && !isShell) emitType = 2; // Volume
                    else emitType = 3; // VolumeShell
                    
                    emitType = EditorGUILayout.IntPopup("Emit From", emitType, 
                        new[] { "Base", "Base Shell", "Volume", "Volume Shell" }, 
                        new[] { 0, 1, 2, 3 });
                    
                    // 根据emitType设置shapeType和radiusThickness
                    switch (emitType)
                    {
                        case 0: // Base
                            shapeType.intValue = (int)ParticleSystemShapeType.Cone;
                            radiusThickness.floatValue = 1;
                            break;
                        case 1: // BaseShell
                            shapeType.intValue = (int)ParticleSystemShapeType.Cone;
                            radiusThickness.floatValue = 0;
                            break;
                        case 2: // Volume
                            shapeType.intValue = (int)ParticleSystemShapeType.ConeVolume;
                            radiusThickness.floatValue = 1;
                            break;
                        case 3: // VolumeShell
                            shapeType.intValue = (int)ParticleSystemShapeType.ConeVolume;
                            radiusThickness.floatValue = 0;
                            break;
                    }
                    break;
                    
                case ParticleSystemShapeType.Box:
                    // BoxShape: x, y, z (length)
                    shapeScale.vector3Value = EditorGUILayout.Vector3Field("Size", shapeScale.vector3Value);
                    break;
                    
                case ParticleSystemShapeType.Circle:
                    // CircleShape: radius, emitFromEdge, arcDEG
                    shapeRadius.floatValue = EditorGUILayout.FloatField("Radius", shapeRadius.floatValue);
                    bool emitFromEdge = radiusThickness.floatValue == 0;
                    emitFromEdge = EditorGUILayout.Toggle("Emit From Edge", emitFromEdge);
                    radiusThickness.floatValue = emitFromEdge ? 0 : 1;
                    shapeArcValue.floatValue = EditorGUILayout.Slider("Arc", shapeArcValue.floatValue, 0f, 360f);
                    break;
                default:
                    break;
            }
            
            // randomDirection (所有Shape都支持)
            bool randomDir = randomDirectionAmount.floatValue > 0;
            randomDir = EditorGUILayout.Toggle("Randomize Direction", randomDir);
            randomDirectionAmount.floatValue = randomDir ? 1 : 0;
        }
        EditorGUILayout.EndToggleGroup();
    }
    #endregion

    #region Velocity Over Lifetime Module
    // LayaAir 3.0 支持的VelocityOverLifetime属性:
    // enable, velocity._type (Constant=0, Curve=1, TwoConstants=2, TwoCurves=3)
    // velocity._constant, _constantMin, _constantMax (Vector3)
    // velocity._gradientX/Y/Z, _gradientXMin/YMin/ZMin, _gradientXMax/YMax/ZMax (曲线)
    // space (0=local, 1=world)
    
    SerializedProperty velocityEnalbe;
    SerializedProperty velocityX;
    SerializedProperty velocityY;
    SerializedProperty velocityZ;
    SerializedProperty inWorldSpace;

    private void VelocityModulePropInit()
    {
        SerializedProperty velocity = serializedObject.FindProperty("VelocityModule");
        velocityEnalbe = velocity.FindPropertyRelative("enabled");
        velocityX = velocity.FindPropertyRelative("x");
        velocityY = velocity.FindPropertyRelative("y");
        velocityZ = velocity.FindPropertyRelative("z");
        inWorldSpace = velocity.FindPropertyRelative("inWorldSpace");
    }

    private void VelocityModule()
    {
        velocityEnalbe.boolValue = EditorGUILayout.BeginToggleGroup("Velocity over Lifetime", velocityEnalbe.boolValue);
        EditorGUILayout.Space();
        if (velocityEnalbe.boolValue)
        {
            // x (支持 Constant, Curve, TwoConstants, TwoCurves)
            EditorGUILayout.BeginHorizontal();
            CurveEditor(ref velocityX, "X");
            CurveModeEditor(ref velocityX, (CurveMode_Constant | CurveMode_Curve | CurveMode_TwoConstants | CurveMode_TwoCurves), 4);
            EditorGUILayout.EndHorizontal();
            // y
            EditorGUILayout.BeginHorizontal();
            CurveEditor(ref velocityY, "Y");
            velocityY.FindPropertyRelative("minMaxState").intValue = velocityX.FindPropertyRelative("minMaxState").intValue;
            EditorGUILayout.EndHorizontal();
            // z
            EditorGUILayout.BeginHorizontal();
            CurveEditor(ref velocityZ, "Z");
            velocityZ.FindPropertyRelative("minMaxState").intValue = velocityX.FindPropertyRelative("minMaxState").intValue;
            EditorGUILayout.EndHorizontal();

            // space (LayaAir: 0=local, 1=world)
            int space = inWorldSpace.boolValue ? 1 : 0;
            space = EditorGUILayout.IntPopup("Space", space, new[] { "Local", "World" }, new[] { 0, 1 });
            inWorldSpace.boolValue = (space == 1);
        }
        EditorGUILayout.EndToggleGroup();
    }

    #endregion

    #region Color over Lifetime module
    // LayaAir 3.0 支持的ColorOverLifetime属性:
    // enable, color._type (Constant=0, Gradient=1, TwoConstants=2, TwoGradients=3)
    // color._constant, _constantMin, _constantMax (Vector4/Color)
    // color._gradient, _gradientMin, _gradientMax (Gradient)
    
    SerializedProperty colorEnable;
    SerializedProperty colorGradient;

    private void ColorModulePropInit()
    {
        SerializedProperty color = serializedObject.FindProperty("ColorModule");
        colorEnable = color.FindPropertyRelative("enabled");
        colorGradient = color.FindPropertyRelative("gradient");
    }

    private void ColorModule()
    {
        colorEnable.boolValue = EditorGUILayout.BeginToggleGroup("Color over Lifetime", colorEnable.boolValue);
        EditorGUILayout.Space();
        if (colorEnable.boolValue)
        {
            // 支持 Color, Gradient, TwoColors, TwoGradients
            GradientEditor(ref colorGradient, "Color");
            GradientModeEditor(ref colorGradient, (GradientMode_Color | GradientMode_Gradient | GradientMode_TwoColors | GradientMode_TwoGradients), 4);
        }
        EditorGUILayout.EndToggleGroup();
    }
    #endregion

    #region Size over Lifetime module
    // LayaAir 3.0 支持的SizeOverLifetime属性:
    // enable, size._separateAxes (3D)
    // size._type (Curve=0, TwoConstants=1, TwoCurves=2) - 注意没有Constant
    // size._constantMin, _constantMax, _constantMinSeparate, _constantMaxSeparate
    // size._gradient, _gradientX/Y/Z, _gradientMin, _gradientMax, _gradientXMin/YMin/ZMin, _gradientXMax/YMax/ZMax
    
    SerializedProperty sizeEnable;
    SerializedProperty sizeSeparateAxes;
    SerializedProperty sizeX;
    SerializedProperty sizeY;
    SerializedProperty sizeZ;

    private void SizeModulePropInit()
    {
        SerializedProperty size = serializedObject.FindProperty("SizeModule");
        sizeEnable = size.FindPropertyRelative("enabled");
        sizeSeparateAxes = size.FindPropertyRelative("separateAxes");
        sizeX = size.FindPropertyRelative("curve");
        sizeY = size.FindPropertyRelative("y");
        sizeZ = size.FindPropertyRelative("z");
    }

    private void SizeModule()
    {
        sizeEnable.boolValue = EditorGUILayout.BeginToggleGroup("Size over Lifetime", sizeEnable.boolValue);
        EditorGUILayout.Space();
        if (sizeEnable.boolValue)
        {
            // Separate Axes (3D)
            sizeSeparateAxes.boolValue = EditorGUILayout.Toggle("Separate Axes", sizeSeparateAxes.boolValue);

            if (sizeSeparateAxes.boolValue)
            {
                // 支持 Curve, TwoConstants, TwoCurves
                EditorGUILayout.BeginHorizontal();
                CurveEditor(ref sizeX, "X");
                CurveModeEditor(ref sizeX, (CurveMode_Curve | CurveMode_TwoConstants | CurveMode_TwoCurves), 3);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                CurveEditor(ref sizeY, "Y");
                sizeY.FindPropertyRelative("minMaxState").intValue = sizeX.FindPropertyRelative("minMaxState").intValue;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                CurveEditor(ref sizeZ, "Z");
                sizeZ.FindPropertyRelative("minMaxState").intValue = sizeX.FindPropertyRelative("minMaxState").intValue;
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                CurveEditor(ref sizeX, "Size");
                CurveModeEditor(ref sizeX, (CurveMode_Curve | CurveMode_TwoConstants | CurveMode_TwoCurves), 3);
                EditorGUILayout.EndHorizontal();
            }
        }
        EditorGUILayout.EndToggleGroup();
    }
    #endregion

    #region Rotation Over Lifetime Module
    // LayaAir 3.0 支持的RotationOverLifetime属性:
    // enable, angularVelocity._separateAxes (3D)
    // angularVelocity._type (Constant=0, Curve=1, TwoConstants=2, TwoCurves=3)
    // angularVelocity._constant, _constantMin, _constantMax
    // angularVelocity._constantSeparate, _constantMinSeparate, _constantMaxSeparate (Vector3)
    // angularVelocity._gradient, _gradientX/Y/Z, _gradientMin, _gradientMax, etc.
    
    SerializedProperty rotationEnable;
    SerializedProperty rotationSeparateAxes;
    SerializedProperty rotationX;
    SerializedProperty rotationY;
    SerializedProperty rotationZ;

    private void RotationModulePropInit()
    {
        SerializedProperty rotation = serializedObject.FindProperty("RotationModule");
        rotationEnable = rotation.FindPropertyRelative("enabled");
        rotationSeparateAxes = rotation.FindPropertyRelative("separateAxes");
        rotationX = rotation.FindPropertyRelative("x");
        rotationY = rotation.FindPropertyRelative("y");
        rotationZ = rotation.FindPropertyRelative("curve");
    }

    private void RotationModule()
    {
        rotationEnable.boolValue = EditorGUILayout.BeginToggleGroup("Rotation over Lifetime", rotationEnable.boolValue);
        EditorGUILayout.Space();
        if (rotationEnable.boolValue)
        {
            // Separate Axes (3D)
            rotationSeparateAxes.boolValue = EditorGUILayout.Toggle("Separate Axes", rotationSeparateAxes.boolValue);

            if (rotationSeparateAxes.boolValue)
            {
                // 支持 Constant, Curve, TwoConstants, TwoCurves
                // X
                EditorGUILayout.BeginHorizontal();
                CurveEditor(ref rotationX, "X");
                CurveModeEditor(ref rotationX, (CurveMode_Constant | CurveMode_Curve | CurveMode_TwoConstants | CurveMode_TwoCurves), 4);
                EditorGUILayout.EndHorizontal();
                // Y
                EditorGUILayout.BeginHorizontal();
                CurveEditor(ref rotationY, "Y");
                rotationY.FindPropertyRelative("minMaxState").intValue = rotationX.FindPropertyRelative("minMaxState").intValue;
                EditorGUILayout.EndHorizontal();
                // Z
                EditorGUILayout.BeginHorizontal();
                CurveEditor(ref rotationZ, "Z");
                rotationZ.FindPropertyRelative("minMaxState").intValue = rotationX.FindPropertyRelative("minMaxState").intValue;
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                CurveEditor(ref rotationZ, "Angular Velocity");
                CurveModeEditor(ref rotationZ, (CurveMode_Constant | CurveMode_Curve | CurveMode_TwoConstants | CurveMode_TwoCurves), 4);
                EditorGUILayout.EndHorizontal();
            }
        }
        EditorGUILayout.EndToggleGroup();
    }
    #endregion

    #region Texture Sheet Animation
    // LayaAir 3.0 支持的TextureSheetAnimation属性:
    // enable, tiles (Vector2: x, y), type (WholeSheet=0, SingleRow=1), rowIndex
    // frame._type (Constant=0, Curve=1, TwoConstants=2, TwoCurves=3)
    // frame._constant, _constantMin, _constantMax, _overTime, _overTimeMin, _overTimeMax
    // startFrame._type (Constant=0, TwoConstants=1)
    // startFrame._constant, _constantMin, _constantMax
    // cycles
    
    SerializedProperty uvEnable;
    SerializedProperty uvMode;
    SerializedProperty tilesX;
    SerializedProperty tilesY;
    SerializedProperty animationType;
    SerializedProperty rowIndex;
    SerializedProperty randowRow;
    SerializedProperty frameOverTime;
    SerializedProperty startFrame;
    SerializedProperty cycles;

    private void UVModulePropInit()
    {
        SerializedProperty uv = serializedObject.FindProperty("UVModule");
        uvEnable = uv.FindPropertyRelative("enabled");
        uvMode = uv.FindPropertyRelative("mode");
        tilesX = uv.FindPropertyRelative("tilesX");
        tilesY = uv.FindPropertyRelative("tilesY");
        animationType = uv.FindPropertyRelative("animationType");
        rowIndex = uv.FindPropertyRelative("rowIndex");
        randowRow = uv.FindPropertyRelative("randomRow");
        frameOverTime = uv.FindPropertyRelative("frameOverTime");
        startFrame = uv.FindPropertyRelative("startFrame");
        cycles = uv.FindPropertyRelative("cycles");
    }

    private void UVModule()
    {
        uvEnable.boolValue = EditorGUILayout.BeginToggleGroup("Texture Sheet Animation", uvEnable.boolValue);
        EditorGUILayout.Space();
        if (uvEnable.boolValue)
        {
            // mode (LayaAir只支持Grid)
            uvMode.intValue = EditorGUILayout.IntPopup("Mode", uvMode.intValue, new[] { "Grid" }, new[] { (int)ParticleSystemAnimationMode.Grid });

            // Tiles
            EditorGUILayout.LabelField("Tiles");
            EditorGUI.indentLevel++;
            tilesX.intValue = EditorGUILayout.IntField("X", tilesX.intValue);
            tilesY.intValue = EditorGUILayout.IntField("Y", tilesY.intValue);
            EditorGUI.indentLevel--;

            // Animation type (WholeSheet=0, SingleRow=1)
            animationType.intValue = EditorGUILayout.IntPopup("Animation", animationType.intValue, new[] { "Whole Sheet", "Single Row" }, new[] { (int)ParticleSystemAnimationType.WholeSheet, (int)ParticleSystemAnimationType.SingleRow });

            if (animationType.intValue == (int)ParticleSystemAnimationType.SingleRow)
            {
                // Row Index (当使用Single Row时)
                rowIndex.intValue = EditorGUILayout.IntField("Row Index", rowIndex.intValue);
            }

            // Frame over Time (支持 Constant, Curve, TwoConstants, TwoCurves)
            EditorGUILayout.BeginHorizontal();
            CurveEditor(ref frameOverTime, "Frame over Time");
            CurveModeEditor(ref frameOverTime, (CurveMode_Constant | CurveMode_Curve | CurveMode_TwoConstants | CurveMode_TwoCurves), 4);
            EditorGUILayout.EndHorizontal();

            // Start Frame (支持 Constant, TwoConstants)
            EditorGUILayout.BeginHorizontal();
            CurveEditor(ref startFrame, "Start Frame");
            CurveModeEditor(ref startFrame, (CurveMode_Constant | CurveMode_TwoConstants), 2);
            EditorGUILayout.EndHorizontal();

            // Cycles
            cycles.floatValue = EditorGUILayout.FloatField("Cycles", cycles.floatValue);
        }
        EditorGUILayout.EndToggleGroup();
    }

    #endregion

    #region ParticleSystem Renderer
    // LayaAir 3.0 支持的Renderer属性:
    // renderMode (Billboard=0, StretchBillboard=1, HorizontalBillboard=2, VerticalBillboard=3, Mesh=4)
    // stretchedBillboardSpeedScale (Speed Scale)
    // stretchedBillboardLengthScale (Length Scale)
    // mesh
    // sortingFudge (Sort Fudge)
    
    SerializedObject render;
    SerializedProperty renderEnable;
    SerializedProperty renderMode;
    SerializedProperty m_VelocityScale;
    SerializedProperty m_LengthScale;
    SerializedProperty renderMesh;
    SerializedProperty m_Materials;
    SerializedProperty m_Material;
    SerializedProperty m_SortingFudge;

    private void RendererModulePropInit()
    {
        int count = targets.Length;

        ParticleSystemRenderer[] pRenders = new ParticleSystemRenderer[count];

        for (int i = 0; i < count; i++)
        {
            ParticleSystem ps = targets[i] as ParticleSystem;
            ParticleSystemRenderer pRender = ps.GetComponent<ParticleSystemRenderer>();
            pRenders[i] = pRender;
        }

        render = new SerializedObject(pRenders);

        renderEnable = render.FindProperty("m_Enabled");
        renderMode = render.FindProperty("m_RenderMode");
        m_VelocityScale = render.FindProperty("m_VelocityScale");
        m_LengthScale = render.FindProperty("m_LengthScale");
        renderMesh = render.FindProperty("m_Mesh");
        m_Materials = render.FindProperty("m_Materials");
        m_Material = m_Materials.GetArrayElementAtIndex(0);
        m_SortingFudge = render.FindProperty("m_SortingFudge");
    }

    private void RendererModule()
    {
        renderEnable.boolValue = EditorGUILayout.BeginToggleGroup("Renderer", renderEnable.boolValue);
        EditorGUILayout.Space();
        if (renderEnable.boolValue)
        {
            // render mode (LayaAir: Billboard=0, Stretch=1, Horizontal=2, Vertical=3, Mesh=4)
            renderMode.intValue = EditorGUILayout.IntPopup("Render Mode", renderMode.intValue, 
                new[] { "Billboard", "Stretched Billboard", "Horizontal Billboard", "Vertical Billboard", "Mesh" }, 
                new[] { (int)ParticleSystemRenderMode.Billboard, (int)ParticleSystemRenderMode.Stretch, (int)ParticleSystemRenderMode.HorizontalBillboard, (int)ParticleSystemRenderMode.VerticalBillboard, (int)ParticleSystemRenderMode.Mesh });

            ParticleSystemRenderMode rm = (ParticleSystemRenderMode)renderMode.intValue;
            switch (rm)
            {
                case ParticleSystemRenderMode.Billboard:
                case ParticleSystemRenderMode.HorizontalBillboard:
                case ParticleSystemRenderMode.VerticalBillboard:
                    break;
                case ParticleSystemRenderMode.Stretch:
                    // Speed Scale
                    m_VelocityScale.floatValue = EditorGUILayout.FloatField("Speed Scale", m_VelocityScale.floatValue);
                    // Length Scale
                    m_LengthScale.floatValue = EditorGUILayout.FloatField("Length Scale", m_LengthScale.floatValue);
                    break;
                case ParticleSystemRenderMode.Mesh:
                    // Mesh
                    EditorGUILayout.PropertyField(renderMesh);
                    break;
                case ParticleSystemRenderMode.None:
                    break;
                default:
                    break;
            }
            
            // Sort Fudge
            m_SortingFudge.floatValue = EditorGUILayout.FloatField("Sort Fudge", m_SortingFudge.floatValue);

            // Material
            m_Material.objectReferenceValue = EditorGUILayout.ObjectField("Material", m_Material.objectReferenceValue, typeof(Material), false);
        }
        EditorGUILayout.EndToggleGroup();

        // apply render 
        render.ApplyModifiedProperties();
    }
    #endregion

    #region editor func

    const uint CurveMode_Constant = 0x01;
    const uint CurveMode_Curve = 0x01 << 1;
    const uint CurveMode_TwoConstants = 0x01 << 2;
    const uint CurveMode_TwoCurves = 0x01 << 3;
    const uint GradientMode_Color = 0x01 << 4;
    const uint GradientMode_Gradient = 0x01 << 5;
    const uint GradientMode_TwoColors = 0x01 << 6;
    const uint GradientMode_TwoGradients = 0x01 << 7;
    const uint GradientMode_RandomColor = 0x01 << 8;

    static Dictionary<uint, string[]> curveEditorString = new Dictionary<uint, string[]>();
    static Dictionary<uint, int[]> curveEditorFlag = new Dictionary<uint, int[]>();
    
    private void CurveEditor(ref SerializedProperty serializedProperty, string name)
    {
        EditorGUILayout.PrefixLabel(name);
        ParticleSystemCurveMode mode = (ParticleSystemCurveMode)serializedProperty.FindPropertyRelative("minMaxState").intValue;
        switch (mode)
        {
            case ParticleSystemCurveMode.Constant:
                serializedProperty.FindPropertyRelative("scalar").floatValue = EditorGUILayout.FloatField(serializedProperty.FindPropertyRelative("scalar").floatValue);
                break;
            case ParticleSystemCurveMode.Curve:
                serializedProperty.FindPropertyRelative("maxCurve").animationCurveValue = EditorGUILayout.CurveField(serializedProperty.FindPropertyRelative("maxCurve").animationCurveValue);
                break;
            case ParticleSystemCurveMode.TwoConstants:
                serializedProperty.FindPropertyRelative("minScalar").floatValue = EditorGUILayout.FloatField(serializedProperty.FindPropertyRelative("minScalar").floatValue);
                serializedProperty.FindPropertyRelative("scalar").floatValue = EditorGUILayout.FloatField(serializedProperty.FindPropertyRelative("scalar").floatValue);
                break;
            case ParticleSystemCurveMode.TwoCurves:
                serializedProperty.FindPropertyRelative("minCurve").animationCurveValue = EditorGUILayout.CurveField(serializedProperty.FindPropertyRelative("minCurve").animationCurveValue);
                serializedProperty.FindPropertyRelative("maxCurve").animationCurveValue = EditorGUILayout.CurveField(serializedProperty.FindPropertyRelative("maxCurve").animationCurveValue);
                break;
            default:
                break;
        }
    }

    private void CurveModeEditor(ref SerializedProperty serializedProperty, uint key, int num)
    {
        string[] names;
        int[] flags;
        if (curveEditorString.ContainsKey(key))
        {
            names = curveEditorString[key];
            flags = curveEditorFlag[key];
        }
        else
        {
            names = new string[num];
            flags = new int[num];
            int count = 0;
            if ((key & CurveMode_Constant) != 0)
            {
                names[count] = "Constant";
                flags[count] = (int)ParticleSystemCurveMode.Constant;
                count++;
            }
            if ((key & CurveMode_Curve) != 0)
            {
                names[count] = "Curve";
                flags[count] = (int)ParticleSystemCurveMode.Curve;
                count++;
            }
            if ((key & CurveMode_TwoConstants) != 0)
            {
                names[count] = "Random Between Two Constants";
                flags[count] = (int)ParticleSystemCurveMode.TwoConstants;
                count++;
            }
            if ((key & CurveMode_TwoCurves) != 0)
            {
                names[count] = "Random Between Two Curves";
                flags[count] = (int)ParticleSystemCurveMode.TwoCurves;
            }
            curveEditorString.Add(key, names);
            curveEditorFlag.Add(key, flags);
        }

        serializedProperty.FindPropertyRelative("minMaxState").intValue = EditorGUILayout.IntPopup(serializedProperty.FindPropertyRelative("minMaxState").intValue, names, flags, GUILayout.Width(100));
    }

    private void GradientEditor(ref SerializedProperty serializedProperty, string name)
    {
        EditorGUILayout.PrefixLabel(name);
        ParticleSystemGradientMode mode = (ParticleSystemGradientMode)serializedProperty.FindPropertyRelative("minMaxState").intValue;
        switch (mode)
        {
            case ParticleSystemGradientMode.Color:
                serializedProperty.FindPropertyRelative("maxColor").colorValue = EditorGUILayout.ColorField(serializedProperty.FindPropertyRelative("maxColor").colorValue);
                break;
            case ParticleSystemGradientMode.Gradient:
                EditorGUILayout.PropertyField(serializedProperty.FindPropertyRelative("maxGradient"));
                break;
            case ParticleSystemGradientMode.TwoColors:
                serializedProperty.FindPropertyRelative("maxColor").colorValue = EditorGUILayout.ColorField(serializedProperty.FindPropertyRelative("maxColor").colorValue);
                serializedProperty.FindPropertyRelative("minColor").colorValue = EditorGUILayout.ColorField(serializedProperty.FindPropertyRelative("minColor").colorValue);
                break;
            case ParticleSystemGradientMode.TwoGradients:
                EditorGUILayout.PropertyField(serializedProperty.FindPropertyRelative("maxGradient"));
                EditorGUILayout.PropertyField(serializedProperty.FindPropertyRelative("minGradient"));
                break;
            case ParticleSystemGradientMode.RandomColor:
                break;
            default:
                break;
        }
    }

    private void GradientModeEditor(ref SerializedProperty serializedProperty, uint key, int num)
    {
        string[] names;
        int[] flags;

        if (curveEditorString.ContainsKey(key))
        {
            names = curveEditorString[key];
            flags = curveEditorFlag[key];
        }
        else
        {
            names = new string[num];
            flags = new int[num];
            int count = 0;
            if ((key & GradientMode_Color) != 0)
            {
                names[count] = "Color";
                flags[count] = (int)ParticleSystemGradientMode.Color;
                count++;
            }
            if ((key & GradientMode_Gradient) != 0)
            {
                names[count] = "Gradient";
                flags[count] = (int)ParticleSystemGradientMode.Gradient;
                count++;
            }
            if ((key & GradientMode_TwoColors) != 0)
            {
                names[count] = "Two Colors";
                flags[count] = (int)ParticleSystemGradientMode.TwoColors;
                count++;
            }
            if ((key & GradientMode_TwoGradients) != 0)
            {
                names[count] = "Two Gradients";
                flags[count] = (int)ParticleSystemGradientMode.TwoGradients;
                count++;
            }
            if ((key & GradientMode_RandomColor) != 0)
            {
                names[count] = "Random Color";
                flags[count] = (int)ParticleSystemGradientMode.RandomColor;
            }
            curveEditorString.Add(key, names);
            curveEditorFlag.Add(key, flags);
        }

        serializedProperty.FindPropertyRelative("minMaxState").intValue = EditorGUILayout.IntPopup(serializedProperty.FindPropertyRelative("minMaxState").intValue, names, flags, GUILayout.Width(100));
    }
    #endregion

    #region Animator func

    const string charFoldout = "-";
    const string charCollapsed = "≡";
    float animSpeed = 4f;

    static bool showmain = false;
    static bool showEmission = false;
    static bool showLayaShape = false;
    static bool showVelocityOverLifeTime = false;
    static bool showColorOverLifeTime = false;
    static bool showSizeOverLifeTime = false;
    static bool showRotationOverLifeTime = false;
    static bool showTextureSheetAnimation = false;
    static bool showRender = false;

    static AnimBool main;
    static AnimBool Emission;
    static AnimBool LayaShape;
    static AnimBool VelocityOverLifeTime;
    static AnimBool ColorOverLifeTime;
    static AnimBool SizeOverLifeTime;
    static AnimBool RotationOverLifeTime;
    static AnimBool TextureSheetAnimation;
    static AnimBool Render;

    private static GUIStyle _CollapseButton;
    public static GUIStyle CollapseButton
    {
        get
        {
            if (_CollapseButton == null)
            {
                _CollapseButton = new GUIStyle(EditorStyles.miniButtonLeft)
                {
                    fontSize = 16,
                    fontStyle = FontStyle.Normal,
                    fixedWidth = 30,
                    fixedHeight = 21.5f,
                };
            }

            return _CollapseButton;
        }
    }

    private static GUIStyle _GroupFoldout;
    public static GUIStyle GroupFoldout
    {
        get
        {
            if (_GroupFoldout == null)
            {
                _GroupFoldout = new GUIStyle(EditorStyles.miniButtonMid)
                {
                    fontSize = 11,
                    alignment = TextAnchor.MiddleLeft,
                    stretchWidth = true,
                    padding = new RectOffset()
                    {
                        left = 10,
                        top = 4,
                        bottom = 5
                    }
                };
            }

            return _GroupFoldout;
        }
    }
    
    void DoAnimat(AnimBool animbool, ref bool showBool, string modeName)
    {
        //Head
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button((showBool) ? charFoldout : charCollapsed, CollapseButton))
        {
            showBool = !showBool;
        }
        if (GUILayout.Button(modeName, GroupFoldout))
        {
            showBool = !showBool;
        }
        animbool.target = showBool;
        EditorGUILayout.EndHorizontal();
    }

    private void InspectorAnimator()
    {
        main = new AnimBool(true);
        main.valueChanged.AddListener(Repaint);
        main.speed = animSpeed;

        Emission = new AnimBool(true);
        Emission.valueChanged.AddListener(Repaint);
        Emission.speed = animSpeed;

        LayaShape = new AnimBool(true);
        LayaShape.valueChanged.AddListener(Repaint);
        LayaShape.speed = animSpeed;

        VelocityOverLifeTime = new AnimBool(true);
        VelocityOverLifeTime.valueChanged.AddListener(Repaint);
        VelocityOverLifeTime.speed = animSpeed;

        ColorOverLifeTime = new AnimBool(true);
        ColorOverLifeTime.valueChanged.AddListener(Repaint);
        ColorOverLifeTime.speed = animSpeed;

        SizeOverLifeTime = new AnimBool(true);
        SizeOverLifeTime.valueChanged.AddListener(Repaint);
        SizeOverLifeTime.speed = animSpeed;

        RotationOverLifeTime = new AnimBool(true);
        RotationOverLifeTime.valueChanged.AddListener(Repaint);
        RotationOverLifeTime.speed = animSpeed;

        TextureSheetAnimation = new AnimBool(true);
        TextureSheetAnimation.valueChanged.AddListener(Repaint);
        TextureSheetAnimation.speed = animSpeed;

        Render = new AnimBool(true);
        Render.valueChanged.AddListener(Repaint);
        Render.speed = animSpeed;
    }
    #endregion
}
