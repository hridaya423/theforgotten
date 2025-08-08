using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class ZombiePatrollingState : StateMachineBehaviour
{
    float timer;
    public float patrollingTime = 0f;

    Transform player;
    NavMeshAgent navAgent;

    public float detectionArea = 200f;
    public float patrolSpeed = 2f;

    List<Transform> waypointsList = new List<Transform>();
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        navAgent = animator.GetComponent<NavMeshAgent>();

        navAgent.speed = patrolSpeed;
        timer = 0;

        GameObject waypointCluster = GameObject.FindGameObjectWithTag("Waypoints");
        foreach (Transform t in waypointCluster.transform)
        {
            waypointsList.Add(t);
        }
        Vector3 nextPosition = waypointsList[Random.Range(0, waypointsList.Count)].position;
        navAgent.SetDestination(nextPosition);
    }
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {

        if (SoundManager.Instance.zombieChannel.isPlaying == false)
        {
            SoundManager.Instance.zombieChannel.PlayOneShot(SoundManager.Instance.zombieWalking);
            SoundManager.Instance.zombieChannel.PlayDelayed(1f);
        }
        if (navAgent.remainingDistance < navAgent.stoppingDistance)
        {
            navAgent.SetDestination(waypointsList[Random.Range(0, waypointsList.Count)].position);
        }

        timer += Time.deltaTime;
        if (timer > patrollingTime)
        {
            animator.SetBool("isPatrolling", false);
        }

        float distanceFromPlayer = Vector3.Distance(player.position, animator.transform.position);
        if (distanceFromPlayer >     detectionArea)
        {
            animator.SetBool("isChasing", true);
        }
    }
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        navAgent.SetDestination(navAgent.transform.position);
        SoundManager.Instance.zombieChannel.Stop();
    }
}

