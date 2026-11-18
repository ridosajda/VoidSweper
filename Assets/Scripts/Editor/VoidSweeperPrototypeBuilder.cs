using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>
/// Menu: Void Sweeper > Build Prototype Scene
/// Membuat semua aset placeholder (mesh low-poly prosedural, material, prefab), scene, dan UI.
/// </summary>
public static class VoidSweeperPrototypeBuilder
{
    const string Root = "Assets/VoidSweeper";

    // ------------------------------------------------------------------ ENTRY
    [MenuItem("Void Sweeper/Build Prototype Scene")]
    public static void Build()
    {
        if (TMP_Settings.instance == null)
        {
            if (EditorUtility.DisplayDialog("TextMeshPro belum di-import",
                "UI butuh TMP Essential Resources.\nKlik Import, setelah selesai jalankan menu ini sekali lagi.",
                "Import", "Batal"))
                EditorApplication.ExecuteMenuItem("Window/TextMeshPro/Import TMP Essential Resources");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        EnsureFolder(Root + "/Materials");
        EnsureFolder(Root + "/Meshes");
        EnsureFolder(Root + "/Prefabs");
        EnsureFolder(Root + "/Scenes");

        // ---------- Material
        Material mShip   = Mat("ShipBody", new Color(0.70f, 0.78f, 0.88f));
        Material mShipDk = Mat("ShipDark", new Color(0.25f, 0.30f, 0.45f));
        Material mGlass  = Mat("ShipGlass", new Color(0.25f, 0.85f, 1f));
        Material mDrone  = Mat("Drone", new Color(1f, 0.55f, 0.1f));
        Material mRock   = Mat("Rock", new Color(0.45f, 0.40f, 0.38f));
        Material mHL     = Mat("Highlight", new Color(1f, 0.9f, 0.2f), true);
        Material mStar   = Mat("Star", Color.white, true);
        Material mBound  = Mat("Boundary", new Color(1f, 0.25f, 0.25f), true);
        Color[] trashCols = {
            new Color(0.55f, 0.35f, 0.2f), new Color(0.5f, 0.5f, 0.55f),
            new Color(0.35f, 0.5f, 0.3f),  new Color(0.6f, 0.2f, 0.2f), new Color(0.3f, 0.4f, 0.6f) };
        Material[] mTrash = new Material[trashCols.Length];
        for (int i = 0; i < mTrash.Length; i++) mTrash[i] = Mat("Trash" + i, trashCols[i]);

        // ---------- Prefab
        DroneController drone = BuildDronePrefab(mDrone, mShipDk);
        GameObject shipPrefab = BuildMothershipPrefab(drone, mShip, mShipDk, mGlass);

        var clusters = new DebrisCluster[3];
        for (int i = 0; i < 3; i++) clusters[i] = BuildClusterPrefab("DebrisCluster_" + (i + 1), 100 + i * 17, mTrash, mHL);

        float[] sizes = { 1.5f, 2.2f, 3.0f };
        var asteroids = new Asteroid[3];
        for (int i = 0; i < 3; i++) asteroids[i] = BuildAsteroidPrefab("Asteroid_" + (i + 1), 7 + i * 31, sizes[i], mRock);

        // ---------- Scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.4f, 0.4f, 0.5f);

        var lightGO = new GameObject("Directional Light");
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional; light.intensity = 1.2f;
        lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        var gmGO = new GameObject("GameManager");
        var gm = gmGO.AddComponent<GameManager>();
        gm.playAreaRadius = 70f; gm.parTimeSeconds = 120f;

        var shipGO = (GameObject)PrefabUtility.InstantiatePrefab(shipPrefab);
        var ship = shipGO.GetComponent<Mothership>();
        ship.transform.position = Vector3.zero;

        var camGO = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.02f, 0.02f, 0.07f);
        cam.farClipPlane = 500f; cam.fieldOfView = 60f;
        camGO.AddComponent<AudioListener>();
        var follow = camGO.AddComponent<CameraFollow>();
        follow.target = ship.transform;
        camGO.transform.position = ship.transform.position + follow.offset;
        camGO.transform.rotation = Quaternion.LookRotation(-follow.offset.normalized, Vector3.up);
        var input = camGO.AddComponent<PlayerInput>();
        input.cam = cam; input.mothership = ship;

        var spGO = new GameObject("LevelSpawner");
        var sp = spGO.AddComponent<LevelSpawner>();
        sp.clusterPrefabs = clusters; sp.asteroidPrefabs = asteroids;
        sp.clusterCount = 8; sp.asteroidCount = 25;

        BuildStars(mStar);
        BuildBoundary(mBound, gm.playAreaRadius);
        BuildUI();

        string scenePath = Root + "/Scenes/VoidSweeper_Prototype.unity";
        EditorSceneManager.SaveScene(scene, scenePath);
        AddToBuildSettings(scenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        bool needRestart = EnsureOldInputEnabled();
        EditorUtility.DisplayDialog("Void Sweeper",
            "Prototype selesai dibuat!\n\nScene: " + scenePath + "\nTekan Play untuk mencoba." +
            (needRestart ? "\n\nInput Handling diubah ke 'Both'. Jika Unity meminta restart editor, pilih Yes." : ""),
            "OK");
    }

    // ------------------------------------------------------------------ PREFABS
    static DroneController BuildDronePrefab(Material body, Material dark)
    {
        var root = new GameObject("Drone");
        var sc = root.AddComponent<SphereCollider>(); sc.isTrigger = true; sc.radius = 0.7f;
        var d = root.AddComponent<DroneController>();   // otomatis menambah Rigidbody
        Prim(PrimitiveType.Cube, "Body", root.transform, Vector3.zero, new Vector3(0.7f, 0.35f, 1.2f), body);
        Prim(PrimitiveType.Cube, "Wings", root.transform, new Vector3(0, 0, -0.2f), new Vector3(1.6f, 0.08f, 0.5f), dark);
        Prim(PrimitiveType.Sphere, "Nose", root.transform, new Vector3(0, 0, 0.6f), new Vector3(0.5f, 0.3f, 0.5f), dark);
        return SavePrefab(root, "Drone").GetComponent<DroneController>();
    }

    static GameObject BuildMothershipPrefab(DroneController dronePrefab, Material body, Material dark, Material glass)
    {
        var root = new GameObject("Mothership");
        var sc = root.AddComponent<SphereCollider>(); sc.isTrigger = true; sc.radius = 2.4f;
        var ms = root.AddComponent<Mothership>();       // otomatis menambah Rigidbody
        Prim(PrimitiveType.Cube, "Hull", root.transform, Vector3.zero, new Vector3(3f, 0.9f, 6f), body);
        Prim(PrimitiveType.Cube, "WingL", root.transform, new Vector3(-2.4f, -0.1f, -1.2f), new Vector3(2.4f, 0.2f, 2.2f), dark);
        Prim(PrimitiveType.Cube, "WingR", root.transform, new Vector3(2.4f, -0.1f, -1.2f), new Vector3(2.4f, 0.2f, 2.2f), dark);
        Prim(PrimitiveType.Sphere, "Cockpit", root.transform, new Vector3(0, 0.6f, 1.6f), new Vector3(1.6f, 1.1f, 1.8f), glass);
        Prim(PrimitiveType.Cube, "Engine", root.transform, new Vector3(0, 0, -3.1f), new Vector3(2f, 0.6f, 0.4f), glass);
        var dock = new GameObject("DockPoint");
        dock.transform.SetParent(root.transform, false);
        dock.transform.localPosition = new Vector3(0, 0, -3.6f);
        ms.dockPoint = dock.transform;
        ms.dronePrefab = dronePrefab;
        return SavePrefab(root, "Mothership");
    }

    static DebrisCluster BuildClusterPrefab(string name, int seed, Material[] trash, Material highlight)
    {
        var rng = new System.Random(seed);
        var root = new GameObject(name);
        var sc = root.AddComponent<SphereCollider>(); sc.isTrigger = true; sc.radius = 2.6f;
        var dc = root.AddComponent<DebrisCluster>();
        dc.approachRadius = 3f;

        int pieces = 7 + rng.Next(0, 4);
        for (int i = 0; i < pieces; i++)
        {
            var type = rng.Next(0, 3) == 0 ? PrimitiveType.Cylinder : PrimitiveType.Cube;
            Vector3 pos = new Vector3(R(rng, -1.8f, 1.8f), R(rng, -0.4f, 0.8f), R(rng, -1.8f, 1.8f));
            Vector3 scl = new Vector3(R(rng, 0.6f, 1.6f), R(rng, 0.5f, 1.3f), R(rng, 0.6f, 1.6f));
            Vector3 rot = new Vector3(R(rng, 0, 360), R(rng, 0, 360), R(rng, 0, 360));
            Prim(type, "Junk" + i, root.transform, pos, scl, trash[rng.Next(trash.Length)], rot);
        }

        var ring = Prim(PrimitiveType.Cylinder, "Highlight", root.transform, new Vector3(0, -0.6f, 0), new Vector3(7f, 0.05f, 7f), highlight);
        ring.SetActive(false);
        dc.highlightIndicator = ring;

        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(root.transform, false);
        labelGO.transform.localPosition = new Vector3(0, 4f, 0);
        var tmp = labelGO.AddComponent<TextMeshPro>();
        tmp.text = "30"; tmp.fontSize = 24; tmp.alignment = TextAlignmentOptions.Center; tmp.color = Color.white;
        dc.amountLabel = tmp;

        return SavePrefab(root, name).GetComponent<DebrisCluster>();
    }

    static Asteroid BuildAsteroidPrefab(string name, int seed, float size, Material rock)
    {
        var root = new GameObject(name);
        root.AddComponent<MeshFilter>().sharedMesh = BuildAsteroidMesh(name, seed);
        root.AddComponent<MeshRenderer>().sharedMaterial = rock;
        var sc = root.AddComponent<SphereCollider>(); sc.isTrigger = true; sc.radius = 0.95f;
        var a = root.AddComponent<Asteroid>();
        a.radius = size;
        root.transform.localScale = Vector3.one * size;
        return SavePrefab(root, name).GetComponent<Asteroid>();
    }

    // Mesh asteroid low-poly (icosahedron + jitter + flat shading)
    static Mesh BuildAsteroidMesh(string name, int seed)
    {
        var rng = new System.Random(seed);
        float t = (1f + Mathf.Sqrt(5f)) / 2f;
        Vector3[] v = {
            new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
            new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
            new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1) };
        for (int i = 0; i < v.Length; i++)
            v[i] = v[i].normalized * R(rng, 0.8f, 1.15f);
        int[] f = {
            0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11, 1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8,
            3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9, 4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1 };

        var verts = new List<Vector3>(); var tris = new List<int>();
        for (int i = 0; i < f.Length; i += 3)
        {
            Vector3 a = v[f[i]], b = v[f[i + 1]], c = v[f[i + 2]];
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), (a + b + c) / 3f) < 0f) { var tmp = b; b = c; c = tmp; }
            int s = verts.Count;
            verts.Add(a); verts.Add(b); verts.Add(c);
            tris.Add(s); tris.Add(s + 1); tris.Add(s + 2);
        }
        var m = new Mesh { name = name + "_Mesh" };
        m.SetVertices(verts); m.SetTriangles(tris, 0);
        m.RecalculateNormals(); m.RecalculateBounds();
        AssetDatabase.CreateAsset(m, Root + "/Meshes/" + name + ".asset");
        return m;
    }

    // ------------------------------------------------------------------ SCENE EXTRAS
    static void BuildStars(Material mat)
    {
        var parent = new GameObject("Stars");
        var rng = new System.Random(42);
        for (int i = 0; i < 250; i++)
        {
            float ang = R(rng, 0, Mathf.PI * 2f), rad = Mathf.Sqrt((float)rng.NextDouble()) * 170f;
            float s = R(rng, 0.5f, 1.2f);
            var g = Prim(PrimitiveType.Cube, "Star", parent.transform,
                new Vector3(Mathf.Cos(ang) * rad, -25f, Mathf.Sin(ang) * rad), Vector3.one * s, mat);
            g.isStatic = true;
        }
    }

    static void BuildBoundary(Material mat, float radius)
    {
        var go = new GameObject("PlayAreaBoundary");
        var lr = go.AddComponent<LineRenderer>();
        int n = 120;
        lr.positionCount = n; lr.loop = true; lr.useWorldSpace = true;
        lr.widthMultiplier = 0.5f; lr.sharedMaterial = mat;
        for (int i = 0; i < n; i++)
        {
            float a = i / (float)n * Mathf.PI * 2f;
            lr.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
        }
    }

    // ------------------------------------------------------------------ UI
    static void BuildUI()
    {
        var canvasGO = new GameObject("Canvas", typeof(RectTransform));
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();
        Transform c = canvasGO.transform;

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        var uiGO = new GameObject("UIManager");
        var ui = uiGO.AddComponent<UIManager>();

        // HP slider
        var sRT = UIRect("HPSlider", c);
        sRT.anchorMin = sRT.anchorMax = sRT.pivot = new Vector2(0, 1);
        sRT.anchoredPosition = new Vector2(40, -40); sRT.sizeDelta = new Vector2(420, 32);
        var slider = sRT.gameObject.AddComponent<Slider>();
        var bg = UIRect("Background", sRT); Stretch(bg, 0);
        bg.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.6f);
        var fa = UIRect("Fill Area", sRT); Stretch(fa, 4);
        var fill = UIRect("Fill", fa); fill.sizeDelta = Vector2.zero;
        var fillImg = fill.gameObject.AddComponent<Image>(); fillImg.color = new Color(0.25f, 0.9f, 0.35f);
        slider.fillRect = fill; slider.targetGraphic = fillImg;
        slider.transition = Selectable.Transition.None; slider.interactable = false;
        slider.minValue = 0; slider.maxValue = 1; slider.value = 1;
        ui.hpSlider = slider;

        ui.hpText = UIText(c, "HPText", new Vector2(0, 1), new Vector2(40, -80), new Vector2(420, 40), "HP", 30, TextAlignmentOptions.Left);
        ui.timerText = UIText(c, "Timer", new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(300, 70), "00:00", 56, TextAlignmentOptions.Center);
        ui.trashText = UIText(c, "Trash", new Vector2(1, 1), new Vector2(-40, -30), new Vector2(500, 50), "Sampah", 38, TextAlignmentOptions.Right);
        ui.dronesText = UIText(c, "Drones", new Vector2(1, 1), new Vector2(-40, -85), new Vector2(500, 50), "Drone", 34, TextAlignmentOptions.Right);
        ui.toastText = UIText(c, "Toast", new Vector2(0.5f, 1), new Vector2(0, -130), new Vector2(1000, 60), "", 38, TextAlignmentOptions.Center);
        ui.toastText.color = new Color(1f, 0.9f, 0.3f);
        var hint = UIText(c, "Hint", new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(1400, 50),
            "WASD / Panah: gerak kapal   |   Klik kiri tumpukan sampah: kirim drone   |   Hindari asteroid!", 28, TextAlignmentOptions.Center);
        hint.rectTransform.pivot = new Vector2(0.5f, 0);
        // End panel
        var panel = UIRect("EndPanel", c);
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0.5f);
        panel.anchoredPosition = Vector2.zero; panel.sizeDelta = new Vector2(800, 520);
        panel.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.85f);
        ui.endTitleText = UIText(panel, "Title", new Vector2(0.5f, 0.5f), new Vector2(0, 170), new Vector2(760, 100), "", 72, TextAlignmentOptions.Center);
        ui.endInfoText = UIText(panel, "Info", new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(700, 200), "", 44, TextAlignmentOptions.Center);

        var bRT = UIRect("RestartButton", panel);
        bRT.anchorMin = bRT.anchorMax = bRT.pivot = new Vector2(0.5f, 0.5f);
        bRT.anchoredPosition = new Vector2(0, -170); bRT.sizeDelta = new Vector2(360, 90);
        var bImg = bRT.gameObject.AddComponent<Image>(); bImg.color = new Color(0.2f, 0.45f, 0.9f);
        var btn = bRT.gameObject.AddComponent<Button>(); btn.targetGraphic = bImg;
        UIText(bRT, "Label", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(340, 80), "Main Lagi", 42, TextAlignmentOptions.Center);
        UnityEventTools.AddPersistentListener(btn.onClick, ui.OnRestartClicked);

        ui.endPanel = panel.gameObject;
        panel.gameObject.SetActive(false);
    }

    static RectTransform UIRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    static void Stretch(RectTransform rt, float inset)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(inset, inset); rt.offsetMax = new Vector2(-inset, -inset);
    }

    static TextMeshProUGUI UIText(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size,
                                  string text, float fontSize, TextAlignmentOptions align)
    {
        var rt = UIRect(name, parent);
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = fontSize; t.alignment = align; t.color = Color.white;
        t.raycastTarget = false;
        return t;
    }

    // ------------------------------------------------------------------ UTIL
    static float R(System.Random rng, float a, float b) { return a + (float)rng.NextDouble() * (b - a); }

    static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale,
                           Material mat, Vector3 euler = default)
    {
        var g = GameObject.CreatePrimitive(type);
        g.name = name;
        Object.DestroyImmediate(g.GetComponent<Collider>());
        g.transform.SetParent(parent, false);
        g.transform.localPosition = pos;
        g.transform.localScale = scale;
        g.transform.localEulerAngles = euler;
        g.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return g;
    }

    static GameObject SavePrefab(GameObject root, string name)
    {
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, Root + "/Prefabs/" + name + ".prefab");
        Object.DestroyImmediate(root);
        return prefab;
    }

    static Material Mat(string name, Color color, bool unlit = false)
    {
        string path = Root + "/Materials/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        bool urp = GraphicsSettings.currentRenderPipeline != null;
        Shader sh = unlit
            ? Shader.Find(urp ? "Universal Render Pipeline/Unlit" : "Unlit/Color")
            : Shader.Find(urp ? "Universal Render Pipeline/Lit" : "Standard");
        if (sh == null) sh = Shader.Find("Standard");
        if (sh == null) sh = Shader.Find("Sprites/Default");
        if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, path); }
        else m.shader = sh;
        m.color = color;
        EditorUtility.SetDirty(m);
        return m;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    static void AddToBuildSettings(string scenePath)
    {
        var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (var s in list) if (s.path == scenePath) return;
        list.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = list.ToArray();
    }

    // Script game memakai Input klasik. Jika project hanya Input System baru, ubah ke "Both".
    static bool EnsureOldInputEnabled()
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
        if (assets == null || assets.Length == 0) return false;
        var so = new SerializedObject(assets[0]);
        var p = so.FindProperty("activeInputHandler");
        if (p == null || p.intValue != 1) return false;
        p.intValue = 2;
        so.ApplyModifiedProperties();
        return true;
    }
}
