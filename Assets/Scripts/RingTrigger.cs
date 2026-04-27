using UnityEngine;
using System;

public class RingTrigger : MonoBehaviour
{
    public static event Action OnRingPassed;

    [Header("Audio")]
    public AudioClip ringBreakClip;
    [Range(0f, 1f)] public float ringBreakVolume = 0.8f;
    [Range(0f, 0.2f)] public float ringBreakPitchJitter = 0.05f;

    private RingBuilder parentRing;
    private float       ringY;
    private bool        triggered  = false;
    private bool        ballWasAbove = false;
    private Transform   ball;
    private AudioSource audioSource;

    public void Setup(RingBuilder ring, float y)
    {
        parentRing = ring;
        ringY      = y;
    }

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

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

        PlayRingBreakSfx();

        OnRingPassed?.Invoke();

        Debug.Log("Anillo superado!");
    }

    void PlayRingBreakSfx()
    {
        if (ringBreakClip == null || audioSource == null)
            return;

        audioSource.pitch = 1f + UnityEngine.Random.Range(-ringBreakPitchJitter, ringBreakPitchJitter);
        audioSource.PlayOneShot(ringBreakClip, ringBreakVolume);
    }
}