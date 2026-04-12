using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class BotAI : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform player;

    [Header("Rangos")]
    [SerializeField] private float detectionRange = 12f;
    [SerializeField] private float loseRange = 18f;
    [SerializeField] private float attackRange = 1.8f;

    [Header("Ataque")]
    [SerializeField] private float attackCooldown = 1.2f;

    [Header("Velocidades")]
    [SerializeField] private float runSpeed = 3.8f;

    [Header("Referencias")]
    [SerializeField] private Animator animator;

    [Header("Animator Parameters")]
    [SerializeField] private string walkParameter = "Walk";
    [SerializeField] private string runParameter = "Run";
    [SerializeField] private string attackParameter = "Attack";

    [Header("NavMesh Fix")]
    [SerializeField] private float snapToNavMeshDistance = 2f;

    private NavMeshAgent agent;
    private float nextAttackTime;
    private bool isChasing;
    private bool isAttacking;

    private int walkHash;
    private int runHash;
    private int attackHash;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator == null)
            Debug.LogError("BotAI: No se encontró Animator.");

        walkHash = Animator.StringToHash(walkParameter);
        runHash = Animator.StringToHash(runParameter);
        attackHash = Animator.StringToHash(attackParameter);
    }

    private void Start()
    {
        SnapAgentToNavMesh();
    }

    private void Update()
    {
        if (player == null || agent == null || animator == null)
        {
            SetMovementAnimation(false, false);
            return;
        }

        if (!agent.enabled || !agent.isOnNavMesh)
        {
            SetMovementAnimation(false, false);
            return;
        }

        float dist = Vector3.Distance(transform.position, player.position);

        if (!isChasing && dist <= detectionRange)
            isChasing = true;

        if (isChasing && dist > loseRange)
            isChasing = false;

        if (!isChasing)
        {
            HandleIdle();
            return;
        }

        if (dist <= attackRange)
        {
            HandleAttack();
        }
        else
        {
            HandleChase();
        }
    }

    private void SnapAgentToNavMesh()
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, snapToNavMeshDistance, NavMesh.AllAreas))
        {
            transform.position = hit.position;
            agent.Warp(hit.position);
        }
    }

    private void HandleIdle()
    {
        if (agent.hasPath)
            agent.ResetPath();

        SetMovementAnimation(false, false);
    }

    private void HandleChase()
    {
        if (isAttacking)
            return;

        agent.speed = runSpeed;
        agent.stoppingDistance = attackRange - 0.1f;
        agent.SetDestination(player.position);

        bool moving = agent.velocity.magnitude > 0.1f;
        SetMovementAnimation(false, moving);
    }

    private void HandleAttack()
    {
        agent.ResetPath();
        SetMovementAnimation(false, false);

        Vector3 dir = player.position - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }

        if (Time.time >= nextAttackTime && !isAttacking)
        {
            isAttacking = true;
            animator.ResetTrigger(attackHash);
            animator.SetTrigger(attackHash);
            nextAttackTime = Time.time + attackCooldown;
        }
    }

    private void SetMovementAnimation(bool walk, bool run)
    {
        animator.SetBool(walkHash, walk);
        animator.SetBool(runHash, run);
    }

    public void EndAttack()
    {
        isAttacking = false;
    }

    public void SetPlayer(Transform newPlayer)
    {
        player = newPlayer;
    }
}