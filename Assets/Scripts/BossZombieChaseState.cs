using UnityEngine;
using UnityEngine.AI;

public class BossZombieChaseState : StateMachineBehaviour
{
    private NavMeshAgent agent;
    private Transform player;
    private BossZombie bossComponent;

    public float chaseSpeed = 4f;
    public float enragedChaseSpeed = 6f;
    public float stopChasingDistance = 150f;
    public float attackingDistance = 3.5f;

    private float nextRoarTime = 0f;
    private float roarInterval = 8f;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        
        player = GameObject.FindGameObjectWithTag("Player").transform;

        
        agent = animator.GetComponent<NavMeshAgent>();
        bossComponent = animator.GetComponent<BossZombie>();

        
        agent.speed = animator.GetBool("isEnraged") ? enragedChaseSpeed : chaseSpeed;

        
        PlayChaseSound(animator);
        nextRoarTime = Time.time + roarInterval;
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        
        if (Time.time >= nextRoarTime)
        {
            PlayChaseSound(animator);
            nextRoarTime = Time.time + roarInterval;
        }

        
        agent.SetDestination(player.position);

        
        float distanceFromPlayer = Vector3.Distance(player.position, animator.transform.position);

        
        if (distanceFromPlayer > stopChasingDistance)
        {
            animator.SetBool("isChasing", false);
        }

        
        if (distanceFromPlayer < attackingDistance)
        {
            animator.SetBool("isAttacking", true);
        }

        
        if (animator.GetBool("isEnraged") && distanceFromPlayer < 15f && distanceFromPlayer > attackingDistance)
        {
            if (Random.value < 0.005f) 
            {
                PerformLeapAttack(animator);
            }
        }
    }

    private void PlayChaseSound(Animator animator)
    {
        if (SoundManager.Instance != null && !SoundManager.Instance.zombieChannel.isPlaying)
        {
            
            if (animator.GetBool("isEnraged"))
            {
                SoundManager.Instance.zombieChannel.PlayOneShot(SoundManager.Instance.bossSpecialAttackSound);
            }
            else
            {
                SoundManager.Instance.zombieChannel.PlayOneShot(SoundManager.Instance.zombieChasing);
            }
        }
    }

    private void PerformLeapAttack(Animator animator)
    {
        
        if (!animator.GetBool("isEnraged")) return;

        
        animator.SetTrigger("LEAP");

        
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.zombieChannel.PlayOneShot(SoundManager.Instance.bossSpecialAttackSound);
        }
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        
        agent.SetDestination(agent.transform.position);

        
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.zombieChannel.Stop();
        }
    }
}
