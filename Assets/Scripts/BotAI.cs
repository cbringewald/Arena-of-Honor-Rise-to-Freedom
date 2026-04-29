using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class BotAI : MonoBehaviour
{
    public enum EnemyState
    {
        Patrolling,
        Chasing,
        Attacking,
        Defending,
        Retreating,
        Fleeing
    }

    [Header("Target")]
    [SerializeField] private Transform player;
    [SerializeField] private Health playerHealth;

    [Header("Rangos")]
    [SerializeField] private float detectionRange = 12f;
    [SerializeField] private float loseRange = 18f;
    [SerializeField] private float attackRange = 1.8f;

    [Header("Ataque")]
    [SerializeField] private float attackCooldown = 1.2f;

    [Header("Velocidades")]
    [SerializeField] private float runSpeed = 3.8f;
    [SerializeField] private float retreatSpeed = 3.2f;

    [Header("Huida")]
    [SerializeField] private float fleeSpeed = 5.5f;
    [SerializeField] private float fleeDistance = 8f;
    [SerializeField] private float fleeDuration = 2.5f;
    [SerializeField] private float safeDistance = 7f;

    [Header("Hit Reaction")]
    [SerializeField] private float hitReactionDuration = 0.45f;
    [SerializeField] private bool cancelAttackOnHit = true;

    [Header("Combat Decisions")]
    [SerializeField] private float defendDuration = 0.8f;
    [SerializeField] private float retreatDuration = 1.0f;
    [SerializeField] private float retreatDistance = 3f;
    [SerializeField] private float decisionCooldown = 0.4f;

    [Header("Probabilidades con vida alta")]
    [Range(0f, 1f)] [SerializeField] private float attackChanceHighHealth = 0.65f;
    [Range(0f, 1f)] [SerializeField] private float defendChanceHighHealth = 0.20f;

    [Header("Probabilidades con vida baja")]
    [Range(0f, 1f)] [SerializeField] private float attackChanceLowHealth = 0.35f;
    [Range(0f, 1f)] [SerializeField] private float defendChanceLowHealth = 0.30f;

    [Header("Vida baja")]
    [Range(0f, 1f)] [SerializeField] private float lowHealthThreshold = 0.4f;

    [Header("Huida con vida crítica")]
    [Range(0f, 1f)] [SerializeField] private float fleeHealthThreshold = 0.25f;
    [Range(0f, 1f)] [SerializeField] private float fleeChanceLowHealth = 0.65f;

    [Header("Referencias")]
    [SerializeField] private Animator animator;
    [SerializeField] private Health health;

    [Header("Animator Parameters")]
    [SerializeField] private string walkParameter = "Walk";
    [SerializeField] private string runParameter = "Run";
    [SerializeField] private string attackParameter = "Attack";
    [SerializeField] private string hitParameter = "Hit";
    [SerializeField] private string blockParameter = "Block";
    [SerializeField] private string backpedalParameter = "Backpedal";

    [Header("NavMesh Fix")]
    [SerializeField] private float snapToNavMeshDistance = 2f;

    private NavMeshAgent agent;

    private float nextAttackTime;
    private float stateEndTime;
    private float nextDecisionTime;

    private bool isChasing;
    private bool isAttacking;
    private bool isHitReacting;
    private bool ignoreHitReactionDuringCommit;

    private float hitReactEndTime;

    private EnemyState currentState = EnemyState.Patrolling;

    private int walkHash;
    private int runHash;
    private int attackHash;
    private int hitHash;
    private int blockHash;
    private int backpedalHash;

    private Vector3 retreatTarget;
    private Vector3 fleeTarget;
    private Vector3 lastKnownPlayerDir = Vector3.forward;

    public EnemyState CurrentState => currentState;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (health == null)
            health = GetComponent<Health>();

        if (player != null && playerHealth == null)
            playerHealth = player.GetComponent<Health>();

        walkHash = Animator.StringToHash(walkParameter);
        runHash = Animator.StringToHash(runParameter);
        attackHash = Animator.StringToHash(attackParameter);
        hitHash = Animator.StringToHash(hitParameter);
        blockHash = Animator.StringToHash(blockParameter);
        backpedalHash = Animator.StringToHash(backpedalParameter);

        if (agent != null)
            agent.updateRotation = false;
    }

    private void OnEnable()
    {
        if (health != null)
            health.OnDamaged += HandleDamaged;
    }

    private void OnDisable()
    {
        if (health != null)
            health.OnDamaged -= HandleDamaged;
    }

    private void Start()
    {
        SnapAgentToNavMesh();
        SetState(EnemyState.Patrolling);
    }

    private void Update()
    {
        if (health != null && health.IsDead)
        {
            StopCombatCompletely();
            return;
        }

        if (player == null)
        {
            StopCombatCompletely();
            return;
        }

        if (playerHealth == null)
            playerHealth = player.GetComponent<Health>();

        if (playerHealth != null && playerHealth.IsDead)
        {
            StopCombatCompletely();
            return;
        }

        if (agent == null || animator == null || !agent.enabled || !agent.isOnNavMesh)
        {
            SetMovementAnimation(false, false);
            SetBlockAnimation(false);
            SetBackpedalAnimation(false);
            return;
        }

        if (isHitReacting)
        {
            StopAgent();

            SetMovementAnimation(false, false);
            SetBlockAnimation(false);
            SetBackpedalAnimation(false);

            if (Time.time >= hitReactEndTime)
                isHitReacting = false;

            return;
        }

        float dist = Vector3.Distance(transform.position, player.position);

        if (!isChasing && dist <= detectionRange)
            isChasing = true;

        if (isChasing && dist > loseRange)
            isChasing = false;

        if (!isChasing)
        {
            SetState(EnemyState.Patrolling);
            HandlePatrolling();
            return;
        }

        UpdateLastKnownDirection();

        switch (currentState)
        {
            case EnemyState.Patrolling:
                if (dist <= attackRange)
                    DecideCloseCombatAction();
                else
                    SetState(EnemyState.Chasing);
                break;

            case EnemyState.Chasing:
                if (dist <= attackRange)
                    DecideCloseCombatAction();
                else
                    HandleChase();
                break;

            case EnemyState.Attacking:
                HandleAttack();
                break;

            case EnemyState.Defending:
                HandleDefending(dist);
                break;

            case EnemyState.Retreating:
                HandleRetreating(dist);
                break;

            case EnemyState.Fleeing:
                HandleFleeing(dist);
                break;
        }
    }

    private void StopCombatCompletely()
    {
        isChasing = false;
        isAttacking = false;
        isHitReacting = false;
        ignoreHitReactionDuringCommit = false;

        StopAgent();

        SetMovementAnimation(false, false);
        SetBlockAnimation(false);
        SetBackpedalAnimation(false);

        if (animator != null)
        {
            animator.ResetTrigger(attackHash);
            animator.ResetTrigger(hitHash);
        }

        currentState = EnemyState.Patrolling;
    }

    private void SnapAgentToNavMesh()
    {
        if (agent == null)
            return;

        NavMeshHit hit;

        if (NavMesh.SamplePosition(transform.position, out hit, snapToNavMeshDistance, NavMesh.AllAreas))
        {
            transform.position = hit.position;
            agent.Warp(hit.position);
        }
    }

    private void UpdateLastKnownDirection()
    {
        Vector3 dir = player.position - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude > 0.01f)
            lastKnownPlayerDir = dir.normalized;
    }

    private void SetState(EnemyState newState)
    {
        if (currentState == newState)
            return;

        currentState = newState;

        if (currentState != EnemyState.Defending)
            SetBlockAnimation(false);

        if (currentState != EnemyState.Retreating)
            SetBackpedalAnimation(false);
    }

    private void HandlePatrolling()
    {
        StopAgent();

        SetMovementAnimation(false, false);
        SetBlockAnimation(false);
        SetBackpedalAnimation(false);

        if (isChasing)
            SetState(EnemyState.Chasing);
    }

    private void HandleChase()
    {
        if (isAttacking || isHitReacting)
            return;

        if (!agent.enabled || !agent.isOnNavMesh)
            return;

        SetState(EnemyState.Chasing);

        SetBlockAnimation(false);
        SetBackpedalAnimation(false);

        agent.isStopped = false;
        agent.speed = runSpeed;
        agent.stoppingDistance = Mathf.Max(attackRange - 0.15f, 0.5f);
        agent.SetDestination(player.position);

        bool moving = agent.velocity.magnitude > 0.1f;

        SetMovementAnimation(false, moving);
        FaceMovementDirection();
    }

    private void HandleAttack()
    {
        SetState(EnemyState.Attacking);

        StopAgent();

        SetMovementAnimation(false, false);
        SetBlockAnimation(false);
        SetBackpedalAnimation(false);

        FacePlayer();

        if (Time.time >= nextAttackTime && !isAttacking)
        {
            isAttacking = true;

            animator.ResetTrigger(attackHash);
            animator.SetTrigger(attackHash);

            nextAttackTime = Time.time + attackCooldown;
        }
    }

    private void HandleDefending(float dist)
    {
        SetState(EnemyState.Defending);

        StopAgent();

        SetMovementAnimation(false, false);
        SetBackpedalAnimation(false);
        SetBlockAnimation(true);

        FacePlayer();

        if (Time.time >= stateEndTime)
        {
            SetBlockAnimation(false);

            if (dist <= attackRange)
                DecideCloseCombatAction();
            else
                SetState(EnemyState.Chasing);
        }
    }

    private void HandleRetreating(float dist)
    {
        SetState(EnemyState.Retreating);

        SetBlockAnimation(false);

        if (!agent.enabled || !agent.isOnNavMesh)
            return;

        agent.isStopped = false;
        agent.speed = retreatSpeed;
        agent.stoppingDistance = 0f;
        agent.SetDestination(retreatTarget);

        bool moving = agent.velocity.magnitude > 0.1f;

        SetMovementAnimation(false, false);
        SetBackpedalAnimation(moving);

        FacePlayer();

        bool reachedRetreatTarget = !agent.pathPending && agent.remainingDistance <= 0.4f;

        if (Time.time >= stateEndTime || reachedRetreatTarget)
        {
            SetBackpedalAnimation(false);

            if (dist <= attackRange)
                DecideCloseCombatAction();
            else
                SetState(EnemyState.Chasing);
        }
    }

    private void HandleFleeing(float dist)
    {
        SetState(EnemyState.Fleeing);

        SetBlockAnimation(false);
        SetBackpedalAnimation(false);

        if (!agent.enabled || !agent.isOnNavMesh)
            return;

        agent.isStopped = false;
        agent.speed = fleeSpeed;
        agent.stoppingDistance = 0f;
        agent.SetDestination(fleeTarget);

        bool moving = agent.velocity.magnitude > 0.1f;

        SetMovementAnimation(false, moving);

        // En huida real NO mira al jugador.
        // Mira hacia donde corre para que la animación Run no parezca rara.
        FaceMovementDirection();

        bool reachedFleeTarget = !agent.pathPending && agent.remainingDistance <= 0.5f;

        if (Time.time >= stateEndTime || reachedFleeTarget || dist >= safeDistance)
        {
            SetMovementAnimation(false, false);

            if (dist <= detectionRange)
                SetState(EnemyState.Chasing);
            else
            {
                isChasing = false;
                SetState(EnemyState.Patrolling);
            }
        }
    }

    private void DecideCloseCombatAction()
    {
        if (Time.time < nextDecisionTime || isAttacking || isHitReacting)
            return;

        nextDecisionTime = Time.time + decisionCooldown;

        if (ShouldTryToFlee())
        {
            StartFlee();
            return;
        }

        float attackChance = IsLowHealth() ? attackChanceLowHealth : attackChanceHighHealth;
        float defendChance = IsLowHealth() ? defendChanceLowHealth : defendChanceHighHealth;

        float roll = Random.value;

        if (roll < attackChance)
        {
            SetState(EnemyState.Attacking);
        }
        else if (roll < attackChance + defendChance)
        {
            SetState(EnemyState.Defending);
            stateEndTime = Time.time + defendDuration;
        }
        else
        {
            StartRetreat();
        }
    }

    private bool ShouldTryToFlee()
    {
        if (health == null)
            return false;

        if (health.NormalizedHealth > fleeHealthThreshold)
            return false;

        return Random.value < fleeChanceLowHealth;
    }

    private void StartRetreat()
    {
        if (player == null)
            return;

        SetState(EnemyState.Retreating);

        isAttacking = false;
        stateEndTime = Time.time + retreatDuration;

        Vector3 awayDir = transform.position - player.position;
        awayDir.y = 0f;

        if (awayDir.sqrMagnitude < 0.001f)
            awayDir = -transform.forward;

        awayDir.Normalize();

        retreatTarget = transform.position + awayDir * retreatDistance;

        NavMeshHit hit;

        if (NavMesh.SamplePosition(retreatTarget, out hit, retreatDistance + 2f, NavMesh.AllAreas))
            retreatTarget = hit.position;
    }

    private void StartFlee()
    {
        if (player == null)
            return;

        SetState(EnemyState.Fleeing);

        isAttacking = false;
        stateEndTime = Time.time + fleeDuration;

        Vector3 awayDir = transform.position - player.position;
        awayDir.y = 0f;

        if (awayDir.sqrMagnitude < 0.001f)
            awayDir = -transform.forward;

        awayDir.Normalize();

        fleeTarget = transform.position + awayDir * fleeDistance;

        NavMeshHit hit;

        if (NavMesh.SamplePosition(fleeTarget, out hit, fleeDistance + 3f, NavMesh.AllAreas))
            fleeTarget = hit.position;
    }

    private bool IsLowHealth()
    {
        if (health == null)
            return false;

        return health.NormalizedHealth <= lowHealthThreshold;
    }

    private void FacePlayer()
    {
        Vector3 dir = lastKnownPlayerDir;
        dir.y = 0f;

        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }
    }

    private void FaceMovementDirection()
    {
        if (agent == null)
            return;

        Vector3 dir = agent.velocity;
        dir.y = 0f;

        if (dir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(dir.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }
    }

    private void StopAgent()
    {
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }

    private void SetMovementAnimation(bool walk, bool run)
    {
        if (animator == null)
            return;

        animator.SetBool(walkHash, walk);
        animator.SetBool(runHash, run);
    }

    private void SetBlockAnimation(bool value)
    {
        if (animator == null)
            return;

        animator.SetBool(blockHash, value);
    }

    private void SetBackpedalAnimation(bool value)
    {
        if (animator == null)
            return;

        animator.SetBool(backpedalHash, value);
    }

    private void HandleDamaged(int damage, string hitZone)
    {
        if (health != null && health.IsDead)
            return;

        if (ignoreHitReactionDuringCommit)
            return;

        isHitReacting = true;
        hitReactEndTime = Time.time + hitReactionDuration;

        if (cancelAttackOnHit)
            isAttacking = false;

        StopAgent();

        SetMovementAnimation(false, false);
        SetBlockAnimation(false);
        SetBackpedalAnimation(false);

        if (animator != null)
        {
            animator.ResetTrigger(attackHash);
            animator.ResetTrigger(hitHash);
            animator.SetTrigger(hitHash);
        }
    }

    public void EndAttack()
    {
        isAttacking = false;
        ignoreHitReactionDuringCommit = false;

        if (player == null || (playerHealth != null && playerHealth.IsDead))
        {
            StopCombatCompletely();
            return;
        }

        float dist = Vector3.Distance(transform.position, player.position);

        if (dist <= attackRange)
            DecideCloseCombatAction();
        else
            SetState(EnemyState.Chasing);
    }

    public void BeginAttackCommit()
    {
        ignoreHitReactionDuringCommit = true;
    }

    public void EndAttackCommit()
    {
        ignoreHitReactionDuringCommit = false;
    }

    public void EndHitReaction()
    {
        isHitReacting = false;
    }

    public void IsAttacking()
    {
        isAttacking = true;
    }

    public void SetPlayer(Transform newPlayer)
    {
        player = newPlayer;
        playerHealth = newPlayer != null ? newPlayer.GetComponent<Health>() : null;
    }
}