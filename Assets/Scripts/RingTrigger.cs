using UnityEngine;

public class RingTrigger : MonoBehaviour
{
    private RingBuilder parentRing;
    private float       ringY;
    private bool        triggered  = false;
    private bool        ballWasAbove = false;
    private Transform   ball;

    public void Setup(RingBuilder ring, float y)
    {
        parentRing = ring;
        ringY      = y;
    }

    void Start()
    {
        GameObject ballGO = GameObject.FindGameObjectWithTag("Ball");
        if (ballGO != null) ball = ballGO.transform;
    }

    void Update()
    {
        if (triggered || ball == null) return;

        bool isAbove = ball.position.y > ringY;

        if (ballWasAbove && !isAbove)
        {
            triggered = true;
            DisableRing();
        }

        ballWasAbove = isAbove;
    }

    void DisableRing()
    {
        if (parentRing == null) return;

        foreach (var seg in parentRing.segments)
            if (seg != null) seg.gameObject.SetActive(false);

        Debug.Log("Anillo superado!");
    }
}