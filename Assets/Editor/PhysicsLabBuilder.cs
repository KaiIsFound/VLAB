using UnityEngine;
using UnityEditor;
using TMPro;
using VLAB.PhysicsLab;

/// <summary>
/// Editor tool: adds physics lab furniture and interactable equipment
/// directly into the PhysicLab scene (persists in Edit mode).
/// 
/// Usage: In Unity menu → VLAB → Build Physics Lab
/// </summary>
public class PhysicsLabBuilder : EditorWindow
{
    // ── Colours ──────────────────────────────────────────
    static Color tableColor    = new Color(0.55f, 0.35f, 0.17f);
    static Color legColor      = new Color(0.25f, 0.25f, 0.25f);
    static Color shelfColor    = new Color(0.40f, 0.40f, 0.40f);
    static Color beakerColor   = new Color(0.85f, 0.92f, 0.98f, 0.7f);
    static Color flaskColor    = new Color(0.80f, 0.90f, 0.80f, 0.7f);
    static Color tubeColor     = new Color(0.30f, 0.55f, 0.90f);
    static Color scopeColor    = new Color(0.15f, 0.15f, 0.15f);
    static Color weightColor   = new Color(0.75f, 0.70f, 0.20f);
    static Color watchColor    = new Color(0.90f, 0.90f, 0.90f);
    static Color whiteBoardC   = new Color(0.95f, 0.95f, 0.95f);
    static Color pendBaseC     = new Color(0.35f, 0.22f, 0.10f);
    static Color pendBallC     = new Color(0.80f, 0.15f, 0.10f);
    static Color rulerColor    = new Color(0.95f, 0.85f, 0.55f);
    static Color springColor   = new Color(0.60f, 0.60f, 0.60f);
    static Color sinkColor     = new Color(0.70f, 0.70f, 0.72f);
    static Color cabinetColor  = new Color(0.45f, 0.30f, 0.15f);

    [MenuItem("VLAB/Build Physics Lab")]
    static void BuildPhysicsLab()
    {
        RemoveStarterAssetDemoVisuals();

        // Remove old generated group if re-running
        GameObject old = GameObject.Find("__PhysicsLab_Generated__");
        if (old != null) DestroyImmediate(old);

        GameObject root = new GameObject("__PhysicsLab_Generated__");
        Undo.RegisterCreatedObjectUndo(root, "Build Physics Lab");

        BuildRoom(root.transform);
        BuildFurniture(root.transform);
        PendulumExperiment experiment = BuildLabEquipment(root.transform);
        BuildWallDecorations(root.transform);
        BuildSink(root.transform);
        BuildLearningConsole(root.transform, experiment);
        root.AddComponent<VLabControllerBridge>().Configure(experiment);

        EditorUtility.SetDirty(root);
        Debug.Log("[VLAB] Physics lab built! Save the scene to keep changes.");
    }

    // The starter scene is useful for its XR rig but not as classroom content.
    // Keep the rig and interaction manager; remove only its visual demo stations.
    static void RemoveStarterAssetDemoVisuals()
    {
        string[] demoObjectNames =
        {
            "Demo Environment", "Grab Interactable Table", "Far Grab Interactable Table",
            "Gaze Interactables", "Gaze Interactable Info", "Poke Interactions Table",
            "Poke Interactions Info", "Gaze Select/Deselect Interactable Info",
            "Gaze Select/Deselect Simple Interactable", "Gaze Assisted Simple Interactable",
            "Far Grab Interactable Info", "Grab Interactable Info"
        };
        foreach (string itemName in demoObjectNames)
        {
            GameObject item = GameObject.Find(itemName);
            if (item != null) DestroyImmediate(item);
        }
    }

    // ═══════════════════════════════════════════════════════
    //  VR ROOM AND LEARNING CONSOLE
    // ═══════════════════════════════════════════════════════

