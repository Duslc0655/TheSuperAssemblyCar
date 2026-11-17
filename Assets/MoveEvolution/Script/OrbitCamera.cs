using UnityEngine;
using UnityEngine.InputSystem;

public class OrbitCamera : MonoBehaviour
{
    public Transform followTarget;      // 試駕時由 GridBuilder 自動設定
    public Vector3 center = new Vector3(0, 1, 0);
    public float distance = 11f;
    public float minDistance = 3f;
    public float maxDistance = 30f;
    public float rotateSpeed = 0.2f;
    public float zoomSpeed = 1f;

    float yaw, pitch;

    void Start()
    {
        Vector3 a = transform.eulerAngles;
        yaw = a.y;
        pitch = a.x > 180f ? a.x - 360f : a.x;
    }

    void LateUpdate()
    {
        var mouse = Mouse.current;
        var kb = Keyboard.current;
        if (mouse == null) return;

        bool altLeft = kb != null && kb.leftAltKey.isPressed && mouse.leftButton.isPressed;
        if (mouse.middleButton.isPressed || altLeft)
        {
            Vector2 d = mouse.delta.ReadValue();
            yaw += d.x * rotateSpeed;
            pitch = Mathf.Clamp(pitch - d.y * rotateSpeed, 5f, 85f);
        }

        float scroll = mouse.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.01f)
            distance = Mathf.Clamp(distance - Mathf.Sign(scroll) * zoomSpeed, minDistance, maxDistance);

        Vector3 c = followTarget != null ? followTarget.position + Vector3.up * 0.5f : center;
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        transform.position = c - transform.forward * distance;
    }
}