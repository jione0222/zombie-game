using UnityEngine;

// 게임 상태(게임오버/승리), 킬 수, 화면 HUD(OnGUI)
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public Transform Player { get; private set; }
    public Health PlayerHealth { get; private set; }
    public PlayerWeapons PlayerWeapons { get; private set; }
    public WaveManager Waves { get; private set; }

    public int Kills { get; private set; }
    public bool IsGameOver { get; private set; }
    public bool IsVictory { get; private set; }
    public bool IsFinished => IsGameOver || IsVictory;
    public float PlayTime { get; private set; }

    string message;
    float messageUntil;
    float hurtFlashUntil;
    GUIStyle labelStyle, centerStyle, bigStyle, smallCenterStyle, buttonStyle;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void RegisterPlayer(GameObject player)
    {
        Player = player.transform;
        PlayerHealth = player.GetComponent<Health>();
        PlayerWeapons = player.GetComponent<PlayerWeapons>();
        PlayerHealth.OnDeath += () => IsGameOver = true;
        PlayerHealth.OnDamaged += _ => hurtFlashUntil = Time.time + 0.15f;
    }

    public void RegisterWaves(WaveManager waves) { Waves = waves; }

    public void RegisterKill() { if (!IsFinished) Kills++; }

    public void Victory() { if (!IsFinished) IsVictory = true; }

    public void ShowMessage(string text, float duration = 1.5f)
    {
        message = text;
        messageUntil = Time.time + duration;
    }

    void Update()
    {
        if (!IsFinished)
        {
            PlayTime += Time.deltaTime;
        }
        else if (GameInput.RestartPressed)
        {
            Restart();
        }
    }

    void Restart()
    {
        var bootstrap = FindAnyObjectByType<GameBootstrap>();
        if (bootstrap != null) bootstrap.Restart();
    }

    void BackToLobby()
    {
        var bootstrap = FindAnyObjectByType<GameBootstrap>();
        if (bootstrap != null) bootstrap.ShowLobby();
    }

    // ───────────── HUD ─────────────

    void OnGUI()
    {
        EnsureStyles();
        DrawWorldOverlays();
        DrawHud();

        if (Time.time < hurtFlashUntil) FillScreen(new Color(1f, 0f, 0f, 0.25f));

        if (IsGameOver) DrawEndPanel("GAME OVER", new Color(1f, 0.3f, 0.3f));
        else if (IsVictory) DrawEndPanel("VICTORY!", new Color(0.4f, 1f, 0.5f));
    }

    void DrawWorldOverlays()
    {
        var cam = Camera.main;
        if (cam == null) return;

        // 맞은 좀비 체력바
        foreach (var z in Zombie.All)
        {
            var h = z.Health;
            if (h == null || h.Current >= h.maxHealth) continue;
            if (WorldToGui(cam, z.transform.position + Vector3.up * 1.3f, out Vector2 p))
                DrawBar(new Rect(p.x - 20, p.y, 40, 6), h.Current / h.maxHealth, new Color(0.9f, 0.2f, 0.2f));
        }

        // 드롭 아이템 이름
        foreach (var pickup in Pickup.All)
        {
            if (WorldToGui(cam, pickup.transform.position + Vector3.up * 0.7f, out Vector2 p))
            {
                smallCenterStyle.normal.textColor = pickup.LabelColor;
                GUI.Label(new Rect(p.x - 50, p.y - 10, 100, 20), pickup.Label, smallCenterStyle);
            }
        }

        // 머리 위 장전 바
        if (Player != null && PlayerWeapons != null && PlayerWeapons.IsReloading && !IsFinished)
        {
            if (WorldToGui(cam, Player.position + Vector3.up * 1.6f, out Vector2 p))
            {
                smallCenterStyle.normal.textColor = Color.white;
                GUI.Label(new Rect(p.x - 50, p.y - 22, 100, 20), "RELOADING", smallCenterStyle);
                DrawBar(new Rect(p.x - 35, p.y, 70, 9), PlayerWeapons.ReloadProgress, new Color(1f, 0.85f, 0.2f));
            }
        }
    }

    void DrawHud()
    {
        // 체력
        if (PlayerHealth != null)
        {
            var r = new Rect(20, 20, 240, 24);
            DrawBar(r, PlayerHealth.Current / PlayerHealth.maxHealth,
                Color.Lerp(new Color(0.9f, 0.2f, 0.2f), new Color(0.3f, 0.9f, 0.4f), PlayerHealth.Current / PlayerHealth.maxHealth));
            centerStyle.normal.textColor = Color.white;
            GUI.Label(r, $"HP {PlayerHealth.Current:0} / {PlayerHealth.maxHealth:0}", centerStyle);
        }

        labelStyle.normal.textColor = Color.white;
        if (Waves != null) GUI.Label(new Rect(20, 52, 500, 24), Waves.StatusText, labelStyle);
        GUI.Label(new Rect(20, 76, 500, 24), $"Zombies: {Zombie.All.Count}    Kills: {Kills}", labelStyle);

        // 무기 목록
        if (PlayerWeapons != null)
        {
            float x = 20;
            float y = Screen.height - 44;
            for (int i = 0; i < 4; i++)
            {
                var type = (WeaponType)i;
                if (!PlayerWeapons.Owns(type)) continue;
                var data = PlayerWeapons.Data(type);
                bool current = type == PlayerWeapons.Current;

                var r = new Rect(x, y, 170, 28);
                var prev = GUI.color;
                GUI.color = current ? new Color(0f, 0f, 0f, 0.75f) : new Color(0f, 0f, 0f, 0.4f);
                GUI.DrawTexture(r, Texture2D.whiteTexture);
                GUI.color = prev;

                centerStyle.normal.textColor = current ? WeaponData.ColorOf(type) : new Color(0.7f, 0.7f, 0.7f);
                string ammoText = current && PlayerWeapons.IsReloading ? "..." : $"{PlayerWeapons.AmmoOf(type)}/{data.magazineSize}";
                GUI.Label(r, $"[{i + 1}] {data.displayName}  {ammoText}", centerStyle);
                x += 178;
            }
        }

        // 가운데 알림
        if (!string.IsNullOrEmpty(message) && Time.time < messageUntil)
        {
            bigStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(0, Screen.height * 0.2f, Screen.width, 50), message, bigStyle);
        }
    }

    void DrawEndPanel(string title, Color color)
    {
        FillScreen(new Color(0f, 0f, 0f, 0.55f));
        bigStyle.normal.textColor = color;
        GUI.Label(new Rect(0, Screen.height / 2f - 70, Screen.width, 60), title, bigStyle);

        centerStyle.normal.textColor = Color.white;
        int wave = Waves != null ? Waves.CurrentWave : 0;
        GUI.Label(new Rect(0, Screen.height / 2f, Screen.width, 30),
            $"Wave {wave}   Kills {Kills}   Time {PlayTime:0}s", centerStyle);
        float cx = Screen.width / 2f;
        float by = Screen.height / 2f + 50;
        if (GUI.Button(new Rect(cx - 170, by, 160, 44), "RESTART (R)", buttonStyle)) Restart();
        if (GUI.Button(new Rect(cx + 10, by, 160, 44), "LOBBY", buttonStyle)) BackToLobby();
    }

    void EnsureStyles()
    {
        if (labelStyle != null) return;
        labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold };
        centerStyle = new GUIStyle(labelStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 16 };
        smallCenterStyle = new GUIStyle(labelStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 13 };
        bigStyle = new GUIStyle(labelStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 40 };
        buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 18, fontStyle = FontStyle.Bold };
    }

    static bool WorldToGui(Camera cam, Vector3 world, out Vector2 guiPos)
    {
        Vector3 s = cam.WorldToScreenPoint(world);
        guiPos = new Vector2(s.x, Screen.height - s.y);
        return s.z > 0f;
    }

    static void DrawBar(Rect r, float ratio, Color fill)
    {
        var prev = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.65f);
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = fill;
        GUI.DrawTexture(new Rect(r.x + 1, r.y + 1, (r.width - 2) * Mathf.Clamp01(ratio), r.height - 2), Texture2D.whiteTexture);
        GUI.color = prev;
    }

    static void FillScreen(Color color)
    {
        var prev = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = prev;
    }
}
