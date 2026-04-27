using UnityEngine;
using System.Collections.Generic;

public class LevelGenerator : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject ringPrefab;

    [Header("Torre")]
    public int   initialRings   = 15;
    public float ringSpacing    = 1.2f;
    public float startY         = 0f;

    [Header("Dificultad por anillo")]
    public int   totalSegments  = 12;
    public int   emptySegments  = 3;
    public int   dangerSegments = 2;

    [Header("Generación infinita")]
    public Transform ball;
    public float     generateAheadDist = 5f;

    private List<GameObject> activeRings = new List<GameObject>();
    private float nextRingY;
    private int   ringCount = 0;

    void Start()
    {
        nextRingY = startY;
        for (int i = 0; i < initialRings; i++)
            SpawnRing();
    }

    void Update()
    {
        if (ball == null) return;

        while (nextRingY > ball.position.y - generateAheadDist)
            SpawnRing();

        CleanupOldRings();
    }

    void SpawnRing()
    {
        if (ringPrefab == null)
        {
            Debug.LogError("LevelGenerator: falta asignar ringPrefab en el Inspector.");
            return;
        }

        GameObject ring = Instantiate(ringPrefab, new Vector3(0, nextRingY, 0), Quaternion.identity);
        ring.transform.SetParent(transform);

        RingBuilder rb = ring.GetComponent<RingBuilder>();
        if (rb != null)
        {
            rb.totalSegments = totalSegments;
            rb.randomRotation = true;

            if (ringCount == 0)
            {
                // Primer anillo: 1 hueco para que la pelota pueda bajar, sin peligro
                rb.emptySegments     = 1;
                rb.dangerSegments    = 0;
                rb.protectSpawnAngle = true;   // el hueco NO cae donde está la pelota
                rb.spawnAngleDeg     = 0f;
            }
            else
            {
                rb.emptySegments     = EmptyForDifficulty();
                rb.dangerSegments    = DangerForDifficulty();
                rb.protectSpawnAngle = false;
            }

            rb.BuildRing();
        }

        activeRings.Add(ring);
        nextRingY -= ringSpacing;
        ringCount++;
    }

    int EmptyForDifficulty()
    {
        int max = totalSegments / 2;
        int val = emptySegments + ringCount / 10;
        return Mathf.Clamp(val, 2, max);
    }

    int DangerForDifficulty()
    {
        int val = dangerSegments + ringCount / 15;
        return Mathf.Clamp(val, 1, totalSegments / 4);
    }

    void CleanupOldRings()
    {
        for (int i = activeRings.Count - 1; i >= 0; i--)
        {
            if (activeRings[i] == null)
            {
                activeRings.RemoveAt(i);
                continue;
            }

            if (activeRings[i].transform.position.y > ball.position.y + 10f)
            {
                Destroy(activeRings[i]);
                activeRings.RemoveAt(i);
            }
        }
    }

    public void ResetTower()
    {
        // Borrar todo lo generado en runs anteriores, incluso si no quedó en activeRings.
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child != null)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
        }

        activeRings.Clear();
        nextRingY = startY;
        ringCount = 0;

        for (int i = 0; i < initialRings; i++)
            SpawnRing();
    }
}