    static void BuildRoom(Transform root)
    {
        GameObject room = CreateChild(root, "PhysicsLabRoom");
        MakePrimitive(PrimitiveType.Cube, room.transform, new Vector3(0, -0.08f, 0), new Vector3(9f, 0.16f, 9f), new Color(0.12f, 0.20f, 0.25f), "Floor");
        MakePrimitive(PrimitiveType.Cube, room.transform, new Vector3(0, 2.2f, 4.4f), new Vector3(9f, 4.4f, 0.12f), new Color(0.70f, 0.80f, 0.85f), "NorthWall");
        MakePrimitive(PrimitiveType.Cube, room.transform, new Vector3(0, 2.2f, -4.4f), new Vector3(9f, 4.4f, 0.12f), new Color(0.70f, 0.80f, 0.85f), "SouthWall");
        MakePrimitive(PrimitiveType.Cube, room.transform, new Vector3(-4.4f, 2.2f, 0), new Vector3(0.12f, 4.4f, 9f), new Color(0.64f, 0.75f, 0.82f), "WestWall");
        MakePrimitive(PrimitiveType.Cube, room.transform, new Vector3(4.4f, 2.2f, 0), new Vector3(0.12f, 4.4f, 9f), new Color(0.64f, 0.75f, 0.82f), "EastWall");
        CreateLight(room.transform, new Vector3(-2.5f, 3.6f, -1f));
        CreateLight(room.transform, new Vector3(2.5f, 3.6f, -1f));
    }

    static void CreateLight(Transform parent, Vector3 position)
    {
        GameObject lightObject = CreateChild(parent, "LabLight");
        lightObject.transform.localPosition = position;
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 8f;
        light.intensity = 3.2f;
        light.color = new Color(0.82f, 0.92f, 1f);
    }

    static void BuildLearningConsole(Transform root, PendulumExperiment experiment)
    {
        GameObject console = CreateChild(root, "PendulumLearningConsole");
        console.transform.localPosition = new Vector3(0f, 0.93f, -0.55f);

        MakePrimitive(PrimitiveType.Cube, console.transform, new Vector3(0, -0.05f, 0), new Vector3(2.3f, 0.12f, 0.45f), new Color(0.08f, 0.15f, 0.20f), "ConsoleBase");
        CreateControlButton(console.transform, experiment, new Vector3(-0.82f, 0.08f, 0), "L -", new Color(0.22f, 0.58f, 0.88f), LabControlButton.Command.ShorterString);
        CreateControlButton(console.transform, experiment, new Vector3(-0.42f, 0.08f, 0), "L +", new Color(0.22f, 0.58f, 0.88f), LabControlButton.Command.LongerString);
        CreateControlButton(console.transform, experiment, new Vector3(0.00f, 0.08f, 0), "BẮT ĐẦU", new Color(0.18f, 0.72f, 0.38f), LabControlButton.Command.Start);
        CreateControlButton(console.transform, experiment, new Vector3(0.46f, 0.08f, 0), "DỪNG/GHI", new Color(0.94f, 0.52f, 0.16f), LabControlButton.Command.StopAndRecord);
        CreateControlButton(console.transform, experiment, new Vector3(0.86f, 0.08f, 0), "ĐẶT LẠI", new Color(0.72f, 0.30f, 0.30f), LabControlButton.Command.Reset);

        GameObject board = MakePrimitive(PrimitiveType.Cube, root, new Vector3(0, 2.65f, 1.6f), new Vector3(3.5f, 1.45f, 0.05f), new Color(0.03f, 0.12f, 0.16f), "PendulumInstructionBoard");
        TextMeshPro statusText = CreateWorldText(board.transform, "StatusText", Vector3.zero, new Vector2(3.1f, 1.15f), 0.22f, Color.white);
        statusText.transform.localPosition = new Vector3(-1.5f, 0.48f, -0.04f);
        statusText.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        statusText.alignment = TextAlignmentOptions.TopLeft;
        board.AddComponent<PendulumStatusDisplay>().Configure(experiment, statusText);
    }

    static void CreateControlButton(Transform parent, PendulumExperiment experiment, Vector3 pos, string label, Color color, LabControlButton.Command command)
    {
        GameObject button = MakePrimitive(PrimitiveType.Cube, parent, pos, new Vector3(0.34f, 0.08f, 0.26f), color, "Button_" + label);
        button.AddComponent<LabControlButton>().Configure(experiment, command);
        TextMeshPro labelText = CreateWorldText(button.transform, "Label", new Vector3(0, 0.06f, 0), new Vector2(0.30f, 0.20f), 0.10f, Color.black);
        labelText.text = label;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
    }

    static TextMeshPro CreateWorldText(Transform parent, string name, Vector3 localPosition, Vector2 size, float fontSize, Color color)
    {
        GameObject textObject = CreateChild(parent, name);
        textObject.transform.localPosition = localPosition;
        TextMeshPro text = textObject.AddComponent<TextMeshPro>();
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = fontSize;
        text.color = color;
        text.rectTransform.sizeDelta = size;
        text.enableWordWrapping = true;
        return text;
    }

