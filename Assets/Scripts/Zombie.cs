using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 플레이어를 쫓아가고(다른 방이면 문으로 돌아감), 붙으면 2초마다 공격. 죽으면 보상 드롭
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Health))]
public class Zombie : MonoBehaviour
{
    public static readonly List<Zombie> All = new List<Zombie>();

    const float GroundY = 0.9f;

    public float speed = 4.8f;
    public float damage = 10f;
    public float attackInterval = 2f;
    public float attackRange = 1.3f;

    [Header("Drops")]
    public float shotgunDropChance = 0.1f;
    public float rifleDropChance = 0.1f;
    public float sniperDropChance = 0.1f;
    public float healthDropChance = 0.05f;
    public float healAmount = 10f;

    public Health Health { get; private set; }

    CharacterController controller;
    Transform player;
    Health playerHealth;
    Renderer body;
    Color bodyColor;
    float nextAttackTime;

    public static Zombie Spawn(Vector3 position, float speed)
    {
        var go = new GameObject("Zombie");
        go.transform.position = new Vector3(position.x, GroundY, position.z);
        if (GameBootstrap.Root != null) go.transform.SetParent(GameBootstrap.Root, true);

        var cc = go.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.45f;
        cc.center = Vector3.zero;
        cc.stepOffset = 0.1f;

        var bodyGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        bodyGo.name = "Body";
        DestroyImmediate(bodyGo.GetComponent<Collider>());
        bodyGo.transform.SetParent(go.transform, false);
        bodyGo.transform.localScale = Vector3.one * 0.9f;
        GameBootstrap.Paint(bodyGo, new Color(0.35f, 0.6f, 0.3f));

        for (int i = -1; i <= 1; i += 2)
        {
            var arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arm.name = "Arm";
            DestroyImmediate(arm.GetComponent<Collider>());
            arm.transform.SetParent(go.transform, false);
            arm.transform.localPosition = new Vector3(0.3f * i, 0.35f, 0.45f);
            arm.transform.localScale = new Vector3(0.15f, 0.15f, 0.6f);
            GameBootstrap.Paint(arm, new Color(0.25f, 0.42f, 0.2f));
        }

        go.AddComponent<Health>().Init(100f);
        var zombie = go.AddComponent<Zombie>();
        zombie.speed = speed;
        return zombie;
    }

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        Health = GetComponent<Health>();
    }

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    void Start()
    {
        var gm = GameManager.Instance;
        if (gm != null && gm.Player != null)
        {
            player = gm.Player;
            playerHealth = player.GetComponent<Health>();
        }

        body = GetComponentInChildren<Renderer>();
        if (body != null) bodyColor = body.material.color;

        Health.OnDamaged += _ => { if (isActiveAndEnabled) StartCoroutine(Flash()); };
        Health.OnDeath += Die;
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (player == null || gm == null || gm.IsFinished) return;

        Vector3 pos = transform.position;
        Vector3 toPlayer = player.position - pos;
        toPlayer.y = 0f;
        bool sameRoom = MapLayout.SameRoom(pos, player.position);

        if (sameRoom && toPlayer.magnitude <= attackRange)
        {
            Face(toPlayer);
            if (Time.time >= nextAttackTime)
            {
                nextAttackTime = Time.time + attackInterval;
                if (playerHealth != null) playerHealth.TakeDamage(damage);
            }
            return;
        }

        Vector3 goal = sameRoom ? player.position : MapLayout.DoorWaypoint(pos);
        Vector3 dir = goal - pos;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f)
        {
            dir.Normalize();
            Face(dir);
        }

        Vector3 delta = dir * speed * Time.deltaTime;
        delta.y = GroundY - pos.y;
        controller.Move(delta);
    }

    void Face(Vector3 dir)
    {
        if (dir.sqrMagnitude < 0.0001f) return;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);
    }

    IEnumerator Flash()
    {
        if (body == null) yield break;
        body.material.color = Color.white;
        yield return new WaitForSeconds(0.08f);
        if (body != null) body.material.color = bodyColor;
    }

    void Die()
    {
        if (GameManager.Instance != null) GameManager.Instance.RegisterKill();
        DropLoot();
        Destroy(gameObject);
    }

    void DropLoot()
    {
        Vector3 p = transform.position;
        p.y = 0f;

        // 무기는 셋 중 하나만 (각 10%, 합쳐서 30%)
        float roll = Random.value;
        if (roll < shotgunDropChance)
            Pickup.SpawnWeapon(WeaponType.Shotgun, p);
        else if (roll < shotgunDropChance + rifleDropChance)
            Pickup.SpawnWeapon(WeaponType.Rifle, p);
        else if (roll < shotgunDropChance + rifleDropChance + sniperDropChance)
            Pickup.SpawnWeapon(WeaponType.Sniper, p);

        if (Random.value < healthDropChance)
            Pickup.SpawnHealth(p + new Vector3(0.8f, 0f, 0f), healAmount);
    }
}
