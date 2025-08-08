using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    [SerializeField] internal int HP = 100;
    private Animator animator;
    private NavMeshAgent navAgent;
    internal bool isDead;

    private Rigidbody[] ragdollRigidbodies;
    private Collider[] ragdollColliders;
    private Rigidbody mainRigidbody;

    public float headshotForce = 50f;
    public float bodyForce = 30f;
    public float torqueAmount = 10f;

    public bool enableKillSlowMo = true;
    public float slowMoChance = 0.3f;
    public float headshotSlowMoChance = 0.6f;
    public float slowMoTimeScale = 0.3f;
    public float slowMoDuration = 0.4f;

    private static int recentKills = 0;
    private static float lastKillTime = 0;

    public Material particleMaterial;

    private void Start()
    {
        animator = GetComponent<Animator>();
        navAgent = GetComponent<NavMeshAgent>();
        SetupRagdoll();
    }

    private void SetupRagdoll()
    {
        ragdollRigidbodies = GetComponentsInChildren<Rigidbody>();
        ragdollColliders = GetComponentsInChildren<Collider>();

        mainRigidbody = GetComponent<Rigidbody>();
        if (mainRigidbody == null)
        {
            mainRigidbody = gameObject.AddComponent<Rigidbody>();
            mainRigidbody.mass = 1f;
            mainRigidbody.isKinematic = true;
        }

        SetRagdollState(false);
    }

    private void SetRagdollState(bool enabled)
    {
        Collider mainCollider = GetComponent<Collider>();

        foreach (Rigidbody rb in ragdollRigidbodies)
        {
            if (rb == mainRigidbody) continue;
            rb.isKinematic = !enabled;
        }

        foreach (Collider col in ragdollColliders)
        {
            if (col == mainCollider) continue;
            col.enabled = enabled;
        }

        if (animator != null)
            animator.enabled = !enabled;

        if (mainCollider != null)
            mainCollider.enabled = !enabled;
    }

    public virtual void TakeDamage(int damageAmount, bool isHeadshot, Vector3 hitPoint = default, Vector3 hitDirection = default)
    {
        if (isDead)
        {
            if (hitPoint != default)
            {
                ApplyForceToDeadBody(hitPoint, hitDirection, damageAmount);
            }
            return;
        }

        HP -= damageAmount;

        if (DamagePopup.Instance != null)
        {
            float zombieHeight = 2f;
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                zombieHeight = col.bounds.size.y;
            }

            DamagePopup.Instance.Create(
                transform.position,
                damageAmount,
                isHeadshot,
                zombieHeight + 0.5f
            );
        }

        if (HP <= 0)
        {
            if (!isDead)
            {
                isDead = true;
                TrackKill();
                GlobalReferences.Instance.IncrementZombiesKilled();

                ZombieSpawnController spawnController = FindObjectOfType<ZombieSpawnController>();
                if (spawnController != null)
                {
                    spawnController.OnEnemyDeath(gameObject);
                }

                if (navAgent != null)
                    navAgent.enabled = false;

                ApplyRagdollDeath(hitPoint, hitDirection, isHeadshot);
                SoundManager.Instance.zombieChannel1.PlayOneShot(SoundManager.Instance.zombieDeath);

                if (enableKillSlowMo)
                {
                    TriggerKillSlowMo(isHeadshot);
                }

                StartCoroutine(DespawnAfterDelay(5f));
            }
        }
        else
        {
            animator.SetTrigger("DAMAGE");
            SoundManager.Instance.zombieChannel1.PlayOneShot(SoundManager.Instance.zombieHurt);

            if (hitDirection != default && navAgent != null)
            {
                StartCoroutine(HitStagger(hitDirection));
            }
        }
    }

    private void TrackKill()
    {
        if (Time.time - lastKillTime < 2f)
        {
            recentKills++;
        }
        else
        {
            recentKills = 1;
        }
        lastKillTime = Time.time;
    }

    private void TriggerKillSlowMo(bool isHeadshot)
    {
        float chance = isHeadshot ? headshotSlowMoChance : slowMoChance;

        if (recentKills > 2)
        {
            chance = Mathf.Min(1f, chance + (recentKills * 0.1f));
        }

        if (Random.value < chance)
        {
            StartCoroutine(KillSlowMotion(isHeadshot));
        }
    }

    private IEnumerator KillSlowMotion(bool isHeadshot)
    {
        if (Time.timeScale < 0.9f) yield break;

        float duration = isHeadshot ? slowMoDuration * 1.5f : slowMoDuration;

        Time.timeScale = slowMoTimeScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        if (CameraShaker.Instance != null && isHeadshot)
        {
            CameraShaker.Instance.ShakeLight();
        }

        yield return new WaitForSecondsRealtime(duration);

        float elapsed = 0;
        float returnDuration = 0.1f;
        float startScale = Time.timeScale;

        while (elapsed < returnDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            Time.timeScale = Mathf.Lerp(startScale, 1f, elapsed / returnDuration);
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            yield return null;
        }

        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
    }

    private void ApplyForceToDeadBody(Vector3 hitPoint, Vector3 hitDirection, int damage)
    {
        Rigidbody closestRb = null;
        float closestDistance = float.MaxValue;

        foreach (Rigidbody rb in ragdollRigidbodies)
        {
            if (rb == mainRigidbody) continue;

            float dist = Vector3.Distance(rb.position, hitPoint);
            if (dist < closestDistance)
            {
                closestDistance = dist;
                closestRb = rb;
            }
        }

        if (closestRb != null && closestDistance < 0.5f)
        {
            float force = Mathf.Min(damage * 0.05f, 2f);
            closestRb.isKinematic = false;
            closestRb.AddForceAtPosition(hitDirection * force, hitPoint, ForceMode.Impulse);
        }
    }

    private void ApplyRagdollDeath(Vector3 hitPoint, Vector3 hitDirection, bool isHeadshot)
    {
        SetRagdollState(true);

        if (hitDirection == default)
        {
            if (Camera.main != null)
            {
                hitDirection = transform.position - Camera.main.transform.position;
                hitDirection.y = 0;
                hitDirection.Normalize();
            }
            else
            {
                hitDirection = -transform.forward;
            }
        }

        if (hitPoint == default)
        {
            hitPoint = transform.position + Vector3.up * 1f;
        }

        Rigidbody closestRb = null;
        float closestDistance = float.MaxValue;

        foreach (Rigidbody rb in ragdollRigidbodies)
        {
            if (rb == mainRigidbody) continue;

            float dist = Vector3.Distance(rb.position, hitPoint);
            if (dist < closestDistance)
            {
                closestDistance = dist;
                closestRb = rb;
            }
        }

        float forceMultiplier = (isHeadshot ? headshotForce : bodyForce) * 0.1f;

        if (closestRb != null)
        {
            closestRb.AddForce(hitDirection * forceMultiplier, ForceMode.Impulse);

            if (isHeadshot)
            {
                foreach (Rigidbody rb in ragdollRigidbodies)
                {
                    if (rb.name.ToLower().Contains("head"))
                    {
                        rb.AddForce(hitDirection * forceMultiplier * 1.2f, ForceMode.Impulse);
                        rb.AddTorque(Random.insideUnitSphere * torqueAmount * 0.1f, ForceMode.Impulse);
                        break;
                    }
                }
            }
        }

        foreach (Rigidbody rb in ragdollRigidbodies)
        {
            if (rb == mainRigidbody) continue;

            float distance = Vector3.Distance(rb.position, hitPoint);
            float falloff = Mathf.Clamp01(1f - (distance / 3f));

            rb.AddForce(hitDirection * forceMultiplier * falloff * 0.1f, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * torqueAmount * 0.02f * falloff, ForceMode.Impulse);
        }
    }

    public void TakeDamage(int damageAmount)
    {
        Vector3 hitDir = Vector3.zero;
        Vector3 hitPoint = transform.position + Vector3.up;

        if (Camera.main != null)
        {
            hitDir = transform.position - Camera.main.transform.position;
            hitDir.y = 0;
            hitDir.Normalize();
        }

        TakeDamage(damageAmount, false, hitPoint, hitDir);
    }

    public void TakeDamage(int damageAmount, bool isHeadshot)
    {
        Vector3 hitDir = Vector3.zero;
        Vector3 hitPoint = transform.position + Vector3.up * (isHeadshot ? 1.8f : 1f);

        if (Camera.main != null)
        {
            hitDir = transform.position - Camera.main.transform.position;
            hitDir.y = 0;
            hitDir.Normalize();
        }

        TakeDamage(damageAmount, isHeadshot, hitPoint, hitDir);
    }

    private IEnumerator HitStagger(Vector3 force)
    {
        navAgent.velocity = force * 2f;
        yield return new WaitForSeconds(0.2f);
    }

    private void DetermineDeathAnimation(Vector3 hitPoint)
    {
    }

    private IEnumerator DespawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        yield return StartCoroutine(Disintegrate());
        Destroy(gameObject);
    }

    private IEnumerator Disintegrate()
    {
        Collider[] allColliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in allColliders)
        {
            col.enabled = false;
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        CreateDisintegrationParticles();

        float dissolveTime = 2f;
        float elapsed = 0f;

        Material[] dissolveMaterials = new Material[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && renderers[i].material != null)
            {
                dissolveMaterials[i] = new Material(renderers[i].material);
                renderers[i].material = dissolveMaterials[i];
            }
        }

        while (elapsed < dissolveTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dissolveTime;

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && dissolveMaterials[i] != null)
                {
                    Color color = dissolveMaterials[i].color;
                    color.a = 1f - t;
                    dissolveMaterials[i].color = color;

                    if (dissolveMaterials[i].HasProperty("_Color"))
                    {
                        dissolveMaterials[i].SetColor("_Color", color);
                    }

                    float noise = Mathf.PerlinNoise(Time.time * 3f, i * 0.1f);
                    if (noise * (1f - t) < 0.3f)
                    {
                        renderers[i].enabled = false;
                    }
                }
            }

            yield return null;
        }
    }

    private void CreateDisintegrationParticles()
    {
        Vector3 particlePos = Vector3.zero;
        int count = 0;

        foreach (Rigidbody rb in ragdollRigidbodies)
        {
            if (rb == mainRigidbody) continue;
            particlePos += rb.position;
            count++;
        }

        if (count > 0)
            particlePos /= count;
        else
            particlePos = transform.position + Vector3.up * 0.5f;

        GameObject particleObj = new GameObject("DisintegrationParticles");
        particleObj.transform.position = particlePos;
        particleObj.transform.rotation = Quaternion.identity;

        ParticleSystem particles = particleObj.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psRenderer = particleObj.GetComponent<ParticleSystemRenderer>();

        if (particleMaterial != null)
        {
            psRenderer.material = particleMaterial;
        }
        else
        {
            Material fallbackMaterial = new Material(Shader.Find("Standard"));
            fallbackMaterial.color = new Color(0.4f, 0.3f, 0.2f);
            psRenderer.material = fallbackMaterial;
        }

        var main = particles.main;
        main.playOnAwake = false;
        main.duration = 0.1f;
        main.startLifetime = 4f;
        main.startSpeed = 0.5f;
        main.maxParticles = 15000;
        main.startSize = 0.002f;
        main.startColor = new Color(0.4f, 0.3f, 0.2f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.loop = false;

        var emission = particles.emission;
        emission.enabled = true;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0.0f, 1200)
        });

        var shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.7f;

        Mesh bodyMesh = GetBodyMesh();
        if (bodyMesh != null)
        {
            shape.shapeType = ParticleSystemShapeType.Mesh;
            shape.mesh = bodyMesh;
            shape.scale = Vector3.one * 0.8f;
        }

        var velocityOverLifetime = particles.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
        velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f);
        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(1.0f, 2.0f);
        velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f);

        var sizeOverLifetime = particles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(0.5f, new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.3f, 0.5f),
            new Keyframe(1f, 0.1f)
        ));

        var colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.5f, 0.4f, 0.3f), 0.0f),
                new GradientColorKey(new Color(0.4f, 0.3f, 0.2f), 0.5f),
                new GradientColorKey(new Color(0.3f, 0.2f, 0.1f), 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1.0f, 0.0f),
                new GradientAlphaKey(0.5f, 0.7f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        colorOverLifetime.color = gradient;

        var noise = particles.noise;
        noise.enabled = true;
        noise.strength = 0.2f;
        noise.frequency = 1.0f;
        noise.scrollSpeed = 0.3f;
        noise.quality = ParticleSystemNoiseQuality.High;

        var rotationOverLifetime = particles.rotationOverLifetime;
        rotationOverLifetime.enabled = true;
        rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(10f, new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(1f, 360f)
        ));

        psRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        psRenderer.alignment = ParticleSystemRenderSpace.World;
        psRenderer.minParticleSize = 0.001f;
        psRenderer.maxParticleSize = 0.005f;

        particles.Play();
        Destroy(particleObj, 5f); 
    }

    private Mesh GetBodyMesh()
    {
        SkinnedMeshRenderer skinnedRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        if (skinnedRenderer != null)
        {
            Mesh mesh = new Mesh();
            skinnedRenderer.BakeMesh(mesh);
            return mesh;
        }

        MeshFilter meshFilter = GetComponentInChildren<MeshFilter>();
        if (meshFilter != null)
        {
            return meshFilter.mesh;
        }

        return null;
    }
}