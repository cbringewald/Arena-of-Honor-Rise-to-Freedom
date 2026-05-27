using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ArenaElevatorSpawn : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private string elevatorId;

    [Header("Spawn")]
    [SerializeField] private Transform spawnPoint;

    [Header("Lift")]
    [SerializeField] private Transform platform;
    [SerializeField] private float hiddenDepth = 3f;
    [SerializeField] private float riseDuration = 1.2f;
    [SerializeField] private float holdAfterRise = 0.2f;
    [SerializeField] private bool resetBelowAfterSpawn = false;
    [SerializeField, Min(0.05f)] private float navMeshSnapRadiusAfterLift = 2f;

    private Vector3 platformUpPosition;
    private Vector3 platformDownPosition;
    private Coroutine liftRoutine;
    private readonly List<GameObject> currentPassengers = new List<GameObject>();
    private readonly List<PassengerLock> passengerLocks = new List<PassengerLock>();

    public Transform SpawnPoint => spawnPoint != null ? spawnPoint : transform;
    public string ElevatorId => string.IsNullOrWhiteSpace(elevatorId) ? gameObject.name : elevatorId;

    private void Awake()
    {
        if (platform == null)
            platform = transform;

        platformUpPosition = platform.position;
        platformDownPosition = platformUpPosition + Vector3.down * hiddenDepth;

        if (spawnPoint == null)
            spawnPoint = transform;
    }

    public GameObject Spawn(GameObject prefab, Transform playerTarget)
    {
        if (prefab == null)
            return null;

        if (liftRoutine != null)
            StopCoroutine(liftRoutine);

        platform.position = platformDownPosition;

        GameObject spawned = Instantiate(prefab, SpawnPoint.position, SpawnPoint.rotation);
        currentPassengers.Add(spawned);

        if (platform != null)
        {
            spawned.transform.SetParent(platform, true);
            passengerLocks.Add(new PassengerLock(spawned, platform));
        }

        SetPassengerAIEnabled(spawned, false);
        ConfigureSpawnedEnemy(spawned, playerTarget);

        liftRoutine = StartCoroutine(RiseRoutine());
        return spawned;
    }

    private IEnumerator RiseRoutine()
    {
        float elapsed = 0f;

        while (elapsed < riseDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / riseDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            platform.position = Vector3.Lerp(platformDownPosition, platformUpPosition, t);
            LockPassengersToPlatform();
            yield return null;
        }

        platform.position = platformUpPosition;
        LockPassengersToPlatform();

        if (holdAfterRise > 0f)
            yield return new WaitForSeconds(holdAfterRise);

        foreach (GameObject passenger in currentPassengers)
        {
            if (passenger == null)
                continue;

            passenger.transform.SetParent(null, true);
            SetPassengerAIEnabled(passenger, true);
        }

        currentPassengers.Clear();
        passengerLocks.Clear();

        if (resetBelowAfterSpawn)
            platform.position = platformDownPosition;

        liftRoutine = null;
    }

    private void LockPassengersToPlatform()
    {
        if (platform == null)
            return;

        foreach (PassengerLock passengerLock in passengerLocks)
        {
            if (passengerLock.passenger == null)
                continue;

            passengerLock.passenger.transform.position = platform.TransformPoint(passengerLock.localPosition);
            passengerLock.passenger.transform.rotation = platform.rotation * passengerLock.localRotation;
        }
    }

    private static void ConfigureSpawnedEnemy(GameObject spawned, Transform playerTarget)
    {
        if (spawned == null || playerTarget == null)
            return;

        BotAI bot = spawned.GetComponent<BotAI>();

        if (bot == null)
            bot = spawned.GetComponentInChildren<BotAI>();

        if (bot != null)
        {
            bot.SetPlayer(playerTarget);
            return;
        }

        LionCombatController lion = spawned.GetComponent<LionCombatController>();

        if (lion == null)
            lion = spawned.GetComponentInChildren<LionCombatController>();

        if (lion != null)
            lion.SetPlayer(playerTarget);
    }

    private void SetPassengerAIEnabled(GameObject passenger, bool enabled)
    {
        if (passenger == null)
            return;

        NavMeshAgent agent = passenger.GetComponent<NavMeshAgent>();

        if (agent == null)
            agent = passenger.GetComponentInChildren<NavMeshAgent>();

        if (agent != null)
        {
            agent.enabled = enabled;

            if (enabled)
            {
                if (NavMesh.SamplePosition(agent.transform.position, out NavMeshHit hit, navMeshSnapRadiusAfterLift, NavMesh.AllAreas))
                {
                    Vector3 agentOffset = hit.position - agent.transform.position;
                    passenger.transform.position += agentOffset;
                    agent.Warp(hit.position);
                }
                else
                {
                    agent.enabled = false;
                    enabled = false;

                    Debug.LogWarning("ArenaElevatorSpawn: el pasajero " + passenger.name +
                        " ha terminado fuera del NavMesh en " + passenger.transform.position +
                        ". Mueve el spawn del ascensor o bakea NavMesh sobre esa zona.");
                }
            }
        }

        BotAI bot = passenger.GetComponent<BotAI>();

        if (bot == null)
            bot = passenger.GetComponentInChildren<BotAI>();

        if (bot != null)
            bot.enabled = enabled;

        LionCombatController lion = passenger.GetComponent<LionCombatController>();

        if (lion == null)
            lion = passenger.GetComponentInChildren<LionCombatController>();

        if (lion != null)
            lion.enabled = enabled;
    }

    private struct PassengerLock
    {
        public readonly GameObject passenger;
        public readonly Vector3 localPosition;
        public readonly Quaternion localRotation;

        public PassengerLock(GameObject passenger, Transform platform)
        {
            this.passenger = passenger;
            localPosition = platform.InverseTransformPoint(passenger.transform.position);
            localRotation = Quaternion.Inverse(platform.rotation) * passenger.transform.rotation;
        }
    }
}
