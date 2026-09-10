using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using Unity.XR.CoreUtils;
using VLAB.ChemistryLab;

public static class VLABP1RigAssetBuilder
{
    public const string RigPrefabPath = "Assets/VLABChemistryLab/Prefabs/XR/VLAB XR Origin.prefab";
    public const string SimulatorPrefabPath = "Assets/VLABChemistryLab/Prefabs/XR/VLAB XR Interaction Simulator.prefab";
    public const string RigActionsPath = "Assets/VLABChemistryLab/Input/VLAB XRI Default Input Actions.inputactions";
    public const string SimulatorActionsPath = "Assets/VLABChemistryLab/Input/VLAB XR Interaction Simulator Controls.inputactions";

    private const string SourceRigPath = "Assets/Samples/XR Interaction Toolkit/3.5.1/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";
    private const string SourceRigActionsPath = "Assets/Samples/XR Interaction Toolkit/3.5.1/Starter Assets/XRI Default Input Actions.inputactions";
    private const string SourceSimulatorPath = "Assets/Samples/XR Interaction Toolkit/3.5.1/XR Interaction Simulator/XR Interaction Simulator.prefab";
    private const string SourceSimulatorActionsPath = "Assets/Samples/XR Interaction Toolkit/3.5.1/XR Interaction Simulator/XR Interaction Simulator Controls.inputactions";

