using UnityEngine;

// 맵 모서리의 1/4 원 스폰 구역
public class SpawnZone : MonoBehaviour
{
    public float radius = 4f;
    public Vector3 inwardX = Vector3.right; // 모서리에서 맵 안쪽으로 향하는 두 방향
    public Vector3 inwardZ = Vector3.back;

    public static SpawnZone Create(Transform parent, string name, Vector3 corner, Vector3 inX, Vector3 inZ, float radius, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = corner;

        var zone = go.AddComponent<SpawnZone>();
        zone.radius = radius;
        zone.inwardX = inX;
        zone.inwardZ = inZ;
        zone.BuildVisual(color);
        return zone;
    }

    public Vector3 GetSpawnPoint()
    {
        Vector3 p = transform.position;
        for (int attempt = 0; attempt < 12; attempt++)
        {
            float a = Random.Range(0f, Mathf.PI * 0.5f);
            float r = Random.Range(0.8f, radius - 0.3f);
            float ox = Mathf.Max(Mathf.Cos(a) * r, 0.7f); // 벽에 끼지 않도록 최소 거리
            float oz = Mathf.Max(Mathf.Sin(a) * r, 0.7f);
            p = transform.position + inwardX * ox + inwardZ * oz;

            // 다른 좀비랑 겹치지 않는 자리 찾기
            if (!Physics.CheckCapsule(p + Vector3.up * 0.6f, p + Vector3.up * 1.3f, 0.45f,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                return p;
        }
        return p;
    }

    void BuildVisual(Color color)
    {
        const int segments = 24;
        var verts = new Vector3[segments + 2];
        var tris = new int[segments * 3];
        Vector3 lift = Vector3.up * 0.02f;

        verts[0] = lift;
        for (int i = 0; i <= segments; i++)
        {
            float a = Mathf.PI * 0.5f * i / segments;
            verts[i + 1] = (inwardX * Mathf.Cos(a) + inwardZ * Mathf.Sin(a)) * radius + lift;
        }

        // 위에서 봤을 때 앞면이 보이도록 감김 방향 맞춤
        bool flip = Vector3.Cross(inwardX, inwardZ).y < 0f;
        for (int i = 0; i < segments; i++)
        {
            tris[i * 3] = 0;
            tris[i * 3 + 1] = flip ? i + 2 : i + 1;
            tris[i * 3 + 2] = flip ? i + 1 : i + 2;
        }

        var mesh = new Mesh { name = "SpawnZoneMesh" };
        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        var visual = new GameObject("Visual");
        visual.transform.SetParent(transform, false);
        visual.AddComponent<MeshFilter>().sharedMesh = mesh;
        visual.AddComponent<MeshRenderer>();
        GameBootstrap.Paint(visual, color);
    }
}
