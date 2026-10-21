using UnityEngine;

// 손그림 맵을 월드 좌표로 옮긴 값 (위쪽이 +Z)
//
//  ┌(스폰)──────────────(스폰)┐  z = +25
//  │                          │
//  │        윗방              │
//  ├───────────┐    ┌─────────┤  z = 9.6 (가운데 벽, 문 x = -0.5 ~ 3.2)
//  │                          │
//  │        아랫방  (시작)    │
//  │                          │
//  └──────────────────────────┘  z = -25
//  x = -16                x = +16
public static class MapLayout
{
    public const float HalfWidth = 16f;
    public const float HalfDepth = 25f;
    public const float WallThickness = 1f;
    public const float WallHeight = 2f;

    public const float MidWallZ = 9.6f;
    public const float MidWallThickness = 0.6f;
    public const float DoorMinX = -0.5f;
    public const float DoorMaxX = 3.2f;

    public const float LeftSpawnRadius = 4.5f;
    public const float RightSpawnRadius = 3.5f;

    public static readonly Vector3 PlayerStart = new Vector3(1.3f, 1f, -12f);
    public static readonly Vector3 LeftSpawnCorner = new Vector3(-HalfWidth, 0f, HalfDepth);
    public static readonly Vector3 RightSpawnCorner = new Vector3(HalfWidth, 0f, HalfDepth);

    public static float DoorCenterX => (DoorMinX + DoorMaxX) * 0.5f;

    public static bool IsUpperRoom(Vector3 p) => p.z > MidWallZ;

    public static bool SameRoom(Vector3 a, Vector3 b) => IsUpperRoom(a) == IsUpperRoom(b);

    // 다른 방에 있는 목표로 가기 위해 지금 향해야 할 지점 (문 앞 → 문 통과)
    public static Vector3 DoorWaypoint(Vector3 from)
    {
        float side = IsUpperRoom(from) ? 1f : -1f;
        bool inDoorLane = from.x > DoorMinX + 0.5f && from.x < DoorMaxX - 0.5f;

        if (inDoorLane && Mathf.Abs(from.z - MidWallZ) < 2.5f)
        {
            float x = Mathf.Clamp(from.x, DoorMinX + 0.6f, DoorMaxX - 0.6f);
            return new Vector3(x, from.y, MidWallZ - side * 1.5f);
        }
        return new Vector3(DoorCenterX, from.y, MidWallZ + side * 1.8f);
    }
}
