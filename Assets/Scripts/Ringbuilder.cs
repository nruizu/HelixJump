using UnityEngine;
using System.Collections.Generic;

// Construye un anillo completo compuesto por N segmentos independientes
public class RingBuilder : MonoBehaviour
{
    [Header("Configuración del Anillo")]
    public int   totalSegments  = 12;
    public int   emptySegments  = 3;
    public int   dangerSegments = 2;

    [Header("Dimensiones")]
    public float innerRadius = 0.6f;
    public float outerRadius = 2.0f;
    public float thickness   = 0.6f;

    [Header("Materiales")]
    public Material safeMaterial;
    public Material dangerMaterial;

    [Header("Rotación inicial aleatoria")]
    public bool randomRotation = true;

    [Header("Ángulo protegido")]
    public bool  protectSpawnAngle = false;
    public float spawnAngleDeg     = 0f;

    [HideInInspector] public List<RingSegment> segments = new List<RingSegment>();

    void Awake()
    {
        BuildRing();
    }

    public void BuildRing()
    {
        foreach (var s in segments)
            if (s != null) Destroy(s.gameObject);
        segments.Clear();

        float segmentAngle = 360f / totalSegments;

        // Rotar el padre aleatoriamente
        float rotationY = randomRotation ? Random.Range(0f, 360f) : 0f;
        transform.localRotation = Quaternion.Euler(0f, rotationY, 0f);

        // Calcular qué índice cubre el ángulo de spawn en espacio LOCAL
        int protectedIndex = -1;
        if (protectSpawnAngle)
        {
            float localAngle = Mathf.Repeat(spawnAngleDeg - rotationY, 360f);
            protectedIndex = Mathf.FloorToInt(localAngle / segmentAngle) % totalSegments;
        }

        // Candidatos para empty/danger = todos excepto el protegido
        List<int> candidates = new List<int>();
        for (int i = 0; i < totalSegments; i++)
            if (i != protectedIndex) candidates.Add(i);

        Shuffle(candidates);

        int maxEmpty  = Mathf.Min(emptySegments,  candidates.Count);
        int maxDanger = Mathf.Min(dangerSegments, candidates.Count - maxEmpty);

        HashSet<int> emptySet  = new HashSet<int>(candidates.GetRange(0, maxEmpty));
        HashSet<int> dangerSet = new HashSet<int>(candidates.GetRange(maxEmpty, maxDanger));

        for (int i = 0; i < totalSegments; i++)
        {
            RingSegment.SegmentType type;

            if      (i == protectedIndex)   type = RingSegment.SegmentType.Safe;  // siempre seguro
            else if (emptySet.Contains(i))  type = RingSegment.SegmentType.Empty;
            else if (dangerSet.Contains(i)) type = RingSegment.SegmentType.Danger;
            else                            type = RingSegment.SegmentType.Safe;

            if (type == RingSegment.SegmentType.Empty) continue;

            float startDeg = i * segmentAngle;
            float endDeg   = startDeg + segmentAngle;

            GameObject go = new GameObject($"Seg_{i}_{type}");
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>();
            go.AddComponent<MeshRenderer>();
            go.AddComponent<MeshCollider>();
            RingSegment seg = go.AddComponent<RingSegment>();

            seg.innerRadius = innerRadius;
            seg.outerRadius = outerRadius;
            seg.thickness   = thickness;
            seg.Setup(startDeg, endDeg, type);

            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            if (type == RingSegment.SegmentType.Safe   && safeMaterial   != null) mr.material = safeMaterial;
            if (type == RingSegment.SegmentType.Danger && dangerMaterial != null) mr.material = dangerMaterial;

            segments.Add(seg);
        }

        // Crear trigger invisible debajo del anillo
        CreateTrigger();
    }

    void CreateTrigger()
    {
        // Destruir trigger anterior si existe
        Transform old = transform.Find("RingTrigger");
        if (old != null) Destroy(old.gameObject);

        GameObject triggerGO = new GameObject("RingTrigger");
        triggerGO.transform.SetParent(transform, false);
        RingTrigger rt = triggerGO.AddComponent<RingTrigger>();
        rt.Setup(this, transform.position.y);
    }

    void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Rebuild Ring")]
    void RebuildEditor()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);
        segments.Clear();
        BuildRing();
        UnityEditor.EditorUtility.SetDirty(gameObject);
    }
#endif
}