using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public interface IDamageable
{
    void TakeDamage(int damage);
}

public enum ZombieMutation
{
    Normal,
    Runner,
    Tank,
    Screamer,
    Exploder
}

public class ZombieSpawnController : MonoBehaviour, IDamageable
{
    [Header("Wave Settings")]
    public float baseSpawnInterval = 10f;
    public float minSpawnInterval = 1f;
    public float difficultyRampSpeed = 0.7f;
    public int maxZombiesPerWave = 40;

    [Header("Spawner Health")]
    public int spawnerHealth = 5;
    public GameObject spawnerVisual;
    public ParticleSystem damageEffect;
    public ParticleSystem destructionEffect;

    [Header("Spawn Settings")]
    public float spawnRadius = 30f;
    public float minSpawnDistance = 15f;
    public float spawnHeightOffset = 0.5f;

    [Header("Mutation Settings")]
    [Range(0f, 1f)]
    public float baseMutationChance = 0.1f;
    public float mutationChancePerWave = 0.05f;
    public Color runnerColor = Color.green;
    public Color tankColor = Color.red;
    public Color screamerColor = Color.yellow;
    public Color exploderColor = Color.magenta;

    [Header("Mutation Prefabs")]
    public GameObject runnerPrefab;
    public GameObject tankPrefab;
    public GameObject screamerPrefab;
    public GameObject exploderPrefab;

    [Header("UI References")]
    public Slider waveProgressBar;
    public Image progressBarFill;
    public Color progressBarColor = Color.green;
    public TextMeshProUGUI currentWaveText;
    public Image currentWaveImage;
    public TextMeshProUGUI nextWaveText;
    public TextMeshProUGUI spawnerHealthText;
    public TextMeshProUGUI spawnRateText;
    public TextMeshProUGUI mutationText;

    [Header("Audio")]
    public AudioClip spawnerHitSound;
    public AudioClip spawnerDestroyedSound;
    public AudioClip mutationSound;

    
    private const float BASE_CHASE_SPEED = 1.5f;

    public int currentWave = 1;
    private float currentSpawnInterval;
    private bool spawnerDestroyed = false;
    private int currentSpawnerHealth;
    private List<GameObject> currentZombiesAlive = new List<GameObject>();
    private Coroutine spawnCoroutine;
    private Renderer spawnerRenderer;
    private Color originalColor;

    private int zombiesSpawnedThisWave = 0;
    private float waveStartTime;

    private bool isRespawning = false;
    private float respawnTimer = 15f;
    private int originalSpawnerHealth;

    private Bounds playAreaBounds;
    private bool hasBoundaries = false;

    private Dictionary<ZombieMutation, int> currentMutationCounts = new Dictionary<ZombieMutation, int>();

    private void Start()
    {
        InitializeSpawner();
        InitializeUI();
        InitializeMutationCounts();

        originalSpawnerHealth = spawnerHealth;
        currentSpawnerHealth = spawnerHealth;
        currentSpawnInterval = baseSpawnInterval;
        waveStartTime = Time.time;

        UpdateUI();
        StartInfiniteSpawning();
        InitializeBoundaries();
    }

    private void InitializeSpawner()
    {
        if (GetComponent<Collider>() == null)
        {
            var collider = gameObject.AddComponent<BoxCollider>();
            collider.size = Vector3.one * 2f;
        }

        if (spawnerVisual != null)
        {
            spawnerRenderer = spawnerVisual.GetComponent<Renderer>();
        }

        if (spawnerRenderer == null)
        {
            spawnerRenderer = GetComponent<Renderer>();
        }

        if (spawnerRenderer == null)
        {
            spawnerRenderer = GetComponent<MeshRenderer>();
        }

        if (spawnerRenderer != null && spawnerRenderer.material != null)
        {
            originalColor = spawnerRenderer.material.color;
        }

        try
        {
            if (gameObject.tag == "Untagged")
            {
                gameObject.tag = "Spawner";
            }
        }
        catch
        {
            Debug.LogWarning("Spawner tag not defined in project. Please create 'Spawner' tag in Tags and Layers.");
        }
    }

