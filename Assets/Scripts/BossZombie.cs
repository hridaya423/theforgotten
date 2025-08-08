using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using TMPro;

public class BossZombie : Enemy
{
    [Header("Boss Settings")]
    [SerializeField] private int bossHP = 500;
    [SerializeField] private int bossDamage = 25;
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float enragedMoveSpeed = 6f;
    [SerializeField] private float attackRange = 3f;
    [SerializeField] private float specialAttackCooldown = 10f;
    [SerializeField] private float enragedSpecialAttackCooldown = 5f;
    [SerializeField] private GameObject specialAttackEffect;
    [SerializeField] private GameObject enrageEffectPrefab;
    [SerializeField] private TextMeshProUGUI bossHealthText;
    [SerializeField] private GameObject bossHealthBar;
    [SerializeField] private GameObject bossDeathRewardPrefab;

    private Animator bossAnimator;
    private NavMeshAgent bossAgent;
    private Transform player;
    private float nextSpecialAttackTime;
    private bool isSpecialAttacking;
    private bool isEnraged = false;
    private GameObject activeEnrageEffect;

    private float moveSoundCooldown = 0f;
    private float idleSoundCooldown = 0f;

    public static event System.Action OnBossDeath;

    private void Awake()
    {
        bossAnimator = GetComponent<Animator>();
        bossAgent = GetComponent<NavMeshAgent>();

        HP = bossHP;

        bossAgent.speed = moveSpeed;
        bossAgent.stoppingDistance = attackRange * 0.8f;
    }

    private void OnEnable()
    {
        if (GlobalReferences.Instance != null && GlobalReferences.Instance.player != null)
        {
            player = GlobalReferences.Instance.player.transform;
        }
        else
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }

        Zombie zombieComponent = GetComponent<Zombie>();
        if (zombieComponent != null)
        {
            zombieComponent.zombieDamage = bossDamage;
        }

        if (bossHealthBar != null)
        {
            bossHealthBar.SetActive(true);
            UpdateHealthUI();
        }
        SetupHandCollider();

