using UnityEngine;
using System.Collections;

public class BallController : MonoBehaviour
{
    [Header("Física")]
    public float gravityScale  = 2.0f;
    public float maxFallSpeed  = 15f;
    public float bounceForce   = 5f;

    [Header("Anti traspaso")]
    public LayerMask platformMask = ~0;
    public float collisionSkin = 0.02f;

    [Header("Polish - Splash")]
    public bool enableBounceSplash = true;
    public GameObject splashPrefab;
    public float splashPrefabScale = 1f;
    public float splashPrefabYOffset = 0.02f;
    public bool alignSplashToNormal = true;

    [Header("Muerte - Dissolve")]
    public bool enableDissolveOnDeath = true;
    public float dissolveDuration = 0.65f;
    public Material dissolveMaterialTemplate;

    private Rigidbody rb;
    private SphereCollider sphereCollider;
    private Renderer cachedRenderer;
    private Material[] originalMaterials;
    private Material dissolveMaterialInstance;
    private Coroutine deathRoutine;
    private bool warnedMissingSplashPrefab;
    private bool      isDead = false;

    const string DissolveProperty = "_DissolveAmount";

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        sphereCollider = GetComponent<SphereCollider>();
        cachedRenderer = GetComponentInChildren<Renderer>();

        if (cachedRenderer != null)
            originalMaterials = cachedRenderer.materials;

        rb.useGravity  = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation |
                         RigidbodyConstraints.FreezePositionX |
                         RigidbodyConstraints.FreezePositionZ;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void FixedUpdate()
    {
        if (isDead) return;

        Vector3 vel = rb.linearVelocity;
        vel.y -= gravityScale * 9.8f * Time.fixedDeltaTime;
        vel.y  = Mathf.Max(vel.y, -maxFallSpeed);

        if (vel.y < 0f)
            PredictiveDownCollision(ref vel);

        rb.linearVelocity = vel;
    }