    private void InitializeBoundaries()
    {
        GameObject[] borders = GameObject.FindGameObjectsWithTag("Border");
        if (borders.Length == 0) return;

        
        playAreaBounds = new Bounds(transform.position, Vector3.zero);
        foreach (GameObject border in borders)
        {
            Collider col = border.GetComponent<Collider>();
            if (col != null)
            {
                playAreaBounds.Encapsulate(col.bounds);
            }
        }

        
        float shrinkAmount = 1.5f; 
        Vector3 min = playAreaBounds.min;
        Vector3 max = playAreaBounds.max;
        playAreaBounds.SetMinMax(
            new Vector3(min.x + shrinkAmount, min.y, min.z + shrinkAmount),
            new Vector3(max.x - shrinkAmount, max.y, max.z - shrinkAmount)
        );

        hasBoundaries = true;

        
        if (!playAreaBounds.Contains(transform.position))
        {
            transform.position = playAreaBounds.ClosestPoint(transform.position);
        }
    }

    private Vector3 ClampToBoundaries(Vector3 position)
    {
        if (!hasBoundaries) return position;

        return new Vector3(
            Mathf.Clamp(position.x, playAreaBounds.min.x, playAreaBounds.max.x),
            position.y,
            Mathf.Clamp(position.z, playAreaBounds.min.z, playAreaBounds.max.z)
        );
    }


    private void InitializeUI()
    {
        if (waveProgressBar != null)
        {
            waveProgressBar.gameObject.SetActive(true);
            waveProgressBar.minValue = 0f;
            waveProgressBar.maxValue = 1f;
            waveProgressBar.value = 0f;

            if (progressBarFill != null)
                progressBarFill.color = progressBarColor;
        }

        UpdateUI();
    }

    private void InitializeMutationCounts()
    {
        currentMutationCounts[ZombieMutation.Normal] = 0;
        currentMutationCounts[ZombieMutation.Runner] = 0;
        currentMutationCounts[ZombieMutation.Tank] = 0;
        currentMutationCounts[ZombieMutation.Screamer] = 0;
        currentMutationCounts[ZombieMutation.Exploder] = 0;
    }

