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

    [Header("Ragdoll Forces")]
    public float headshotForce = 50f;
    public float bodyForce = 30f;
    public float torqueAmount = 10f;

    [Header("Slow Motion")]
    public bool enableKillSlowMo = true;
    public float slowMoChance = 0.3f;
    public float headshotSlowMoChance = 0.6f;
    public float slowMoTimeScale = 0.3f;
    public float slowMoDuration = 0.4f;

    [Header("Disintegration")]
    public Material particleMaterial;
    public Color particleStartColor = new Color(0.8f, 0.4f, 0.2f);
    public Color particleEndColor = new Color(0.3f, 0.15f, 0.05f);
    public float disintegrationDelay = 3f;
    public float disintegrationDuration = 2.5f;

    private static int recentKills = 0;
    private static float lastKillTime = 0;

    
    private AudioSource[] audioSources;
    private bool hasStoppedAudio = false;

    private void Start()
    {
        animator = GetComponent<Animator>();
        navAgent = GetComponent<NavMeshAgent>();
        audioSources = GetComponents<AudioSource>();
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

                
                if (KillStreakManager.Instance != null)
                {
                    KillStreakManager.Instance.RegisterKill(transform.position, isHeadshot);
                }

                ZombieSpawnController spawnController = FindObjectOfType<ZombieSpawnController>();
                if (spawnController != null)
                {
                    spawnController.OnEnemyDeath(gameObject);
                }

                if (navAgent != null)
                    navAgent.enabled = false;

                ApplyRagdollDeath(hitPoint, hitDirection, isHeadshot);

                
                if (SoundManager.Instance != null && SoundManager.Instance.zombieChannel1 != null)
                {
                    SoundManager.Instance.zombieChannel1.PlayOneShot(SoundManager.Instance.zombieDeath);
                }

                
                StopAllAudio();

                if (enableKillSlowMo)
                {
                    TriggerKillSlowMo(isHeadshot);
                }

                StartCoroutine(DespawnAfterDelay(disintegrationDelay));
            }
        }
        else
        {
            
            if (!isDead)
            {
                animator.SetTrigger("DAMAGE");
                if (SoundManager.Instance != null && SoundManager.Instance.zombieChannel1 != null)
                {
                    SoundManager.Instance.zombieChannel1.PlayOneShot(SoundManager.Instance.zombieHurt);
                }

                if (hitDirection != default && navAgent != null)
                {
                    StartCoroutine(HitStagger(hitDirection));
                }
            }
        }
    }   

    private void StopAllAudio()
    {
        if (hasStoppedAudio) return;
        hasStoppedAudio = true;

        
        if (audioSources != null)
        {
            foreach (AudioSource source in audioSources)
            {
                if (source != null)
                {
                    source.Stop();
                    source.enabled = false;
                }
            }
        }

        
        AudioSource[] childAudioSources = GetComponentsInChildren<AudioSource>();
        foreach (AudioSource source in childAudioSources)
        {
            if (source != null)
            {
                source.Stop();
                source.enabled = false;
            }
        }

        
        if (animator != null)
        {
            animator.enabled = false;
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

    private IEnumerator DespawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        
        StopAllAudio();

        yield return StartCoroutine(Disintegrate());
        Destroy(gameObject);
    }

    private IEnumerator Disintegrate()
    {
        
        StopAllAudio();

        
        Collider[] allColliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in allColliders)
        {
            if (col != null)
                col.enabled = false;
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        
        GameObject particleSystem = CreateDisintegrationEffect();

        float elapsed = 0f;

        
        Material[] dissolveMaterials = new Material[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && renderers[i].material != null)
            {
                dissolveMaterials[i] = new Material(renderers[i].material);
                renderers[i].material = dissolveMaterials[i];

                
                if (dissolveMaterials[i].HasProperty("_Mode"))
                {
                    dissolveMaterials[i].SetInt("_Mode", 3); 
                    dissolveMaterials[i].SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    dissolveMaterials[i].SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    dissolveMaterials[i].SetInt("_ZWrite", 0);
                    dissolveMaterials[i].DisableKeyword("_ALPHATEST_ON");
                    dissolveMaterials[i].EnableKeyword("_ALPHABLEND_ON");
                    dissolveMaterials[i].DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    dissolveMaterials[i].renderQueue = 3000;
                }
            }
        }

        
        while (elapsed < disintegrationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / disintegrationDuration;

            
            float dissolveAmount = Mathf.SmoothStep(0f, 1f, t);

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && dissolveMaterials[i] != null)
                {
                    
                    Color color = dissolveMaterials[i].color;
                    color.a = 1f - dissolveAmount;
                    dissolveMaterials[i].color = color;

                    if (dissolveMaterials[i].HasProperty("_Color"))
                    {
                        dissolveMaterials[i].SetColor("_Color", color);
                    }
                }
            }

            yield return null;
        }

        
        foreach (Renderer r in renderers)
        {
            if (r != null)
                r.enabled = false;
        }
    }

    private GameObject CreateDisintegrationEffect()
    {
        
        Bounds zombieBounds = new Bounds(transform.position, Vector3.one);
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            zombieBounds = renderers[0].bounds;
            foreach (Renderer r in renderers)
            {
                if (r != null && r.enabled)
                    zombieBounds.Encapsulate(r.bounds);
            }
        }

        
        GameObject particleObj = new GameObject("ZombieDisintegration");
        particleObj.transform.position = zombieBounds.center;
        particleObj.transform.rotation = transform.rotation;

        ParticleSystem particles = particleObj.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psRenderer = particleObj.GetComponent<ParticleSystemRenderer>();

        
        if (particleMaterial != null)
        {
            psRenderer.material = new Material(particleMaterial);
        }
        else
        {
            Material particleMat = new Material(Shader.Find("Sprites/Default"));
            particleMat.color = particleStartColor;
            psRenderer.material = particleMat;
        }

        
        var main = particles.main;
        main.duration = disintegrationDuration;
        main.loop = false;
        main.prewarm = false;
        main.startLifetime = 4f;
        main.startSpeed = 0.5f;
        main.maxParticles = 8000;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
        main.gravityModifier = -0.3f; 
        main.startColor = particleStartColor;

        
        var emission = particles.emission;
        emission.enabled = true;
        emission.rateOverTime = 3000f / disintegrationDuration; 

        
        var shape = particles.shape;
        shape.enabled = true;

        
        SkinnedMeshRenderer skinnedMesh = GetComponentInChildren<SkinnedMeshRenderer>();
        if (skinnedMesh != null && skinnedMesh.sharedMesh != null)
        {
            shape.shapeType = ParticleSystemShapeType.SkinnedMeshRenderer;
            shape.skinnedMeshRenderer = skinnedMesh;
            shape.meshShapeType = ParticleSystemMeshShapeType.Triangle;
            shape.normalOffset = 0.01f;
        }
        else
        {
            MeshFilter meshFilter = GetComponentInChildren<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                shape.shapeType = ParticleSystemShapeType.Mesh;
                shape.mesh = meshFilter.sharedMesh;
                shape.meshShapeType = ParticleSystemMeshShapeType.Triangle;
            }
            else
            {
                
                shape.shapeType = ParticleSystemShapeType.Box;
                shape.scale = zombieBounds.size;
            }
        }

        
        var velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.5f, 1.5f); 
        velocity.z = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f);

        
        var sizeOverLifetime = particles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0.1f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        
        var colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(particleStartColor, 0f),
                new GradientColorKey(particleEndColor, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.8f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;

        
        var noise = particles.noise;
        noise.enabled = true;
        noise.strength = 0.1f;
        noise.frequency = 0.5f;
        noise.damping = true;

        
        psRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        psRenderer.alignment = ParticleSystemRenderSpace.View;
        psRenderer.sortMode = ParticleSystemSortMode.Distance;

        
        particles.Play();

        
        Destroy(particleObj, disintegrationDuration + 5f);

        return particleObj;
    }

    private void OnDestroy()
    {
        
        StopAllAudio();
    }
}