using UnityEngine;

// 매 프레임 SphereCast로 충돌을 검사해서 빠른 총알도 벽/좀비를 뚫지 않음
public class Bullet : MonoBehaviour
{
    const float Radius = 0.12f;

    Vector3 direction;
    float speed;
    float damage;
    float lifeTime = 2f;
    GameObject owner;

    public static void Spawn(Vector3 position, Vector3 direction, float speed, float damage, GameObject owner, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Bullet";
        DestroyImmediate(go.GetComponent<Collider>());
        go.transform.position = position;
        go.transform.localScale = Vector3.one * Radius * 2.5f;
        GameBootstrap.Paint(go, color);
        if (GameBootstrap.Root != null) go.transform.SetParent(GameBootstrap.Root, true);

        var b = go.AddComponent<Bullet>();
        b.direction = direction.normalized;
        b.speed = speed;
        b.damage = damage;
        b.owner = owner;
    }

    void Update()
    {
        float step = speed * Time.deltaTime;

        if (Physics.SphereCast(transform.position, Radius, direction, out RaycastHit hit, step,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (owner == null || !hit.collider.transform.IsChildOf(owner.transform))
            {
                var health = hit.collider.GetComponentInParent<Health>();
                if (health != null) health.TakeDamage(damage);
                Destroy(gameObject);
                return;
            }
        }

        transform.position += direction * step;

        lifeTime -= Time.deltaTime;
        if (lifeTime <= 0f) Destroy(gameObject);
    }
}