    // ═══════════════════════════════════════════════════════
    //  FURNITURE
    // ═══════════════════════════════════════════════════════

    static void BuildFurniture(Transform root)
    {
        GameObject furniture = CreateChild(root, "LabFurniture");

        // Main experiment table (centre)
        CreateTable(furniture.transform, Vector3.zero, new Vector3(3f, 0.05f, 1.2f), "MainTable");

        // North wall table
        CreateTable(furniture.transform, new Vector3(-2.5f, 0f, 3f), new Vector3(2.5f, 0.05f, 0.8f), "NorthTable");

        // South wall table
        CreateTable(furniture.transform, new Vector3(2f, 0f, -3f), new Vector3(2f, 0.05f, 0.8f), "SouthTable");

        // Storage shelf (east wall)
        CreateShelf(furniture.transform, new Vector3(3.5f, 0f, 0f));

        // Stools
        CreateStool(furniture.transform, new Vector3(-1f, 0f, -0.8f));
        CreateStool(furniture.transform, new Vector3(1f, 0f, -0.8f));
    }

    static void CreateTable(Transform parent, Vector3 pos, Vector3 topScale, string label)
    {
        float tableH = 0.9f;
        GameObject table = CreateChild(parent, label);
        table.transform.localPosition = pos;

        MakePrimitive(PrimitiveType.Cube, table.transform, new Vector3(0, tableH, 0), topScale, tableColor, label + "_Top");

        float legH = tableH / 2f;
        float hx = topScale.x / 2f - 0.05f;
        float hz = topScale.z / 2f - 0.05f;
        Vector3 ls = new Vector3(0.06f, tableH, 0.06f);

        MakePrimitive(PrimitiveType.Cube, table.transform, new Vector3(-hx, legH, -hz), ls, legColor, label + "_Leg1");
        MakePrimitive(PrimitiveType.Cube, table.transform, new Vector3( hx, legH, -hz), ls, legColor, label + "_Leg2");
        MakePrimitive(PrimitiveType.Cube, table.transform, new Vector3(-hx, legH,  hz), ls, legColor, label + "_Leg3");
        MakePrimitive(PrimitiveType.Cube, table.transform, new Vector3( hx, legH,  hz), ls, legColor, label + "_Leg4");
    }

    static void CreateShelf(Transform parent, Vector3 pos)
    {
        GameObject shelf = CreateChild(parent, "StorageShelf");
        shelf.transform.localPosition = pos;

        float h = 1.8f;
        Vector3 ps = new Vector3(0.04f, h, 0.04f);
        float px = 0.5f, pz = 0.25f;

        MakePrimitive(PrimitiveType.Cube, shelf.transform, new Vector3(-px, h/2, -pz), ps, shelfColor, "Post1");
        MakePrimitive(PrimitiveType.Cube, shelf.transform, new Vector3( px, h/2, -pz), ps, shelfColor, "Post2");
        MakePrimitive(PrimitiveType.Cube, shelf.transform, new Vector3(-px, h/2,  pz), ps, shelfColor, "Post3");
        MakePrimitive(PrimitiveType.Cube, shelf.transform, new Vector3( px, h/2,  pz), ps, shelfColor, "Post4");

        Vector3 bs = new Vector3(1.1f, 0.03f, 0.55f);
        MakePrimitive(PrimitiveType.Cube, shelf.transform, new Vector3(0, 0.5f, 0), bs, shelfColor, "Board1");
        MakePrimitive(PrimitiveType.Cube, shelf.transform, new Vector3(0, 1.0f, 0), bs, shelfColor, "Board2");
        MakePrimitive(PrimitiveType.Cube, shelf.transform, new Vector3(0, 1.5f, 0), bs, shelfColor, "Board3");
    }

    static void CreateStool(Transform parent, Vector3 pos)
    {
        GameObject stool = CreateChild(parent, "Stool");
        stool.transform.localPosition = pos;

        float sH = 0.55f;
        MakePrimitive(PrimitiveType.Cylinder, stool.transform, new Vector3(0, sH, 0), new Vector3(0.3f, 0.03f, 0.3f), legColor, "SeatTop");
        MakePrimitive(PrimitiveType.Cylinder, stool.transform, new Vector3(0, sH/2f, 0), new Vector3(0.05f, sH/2f, 0.05f), legColor, "StoolLeg");
        MakePrimitive(PrimitiveType.Cylinder, stool.transform, new Vector3(0, 0.03f, 0), new Vector3(0.25f, 0.03f, 0.25f), legColor, "StoolBase");
    }

