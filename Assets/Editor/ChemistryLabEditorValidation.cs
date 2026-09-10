using System;
using System.Linq;
using VLAB.ChemistryLab;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Editor-side end-to-end verification of the desktop lesson flow.</summary>
internal static class ChemistryLabEditorValidation
{
    /// <summary>Verifies the current production scene without invoking the checkpoint-gated builder.</summary>
    internal static void BuildAndValidate()
    {
        Validate();
    }

    [MenuItem("VLAB/Validate Desktop Chemistry Lab")]
    internal static void Validate()
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/ChemistryLab.unity", OpenSceneMode.Single);
        ValidateOpenScene(scene);
    }

    /// <summary>Validates the supplied, already-open scene without reopening the production target.</summary>
    internal static void ValidateOpenScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            throw new InvalidOperationException("ChemistryLab validation requires a loaded scene.");
        GameObject root = GameObject.Find("__ChemistryLab_Generated__");
        if (root == null) throw new InvalidOperationException("ChemistryLab generated root is missing.");
        if (root.GetComponent<DesktopTitrationInterface>() == null)
            throw new InvalidOperationException("DesktopTitrationInterface is missing from ChemistryLab.");
        ChemistryLabModeController modeController = root.GetComponent<ChemistryLabModeController>();
        if (modeController == null)
            throw new InvalidOperationException("ChemistryLabModeController is missing from ChemistryLab.");
        if (GameObject.Find("ChemistryLab Desktop Camera")?.GetComponent<Camera>() == null)
            throw new InvalidOperationException("Desktop camera is missing from ChemistryLab.");
        GameObject xrOrigin = FindSceneObject("VLAB XR Origin");
        GameObject xrSimulator = FindSceneObject("VLAB XR Interaction Simulator");
        if (xrOrigin == null || xrSimulator == null)
            throw new InvalidOperationException("Project-owned VLAB XR Origin or XR Interaction Simulator is missing.");
        if (xrOrigin.transform.lossyScale != Vector3.one)
            throw new InvalidOperationException("VLAB XR Origin must keep a 1,1,1 world scale.");
        MonoBehaviour[] xrBehaviours = xrOrigin.GetComponentsInChildren<MonoBehaviour>(true);
        int directInteractors = 0;
        int trackingGuards = 0;
        foreach (MonoBehaviour behaviour in xrBehaviours)
        {
            if (behaviour == null) continue;
            if (behaviour.GetType().Name == "XRDirectInteractor") directInteractors++;
            if (behaviour is XRTrackedControllerGuard) trackingGuards++;
        }
        if (directInteractors != 2 || trackingGuards != 2)
            throw new InvalidOperationException("VLAB XR Origin needs two direct interactors and two tracking-loss guards.");
        if (!xrSimulator.GetComponentsInChildren<MonoBehaviour>(true)
            .Any(behaviour => behaviour != null && (behaviour.GetType().Name == "XRInteractionSimulator" || behaviour.GetType().Name == "XRDeviceSimulator")))
            throw new InvalidOperationException("VLAB simulator wrapper is missing XRInteractionSimulator.");
        GameObject floor = GameObject.Find("Floor");
        if (floor == null || floor.transform.lossyScale.x < 15f || floor.transform.lossyScale.z < 15f)
            throw new InvalidOperationException("ChemistryLab must provide the expanded 16 m movement floor.");
        GameObject desktopCamera = GameObject.Find("ChemistryLab Desktop Camera");
        if (desktopCamera.GetComponent<DesktopLabNavigator>() == null)
            throw new InvalidOperationException("Desktop camera is missing navigation.");
        CharacterController desktopBody = desktopCamera.GetComponent<CharacterController>();
        if (desktopBody == null || desktopBody.height < 1.6f || desktopBody.center.y >= 0f)
            throw new InvalidOperationException("Desktop movement must use a grounded CharacterController below the eye camera.");
        if (desktopCamera.GetComponent<DesktopLabNavigator>() == null)
            throw new InvalidOperationException("Desktop movement safety controller is missing.");
        if (QualitySettings.antiAliasing < 8)
            throw new InvalidOperationException("ChemistryLab must use 8x MSAA for readable desktop and simulated-VR rendering.");
        ChemistryLabLessonHub hub = root.GetComponent<ChemistryLabLessonHub>();
        if (hub == null) throw new InvalidOperationException("Lesson hub is missing from ChemistryLab.");
        TextMesh[] worldLabels = root.GetComponentsInChildren<TextMesh>(true);
        if (worldLabels.Length < 24)
            throw new InvalidOperationException("ChemistryLab must generate readable world-space labels for controls and stations.");
        int topFaceButtonLabels = 0;
        foreach (TextMesh label in worldLabels)
        {
            if (label.font == null || string.IsNullOrWhiteSpace(label.text))
                throw new InvalidOperationException("A generated world-space label is missing its Unicode font or content.");
            Vector3 inheritedScale = label.transform.lossyScale;
            if (Mathf.Abs(inheritedScale.x - inheritedScale.y) > .01f || Mathf.Abs(inheritedScale.y - inheritedScale.z) > .01f)
                throw new InvalidOperationException("World-space text must not inherit a non-uniform scale from a button, bottle, or sign.");
            if (Mathf.Abs(Mathf.DeltaAngle(label.transform.eulerAngles.y, 180f)) < 1f)
                throw new InvalidOperationException("World-space text must not be rotated backward.");
            if (label.text.Contains("₄") || label.text.Contains("⁺"))
                throw new InvalidOperationException("Generated labels must use glyph-safe chemistry notation such as CuSO4 and Zn2+.");
            if (label.gameObject.name.StartsWith("FloatingText_"))
            {
                topFaceButtonLabels++;
                if (label.gameObject.name.EndsWith("_Outline"))
                {
                    if (label.characterSize < .022f)
                        throw new InvalidOperationException("Every control label needs a thick outline layer.");
                }
                else if (label.characterSize < .018f || label.color.grayscale < .8f)
                    throw new InvalidOperationException("Control labels must use large, high-contrast white text on the button top face.");
            }
        }
        if (topFaceButtonLabels != 18)
            throw new InvalidOperationException("ChemistryLab must generate two high-contrast label layers for each of nine controls.");
        if (root.GetComponentsInChildren<TMPro.TextMeshPro>(true).Length != 0)
            throw new InvalidOperationException("Generated station labels must not use TMP point sizes for world-scale text.");
        if (root.transform.Find("__ChemistryLab_Generated_v10") == null)
            throw new InvalidOperationException("ChemistryLab scene is stale; the v10 hands-on generation has not completed.");
        var handsOn = root.GetComponentInChildren<VLAB.ChemistryLab.Interaction.HandsOnTitration>(true);
        if (handsOn == null || handsOn.GetComponentsInChildren<VLAB.ChemistryLab.Interaction.LabLiquidVessel>(true).Length != 11)
            throw new InvalidOperationException("Hands-on titration needs eleven wired vessels, including reagent shelf glassware.");
        if (GameObject.Find("WasteBeaker") != null)
            throw new InvalidOperationException("The redundant waste beaker must not be generated at the reagent station.");
        string[] bottleStickers = { "NaOH_0.100MSticker", "DilutedVinegarSticker", "PhenolphthaleinSticker" };
        foreach (string sticker in bottleStickers)
        {
            GameObject stickerObject = GameObject.Find(sticker);
            GameObject stickerText = GameObject.Find(sticker + "Text");
            TextMesh stickerLabel = stickerText == null ? null : stickerText.GetComponent<TextMesh>();
            if (stickerObject == null || stickerText == null || stickerLabel == null ||
                stickerObject.transform.parent != stickerText.transform.parent ||
                Vector3.Distance(stickerObject.transform.position, stickerText.transform.position) > .03f ||
                stickerLabel.characterSize < .007f)
                throw new InvalidOperationException("Each reagent must use an independent, bottle-mounted sticker label.");
        }
        foreach (string prefix in new[] { "Daniell", "Electrolysis" })
        {
            if (GameObject.Find(prefix + "ControlTableTop") == null)
                throw new InvalidOperationException("Each three-button experiment console must be placed on its own tabletop.");
            foreach (string action in new[] { "BƯỚC", "GHI", "RESET" })
            {
                Light labelGlow = GameObject.Find(prefix + "_FloatingText_" + action)?.GetComponent<Light>();
                GameObject label = GameObject.Find(prefix + "_FloatingText_" + action);
                if (labelGlow == null || labelGlow.intensity < .15f || label == null ||
                    Mathf.Abs(Mathf.DeltaAngle(label.transform.eulerAngles.x, 0f)) > 1f)
                    throw new InvalidOperationException("Each experiment button label must have a subtle glow.");
            }
        }
        GameObject washLabel = GameObject.Find("WashLabel");
        if (washLabel == null || washLabel.transform.eulerAngles.x < 80f || washLabel.transform.eulerAngles.x > 100f || washLabel.transform.position.y > 1.10f)
            throw new InvalidOperationException("The emergency-wash label must lie flat on the sink tabletop.");
        GameObject flaskLiquid = FindSceneObject("VinegarWithIndicator");
        if (flaskLiquid == null || flaskLiquid.GetComponent<SphereCollider>() == null)
            throw new InvalidOperationException("Flask liquid must be a curved lower volume, not a flat cylinder.");
        ConfigurableExperimentController[] extraLessons = root.GetComponents<ConfigurableExperimentController>();
        if (extraLessons.Length != 2)
            throw new InvalidOperationException("ChemistryLab must contain exactly two configurable senior-high-school lessons.");
        foreach (ConfigurableExperimentController lesson in extraLessons)
        {
            if (lesson.Definition == null || lesson.Definition.requirements.Count == 0 || lesson.Definition.procedureSteps.Count < 3)
                throw new InvalidOperationException("Configurable lesson is missing editable requirements or a complete procedure.");
            for (int i = 0; i < lesson.Definition.procedureSteps.Count; i++) lesson.Advance();
            lesson.RecordResult();
            if (!lesson.IsComplete || !lesson.StatusMessage.Contains(lesson.Definition.targetResult))
                throw new InvalidOperationException("Configurable lesson could not reach and record its expected result.");
            lesson.ResetExperiment();
        }

        modeController.ApplyMode(false);
        Camera[] cameras = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include);
        AudioListener[] listeners = UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include);
        int activeCameras = cameras.Count(camera => camera.enabled && camera.gameObject.activeInHierarchy);
        int mainCameraTags = cameras.Count(camera => camera.CompareTag("MainCamera"));
        int activeListeners = listeners.Count(listener => listener.enabled && listener.gameObject.activeInHierarchy);
        if (activeCameras != 1 || activeListeners != 1 || mainCameraTags != 1)
            throw new InvalidOperationException("Desktop mode must own exactly one active camera, AudioListener and MainCamera tag.");
        foreach (MonoBehaviour behaviour in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
        {
            string typeName = behaviour.GetType().FullName ?? string.Empty;
            if ((typeName.Contains("DeviceSimulator") || typeName.Contains("InteractionSimulator")) && behaviour.gameObject.activeInHierarchy)
                throw new InvalidOperationException("XR simulator UI/input must be inactive in desktop mode.");
        }

        GameObject probe = new GameObject("__ChemistryLabValidationProbe__");
        try
        {
            TitrationLessonController controller = probe.AddComponent<TitrationLessonController>();
            for (int trial = 0; trial < 3; trial++)
            {
                controller.PerformSafety();
                controller.PrepareBurette();
                controller.AddSample();
                controller.AddIndicator();
                for (int i = 0; i < 8; i++) controller.DoseCoarse();
                for (int i = 0; i < 3; i++) controller.DoseFast();
                for (int i = 0; i < 3; i++) controller.DoseDrop();
                controller.RecordResult();
                if (!controller.LastActionAccepted)
                    throw new InvalidOperationException($"Desktop workflow failed to record trial {trial + 1}.");
            }

            if (!controller.Experiment.IsComplete || controller.Experiment.Observations.Count != 3)
                throw new InvalidOperationException("Desktop workflow did not complete three concordant trials.");
            if (Math.Abs(controller.Experiment.MassVolumePercent - 5.002d) > .02d)
                throw new InvalidOperationException("Desktop workflow calculated an unexpected vinegar result.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(probe);
        }

        Debug.Log("[VLAB] ChemistryLab validation passed: responsive desktop setup, two configurable lessons, simulator overlay disabled and three concordant titration trials completed end-to-end.");
    }

    private static GameObject FindSceneObject(string name)
    {
        Transform match = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include)
            .FirstOrDefault(item => item.name == name && item.gameObject.scene.IsValid());
        return match == null ? null : match.gameObject;
    }
}
