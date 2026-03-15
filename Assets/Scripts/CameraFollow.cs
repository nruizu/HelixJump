using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Referencias")]
    public Transform ball;

    [Header("Configuración")]
    public Vector3 offset      = new Vector3(0f, 8f, -6f);
    public float   smoothSpeed = 8f;

    [Header("Límite inferior")]
    [Tooltip("La cámara nunca dejará la pelota por debajo de este margen en pantalla")]
    public float minBallMargin = 2f;   // Unidades mínimas que la pelota debe estar sobre el borde inferior

    private float         lowestBallY;
    private Quaternion    fixedRotation;

    void Start()
    {
        if (ball == null) return;
        lowestBallY   = ball.position.y;
        fixedRotation = transform.rotation;
    }

    void LateUpdate()
    {
        if (ball == null) return;

        // Actualizar el mínimo Y de la pelota
        if (ball.position.y < lowestBallY)
            lowestBallY = ball.position.y;

        float targetY = lowestBallY + offset.y;

        // Lerp suave hacia la posición target
        float newY = Mathf.Lerp(transform.position.y, targetY, smoothSpeed * Time.deltaTime);

        float maxAllowedY = ball.position.y + offset.y + minBallMargin;
        newY = Mathf.Min(newY, maxAllowedY);

        transform.position = new Vector3(offset.x, newY, offset.z);
        transform.rotation = fixedRotation;
    }

    public void ResetCamera()
    {
        if (ball == null) return;
        lowestBallY        = ball.position.y;
        transform.position = new Vector3(offset.x, lowestBallY + offset.y, offset.z);
        transform.rotation = fixedRotation;
    }
}