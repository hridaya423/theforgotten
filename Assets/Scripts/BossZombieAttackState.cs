using UnityEngine;
using UnityEngine.AI;

public class BossZombieAttackState : StateMachineBehaviour
{
    private Transform player;
    private NavMeshAgent agent;
    private BossZombie bossComponent;

    public float stopAttackingDistance = 3.5f;
    private float attackCooldown = 1.5f;
    private float nextAttackTime = 0f;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        
        player = GameObject.FindGameObjectWithTag("Player").transform;

        
        agent = animator.GetComponent<NavMeshAgent>();
        bossComponent = animator.GetComponent<BossZombie>();

        
        agent.isStopped = true;
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        
        if (SoundManager.Instance != null && !SoundManager.Instance.zombieChannel.isPlaying)
        {
            SoundManager.Instance.zombieChannel.PlayOneShot(SoundManager.Instance.zombieAttacking);
        }

        
        LookAtPlayer(animator);

        
        float distanceFromPlayer = Vector3.Distance(player.position, animator.transform.position);

        if (distanceFromPlayer > stopAttackingDistance)
        {
            
            animator.SetBool("isAttacking", false);
        }

        
        if (Time.time >= nextAttackTime)
        {
            
            ActivateAttackColliders(animator);

            
            if (bossComponent != null)
            {
                bossComponent.PerformBossAttack();
            }

            
            nextAttackTime = Time.time + attackCooldown;

            
            if (animator.GetBool("isEnraged"))
            {
                nextAttackTime = Time.time + (attackCooldown * 0.7f);
            }
        }
    }

    private void ActivateAttackColliders(Animator animator)
    {
        
        BossZombie boss = animator.GetComponent<BossZombie>();
        if (boss != null)
        {
            boss.ActivateHandColliders();
        }
    }

    private void LookAtPlayer(Animator animator)
    {
        if (player == null) return;

        Vector3 direction = player.position - animator.transform.position;
        direction.y = 0; 
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        animator.transform.rotation = Quaternion.Euler(0, lookRotation.eulerAngles.y, 0);
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.zombieChannel.Stop();
        }

        
        agent.isStopped = false;
    }
}