    private void StartInfiniteSpawning()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
        }

        spawnCoroutine = StartCoroutine(InfiniteSpawnLoop());
    }

    private IEnumerator InfiniteSpawnLoop()
    {
        while (!spawnerDestroyed)
        {
            currentSpawnInterval = CalculateSpawnInterval(currentWave);

            if (ShouldAdvanceWave())
            {
                AdvanceToNextWave();
            }

            currentZombiesAlive.RemoveAll(zombie => zombie == null);

            if (currentZombiesAlive.Count < GetMaxZombiesForWave(currentWave))
            {
                SpawnMutatedZombie();
                zombiesSpawnedThisWave++;
            }

            UpdateUI();
            yield return new WaitForSeconds(currentSpawnInterval);
        }
    }

    private float CalculateSpawnInterval(int wave)
    {
        float interval = baseSpawnInterval * Mathf.Pow(difficultyRampSpeed, wave * 1.2f);
        return Mathf.Max(interval, minSpawnInterval);
    }

    private int GetMaxZombiesForWave(int wave)
    {
        int maxZombies = 5 + (wave - 1) * 4;
        return Mathf.Min(maxZombies, maxZombiesPerWave);
    }

    private bool ShouldAdvanceWave()
    {
        float waveTime = Time.time - waveStartTime;
        bool timeThreshold = waveTime > 60f;
        bool zombieThreshold = zombiesSpawnedThisWave >= GetMaxZombiesForWave(currentWave) * 2;

        return timeThreshold || zombieThreshold;
    }

    private void AdvanceToNextWave()
    {
        currentWave++;
        zombiesSpawnedThisWave = 0;
        waveStartTime = Time.time;

        if (GlobalReferences.Instance != null)
            GlobalReferences.Instance.waveNumber = currentWave;

        UpdateUI();
    }

    private void SpawnMutatedZombie()
    {
        if (GlobalReferences.Instance?.zombiePrefab == null || GlobalReferences.Instance?.player == null)
            return;

        Vector3 spawnPosition = GetValidSpawnPosition();
        ZombieMutation mutation = DetermineZombieMutation();

        GameObject prefabToUse = GetPrefabForMutation(mutation);
        if (prefabToUse == null)
        {
            prefabToUse = GlobalReferences.Instance.zombiePrefab;
        }

        GameObject zombie = Instantiate(prefabToUse, spawnPosition, Quaternion.identity);

        if (hasBoundaries)
        {
            BoundaryConstraint constraint = zombie.AddComponent<BoundaryConstraint>();
            constraint.Initialize(playAreaBounds);
        }


        ApplyMutation(zombie, mutation);

        currentZombiesAlive.Add(zombie);
        currentMutationCounts[mutation]++;

        if (mutation != ZombieMutation.Normal && mutationSound != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.zombieChannel1.PlayOneShot(mutationSound);
        }

        if (GlobalReferences.Instance.player != null)
        {
            Vector3 lookDirection = (GlobalReferences.Instance.player.transform.position - zombie.transform.position).normalized;
            if (lookDirection != Vector3.zero)
            {
                zombie.transform.rotation = Quaternion.LookRotation(lookDirection);
            }
        }


    }

    private ZombieMutation DetermineZombieMutation()
    {
        float mutationChance = GetCurrentMutationChance();

        if (Random.value > mutationChance)
            return ZombieMutation.Normal;

        float rand = Random.value;

        if (currentWave >= 5)
        {
            if (rand < 0.25f) return ZombieMutation.Runner;
            else if (rand < 0.45f) return ZombieMutation.Tank;
            else if (rand < 0.70f) return ZombieMutation.Screamer;
            else return ZombieMutation.Exploder;
        }
        else if (currentWave >= 4)
        {
            if (rand < 0.35f) return ZombieMutation.Runner;
            else if (rand < 0.65f) return ZombieMutation.Tank;
            else return ZombieMutation.Screamer;
        }
        else if (currentWave >= 3)
        {
            if (rand < 0.40f) return ZombieMutation.Runner;
            else if (rand < 0.80f) return ZombieMutation.Tank;
            else return ZombieMutation.Screamer;
        }
        else if (currentWave >= 2)
        {
            if (rand < 0.60f) return ZombieMutation.Runner;
            else return ZombieMutation.Tank;
        }
        else
        {
            return ZombieMutation.Runner;
        }
    }

    private float GetCurrentMutationChance()
    {
        return Mathf.Min(baseMutationChance + (currentWave - 1) * mutationChancePerWave, 0.8f);
    }

    private GameObject GetPrefabForMutation(ZombieMutation mutation)
    {
        switch (mutation)
        {
            case ZombieMutation.Runner:
                return runnerPrefab != null ? runnerPrefab : null;
            case ZombieMutation.Tank:
                return tankPrefab != null ? tankPrefab : null;
            case ZombieMutation.Screamer:
                return screamerPrefab != null ? screamerPrefab : null;
            case ZombieMutation.Exploder:
                return exploderPrefab != null ? exploderPrefab : null;
            default:
                return null;
        }
    }

    private void ApplyMutation(GameObject zombie, ZombieMutation mutation)
    {
        var enemy = zombie.GetComponent<Enemy>();
        var navAgent = zombie.GetComponent<NavMeshAgent>();
        var animator = zombie.GetComponent<Animator>();

        switch (mutation)
        {
            case ZombieMutation.Runner:
                
                float runnerSpeed = BASE_CHASE_SPEED * 2;
                float runnerSpeedMultiplier = 2; 

                if (animator != null)
                {
                    
                    animator.speed = runnerSpeedMultiplier; 

                    var controller = animator.runtimeAnimatorController;
                    if (controller != null)
                    {
                        var behaviors = animator.GetBehaviours<ZombieChaseState>();
                        foreach (var chaseBehavior in behaviors)
                        {
                            chaseBehavior.chaseSpeed = runnerSpeed;
                        }
                    }

                    if (navAgent != null)
                    {
                        navAgent.speed = runnerSpeed;
                    }
                }

                if (enemy != null)
                {
                    enemy.HP = Mathf.RoundToInt(enemy.HP * 0.5f);
                }

                zombie.transform.localScale *= 0.9f;
                ChangeZombieColor(zombie, runnerColor);
                break;

            case ZombieMutation.Tank:
                
                float tankSpeed = BASE_CHASE_SPEED * 0.7f;
                float tankSpeedMultiplier = 0.7f; 

                if (animator != null)
                {
                    
                    animator.speed = tankSpeedMultiplier; 

                    var behaviors = animator.GetBehaviours<ZombieChaseState>();
                    foreach (var chaseBehavior in behaviors)
                    {
                        chaseBehavior.chaseSpeed = tankSpeed;
                    }

                    if (navAgent != null)
                    {
                        navAgent.speed = tankSpeed;
                        navAgent.acceleration = 6f;
                    }
                }

                if (enemy != null)
                {
                    enemy.HP = Mathf.RoundToInt(enemy.HP * 3f);
                }

                if (enemy != null)
                {
                    animator.GetComponent<ZombieAttackState>().attackDamage = (int)(30 * 1.5f);
                }

                zombie.transform.localScale *= 1.2f;
                ChangeZombieColor(zombie, tankColor);
                break;

            case ZombieMutation.Screamer:
                
                ChangeZombieColor(zombie, screamerColor);

                var screamerBehavior = zombie.AddComponent<ScreamerBehavior>();
                screamerBehavior.alertRadius = 20f;
                screamerBehavior.cooldownTime = 5f;
                break;

            case ZombieMutation.Exploder:
                
                float exploderSpeed = BASE_CHASE_SPEED * 0.83f;
                float exploderSpeedMultiplier = 0.83f; 

                if (animator != null)
                {
                    
                    animator.speed = exploderSpeedMultiplier; 

                    var behaviors = animator.GetBehaviours<ZombieChaseState>();
                    foreach (var chaseBehavior in behaviors)
                    {
                        chaseBehavior.chaseSpeed = exploderSpeed;
                    }

                    if (navAgent != null)
                    {
                        navAgent.speed = exploderSpeed;
                    }
                }

                if (enemy != null)
                {
                    enemy.HP = Mathf.RoundToInt(enemy.HP * 0.8f);
                }

                ChangeZombieColor(zombie, exploderColor);

                var exploderBehavior = zombie.AddComponent<ExploderBehavior>();
                exploderBehavior.explosionRange = 4f;
                exploderBehavior.explosionDamage = 50;
                break;
        }
    }

    private void ChangeZombieColor(GameObject zombie, Color newColor)
    {
        Renderer[] renderers = zombie.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            Material newMaterial = new Material(renderer.material);
            newMaterial.color = newColor;
            renderer.material = newMaterial;
        }
    }

    public void TakeDamage(int damage)
    {
        if (spawnerDestroyed) return;

        currentSpawnerHealth -= damage;

        StartCoroutine(FlashSpawner());

        if (damageEffect != null)
        {
            damageEffect.Play();
        }

        if (spawnerHitSound != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.zombieChannel1.PlayOneShot(spawnerHitSound);
        }

        if (currentSpawnerHealth <= 0)
        {
            DestroySpawner();
        }

        UpdateUI();
    }

    private IEnumerator FlashSpawner()
    {
        Renderer flashRenderer = spawnerRenderer;

        if (flashRenderer == null)
        {
            flashRenderer = GetComponent<Renderer>();
        }

        if (flashRenderer == null)
        {
            flashRenderer = GetComponent<MeshRenderer>();
        }

        if (flashRenderer != null && flashRenderer.enabled)
        {
            Color prevColor = flashRenderer.material.color;
            flashRenderer.material.color = Color.red;
            yield return new WaitForSeconds(0.2f);

            if (!spawnerDestroyed && flashRenderer.enabled)
            {
                flashRenderer.material.color = prevColor;
            }
        }
    }

    private void DestroySpawner()
    {
        spawnerDestroyed = true;

        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
        }

        if (destructionEffect != null)
        {
            destructionEffect.Play();
        }

        if (spawnerDestroyedSound != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.zombieChannel1.PlayOneShot(spawnerDestroyedSound);
        }

        if (CameraShaker.Instance != null)
        {
            CameraShaker.Instance.TriggerShake(0.4f, 0.2f);
        }

        HideSpawner();

        Collider spawnerCollider = GetComponent<Collider>();
        if (spawnerCollider != null)
        {
            spawnerCollider.enabled = false;
        }

        isRespawning = true;
        Invoke(nameof(StartRespawnCountdown), 0.1f);

        UpdateUI();
    }

    private void StartRespawnCountdown()
    {
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(RespawnSpawner());
        }
        else
        {
            Debug.LogWarning("GameObject inactive, using alternative respawn method");
            Invoke(nameof(RespawnSpawnerNow), respawnTimer);
        }
    }

    private void HideSpawner()
    {
        if (spawnerVisual != null && spawnerVisual != gameObject)
        {
            spawnerVisual.SetActive(false);
        }

        Renderer spawnerMeshRenderer = GetComponent<Renderer>();
        if (spawnerMeshRenderer != null)
        {
            spawnerMeshRenderer.enabled = false;
        }

        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.enabled = false;
        }

        Renderer[] childRenderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in childRenderers)
        {
            if (renderer.GetComponent<ParticleSystem>() == null)
            {
                renderer.enabled = false;
            }
        }
    }

    private void ShowSpawner()
    {
        if (spawnerVisual != null && spawnerVisual != gameObject)
        {
            spawnerVisual.SetActive(true);
        }

        Renderer spawnerMeshRenderer = GetComponent<Renderer>();
        if (spawnerMeshRenderer != null)
        {
            spawnerMeshRenderer.enabled = true;
        }

        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.enabled = true;
        }

        Renderer[] childRenderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in childRenderers)
        {
            renderer.enabled = true;
        }
    }

    private IEnumerator RespawnSpawner()
    {
        if (!isRespawning) yield break;

        float countdown = respawnTimer;

        while (countdown > 0 && isRespawning)
        {
            if (spawnerHealthText != null)
            {
                spawnerHealthText.text = $"Respawning in {countdown:F0}s";
                spawnerHealthText.color = Color.cyan;
            }

            countdown -= Time.deltaTime;
            yield return null;
        }

        if (isRespawning)
        {
            RespawnSpawnerNow();
        }
    }

    private void RespawnSpawnerNow()
    {
        currentSpawnerHealth = originalSpawnerHealth;
        spawnerDestroyed = false;
        isRespawning = false;

        AdvanceToNextWave();

        ShowSpawner();

        StartCoroutine(RespawnFlashEffect());

        Collider spawnerCollider = GetComponent<Collider>();
        if (spawnerCollider != null)
        {
            spawnerCollider.enabled = true;
        }

        StartInfiniteSpawning();

        UpdateUI();
    }

    private IEnumerator RespawnFlashEffect()
    {
        Renderer flashRenderer = spawnerRenderer;

        if (flashRenderer == null)
        {
            flashRenderer = GetComponent<Renderer>();
        }

        if (flashRenderer == null)
        {
            flashRenderer = GetComponent<MeshRenderer>();
        }

        if (flashRenderer != null)
        {
            for (int i = 0; i < 3; i++)
            {
                if (flashRenderer.material != null)
                {
                    flashRenderer.material.color = Color.cyan;
                    yield return new WaitForSeconds(0.1f);
                    flashRenderer.material.color = Color.white;
                    yield return new WaitForSeconds(0.1f);
                }
            }

            if (flashRenderer.material != null)
            {
                flashRenderer.material.color = originalColor;
            }
        }
    }

    private void UpdateUI()
    {
        if (currentWaveText != null)
        {
            currentWaveText.text = currentWave.ToString();
            currentWaveText.gameObject.SetActive(true);
        }

        if (nextWaveText != null)
        {
            if (isRespawning)
            {
                nextWaveText.text = (currentWave + 1).ToString();
            }
            else
            {
                nextWaveText.text = (currentWave + 1).ToString();
            }
            nextWaveText.gameObject.SetActive(true);
        }

        if (spawnerHealthText != null)
        {
            if (isRespawning)
            {
                
            }
            else if (spawnerDestroyed)
            {
                spawnerHealthText.text = "Destroyed";
                spawnerHealthText.color = Color.red;
            }
            else
            {
                spawnerHealthText.text = $"Spawner: {currentSpawnerHealth}/{originalSpawnerHealth}";
                spawnerHealthText.color = Color.white;
            }
        }

        if (spawnRateText != null)
        {
            if (spawnerDestroyed || isRespawning)
            {
                spawnRateText.text = "Stopped";
            }
            else
            {
                spawnRateText.text = $"Rate: {currentSpawnInterval:F1}s";
            }
        }

        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        if (waveProgressBar != null)
        {
            float healthPercentage;

            if (spawnerDestroyed || isRespawning)
            {
                healthPercentage = 0f;
            }
            else
            {
                healthPercentage = (float)currentSpawnerHealth / originalSpawnerHealth;
            }

            waveProgressBar.value = healthPercentage;

            if (progressBarFill != null)
            {
                if (isRespawning)
                {
                    progressBarFill.color = Color.cyan;
                }
                else if (healthPercentage > 0.6f)
                {
                    progressBarFill.color = Color.green;
                }
                else if (healthPercentage > 0.3f)
                {
                    progressBarFill.color = Color.yellow;
                }
                else
                {
                    progressBarFill.color = Color.red;
                }
            }
        }
    }

    public void OnEnemyDeath(GameObject enemyGameObject)
    {
        if (currentZombiesAlive.Contains(enemyGameObject))
        {
            currentZombiesAlive.Remove(enemyGameObject);

            var screamerBehavior = enemyGameObject.GetComponent<ScreamerBehavior>();
            var exploderBehavior = enemyGameObject.GetComponent<ExploderBehavior>();

            ZombieMutation mutationType = ZombieMutation.Normal;

            if (screamerBehavior != null)
            {
                mutationType = ZombieMutation.Screamer;
            }
            else if (exploderBehavior != null)
            {
                mutationType = ZombieMutation.Exploder;
            }
            else
            {
                Renderer renderer = enemyGameObject.GetComponentInChildren<Renderer>();
                if (renderer != null)
                {
                    Color zombieColor = renderer.material.color;
                    if (Vector3.Distance(new Vector3(zombieColor.r, zombieColor.g, zombieColor.b),
                                       new Vector3(Color.green.r, Color.green.g, Color.green.b)) < 0.1f)
                    {
                        mutationType = ZombieMutation.Runner;
                    }
                    else if (Vector3.Distance(new Vector3(zombieColor.r, zombieColor.g, zombieColor.b),
                                           new Vector3(Color.red.r, Color.red.g, Color.red.b)) < 0.1f)
                    {
                        mutationType = ZombieMutation.Tank;
                    }
                }
            }

            if (currentMutationCounts.ContainsKey(mutationType))
            {
                currentMutationCounts[mutationType] = Mathf.Max(0, currentMutationCounts[mutationType] - 1);
            }
        }
    }

    private bool IsPositionInBounds(Vector3 position)
    {
        if (!hasBoundaries) return true;
        return playAreaBounds.Contains(position);
    }


    private Vector3 GetValidSpawnPosition(bool isBoss = false)
    {
        if (GlobalReferences.Instance?.player == null)
            return transform.position + Vector3.forward * minSpawnDistance;

        Vector3 playerPosition = GlobalReferences.Instance.player.transform.position;
        Vector3 spawnerPosition = transform.position;
        int maxAttempts = 100;
        float bossMinDistance = isBoss ? minSpawnDistance + 10f : minSpawnDistance;
        float bossMaxDistance = isBoss ? minSpawnDistance + 20f : spawnRadius;

        for (int i = 0; i < maxAttempts; i++)
        {
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            float randomDist = Random.Range(bossMinDistance, bossMaxDistance);

            Vector3 spawnCandidate = spawnerPosition + new Vector3(
                randomDir.x * randomDist,
                spawnHeightOffset,
                randomDir.y * randomDist
            );

            if (NavMesh.SamplePosition(spawnCandidate, out NavMeshHit hit, 10f, NavMesh.AllAreas))
            {
                Vector3 validPosition = ClampToBoundaries(hit.position);

                if (Vector3.Distance(playerPosition, validPosition) >= minSpawnDistance &&
                    IsPositionInBounds(validPosition))
                {
                    return validPosition;
                }
            }
        }

        for (int angle = 0; angle < 360; angle += 30)
        {
            float rad = angle * Mathf.Deg2Rad;
            Vector3 fallbackPos = spawnerPosition + new Vector3(
                Mathf.Cos(rad) * minSpawnDistance,
                spawnHeightOffset,
                Mathf.Sin(rad) * minSpawnDistance
            );

            if (NavMesh.SamplePosition(fallbackPos, out NavMeshHit fallbackHit, 50f, NavMesh.AllAreas))
            {
                return fallbackHit.position;
            }
        }

        Vector3 finalFallback = spawnerPosition + Vector3.forward * minSpawnDistance;
        Debug.LogWarning($"No valid NavMesh position found, using final fallback at {finalFallback}");
        return finalFallback;
    }

    private void Update()
    {
        UpdateUI();

        if (isRespawning && spawnerHealthText != null)
        {
            respawnTimer -= Time.deltaTime;
            if (respawnTimer <= 0)
            {
                RespawnSpawnerNow();
            }
            else
            {
                spawnerHealthText.text = $"Respawning in {respawnTimer:F0}s";
                spawnerHealthText.color = Color.cyan;
            }
        }
    }
}

