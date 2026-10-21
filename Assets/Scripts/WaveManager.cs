using System.Collections.Generic;
using UnityEngine;

// 웨이브: 30초 간격, 각 스폰 구역에서 1초에 1마리씩. 1웨이브 구역당 5마리, 웨이브마다 +5
public class WaveManager : MonoBehaviour
{
    public int totalWaves = 5;
    public int zombiesPerZoneFirstWave = 5;
    public int increasePerWave = 5;
    public float waveDuration = 30f;
    public float spawnInterval = 1f;
    public float firstWaveDelay = 3f;
    public float zombieSpeedRatio = 0.8f;   // 플레이어 속도 대비
    public bool startNextWaveWhenCleared = false; // 켜면 다 잡았을 때 1분 안 기다리고 다음 웨이브

    public readonly List<SpawnZone> zones = new List<SpawnZone>();

    public int CurrentWave { get; private set; }
    public float TimeLeft { get; private set; }
    public bool IsFinalWave => CurrentWave >= totalWaves;
    public int PendingSpawns => (targetPerZone - spawnedPerZone) * zones.Count;

    PlayerController player;
    int targetPerZone;
    int spawnedPerZone;
    float spawnTimer;

    public void Init(PlayerController playerController)
    {
        player = playerController;
        TimeLeft = firstWaveDelay;
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm != null && gm.IsFinished) return;

        TimeLeft -= Time.deltaTime;

        if (CurrentWave == 0)
        {
            if (TimeLeft <= 0f) StartWave(1);
            return;
        }

        if (spawnedPerZone < targetPerZone)
        {
            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f)
            {
                spawnTimer = spawnInterval;
                SpawnFromAllZones();
            }
        }

        bool cleared = spawnedPerZone >= targetPerZone && Zombie.All.Count == 0;

        if (IsFinalWave)
        {
            if (cleared && gm != null) gm.Victory();
        }
        else if (TimeLeft <= 0f || (startNextWaveWhenCleared && cleared))
        {
            StartWave(CurrentWave + 1);
        }
    }

    void StartWave(int wave)
    {
        CurrentWave = wave;
        TimeLeft = waveDuration;
        targetPerZone = zombiesPerZoneFirstWave + increasePerWave * (wave - 1);
        spawnedPerZone = 0;
        spawnTimer = 0f;

        if (GameManager.Instance != null)
            GameManager.Instance.ShowMessage(IsFinalWave ? $"FINAL WAVE {wave}" : $"WAVE {wave}", 2.5f);
    }

    void SpawnFromAllZones()
    {
        float speed = (player != null ? player.moveSpeed : 6f) * zombieSpeedRatio;
        foreach (var zone in zones)
            Zombie.Spawn(zone.GetSpawnPoint(), speed);
        spawnedPerZone++;
    }

    public string StatusText
    {
        get
        {
            if (CurrentWave == 0) return $"GET READY... {Mathf.CeilToInt(Mathf.Max(0f, TimeLeft))}";
            if (IsFinalWave) return $"WAVE {CurrentWave}/{totalWaves}  (FINAL)  Remaining: {Zombie.All.Count + PendingSpawns}";
            return $"WAVE {CurrentWave}/{totalWaves}   Next wave in {Mathf.CeilToInt(Mathf.Max(0f, TimeLeft))}s";
        }
    }
}
