using UnityEngine;

public class BallController : MonoBehaviour
{
    [Header("Física")]
    public float gravityScale  = 2.0f;
    public float maxFallSpeed  = 15f;
    public float bounceForce   = 5f;  

    private Rigidbody rb;
    private bool      isDead = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity  = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation |
                         RigidbodyConstraints.FreezePositionX |
                         RigidbodyConstraints.FreezePositionZ;
    }

    void FixedUpdate()
    {
        if (isDead) return;

        Vector3 vel = rb.linearVelocity;
        vel.y -= gravityScale * 9.8f * Time.fixedDeltaTime;
        vel.y  = Mathf.Max(vel.y, -maxFallSpeed);
        rb.linearVelocity = vel;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (isDead) return;

        if (collision.gameObject.CompareTag("Danger"))
        {
            Die();
        }
        else if (collision.gameObject.CompareTag("Safe"))
        {
            Vector3 vel = rb.linearVelocity;
            vel.y = bounceForce;
            rb.linearVelocity = vel;
        }
    }

    void Die()
    {
        isDead            = true;
        rb.linearVelocity = Vector3.zero;
        rb.useGravity     = false;
        Debug.Log("Game Over");
    }

    public void ResetBall(Vector3 startPosition)
    {
        isDead             = false;
        transform.position = startPosition;
        rb.linearVelocity  = Vector3.zero;
    }
}