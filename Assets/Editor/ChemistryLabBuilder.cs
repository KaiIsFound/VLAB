using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using VLAB.ChemistryLab;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Creates a self-contained acid/base titration room from primitive geometry.
/// It belongs to the standalone Chemistry Lab project and has no Physics Lab dependency.
/// </summary>
public static class ChemistryLabBuilder
{
    private const string TargetScenePath = "Assets/ChemistryLab.unity";
    private const string GeneratedRootName = "__ChemistryLab_Generated__";
    private const string TemporarySceneFolder = "Assets/__VLABAtomicBuildTemp";
    private const string DaniellDefinitionPath = "Assets/VLABChemistryLab/Experiments/PinDaniell.asset";
    private const string ElectrolysisDefinitionPath = "Assets/VLABChemistryLab/Experiments/DienPhanCuSO4.asset";
    private const string XrRigPrefabPath = "Assets/VLABChemistryLab/Prefabs/XR/VLAB XR Origin.prefab";
    private const string XrSimulatorPrefabPath = "Assets/VLABChemistryLab/Prefabs/XR/VLAB XR Interaction Simulator.prefab";
    private static GameObject desktopCameraObject;
    private static GameObject xrOriginObject;
    private static Camera xrCameraComponent;
    private static GameObject xrInteractionManagerObject;
    private static GameObject xrInteractionSimulatorObject;

    // A restrained lab palette: neutral room surfaces make instructional colours easy to read.
    private static readonly Color FloorColor = new Color(0.16f, 0.19f, 0.22f);
    private static readonly Color WallColor = new Color(0.82f, 0.86f, 0.87f);
    private static readonly Color BenchColor = new Color(0.20f, 0.25f, 0.27f);
    private static readonly Color SteelColor = new Color(0.58f, 0.64f, 0.66f);
    private static readonly Color GlassColor = new Color(0.76f, 0.91f, 0.96f);
    private static readonly Color NaohColor = new Color(0.16f, 0.60f, 0.89f);
    private static readonly Color VinegarColor = new Color(0.92f, 0.56f, 0.18f);
    private static readonly Color IndicatorColor = new Color(0.84f, 0.16f, 0.43f);

    private enum InjectedFailurePoint
    {
        None,
        Preflight,
        Build,
        Validation,
        Swap
    }

    [MenuItem("VLAB/Build Chemistry Lab")]
    public static void BuildChemistryLab()
    {
        if (Application.isBatchMode) { BuildChemistryLabBatch(); return; }
        string selected = EditorUtility.OpenFolderPanel("Choose a recovery checkpoint matching the saved scene", "", "");
        if (string.IsNullOrEmpty(selected)) return;
        BuildChemistryLabInternal(new CheckpointContext(Path.GetFileName(selected.TrimEnd('/', '\\')), selected), InjectedFailurePoint.None);
    }

    // Suitable for: Unity.exe -executeMethod ChemistryLabBuilder.BuildChemistryLabBatch
    public static void BuildChemistryLabBatch()
    {
        BuildChemistryLabInternal(ReadCheckpointFromCommandLine(), InjectedFailurePoint.None);
    }

    /// <summary>
    /// Test-only entry point. It deliberately exercises the production transaction against
    /// the real target scene while allowing failure before each irreversible boundary.
    /// </summary>
    internal static void BuildChemistryLabForTests(string checkpointId, string checkpointRoot, string failurePoint)
    {
        if (!Enum.TryParse(failurePoint, true, out InjectedFailurePoint injectedFailure))
            throw new InvalidOperationException("Unknown injected failure point: " + failurePoint);
        BuildChemistryLabInternal(new CheckpointContext(checkpointId, checkpointRoot), injectedFailure);
    }

    private static void BuildChemistryLabInternal(CheckpointContext checkpoint, InjectedFailurePoint injectedFailure)
    {
        ValidatePreflight(checkpoint);
        ThrowIfInjected(injectedFailure, InjectedFailurePoint.Preflight);

        string temporaryScenePath = TemporarySceneFolder + "/ChemistryLab." + Guid.NewGuid().ToString("N") + ".unity";
        string originalScenePath = SceneManager.GetActiveScene().path;
        string backupPath = Path.Combine(ProjectRoot, "Library", "VLABAtomicBuild", "ChemistryLab." + Guid.NewGuid().ToString("N") + ".bak");
        bool swapped = false;
        try
        {
            EnsureAssetFolder(TemporarySceneFolder);
            if (!AssetDatabase.CopyAsset(TargetScenePath, temporaryScenePath))
                throw new InvalidOperationException("Could not create the temporary ChemistryLab scene.");
            AssetDatabase.ImportAsset(temporaryScenePath, ImportAssetOptions.ForceSynchronousImport);

            Scene temporaryScene = EditorSceneManager.OpenScene(temporaryScenePath, OpenSceneMode.Single);
            BuildSceneContents(temporaryScene, temporaryScenePath, injectedFailure);
            ChemistryLabEditorValidation.ValidateOpenScene(temporaryScene);
            ThrowIfInjected(injectedFailure, InjectedFailurePoint.Validation);
            EditorSceneManager.SaveScene(temporaryScene, temporaryScenePath);

            ThrowIfInjected(injectedFailure, InjectedFailurePoint.Swap);
            SwapValidatedScene(temporaryScenePath, backupPath);
            swapped = true;
            EnsureBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
            DeleteIfExists(backupPath);
            Debug.Log("[VLAB] ChemistryLab atomically created and saved at " + TargetScenePath + " using checkpoint " + checkpoint.Id + ".");
        }
        catch
        {
            if (swapped)
                RestoreBackup(backupPath);
            throw;
        }
        finally
        {
            CleanupTemporaryScene(temporaryScenePath);
            RestoreOriginalScene(originalScenePath);
        }
    }

