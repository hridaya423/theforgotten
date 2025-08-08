using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    [SerializeField] internal int HP = 100;
    private Animator animator;
    private NavMeshAgent navAgent;
    internal bool isDead;

    private void Start()
    {
        animator = GetComponent<Animator>();
        navAgent = GetComponent<NavMeshAgent>();
    }

    public virtual void TakeDamage(int damageAmount)
    {
        if (isDead) return;

        HP -= damageAmount;

        if (HP <= 0)
        {
            if (!isDead)
            {
                isDead = true;
                GlobalReferences.Instance.IncrementZombiesKilled();

                ZombieSpawnController spawnController = FindObjectOfType<ZombieSpawnController>();
                if (spawnController != null)
                {
                    spawnController.OnEnemyDeath(gameObject);
                }

                navAgent.enabled = false;
                GetComponent<CapsuleCollider>().enabled = false;

                int randomValue = Random.Range(0, 2);
                animator.SetTrigger(randomValue == 0 ? "DIE1" : "DIE2");

                SoundManager.Instance.zombieChannel1.PlayOneShot(SoundManager.Instance.zombieDeath);
                StartCoroutine(DespawnAfterDelay(3f));
            }
        }
        else
        {
            animator.SetTrigger("DAMAGE");
            SoundManager.Instance.zombieChannel1.PlayOneShot(SoundManager.Instance.zombieHurt);
        }
    }

    private IEnumerator DespawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }
}