public class ScreamerBehavior : MonoBehaviour
{
    [HideInInspector] public float alertRadius = 20f;
    [HideInInspector] public float cooldownTime = 5f;

    private Transform player;
    private float lastScreamTime = 0f;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    private void Update()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= alertRadius && Time.time - lastScreamTime > cooldownTime)
        {
            PerformScream();
            lastScreamTime = Time.time;
        }
    }

    private void PerformScream()
    {
        Collider[] nearbyZombies = Physics.OverlapSphere(transform.position, alertRadius);

        foreach (Collider col in nearbyZombies)
        {
            if (col.gameObject != gameObject && col.GetComponent<Enemy>() != null)
            {
                Animator nearbyAnimator = col.GetComponent<Animator>();
                if (nearbyAnimator != null)
                {
                    nearbyAnimator.SetBool("isChasing", true);
                    StartCoroutine(BoostZombieSpeed(col.gameObject, 1.5f, 8f));
                }
            }
        }
    }

    private IEnumerator BoostZombieSpeed(GameObject zombie, float multiplier, float duration)
    {
        if (zombie == null) yield break;

        var navAgent = zombie.GetComponent<NavMeshAgent>();
        var animator = zombie.GetComponent<Animator>();

        if (navAgent == null || animator == null) yield break;

        float UpdatedSpeedNav = navAgent.speed * multiplier;
        float UpdatedSpeedAnim = animator.speed * multiplier;

        navAgent.speed = UpdatedSpeedNav;
        animator.speed = UpdatedSpeedAnim;  

        yield return new WaitForSeconds(duration);


    }
}