    // ═══════════════════════════════════════════════════════
    //  LAB EQUIPMENT (interactable, Layer 7)
    // ═══════════════════════════════════════════════════════

    static PendulumExperiment BuildLabEquipment(Transform root)
    {
        GameObject equip = CreateChild(root, "LabEquipment");
        float tY = 0.93f;

        // ─── Main Table: cơ học ───
        MakeLabItem(equip.transform, "Đồng hồ bấm giờ",    new Vector3(-0.75f, tY, 0f),     PrimitiveType.Cylinder, new Vector3(0.08f, 0.02f, 0.08f), watchColor, 0.08f);
        MakeLabItem(equip.transform, "Quả cân 200g",        new Vector3(-0.48f, tY, 0f),     PrimitiveType.Cylinder, new Vector3(0.05f, 0.05f, 0.05f),  weightColor, 0.2f);
        MakeLabItem(equip.transform, "Quả cân 100g",        new Vector3(-0.30f, tY, 0f),     PrimitiveType.Cylinder, new Vector3(0.04f, 0.04f, 0.04f), weightColor, 0.1f);
        MakeLabItem(equip.transform, "Quả cân 50g",         new Vector3(-0.15f, tY, 0f),     PrimitiveType.Cylinder, new Vector3(0.03f, 0.03f, 0.03f), weightColor, 0.05f);

        // Pendulum
        PendulumExperiment experiment = BuildPendulum(equip.transform, new Vector3(0f, tY, 0.35f));

        // ─── North Table: điện học ───
        MakeLabItem(equip.transform, "Nguồn điện một chiều", new Vector3(-2.6f, tY, 3f), PrimitiveType.Cube, new Vector3(0.38f, 0.12f, 0.22f), new Color(0.18f, 0.22f, 0.28f), 1f);
        MakeLabItem(equip.transform, "Ampe kế",              new Vector3(-2.1f, tY, 3f), PrimitiveType.Cylinder, new Vector3(0.10f, 0.03f, 0.10f), new Color(0.88f, 0.88f, 0.82f), 0.2f);
        MakeLabItem(equip.transform, "Vôn kế",               new Vector3(-1.8f, tY, 3f), PrimitiveType.Cylinder, new Vector3(0.10f, 0.03f, 0.10f), new Color(0.88f, 0.88f, 0.82f), 0.2f);
        MakeLabItem(equip.transform, "Điện trở",             new Vector3(-1.5f, tY, 3f), PrimitiveType.Cylinder, new Vector3(0.035f, 0.12f, 0.035f), new Color(0.65f, 0.38f, 0.18f), 0.05f);

        // ─── South Table items ───
        MakeLabItem(equip.transform, "Lò xo",               new Vector3(2.3f, tY, -3f),     PrimitiveType.Capsule,  new Vector3(0.03f, 0.08f, 0.03f), springColor, 0.1f);
        MakeLabItem(equip.transform, "Thước kẻ 30cm",       new Vector3(1.5f, tY, -3.1f),   PrimitiveType.Cube,     new Vector3(0.3f, 0.005f, 0.03f), rulerColor,  0.05f);
        MakeLabItem(equip.transform, "Thước đo góc",        new Vector3(2.6f, tY, -2.8f),   PrimitiveType.Cylinder, new Vector3(0.08f, 0.003f, 0.08f), new Color(0.95f, 0.95f, 0.80f), 0.03f);

        // ─── Shelf: dụng cụ vật lí dự phòng ───
        MakeLabItem(equip.transform, "Nam châm thanh",          new Vector3(3.3f, 0.53f, 0f), PrimitiveType.Cube, new Vector3(0.18f, 0.05f, 0.05f), new Color(0.80f, 0.12f, 0.12f), 0.2f);
        MakeLabItem(equip.transform, "Cuộn dây",                new Vector3(3.6f, 0.53f, 0f), PrimitiveType.Cylinder, new Vector3(0.08f, 0.05f, 0.08f), new Color(0.86f, 0.55f, 0.18f), 0.15f);
        MakeLabItem(equip.transform, "Quả cân 500g",            new Vector3(3.3f, 1.03f, 0f), PrimitiveType.Cylinder, new Vector3(0.05f, 0.05f, 0.05f), weightColor, 0.5f);
        MakeLabItem(equip.transform, "Quả cầu sắt",             new Vector3(3.6f, 1.03f, 0.1f), PrimitiveType.Sphere, new Vector3(0.06f, 0.06f, 0.06f), new Color(0.50f, 0.50f, 0.55f), 0.4f);
        MakeLabItem(equip.transform, "Thấu kính hội tụ",        new Vector3(3.4f, 1.53f, 0f), PrimitiveType.Cylinder, new Vector3(0.09f, 0.01f, 0.09f), new Color(0.45f, 0.78f, 0.96f), 0.03f);
        return experiment;
    }

