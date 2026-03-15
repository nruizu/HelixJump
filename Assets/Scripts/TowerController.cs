using UnityEngine;
using UnityEngine.InputSystem;

public class TowerController : MonoBehaviour
{
    [Header("Rotación")]
    public float rotationSpeed = 0.5f;

    private float lastMouseX;
    private bool  isDragging;

    void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        // Comenzar arrastre
        if (mouse.leftButton.wasPressedThisFrame)
        {
            lastMouseX = mouse.position.ReadValue().x;
            isDragging = true;
        }

        // Soltar
        if (mouse.leftButton.wasReleasedThisFrame)
            isDragging = false;

        // Rotar mientras arrastra
        if (isDragging && mouse.leftButton.isPressed)
        {
            float currentX = mouse.position.ReadValue().x;
            float delta    = currentX - lastMouseX;
            lastMouseX     = currentX;

            transform.Rotate(Vector3.up, -delta * rotationSpeed, Space.World);

            Debug.Log($"Delta: {delta}  RotY: {transform.eulerAngles.y}");
        }

        // Touch (móvil)
        var touchscreen = Touchscreen.current;
        if (touchscreen != null && touchscreen.primaryTouch.press.isPressed)
        {
            float touchDelta = touchscreen.primaryTouch.delta.ReadValue().x;
            transform.Rotate(Vector3.up, -touchDelta * rotationSpeed, Space.World);
        }
    }
}