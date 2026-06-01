using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Unity.XR.CoreUtils;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// Attach this to an empty GameObject and press Play.
/// It builds the entire puzzle scene procedurally — no manual setup needed.
/// </summary>
public class SceneSetup : MonoBehaviour
{
    private Transform puzzleRoot;

    void Awake()
    {
        BuildScene();
        Destroy(this); // One-time setup
    }

    void BuildScene()
    {
        // Root that parents the entire puzzle so AR placement can move/scale it as one.
        GameObject rootObj = new GameObject("PuzzleRoot");
        puzzleRoot = rootObj.transform;

        // --- AR rig ---
        GameObject sessionObj = new GameObject("AR Session");
        ARSession arSession = sessionObj.AddComponent<ARSession>();
        sessionObj.AddComponent<ARInputManager>();

        GameObject originObj = new GameObject("XR Origin");
        XROrigin xrOrigin = originObj.AddComponent<XROrigin>();

        GameObject camOffset = new GameObject("Camera Offset");
        camOffset.transform.SetParent(originObj.transform, false);
        xrOrigin.CameraFloorOffsetObject = camOffset;

        GameObject camObj = new GameObject("AR Camera");
        camObj.transform.SetParent(camOffset.transform, false);
        camObj.tag = "MainCamera";
        Camera cam = camObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 20f;
        camObj.AddComponent<ARCameraManager>();
        camObj.AddComponent<ARCameraBackground>();
        camObj.AddComponent<UnityEngine.SpatialTracking.TrackedPoseDriver>();
        xrOrigin.Camera = cam;

        // Ground
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Ground";
        ground.transform.position = new Vector3(0, -0.1f, 0);
        ground.transform.localScale = new Vector3(15, 0.2f, 15);
        ground.GetComponent<Renderer>().material.color = new Color(0.3f, 0.7f, 0.3f);
        Destroy(ground.GetComponent<Collider>()); // Don't interfere with raycasts
        ground.transform.SetParent(puzzleRoot, true);

        // Lighting
        GameObject light = new GameObject("DirectionalLight");
        light.transform.rotation = Quaternion.Euler(50, -30, 0);
        Light l = light.AddComponent<Light>();
        l.type = LightType.Directional;
        l.intensity = 1f;
        l.shadows = LightShadows.Soft;

        // GameManager
        GameObject gm = new GameObject("GameManager");
        GameManager manager = gm.AddComponent<GameManager>();
        manager.totalPieces = 7;

        // UI
        SetupUI(manager);

        // Define house pieces: name, target position, target rotation, scale, color, scatter offset
        PieceData[] pieces = new PieceData[]
        {
            new PieceData("Floor",     new Vector3(0, 0.1f, 0),    Vector3.zero,              new Vector3(4, 0.2f, 4),   new Color(0.6f, 0.4f, 0.2f), new Vector3(-5, 0, -3)),
            new PieceData("FrontWall", new Vector3(0, 1.35f, -2),  Vector3.zero,              new Vector3(4, 2.5f, 0.3f), new Color(0.9f, 0.85f, 0.7f), new Vector3(5, 0, -4)),
            new PieceData("BackWall",  new Vector3(0, 1.35f, 2),   Vector3.zero,              new Vector3(4, 2.5f, 0.3f), new Color(0.9f, 0.85f, 0.7f), new Vector3(-4, 0, 5)),
            new PieceData("LeftWall",  new Vector3(-2, 1.35f, 0),  new Vector3(0, 90, 0),     new Vector3(4, 2.5f, 0.3f), new Color(0.85f, 0.8f, 0.65f), new Vector3(6, 0, 3)),
            new PieceData("RightWall", new Vector3(2, 1.35f, 0),   new Vector3(0, 90, 0),     new Vector3(4, 2.5f, 0.3f), new Color(0.85f, 0.8f, 0.65f), new Vector3(-6, 0, 4)),
            new PieceData("Roof",      new Vector3(0, 3.1f, 0),    new Vector3(0, 0, 0),      new Vector3(4.5f, 0.3f, 4.5f), new Color(0.7f, 0.2f, 0.2f), new Vector3(5, 0, 6)),
            new PieceData("Door",      new Vector3(0, 0.85f, -2.1f), Vector3.zero,            new Vector3(1, 1.7f, 0.1f), new Color(0.4f, 0.25f, 0.1f), new Vector3(-5, 0, -6)),
        };

        foreach (PieceData data in pieces)
        {
            CreatePiece(data);
        }

        // Confetti particle system
        GameObject confettiObj = new GameObject("Confetti");
        confettiObj.transform.position = new Vector3(0, 5, 0);
        ParticleSystem ps = confettiObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 3f;
        main.startSpeed = 5f;
        main.startSize = 0.2f;
        main.maxParticles = 200;
        main.startColor = new ParticleSystem.MinMaxGradient(Color.yellow, Color.cyan);
        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0, 100) });
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 1f;
        ps.Stop();
        manager.confettiEffect = ps;
        confettiObj.transform.SetParent(puzzleRoot, true);

        // Auto-place the whole puzzle in front of the AR camera once tracking is ready.
        ARPuzzlePlacer placer = rootObj.AddComponent<ARPuzzlePlacer>();
        placer.puzzleRoot = puzzleRoot;
        placer.arSession = arSession;
        placer.arCamera = camObj.transform;
        placer.distance = 0.5f;
        placer.rootScale = 0.1f;
    }

    void CreatePiece(PieceData data)
    {
        // Ghost target
        GameObject ghost = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ghost.name = data.name + "_Ghost";
        ghost.transform.position = data.targetPos;
        ghost.transform.eulerAngles = data.targetRot;
        ghost.transform.localScale = data.scale;
        ghost.GetComponent<Renderer>().material.color = new Color(1, 1, 1, 0.3f);
        ghost.AddComponent<GhostTarget>();
        Destroy(ghost.GetComponent<Collider>()); // Ghosts shouldn't block raycasts

        // Actual piece (scattered)
        GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
        piece.name = data.name;
        piece.transform.position = data.scatterPos + Vector3.up * (data.scale.y / 2f + 0.1f);
        piece.transform.eulerAngles = new Vector3(0, Random.Range(0, 360), 0);
        piece.transform.localScale = data.scale;
        piece.GetComponent<Renderer>().material.color = data.color;

        PuzzlePiece pp = piece.AddComponent<PuzzlePiece>();
        pp.targetPosition = ghost.transform;
        pp.snapThreshold = Mathf.Max(data.scale.x, data.scale.z) * 0.4f;

        ghost.transform.SetParent(puzzleRoot, true);
        piece.transform.SetParent(puzzleRoot, true);
    }

    void SetupUI(GameManager manager)
    {
        // Canvas
        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        // EventSystem (required for UI button clicks)
        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();

        // Progress text
        GameObject textObj = new GameObject("ProgressText");
        textObj.transform.SetParent(canvasObj.transform, false);
        Text progressText = textObj.AddComponent<Text>();
        progressText.text = "Placed: 0/7";
        progressText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        progressText.fontSize = 24;
        progressText.color = Color.white;
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, 1);
        textRect.anchorMax = new Vector2(0, 1);
        textRect.pivot = new Vector2(0, 1);
        textRect.anchoredPosition = new Vector2(20, -20);
        textRect.sizeDelta = new Vector2(200, 40);
        manager.progressText = progressText;

        // Reset button
        GameObject buttonObj = new GameObject("ResetButton");
        buttonObj.transform.SetParent(canvasObj.transform, false);
        Image btnImage = buttonObj.AddComponent<Image>();
        btnImage.color = new Color(0.8f, 0.3f, 0.3f);
        Button button = buttonObj.AddComponent<Button>();
        RectTransform btnRect = buttonObj.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(1, 1);
        btnRect.anchorMax = new Vector2(1, 1);
        btnRect.pivot = new Vector2(1, 1);
        btnRect.anchoredPosition = new Vector2(-20, -20);
        btnRect.sizeDelta = new Vector2(100, 40);
        button.onClick.AddListener(() => manager.ResetGame());

        GameObject btnTextObj = new GameObject("Text");
        btnTextObj.transform.SetParent(buttonObj.transform, false);
        Text btnText = btnTextObj.AddComponent<Text>();
        btnText.text = "Reset";
        btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        btnText.fontSize = 20;
        btnText.color = Color.white;
        btnText.alignment = TextAnchor.MiddleCenter;
        RectTransform btnTextRect = btnTextObj.GetComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.sizeDelta = Vector2.zero;

        // Win panel
        GameObject winPanel = new GameObject("WinPanel");
        winPanel.transform.SetParent(canvasObj.transform, false);
        Image winBg = winPanel.AddComponent<Image>();
        winBg.color = new Color(0, 0, 0, 0.7f);
        RectTransform winRect = winPanel.GetComponent<RectTransform>();
        winRect.anchorMin = Vector2.zero;
        winRect.anchorMax = Vector2.one;
        winRect.sizeDelta = Vector2.zero;

        GameObject winTextObj = new GameObject("WinText");
        winTextObj.transform.SetParent(winPanel.transform, false);
        Text winText = winTextObj.AddComponent<Text>();
        winText.text = "Congratulations!\nPuzzle Complete!";
        winText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        winText.fontSize = 48;
        winText.color = Color.white;
        winText.alignment = TextAnchor.MiddleCenter;
        RectTransform winTextRect = winTextObj.GetComponent<RectTransform>();
        winTextRect.anchorMin = Vector2.zero;
        winTextRect.anchorMax = Vector2.one;
        winTextRect.sizeDelta = Vector2.zero;

        manager.winPanel = winPanel;
        winPanel.SetActive(false);
    }

    struct PieceData
    {
        public string name;
        public Vector3 targetPos;
        public Vector3 targetRot;
        public Vector3 scale;
        public Color color;
        public Vector3 scatterPos;

        public PieceData(string name, Vector3 targetPos, Vector3 targetRot, Vector3 scale, Color color, Vector3 scatterPos)
        {
            this.name = name;
            this.targetPos = targetPos;
            this.targetRot = targetRot;
            this.scale = scale;
            this.color = color;
            this.scatterPos = scatterPos;
        }
    }
}