public class ExploderBehavior : MonoBehaviour
{
    [HideInInspector] public float explosionRange = 4f;
    [HideInInspector] public int explosionDamage = 50;

    private Transform player;
    private bool hasExploded = false;
    private Enemy enemyComponent;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        enemyComponent = GetComponent<Enemy>();

        StartCoroutine(PulseSize());
    }

    private void Update()
    {
        if (player == null || hasExploded || enemyComponent.isDead) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= explosionRange)
        {
            StartCoroutine(ExplodeAfterDelay(0.8f));
        }
    }

    private IEnumerator ExplodeAfterDelay(float delay)
    {
        hasExploded = true;

        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.isStopped = true;
        }

        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetTrigger("DAMAGE");
        }

        yield return new WaitForSeconds(delay);

        Explode();
    }

    private void Explode()
    {
        
        if (CameraShaker.Instance && GlobalReferences.Instance?.player)
        {
            float distance = Vector3.Distance(transform.position, GlobalReferences.Instance.player.transform.position);
            float maxShakeDistance = 20f;
            float closeShakeIntensity = 0.2f;
            float closeShakeDuration = 0.4f;
            float farShakeIntensity = 0.04f;
            float farShakeDuration = 0.15f;

            if (distance <= maxShakeDistance)
            {
                float shakeIntensity = Mathf.Lerp(closeShakeIntensity, farShakeIntensity, distance / maxShakeDistance);
                float shakeDuration = Mathf.Lerp(closeShakeDuration, farShakeDuration, distance / maxShakeDistance);
                CameraShaker.Instance.TriggerShake(shakeDuration, shakeIntensity);
            }
        }

        
        if (GlobalReferences.Instance?.grenadeExplosionEffect != null)
        {
            Instantiate(GlobalReferences.Instance.grenadeExplosionEffect, transform.position, Quaternion.identity);
        }

        if (SoundManager.Instance != null && SoundManager.Instance.throwablesChannel != null)
        {
            SoundManager.Instance.throwablesChannel.PlayOneShot(SoundManager.Instance.grenadeSound);
        }

        
        float damageRadius = 20f;
        float explosionForce = 1200f;
        float maxDamage = 100f;
        Collider[] colliders = Physics.OverlapSphere(transform.position, damageRadius);
        HashSet<GameObject> damagedEntities = new HashSet<GameObject>();

        foreach (Collider nearby in colliders)
        {
            Rigidbody rb = nearby.attachedRigidbody;
            if (rb != null)
            {
                rb.AddExplosionForce(explosionForce, transform.position, damageRadius);
            }

            
            GameObject rootObject = transform.root.gameObject;

            
            if (damagedEntities.Contains(rootObject)) return;
            damagedEntities.Add(rootObject);

            float distance = Vector3.Distance(transform.position, rootObject.transform.position);
            int damage = Mathf.RoundToInt(
                Mathf.Clamp(maxDamage * (1 - distance / damageRadius), 0, maxDamage)
            );

            
            Enemy enemy = rootObject.GetComponent<Enemy>();
            if (enemy)
            {
                enemy.TakeDamage(damage);
                return;
            }

            
            Player player = rootObject.GetComponent<Player>();
            if (player)
            {
                player.TakeDamage(damage);
            }
        }

        
        if (enemyComponent != null)
        {
            enemyComponent.TakeDamage(999);
        }
    }

    private IEnumerator PulseSize()
    {
        Vector3 originalScale = transform.localScale;

        while (!hasExploded && !enemyComponent.isDead)
        {
            float pulse = (Mathf.Sin(Time.time * 5f) + 1f) * 0.05f;
            transform.localScale = originalScale + Vector3.one * pulse;
            yield return null;
        }
    }
}