    [MenuItem("VLAB/Setup/Use Package XR Device Simulator")]
    public static void UsePackageDeviceSimulator()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play mode before changing the simulator.");
        const string samplePath = "Assets/Samples/XR Interaction Toolkit/3.5.1/XR Device Simulator/XR Device Simulator.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(samplePath) == null)
        {
            EditorUtility.DisplayDialog("Import XR Device Simulator", "Window > Package Manager > XR Interaction Toolkit > Samples > XR Device Simulator > Import. Then run this menu again.", "OK");
            return;
        }
        // Preserve the old owned prefab before replacing it; keep its GUID for scene references.
        string backup = "Assets/VLABChemistryLab/Prefabs/XR/Simulator Backup " + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + ".prefab";
        if (!AssetDatabase.CopyAsset(SimulatorPrefabPath, backup))
            throw new InvalidOperationException("Could not back up the existing simulator; no replacement performed.");
        SaveOwnedPrefab(samplePath, SimulatorPrefabPath, "VLAB XR Interaction Simulator",
            new Dictionary<UnityEngine.Object, UnityEngine.Object>(), null);
        AssetDatabase.SaveAssets();
        Debug.Log("[VLAB] Package XR Device Simulator installed. Old prefab saved at " + backup +
            ". Rebuild via VLAB / Build Chemistry Lab with a matching checkpoint, then set Default Request to XRSimulator before Play.");
    }

    public static void InstallDeviceSimulatorAndBuildBatch()
    {
        UsePackageDeviceSimulator();
        ChemistryLabBuilder.BuildChemistryLabBatch();
    }

    [MenuItem("VLAB/Upgrade/Phase 1/Create Owned XR Rig Assets")]
    public static void CreateOwnedAssets()
    {
        EnsureFolder("Assets/VLABChemistryLab/Prefabs");
        EnsureFolder("Assets/VLABChemistryLab/Prefabs/XR");
        EnsureFolder("Assets/VLABChemistryLab/Input");

        CopyOrReplace(SourceRigActionsPath, RigActionsPath);
        CopyOrReplace(SourceSimulatorActionsPath, SimulatorActionsPath);

        InputActionAsset sourceRigActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(SourceRigActionsPath);
        InputActionAsset ownedRigActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(RigActionsPath);
        InputActionAsset sourceSimulatorActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(SourceSimulatorActionsPath);
        InputActionAsset ownedSimulatorActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(SimulatorActionsPath);

        SaveOwnedPrefab(SourceRigPath, RigPrefabPath, "VLAB XR Origin",
            new Dictionary<UnityEngine.Object, UnityEngine.Object> { { sourceRigActions, ownedRigActions } }, ConfigureRig);
        SaveOwnedPrefab(SourceSimulatorPath, SimulatorPrefabPath, "VLAB XR Interaction Simulator",
            new Dictionary<UnityEngine.Object, UnityEngine.Object> { { sourceSimulatorActions, ownedSimulatorActions } }, null);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        ValidateOwnedAssets();
        Debug.Log("[VLAB][P1.d1] Project-owned XR rig, simulator and primary input-action assets created and validated.");
    }

    public static void CreateOwnedAssetsBatch()
    {
        try
        {
            CreateOwnedAssets();
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    private static void ConfigureRig(GameObject root)
    {
        root.transform.localScale = Vector3.one;
        XROrigin origin = root.GetComponent<XROrigin>();
        if (origin == null)
            throw new InvalidOperationException("The canonical rig source has no XROrigin component.");
        origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
        origin.CameraYOffset = 1.65f;

        AddControllerContracts(root, "Left Controller", XRNode.LeftHand);
        AddControllerContracts(root, "Right Controller", XRNode.RightHand);
        ConfigureDirectInputs(root);
    }

    public static void ConfigureDirectInputs(GameObject root)
    {
        var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(RigActionsPath);
        foreach (var direct in root.GetComponentsInChildren<XRDirectInteractor>(true))
        {
            string side = direct.transform.parent.name.Contains("Left") ? "Left" : "Right";
            string map = "XRI " + side + " Interaction/";
            direct.selectInput = new XRInputButtonReader
            {
                inputSourceMode = XRInputButtonReader.InputSourceMode.InputAction,
                inputActionPerformed = actions.FindAction(map + "Select", true).Clone(),
                inputActionValue = actions.FindAction(map + "Select Value", true).Clone()
            };
            direct.activateInput = new XRInputButtonReader
            {
                inputSourceMode = XRInputButtonReader.InputSourceMode.InputAction,
                inputActionPerformed = actions.FindAction(map + "Activate", true).Clone(),
                inputActionValue = actions.FindAction(map + "Activate Value", true).Clone()
            };
            EditorUtility.SetDirty(direct);
            if (PrefabUtility.IsPartOfPrefabInstance(direct)) PrefabUtility.RecordPrefabInstancePropertyModifications(direct);
        }
    }

    private static void AddControllerContracts(GameObject root, string controllerName, XRNode node)
    {
        Transform controller = root.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(item => item.name == controllerName);
        if (controller == null)
            throw new InvalidOperationException("Canonical rig is missing " + controllerName + ".");

        XRTrackedControllerGuard guard = controller.GetComponent<XRTrackedControllerGuard>();
        if (guard == null) guard = controller.gameObject.AddComponent<XRTrackedControllerGuard>();
        guard.Configure(node);

        Transform direct = controller.Find("VLAB Direct Interactor");
        if (direct == null)
        {
            GameObject directObject = new GameObject("VLAB Direct Interactor");
            directObject.transform.SetParent(controller, false);
            SphereCollider collider = directObject.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = .075f;
            XRDirectInteractor interactor = directObject.AddComponent<XRDirectInteractor>();
            interactor.improveAccuracyWithSphereCollider = true;
        }
    }

    private static void SaveOwnedPrefab(
        string sourcePath,
        string destinationPath,
        string rootName,
        IReadOnlyDictionary<UnityEngine.Object, UnityEngine.Object> replacements,
        Action<GameObject> configure)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath) == null)
            throw new InvalidOperationException("Required XRI sample prefab is missing: " + sourcePath);
        GameObject contents = PrefabUtility.LoadPrefabContents(sourcePath);
        try
        {
            contents.name = rootName;
            RewireObjectReferences(contents, replacements);
            configure?.Invoke(contents);
            PrefabUtility.SaveAsPrefabAsset(contents, destinationPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }

    private static void RewireObjectReferences(
        GameObject root,
        IReadOnlyDictionary<UnityEngine.Object, UnityEngine.Object> replacements)
    {
        foreach (Component component in root.GetComponentsInChildren<Component>(true))
        {
            if (component == null) continue;
            SerializedObject serialized = new SerializedObject(component);
            SerializedProperty property = serialized.GetIterator();
            bool enterChildren = true;
            bool changed = false;
            while (property.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (property.propertyType != SerializedPropertyType.ObjectReference || property.objectReferenceValue == null)
                    continue;
                if (!replacements.TryGetValue(property.objectReferenceValue, out UnityEngine.Object replacement) || replacement == null)
                    continue;
                property.objectReferenceValue = replacement;
                changed = true;
            }
            if (changed) serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void CopyOrReplace(string source, string destination)
    {
        if (AssetDatabase.LoadMainAssetAtPath(source) == null)
            throw new InvalidOperationException("Required XRI sample asset is missing: " + source);
        if (AssetDatabase.LoadMainAssetAtPath(destination) != null && !AssetDatabase.DeleteAsset(destination))
            throw new InvalidOperationException("Could not replace owned asset: " + destination);
        if (!AssetDatabase.CopyAsset(source, destination))
            throw new InvalidOperationException("Could not copy owned asset: " + destination);
        AssetDatabase.ImportAsset(destination, ImportAssetOptions.ForceSynchronousImport);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = path.Substring(0, path.LastIndexOf('/'));
        string leaf = path.Substring(path.LastIndexOf('/') + 1);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }

    public static void ValidateOwnedAssets()
    {
        GameObject rig = AssetDatabase.LoadAssetAtPath<GameObject>(RigPrefabPath);
        GameObject simulator = AssetDatabase.LoadAssetAtPath<GameObject>(SimulatorPrefabPath);
        if (rig == null || simulator == null)
            throw new InvalidOperationException("Owned rig or simulator prefab is missing.");
        if (rig.transform.localScale != Vector3.one)
            throw new InvalidOperationException("VLAB XR Origin scale must be 1,1,1.");
        XROrigin origin = rig.GetComponent<XROrigin>();
        if (origin == null || origin.RequestedTrackingOriginMode != XROrigin.TrackingOriginMode.Floor || origin.CameraYOffset < 1f)
            throw new InvalidOperationException("VLAB XR Origin must prefer Floor tracking and retain a Device-mode camera offset fallback.");
        if (rig.GetComponentsInChildren<XRDirectInteractor>(true).Length != 2)
            throw new InvalidOperationException("VLAB XR Origin must have one direct interactor per controller.");
        if (rig.GetComponentsInChildren<XRTrackedControllerGuard>(true).Length != 2)
            throw new InvalidOperationException("VLAB XR Origin must guard both controllers against tracking loss.");
        if (!simulator.GetComponentsInChildren<MonoBehaviour>(true).Any(item => item != null && (item.GetType().Name == "XRInteractionSimulator" || item.GetType().Name == "XRDeviceSimulator")))
            throw new InvalidOperationException("VLAB simulator prefab does not contain XRInteractionSimulator.");
    }
}
