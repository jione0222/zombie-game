using System;
using UnityEngine;

public enum WeaponType { Pistol = 0, Shotgun = 1, Rifle = 2, Sniper = 3 }

[Serializable]
public class WeaponData
{
    public WeaponType type;
    public string displayName;
    public float damage;
    public int magazineSize;
    public float fireInterval;   // 발사 사이 간격(초)
    public float reloadTime;     // 재장전 시간(초)
    public int pellets = 1;      // 한 번에 나가는 총알 수
    public float spreadAngle;    // 산탄 퍼짐 각도
    public float bulletSpeed = 35f;

    public static WeaponData[] CreateDefaults()
    {
        return new[]
        {
            new WeaponData { type = WeaponType.Pistol,  displayName = "PISTOL",  damage = 25f,  magazineSize = 6,  fireInterval = 0.7f, reloadTime = 1.2f },
            new WeaponData { type = WeaponType.Shotgun, displayName = "SHOTGUN", damage = 25f,  magazineSize = 2,  fireInterval = 0.2f, reloadTime = 1.0f, pellets = 4, spreadAngle = 20f },
            new WeaponData { type = WeaponType.Rifle,   displayName = "RIFLE",   damage = 20f,  magazineSize = 40, fireInterval = 0.1f, reloadTime = 1.5f, bulletSpeed = 45f },
            new WeaponData { type = WeaponType.Sniper,  displayName = "SNIPER",  damage = 100f, magazineSize = 1,  fireInterval = 0f,   reloadTime = 1.5f, bulletSpeed = 70f },
        };
    }

    public static Color ColorOf(WeaponType type)
    {
        switch (type)
        {
            case WeaponType.Shotgun: return new Color(1f, 0.55f, 0.15f);
            case WeaponType.Rifle: return new Color(0.7f, 0.4f, 1f);
            case WeaponType.Sniper: return new Color(0.3f, 0.9f, 1f);
            default: return new Color(1f, 0.9f, 0.3f);
        }
    }
}
