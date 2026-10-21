using System.Collections.Generic;
using UnityEngine;

// 로비 화면 → START 누르면 손그림 맵(MapLayout) 그대로 게임을 자동 생성
public class GameBootstrap : MonoBehaviour
{
    public static Transform Root { get; private set; }

    public float playerMaxHealth = 100f;

    static readonly Dictionary<Color, Material> materialCache = new Dictionary<Color, Material>();
    static Material baseMaterial;

    public bool InLobby { get; private set; }

    GUIStyle titleStyle, subtitleStyle, buttonStyle, infoStyle;

    void Start()
    {
        EnsureLight();
        ShowLobby();
    }

    public void StartGame()
    {
        if (Root != null) Destroy(Root.gameObject);
        InLobby = false;
        Build();
    }

    public void Restart()
    {
        StartGame();
    }

    public void ShowLobby()
    {
        if (Root != null) Destroy(Root.gameObject);
        Root = null;
        InLobby = true;

        var cam = Camera.main;
        if (cam != null)
        {
            var follow = cam.GetComponent<CameraFollow>();
            if (follow != null) follow.target = null;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.06f, 0.07f, 0.06f);
        }
    }

    // ───────────── 로비 화면 ─────────────

    void Update()
    {
        if (InLobby && GameInput.RestartPressed) StartGame(); // Enter 또는 R로도 시작
    }

    void OnGUI()
    {
        if (!InLobby) return;
        EnsureLobbyStyles();

        float cx = Screen.width / 2f;
        float y = Screen.height * 0.22f;

        GUI.Label(new Rect(0, y, Screen.width, 80), "ZOMBIE SURVIVAL", titleStyle);
        GUI.Label(new Rect(0, y + 80, Screen.width, 30), "Survive 5 waves", subtitleStyle);

        if (GUI.Button(new Rect(cx - 120, y + 150, 240, 56), "START", buttonStyle)) StartGame();
        if (GUI.Button(new Rect(cx - 120, y + 220, 240, 56), "QUIT", buttonStyle)) Quit();

        GUI.Label(new Rect(0, y + 310, Screen.width, 120),
            "WASD  Move      Mouse  Aim      Click  Shoot\n" +
            "R  Reload      1~4 / Q  Switch weapon\n\n" +
            "Kill zombies to find Shotgun, Rifle, Sniper and Health packs", infoStyle);
    }

    void EnsureLobbyStyles()
    {
        if (titleStyle != null) return;
        titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 60, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        titleStyle.normal.textColor = new Color(0.55f, 0.9f, 0.4f);
        subtitleStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, alignment = TextAnchor.MiddleCenter };
        subtitleStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
        buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 24, fontStyle = FontStyle.Bold };
        infoStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, alignment = TextAnchor.UpperCenter };
        infoStyle.normal.textColor = new Color(0.65f, 0.65f, 0.65f);
    }

    public static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void Build()
    {
        Root = new GameObject("GameRoot").transform;

        BuildMap();

        var manager = new GameObject("GameManager").AddComponent<GameManager>();
        manager.transform.SetParent(Root);

        var player = BuildPlayer();
        manager.RegisterPlayer(player);

        var waves = new GameObject("WaveManager").AddComponent<WaveManager>();
        waves.transform.SetParent(Root);
        waves.zones.Add(SpawnZone.Create(Root, "SpawnZone_Left", MapLayout.LeftSpawnCorner,
            Vector3.right, Vector3.back, MapLayout.LeftSpawnRadius, new Color(0.55f, 0.15f, 0.15f)));
        waves.zones.Add(SpawnZone.Create(Root, "SpawnZone_Right", MapLayout.RightSpawnCorner,
            Vector3.left, Vector3.back, MapLayout.RightSpawnRadius, new Color(0.55f, 0.15f, 0.15f)));
        waves.Init(player.GetComponent<PlayerController>());
        manager.RegisterWaves(waves);

        SetupCamera(player.transform);
    }

    // ───────────── 맵 ─────────────

    void BuildMap()
    {
        float w = MapLayout.HalfWidth;
        float d = MapLayout.HalfDepth;
        float t = MapLayout.WallThickness;
        float h = MapLayout.WallHeight;
        var wallColor = new Color(0.45f, 0.45f, 0.5f);

        // 바닥 (Plane 기본 크기 10x10)
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.SetParent(Root);
        ground.transform.localScale = new Vector3((w + t) / 5f, 1f, (d + t) / 5f);
        Paint(ground, new Color(0.22f, 0.25f, 0.28f));

        // 바깥 벽 (안쪽 면이 정확히 ±HalfWidth, ±HalfDepth)
        CreateWall("Wall_Top", new Vector3(0, h / 2, d + t / 2), new Vector3((w + t) * 2, h, t), wallColor);
        CreateWall("Wall_Bottom", new Vector3(0, h / 2, -d - t / 2), new Vector3((w + t) * 2, h, t), wallColor);
        CreateWall("Wall_Left", new Vector3(-w - t / 2, h / 2, 0), new Vector3(t, h, d * 2), wallColor);
        CreateWall("Wall_Right", new Vector3(w + t / 2, h / 2, 0), new Vector3(t, h, d * 2), wallColor);

        // 가운데 벽 (문 양옆)
        float z = MapLayout.MidWallZ;
        float mt = MapLayout.MidWallThickness;
        float leftLen = MapLayout.DoorMinX + w;
        float rightLen = w - MapLayout.DoorMaxX;
        CreateWall("Wall_Mid_Left", new Vector3(-w + leftLen / 2, h / 2, z), new Vector3(leftLen, h, mt), wallColor);
        CreateWall("Wall_Mid_Right", new Vector3(MapLayout.DoorMaxX + rightLen / 2, h / 2, z), new Vector3(rightLen, h, mt), wallColor);
    }

    void CreateWall(string name, Vector3 pos, Vector3 scale, Color color)
    {
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.SetParent(Root);
        wall.transform.position = pos;
        wall.transform.localScale = scale;
        Paint(wall, color);
    }

    // ───────────── 플레이어 ─────────────

    GameObject BuildPlayer()
    {
        var player = new GameObject("Player");
        player.tag = "Player";
        player.transform.SetParent(Root);
        player.transform.position = MapLayout.PlayerStart;

        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        DestroyImmediate(body.GetComponent<Collider>());
        body.transform.SetParent(player.transform, false);
        Paint(body, new Color(0.2f, 0.6f, 1f));

        var gun = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gun.name = "Gun";
        DestroyImmediate(gun.GetComponent<Collider>());
        gun.transform.SetParent(player.transform, false);
        gun.transform.localPosition = new Vector3(0.25f, 0.3f, 0.6f);
        gun.transform.localScale = new Vector3(0.2f, 0.2f, 0.7f);
        Paint(gun, new Color(0.15f, 0.15f, 0.2f));

        var cc = player.AddComponent<CharacterController>();
        cc.height = 2f;
        cc.radius = 0.5f;
        cc.center = Vector3.zero;
        cc.stepOffset = 0.1f;

        player.AddComponent<Health>().Init(playerMaxHealth);
        player.AddComponent<PlayerController>();
        player.AddComponent<PlayerWeapons>();
        return player;
    }

    void SetupCamera(Transform target)
    {
        var cam = Camera.main;
        if (cam == null)
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
        }
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.08f, 0.08f, 0.1f);

        var follow = cam.GetComponent<CameraFollow>();
        if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
        follow.target = target;
        follow.SnapToTarget();
    }

    static void EnsureLight()
    {
        if (FindAnyObjectByType<Light>() != null) return;
        var lightGo = new GameObject("Directional Light");
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.shadows = LightShadows.Soft;
        lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }

    // ───────────── 머티리얼 ─────────────

    static Material BaseMaterial
    {
        get
        {
            if (baseMaterial == null)
            {
                var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                baseMaterial = tmp.GetComponent<Renderer>().sharedMaterial;
                DestroyImmediate(tmp);
            }
            return baseMaterial;
        }
    }

    // 같은 색은 머티리얼을 공유해서 총알이 많아도 머티리얼이 무한히 늘지 않게 함
    public static void Paint(GameObject go, Color color)
    {
        var rend = go.GetComponent<Renderer>();
        if (rend == null) return;
        if (!materialCache.TryGetValue(color, out var mat) || mat == null)
        {
            mat = new Material(BaseMaterial) { color = color };
            materialCache[color] = mat;
        }
        rend.sharedMaterial = mat;
    }
}

// 씬에 GameBootstrap이 없어도 Play 누르면 자동으로 만들어서 바로 시작
static class GameAutoStart
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void CreateBootstrapIfMissing()
    {
        if (Object.FindAnyObjectByType<GameBootstrap>() != null) return;
        new GameObject("GameBootstrap").AddComponent<GameBootstrap>();
    }
}
