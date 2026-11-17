using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerVehicleInput : MonoBehaviour
{
    IVehicleController vehicle;

    void Update()
    {
        if (vehicle == null) vehicle = GetComponent<IVehicleController>();
        var kb = Keyboard.current;
        if (kb == null || vehicle == null) return;

        float throttle = (kb.wKey.isPressed || kb.upArrowKey.isPressed) ? 1f : 0f;
        bool back = kb.sKey.isPressed || kb.downArrowKey.isPressed;
        float steer = ((kb.dKey.isPressed || kb.rightArrowKey.isPressed) ? 1f : 0f)
                    - ((kb.aKey.isPressed || kb.leftArrowKey.isPressed) ? 1f : 0f);
        float brake = kb.leftShiftKey.isPressed ? 1f : 0f;
        bool handbrake = kb.spaceKey.isPressed;

        if (back)
        {
            if (ForwardSpeed() > 1f) brake = 1f;   // 還在往前衝，S 先當煞車
            else throttle = -1f;
        }

        vehicle.SetInput(throttle, steer, brake, handbrake);
    }

    float ForwardSpeed()
    {
        Transform t = vehicle.VehicleTransform;
        if (t == null) return 0f;
        Rigidbody rb = t.GetComponent<Rigidbody>();
        return rb == null ? 0f : Vector3.Dot(rb.linearVelocity, t.forward);
    }
}