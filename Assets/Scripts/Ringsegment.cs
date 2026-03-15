using UnityEngine;

// Una sola "rebanada" del anillo helicoidal
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class RingSegment : MonoBehaviour
{
    [Header("Dimensiones")]
    public float innerRadius = 0.6f;
    public float outerRadius = 2.0f;
    public float thickness   = 0.6f;

    [Header("Arco")]
    public float startAngleDeg = 0f;    // Ángulo donde empieza esta rebanada
    public float endAngleDeg   = 30f;   // Ángulo donde termina (360/12 = 30° por pieza)

    [Header("Tipo")]
    public SegmentType segmentType = SegmentType.Safe;

    public enum SegmentType
    {
        Safe,       // Plataforma normal - la pelota rebota
        Danger,     // Plataforma peligrosa - game over al tocar
        Empty       // No existe - la pelota cae
    }

    void Awake()
    {
        ApplyType();
        if (segmentType != SegmentType.Empty)
            GenerateMesh();
        else
            gameObject.SetActive(false); // Las vacías simplemente no existen
    }

    public void Setup(float startDeg, float endDeg, SegmentType type)
    {
        startAngleDeg = startDeg;
        endAngleDeg   = endDeg;
        segmentType   = type;
        ApplyType();
        if (segmentType != SegmentType.Empty)
            GenerateMesh();
        else
            gameObject.SetActive(false);
    }

    void ApplyType()
    {
        // El material/color se asigna desde el RingBuilder según el tipo
        // Aquí solo marcamos el tag para que BallController lo detecte
        switch (segmentType)
        {
            case SegmentType.Safe:   gameObject.tag = "Safe";   break;
            case SegmentType.Danger: gameObject.tag = "Danger"; break;
        }
    }

    void GenerateMesh()
    {
        float startRad = startAngleDeg * Mathf.Deg2Rad;
        float endRad   = endAngleDeg   * Mathf.Deg2Rad;
        float arcAngle = endRad - startRad;

        int segments    = Mathf.Max(2, Mathf.RoundToInt(20f * (arcAngle / (2f * Mathf.PI))));
        int vertsPerRing = segments + 1;

        Vector3[] verts = new Vector3[vertsPerRing * 4];
        Vector2[] uvs   = new Vector2[vertsPerRing * 4];
        float halfH = thickness / 2f;

        for (int i = 0; i <= segments; i++)
        {
            float t     = (float)i / segments;
            float angle = startRad + t * arcAngle;
            float cos   = Mathf.Cos(angle);
            float sin   = Mathf.Sin(angle);

            verts[i]                    = new Vector3(cos * outerRadius,  halfH, sin * outerRadius);
            verts[vertsPerRing + i]     = new Vector3(cos * innerRadius,  halfH, sin * innerRadius);
            verts[2 * vertsPerRing + i] = new Vector3(cos * outerRadius, -halfH, sin * outerRadius);
            verts[3 * vertsPerRing + i] = new Vector3(cos * innerRadius, -halfH, sin * innerRadius);

            uvs[i]                    = new Vector2(t, 1);
            uvs[vertsPerRing + i]     = new Vector2(t, 0);
            uvs[2 * vertsPerRing + i] = new Vector2(t, 1);
            uvs[3 * vertsPerRing + i] = new Vector2(t, 0);
        }

        int triCount = segments * 4 * 6 + 2 * 2 * 3;
        int[] tris = new int[triCount];
        int ti = 0;

        for (int i = 0; i < segments; i++)
        {
            int so = i;
            int si = vertsPerRing + i;
            int io = 2 * vertsPerRing + i;
            int ii = 3 * vertsPerRing + i;

            // Top
            tris[ti++] = so;     tris[ti++] = si + 1; tris[ti++] = so + 1;
            tris[ti++] = so;     tris[ti++] = si;     tris[ti++] = si + 1;
            // Bottom
            tris[ti++] = io;     tris[ti++] = io + 1; tris[ti++] = ii + 1;
            tris[ti++] = io;     tris[ti++] = ii + 1; tris[ti++] = ii;
            // Outer
            tris[ti++] = so;     tris[ti++] = so + 1; tris[ti++] = io + 1;
            tris[ti++] = so;     tris[ti++] = io + 1; tris[ti++] = io;
            // Inner
            tris[ti++] = si;     tris[ti++] = ii + 1; tris[ti++] = si + 1;
            tris[ti++] = si;     tris[ti++] = ii;     tris[ti++] = ii + 1;
        }

        // Tapas laterales (los dos cortes del segmento)
        int s_so = 0;                        int e_so = segments;
        int s_si = vertsPerRing;             int e_si = vertsPerRing + segments;
        int s_io = 2 * vertsPerRing;         int e_io = 2 * vertsPerRing + segments;
        int s_ii = 3 * vertsPerRing;         int e_ii = 3 * vertsPerRing + segments;

        tris[ti++] = s_so; tris[ti++] = s_io; tris[ti++] = s_ii;
        tris[ti++] = s_so; tris[ti++] = s_ii; tris[ti++] = s_si;

        tris[ti++] = e_so; tris[ti++] = e_ii; tris[ti++] = e_io;
        tris[ti++] = e_so; tris[ti++] = e_si; tris[ti++] = e_ii;

        Mesh mesh = new Mesh();
        mesh.name      = "RingSegment";
        mesh.vertices  = verts;
        mesh.triangles = tris;
        mesh.uv        = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GetComponent<MeshFilter>().mesh        = mesh;
        GetComponent<MeshCollider>().sharedMesh = mesh;
    }

#if UNITY_EDITOR
    [ContextMenu("Regenerate Mesh")]
    void RegenerateMeshEditor() { GenerateMesh(); }
#endif
}