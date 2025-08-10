using System;
using UnityEngine;
using UnityEngine.AI;

public class ZombieAttackState : StateMachineBehaviour
{
    Transform player;
    NavMeshAgent agent;

    [Header("Attack Settings")]
    public float stopAttackingDistance = 2.5f;
    public float attackRange = 2f; 
    public int attackDamage = 30;

    [Header("Attack Timing")]
    public float attackHitDelay = 0.5f; 

    private float animationTimer = 0f;
    private bool hasDealtDamage = false;


    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        agent = animator.GetComponent<NavMeshAgent>();

        
        animationTimer = 0f;
        hasDealtDamage = false;
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        
        if (SoundManager.Instance != null && !SoundManager.Instance.zombieChannel.isPlaying)
        {
            SoundManager.Instance.zombieChannel.PlayOneShot(SoundManager.Instance.zombieAttacking);
        }

        LookAtPlayer();

        
        animationTimer += Time.deltaTime;

        
        if (!hasDealtDamage && animationTimer >= attackHitDelay)
        {
            TryDealDamage(animator);
            hasDealtDamage = true;
        }

        if (stateInfo.normalizedTime % 1f < 0.1f && animationTimer > 0.2f)
        {
            animationTimer = 0f;
            hasDealtDamage = false;
        }

        
        float distanceFromPlayer = Vector3.Distance(player.position, animator.transform.position);
        if (distanceFromPlayer > stopAttackingDistance)
        {
            animator.SetBool("isAttacking", false);
        }
    }


    private void TryDealDamage(Animator animator)
    {
        if (player == null) return;


        
        Vector3 attackPosition = animator.transform.position + animator.transform.forward * 0.5f;
        attackPosition.y += 1f; 

        
        Collider[] hitColliders = Physics.OverlapSphere(attackPosition, attackRange);

        foreach (Collider hit in hitColliders)
        {
            if (hit.CompareTag("Player"))
            {
                Player playerScript = hit.GetComponent<Player>();
                if (playerScript != null && !playerScript.isDead)
                {
                    
                    playerScript.TakeDamage(attackDamage);



                    
                    CreateHitEffect(hit.transform.position);

                    break; 
                }
            }
        }
    }

    private void CreateHitEffect(Vector3 hitPosition)
    {
        
        if (GlobalReferences.Instance != null && GlobalReferences.Instance.bloodSprayEffect != null)
        {
            Vector3 effectPos = hitPosition;
            effectPos.y += 1f; 

            GameObject bloodEffect = Instantiate(
                GlobalReferences.Instance.bloodSprayEffect,
                effectPos,
                Quaternion.identity
            );

            
            if (agent != null)
            {
                Vector3 direction = (hitPosition - agent.transform.position).normalized;
                bloodEffect.transform.rotation = Quaternion.LookRotation(direction);
            }

            Destroy(bloodEffect, 3f);
        }
    }

    private void LookAtPlayer()
    {
        if (player == null || agent == null) return;

        Vector3 direction = player.position - agent.transform.position;
        agent.transform.rotation = Quaternion.LookRotation(direction);
        var yRotation = agent.transform.eulerAngles.y;
        agent.transform.rotation = Quaternion.Euler(0, yRotation, 0);
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.zombieChannel.Stop();
        }
    }

}