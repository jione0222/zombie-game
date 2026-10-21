using System.Collections.Generic;
using UnityEngine;

// 무기 보유/교체/사격/재장전. 수치는 Inspector의 weapons 배열에서 조절 가능
public class PlayerWeapons : MonoBehaviour
{
    public WeaponData[] weapons = WeaponData.CreateDefaults();

    readonly Dictionary<WeaponType, int> ammo = new Dictionary<WeaponType, int>();
    float nextFireTime;
    float reloadStart;
    float reloadEnd;

    public WeaponType Current { get; private set; }
    public bool IsReloading { get; private set; }
    public WeaponData CurrentData => Data(Current);

    public float ReloadProgress =>
        IsReloading ? Mathf.Clamp01((Time.time - reloadStart) / Mathf.Max(0.01f, reloadEnd - reloadStart)) : 0f;

    void Awake()
    {
        Give(WeaponType.Pistol);
    }

    public WeaponData Data(WeaponType type)
    {
        foreach (var w in weapons)
            if (w.type == type) return w;
        return null;
    }

    public bool Owns(WeaponType type) => ammo.ContainsKey(type);

    public int AmmoOf(WeaponType type) => ammo.TryGetValue(type, out int a) ? a : 0;

    // 새 무기를 얻으면 탄창을 채우고 바로 장착 (이미 있으면 탄창만 채움)
    public void Give(WeaponType type)
    {
        ammo[type] = Data(type).magazineSize;
        Equip(type, true);
    }

    public void Equip(WeaponType type, bool force = false)
    {
        if (!Owns(type)) return;
        if (!force && type == Current) return;
        Current = type;
        IsReloading = false;
        nextFireTime = Time.time + 0.15f;
        if (ammo[type] <= 0) StartReload();
    }

    public void StartReload()
    {
        if (IsReloading) return;
        var data = CurrentData;
        if (ammo[Current] >= data.magazineSize) return;
        IsReloading = true;
        reloadStart = Time.time;
        reloadEnd = Time.time + data.reloadTime;
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm != null && gm.IsFinished) return;

        int slot = GameInput.WeaponSlotPressed;
        if (slot >= 0) Equip((WeaponType)slot);
        if (GameInput.NextWeaponPressed) CycleWeapon();

        if (IsReloading && Time.time >= reloadEnd)
        {
            IsReloading = false;
            ammo[Current] = CurrentData.magazineSize;
        }

        if (GameInput.ReloadPressed) StartReload();
        if (GameInput.FireHeld) TryFire();
    }

    void CycleWeapon()
    {
        for (int i = 1; i <= 4; i++)
        {
            var type = (WeaponType)(((int)Current + i) % 4);
            if (Owns(type))
            {
                Equip(type);
                return;
            }
        }
    }

    void TryFire()
    {
        if (IsReloading || Time.time < nextFireTime) return;
        var data = CurrentData;
        if (ammo[Current] <= 0)
        {
            StartReload();
            return;
        }

        ammo[Current]--;
        nextFireTime = Time.time + data.fireInterval;

        // 플레이어 몸 중심에서 쏴야 벽에 붙어 있어도 총알이 벽을 뚫지 않음
        Vector3 origin = transform.position + Vector3.up * 0.3f;
        Color color = WeaponData.ColorOf(data.type);
        for (int i = 0; i < data.pellets; i++)
        {
            float angle = 0f;
            if (data.pellets > 1)
            {
                angle = Mathf.Lerp(-data.spreadAngle * 0.5f, data.spreadAngle * 0.5f, i / (float)(data.pellets - 1))
                      + Random.Range(-2f, 2f);
            }
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * transform.forward;
            Bullet.Spawn(origin, dir, data.bulletSpeed, data.damage, gameObject, color);
        }

        if (ammo[Current] == 0) StartReload();
    }
}
