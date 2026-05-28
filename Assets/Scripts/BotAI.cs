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
    [SerializeField] private PlayerCombat playerCombat;
    [SerializeField] private bool canTargetPlayer = true;
    [SerializeField] private bool canTargetEnemies = true;
    [SerializeField] private bool canTargetOtherBots = false;
    [SerializeField] private float targetRefreshInterval = 0.35f;
    [SerializeField] private float targetSwitchDistanceBias = 0.75f;

    [Header("Rangos")]
    [SerializeField] private float detectionRange = 12f;
    [SerializeField] private float loseRange = 18f;
    [SerializeField] private float attackRange = 1.8f;

    [Header("Ataque")]
    [SerializeField] private float attackCooldown = 1.2f;
    [SerializeField] private Vector2 attackCooldownJitter = new Vector2(0f, 0.35f);
    [SerializeField] private float attackFailsafeDuration = 1.25f;
    [SerializeField] private float attackRangeBuffer = 0.25f;
    [SerializeField] private float attackFacingAngle = 70f;
    [SerializeField] private bool lockPositionWhileAttacking = true;
    [SerializeField] private EnemyAttackComboController attackComboController;

    [Header("Velocidades")]
    [SerializeField] private float runSpeed = 3.8f;
    [SerializeField] private float retreatSpeed = 1.9f;

    [Header("Control de distancia")]
    [SerializeField] private float forwardStepSpeed = 2.1f;
    [SerializeField] private float forwardStepDistance = 2.6f;
    [SerializeField] private float maxBackpedalDistanceFromPlayer = 2.35f;
    [SerializeField, HideInInspector] private float fleeSpeed = 5.5f;
    [SerializeField, HideInInspector] private float fleeDistance = 8f;
    [SerializeField, HideInInspector] private float fleeDuration = 2.5f;
    [SerializeField, HideInInspector] private float safeDistance = 7f;
    [SerializeField, HideInInspector] private float fleeRetryCooldown = 3f;

    [Header("Hit Reaction")]
    [SerializeField] private float hitReactionDuration = 0.45f;
    [SerializeField] private bool cancelAttackOnHit = true;

    [Header("Combat Decisions")]
    [SerializeField] private float defendDuration = 0.8f;
    [SerializeField] private float retreatDuration = 0.55f;
    [SerializeField] private float retreatDistance = 1.15f;
    [SerializeField] private float retreatRecoveryTime = 0.45f;
    [SerializeField] private float postAttackRecoveryTime = 0.45f;
    [SerializeField] private float decisionCooldown = 0.4f;
    [SerializeField] private float minSpacingDistance = 1.15f;
    [SerializeField] private float lowHealthKeepAwayDistance = 2.1f;

    [Header("Probabilidades con vida alta")]
    [Range(0f, 1f)] [SerializeField] private float attackChanceHighHealth = 0.65f;
    [Range(0f, 1f)] [SerializeField] private float defendChanceHighHealth = 0.20f;

    [Header("Probabilidades con vida baja")]
    [Range(0f, 1f)] [SerializeField] private float attackChanceLowHealth = 0.35f;
    [Range(0f, 1f)] [SerializeField] private float defendChanceLowHealth = 0.30f;
    [Range(0f, 1f)] [SerializeField] private float lowHealthRetreatChance = 0.55f;

    [Header("Vida baja")]
    [Range(0f, 1f)] [SerializeField] private float lowHealthThreshold = 0.4f;

    [Header("Reaccion al ataque del jugador")]
    [SerializeField] private float playerAttackThreatRange = 2.4f;
    [SerializeField] private float playerAttackFacingAngle = 115f;
    [SerializeField] private float playerAttackReactionCooldown = 0.65f;
    [Range(0f, 1f)] [SerializeField] private float blockPlayerAttackChance = 0.45f;
    [Range(0f, 1f)] [SerializeField] private float backpedalPlayerAttackChance = 0.45f;
    [Range(0f, 1f)] [SerializeField] private float lowHealthBackpedalPlayerAttackChance = 0.75f;

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
    [SerializeField] private float repathInterval = 0.15f;
    [SerializeField] private float destinationUpdateDistance = 0.35f;

    private NavMeshAgent agent;

    private float nextAttackTime;
    private float stateEndTime;
    private float nextDecisionTime;
    private float nextFleeTime;
    private float nextRepathTime;
    private float nextPlayerAttackReactionTime;
    private float postAttackRecoveryEndTime;
    private float nextTargetRefreshTime;
    private float attackStartTime;

    private bool isChasing;
    private bool isAttacking;
    private bool isHitReacting;
    private bool ignoreHitReactionDuringCommit;
    private bool wantsDefensiveActionAfterHit;
    private bool hasAttackLockPosition;
    private bool hasPostAttackLockPosition;
    private bool hasHitReactionLockPosition;

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
    private Vector3 attackLockPosition;
    private Vector3 postAttackLockPosition;
    private Vector3 hitReactionLockPosition;
    private Vector3 lastChaseDestination;
    private Vector3 lastKnownPlayerDir = Vector3.forward;
    private Transform currentTarget;
    private Health currentTargetHealth;
    private PlayerCombat currentTargetCombat;

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

        if (attackComboController == null)
            attackComboController = GetComponent<EnemyAttackComboController>();

        if (attackComboController == null)
            attackComboController = GetComponentInChildren<EnemyAttackComboController>();

        if (player != null && playerHealth == null)
        {
            playerHealth = player.GetComponent<Health>() ??
                player.GetComponentInParent<Health>() ??
                player.GetComponentInChildren<Health>();
        }

        if (player != null && playerCombat == null)
        {
            playerCombat = player.GetComponent<PlayerCombat>() ??
                player.GetComponentInParent<PlayerCombat>() ??
                player.GetComponentInChildren<PlayerCombat>();
        }

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

        ResolveTarget();

        if (currentTarget == null || currentTargetHealth == null || currentTargetHealth.IsDead)
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
            {
                isHitReacting = false;
                hasHitReactionLockPosition = false;

                if (wantsDefensiveActionAfterHit)
                {
                    wantsDefensiveActionAfterHit = false;
                    StartRetreat();
                }
            }

            return;
        }

        float dist = Vector3.Distance(transform.position, currentTarget.position);

        if (isAttacking && Time.time >= attackStartTime + attackFailsafeDuration)
            EndAttack();

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

        if (IsPostAttackRecovering())
        {
            HandlePostAttackRecovery();
            return;
        }

        if (TryReactToPlayerAttack(dist))
            return;

        switch (currentState)
        {
            case EnemyState.Patrolling:
                if (dist <= attackRange)
                    SetState(EnemyState.Attacking);
                else
                    SetState(EnemyState.Chasing);
                break;

            case EnemyState.Chasing:
                if (dist <= attackRange)
                    SetState(EnemyState.Attacking);
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
                StartRetreat();
                break;
        }
    }

    private void LateUpdate()
    {
        LockAttackPosition();
        LockPostAttackPosition();
        LockHitReactionPosition();
    }

    private void StopCombatCompletely()
    {
        isChasing = false;
        isAttacking = false;
        isHitReacting = false;
        ignoreHitReactionDuringCommit = false;
        wantsDefensiveActionAfterHit = false;
        hasAttackLockPosition = false;
        hasPostAttackLockPosition = false;
        hasHitReactionLockPosition = false;

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

    private void ResolveTarget()
    {
        ResolvePlayerReference();

        if (Time.time < nextTargetRefreshTime && IsCurrentTargetValid(loseRange))
            return;

        nextTargetRefreshTime = Time.time + targetRefreshInterval;

        Health bestTarget = FindBestTarget();

        if (bestTarget != null)
        {
            SetCurrentTarget(bestTarget);
            isChasing = true;
            return;
        }

        if (!IsCurrentTargetValid(loseRange))
            SetCurrentTarget(null);
    }

    private void ResolvePlayerReference()
    {
        if (player == null)
            return;

        if (playerHealth == null)
        {
            playerHealth = player.GetComponent<Health>() ??
                player.GetComponentInParent<Health>() ??
                player.GetComponentInChildren<Health>();
        }

        if (playerCombat == null)
        {
            playerCombat = player.GetComponent<PlayerCombat>() ??
                player.GetComponentInParent<PlayerCombat>() ??
                player.GetComponentInChildren<PlayerCombat>();
        }
    }

    private Health FindBestTarget()
    {
        Health[] candidates = FindObjectsByType<Health>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        Health bestTarget = null;
        float bestDistance = float.MaxValue;
        float maxDistance = isChasing ? loseRange : detectionRange;
        float maxDistanceSqr = maxDistance * maxDistance;
        float currentDistance = IsCurrentTargetValid(loseRange)
            ? Vector3.Distance(transform.position, currentTarget.position)
            : float.MaxValue;

        foreach (Health candidate in candidates)
        {
            if (!IsValidTargetHealth(candidate))
                continue;

            float sqrDistance = (candidate.transform.position - transform.position).sqrMagnitude;

            if (sqrDistance > maxDistanceSqr)
                continue;

            float distance = Mathf.Sqrt(sqrDistance);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestTarget = candidate;
            }
        }

        if (bestTarget == null)
            return null;

        if (currentTargetHealth != null && bestTarget != currentTargetHealth &&
            currentDistance <= bestDistance + targetSwitchDistanceBias)
            return currentTargetHealth;

        return bestTarget;
    }

    private bool IsCurrentTargetValid(float maxDistance)
    {
        if (!IsValidTargetHealth(currentTargetHealth) || currentTarget == null)
            return false;

        return Vector3.Distance(transform.position, currentTarget.position) <= maxDistance;
    }

    private bool IsValidTargetHealth(Health targetHealth)
    {
        if (targetHealth == null || targetHealth.IsDead || targetHealth == health)
            return false;

        if (targetHealth.transform.root == transform.root)
            return false;

        bool isPlayer = IsPlayerTarget(targetHealth);

        if (isPlayer)
            return canTargetPlayer;

        BotAI targetBot = targetHealth.GetComponent<BotAI>() ??
            targetHealth.GetComponentInChildren<BotAI>() ??
            targetHealth.GetComponentInParent<BotAI>();

        if (targetBot != null && !canTargetOtherBots)
            return false;

        return canTargetEnemies;
    }

    private bool IsPlayerTarget(Health targetHealth)
    {
        if (targetHealth == null)
            return false;

        if (playerHealth != null && targetHealth == playerHealth)
            return true;

        return player != null && targetHealth.transform.root == player.root;
    }

    private void SetCurrentTarget(Health targetHealth)
    {
        currentTargetHealth = targetHealth;
        currentTarget = targetHealth != null ? targetHealth.transform : null;
        currentTargetCombat = targetHealth != null ? targetHealth.GetComponent<PlayerCombat>() : null;

        if (currentTargetCombat == null && targetHealth != null)
            currentTargetCombat = targetHealth.GetComponentInParent<PlayerCombat>() ?? targetHealth.GetComponentInChildren<PlayerCombat>();
    }

    private void UpdateLastKnownDirection()
    {
        Vector3 dir = currentTarget.position - transform.position;
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
        if (IsPostAttackRecovering())
            return;

        if (isAttacking || isHitReacting)
            return;

        if (!agent.enabled || !agent.isOnNavMesh)
            return;

        SetState(EnemyState.Chasing);

        SetBlockAnimation(false);
        SetBackpedalAnimation(false);

        float dist = currentTarget != null ? Vector3.Distance(transform.position, currentTarget.position) : Mathf.Infinity;

        bool closeForwardStep = dist <= forwardStepDistance;

        agent.isStopped = false;
        agent.speed = closeForwardStep ? forwardStepSpeed : runSpeed;
        agent.stoppingDistance = Mathf.Max(attackRange - 0.15f, 0.5f);

        bool shouldUpdateDestination = Time.time >= nextRepathTime ||
            (currentTarget.position - lastChaseDestination).sqrMagnitude >= destinationUpdateDistance * destinationUpdateDistance;

        if (shouldUpdateDestination)
        {
            nextRepathTime = Time.time + repathInterval;
            lastChaseDestination = currentTarget.position;
            agent.SetDestination(lastChaseDestination);
        }

        bool moving = agent.velocity.magnitude > 0.1f;

        SetMovementAnimation(moving && closeForwardStep, moving && !closeForwardStep);
        FacePlayer();
    }

    private void HandleAttack()
    {
        SetState(EnemyState.Attacking);

        StopAgent();

        SetMovementAnimation(false, false);
        SetBlockAnimation(false);
        SetBackpedalAnimation(false);

        LockAttackPosition();

        FacePlayer();
        LockAttackPosition();

        float dist = Vector3.Distance(transform.position, currentTarget.position);

        if (!isAttacking && dist > attackRange + attackRangeBuffer)
        {
            isAttacking = false;
            hasAttackLockPosition = false;
            SetState(EnemyState.Chasing);
            return;
        }

        if (!isAttacking && attackComboController != null && !attackComboController.CanAttackAtDistance(dist))
        {
            hasAttackLockPosition = false;
            SetState(EnemyState.Chasing);
            return;
        }

        if (!IsFacingPlayer(attackFacingAngle))
            return;

        if (Time.time >= nextAttackTime && !isAttacking)
        {
            isAttacking = true;
            attackStartTime = Time.time;
            CaptureAttackLockPosition();

            float cooldownMultiplier = attackComboController != null
                ? attackComboController.SelectAttack(dist)
                : 1f;

            animator.ResetTrigger(attackHash);
            animator.SetTrigger(attackHash);

            float minJitter = Mathf.Min(attackCooldownJitter.x, attackCooldownJitter.y);
            float maxJitter = Mathf.Max(attackCooldownJitter.x, attackCooldownJitter.y);
            float cooldownJitter = Random.Range(minJitter, maxJitter);
            nextAttackTime = Time.time + (attackCooldown * cooldownMultiplier) + Mathf.Max(0f, cooldownJitter);
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
                SetState(EnemyState.Attacking);
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

            if (IsLowHealth() && dist <= lowHealthKeepAwayDistance)
            {
                SetState(EnemyState.Attacking);
            }
            else if (dist <= attackRange)
            {
                SetState(EnemyState.Attacking);
            }
            else
            {
                SetState(EnemyState.Chasing);
            }
        }
    }

    private void HandleFleeing(float dist)
    {
        StartRetreat();
        return;

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

        float dist = currentTarget != null ? Vector3.Distance(transform.position, currentTarget.position) : Mathf.Infinity;

        if (dist <= attackRange && Time.time >= nextAttackTime)
        {
            SetState(EnemyState.Attacking);
            return;
        }

        if (ShouldKeepDistance(dist))
        {
            StartRetreat();
            return;
        }

        if (IsLowHealth() && dist <= lowHealthKeepAwayDistance && Random.value < lowHealthRetreatChance)
        {
            StartRetreat();
            return;
        }

        float attackChance = IsLowHealth() ? attackChanceLowHealth : attackChanceHighHealth;
        float defendChance = IsLowHealth() ? defendChanceLowHealth : defendChanceHighHealth;
        float retreatChance = IsLowHealth() ? lowHealthRetreatChance : 0f;

        float totalChance = Mathf.Max(attackChance + defendChance + retreatChance, 0.001f);
        float roll = Random.value * totalChance;

        if (roll < retreatChance)
        {
            StartRetreat();
        }
        else if (roll < retreatChance + attackChance)
        {
            SetState(EnemyState.Attacking);
        }
        else if (roll < retreatChance + attackChance + defendChance)
        {
            SetState(EnemyState.Defending);
            stateEndTime = Time.time + defendDuration;
        }
        else
        {
            StartRetreat();
        }
    }

    private bool TryReactToPlayerAttack(float dist)
    {
        if (currentTargetCombat == null || !currentTargetCombat.IsAttacking)
            return false;

        if (Time.time < nextPlayerAttackReactionTime)
            return false;

        if (isAttacking || isHitReacting || currentState == EnemyState.Fleeing)
            return false;

        if (dist > playerAttackThreatRange)
            return false;

        if (!IsPlayerFacingEnemy(playerAttackFacingAngle))
            return false;

        nextPlayerAttackReactionTime = Time.time + playerAttackReactionCooldown;

        float backpedalChance = IsLowHealth() ? lowHealthBackpedalPlayerAttackChance : backpedalPlayerAttackChance;
        float roll = Random.value;

        if (roll < backpedalChance)
        {
            StartRetreat();
            return true;
        }

        if (roll < backpedalChance + blockPlayerAttackChance)
        {
            SetState(EnemyState.Defending);
            stateEndTime = Time.time + defendDuration;
            return true;
        }

        SetState(EnemyState.Attacking);
        return true;
    }

    private bool ShouldKeepDistance(float dist)
    {
        return dist <= minSpacingDistance;
    }

    private void StartRetreat()
    {
        if (currentTarget == null)
            return;

        float currentDistance = Vector3.Distance(transform.position, currentTarget.position);

        if (currentDistance >= maxBackpedalDistanceFromPlayer)
        {
            StopAgent();
            hasPostAttackLockPosition = false;
            postAttackRecoveryEndTime = 0f;
            SetState(EnemyState.Defending);
            stateEndTime = Time.time + retreatRecoveryTime;
            return;
        }

        SetState(EnemyState.Retreating);

        isAttacking = false;
        hasPostAttackLockPosition = false;
        postAttackRecoveryEndTime = 0f;
        stateEndTime = Time.time + retreatDuration;

        Vector3 awayDir = transform.position - currentTarget.position;
        awayDir.y = 0f;

        if (awayDir.sqrMagnitude < 0.001f)
            awayDir = -transform.forward;

        awayDir.Normalize();

        float remainingRoom = Mathf.Max(0.25f, maxBackpedalDistanceFromPlayer - currentDistance);
        float stepDistance = Mathf.Min(retreatDistance, remainingRoom);

        if (currentDistance > minSpacingDistance)
            stepDistance *= 0.7f;

        retreatTarget = transform.position + awayDir * stepDistance;

        NavMeshHit hit;

        if (NavMesh.SamplePosition(retreatTarget, out hit, stepDistance + 0.75f, NavMesh.AllAreas))
            retreatTarget = hit.position;
    }

    private void StartFlee()
    {
        StartRetreat();
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

    private bool IsFacingPlayer(float maxAngle)
    {
        Vector3 dir = lastKnownPlayerDir;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.001f)
            return true;

        float angle = Vector3.Angle(transform.forward, dir.normalized);
        return angle <= maxAngle;
    }

    private bool IsPlayerFacingEnemy(float maxAngle)
    {
        if (player == null)
            return false;

        Vector3 dirToEnemy = transform.position - player.position;
        dirToEnemy.y = 0f;

        if (dirToEnemy.sqrMagnitude < 0.001f)
            return true;

        Vector3 playerForward = player.forward;
        playerForward.y = 0f;

        if (playerForward.sqrMagnitude < 0.001f)
            return true;

        float angle = Vector3.Angle(playerForward.normalized, dirToEnemy.normalized);
        return angle <= maxAngle * 0.5f;
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

    private bool IsPostAttackRecovering()
    {
        return Time.time < postAttackRecoveryEndTime;
    }

    private void BeginPostAttackRecovery()
    {
        postAttackRecoveryEndTime = Time.time + postAttackRecoveryTime;
        postAttackLockPosition = transform.position;
        hasPostAttackLockPosition = true;

        StopAgent();
        SetMovementAnimation(false, false);
        SetBlockAnimation(false);
        SetBackpedalAnimation(false);

        if (agent != null && agent.enabled && agent.isOnNavMesh)
            agent.Warp(postAttackLockPosition);
    }

    private void HandlePostAttackRecovery()
    {
        StopAgent();
        SetMovementAnimation(false, false);
        SetBlockAnimation(false);
        SetBackpedalAnimation(false);
        FacePlayer();
        LockPostAttackPosition();
    }

    private void StopAgent()
    {
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.velocity = Vector3.zero;
        }
    }

    private void CaptureAttackLockPosition()
    {
        if (!lockPositionWhileAttacking)
            return;

        attackLockPosition = transform.position;
        hasAttackLockPosition = true;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
            agent.Warp(attackLockPosition);
    }

    private void LockAttackPosition()
    {
        if (!lockPositionWhileAttacking || !isAttacking)
            return;

        if (!hasAttackLockPosition)
            CaptureAttackLockPosition();

        Quaternion currentRotation = transform.rotation;
        transform.position = attackLockPosition;
        transform.rotation = currentRotation;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.nextPosition = attackLockPosition;
            agent.velocity = Vector3.zero;
        }
    }

    private void LockPostAttackPosition()
    {
        if (!lockPositionWhileAttacking || !hasPostAttackLockPosition)
            return;

        if (!IsPostAttackRecovering())
        {
            hasPostAttackLockPosition = false;
            return;
        }

        Quaternion currentRotation = transform.rotation;
        transform.position = postAttackLockPosition;
        transform.rotation = currentRotation;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.nextPosition = postAttackLockPosition;
            agent.velocity = Vector3.zero;
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
        hitReactionLockPosition = transform.position;
        hasHitReactionLockPosition = true;
        wantsDefensiveActionAfterHit = IsLowHealth() || Random.value < 0.35f;

        if (cancelAttackOnHit)
        {
            isAttacking = false;
            hasAttackLockPosition = false;
        }

        hasPostAttackLockPosition = false;
        postAttackRecoveryEndTime = 0f;

        StopAgent();
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.Warp(hitReactionLockPosition);
            agent.nextPosition = hitReactionLockPosition;
        }

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
        hasAttackLockPosition = false;

        if (currentTarget == null || (currentTargetHealth != null && currentTargetHealth.IsDead))
        {
            StopCombatCompletely();
            return;
        }

        BeginPostAttackRecovery();
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
        hasHitReactionLockPosition = false;
    }

    private void LockHitReactionPosition()
    {
        if (!hasHitReactionLockPosition || !isHitReacting)
            return;

        Quaternion currentRotation = transform.rotation;
        transform.position = hitReactionLockPosition;
        transform.rotation = currentRotation;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.nextPosition = hitReactionLockPosition;
            agent.velocity = Vector3.zero;
        }
    }

    public void IsAttacking()
    {
        isAttacking = true;
        attackStartTime = Time.time;
        CaptureAttackLockPosition();
    }

    public void ApplyRoundScaling(float damageMultiplier, float attackSpeedMultiplier, float movementSpeedMultiplier)
    {
        float safeAttackSpeed = Mathf.Max(0.1f, attackSpeedMultiplier);
        float safeMovementSpeed = Mathf.Max(0.1f, movementSpeedMultiplier);

        attackCooldown = Mathf.Max(0.25f, attackCooldown / safeAttackSpeed);
        attackCooldownJitter *= Mathf.Clamp01(1f / safeAttackSpeed);
        attackFailsafeDuration = Mathf.Max(0.55f, attackFailsafeDuration / safeAttackSpeed);
        runSpeed *= safeMovementSpeed;
        forwardStepSpeed *= safeMovementSpeed;
        retreatSpeed *= safeMovementSpeed;

        if (attackComboController != null)
            attackComboController.ApplyDamageMultiplier(damageMultiplier);
    }

    public void SetPlayer(Transform newPlayer)
    {
        player = newPlayer;
        playerHealth = null;
        playerCombat = null;

        if (newPlayer != null)
        {
            playerHealth = newPlayer.GetComponent<Health>() ??
                newPlayer.GetComponentInParent<Health>() ??
                newPlayer.GetComponentInChildren<Health>();

            playerCombat = newPlayer.GetComponent<PlayerCombat>() ??
                newPlayer.GetComponentInParent<PlayerCombat>() ??
                newPlayer.GetComponentInChildren<PlayerCombat>();
        }

        if (currentTarget == null && playerHealth != null && !playerHealth.IsDead)
            SetCurrentTarget(playerHealth);
    }
}