public class BoundaryConstraint : MonoBehaviour
{
    private Bounds bounds;
    private NavMeshAgent agent;
    private float checkInterval = 0.5f;
    private float lastCheckTime;

    public void Initialize(Bounds playAreaBounds)
    {
        bounds = playAreaBounds;
    }

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        lastCheckTime = Time.time;
    }

    private void Update()
    {
        if (Time.time - lastCheckTime > checkInterval)
        {
            lastCheckTime = Time.time;
            ConstrainToBounds();
        }
    }

    private void ConstrainToBounds()
    {
        if (bounds.size == Vector3.zero) return;

        Vector3 currentPos = transform.position;
        bool needsCorrection = false;
        Vector3 correctedPos = currentPos;

        if (!bounds.Contains(currentPos))
        {
            correctedPos = bounds.ClosestPoint(currentPos);
            needsCorrection = true;
        }

        
        float wallBuffer = 1.5f;
        if (currentPos.x <= bounds.min.x + wallBuffer ||
            currentPos.x >= bounds.max.x - wallBuffer ||
            currentPos.z <= bounds.min.z + wallBuffer ||
            currentPos.z >= bounds.max.z - wallBuffer)
        {
            
            Vector3 centerDirection = (bounds.center - currentPos).normalized;
            correctedPos = currentPos + centerDirection * wallBuffer;
            needsCorrection = true;
        }

        if (needsCorrection && agent != null && agent.enabled)
        {
            agent.Warp(correctedPos);
        }
    }
}