using System.Collections.Generic;
using UnityEngine;

public enum PickupKind { Weapon, Health }

// 좀비가 떨군 보상. 가까이 가면 자동으로 획득, 30초 뒤 사라짐
public class Pickup : MonoBehaviour
{
    public static readonly List<Pickup> All = new List<Pickup>();

    public PickupKind kind;
    public WeaponType weapon;
    public float healAmount = 10f;
    public float lifeTime = 30f;
    public float pickupRadius = 1.3f;

    float age;
    float baseY;
    Renderer rend;

    public string Label => kind == PickupKind.Health ? $"+{healAmount:0} HP" : weapon.ToString().ToUpper();
    public Color LabelColor => kind == PickupKind.Health ? new Color(0.3f, 1f, 0.4f) : WeaponData.ColorOf(weapon);

    public static Pickup SpawnWeapon(WeaponType type, Vector3 groundPos)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.transform.localScale = new Vector3(0.9f, 0.3f, 0.3f);
        GameBootstrap.Paint(go, WeaponData.ColorOf(type));
        var p = Setup(go, groundPos, $"Pickup_{type}");
        p.kind = PickupKind.Weapon;
        p.weapon = type;
        return p;
    }

    public static Pickup SpawnHealth(Vector3 groundPos, float amount)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.transform.localScale = Vector3.one * 0.5f;
        GameBootstrap.Paint(go, new Color(0.3f, 1f, 0.4f));
        var p = Setup(go, groundPos, "Pickup_Health");
        p.kind = PickupKind.Health;
        p.healAmount = amount;
        return p;
    }

    static Pickup Setup(GameObject go, Vector3 groundPos, string name)
    {
        go.name = name;
        DestroyImmediate(go.GetComponent<Collider>());
        go.transform.position = groundPos + Vector3.up * 0.6f;
        if (GameBootstrap.Root != null) go.transform.SetParent(GameBootstrap.Root, true);
        return go.AddComponent<Pickup>();
    }

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    void Start()
    {
        baseY = transform.position.y;
        rend = GetComponent<Renderer>();
    }

    void Update()
    {
        age += Time.deltaTime;
        if (age >= lifeTime)
        {
            Destroy(gameObject);
            return;
        }

        transform.Rotate(0f, 120f * Time.deltaTime, 0f, Space.World);
        Vector3 pos = transform.position;
        pos.y = baseY + Mathf.Sin(age * 3f) * 0.15f;
        transform.position = pos;

        // 사라지기 5초 전부터 깜빡임
        if (rend != null) rend.enabled = lifeTime - age > 5f || Mathf.FloorToInt(age * 8f) % 2 == 0;

        var gm = GameManager.Instance;
        if (gm == null || gm.Player == null || gm.IsFinished) return;
        Vector3 d = gm.Player.position - transform.position;
        d.y = 0f;
        if (d.sqrMagnitude <= pickupRadius * pickupRadius) TryCollect(gm);
    }

    void TryCollect(GameManager gm)
    {
        if (kind == PickupKind.Health)
        {
            if (gm.PlayerHealth == null || gm.PlayerHealth.Current >= gm.PlayerHealth.maxHealth) return; // 체력 가득이면 남겨둠
            gm.PlayerHealth.Heal(healAmount);
        }
        else
        {
            if (gm.PlayerWeapons == null) return;
            gm.PlayerWeapons.Give(weapon);
        }
        gm.ShowMessage($"GOT {Label}");
        Destroy(gameObject);
    }
}