    private static void BuildSceneContents(Scene scene, string scenePath, InjectedFailurePoint injectedFailure)
    {
        desktopCameraObject = null;
        xrOriginObject = null;
        xrCameraComponent = null;
        xrInteractionManagerObject = null;
        xrInteractionSimulatorObject = null;
        RemoveGeneratedContent();
        EnsurePlayableShell();
        PrepareDesktopExperience();
        ConfigureLighting();

        GameObject root = new GameObject(GeneratedRootName);
        Undo.RegisterCreatedObjectUndo(root, "Build Chemistry Lab");
        Child(root.transform, "__ChemistryLab_Generated_v10");

        BuildRoom(root.transform);
        ThrowIfInjected(injectedFailure, InjectedFailurePoint.Build);
        BuildBenches(root.transform);

        Component lessonController = AddRuntimeComponent(root, "TitrationLessonController");
        SetFloatValue(lessonController, .01f, "dropDoseMl");
        SetFloatValue(lessonController, .10f, "fastDoseMl");
        ChemistryLabHandsOnBuilder.Build(root.transform, (TitrationLessonController)lessonController);
        BuildReagentStation(root.transform);
        BuildPpeStation(root.transform);
        BuildSink(root.transform);
        BuildSafetySigns(root.transform);
        BuildLearningConsole(root.transform, lessonController);

        ChemistryExperimentDefinition daniellDefinition = EnsureDaniellDefinition();
        ChemistryExperimentDefinition electrolysisDefinition = EnsureElectrolysisDefinition();
        Component daniellController = AddRuntimeComponent(root, "ConfigurableExperimentController");
        Component electrolysisController = AddRuntimeComponent(root, "ConfigurableExperimentController");
        SetObjectReference(daniellController, daniellDefinition, "definition");
        SetObjectReference(electrolysisController, electrolysisDefinition, "definition");
        BuildDaniellStation(root.transform, daniellController);
        BuildElectrolysisStation(root.transform, electrolysisController);

        Component lessonHub = AddRuntimeComponent(root, "ChemistryLabLessonHub");
        SetObjectReference(lessonHub, lessonController, "titration");
        SetObjectReference(lessonHub, daniellController, "daniellCell");
        SetObjectReference(lessonHub, electrolysisController, "copperElectrolysis");

        Component bridge = AddRuntimeComponent(root, "ChemistryControllerBridge");
        SetObjectReference(bridge, lessonController, "controller", "lessonController", "experiment");

        Component desktopInterface = AddRuntimeComponent(root, "DesktopTitrationInterface");
        SetObjectReference(desktopInterface, lessonController, "controller", "lessonController", "experiment");
        SetObjectReference(desktopInterface, lessonHub, "lessonHub");

        Component modeController = AddRuntimeComponent(root, "ChemistryLabModeController");
        SetObjectReference(modeController, desktopCameraObject, "desktopCamera");
        SetObjectReference(modeController, desktopInterface, "desktopInterface");
        SetObjectReference(modeController, xrOriginObject, "xrOrigin");
        SetObjectReference(modeController, xrCameraComponent, "xrCamera");
        SetObjectReference(modeController, xrInteractionManagerObject, "xrInteractionManager");
        SetObjectReference(modeController, xrInteractionSimulatorObject, "xrInteractionSimulator");

        EditorUtility.SetDirty(root);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, scenePath);
    }

    private static CheckpointContext ReadCheckpointFromCommandLine()
    {
        string[] arguments = Environment.GetCommandLineArgs();
        string checkpointId = ReadCommandLineValue(arguments, "-vlabCheckpointId");
        string checkpointRoot = ReadCommandLineValue(arguments, "-vlabCheckpointRoot");
        if (string.IsNullOrWhiteSpace(checkpointId) || string.IsNullOrWhiteSpace(checkpointRoot))
        {
            throw new InvalidOperationException(
                "Atomic build requires -vlabCheckpointId <id> and -vlabCheckpointRoot <absolute path>. " +
                "Create a recovery checkpoint whose Assets/ChemistryLab.unity hash matches the target first.");
        }
        return new CheckpointContext(checkpointId, checkpointRoot);
    }

    private static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;

    private static string ReadCommandLineValue(string[] arguments, string name)
    {
        for (int index = 0; index < arguments.Length - 1; index++)
        {
            if (string.Equals(arguments[index], name, StringComparison.OrdinalIgnoreCase))
                return arguments[index + 1];
        }
        return null;
    }

    private static void ValidatePreflight(CheckpointContext checkpoint)
    {
        if (checkpoint == null || string.IsNullOrWhiteSpace(checkpoint.Id) || string.IsNullOrWhiteSpace(checkpoint.Root))
            throw new InvalidOperationException("A non-empty checkpoint ID and root are required.");
        if (!Regex.IsMatch(checkpoint.Id, "^[A-Za-z0-9][A-Za-z0-9._-]{7,127}$"))
            throw new InvalidOperationException("Checkpoint ID must be 8-128 URL-safe characters.");
        string checkpointRoot = Path.GetFullPath(checkpoint.Root);
        if (!Directory.Exists(checkpointRoot) || !string.Equals(Path.GetFileName(checkpointRoot), checkpoint.Id, StringComparison.Ordinal))
            throw new InvalidOperationException("Checkpoint root is missing or does not match its explicit checkpoint ID.");

        string targetScene = Path.Combine(ProjectRoot, TargetScenePath.Replace('/', Path.DirectorySeparatorChar));
        string checkpointScene = Path.Combine(checkpointRoot, TargetScenePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(targetScene) || !File.Exists(checkpointScene))
            throw new InvalidOperationException("Both target and checkpoint ChemistryLab scenes must exist before an atomic build.");
        if (!string.Equals(GetSha256(targetScene), GetSha256(checkpointScene), StringComparison.Ordinal))
            throw new InvalidOperationException("Checkpoint ChemistryLab scene hash does not match the target scene.");

        for (int index = 0; index < SceneManager.sceneCount; index++)
        {
            Scene loadedScene = SceneManager.GetSceneAt(index);
            if (loadedScene.isDirty)
                throw new InvalidOperationException("Save or discard changes in all open scenes before an atomic ChemistryLab build.");
        }

        AssertBuiltInRenderer();
        AssertRuntimeContract();
    }

    private static void AssertBuiltInRenderer()
    {
        if (UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline != null ||
            UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null)
            throw new InvalidOperationException("Atomic build is blocked because a Scriptable Render Pipeline is active.");

        int originalQuality = QualitySettings.GetQualityLevel();
        try
        {
            for (int index = 0; index < QualitySettings.names.Length; index++)
            {
                QualitySettings.SetQualityLevel(index, false);
                if (QualitySettings.renderPipeline != null)
                    throw new InvalidOperationException("Atomic build is blocked because a Quality level enables a Scriptable Render Pipeline.");
            }
        }
        finally
        {
            QualitySettings.SetQualityLevel(originalQuality, false);
        }
    }

    private static void AssertRuntimeContract()
    {
        AssertMonoBehaviourType("TitrationLessonController", "dropDoseMl", "fastDoseMl");
        AssertMonoBehaviourType("ConfigurableExperimentController", "definition");
        AssertMonoBehaviourType("ChemistryLabLessonHub", "titration", "daniellCell", "copperElectrolysis");
        AssertMonoBehaviourType("ChemistryControllerBridge", "controller", "lessonController", "experiment");
        AssertMonoBehaviourType("DesktopTitrationInterface", "controller", "lessonController", "experiment", "lessonHub");
        AssertMonoBehaviourType("ChemistryLabModeController", "desktopCamera", "desktopInterface", "xrOrigin", "xrCamera", "xrInteractionManager", "xrInteractionSimulator");

        foreach (string assetPath in new[] { DaniellDefinitionPath, ElectrolysisDefinitionPath })
        {
            if (AssetDatabase.LoadAssetAtPath<ChemistryExperimentDefinition>(assetPath) == null)
                throw new InvalidOperationException("Required experiment definition is missing: " + assetPath);
        }
        foreach (string prefabPath in new[] { XrRigPrefabPath, XrSimulatorPrefabPath })
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
                throw new InvalidOperationException("Required VLAB presentation prefab is missing: " + prefabPath);
        }
    }

    private static void AssertMonoBehaviourType(string shortTypeName, params string[] propertyNames)
    {
        Type type = TypeCache.GetTypesDerivedFrom<MonoBehaviour>()
            .SingleOrDefault(candidate => candidate.Name == shortTypeName && !candidate.IsAbstract);
        if (type == null)
            throw new InvalidOperationException("Required runtime component is not uniquely compiled: " + shortTypeName);

        Scene preview = EditorSceneManager.NewPreviewScene();
        GameObject probe = new GameObject("__VLABAtomicPreflightProbe__");
        SceneManager.MoveGameObjectToScene(probe, preview);
        try
        {
            Component component = probe.AddComponent(type);
            SerializedObject serialized = new SerializedObject(component);
            bool hasExpectedProperty = propertyNames.Any(name => serialized.FindProperty(name) != null);
            if (!hasExpectedProperty)
                throw new InvalidOperationException("Required serialized contract is missing on " + shortTypeName + ".");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(probe);
            EditorSceneManager.ClosePreviewScene(preview);
        }
    }

    private static void ThrowIfInjected(InjectedFailurePoint actual, InjectedFailurePoint expected)
    {
        if (actual == expected)
            throw new InvalidOperationException("Injected atomic Builder failure at " + expected + ".");
    }

    private static void EnsureAssetFolder(string assetFolder)
    {
        if (AssetDatabase.IsValidFolder(assetFolder))
            return;
        string parent = Path.GetDirectoryName(assetFolder)?.Replace('\\', '/');
        string leaf = Path.GetFileName(assetFolder);
        if (string.IsNullOrWhiteSpace(parent) || !AssetDatabase.IsValidFolder(parent))
            throw new InvalidOperationException("Cannot create temporary scene folder: " + assetFolder);
        AssetDatabase.CreateFolder(parent, leaf);
    }

    private static void SwapValidatedScene(string temporaryScenePath, string backupPath)
    {
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        AssetDatabase.SaveAssets();
        Directory.CreateDirectory(Path.GetDirectoryName(backupPath));
        string temporaryFullPath = Path.Combine(ProjectRoot, temporaryScenePath.Replace('/', Path.DirectorySeparatorChar));
        string targetFullPath = Path.Combine(ProjectRoot, TargetScenePath.Replace('/', Path.DirectorySeparatorChar));
        File.Replace(temporaryFullPath, targetFullPath, backupPath, true);
        DeleteIfExists(temporaryFullPath + ".meta");
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
    }

    private static void RestoreBackup(string backupPath)
    {
        if (!File.Exists(backupPath))
            throw new InvalidOperationException("Atomic Builder could not restore its target-scene backup: " + backupPath);
        string targetFullPath = Path.Combine(ProjectRoot, TargetScenePath.Replace('/', Path.DirectorySeparatorChar));
        File.Replace(backupPath, targetFullPath, null, true);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
    }

    private static void CleanupTemporaryScene(string temporaryScenePath)
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(temporaryScenePath) != null)
            AssetDatabase.DeleteAsset(temporaryScenePath);
        string temporaryMeta = Path.Combine(ProjectRoot, temporaryScenePath.Replace('/', Path.DirectorySeparatorChar)) + ".meta";
        DeleteIfExists(temporaryMeta);
        string temporaryFolder = Path.Combine(ProjectRoot, TemporarySceneFolder.Replace('/', Path.DirectorySeparatorChar));
        if (Directory.Exists(temporaryFolder) && !Directory.EnumerateFileSystemEntries(temporaryFolder).Any())
        {
            Directory.Delete(temporaryFolder);
            DeleteIfExists(temporaryFolder + ".meta");
        }
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
    }

    private static void RestoreOriginalScene(string originalScenePath)
    {
        if (!string.IsNullOrWhiteSpace(originalScenePath) && File.Exists(Path.Combine(ProjectRoot, originalScenePath.Replace('/', Path.DirectorySeparatorChar))))
            EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
        else
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }

    private static string GetSha256(string path)
    {
        using (FileStream stream = File.OpenRead(path))
        using (SHA256 algorithm = SHA256.Create())
            return BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-", string.Empty);
    }

    private sealed class CheckpointContext
    {
        internal CheckpointContext(string id, string root)
        {
            Id = id;
            Root = root;
        }

        internal string Id { get; }
        internal string Root { get; }
    }

    private static void RemoveGeneratedContent()
    {
        foreach (string rootName in new[] { GeneratedRootName })
        {
            GameObject oldRoot = GameObject.Find(rootName);
            if (oldRoot != null)
                UnityEngine.Object.DestroyImmediate(oldRoot);
        }

        // These are Starter Assets showcase objects, not part of the reusable XR rig.
        string[] demoNames =
        {
            "Demo Environment", "Grab Interactable Table", "Far Grab Interactable Table",
            "Gaze Interactables", "Gaze Interactable Info", "Poke Interactions Table",
            "Poke Interactions Info", "Far Grab Interactable Info", "Grab Interactable Info",
            "Interactables Sample", "Poke Interactions Sample", "Far Grab Samples",
            "model", "model (1)", "Table"
        };
        foreach (string objectName in demoNames)
        {
            GameObject demo = GameObject.Find(objectName);
            if (demo != null)
                UnityEngine.Object.DestroyImmediate(demo);
        }
    }

    private static void EnsurePlayableShell()
    {
        if (UnityEngine.Object.FindAnyObjectByType<Camera>() == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetPositionAndRotation(new Vector3(0f, 1.65f, -3.2f), Quaternion.identity);
            cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
        }

        if (UnityEngine.Object.FindAnyObjectByType<Light>() == null)
        {
            GameObject lightObject = new GameObject("Directional Light");
            lightObject.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
        }
    }

    private static void PrepareDesktopExperience()
    {
        foreach (string objectName in new[]
        {
            "XR Origin (XR Rig)", "VLAB XR Origin", "XR Device Simulator",
            "XR Interaction Simulator", "VLAB XR Interaction Simulator"
        })
        {
            GameObject stale = FindSceneObject(objectName);
            if (stale != null)
                UnityEngine.Object.DestroyImmediate(stale);
        }

        GameObject rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(XrRigPrefabPath);
        xrOriginObject = PrefabUtility.InstantiatePrefab(rigPrefab) as GameObject;
        if (xrOriginObject != null) VLABP1RigAssetBuilder.ConfigureDirectInputs(xrOriginObject);
        if (xrOriginObject == null)
            throw new InvalidOperationException("Could not instantiate the VLAB XR Origin prefab.");
        xrOriginObject.name = "VLAB XR Origin";
        xrOriginObject.transform.SetPositionAndRotation(new Vector3(0f, 0f, -2.6f), Quaternion.identity);
        xrOriginObject.transform.localScale = Vector3.one;
        xrCameraComponent = xrOriginObject.GetComponentInChildren<Camera>(true);
        if (xrCameraComponent == null)
            throw new InvalidOperationException("VLAB XR Origin has no tracked camera.");

        GameObject simulatorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(XrSimulatorPrefabPath);
        xrInteractionSimulatorObject = PrefabUtility.InstantiatePrefab(simulatorPrefab) as GameObject;
        if (xrInteractionSimulatorObject == null)
            throw new InvalidOperationException("Could not instantiate the VLAB XR Interaction Simulator prefab.");
        xrInteractionSimulatorObject.name = "VLAB XR Interaction Simulator";
        xrInteractionSimulatorObject.SetActive(false);

        xrInteractionManagerObject = FindSceneObject("XR Interaction Manager");
        if (xrInteractionManagerObject == null)
        {
            xrInteractionManagerObject = new GameObject("XR Interaction Manager");
            if (AddRuntimeComponent(xrInteractionManagerObject, "XRInteractionManager") == null)
                throw new InvalidOperationException("XR Interaction Manager component is unavailable.");
        }

        xrOriginObject.SetActive(false);
        xrInteractionManagerObject.SetActive(false);
        GameObject teleportEnvironment = FindSceneObject("Teleportation Environment");
        if (teleportEnvironment != null) teleportEnvironment.SetActive(false);

        desktopCameraObject = FindSceneObject("ChemistryLab Desktop Camera");
        if (desktopCameraObject == null)
        {
            desktopCameraObject = new GameObject("ChemistryLab Desktop Camera");
            desktopCameraObject.AddComponent<Camera>();
            desktopCameraObject.AddComponent<AudioListener>();
        }

        Camera desktopCamera = desktopCameraObject.GetComponent<Camera>();
        desktopCameraObject.tag = "MainCamera";
        desktopCameraObject.transform.position = new Vector3(0f, 1.70f, -6.4f);
        desktopCameraObject.transform.LookAt(new Vector3(0f, 1.25f, .8f));
        desktopCamera.fieldOfView = 55f;
        desktopCamera.allowHDR = true;
        desktopCamera.allowMSAA = true;
        desktopCamera.clearFlags = CameraClearFlags.SolidColor;
        desktopCamera.backgroundColor = new Color(.10f, .16f, .22f);
        desktopCamera.enabled = true;
        CharacterController desktopBody = desktopCameraObject.GetComponent<CharacterController>();
        if (desktopBody == null) desktopBody = desktopCameraObject.AddComponent<CharacterController>();
        desktopBody.height = 1.70f;
        desktopBody.radius = .22f;
        desktopBody.center = new Vector3(0f, -.85f, 0f);
        desktopBody.stepOffset = .25f;
        desktopBody.skinWidth = .03f;
        desktopBody.slopeLimit = 45f;
        if (desktopCameraObject.GetComponent<DesktopLabNavigator>() == null)
            desktopCameraObject.AddComponent<DesktopLabNavigator>();
        var retiredHands = desktopCameraObject.GetComponent<DesktopLabHands>();
        if (retiredHands != null) UnityEngine.Object.DestroyImmediate(retiredHands);

        foreach (Camera camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include))
        {
            bool activeDesktopCamera = camera == desktopCamera;
            camera.enabled = activeDesktopCamera;
            camera.gameObject.tag = activeDesktopCamera ? "MainCamera" : "Untagged";
        }
        foreach (AudioListener listener in UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include))
            listener.enabled = listener.gameObject == desktopCameraObject;
    }

    private static void ConfigureLighting()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.48f, 0.62f, 0.74f);
        RenderSettings.ambientEquatorColor = new Color(0.34f, 0.39f, 0.42f);
        RenderSettings.ambientGroundColor = new Color(0.15f, 0.18f, 0.20f);
        RenderSettings.ambientIntensity = .85f;
        RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Custom;
        RenderSettings.fog = false;
        QualitySettings.SetQualityLevel(QualitySettings.names.Length - 1, true);
        QualitySettings.antiAliasing = 8;
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
        QualitySettings.shadowDistance = 80f;
        QualitySettings.shadowResolution = ShadowResolution.VeryHigh;

        Light directionalLight = UnityEngine.Object.FindObjectsByType<Light>()
            .FirstOrDefault(light => light.type == LightType.Directional);
        if (directionalLight != null)
        {
            directionalLight.color = new Color(1f, 0.98f, 0.94f);
            directionalLight.intensity = 1.10f;
            directionalLight.shadows = LightShadows.Soft;
        }
    }

    private static void BuildRoom(Transform root)
    {
        Transform room = Child(root, "ChemistryLabRoom").transform;
        Primitive(PrimitiveType.Cube, room, "Floor", new Vector3(0f, -0.08f, 0f), new Vector3(16f, 0.16f, 16f), FloorColor);
        Primitive(PrimitiveType.Cube, room, "NorthWall", new Vector3(0f, 2.40f, 7.9f), new Vector3(16f, 4.8f, 0.12f), WallColor);
        Primitive(PrimitiveType.Cube, room, "WestWall", new Vector3(-7.9f, 2.40f, 0f), new Vector3(0.12f, 4.8f, 16f), WallColor);
        Primitive(PrimitiveType.Cube, room, "EastWall", new Vector3(7.9f, 2.40f, 0f), new Vector3(0.12f, 4.8f, 16f), WallColor);
        Primitive(PrimitiveType.Cube, room, "Ceiling", new Vector3(0f, 4.78f, 0f), new Vector3(16f, .08f, 16f), new Color(.68f, .74f, .77f));
        for (int i = -6; i <= 6; i++)
            Primitive(PrimitiveType.Cube, room, "FloorTileLine", new Vector3(i * 1.2f, .01f, 0f), new Vector3(.018f, .01f, 15.6f), new Color(.28f, .33f, .35f));
        for (int i = -6; i <= 6; i++)
            Primitive(PrimitiveType.Cube, room, "FloorTileLine", new Vector3(0f, .012f, i * 1.2f), new Vector3(15.6f, .01f, .018f), new Color(.28f, .33f, .35f));
        foreach (float x in new[] { -4.0f, 0f, 4.0f })
        {
            Primitive(PrimitiveType.Cube, room, "CeilingLightPanel", new Vector3(x, 4.68f, .5f), new Vector3(1.7f, .035f, .60f), new Color(.86f, .95f, 1f));
            CreatePointLight(room, new Vector3(x, 3.85f, .5f));
        }

        CreatePointLight(room, new Vector3(-4.8f, 3.6f, 3.8f));
        CreatePointLight(room, new Vector3(4.8f, 3.6f, 3.8f));
    }

    private static void BuildBenches(Transform root)
    {
        Transform furniture = Child(root, "LabFurniture").transform;
        CreateBench(furniture, "MainTitrationBench", new Vector3(0f, 0f, 0.5f), new Vector3(3.4f, 0.10f, 1.25f));
        CreateBench(furniture, "ReagentBench", new Vector3(-4.7f, 0f, 5.20f), new Vector3(2.4f, 0.10f, 0.70f));
        CreateBench(furniture, "SafetyBench", new Vector3(4.7f, 0f, 5.20f), new Vector3(2.4f, 0.10f, 0.70f));
    }

    private static void CreateBench(Transform parent, string name, Vector3 position, Vector3 topScale)
    {
        Transform bench = Child(parent, name).transform;
        bench.localPosition = position;
        Primitive(PrimitiveType.Cube, bench, "Top", new Vector3(0f, 0.90f, 0f), topScale, BenchColor);
        float x = topScale.x * 0.44f;
        float z = topScale.z * 0.38f;
        foreach (Vector3 p in new[] { new Vector3(-x, .43f, -z), new Vector3(x, .43f, -z), new Vector3(-x, .43f, z), new Vector3(x, .43f, z) })
            Primitive(PrimitiveType.Cube, bench, "Leg", p, new Vector3(.08f, .86f, .08f), SteelColor);
    }

    private static void BuildTitrationStation(Transform root, Component controller)
    {
        Transform station = Child(root, "TitrationStation").transform;

        // Retort stand and burette, centred at comfortable standing reach.
        Primitive(PrimitiveType.Cylinder, station, "StandBase", new Vector3(0f, .99f, .58f), new Vector3(.28f, .025f, .28f), SteelColor);
        Primitive(PrimitiveType.Cylinder, station, "StandRod", new Vector3(.28f, 1.75f, .58f), new Vector3(.025f, .78f, .025f), SteelColor);
        Primitive(PrimitiveType.Cube, station, "BuretteClamp", new Vector3(.12f, 2.18f, .58f), new Vector3(.34f, .05f, .05f), SteelColor);
        GameObject burette = Primitive(PrimitiveType.Cylinder, station, "Burette_50mL_NaOH", new Vector3(0f, 1.82f, .58f), new Vector3(.045f, .62f, .045f), GlassColor);
        GameObject buretteLiquid = Primitive(PrimitiveType.Cylinder, burette.transform, "NaOH_Liquid", new Vector3(0f, -.03f, 0f), new Vector3(.72f, .83f, .72f), NaohColor);
        Primitive(PrimitiveType.Cube, station, "Stopcock", new Vector3(0f, 1.17f, .58f), new Vector3(.18f, .04f, .04f), IndicatorColor);
        Primitive(PrimitiveType.Cylinder, station, "BuretteTip", new Vector3(0f, 1.07f, .58f), new Vector3(.012f, .10f, .012f), GlassColor);

        // A round body + neck gives a readable conical-flask silhouette using primitives only.
        GameObject flask = Child(station, "ErlenmeyerFlask_250mL");
        flask.transform.localPosition = new Vector3(0f, .99f, .58f);
        // Keep the original compact flask silhouette; only the liquid level is raised for classroom visibility.
        Primitive(PrimitiveType.Sphere, flask.transform, "FlaskBody", new Vector3(0f, .13f, 0f), new Vector3(.26f, .20f, .26f), GlassColor);
        Primitive(PrimitiveType.Cylinder, flask.transform, "FlaskNeck", new Vector3(0f, .31f, 0f), new Vector3(.065f, .16f, .065f), GlassColor);
        // A lower half-sphere reads as a real liquid volume through the transparent flask,
        // rather than the flat cylinder silhouette used previously.
        GameObject flaskLiquid = Primitive(PrimitiveType.Sphere, flask.transform, "VinegarWithIndicator", new Vector3(0f, .075f, 0f), new Vector3(.214f, .150f, .214f), new Color(.96f, .72f, .78f));
        Primitive(PrimitiveType.Cylinder, flask.transform, "LiquidMeniscus", new Vector3(0f, .150f, 0f), new Vector3(.202f, .008f, .202f), new Color(1f, .82f, .90f));

        SetObjectReference(controller, burette, "burette", "buretteObject");
        SetObjectReference(controller, buretteLiquid.GetComponent<Renderer>(), "buretteLiquidRenderer", "buretteRenderer");
        SetObjectReference(controller, flask, "flask", "flaskObject");
        SetObjectReference(controller, flaskLiquid.GetComponent<Renderer>(), "flaskLiquidRenderer", "liquidRenderer");
    }

    private static void BuildReagentStation(Transform root)
    {
        Transform reagents = Child(root, "ReagentsAndGlassware").transform;
        Vector3 p = new Vector3(-4.70f, 1.0f, 3.85f);
        Primitive(PrimitiveType.Cube, reagents, "ReagentCart", p + new Vector3(0f, -.06f, 0f), new Vector3(1.55f, .12f, .62f), new Color(.13f, .18f, .20f));
        CreateBottle(reagents, "NaOH_0.100M", p + new Vector3(-.46f, .06f, 0f), NaohColor, "NaOH 0.100 M");
        CreateBottle(reagents, "DilutedVinegar", p + new Vector3(-.06f, .06f, 0f), VinegarColor, "GIẤM PHA LOÃNG");
        CreateBottle(reagents, "Phenolphthalein", p + new Vector3(.34f, .04f, 0f), IndicatorColor, "PHENOLPHTHALEIN");
        var handsOn = root.GetComponentInChildren<VLAB.ChemistryLab.Interaction.HandsOnTitration>();
        var pipette = ChemistryLabHandsOnBuilder.Vessel(handsOn, "ReagentPipette_10mL",
            VLAB.ChemistryLab.Interaction.LabVesselRole.Pipette, VLAB.ChemistryLab.Interaction.LabReagent.None,
            10, 0, p + new Vector3(.28f, .025f, -.24f), .28f, .009f, .009f, true, false);
        pipette.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
    }

    private static void BuildDaniellStation(Transform root, Component controller)
    {
        Transform station = Child(root, "DaniellCellStation").transform;
        Vector3 p = new Vector3(-4.70f, 1.05f, 5.20f);
        Primitive(PrimitiveType.Cube, station, "DaniellTray", p, new Vector3(1.95f, .05f, .62f), new Color(.12f, .17f, .20f));
        GameObject zinc = Primitive(PrimitiveType.Cylinder, station, "ZnSO4_Beaker", p + new Vector3(-.43f, .14f, 0f), new Vector3(.22f, .14f, .22f), GlassColor);
        Primitive(PrimitiveType.Cylinder, zinc.transform, "ZnSolution", new Vector3(0f, -.10f, 0f), new Vector3(.82f, .25f, .82f), new Color(.74f, .82f, .88f));
        Primitive(PrimitiveType.Cube, zinc.transform, "ZnElectrode", new Vector3(0f, .18f, 0f), new Vector3(.05f, .38f, .16f), new Color(.48f, .55f, .60f));
        GameObject copper = Primitive(PrimitiveType.Cylinder, station, "CuSO4_Beaker", p + new Vector3(.43f, .14f, 0f), new Vector3(.22f, .14f, .22f), GlassColor);
        Primitive(PrimitiveType.Cylinder, copper.transform, "CuSO4Solution", new Vector3(0f, -.10f, 0f), new Vector3(.82f, .25f, .82f), new Color(.05f, .42f, .86f));
        Primitive(PrimitiveType.Cube, copper.transform, "CuElectrode", new Vector3(0f, .18f, 0f), new Vector3(.05f, .38f, .16f), new Color(.72f, .32f, .12f));
        Primitive(PrimitiveType.Cylinder, station, "SaltBridge", p + new Vector3(0f, .36f, 0f), new Vector3(.05f, .42f, .05f), GlassColor).transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        CreateStationLabel(station, "DaniellLabel", "PIN DANIELL\nZn | Zn2+ || Cu2+ | Cu", p + new Vector3(0f, .58f, -.35f), new Color(.05f, .20f, .36f));
        BuildGenericConsole(station, p + new Vector3(0f, .08f, -.48f), "Daniell", controller);
    }

    private static void BuildElectrolysisStation(Transform root, Component controller)
    {
        Transform station = Child(root, "CopperElectrolysisStation").transform;
        Vector3 p = new Vector3(4.70f, 1.05f, 5.20f);
        Primitive(PrimitiveType.Cube, station, "ElectrolysisTray", p, new Vector3(1.95f, .05f, .62f), new Color(.12f, .17f, .20f));
        GameObject cell = Primitive(PrimitiveType.Cube, station, "ElectrolysisCell", p + new Vector3(-.15f, .16f, 0f), new Vector3(.55f, .25f, .36f), GlassColor);
        Primitive(PrimitiveType.Cube, cell.transform, "CuSO4Solution", new Vector3(0f, -.28f, 0f), new Vector3(.88f, .35f, .82f), new Color(.06f, .40f, .84f));
        Primitive(PrimitiveType.Cube, cell.transform, "Anode", new Vector3(-.18f, .12f, 0f), new Vector3(.035f, .46f, .22f), new Color(.22f, .24f, .27f));
        Primitive(PrimitiveType.Cube, cell.transform, "Cathode", new Vector3(.18f, .12f, 0f), new Vector3(.035f, .46f, .22f), new Color(.72f, .32f, .12f));
        GameObject power = Primitive(PrimitiveType.Cube, station, "DC_PowerSupply", p + new Vector3(.62f, .17f, 0f), new Vector3(.35f, .25f, .38f), new Color(.08f, .12f, .16f));
        Label(station, "Voltage", "6.0 V", p + new Vector3(.62f, .21f, -.20f), .055f, new Color(.20f, 1f, .55f), 0f);
        CreateStationLabel(station, "ElectrolysisLabel", "ĐIỆN PHÂN\nCuSO4", p + new Vector3(0f, .58f, -.35f), new Color(.38f, .20f, .03f));
        BuildGenericConsole(station, p + new Vector3(0f, .08f, -.48f), "Electrolysis", controller);
    }

    private static void BuildGenericConsole(Transform parent, Vector3 position, string prefix, Component controller)
    {
        string[] labels = { "BƯỚC", "GHI", "RESET" };
        Color[] colors = { new Color(.12f, .52f, .78f), new Color(.94f, .55f, .12f), new Color(.72f, .25f, .25f) };
        CreateControlTable(parent, prefix, position);
        for (int i = 0; i < labels.Length; i++)
        {
            GameObject button = Primitive(PrimitiveType.Cube, parent, prefix + "_" + labels[i], position + new Vector3(-.36f + i * .36f, 0f, 0f), new Vector3(.28f, .08f, .20f), colors[i]);
            Component control = AddRuntimeComponent(button, "ConfigurableExperimentControlButton");
            SetObjectReference(control, controller, "controller");
            SetEnumValue(control, i, "command");
            CompactTopButtonText(parent, prefix + "_FloatingText_" + labels[i], labels[i], position + new Vector3(-.36f + i * .36f, .045f, 0f), colors[i]);
        }
    }

    // A dedicated miniature table gives each three-button experiment console a
    // clear physical surface, instead of leaving controls visually suspended.
    private static void CreateControlTable(Transform parent, string prefix, Vector3 buttonPosition)
    {
        Vector3 topPosition = buttonPosition + new Vector3(0f, -.068f, 0f);
        Primitive(PrimitiveType.Cube, parent, prefix + "ControlTableTop", topPosition,
            new Vector3(1.28f, .050f, .42f), new Color(.09f, .14f, .17f));
        foreach (float x in new[] { -.52f, .52f })
            Primitive(PrimitiveType.Cube, parent, prefix + "ControlTableLeg", topPosition + new Vector3(x, -.085f, .13f),
                new Vector3(.045f, .14f, .045f), SteelColor);
    }

    private static void BuildPpeStation(Transform root)
    {
        Transform ppe = Child(root, "PersonalProtectiveEquipment").transform;
        Vector3 p = new Vector3(4.55f, .98f, 3.85f);
        Primitive(PrimitiveType.Cube, ppe, "PPECart", p + new Vector3(0f, -.07f, 0f), new Vector3(1.55f, .13f, .62f), new Color(.13f, .18f, .20f));
        GameObject tray = Primitive(PrimitiveType.Cube, ppe, "PPETray", p, new Vector3(1.15f, .04f, .50f), SteelColor);
        Primitive(PrimitiveType.Cylinder, tray.transform, "SafetyGoggles_Left", new Vector3(-.20f, .08f, 0f), new Vector3(.12f, .035f, .08f), GlassColor);
        Primitive(PrimitiveType.Cylinder, tray.transform, "SafetyGoggles_Right", new Vector3(.08f, .08f, 0f), new Vector3(.12f, .035f, .08f), GlassColor);
        Primitive(PrimitiveType.Capsule, tray.transform, "NitrileGloves", new Vector3(.35f, .08f, 0f), new Vector3(.10f, .06f, .10f), new Color(.25f, .55f, .90f));
        Label(ppe, "PPELabel", "KÍNH + GĂNG TAY", p + new Vector3(-.35f, .08f, -.28f), .075f, Color.white, 0f);
    }

    private static void BuildSink(Transform root)
    {
        Transform sink = Child(root, "EmergencyWashStation").transform;
        Primitive(PrimitiveType.Cube, sink, "SinkCabinet", new Vector3(6.10f, .48f, 2.20f), new Vector3(1.0f, .95f, .75f), BenchColor);
        Primitive(PrimitiveType.Cube, sink, "SinkBasin", new Vector3(6.10f, 1.00f, 2.20f), new Vector3(.82f, .08f, .58f), SteelColor);
        Primitive(PrimitiveType.Cylinder, sink, "Faucet", new Vector3(6.10f, 1.22f, 2.40f), new Vector3(.035f, .22f, .035f), SteelColor);
        CreateTabletopLabel(sink, "WashLabel", "RỬA\nKHẨN CẤP", new Vector3(6.10f, 1.065f, 1.91f), new Vector3(.66f, .014f, .22f), new Color(.05f, .35f, .65f));
    }

    private static void BuildSafetySigns(Transform root)
    {
        Transform signs = Child(root, "SafetySigns").transform;
        CreateWallSign(signs, new Vector3(-4.4f, 2.55f, 7.81f), "BẮT BUỘC ĐEO KÍNH\nVÀ GĂNG TAY", new Color(.10f, .42f, .78f));
        CreateWallSign(signs, new Vector3(4.4f, 2.55f, 7.81f), "NaOH: CHẤT ĂN MÒN\nRỬA NGAY KHI TIẾP XÚC", new Color(.88f, .52f, .10f));
        CreateWallSign(signs, new Vector3(0f, 3.55f, 7.81f), "VLAB | CHUẨN ĐỘ AXIT–BAZƠ", new Color(.08f, .28f, .34f));
    }

    private static void BuildLearningConsole(Transform root, Component controller)
    {
        Transform console = Child(root, "TitrationLearningConsole").transform;
        // Keep generous separation between controls: the label is a world-space aid,
        // so nine controls on a compact strip makes captions overlap at desktop scale.
        Primitive(PrimitiveType.Cube, console, "ConsoleBase", new Vector3(0f, .96f, -.55f), new Vector3(6.15f, .12f, .52f), new Color(.04f, .12f, .15f));

        string[] labels = { "PPE", "RỬA/NẠP", "PIPET", "CHỈ THỊ", "+0.10", "+0.01", "GHI", "RESET", "XÓA" };
        string[] floatingLabels = { "PPE", "RỬA\nNẠP", "LẤY\nMẪU", "CHỈ\nTHỊ", "+.1", "+.01", "GHI", "LÀM\nLẠI", "XÓA" };
        Color[] colors = { new Color(.15f, .55f, .85f), new Color(.15f, .55f, .85f), new Color(.15f, .55f, .85f), IndicatorColor, new Color(.20f, .72f, .42f), new Color(.32f, .78f, .50f), new Color(.94f, .55f, .12f), new Color(.72f, .25f, .25f), new Color(.50f, .28f, .58f) };
        for (int i = 0; i < labels.Length; i++)
        {
            float x = -2.60f + i * .65f;
            GameObject button = Primitive(PrimitiveType.Cube, console, "Button_" + labels[i], new Vector3(x, 1.06f, -.55f), new Vector3(.54f, .08f, .28f), colors[i]);
            Component control = AddRuntimeComponent(button, "ChemistryLabControlButton");
            SetObjectReference(control, controller, "controller", "lessonController", "experiment");
            SetEnumValue(control, i, "command", "action");
        }

        // Floating 3D text sits independently above each physical button; it is not scaled with the button.
        for (int i = 0; i < labels.Length; i++)
        {
            float x = -2.60f + i * .65f;
            FloatingButtonText(console, "FloatingText_" + labels[i], floatingLabels[i], new Vector3(x, 1.105f, -.55f), colors[i]);
        }
    }

    private static void CreateBottle(Transform parent, string name, Vector3 position, Color liquidColor, string label)
    {
        var owner = parent.parent.GetComponentInChildren<VLAB.ChemistryLab.Interaction.HandsOnTitration>();
        var reagent = name.StartsWith("NaOH") ? VLAB.ChemistryLab.Interaction.LabReagent.NaOH :
            name == "DilutedVinegar" ? VLAB.ChemistryLab.Interaction.LabReagent.DilutedVinegar : VLAB.ChemistryLab.Interaction.LabReagent.Indicator;
        var bottle = ChemistryLabHandsOnBuilder.Vessel(owner, name, VLAB.ChemistryLab.Interaction.LabVesselRole.Bottle,
            reagent, 250, 200, new Vector3(position.x, 1.005f, position.z), .23f, .065f, .027f, true, true);
        CreateBottleSticker(parent, name, new Vector3(position.x, 1.12f, position.z), label);
        parent.Find(name + "Sticker").SetParent(bottle.transform, true);
        parent.Find(name + "StickerText").SetParent(bottle.transform, true);
    }

    private static void CreateBottleSticker(Transform parent, string bottleName, Vector3 bottlePosition, string label)
    {
        // The back face intersects the bottle by a fraction of a millimetre,
        // making the decal read as attached rather than as a floating card.
        Vector3 stickerPosition = bottlePosition + new Vector3(0f, .01f, -.076f);
        Primitive(PrimitiveType.Cube, parent, bottleName + "Sticker", stickerPosition, new Vector3(.145f, .105f, .007f), new Color(.94f, .97f, 1f));
        TextMesh text = Label(parent, bottleName + "StickerText", ShortBottleLabel(label), stickerPosition + new Vector3(0f, 0f, -.006f), .032f, new Color(.02f, .05f, .09f), 0f);
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
    }

    private static string ShortBottleLabel(string label)
    {
        if (label.StartsWith("NaOH")) return "NaOH\n0.10 M";
        if (label.StartsWith("GIẤM")) return "GIẤM\nPHA";
        if (label.StartsWith("PHENOL")) return "PP\nCHỈ THỊ";
        return label;
    }

    private static void CreateTabletopLabel(Transform parent, string name, string message, Vector3 position, Vector3 panelScale, Color panelColor)
    {
        Primitive(PrimitiveType.Cube, parent, name + "Panel", position, panelScale, panelColor);
        TextMesh text = Label(parent, name, message, position + Vector3.up * .010f, .055f, Color.white, 90f);
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
    }

    private static void CreateStationLabel(Transform parent, string name, string message, Vector3 position, Color panelColor)
    {
        Primitive(PrimitiveType.Cube, parent, name + "Panel", position, new Vector3(1.55f, .34f, .035f), panelColor);
        TextMesh text = Label(parent, name, message, position + new Vector3(0f, 0f, -.025f), .105f, Color.white, 0f);
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
    }

    private static void CreateWallSign(Transform parent, Vector3 position, string message, Color color)
    {
        Primitive(PrimitiveType.Cube, parent, "SafetySign", position, new Vector3(1.7f, .62f, .05f), color);
        TextMesh text = Label(parent, "SafetySignText", message, position + new Vector3(0f, 0f, -.035f), .10f, Color.white, 0f);
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
    }

    private static void CreatePointLight(Transform parent, Vector3 position)
    {
        GameObject lightObject = Child(parent, "LabLight");
        lightObject.transform.localPosition = position;
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 7f;
        light.intensity = 1.6f;
        light.color = new Color(.92f, .97f, 1f);
        light.shadows = LightShadows.Soft;
    }

    private static GameObject Primitive(PrimitiveType type, Transform parent, string name, Vector3 position, Vector3 scale, Color color)
    {
        GameObject item = GameObject.CreatePrimitive(type);
        item.name = name;
        if (name == "FloorTileLine") item.GetComponent<Collider>().enabled = false;
        item.transform.SetParent(parent, false);
        item.transform.localPosition = position;
        item.transform.localScale = scale;
        Renderer renderer = item.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = CreateMaterial(color);
        return item;
    }

    private static Material CreateMaterial(Color color)
    {
        // This project currently uses the Built-in Render Pipeline. Prefer Standard so
        // materials never fall back to Unity's magenta "missing URP shader" colour.
        Shader shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            throw new InvalidOperationException("Neither Standard nor URP/Lit shader is available.");
        Material material = new Material(shader) { color = color };
        material.name = "Chemistry_" + ColorUtility.ToHtmlStringRGBA(color);
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        float smoothness = color == SteelColor ? .78f : color == GlassColor ? .88f : .52f;
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);
        if (material.HasProperty("_Metallic"))
            material.SetFloat("_Metallic", color == SteelColor ? .72f : 0f);
        if (color == GlassColor)
        {
            Color transparentGlass = new Color(color.r, color.g, color.b, .34f);
            material.color = transparentGlass;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", transparentGlass);
            material.SetFloat("_Mode", 3f);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
        }
        return material;
    }

    private static ChemistryExperimentDefinition EnsureDaniellDefinition()
    {
        ChemistryExperimentDefinition definition = AssetDatabase.LoadAssetAtPath<ChemistryExperimentDefinition>(DaniellDefinitionPath);
        if (definition != null) return definition;
        definition = ScriptableObject.CreateInstance<ChemistryExperimentDefinition>();
        definition.experimentId = "daniell-cell";
        definition.displayName = "Pin Daniell";
        definition.learningObjective = "Lắp pin điện hóa Zn-Cu và giải thích chiều chuyển electron, cầu muối và suất điện động.";
        definition.safetyNote = "Đeo kính/găng tay; không để dung dịch muối kim loại tiếp xúc da hoặc đổ ra bàn.";
        definition.requirements.AddRange(new[] { "Nhận diện cực Zn, cực Cu và dung dịch tương ứng.", "Dự đoán electron đi từ Zn sang Cu.", "Đo và ghi suất điện động gần 1,10 V." });
        definition.procedureSteps.AddRange(new[] { "Mang PPE và kiểm tra hai cốc điện cực.", "Đặt thanh Zn vào ZnSO4 và thanh Cu vào CuSO4.", "Nối cầu muối và dây dẫn với vôn kế.", "Đọc điện áp ổn định và ghi chiều chuyển electron." });
        definition.expectedObservation = "Vôn kế đọc xấp xỉ 1,10 V; Zn là anode, Cu là cathode.";
        definition.resultLabel = "Suất điện động";
        definition.targetResult = "≈ 1,10 V";
        CreateDefinitionAsset(definition, DaniellDefinitionPath);
        return definition;
    }

    private static ChemistryExperimentDefinition EnsureElectrolysisDefinition()
    {
        ChemistryExperimentDefinition definition = AssetDatabase.LoadAssetAtPath<ChemistryExperimentDefinition>(ElectrolysisDefinitionPath);
        if (definition != null) return definition;
        definition = ScriptableObject.CreateInstance<ChemistryExperimentDefinition>();
        definition.experimentId = "copper-electrolysis";
        definition.displayName = "Điện phân CuSO4";
        definition.learningObjective = "Quan sát sự điện phân dung dịch CuSO4 với điện cực trơ và dự đoán sản phẩm ở mỗi điện cực.";
        definition.safetyNote = "Đeo kính/găng tay; chỉ thao tác nguồn điện áp thấp và không chạm điện cực khi đang cấp điện.";
        definition.requirements.AddRange(new[] { "Lắp đúng anode/cathode.", "Dùng nguồn DC 6,0 V an toàn.", "Ghi màu dung dịch và lớp đồng bám ở cathode." });
        definition.procedureSteps.AddRange(new[] { "Mang PPE và kiểm tra nguồn điện áp thấp.", "Rót CuSO4 vào bình điện phân.", "Đặt hai điện cực trơ, nối đúng cực nguồn.", "Bật 6,0 V, quan sát đồng bám cathode và hiện tượng tại anode." });
        definition.expectedObservation = "Cathode có lớp đồng đỏ nâu; dung dịch xanh nhạt dần và anode có bọt khí.";
        definition.resultLabel = "Hiện tượng";
        definition.targetResult = "Cu bám cathode";
        CreateDefinitionAsset(definition, ElectrolysisDefinitionPath);
        return definition;
    }

    private static void CreateDefinitionAsset(ChemistryExperimentDefinition definition, string path)
    {
        string folder = System.IO.Path.GetDirectoryName(path)?.Replace("\\", "/");
        if (!string.IsNullOrEmpty(folder) && !AssetDatabase.IsValidFolder(folder))
        {
            string parent = System.IO.Path.GetDirectoryName(folder)?.Replace("\\", "/");
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(folder));
        }
        AssetDatabase.CreateAsset(definition, path);
    }

    private static GameObject Child(Transform parent, string name)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);
        return child;
    }

    private static GameObject FindSceneObject(string name)
    {
        Transform result = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include)
            .FirstOrDefault(candidate => candidate.name == name && candidate.gameObject.scene.IsValid());
        return result == null ? null : result.gameObject;
    }

    private static TextMesh Label(Transform parent, string name, string value, Vector3 localPosition, float worldTextSize, Color color, float xRotation)
    {
        GameObject textObject = Child(parent, name);
        textObject.transform.localPosition = localPosition;
        textObject.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        TextMesh text = textObject.AddComponent<TextMesh>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = value;
        text.fontSize = 36;
        // Existing builder values (.05-.13) describe visual world scale, not TMP points.
        text.characterSize = worldTextSize * .22f;
        text.color = color;
        text.fontStyle = FontStyle.Bold;
        text.anchor = TextAnchor.MiddleLeft;
        text.alignment = TextAlignment.Left;
        Renderer renderer = textObject.GetComponent<Renderer>();
        if (renderer != null && renderer.sharedMaterial != null)
        {
            Material material = new Material(renderer.sharedMaterial);
            renderer.sharedMaterial = material;
            if (material.HasProperty("_Color")) material.color = color;
            renderer.sortingOrder = 10;
        }
        return text;
    }

    private static void FloatingButtonText(Transform parent, string name, string value, Vector3 localPosition, Color color)
    {
        // White face plus a deep navy outline is legible on every coloured button.
        // Text lies on the top face, where users look while reaching for a control.
        Color outlineColor = new Color(.01f, .025f, .06f);
        CreateFloatingTextLayer(parent, name + "_Outline", value, localPosition + Vector3.up * .002f, outlineColor, .022f, .40f, 90f, false);
        GameObject core = CreateFloatingTextLayer(parent, name, value, localPosition + Vector3.up * .004f, Color.white, .018f, .20f, 90f, false);

        Light glow = core.AddComponent<Light>();
        glow.type = LightType.Point;
        glow.color = Color.Lerp(color, Color.white, .35f);
        glow.range = .34f;
        glow.intensity = .42f;
        glow.shadows = LightShadows.None;
    }

    private static void CompactTopButtonText(Transform parent, string name, string value, Vector3 localPosition, Color buttonColor)
    {
        // A vertical label at the rear edge is readable from the normal standing
        // position in front of the bench, unlike text laid flat on the button.
        Vector3 uprightPosition = localPosition + new Vector3(0f, .105f, .115f);
        Color glowColor = Color.Lerp(buttonColor, Color.white, .30f);
        CreateFloatingTextLayer(parent, name + "_Outline", value, uprightPosition + new Vector3(0f, 0f, .002f), glowColor, .018f, .85f, 0f, false);
        GameObject core = CreateFloatingTextLayer(parent, name, value, uprightPosition, Color.white, .014f, .55f, 0f, false);
        Light glow = core.AddComponent<Light>();
        glow.type = LightType.Point;
        glow.color = glowColor;
        glow.range = .30f;
        glow.intensity = .24f;
        glow.shadows = LightShadows.None;
    }

    private static GameObject CreateFloatingTextLayer(Transform parent, string name, string value, Vector3 localPosition, Color color, float characterSize, float emissionStrength, float xRotation, bool billboard)
    {
        GameObject textObject = Child(parent, name);
        textObject.transform.localPosition = localPosition;
        textObject.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        TextMesh text = textObject.AddComponent<TextMesh>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = value;
        text.fontSize = 36;
        text.characterSize = characterSize;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.color = color;
        if (billboard) textObject.AddComponent<FloatingLabelBillboard>();
        Renderer renderer = textObject.GetComponent<Renderer>();
        if (renderer != null && renderer.sharedMaterial != null)
        {
            Material material = new Material(renderer.sharedMaterial);
            renderer.sharedMaterial = material;
            if (material.HasProperty("_Color")) material.color = color;
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * emissionStrength);
            }
            renderer.sortingOrder = 20;
        }
        return textObject;
    }

    private static Component AddRuntimeComponent(GameObject target, string shortTypeName)
    {
        Type type = TypeCache.GetTypesDerivedFrom<MonoBehaviour>()
            .FirstOrDefault(candidate => candidate.Name == shortTypeName && !candidate.IsAbstract);
        if (type == null)
        {
            Debug.LogWarning($"[VLAB] Runtime component {shortTypeName} is not compiled yet; rebuild the scene after scripts import.");
            return null;
        }
        return target.AddComponent(type);
    }

    private static void SetObjectReference(Component component, UnityEngine.Object value, params string[] propertyNames)
    {
        if (component == null || value == null)
            return;
        SerializedObject serialized = new SerializedObject(component);
        foreach (string propertyName in propertyNames)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null || property.propertyType != SerializedPropertyType.ObjectReference)
                continue;
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return;
        }
    }

    private static void SetEnumValue(Component component, int value, params string[] propertyNames)
    {
        if (component == null)
            return;
        SerializedObject serialized = new SerializedObject(component);
        foreach (string propertyName in propertyNames)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null || property.propertyType != SerializedPropertyType.Enum)
                continue;
            property.enumValueIndex = Mathf.Clamp(value, 0, Math.Max(0, property.enumNames.Length - 1));
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return;
        }
    }

    private static void SetFloatValue(Component component, float value, params string[] propertyNames)
    {
        if (component == null)
            return;
        SerializedObject serialized = new SerializedObject(component);
        foreach (string propertyName in propertyNames)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null || property.propertyType != SerializedPropertyType.Float)
                continue;
            property.floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return;
        }
    }

    private static void EnsureBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
        foreach (string scenePath in new[] { TargetScenePath })
        {
            EditorBuildSettingsScene existing = scenes.FirstOrDefault(item => item.path == scenePath);
            if (existing == null)
                scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            else
                existing.enabled = true;
        }
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
