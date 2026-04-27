using UnityEngine;
using UnityEngine.InputSystem;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class TowerController : MonoBehaviour
{
    [Header("Rotación")]
    public float rotationSpeed = 0.5f;

    [Header("Cursor")]
    public bool lockAndHideCursor = true;

    void OnEnable()
    {
        ApplyCursorState(true);
    }

    void OnDisable()
    {
        ApplyCursorState(false);
    }

    void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        // Rotar con delta del mouse para no depender de bordes de ventana.
        if (mouse.leftButton.isPressed)
        {
            float delta = mouse.delta.ReadValue().x;
            transform.Rotate(Vector3.up, -delta * rotationSpeed, Space.World);
        }

        // Cierra el juego con ESC.
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            QuitGame();
        }

        // Touch (móvil)
        var touchscreen = Touchscreen.current;
        if (touchscreen != null && touchscreen.primaryTouch.press.isPressed)
        {
            float touchDelta = touchscreen.primaryTouch.delta.ReadValue().x;
            transform.Rotate(Vector3.up, -touchDelta * rotationSpeed, Space.World);
        }
    }

    void ApplyCursorState(bool gameplayActive)
    {
        if (!lockAndHideCursor)
            return;

        if (gameplayActive)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void QuitGame()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}