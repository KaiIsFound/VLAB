using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using VLAB.ChemistryLab;
using VLAB.ChemistryLab.Interaction;

/// <summary>Source of the reusable, metre-scale glassware models and their XR wiring.</summary>
internal static class ChemistryLabHandsOnBuilder
{
    private const string Folder = "Assets/VLABChemistryLab/Models/HandsOn";
    public static void BuildAndPreview()
    {
        ChemistryLabBuilder.BuildChemistryLabBatch();
        CapturePreview();
    }
    public static void CapturePreview()
    {
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/ChemistryLab.unity");
        var cameraObject = new GameObject("HandsOnPreviewCamera");
        var camera = cameraObject.AddComponent<Camera>();
        cameraObject.transform.position = new Vector3(1.9f, 2.0f, -2.3f);
        cameraObject.transform.LookAt(new Vector3(-.25f, 1.35f, .55f));
        camera.fieldOfView = 43; camera.nearClipPlane = .03f;
        var target = new RenderTexture(1600, 1000, 24) { antiAliasing = 8 };
        var texture = new Texture2D(1600, 1000, TextureFormat.RGB24, false);
        var previous = RenderTexture.active;
        try
        {
            camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
            texture.ReadPixels(new Rect(0, 0, 1600, 1000), 0, 0); texture.Apply();
            System.IO.Directory.CreateDirectory("Artifacts/VLABUpgrade/plan-1.4/P2.hands-on/preview");
            System.IO.File.WriteAllBytes("Artifacts/VLABUpgrade/plan-1.4/P2.hands-on/preview/kit.png", texture.EncodeToPNG());
        }
        finally
        { camera.targetTexture = null; RenderTexture.active = previous; Object.DestroyImmediate(texture); Object.DestroyImmediate(target); Object.DestroyImmediate(cameraObject); }
    }
    internal static void Build(Transform root, TitrationLessonController lesson)
    {
        EnsureFolder(Folder);
        var kit = new GameObject("HandsOnTitrationKit"); kit.transform.SetParent(root, false);
        var station = kit.AddComponent<HandsOnTitration>();
        TextMesh instructions = Label(kit.transform, "HandsOnInstructions", new Vector3(.75f, 1.75f, .9f),
            "THỰC HÀNH BẰNG DỤNG CỤ\nGrip: cầm / thả; Trigger: dùng dụng cụ\nTráng 5 mL NaOH → xả thải → nạp 50 mL\nPipette 10 mL → 2 giọt chỉ thị → chuẩn độ", .019f);
        Part(kit.transform, "InstructionsBoard", PrimitiveType.Cube, new Vector3(.75f, 1.75f, .91f),
            new Vector3(1.12f, .35f, .015f), Material("InstructionBoard", new Color(.025f, .07f, .09f)));
        station.Configure(lesson, instructions);
        Vessel(station, "HandsOn_NaOH", LabVesselRole.Bottle, LabReagent.NaOH, 250, 200, new Vector3(-1.25f, .955f, .63f), .23f, .065f, .027f, true, true);
        Vessel(station, "HandsOn_Vinegar", LabVesselRole.Bottle, LabReagent.DilutedVinegar, 250, 100, new Vector3(-.98f, .955f, .63f), .23f, .065f, .027f, true, true);
        Vessel(station, "HandsOn_Indicator", LabVesselRole.Bottle, LabReagent.Indicator, 25, 20, new Vector3(-.72f, .955f, .63f), .15f, .035f, .014f, true, true);
        var flask = Vessel(station, "ErlenmeyerFlask_250mL", LabVesselRole.Flask, LabReagent.None, 250, 0, new Vector3(0, .955f, .58f), .24f, .10f, .032f, true, false);
        Vessel(station, "HandsOn_Waste", LabVesselRole.Waste, LabReagent.None, 500, 0, new Vector3(.6f, .955f, .38f), .18f, .08f, .08f, true, false);
        var pipette = Vessel(station, "VolumetricPipette_10mL", LabVesselRole.Pipette, LabReagent.None, 10, 0,
            new Vector3(-.5f, .97f, .28f), .28f, .009f, .009f, true, false);
        pipette.transform.rotation = Quaternion.Euler(0, 0, 90);
        Part(pipette.transform, "PipetteBulb", PrimitiveType.Sphere, new Vector3(0, .24f, 0), new Vector3(.04f, .06f, .04f), Material("Rubber", new Color(.7f, .2f, .15f)));
        var burette = Vessel(station, "Burette_50mL_NaOH", LabVesselRole.Burette, LabReagent.None, 50, 0,
            new Vector3(0, 1.33f, .58f), .55f, .017f, .021f, false, false);
        var metal = Material("Steel", new Color(.35f, .4f, .44f));
        Part(kit.transform, "StandBase", PrimitiveType.Cylinder, new Vector3(.18f, .97f, .65f), new Vector3(.26f, .015f, .23f), metal);
        Part(kit.transform, "StandRod", PrimitiveType.Cylinder, new Vector3(.18f, 1.47f, .65f), new Vector3(.014f, .5f, .014f), metal);
        Part(kit.transform, "BuretteClamp", PrimitiveType.Cube, new Vector3(.09f, 1.72f, .61f), new Vector3(.21f, .024f, .065f), metal);
        Transform tip = burette.transform.Find("Outlet");
        Part(burette.transform, "BuretteTip", PrimitiveType.Cylinder, new Vector3(0, -.025f, 0), new Vector3(.008f, .025f, .008f), metal);
        var tap = Part(kit.transform, "Stopcock", PrimitiveType.Cube, new Vector3(.045f, 1.35f, .58f), new Vector3(.11f, .025f, .03f), Material("Tap", new Color(.2f, .65f, .8f)));
        tap.AddComponent<BoxCollider>();
        tap.AddComponent<XRSimpleInteractable>();
        tap.AddComponent<LabBuretteTap>().Configure(burette, tip);
        for (int i = 0; i <= 10; i++)
        {
            float y = .025f + i * .05f;
            Part(burette.transform, "Graduation_" + i, PrimitiveType.Cube, new Vector3(.014f, y, -.018f), new Vector3(.015f, .0015f, .001f), metal);
            if (i % 2 == 0) Label(burette.transform, "Scale_" + i, new Vector3(.026f, y, -.018f), (50 - i * 5).ToString(), .006f);
        }
        lesson.Configure(burette.gameObject, null, flask.gameObject, flask.transform.Find("VinegarWithIndicator").GetComponent<Renderer>());
    }
    internal static LabLiquidVessel Vessel(HandsOnTitration owner, string name, LabVesselRole role, LabReagent reagent,
        float capacity, float initial, Vector3 position, float height, float radius, float opening, bool movable, bool capped)
    {
        var go = new GameObject(name); go.transform.SetParent(owner.transform, false); go.transform.localPosition = position;
        var body = new GameObject("GlassBody"); body.transform.SetParent(go.transform, false);
        body.AddComponent<MeshFilter>().sharedMesh = GlassMesh(name, height, radius, opening);
        body.AddComponent<MeshRenderer>().sharedMaterial = Material("Glass", new Color(.7f, .88f, .93f, .22f), true);
        // Segmented walls leave the opening physically accessible to a pipette.
        for (int i = 0; i < 12; i++)
        {
            float angle = i * Mathf.PI * 2 / 12;
            var wall = new GameObject("GlassWall"); wall.transform.SetParent(go.transform, false);
            wall.transform.localPosition = new Vector3(Mathf.Sin(angle) * radius, height * .34f, Mathf.Cos(angle) * radius);
            wall.transform.localRotation = Quaternion.Euler(0, angle * Mathf.Rad2Deg, 0);
            wall.AddComponent<BoxCollider>().size = new Vector3(radius * .53f, height * .68f, .004f);
        }
        var bottom = go.AddComponent<BoxCollider>(); bottom.center = new Vector3(0, .004f, 0); bottom.size = new Vector3(radius * 1.8f, .008f, radius * 1.8f);
        Transform mouth = Anchor(go.transform, "Mouth", new Vector3(0, height, 0));
        Transform outlet = Anchor(go.transform, "Outlet", new Vector3(0, role == LabVesselRole.Burette ? -.055f : role == LabVesselRole.Pipette ? 0 : height, 0));
        Color color = new Color(.6f, .82f, .9f, .4f);
        var liquid = Part(go.transform, role == LabVesselRole.Flask ? "VinegarWithIndicator" : "Liquid", PrimitiveType.Sphere,
            new Vector3(0, height * .30f, 0), new Vector3(radius * 1.75f, height * .54f, radius * 1.75f), Material("ClearLiquid", color, true));
        if (role == LabVesselRole.Flask) liquid.AddComponent<SphereCollider>().isTrigger = true;
        GameObject cap = capped ? Part(go.transform, "Cap", PrimitiveType.Cylinder, new Vector3(0, height + .008f, 0), new Vector3(opening * 2.2f, .014f, opening * 2.2f), Material("Cap", new Color(.16f, .22f, .28f))) : null;
        Label(go.transform, "ReagentLabel", new Vector3(0, height * .6f, -radius - .003f),
            reagent == LabReagent.NaOH ? "NaOH\n0.100 M" : reagent == LabReagent.DilutedVinegar ? "GIẤM\nPHA LOÃNG" : reagent == LabReagent.Indicator ? "CHỈ THỊ" : role == LabVesselRole.Waste ? "BÌNH THẢI" : role == LabVesselRole.Flask ? "BÌNH MẪU" : role == LabVesselRole.Pipette ? "10 mL" : "NaOH / mL", .014f);
        var readout = Label(go.transform, "VolumeReadout", new Vector3(0, height * .25f, -radius - .004f), "0 mL", .01f);
        var streamObject = new GameObject("PourStream"); streamObject.transform.SetParent(go.transform, false);
        var stream = streamObject.AddComponent<LineRenderer>(); stream.positionCount = 2; stream.useWorldSpace = true;
        stream.startWidth = .003f; stream.endWidth = .002f; stream.sharedMaterial = Material("Stream", new Color(.55f, .8f, .94f)); stream.enabled = false;
        if (movable)
        {
            var rigidbody = go.AddComponent<Rigidbody>(); rigidbody.mass = role == LabVesselRole.Pipette ? .04f : .25f;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            var grab = go.AddComponent<XRGrabInteractable>(); grab.useDynamicAttach = true; grab.throwOnDetach = false;
            grab.movementType = XRBaseInteractable.MovementType.VelocityTracking;
            grab.limitLinearVelocity = true; grab.maxLinearVelocityDelta = 3f;
            grab.limitAngularVelocity = true; grab.maxAngularVelocityDelta = 6f;
            go.AddComponent<LabGrabRecovery>();
        }
        var vessel = go.AddComponent<LabLiquidVessel>();
        vessel.Configure(owner, role, reagent, capacity, initial, mouth, outlet, liquid.transform, cap, readout, stream, opening);
        return vessel;
    }
    private static Transform Anchor(Transform parent, string name, Vector3 position)
    { var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = position; return go.transform; }
    private static GameObject Part(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
    {
        var go = GameObject.CreatePrimitive(type); go.name = name; go.transform.SetParent(parent, false);
        go.transform.localPosition = position; go.transform.localScale = scale;
        Object.DestroyImmediate(go.GetComponent<Collider>()); go.GetComponent<Renderer>().sharedMaterial = material; return go;
    }
    private static TextMesh Label(Transform parent, string name, Vector3 position, string text, float size)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = position;
        var label = go.AddComponent<TextMesh>(); label.text = text; label.characterSize = size * .35f; label.fontSize = 64;
        label.anchor = TextAnchor.MiddleCenter; label.alignment = TextAlignment.Center; label.color = Color.white;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        go.GetComponent<MeshRenderer>().sharedMaterial = label.font.material; return label;
    }
    private static Material Material(string name, Color color, bool transparent = false)
    {
        string path = Folder + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;
        material = new Material(Shader.Find("Standard")) { name = name, color = color };
        material.SetFloat("_Glossiness", .7f);
        if (transparent)
        {
            material.SetFloat("_Mode", 3); material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0); material.EnableKeyword("_ALPHAPREMULTIPLY_ON"); material.renderQueue = 3000;
        }
        AssetDatabase.CreateAsset(material, path); return material;
    }
    private static Mesh GlassMesh(string name, float h, float r, float opening)
    {
        string path = Folder + "/" + name + ".asset";
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh != null) return mesh;
        const int sides = 48;
        float thickness = Mathf.Min(.002f, r * .15f);
        var profile = new[] { new Vector2(.001f, 0), new Vector2(r, 0), new Vector2(r, h * .65f),
            new Vector2(opening, h * .85f), new Vector2(opening, h), new Vector2(opening - thickness, h),
            new Vector2(opening - thickness, h * .85f), new Vector2(r - thickness, h * .65f),
            new Vector2(r - thickness, .004f), new Vector2(.001f, .004f) };
        var vertices = new List<Vector3>(); var triangles = new List<int>();
        for (int ring = 0; ring < profile.Length; ring++)
            for (int side = 0; side <= sides; side++)
            { float a = side * Mathf.PI * 2 / sides; vertices.Add(new Vector3(Mathf.Cos(a) * profile[ring].x, profile[ring].y, Mathf.Sin(a) * profile[ring].x)); }
        for (int ring = 0; ring < profile.Length - 1; ring++)
            for (int side = 0; side < sides; side++)
            { int a = ring * (sides + 1) + side, b = a + sides + 1; triangles.Add(a); triangles.Add(b); triangles.Add(a + 1); triangles.Add(a + 1); triangles.Add(b); triangles.Add(b + 1); }
        mesh = new Mesh { name = name }; mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, path); return mesh;
    }
    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int slash = path.LastIndexOf('/'); EnsureFolder(path.Substring(0, slash));
        AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
    }
}