    static PendulumExperiment BuildPendulum(Transform parent, Vector3 basePos)
    {
        GameObject pend = CreateChild(parent, "Pendulum");
        pend.transform.localPosition = basePos;

        MakePrimitive(PrimitiveType.Cube,     pend.transform, Vector3.zero,             new Vector3(0.25f, 0.03f, 0.15f), pendBaseC, "PendBase");
        MakePrimitive(PrimitiveType.Cube,     pend.transform, new Vector3(0, 0.25f, 0), new Vector3(0.02f, 0.5f, 0.02f),  pendBaseC, "PendPost");
        MakePrimitive(PrimitiveType.Cube,     pend.transform, new Vector3(0, 0.5f, 0),  new Vector3(0.15f, 0.015f, 0.015f), pendBaseC, "PendArm");
        GameObject stringVisual = MakePrimitive(PrimitiveType.Cylinder, pend.transform, new Vector3(0, -0.4f, 0), new Vector3(0.005f, 0.4f, 0.005f), Color.white, "PendString");

        GameObject bob = MakeLabItem(pend.transform, "Quả lắc", new Vector3(0, -0.8f, 0),
            PrimitiveType.Sphere, new Vector3(0.06f, 0.06f, 0.06f), pendBallC, 0.15f);

        // The lab builder now produces a working learning experiment, not only a visual prop.
        PendulumExperiment experiment = pend.AddComponent<PendulumExperiment>();
        experiment.ConfigureVisuals(bob.transform, stringVisual.transform);
        return experiment;
    }

    static void BuildMicroscope(Transform parent, Vector3 pos)
    {
        GameObject scope = CreateChild(parent, "Microscope");
        scope.transform.localPosition = pos;

        MakePrimitive(PrimitiveType.Cube, scope.transform, Vector3.zero, new Vector3(0.12f, 0.02f, 0.10f), scopeColor, "ScopeBase");
        MakePrimitive(PrimitiveType.Cube, scope.transform, new Vector3(-0.04f, 0.12f, 0), new Vector3(0.025f, 0.24f, 0.025f), scopeColor, "ScopeArm");

        GameObject ep = MakePrimitive(PrimitiveType.Cylinder, scope.transform, new Vector3(-0.01f, 0.22f, 0), new Vector3(0.025f, 0.06f, 0.025f), scopeColor, "ScopeEyepiece");
        ep.transform.localRotation = Quaternion.Euler(0, 0, 25f);

        MakePrimitive(PrimitiveType.Cube, scope.transform, new Vector3(0.02f, 0.08f, 0), new Vector3(0.08f, 0.005f, 0.08f), new Color(0.3f, 0.3f, 0.3f), "ScopeStage");

        scope.layer = 7;
        scope.AddComponent<Rigidbody>().mass = 1.5f;
        BoxCollider col = scope.AddComponent<BoxCollider>();
        col.size = new Vector3(0.14f, 0.28f, 0.12f);
        col.center = new Vector3(0, 0.12f, 0);
        InteractableItem item = scope.AddComponent<InteractableItem>();
        item.itemName = "Kính hiển vi";
    }

    // ═══════════════════════════════════════════════════════
    //  WALL DECORATIONS
    // ═══════════════════════════════════════════════════════

