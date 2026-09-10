using System;
using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static class ChemistryLabVisualCapture
{
    public static void BuildValidateAndCapture()
    {
        ChemistryLabEditorValidation.BuildAndValidate();
        EditorSceneManager.OpenScene("Assets/ChemistryLab.unity", OpenSceneMode.Single);
        Camera camera = GameObject.Find("ChemistryLab Desktop Camera")?.GetComponent<Camera>();
        if (camera == null) throw new InvalidOperationException("Desktop camera is missing.");
        const int width = 1920;
        const int height = 1080;
        RenderTexture target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        Texture2D image = new Texture2D(width, height, TextureFormat.RGBA32, false);
        RenderTexture previous = RenderTexture.active;
        try
        {
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            File.WriteAllBytes(Path.GetFullPath("Logs/ChemistryLabPreview.png"), image.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = null;
            RenderTexture.active = previous;
            UnityEngine.Object.DestroyImmediate(image);
            UnityEngine.Object.DestroyImmediate(target);
        }
    }
}