    void PredictiveDownCollision(ref Vector3 velocity)
    {
        if (sphereCollider == null) return;

        float dt = Time.fixedDeltaTime;
        float radius = sphereCollider.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
        float castDistance = Mathf.Abs(velocity.y) * dt + collisionSkin;

        if (castDistance <= 0f) return;

        if (!Physics.SphereCast(rb.position, Mathf.Max(0.01f, radius * 0.95f), Vector3.down,
            out RaycastHit hit, castDistance, platformMask, QueryTriggerInteraction.Ignore))
            return;

        if (hit.collider == null || hit.collider.transform == transform)
            return;

        if (hit.collider.CompareTag("Danger"))
        {
            Die();
            return;
        }

        if (hit.collider.CompareTag("Safe"))
        {
            Vector3 pos = rb.position;
            pos.y = hit.point.y + radius + collisionSkin;
            rb.position = pos;
            velocity.y = bounceForce;
            EmitBounceSplash(hit.point, hit.normal);
        }
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

            if (collision.contactCount > 0)
            {
                ContactPoint contact = collision.GetContact(0);
                EmitBounceSplash(contact.point, contact.normal);
            }
        }
    }

    void Die()
    {
        if (isDead) return;

        isDead            = true;
        rb.linearVelocity = Vector3.zero;
        rb.useGravity     = false;

        if (enableDissolveOnDeath)
        {
            if (deathRoutine != null) StopCoroutine(deathRoutine);
            deathRoutine = StartCoroutine(PlayDeathDissolve());
        }
    }

    IEnumerator PlayDeathDissolve()
    {
        if (cachedRenderer == null)
            yield break;

        if (!TrySetupDissolveMaterial())
            yield break;

        float elapsed = 0f;
        while (elapsed < dissolveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / dissolveDuration);
            dissolveMaterialInstance.SetFloat(DissolveProperty, t);
            yield return null;
        }

        cachedRenderer.enabled = false;
    }

    bool TrySetupDissolveMaterial()
    {
        if (cachedRenderer == null)
            return false;

        if (dissolveMaterialInstance != null)
            Destroy(dissolveMaterialInstance);

        Material source = null;
        if (dissolveMaterialTemplate != null)
            source = new Material(dissolveMaterialTemplate);

        if (source == null)
        {
            Shader dissolveShader = Shader.Find("Custom/BallDissolveURP");
            if (dissolveShader == null)
            {
                Debug.LogWarning("No se encontró el shader Custom/BallDissolveURP para el efecto de muerte.");
                return false;
            }

            source = new Material(dissolveShader);
            if (originalMaterials != null && originalMaterials.Length > 0 && originalMaterials[0] != null)
            {
                Material baseMat = originalMaterials[0];

                if (baseMat.HasProperty("_BaseMap") && source.HasProperty("_BaseMap"))
                    source.SetTexture("_BaseMap", baseMat.GetTexture("_BaseMap"));
                else if (baseMat.HasProperty("_MainTex") && source.HasProperty("_BaseMap"))
                    source.SetTexture("_BaseMap", baseMat.GetTexture("_MainTex"));

                if (baseMat.HasProperty("_BaseColor") && source.HasProperty("_BaseColor"))
                    source.SetColor("_BaseColor", baseMat.GetColor("_BaseColor"));
                else if (baseMat.HasProperty("_Color") && source.HasProperty("_BaseColor"))
                    source.SetColor("_BaseColor", baseMat.GetColor("_Color"));
            }
        }

        source.SetFloat(DissolveProperty, 0f);
        dissolveMaterialInstance = source;

        int materialCount = (originalMaterials != null && originalMaterials.Length > 0) ? originalMaterials.Length : 1;
        Material[] mats = new Material[materialCount];
        for (int i = 0; i < materialCount; i++)
            mats[i] = dissolveMaterialInstance;

        cachedRenderer.enabled = true;
        cachedRenderer.materials = mats;
        return true;
    }

    void EmitBounceSplash(Vector3 position, Vector3 normal)
    {
        if (!enableBounceSplash) return;

        if (!TrySpawnSplashPrefab(position, normal))
        {
            if (!warnedMissingSplashPrefab)
            {
                Debug.LogWarning("BallController: splashPrefab no está asignado. Asigna tu prefab de agua en el Inspector.");
                warnedMissingSplashPrefab = true;
            }
        }
    }

    bool TrySpawnSplashPrefab(Vector3 position, Vector3 normal)
    {
        if (splashPrefab == null)
            return false;

        Quaternion rotation = alignSplashToNormal
            ? Quaternion.LookRotation(normal)
            : Quaternion.identity;

        Vector3 spawnPosition = position + normal * splashPrefabYOffset;
        GameObject instance = Instantiate(splashPrefab, spawnPosition, rotation);
        instance.transform.SetParent(null, true);
        instance.transform.localScale *= Mathf.Max(0.01f, splashPrefabScale);

        ParticleSystem[] systems = instance.GetComponentsInChildren<ParticleSystem>(true);
        float maxLifetime = 2f;

        if (systems.Length > 0)
        {
            maxLifetime = 0f;
            for (int i = 0; i < systems.Length; i++)
            {
                ParticleSystem ps = systems[i];
                var main = ps.main;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.loop = false;
                float life = main.duration + main.startLifetime.constantMax;
                if (life > maxLifetime) maxLifetime = life;
            }
        }

        Destroy(instance, maxLifetime + 0.5f);
        return true;
    }

    public void ResetBall(Vector3 startPosition)
    {
        if (deathRoutine != null)
        {
            StopCoroutine(deathRoutine);
            deathRoutine = null;
        }

        isDead             = false;
        transform.position = startPosition;
        rb.position        = startPosition;
        rb.linearVelocity  = Vector3.zero;

        if (cachedRenderer != null)
        {
            cachedRenderer.enabled = true;
            if (originalMaterials != null && originalMaterials.Length > 0)
                cachedRenderer.materials = originalMaterials;
        }

        if (dissolveMaterialInstance != null)
        {
            Destroy(dissolveMaterialInstance);
            dissolveMaterialInstance = null;
        }
    }
}