    static void BuildWallDecorations(Transform root)
    {
        GameObject deco = CreateChild(root, "WallDecorations");

        // Whiteboard
        MakePrimitive(PrimitiveType.Cube, deco.transform, new Vector3(0, 2f, -3.85f), new Vector3(2.5f, 1.2f, 0.05f), whiteBoardC, "Whiteboard");
        MakePrimitive(PrimitiveType.Cube, deco.transform, new Vector3(0, 2f, -3.87f), new Vector3(2.6f, 1.3f, 0.03f), new Color(0.4f, 0.25f, 0.1f), "WhiteboardFrame");

        // Safety sign
        MakePrimitive(PrimitiveType.Cube, deco.transform, new Vector3(3.85f, 2.5f, 1.5f), new Vector3(0.05f, 0.6f, 0.4f), new Color(0.9f, 0.2f, 0.15f), "SafetySign");

        // Clock
        GameObject clk = MakePrimitive(PrimitiveType.Cylinder, deco.transform, new Vector3(1.5f, 3.5f, 3.85f), new Vector3(0.3f, 0.02f, 0.3f), Color.white, "WallClock");
        clk.transform.localRotation = Quaternion.Euler(90, 0, 0);

        GameObject clkFrame = MakePrimitive(PrimitiveType.Cylinder, deco.transform, new Vector3(1.5f, 3.5f, 3.86f), new Vector3(0.33f, 0.015f, 0.33f), new Color(0.2f, 0.2f, 0.2f), "ClockFrame");
        clkFrame.transform.localRotation = Quaternion.Euler(90, 0, 0);

        // Poster placeholder
        MakePrimitive(PrimitiveType.Cube, deco.transform, new Vector3(-1.5f, 2.5f, 3.85f), new Vector3(1.5f, 1f, 0.02f), new Color(0.95f, 0.92f, 0.85f), "FormulaPoster");
    }

    // ═══════════════════════════════════════════════════════
    //  SINK
    // ═══════════════════════════════════════════════════════

    static void BuildSink(Transform root)
    {
        GameObject sink = CreateChild(root, "WashStation");
        sink.transform.localPosition = new Vector3(-3.5f, 0f, 2f);

        MakePrimitive(PrimitiveType.Cube, sink.transform, new Vector3(0, 0.85f, 0), new Vector3(1f, 0.05f, 0.6f), sinkColor, "Counter");
        MakePrimitive(PrimitiveType.Cube, sink.transform, new Vector3(0, 0.80f, 0), new Vector3(0.4f, 0.12f, 0.35f), new Color(0.85f, 0.85f, 0.87f), "Basin");

        MakePrimitive(PrimitiveType.Cylinder, sink.transform, new Vector3(0, 1.0f, -0.15f), new Vector3(0.02f, 0.08f, 0.02f), new Color(0.75f, 0.75f, 0.78f), "FaucetSpout");

        GameObject arm = MakePrimitive(PrimitiveType.Cylinder, sink.transform, new Vector3(0, 1.05f, -0.08f), new Vector3(0.015f, 0.08f, 0.015f), new Color(0.75f, 0.75f, 0.78f), "FaucetArm");
        arm.transform.localRotation = Quaternion.Euler(0, 0, 90);

        MakePrimitive(PrimitiveType.Cube, sink.transform, new Vector3(0, 0.4f, 0), new Vector3(0.95f, 0.78f, 0.55f), cabinetColor, "Cabinet");
    }

    // ═══════════════════════════════════════════════════════
    //  HELPERS
    // ═══════════════════════════════════════════════════════

    static GameObject CreateChild(Transform parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.parent = parent;
        go.transform.localPosition = Vector3.zero;
        return go;
    }

    static GameObject MakePrimitive(PrimitiveType type, Transform parent,
        Vector3 localPos, Vector3 localScale, Color color, string name)
    {
        GameObject obj = GameObject.CreatePrimitive(type);
        obj.name = name;
        obj.transform.parent = parent;
        obj.transform.localPosition = localPos;
        obj.transform.localScale = localScale;

        Renderer rend = obj.GetComponent<Renderer>();
        if (rend != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = color;

            if (color.a < 1f)
            {
                mat.SetFloat("_Surface", 1);
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = 3000;
            }

            rend.sharedMaterial = mat;
        }

        return obj;
    }

    static GameObject MakeLabItem(Transform parent, string itemName, Vector3 pos,
        PrimitiveType shape, Vector3 scale, Color color, float mass)
    {
        GameObject obj = MakePrimitive(shape, parent, pos, scale, color, itemName);
        obj.layer = 7; // Interactable layer

        Rigidbody rb = obj.AddComponent<Rigidbody>();
        rb.mass = mass;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        InteractableItem item = obj.AddComponent<InteractableItem>();
        item.itemName = itemName;
        return obj;
    }
}
