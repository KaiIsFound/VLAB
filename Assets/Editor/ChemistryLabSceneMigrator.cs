using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Detects a stale generated ChemistryLab scene without mutating it. Rebuilding
/// is deliberately explicit so opening the project or leaving Play Mode cannot
/// overwrite user changes in the generated scene.
/// </summary>
[InitializeOnLoad]
internal static class ChemistryLabSceneMigrator
{
    private const string ScenePath = "Assets/ChemistryLab.unity";
    private const string CurrentMarkerName = "__ChemistryLab_Generated_v10";

    static ChemistryLabSceneMigrator()
    {
        EditorApplication.delayCall += ValidateLoadedChemistryScene;
        EditorSceneManager.sceneOpened += OnSceneOpened;
    }

    private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        if (scene.path == ScenePath)
            ValidateScene(scene);
    }

    [MenuItem("VLAB/Validate Chemistry Lab Generation Status")]
    private static void ValidateLoadedChemistryScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.IsValid() || !scene.isLoaded)
            return;

        ValidateScene(scene);
    }

    private static void ValidateScene(Scene scene)
    {
        GameObject root = null;
        foreach (GameObject candidate in scene.GetRootGameObjects())
        {
            if (candidate.name == "__ChemistryLab_Generated__")
            {
                root = candidate;
                break;
            }
        }

        bool isCurrent = root != null &&
            root.transform.Find(CurrentMarkerName) != null &&
            root.GetComponentsInChildren<TextMeshPro>(true).Length == 0;

        if (isCurrent)
        {
            Debug.Log("[VLAB] ChemistryLab generation marker is current (v10).");
            return;
        }

        Debug.LogError(
            "[VLAB] ChemistryLab scene is stale or incomplete. The scene was not changed automatically. " +
            "Create a recovery checkpoint, then run VLAB/Build Chemistry Lab explicitly.");
    }
}
