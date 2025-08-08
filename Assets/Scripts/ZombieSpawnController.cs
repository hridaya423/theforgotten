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

    
    private int currentWave = 1;
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
        var zombieScript = zombie.GetComponent<Zombie>();
        var animator = zombie.GetComponent<Animator>();

        switch (mutation)
        {
            case ZombieMutation.Runner:
                

                
                if (animator != null)
                {
                    
                    var controller = animator.runtimeAnimatorController;
                    if (controller != null)
                    {
                        
                        var behaviors = animator.GetBehaviours<ZombieChaseState>();
                        foreach (var chaseBehavior in behaviors)
                        {
                            chaseBehavior.chaseSpeed = 12f; 
                        }
                    }

                    
                    if (navAgent != null)
                    {
                        navAgent.speed = 12f;
                        navAgent.acceleration = 12f; 
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
                

                
                if (animator != null)
                {
                    var behaviors = animator.GetBehaviours<ZombieChaseState>();
                    foreach (var chaseBehavior in behaviors)
                    {
                        chaseBehavior.chaseSpeed = 4.2f; 
                    }

                    
                    if (navAgent != null)
                    {
                        navAgent.speed = 4.2f;
                        navAgent.acceleration = 6f; 
                    }
                }

                
                if (enemy != null)
                {
                    enemy.HP = Mathf.RoundToInt(enemy.HP * 3f);
                }

                
                if (zombieScript != null)
                {
                    zombieScript.zombieDamage = Mathf.RoundToInt(zombieScript.zombieDamage * 1.5f);
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
                

                
                if (animator != null)
                {
                    var behaviors = animator.GetBehaviours<ZombieChaseState>();
                    foreach (var chaseBehavior in behaviors)
                    {
                        chaseBehavior.chaseSpeed = 5f; 
                    }

                    if (navAgent != null)
                    {
                        navAgent.speed = 5f;
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

        
        if (SoundManager.Instance != null && SoundManager.Instance.zombieChannel1 != null)
        {
            
            
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
                if (Vector3.Distance(playerPosition, hit.position) >= minSpawnDistance)
                {
                    
                    return hit.position;
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

    private void OnDrawGizmosSelected()
    {
        Vector3 spawnerPos = transform.position;

        Gizmos.color = spawnerDestroyed ? Color.gray : Color.red;
        Gizmos.DrawWireSphere(spawnerPos, minSpawnDistance);
        Gizmos.color = spawnerDestroyed ? Color.gray : Color.yellow;
        Gizmos.DrawWireSphere(spawnerPos, spawnRadius);

        Gizmos.color = spawnerDestroyed ? Color.black : Color.green;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 2f);
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

                    
                    StartCoroutine(BoostChaseSpeed(nearbyAnimator, 1.5f, 8f));
                }
            }
        }
    }

    private IEnumerator BoostChaseSpeed(Animator animator, float multiplier, float duration)
    {
        if (animator == null) yield break;

        
        var behaviors = animator.GetBehaviours<ZombieChaseState>();
        float[] originalSpeeds = new float[behaviors.Length];

        
        for (int i = 0; i < behaviors.Length; i++)
        {
            originalSpeeds[i] = behaviors[i].chaseSpeed;
            behaviors[i].chaseSpeed *= multiplier;
        }

        
        NavMeshAgent agent = animator.GetComponent<NavMeshAgent>();
        float originalAgentSpeed = 6f;
        if (agent != null)
        {
            originalAgentSpeed = agent.speed;
            agent.speed *= multiplier;
        }

        yield return new WaitForSeconds(duration);

        
        if (animator != null)
        {
            behaviors = animator.GetBehaviours<ZombieChaseState>();
            for (int i = 0; i < behaviors.Length && i < originalSpeeds.Length; i++)
            {
                behaviors[i].chaseSpeed = originalSpeeds[i];
            }

            if (agent != null)
            {
                agent.speed = originalAgentSpeed;
            }
        }
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
        

        
        Collider[] affectedObjects = Physics.OverlapSphere(transform.position, explosionRange);

        foreach (Collider col in affectedObjects)
        {
            if (col.gameObject != gameObject)
            {
                
                if (col.CompareTag("Player"))
                {
                    
                    
                }

                
                Enemy enemy = col.GetComponent<Enemy>();
                if (enemy != null && !enemy.isDead)
                {
                    enemy.TakeDamage(explosionDamage / 2);
                }
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