        PlayBossIntro();
    }

    private void SetupHandCollider()
    {
        SetupSingleHandCollider("ZombieRig/mixamorig:Hips/mixamorig:Spine/mixamorig:Spine1/mixamorig:Spine2/mixamorig:RightShoulder/mixamorig:RightArm/mixamorig:RightForeArm/mixamorig:RightHand");
        SetupSingleHandCollider("ZombieRig/mixamorig:Hips/mixamorig:Spine/mixamorig:Spine1/mixamorig:Spine2/mixamorig:LeftShoulder/mixamorig:LeftArm/mixamorig:LeftForeArm/mixamorig:LeftHand");
    }

    private void SetupSingleHandCollider(string handPath)
    {
        Transform handTransform = transform.Find(handPath);
        if (handTransform != null)
        {
            SphereCollider handCollider = handTransform.GetComponent<SphereCollider>();
            if (handCollider == null)
            {
                handCollider = handTransform.gameObject.AddComponent<SphereCollider>();
                handCollider.radius = 0.4f;
                handCollider.isTrigger = true;
                handCollider.enabled = false;
                handTransform.gameObject.tag = "ZombieHand";

                ZombieHand zombieHand = handTransform.GetComponent<ZombieHand>();
                if (zombieHand == null)
                {
                    zombieHand = handTransform.gameObject.AddComponent<ZombieHand>();
                    zombieHand.damage = bossDamage;
                }
            }
        }
    }

    private void PlayBossIntro()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.playerChannel.PlayOneShot(SoundManager.Instance.bossSpawnSound);
        }

        bossAnimator.SetTrigger("SPAWN");

        StartCoroutine(FreezeMovementDuring("SPAWN", 3f));
    }

    private IEnumerator FreezeMovementDuring(string animationTrigger, float duration)
    {
        bossAgent.isStopped = true;
        yield return new WaitForSeconds(duration);
        bossAgent.isStopped = false;

        bossAnimator.SetBool("isChasing", true);
    }

    public override void TakeDamage(int damageAmount)
    {
        if (isDead) return;

        int actualDamage = Mathf.Max(1, damageAmount / 2);
        HP -= actualDamage;
        UpdateHealthUI();

        if (HP <= 0)
        {
            HandleDeath();
        }
        else
        {
            HandleDamageFeedback();

            if (HP < bossHP * 0.3f && !isEnraged)
            {
                EnterEnragedState();
            }
        }
    }

    private void HandleDamageFeedback()
    {
        bossAnimator.SetTrigger("DAMAGE");

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.zombieChannel.PlayOneShot(SoundManager.Instance.zombieHurt);
        }

        StartCoroutine(BrieflyStopAgent(0.5f));
    }

    private IEnumerator BrieflyStopAgent(float duration)
    {
        bossAgent.isStopped = true;
        yield return new WaitForSeconds(duration);
        if (!isDead)
        {
            bossAgent.isStopped = false;
        }
    }

    private void HandleDeath()
    {
        if (isDead) return;

        isDead = true;

        if (GlobalReferences.Instance != null)
        {
            GlobalReferences.Instance.IncrementZombiesKilled();
            GlobalReferences.Instance.IncrementBossesKilled();
        }

        bossAgent.enabled = false;
        GetComponent<CapsuleCollider>().enabled = false;

        if (activeEnrageEffect != null)
        {
            Destroy(activeEnrageEffect);
        }

        bossAnimator.SetTrigger("DIE1");

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.zombieChannel.PlayOneShot(SoundManager.Instance.bossDeathSound);
        }

        if (bossHealthBar != null)
        {
            bossHealthBar.SetActive(false);
        }

        if (OnBossDeath != null)
        {
            OnBossDeath.Invoke();
        }

        SpawnDeathRewards();

        StartCoroutine(DelayedDeath(3f));
    }

    private void SpawnDeathRewards()
    {
        if (GlobalReferences.Instance != null)
        {
            GlobalReferences.Instance.AddEssence(1);
        }

        if (bossDeathRewardPrefab != null)
        {
            Instantiate(bossDeathRewardPrefab, transform.position + Vector3.up, Quaternion.identity);
        }
    }

    private IEnumerator DelayedDeath(float delay)
    {
        yield return new WaitForSeconds(delay);

        GameObject deathEffectPrefab = Resources.Load<GameObject>("Prefabs/BossDeathEffect");
        if (deathEffectPrefab != null)
        {
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }

    private void EnterEnragedState()
    {
        isEnraged = true;
        bossAnimator.SetBool("isEnraged", true);

        bossAgent.speed = enragedMoveSpeed;
        nextSpecialAttackTime = Time.time;

        bossAnimator.SetTrigger("ENRAGE");

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.zombieChannel.PlayOneShot(SoundManager.Instance.bossSpecialAttackSound);
        }

        UpdateHandDamageForEnragedState();

        if (enrageEffectPrefab != null)
        {
            activeEnrageEffect = Instantiate(enrageEffectPrefab, transform);
        }

        StartCoroutine(BrieflyStopAgent(1.5f));
    }

    private void UpdateHandDamageForEnragedState()
    {
        UpdateSingleHandDamage("ZombieRig/mixamorig:Hips/mixamorig:Spine/mixamorig:Spine1/mixamorig:Spine2/mixamorig:RightShoulder/mixamorig:RightArm/mixamorig:RightForeArm/mixamorig:RightHand");
        UpdateSingleHandDamage("ZombieRig/mixamorig:Hips/mixamorig:Spine/mixamorig:Spine1/mixamorig:Spine2/mixamorig:LeftShoulder/mixamorig:LeftArm/mixamorig:LeftForeArm/mixamorig:LeftHand");
    }

    private void UpdateSingleHandDamage(string handPath)
    {
        Transform handTransform = transform.Find(handPath);
        if (handTransform != null)
        {
            ZombieHand zombieHand = handTransform.GetComponent<ZombieHand>();
            if (zombieHand != null)
            {
                zombieHand.damage = (int)(bossDamage * 1.5f);
            }
        }
    }

    private void UpdateHealthUI()
    {
        if (bossHealthText != null)
        {
            bossHealthText.text = $"Boss: {HP}/{bossHP}";
        }
    }

    public void PerformBossAttack()
    {
        if (isDead || player == null) return;

        LookAtPlayer();

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (Time.time >= nextSpecialAttackTime && !isSpecialAttacking && distanceToPlayer < 15f)
        {
            StartCoroutine(PerformSpecialAttack());
        }
    }

    public void ActivateHandColliders()
    {
        StartCoroutine(BrieflyEnableHandColliders());
    }

    private IEnumerator BrieflyEnableHandColliders()
    {
        EnableSingleHandCollider("ZombieRig/mixamorig:Hips/mixamorig:Spine/mixamorig:Spine1/mixamorig:Spine2/mixamorig:RightShoulder/mixamorig:RightArm/mixamorig:RightForeArm/mixamorig:RightHand", true);
        EnableSingleHandCollider("ZombieRig/mixamorig:Hips/mixamorig:Spine/mixamorig:Spine1/mixamorig:Spine2/mixamorig:LeftShoulder/mixamorig:LeftArm/mixamorig:LeftForeArm/mixamorig:LeftHand", true);

        yield return new WaitForSeconds(0.3f);

        EnableSingleHandCollider("ZombieRig/mixamorig:Hips/mixamorig:Spine/mixamorig:Spine1/mixamorig:Spine2/mixamorig:RightShoulder/mixamorig:RightArm/mixamorig:RightForeArm/mixamorig:RightHand", false);
        EnableSingleHandCollider("ZombieRig/mixamorig:Hips/mixamorig:Spine/mixamorig:Spine1/mixamorig:Spine2/mixamorig:LeftShoulder/mixamorig:LeftArm/mixamorig:LeftForeArm/mixamorig:LeftHand", false);
    }

    private void EnableSingleHandCollider(string handPath, bool enabled)
    {
        Transform handTransform = transform.Find(handPath);
        if (handTransform != null)
        {
            SphereCollider handCollider = handTransform.GetComponent<SphereCollider>();
            if (handCollider != null)
            {
                handCollider.enabled = enabled;
            }
        }
    }

    private IEnumerator PerformSpecialAttack()
    {
        isSpecialAttacking = true;

        bossAgent.isStopped = true;
        bossAnimator.SetTrigger("SPECIAL_ATTACK");

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.zombieChannel.PlayOneShot(SoundManager.Instance.bossSpecialAttackSound);
        }

        yield return new WaitForSeconds(1.5f);

        if (specialAttackEffect != null)
        {
            Instantiate(specialAttackEffect, transform.position + Vector3.up, Quaternion.identity);
        }

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 8f);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Player"))
            {
                Vector3 directionToPlayer = hitCollider.transform.position - transform.position;
                float distanceToPlayer = directionToPlayer.magnitude;

                RaycastHit hit;
                if (Physics.Raycast(transform.position + Vector3.up, directionToPlayer.normalized, out hit, distanceToPlayer))
                {
                    if (!hit.collider.CompareTag("Player"))
                    {
                        continue;
                    }
                }

                Player playerHealth = hitCollider.GetComponent<Player>();
                if (playerHealth != null)
                {
                    int specialDamage = isEnraged ? bossDamage * 3 : bossDamage * 2;
                    playerHealth.TakeDamage(specialDamage);

                    Debug.DrawLine(transform.position + Vector3.up, hitCollider.transform.position, Color.red, 1.0f);
                }
            }
        }

        yield return new WaitForSeconds(1f);

        float currentCooldown = isEnraged ? enragedSpecialAttackCooldown : specialAttackCooldown;
        nextSpecialAttackTime = Time.time + currentCooldown;

        bossAgent.isStopped = false;
        isSpecialAttacking = false;
    }

    private void LookAtPlayer()
    {
        if (player == null) return;

        Vector3 direction = player.position - transform.position;
        direction.y = 0;
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Euler(0, lookRotation.eulerAngles.y, 0);
    }

    private void OnDrawGizmosSelected()
    {
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 8f);

        
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, 30f);

        
        DrawHandGizmo("ZombieRig/mixamorig:Hips/mixamorig:Spine/mixamorig:Spine1/mixamorig:Spine2/mixamorig:RightShoulder/mixamorig:RightArm/mixamorig:RightForeArm/mixamorig:RightHand");
        DrawHandGizmo("ZombieRig/mixamorig:Hips/mixamorig:Spine/mixamorig:Spine1/mixamorig:Spine2/mixamorig:LeftShoulder/mixamorig:LeftArm/mixamorig:LeftForeArm/mixamorig:LeftHand");
    }

    private void DrawHandGizmo(string handPath)
    {
        Transform handTransform = transform.Find(handPath);
        if (handTransform != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(handTransform.position, 0.4f);
        }
    }
}
