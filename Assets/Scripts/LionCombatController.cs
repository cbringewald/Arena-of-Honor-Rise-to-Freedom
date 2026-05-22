using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(NavMeshAgent))]
public class LionCombatController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform player;
    [SerializeField] private Health playerHealth;
    [SerializeField] private bool canTargetPlayer = true;
    [SerializeField] private bool canTargetEnemies = true;
    [SerializeField] private bool canTargetOtherLions;
    [SerializeField] private float targetRefreshInterval = 0.35f;
    [SerializeField] private float targetSwitchDistanceBias = 0.75f;

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Health health;
    [SerializeField] private Transform hitPoint;

    [Header("Animator Parameters")]
    [SerializeField] private string speedParameter = "Speed";
    [SerializeField] private string backpedalParameter = "Backpedal";
    [SerializeField] private string attackTriggerParameter = "Attack";
    [SerializeField] private string attackIndexParameter = "AttackIndex";
    [SerializeField] private string roarTriggerParameter = "Roar";
    [SerializeField] private string deathTriggerParameter = "Death";

    [Header("Ranges")]
    [SerializeField] private float detectionRange = 14f;
    [SerializeField] private float loseRange = 22f;
    [SerializeField] private float attackRange = 2.1f;
    [SerializeField] private float tooCloseDistance = 1.1f;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 1.8f;
    [SerializeField] private float runSpeed = 4.5f;
    [SerializeField] private float backpedalSpeed = 1.5f;
    [SerializeField] private float repathInterval = 0.15f;
    [SerializeField] private float turnSpeed = 12f;
    [SerializeField] private float animatorDampTime = 0.08f;

    [Header("Attack")]
    [SerializeField] private int attackDamage = 1;
    [SerializeField] private float attackCooldown = 1.4f;
    [SerializeField] private float attackDuration = 0.75f;
    [SerializeField] private float attackHitDelay = 0.45f;
    [SerializeField] private float attackHitRadius = 0.65f;
    [SerializeField] private LayerMask attackMask = ~0;
    [SerializeField] private bool avoidRepeatingAttack = true;

    [Header("Roar")]
    [SerializeField] private bool roarBeforeFirstAttack = true;
    [SerializeField] private float roarDuration = 0.85f;

    private Vector3 lastDestination;
    private float nextRepathTime;
    private float nextAttackTime;
    private bool isChasing;
    private bool isBusy;
    private Coroutine currentActionRoutine;
    private bool hasRoared;
    private int lastAttackIndex = -1;
    private Transform currentTarget;
    private Health currentTargetHealth;
    private float nextTargetRefreshTime;

    private int speedHash;
    private int backpedalHash;
    private int attackHash;
    private int attackIndexHash;
    private int roarHash;
    private int deathHash;

    private bool hasSpeedParameter;
    private bool hasBackpedalParameter;
    private bool hasAttackParameter;
    private bool hasAttackIndexParameter;
    private bool hasRoarParameter;
    private bool hasDeathParameter;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (health == null)
            health = GetComponent<Health>();

        if (hitPoint == null)
            hitPoint = transform;

        if (agent != null)
        {
            agent.updateRotation = false;
            agent.speed = runSpeed;
        }

        CacheAnimatorParameters();
    }

    private void OnEnable()
    {
        if (health != null)
            health.OnDied += HandleDeath;
    }

    private void OnDisable()
    {
        if (health != null)
            health.OnDied -= HandleDeath;
    }

    private void Update()
    {
        if (health != null && health.IsDead)
            return;

        ResolveTarget();

        if (currentTarget == null || currentTargetHealth == null || currentTargetHealth.IsDead)
        {
            StopAgent();
            SetMovementAnimation(0f, false);
            return;
        }

        if (isBusy)
        {
            FaceTarget();
            return;
        }

        float distance = Vector3.Distance(transform.position, currentTarget.position);

        if (!isChasing && distance <= detectionRange)
            isChasing = true;

        if (isChasing && distance > loseRange)
            isChasing = false;

        if (!isChasing)
        {
            StopAgent();
            SetMovementAnimation(0f, false);
            return;
        }

        if (roarBeforeFirstAttack && !hasRoared && hasRoarParameter && distance <= attackRange + 1.2f)
        {
            StartActionCoroutine(RoarRoutine());
            return;
        }

        if (distance <= attackRange && Time.time >= nextAttackTime)
        {
            StartActionCoroutine(AttackRoutine());
            return;
        }

        if (distance <= tooCloseDistance)
        {
            BackpedalFromTarget();
            return;
        }

        ChaseTarget(distance);
    }

    public void SetPlayer(Transform newPlayer)
    {
        player = newPlayer;
        playerHealth = null;

        if (newPlayer != null)
        {
            playerHealth = newPlayer.GetComponent<Health>() ??
                newPlayer.GetComponentInParent<Health>() ??
                newPlayer.GetComponentInChildren<Health>();
        }

        if (currentTarget == null && playerHealth != null && !playerHealth.IsDead)
            SetCurrentTarget(playerHealth);
    }

    public void LionAnim_EndAction()
    {
        EndCurrentAction();
    }

    public void LionAnim_Hit()
    {
        TryDamageCurrentTarget();
    }

    private void ResolvePlayerReference()
    {
        if (player != null)
        {
            if (playerHealth == null)
            {
                playerHealth = player.GetComponent<Health>() ??
                    player.GetComponentInParent<Health>() ??
                    player.GetComponentInChildren<Health>();
            }

            return;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
            SetPlayer(playerObject.transform);
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
        {
            currentTarget = null;
            currentTargetHealth = null;
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

            Transform candidateTransform = candidate.transform;
            float sqrDistance = (candidateTransform.position - transform.position).sqrMagnitude;

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

        Transform targetTransform = targetHealth.transform;

        if (targetTransform.root == transform.root)
            return false;

        bool isPlayer = IsPlayerTarget(targetHealth);

        if (isPlayer)
            return canTargetPlayer;

        LionCombatController targetLion = targetHealth.GetComponent<LionCombatController>() ??
            targetHealth.GetComponentInChildren<LionCombatController>() ??
            targetHealth.GetComponentInParent<LionCombatController>();

        if (targetLion != null && !canTargetOtherLions)
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
    }

    private void ChaseTarget(float distance)
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            return;

        bool shouldRun = distance > attackRange + 2f;
        agent.isStopped = false;
        agent.speed = shouldRun ? runSpeed : walkSpeed;
        agent.stoppingDistance = Mathf.Max(attackRange - 0.2f, 0.4f);

        bool shouldRepath = Time.time >= nextRepathTime ||
            (currentTarget.position - lastDestination).sqrMagnitude > 0.35f * 0.35f;

        if (shouldRepath)
        {
            nextRepathTime = Time.time + repathInterval;
            lastDestination = currentTarget.position;
            agent.SetDestination(lastDestination);
        }

        FaceTarget();
        SetMovementAnimation(shouldRun ? 1f : 0.45f, false);
    }

    private void BackpedalFromTarget()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            return;

        Vector3 away = transform.position - currentTarget.position;
        away.y = 0f;

        if (away.sqrMagnitude < 0.001f)
            away = -transform.forward;

        agent.isStopped = false;
        agent.speed = backpedalSpeed;
        agent.stoppingDistance = 0f;
        agent.SetDestination(transform.position + away.normalized * 0.9f);

        FaceTarget();
        SetMovementAnimation(0.35f, true);
    }

    private IEnumerator RoarRoutine()
    {
        isBusy = true;
        hasRoared = true;
        StopAgent();
        SetMovementAnimation(0f, false);
        FaceTarget();
        animator.SetTrigger(roarHash);

        yield return new WaitForSeconds(roarDuration);

        EndCurrentAction(false);
    }

    private IEnumerator AttackRoutine()
    {
        isBusy = true;
        StopAgent();
        SetMovementAnimation(0f, false);
        FaceTarget();

        int attackIndex = ChooseAttackIndex();

        if (hasAttackIndexParameter)
            animator.SetInteger(attackIndexHash, attackIndex);

        if (hasAttackParameter)
            animator.SetTrigger(attackHash);

        yield return new WaitForSeconds(Mathf.Max(0f, attackHitDelay));
        TryDamageCurrentTarget();

        float remaining = Mathf.Max(0f, attackDuration - attackHitDelay);

        if (remaining > 0f)
            yield return new WaitForSeconds(remaining);

        nextAttackTime = Time.time + attackCooldown;
        EndCurrentAction(false);
    }

    private void StartActionCoroutine(IEnumerator routine)
    {
        if (currentActionRoutine != null)
            StopCoroutine(currentActionRoutine);

        currentActionRoutine = StartCoroutine(routine);
    }

    private void EndCurrentAction(bool stopRoutine = true)
    {
        if (stopRoutine && currentActionRoutine != null)
        {
            StopCoroutine(currentActionRoutine);
            currentActionRoutine = null;
        }

        isBusy = false;
    }

    private int ChooseAttackIndex()
    {
        int attackIndex = Random.Range(0, 2);

        if (avoidRepeatingAttack && attackIndex == lastAttackIndex)
            attackIndex = (attackIndex + 1) % 2;

        lastAttackIndex = attackIndex;
        return attackIndex;
    }

    private void TryDamageCurrentTarget()
    {
        Vector3 center = hitPoint != null ? hitPoint.position : transform.position + transform.forward * 1.1f;
        Collider[] hits = Physics.OverlapSphere(center, attackHitRadius, attackMask, QueryTriggerInteraction.Ignore);
        Health targetToDamage = null;

        foreach (Collider hit in hits)
        {
            Health targetHealth = hit.GetComponentInParent<Health>();

            if (!IsValidTargetHealth(targetHealth))
                continue;

            if (targetHealth == currentTargetHealth)
            {
                targetToDamage = targetHealth;
                break;
            }

            if (targetToDamage == null)
                targetToDamage = targetHealth;
        }

        if (targetToDamage == null)
            return;

        int finalDamage = attackDamage;
        PlayerDefense defense = targetToDamage.GetComponent<PlayerDefense>();

        if (defense != null)
            defense.TryBlockHit(transform, ref finalDamage);

        if (finalDamage > 0)
            targetToDamage.TakeDamage(finalDamage, "Lion");
    }

    private void HandleDeath()
    {
        StopAllCoroutines();
        currentActionRoutine = null;
        isBusy = true;
        StopAgent();
        SetMovementAnimation(0f, false);

        if (hasDeathParameter)
            animator.SetTrigger(deathHash);
    }

    private void FaceTarget()
    {
        if (currentTarget == null)
            return;

        Vector3 direction = currentTarget.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
    }

    private void StopAgent()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            return;

        agent.isStopped = true;
        agent.ResetPath();
        agent.velocity = Vector3.zero;
    }

    private void SetMovementAnimation(float speed, bool backpedal)
    {
        if (animator == null)
            return;

        if (hasSpeedParameter)
            animator.SetFloat(speedHash, speed, animatorDampTime, Time.deltaTime);

        if (hasBackpedalParameter)
            animator.SetBool(backpedalHash, backpedal);
    }

    private void CacheAnimatorParameters()
    {
        speedHash = Animator.StringToHash(speedParameter);
        backpedalHash = Animator.StringToHash(backpedalParameter);
        attackHash = Animator.StringToHash(attackTriggerParameter);
        attackIndexHash = Animator.StringToHash(attackIndexParameter);
        roarHash = Animator.StringToHash(roarTriggerParameter);
        deathHash = Animator.StringToHash(deathTriggerParameter);

        hasSpeedParameter = HasAnimatorParameter(speedParameter, AnimatorControllerParameterType.Float);
        hasBackpedalParameter = HasAnimatorParameter(backpedalParameter, AnimatorControllerParameterType.Bool);
        hasAttackParameter = HasAnimatorParameter(attackTriggerParameter, AnimatorControllerParameterType.Trigger);
        hasAttackIndexParameter = HasAnimatorParameter(attackIndexParameter, AnimatorControllerParameterType.Int);
        hasRoarParameter = HasAnimatorParameter(roarTriggerParameter, AnimatorControllerParameterType.Trigger);
        hasDeathParameter = HasAnimatorParameter(deathTriggerParameter, AnimatorControllerParameterType.Trigger);
    }

    private bool HasAnimatorParameter(string parameterName, AnimatorControllerParameterType type)
    {
        if (animator == null || string.IsNullOrWhiteSpace(parameterName))
            return false;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == type && parameter.name == parameterName)
                return true;
        }

        return false;
    }
}
