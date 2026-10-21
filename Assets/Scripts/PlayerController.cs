using UnityEngine;

// WASD 이동, 마우스 방향 조준 (사격은 PlayerWeapons)
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 6f;

    CharacterController controller;
    Camera cam;
    float groundY;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        groundY = transform.position.y;
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm != null && gm.IsFinished) return;
        if (cam == null) cam = Camera.main;

        Move();
        Aim();
    }

    void Move()
    {
        Vector2 input = GameInput.Move;
        Vector3 dir = new Vector3(input.x, 0f, input.y);
        if (dir.sqrMagnitude > 1f) dir.Normalize();

        Vector3 delta = dir * moveSpeed * Time.deltaTime;
        delta.y = groundY - transform.position.y;
        controller.Move(delta);
    }

    void Aim()
    {
        if (cam == null) return;
        Ray ray = cam.ScreenPointToRay(GameInput.MousePosition);
        Plane plane = new Plane(Vector3.up, transform.position);
        if (plane.Raycast(ray, out float enter))
        {
            Vector3 look = ray.GetPoint(enter) - transform.position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(look);
        }
    }
}
