using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

public enum RoundElevatorSpawnTrigger
{
    RoundStart,
    AfterDelay,
    AfterEnemyDeaths,
    WhenNoEnemiesAlive
}

[System.Serializable]
public class RoundElevatorSpawnData
{
    public bool enabled = true;
    public string label = "Ascensor";

    [Header("Cinematic")]
    public ElevatorCinematic elevatorCinematic;
    public bool waitCinematicBeforeSpawn = true;
    public float cinematicDuration = 4f;
    public bool cinematicAlreadyIncludesElevatorLift = false;

    public RoundElevatorSpawnTrigger trigger = RoundElevatorSpawnTrigger.RoundStart;
    [Min(0f)] public float eventStartDelay = 0f;
    [Min(0f)] public float delaySeconds = 0f;
    [Min(1)] public int enemyDeathsRequired = 1;

    [Header("Direct Elevator Setup")]
    public Transform elevatorPlatform;
    public Transform spawnPoint;
    public float hiddenDepth = 3f;
    public float riseDuration = 1.2f;
    public float holdAfterRise = 0.2f;
    public bool resetBelowAfterSpawn = false;
    public bool keepPassengersLockedDuringLift = true;
    [Min(0.05f)] public float navMeshSnapRadiusAfterLift = 2f;

    [Header("Prefabs")]
    public GameObject[] possiblePrefabs;
    public int count = 1;

    [Header("Optional Legacy Elevator Component")]
    public ArenaElevatorSpawn elevatorSpawn;
    public int elevatorIndex = -1;

    [Header("Stats")]
    public int healthOverride = 0;
}

public struct ElevatorPassengerLock
{
    public GameObject passenger;
    public Vector3 localPosition;
    public Quaternion localRotation;

    public ElevatorPassengerLock(GameObject passenger, Transform platform)
    {
        this.passenger = passenger;
        localPosition = platform.InverseTransformPoint(passenger.transform.position);
        localRotation = Quaternion.Inverse(platform.rotation) * passenger.transform.rotation;
    }
}

[System.Serializable]
public class RoundData
{
    [Header("Round Info")]
    public string roundName = "Ronda";
    [TextArea] public string startMessage;

    [Header("Enemies")]
    public GameObject enemyPrefab;
    public GameObject[] enemyPrefabs;
    public bool randomizeEnemyPrefabs = false;
    public int enemyCount = 1;
    public int enemyHealth = 3;

    [Header("Elevator Spawns")]
    public RoundElevatorSpawnData[] elevatorSpawns;

    [Header("Rewards")]
    public int fameReward = 30;
    public int noDamageFameBonus = 10;
    public int healthReward = 1;
    public float staminaReward = 12f;
    public bool fullRestoreAfterRound;

    [Header("Unlocked Weapons")]
    public GameObject[] unlockedWeaponPickups;

    [Header("Unlocked Shields")]
    public GameObject[] unlockedShieldPickups;
}

public class RoundManager : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip victorySound;

    [Header("Player")]
    [SerializeField] private Transform player;
    [SerializeField] private CharacterController playerController;
    [SerializeField] private PlayerCombat playerCombat;
    [SerializeField] private PlayerBlock playerBlock;
    [SerializeField] private Health playerHealth;
    [SerializeField] private Stamina playerStamina;
    [SerializeField] private PlayerCelebration playerCelebration;
    [SerializeField] private Transform playerSpawnPoint;
    [SerializeField] private CameraOrbitCM playerCamera;

    [Header("Enemy Spawns")]
    [SerializeField] private Transform[] enemySpawnPoints;
    [SerializeField] private ArenaElevatorSpawn[] arenaElevators;
    [SerializeField] private bool autoFindArenaElevators = true;
    [SerializeField] private bool useDeathCameraForEnemyKills = false;
    [SerializeField] private bool snapRegularSpawnsToNavMesh = true;
    [SerializeField] private bool snapElevatorSpawnsToNavMesh = false;
    [SerializeField] private float regularSpawnNavMeshRadius = 2f;

    [Header("Door")]
    [SerializeField] private PortonArena arenaDoor;
    [SerializeField] private EnterArenaTrigger enterArenaTrigger;

    [Header("Weapon Pickups")]
    [SerializeField] private WeaponPickup[] weaponPickups;
    [SerializeField] private ShieldPickup[] shieldPickups;
    [SerializeField] private bool onlyUseCurrentRoundWeaponList = true;
    [SerializeField] private bool autoFindWeaponPickups = true;

    [Header("Rounds")]
    [SerializeField] private List<RoundData> rounds = new List<RoundData>();

    [Header("Fame")]
    [SerializeField] private int fame = 0;
    [SerializeField] private int fameGoal = 100;
    [SerializeField] private bool winWhenFameGoalReached = true;
    [SerializeField] private int contenderFame = 25;
    [SerializeField] private int championFame = 70;

    [Header("Default Round Rewards")]
    [SerializeField] private int defaultNoDamageFameBonus = 10;
    [SerializeField] private int defaultHealthReward = 1;
    [SerializeField] private float defaultStaminaReward = 12f;
    [SerializeField, Min(0f)] private float maxStaminaRewardPerRound = 18f;

    [Header("Enemy Scaling")]
    [SerializeField] private bool scaleEnemiesByRound = true;
    [SerializeField, Min(0f)] private float enemyDamageIncreasePerRound = 0.12f;
    [SerializeField, Min(0f)] private float enemyAttackSpeedIncreasePerRound = 0.08f;
    [SerializeField, Min(0f)] private float enemyMoveSpeedIncreasePerRound = 0.035f;

    [Header("UI")]
    [SerializeField] private TMP_Text roundText;
    [SerializeField] private TMP_Text fameText;

    [Header("Old Message UI Fallback")]
    [SerializeField] private TMP_Text messageText;

    [Header("New Round Message UI")]
    [SerializeField] private RoundMessageUI roundMessageUI;

    [Header("Timing")]
    [SerializeField] private float messageDuration = 3f;
    [SerializeField] private float nextRoundDelay = 4f;

    [Header("Final Victory")]
    [SerializeField] private FadeController fadeController;
    [SerializeField] private PlayableDirector finalVictoryCinematic;
    [SerializeField] private bool waitFinalVictoryCinematic = true;
    [SerializeField, Min(0f)] private float finalVictoryCinematicDuration = 7f;
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private float timeBeforeFade = 3f;
    [SerializeField] private float timeBeforeLoadMenu = 2f;
    [SerializeField] private bool keepPlayerControlOnVictory = true;
    [SerializeField] private bool loadMenuAfterVictory = false;

    private int currentRoundIndex;
    private bool roundEnded;
    private bool finalVictoryStarted;
    private bool playerTookDamageThisRound;
    private int enemiesKilledThisRound;
    private int flawlessRounds;

    private Coroutine messageCoroutine;

    private readonly List<Health> aliveEnemies = new List<Health>();
    private readonly List<GameObject> spawnedEnemies = new List<GameObject>();
    private readonly List<Coroutine> elevatorSpawnRoutines = new List<Coroutine>();
    private readonly List<Coroutine> elevatorLiftRoutines = new List<Coroutine>();
    private readonly HashSet<int> startedElevatorSpawnEvents = new HashSet<int>();
    private readonly HashSet<int> completedElevatorSpawnEvents = new HashSet<int>();

    public int CurrentRound => currentRoundIndex + 1;
    public int Fame => fame;
    public string FameTitle => GetFameTitle();

    private void Start()
    {
        ResolveReferences();

        if (messageText != null)
            messageText.gameObject.SetActive(false);

        if (roundMessageUI != null)
            roundMessageUI.HideInstant();

        ResetPlayerToSpawn();
        StartRound();
        UpdateUI();
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.OnDamaged -= OnPlayerDamaged;
    }

    private void ResolveReferences()
    {
        if (playerController == null && player != null)
            playerController = player.GetComponent<CharacterController>();

        if (playerCombat == null && player != null)
            playerCombat = player.GetComponent<PlayerCombat>();

        if (playerBlock == null && player != null)
            playerBlock = player.GetComponent<PlayerBlock>();

        if (playerHealth == null && player != null)
            playerHealth = player.GetComponent<Health>();

        if (playerStamina == null && player != null)
            playerStamina = player.GetComponent<Stamina>();

        if (playerCelebration == null && player != null)
            playerCelebration = player.GetComponent<PlayerCelebration>();

        if (playerCelebration == null && player != null)
            playerCelebration = player.gameObject.AddComponent<PlayerCelebration>();

        if (playerCamera == null)
            playerCamera = FindFirstObjectByType<CameraOrbitCM>();

        if (playerHealth != null)
            playerHealth.OnDamaged += OnPlayerDamaged;

        if (autoFindWeaponPickups && (weaponPickups == null || weaponPickups.Length == 0))
            weaponPickups = FindObjectsByType<WeaponPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        if (autoFindWeaponPickups && (shieldPickups == null || shieldPickups.Length == 0))
            shieldPickups = FindObjectsByType<ShieldPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        if (autoFindArenaElevators && (arenaElevators == null || arenaElevators.Length == 0))
            arenaElevators = FindObjectsByType<ArenaElevatorSpawn>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        if (enterArenaTrigger == null)
            enterArenaTrigger = FindFirstObjectByType<EnterArenaTrigger>(FindObjectsInactive.Include);
    }

    private void StartRound()
    {
        if (currentRoundIndex < 0 || currentRoundIndex >= rounds.Count)
        {
            StartCoroutine(FinalVictoryRoutine());
            return;
        }

        roundEnded = false;
        playerTookDamageThisRound = false;
        enemiesKilledThisRound = 0;
        startedElevatorSpawnEvents.Clear();
        completedElevatorSpawnEvents.Clear();
        StopElevatorSpawnRoutines();

        if (playerCelebration != null)
            playerCelebration.LockCelebration();

        if (arenaDoor != null)
            arenaDoor.CloseDoor();

        ResetPlayerToSpawn();
        ResetPlayerWeapon();
        ResetPlayerShield();
        ResetWeaponPickups();
        ResetShieldPickups();
        SetupWeaponUnlocks();
        ResetEnterArenaTrigger();
        SpawnEnemiesForRound();
        TryTriggerQueuedElevatorSpawns();
        UpdateUI();

        RoundData data = rounds[currentRoundIndex];
        string messageToShow = BuildRoundStartMessage(data);
        ShowMessageTimed(messageToShow);
    }

    private string BuildRoundStartMessage(RoundData data)
    {
        if (data != null && !string.IsNullOrWhiteSpace(data.startMessage))
            return data.startMessage;

        if (currentRoundIndex >= rounds.Count - 1)
            return "RONDA FINAL\nLucha por tu libertad";

        return data != null ? data.roundName + "\nPreparate!" : "RONDA\nPreparate!";
    }

    private void ResetPlayerWeapon()
    {
        if (playerCombat != null)
            playerCombat.UnequipWeapon();
    }

    private void ResetPlayerShield()
    {
        if (playerBlock != null)
            playerBlock.UnequipShield();
    }

    private void ResetWeaponPickups()
    {
        if (weaponPickups == null)
            return;

        foreach (WeaponPickup pickup in weaponPickups)
        {
            if (pickup != null)
                pickup.ResetPickup();
        }
    }

    private void ResetShieldPickups()
    {
        if (shieldPickups == null)
            return;

        foreach (ShieldPickup pickup in shieldPickups)
        {
            if (pickup != null)
                pickup.ResetPickup();
        }
    }

    private void SetupWeaponUnlocks()
    {
        foreach (RoundData round in rounds)
        {
            if (round.unlockedWeaponPickups != null)
            {
                foreach (GameObject weapon in round.unlockedWeaponPickups)
                {
                    if (weapon != null)
                        weapon.SetActive(false);
                }
            }

            if (round.unlockedShieldPickups != null)
            {
                foreach (GameObject shield in round.unlockedShieldPickups)
                {
                    if (shield != null)
                        shield.SetActive(false);
                }
            }
        }

        int firstRoundToEnable = onlyUseCurrentRoundWeaponList ? currentRoundIndex : 0;

        for (int i = firstRoundToEnable; i <= currentRoundIndex && i < rounds.Count; i++)
        {
            if (rounds[i].unlockedWeaponPickups != null)
            {
                foreach (GameObject weapon in rounds[i].unlockedWeaponPickups)
                {
                    if (weapon != null)
                        weapon.SetActive(true);
                }
            }

            if (rounds[i].unlockedShieldPickups != null)
            {
                foreach (GameObject shield in rounds[i].unlockedShieldPickups)
                {
                    if (shield != null)
                        shield.SetActive(true);
                }
            }
        }
    }

    private void ResetEnterArenaTrigger()
    {
        if (enterArenaTrigger != null)
            enterArenaTrigger.ResetTrigger();
    }

    private void SpawnEnemiesForRound()
    {
        RoundData data = rounds[currentRoundIndex];

        if (!HasAnyEnemyPrefab(data))
        {
            Debug.LogError("RoundManager: falta EnemyPrefab, EnemyPrefabs o Elevator Prefabs en " + data.roundName);
            return;
        }

        ClearSpawnedEnemies();

        SpawnRegularEnemies(data);
        ScheduleElevatorEnemies(data);

        Debug.Log(data.roundName + " creada con " + aliveEnemies.Count + " enemigo(s).");

        if (aliveEnemies.Count <= 0 && AreAllElevatorSpawnEventsComplete(data))
        {
            Debug.LogWarning("La ronda no tiene enemigos vivos registrados. Se completara automaticamente.");
            EndRound();
        }
    }

    private void SpawnRegularEnemies(RoundData data)
    {
        if (!HasRegularEnemyPrefab(data))
            return;

        if (enemySpawnPoints == null || enemySpawnPoints.Length == 0)
        {
            Debug.LogError("RoundManager: faltan EnemySpawnPoints.");
            return;
        }

        for (int i = 0; i < data.enemyCount; i++)
        {
            Transform spawnPoint = enemySpawnPoints[i % enemySpawnPoints.Length];
            GameObject prefab = GetEnemyPrefab(data, i);

            if (prefab == null)
                continue;

            GameObject enemy = SpawnEnemyPrefab(prefab, spawnPoint.position, spawnPoint.rotation, snapRegularSpawnsToNavMesh);
            RegisterSpawnedEnemy(enemy, data.enemyHealth);
        }
    }

    private void ScheduleElevatorEnemies(RoundData data)
    {
        if (data.elevatorSpawns == null || data.elevatorSpawns.Length == 0)
            return;

        for (int i = 0; i < data.elevatorSpawns.Length; i++)
        {
            RoundElevatorSpawnData spawnData = data.elevatorSpawns[i];

            if (!IsElevatorSpawnEnabled(spawnData))
            {
                startedElevatorSpawnEvents.Add(i);
                completedElevatorSpawnEvents.Add(i);
                continue;
            }

            if (spawnData.trigger == RoundElevatorSpawnTrigger.RoundStart)
            {
                SpawnElevatorEvent(data, i);
                continue;
            }

            if (spawnData.trigger == RoundElevatorSpawnTrigger.AfterDelay)
            {
                Coroutine routine = StartCoroutine(SpawnElevatorAfterDelay(data, i, spawnData.delaySeconds));
                elevatorSpawnRoutines.Add(routine);
            }
        }
    }

    private IEnumerator SpawnElevatorAfterDelay(RoundData data, int eventIndex, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        if (!roundEnded && !finalVictoryStarted && currentRoundIndex < rounds.Count && rounds[currentRoundIndex] == data)
        {
            SpawnElevatorEvent(data, eventIndex);
            TryEndRoundIfReady(data);
        }
    }

    private void TryTriggerQueuedElevatorSpawns()
    {
        if (currentRoundIndex < 0 || currentRoundIndex >= rounds.Count)
            return;

        RoundData data = rounds[currentRoundIndex];

        if (data.elevatorSpawns == null)
            return;

        for (int i = 0; i < data.elevatorSpawns.Length; i++)
        {
            if (completedElevatorSpawnEvents.Contains(i))
                continue;

            RoundElevatorSpawnData spawnData = data.elevatorSpawns[i];

            if (!IsElevatorSpawnEnabled(spawnData))
            {
                startedElevatorSpawnEvents.Add(i);
                completedElevatorSpawnEvents.Add(i);
                continue;
            }

            if (spawnData.trigger == RoundElevatorSpawnTrigger.AfterEnemyDeaths &&
                enemiesKilledThisRound >= Mathf.Max(1, spawnData.enemyDeathsRequired))
            {
                SpawnElevatorEvent(data, i);
            }
            else if (spawnData.trigger == RoundElevatorSpawnTrigger.WhenNoEnemiesAlive &&
                aliveEnemies.Count <= 0)
            {
                SpawnElevatorEvent(data, i);
            }
        }
    }

    private void SpawnElevatorEvent(RoundData data, int eventIndex)
    {
        if (data == null || data.elevatorSpawns == null)
            return;

        if (eventIndex < 0 || eventIndex >= data.elevatorSpawns.Length)
            return;

        if (startedElevatorSpawnEvents.Contains(eventIndex))
            return;

        RoundElevatorSpawnData spawnData = data.elevatorSpawns[eventIndex];
        startedElevatorSpawnEvents.Add(eventIndex);

        if (!IsElevatorSpawnEnabled(spawnData))
        {
            completedElevatorSpawnEvents.Add(eventIndex);
            return;
        }

        // Cinemática justo cuando empieza el evento de ascensor
        ArenaElevatorSpawn elevatorComponent = GetElevator(spawnData);

        Coroutine routine = StartCoroutine(SpawnElevatorEventRoutine(data, eventIndex, spawnData, elevatorComponent));
        elevatorLiftRoutines.Add(routine);
    }

    private void SpawnWithElevatorComponent(RoundData data, RoundElevatorSpawnData spawnData, ArenaElevatorSpawn elevator)
    {
        int count = Mathf.Max(1, spawnData.count);

        for (int i = 0; i < count; i++)
        {
            GameObject prefab = GetElevatorEnemyPrefab(spawnData, data);

            if (prefab == null)
                continue;

            GameObject enemy = elevator.Spawn(prefab, player);
            int health = spawnData.healthOverride > 0 ? spawnData.healthOverride : data.enemyHealth;
            RegisterSpawnedEnemy(enemy, health);
        }
    }

    private IEnumerator SpawnElevatorEventRoutine(RoundData data, int eventIndex, RoundElevatorSpawnData spawnData, ArenaElevatorSpawn elevatorComponent)
    {
        Transform capturedPlatform = spawnData.elevatorPlatform;
        Transform capturedSpawn = spawnData.spawnPoint != null ? spawnData.spawnPoint : spawnData.elevatorPlatform;
        bool hasCapturedElevatorPose = capturedPlatform != null && capturedSpawn != null;
        Vector3 platformUpPosition = hasCapturedElevatorPose ? capturedPlatform.position : Vector3.zero;
        Vector3 spawnLocalPosition = hasCapturedElevatorPose ? capturedPlatform.InverseTransformPoint(capturedSpawn.position) : Vector3.zero;
        Quaternion spawnLocalRotation = hasCapturedElevatorPose ? Quaternion.Inverse(capturedPlatform.rotation) * capturedSpawn.rotation : Quaternion.identity;

        if (spawnData.eventStartDelay > 0f)
            yield return new WaitForSeconds(spawnData.eventStartDelay);

        if (roundEnded || finalVictoryStarted || currentRoundIndex >= rounds.Count || rounds[currentRoundIndex] != data)
            yield break;

        if (spawnData.elevatorCinematic != null)
        {
            if (spawnData.waitCinematicBeforeSpawn)
                yield return StartCoroutine(spawnData.elevatorCinematic.PlayAndWait(spawnData.cinematicDuration));
            else
                spawnData.elevatorCinematic.PlayCinematic();
        }

        if (spawnData.cinematicAlreadyIncludesElevatorLift)
        {
            SpawnEnemiesAtElevatorSpawnPoint(data, spawnData);
            CompleteElevatorSpawnEvent(data, eventIndex);
            TryEndRoundIfReady(data);
            yield break;
        }

        if (elevatorComponent != null)
        {
            SpawnWithElevatorComponent(data, spawnData, elevatorComponent);
        }
        else
        {
            yield return StartCoroutine(SpawnWithDirectElevatorRoutine(
                data,
                spawnData,
                hasCapturedElevatorPose,
                platformUpPosition,
                spawnLocalPosition,
                spawnLocalRotation));
        }

        CompleteElevatorSpawnEvent(data, eventIndex);
        TryEndRoundIfReady(data);
    }

    private void CompleteElevatorSpawnEvent(RoundData data, int eventIndex)
    {
        completedElevatorSpawnEvents.Add(eventIndex);
    }

    private GameObject SpawnEnemyPrefab(GameObject prefab, Vector3 position, Quaternion rotation, bool snapToNavMesh)
    {
        GameObject enemy = Instantiate(prefab, position, rotation);

        if (snapToNavMesh)
            PlaceEnemyOnNavMesh(enemy, position);

        return enemy;
    }

    private void PlaceEnemyOnNavMesh(GameObject enemy, Vector3 desiredPosition)
    {
        if (enemy == null)
            return;

        UnityEngine.AI.NavMeshAgent agent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();

        if (agent == null)
            agent = enemy.GetComponentInChildren<UnityEngine.AI.NavMeshAgent>();

        if (agent == null)
            return;

        bool wasEnabled = agent.enabled;

        if (!wasEnabled)
            agent.enabled = true;

        if (UnityEngine.AI.NavMesh.SamplePosition(desiredPosition, out UnityEngine.AI.NavMeshHit hit, regularSpawnNavMeshRadius, UnityEngine.AI.NavMesh.AllAreas))
        {
            Vector3 agentOffset = hit.position - agent.transform.position;
            enemy.transform.position += agentOffset;

            if (agent.enabled)
                agent.Warp(hit.position);
        }
        else
        {
            Debug.LogWarning("RoundManager: no hay NavMesh cerca del spawn " + desiredPosition + " para " + enemy.name);
        }

        if (!wasEnabled)
            agent.enabled = false;
    }

    private void SpawnEnemiesAtElevatorSpawnPoint(RoundData data, RoundElevatorSpawnData spawnData)
    {
        Transform spawn = spawnData.spawnPoint != null ? spawnData.spawnPoint : spawnData.elevatorPlatform;

        if (spawn == null)
        {
            Debug.LogWarning("RoundManager: falta spawnPoint/elevatorPlatform para spawnear tras cinemática en " + spawnData.label);
            return;
        }

        int count = Mathf.Max(1, spawnData.count);

        for (int i = 0; i < count; i++)
        {
            GameObject prefab = GetElevatorEnemyPrefab(spawnData, data);

            if (prefab == null)
                continue;

            GameObject enemy = SpawnEnemyPrefab(prefab, spawn.position, spawn.rotation, snapElevatorSpawnsToNavMesh);

            int health = spawnData.healthOverride > 0 ? spawnData.healthOverride : data.enemyHealth;
            RegisterSpawnedEnemy(enemy, health);
        }
    }

    private IEnumerator SpawnWithDirectElevatorRoutine(
        RoundData data,
        RoundElevatorSpawnData spawnData,
        bool useCapturedElevatorPose,
        Vector3 capturedPlatformUpPosition,
        Vector3 capturedSpawnLocalPosition,
        Quaternion capturedSpawnLocalRotation)
    {
        Transform platform = spawnData.elevatorPlatform;
        Transform spawn = spawnData.spawnPoint != null ? spawnData.spawnPoint : spawnData.elevatorPlatform;

        if (platform == null || spawn == null)
        {
            Debug.LogWarning("RoundManager: falta elevatorPlatform o spawnPoint en " + spawnData.label);
            yield break;
        }

        Vector3 platformUpPosition = useCapturedElevatorPose ? capturedPlatformUpPosition : platform.position;
        Vector3 platformDownPosition = platformUpPosition + Vector3.down * spawnData.hiddenDepth;
        Vector3 spawnLocalPosition = useCapturedElevatorPose ? capturedSpawnLocalPosition : platform.InverseTransformPoint(spawn.position);
        Quaternion spawnLocalRotation = useCapturedElevatorPose ? capturedSpawnLocalRotation : Quaternion.Inverse(platform.rotation) * spawn.rotation;
        bool syncDetachedSpawnPoint = spawn != platform && !spawn.IsChildOf(platform);
        List<GameObject> passengers = new List<GameObject>();
        List<ElevatorPassengerLock> passengerLocks = new List<ElevatorPassengerLock>();

        platform.position = platformDownPosition;
        SyncSpawnPointWithPlatform(spawn, platform, spawnLocalPosition, spawnLocalRotation, syncDetachedSpawnPoint);

        int count = Mathf.Max(1, spawnData.count);

        for (int i = 0; i < count; i++)
        {
            GameObject prefab = GetElevatorEnemyPrefab(spawnData, data);

            if (prefab == null)
                continue;

            Vector3 spawnPosition = platform.TransformPoint(spawnLocalPosition);
            Quaternion spawnRotation = platform.rotation * spawnLocalRotation;
            GameObject enemy = Instantiate(prefab, spawnPosition, spawnRotation);
            enemy.transform.SetParent(platform, true);
            passengers.Add(enemy);
            passengerLocks.Add(new ElevatorPassengerLock(enemy, platform));

            SetPassengerCombatEnabled(enemy, false, spawnData.navMeshSnapRadiusAfterLift);

            int health = spawnData.healthOverride > 0 ? spawnData.healthOverride : data.enemyHealth;
            RegisterSpawnedEnemy(enemy, health);
        }

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, spawnData.riseDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = Mathf.SmoothStep(0f, 1f, t);

            platform.position = Vector3.Lerp(platformDownPosition, platformUpPosition, t);
            SyncSpawnPointWithPlatform(spawn, platform, spawnLocalPosition, spawnLocalRotation, syncDetachedSpawnPoint);
            LockPassengersToPlatform(platform, passengerLocks, spawnData.keepPassengersLockedDuringLift);
            yield return null;
        }

        platform.position = platformUpPosition;
        SyncSpawnPointWithPlatform(spawn, platform, spawnLocalPosition, spawnLocalRotation, syncDetachedSpawnPoint);
        LockPassengersToPlatform(platform, passengerLocks, spawnData.keepPassengersLockedDuringLift);

        if (spawnData.holdAfterRise > 0f)
            yield return new WaitForSeconds(spawnData.holdAfterRise);

        foreach (GameObject passenger in passengers)
        {
            if (passenger == null)
                continue;

            passenger.transform.SetParent(null, true);
            SetPassengerCombatEnabled(passenger, true, spawnData.navMeshSnapRadiusAfterLift);
        }

        if (spawnData.resetBelowAfterSpawn)
        {
            platform.position = platformDownPosition;
            SyncSpawnPointWithPlatform(spawn, platform, spawnLocalPosition, spawnLocalRotation, syncDetachedSpawnPoint);
        }
    }

    private static void SyncSpawnPointWithPlatform(
        Transform spawn,
        Transform platform,
        Vector3 localPosition,
        Quaternion localRotation,
        bool shouldSync)
    {
        if (!shouldSync || spawn == null || platform == null)
            return;

        spawn.position = platform.TransformPoint(localPosition);
        spawn.rotation = platform.rotation * localRotation;
    }

    private static void LockPassengersToPlatform(Transform platform, List<ElevatorPassengerLock> passengers, bool shouldLock)
    {
        if (!shouldLock || platform == null || passengers == null)
            return;

        foreach (ElevatorPassengerLock passengerLock in passengers)
        {
            if (passengerLock.passenger == null)
                continue;

            Transform passengerTransform = passengerLock.passenger.transform;
            passengerTransform.position = platform.TransformPoint(passengerLock.localPosition);
            passengerTransform.rotation = platform.rotation * passengerLock.localRotation;
        }
    }

    private bool IsElevatorSpawnEnabled(RoundElevatorSpawnData spawnData)
    {
        return spawnData != null && spawnData.enabled && HasAnyElevatorPrefab(spawnData);
    }

    private static void SetPassengerCombatEnabled(GameObject passenger, bool enabled, float navMeshSnapRadius)
    {
        if (passenger == null)
            return;

        bool canEnableCombat = enabled;
        UnityEngine.AI.NavMeshAgent agent = passenger.GetComponent<UnityEngine.AI.NavMeshAgent>();

        if (agent == null)
            agent = passenger.GetComponentInChildren<UnityEngine.AI.NavMeshAgent>();

        if (agent != null)
        {
            if (!enabled)
            {
                agent.enabled = false;
            }
            else if (UnityEngine.AI.NavMesh.SamplePosition(
                agent.transform.position,
                out UnityEngine.AI.NavMeshHit hit,
                Mathf.Max(0.05f, navMeshSnapRadius),
                UnityEngine.AI.NavMesh.AllAreas))
            {
                Vector3 agentOffset = hit.position - agent.transform.position;
                passenger.transform.position += agentOffset;
                agent.enabled = true;
                agent.Warp(hit.position);
            }
            else
            {
                agent.enabled = false;
                canEnableCombat = false;

                Debug.LogWarning("RoundManager: el pasajero " + passenger.name +
                    " ha terminado fuera del NavMesh en " + passenger.transform.position +
                    ". Mueve el spawn del ascensor o bakea NavMesh sobre esa zona.");
            }
        }

        BotAI bot = passenger.GetComponent<BotAI>();

        if (bot == null)
            bot = passenger.GetComponentInChildren<BotAI>();

        if (bot != null)
            bot.enabled = canEnableCombat;

        LionCombatController lion = passenger.GetComponent<LionCombatController>();

        if (lion == null)
            lion = passenger.GetComponentInChildren<LionCombatController>();

        if (lion != null)
            lion.enabled = canEnableCombat;
    }

    private bool AreAllElevatorSpawnEventsComplete(RoundData data)
    {
        if (data == null || data.elevatorSpawns == null || data.elevatorSpawns.Length == 0)
            return true;

        for (int i = 0; i < data.elevatorSpawns.Length; i++)
        {
            if (!completedElevatorSpawnEvents.Contains(i))
                return false;
        }

        return true;
    }

    private void RegisterSpawnedEnemy(GameObject enemy, int healthValue)
    {
        if (enemy == null)
            return;

        spawnedEnemies.Add(enemy);

        Health health = enemy.GetComponent<Health>();

        if (health == null)
            health = enemy.GetComponentInChildren<Health>();

        if (health == null)
        {
            Debug.LogWarning("El prefab instanciado no tiene Health y no contara para terminar la ronda: " + enemy.name);
            return;
        }

        health.SetMaxHealth(healthValue, true);
        health.SetUseDeathCamera(useDeathCameraForEnemyKills);
        health.OnDied += OnEnemyDied;
        aliveEnemies.Add(health);

        BotAI bot = enemy.GetComponent<BotAI>();

        if (bot == null)
            bot = enemy.GetComponentInChildren<BotAI>();

        if (bot != null)
        {
            bot.SetPlayer(player);
            ApplyEnemyRoundScaling(bot, null);
            return;
        }

        LionCombatController lion = enemy.GetComponent<LionCombatController>();

        if (lion == null)
            lion = enemy.GetComponentInChildren<LionCombatController>();

        if (lion != null)
        {
            lion.SetPlayer(player);
            ApplyEnemyRoundScaling(null, lion);
        }
        else
            Debug.LogWarning("El enemigo instanciado no tiene BotAI ni LionCombatController. Si es animal, anade una IA compatible o dejalo como objetivo pasivo.");
    }

    private void ApplyEnemyRoundScaling(BotAI bot, LionCombatController lion)
    {
        if (!scaleEnemiesByRound)
            return;

        int completedRounds = Mathf.Max(0, currentRoundIndex);
        float damageMultiplier = 1f + enemyDamageIncreasePerRound * completedRounds;
        float attackSpeedMultiplier = 1f + enemyAttackSpeedIncreasePerRound * completedRounds;
        float movementSpeedMultiplier = 1f + enemyMoveSpeedIncreasePerRound * completedRounds;

        if (bot != null)
            bot.ApplyRoundScaling(damageMultiplier, attackSpeedMultiplier, movementSpeedMultiplier);

        if (lion != null)
            lion.ApplyRoundScaling(damageMultiplier, attackSpeedMultiplier, movementSpeedMultiplier);
    }

    private bool HasAnyEnemyPrefab(RoundData data)
    {
        if (data == null)
            return false;

        if (data.enemyPrefab != null)
            return true;

        if (data.enemyPrefabs != null)
        {
            foreach (GameObject prefab in data.enemyPrefabs)
            {
                if (prefab != null)
                    return true;
            }
        }

        if (data.elevatorSpawns == null)
            return false;

        foreach (RoundElevatorSpawnData spawnData in data.elevatorSpawns)
        {
            if (HasAnyElevatorPrefab(spawnData))
                return true;
        }

        return false;
    }

    private bool HasRegularEnemyPrefab(RoundData data)
    {
        if (data == null)
            return false;

        if (data.enemyPrefab != null)
            return true;

        if (data.enemyPrefabs == null)
            return false;

        foreach (GameObject prefab in data.enemyPrefabs)
        {
            if (prefab != null)
                return true;
        }

        return false;
    }

    private bool HasAnyElevatorPrefab(RoundElevatorSpawnData spawnData)
    {
        if (spawnData == null || spawnData.possiblePrefabs == null)
            return false;

        foreach (GameObject prefab in spawnData.possiblePrefabs)
        {
            if (prefab != null)
                return true;
        }

        return false;
    }

    private GameObject GetEnemyPrefab(RoundData data, int spawnIndex)
    {
        List<GameObject> validPrefabs = GetValidPrefabs(data.enemyPrefabs);

        if (validPrefabs.Count > 0)
        {
            if (data.randomizeEnemyPrefabs)
                return validPrefabs[Random.Range(0, validPrefabs.Count)];

            return validPrefabs[Mathf.Abs(spawnIndex) % validPrefabs.Count];
        }

        return data.enemyPrefab;
    }

    private GameObject GetElevatorEnemyPrefab(RoundElevatorSpawnData spawnData, RoundData data)
    {
        GameObject randomPrefab = GetRandomPrefab(spawnData.possiblePrefabs);
        return randomPrefab != null ? randomPrefab : GetEnemyPrefab(data, 0);
    }

    private GameObject GetRandomPrefab(GameObject[] prefabs)
    {
        if (prefabs == null || prefabs.Length == 0)
            return null;

        List<GameObject> validPrefabs = new List<GameObject>();

        foreach (GameObject prefab in prefabs)
        {
            if (prefab != null)
                validPrefabs.Add(prefab);
        }

        if (validPrefabs.Count <= 0)
            return null;

        return validPrefabs[Random.Range(0, validPrefabs.Count)];
    }

    private List<GameObject> GetValidPrefabs(GameObject[] prefabs)
    {
        List<GameObject> validPrefabs = new List<GameObject>();

        if (prefabs == null)
            return validPrefabs;

        foreach (GameObject prefab in prefabs)
        {
            if (prefab != null)
                validPrefabs.Add(prefab);
        }

        return validPrefabs;
    }

    private ArenaElevatorSpawn GetElevator(RoundElevatorSpawnData spawnData)
    {
        if (spawnData != null && spawnData.elevatorSpawn != null)
            return spawnData.elevatorSpawn;

        if (arenaElevators == null || arenaElevators.Length == 0)
            return null;

        int configuredIndex = spawnData != null ? spawnData.elevatorIndex : -1;

        if (configuredIndex >= 0 && configuredIndex < arenaElevators.Length)
            return arenaElevators[configuredIndex];

        return arenaElevators[Random.Range(0, arenaElevators.Length)];
    }

    private void OnEnemyDied()
    {
        if (roundEnded || finalVictoryStarted)
            return;

        enemiesKilledThisRound++;
        aliveEnemies.RemoveAll(enemy => enemy == null || enemy.IsDead);
        TryTriggerQueuedElevatorSpawns();

        TryEndRoundIfReady(rounds[currentRoundIndex]);
    }

    private void TryEndRoundIfReady(RoundData data)
    {
        if (roundEnded || finalVictoryStarted)
            return;

        aliveEnemies.RemoveAll(enemy => enemy == null || enemy.IsDead);
        TryTriggerQueuedElevatorSpawns();

        if (aliveEnemies.Count <= 0 && AreAllElevatorSpawnEventsComplete(data))
            EndRound();
    }

    private void EndRound()
    {
        if (roundEnded || finalVictoryStarted)
            return;

        roundEnded = true;
        StopElevatorSpawnRoutines();

        foreach (Health enemy in aliveEnemies)
        {
            if (enemy != null)
                enemy.OnDied -= OnEnemyDied;
        }

        RoundData data = rounds[currentRoundIndex];

        int bonusFame = CalculateRoundBonus(data);
        int totalFameReward = data.fameReward + bonusFame;

        fame += totalFameReward;
        ApplyRoundRecovery(data);
        UpdateUI();

        if (sfxSource != null && victorySound != null)
            sfxSource.PlayOneShot(victorySound);

        if (ShouldTriggerFinalVictory())
            StartCoroutine(FinalVictoryRoutine());
        else
            StartCoroutine(NextRoundRoutine(data, totalFameReward, bonusFame));
    }

    private bool ShouldTriggerFinalVictory()
    {
        bool isLastRound = currentRoundIndex >= rounds.Count - 1;
        bool fameGoalReached = winWhenFameGoalReached && fame >= fameGoal;

        return isLastRound || fameGoalReached;
    }

    private IEnumerator NextRoundRoutine(RoundData data, int totalFameReward, int bonusFame)
    {
        ShowMessage(BuildRoundRewardMessage(data, totalFameReward, bonusFame), messageDuration);

        yield return new WaitForSecondsRealtime(messageDuration + nextRoundDelay);

        HideMessage();

        currentRoundIndex++;
        StartRound();
    }

    private IEnumerator FinalVictoryRoutine()
    {
        if (finalVictoryStarted)
            yield break;

        finalVictoryStarted = true;
        roundEnded = true;

        if (arenaDoor != null)
            arenaDoor.OpenDoor();

        if (playerCelebration != null)
            playerCelebration.UnlockCelebration();

        if (!keepPlayerControlOnVictory)
            DisablePlayerControl();

        Cursor.lockState = keepPlayerControlOnVictory ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !keepPlayerControlOnVictory;

        if (finalVictoryCinematic != null)
        {
            finalVictoryCinematic.gameObject.SetActive(true);
            finalVictoryCinematic.time = 0d;
            finalVictoryCinematic.Play();

            if (waitFinalVictoryCinematic)
                yield return new WaitForSecondsRealtime(GetPlayableDuration(finalVictoryCinematic, finalVictoryCinematicDuration));
        }

        ShowMessage("VICTORIA\n" + FameTitle + "\nRondas perfectas: " + flawlessRounds + "\nPulsa C para celebrar", messageDuration);

        if (!loadMenuAfterVictory)
            yield break;

        yield return new WaitForSecondsRealtime(timeBeforeFade);

        if (fadeController != null)
            yield return StartCoroutine(fadeController.FadeOut());

        yield return new WaitForSecondsRealtime(timeBeforeLoadMenu);

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void ResetPlayerToSpawn()
    {
        if (player == null || playerSpawnPoint == null)
            return;

        if (playerController != null)
            playerController.enabled = false;

        player.position = playerSpawnPoint.position;
        player.rotation = playerSpawnPoint.rotation;

        if (playerController != null)
            playerController.enabled = true;

        if (playerCamera != null)
            playerCamera.ResetViewToPlayer();
    }

    private void ClearSpawnedEnemies()
    {
        foreach (Health enemy in aliveEnemies)
        {
            if (enemy != null)
                enemy.OnDied -= OnEnemyDied;
        }

        foreach (GameObject enemy in spawnedEnemies)
        {
            if (enemy != null)
                Destroy(enemy);
        }

        spawnedEnemies.Clear();
        aliveEnemies.Clear();
    }

    private void StopElevatorSpawnRoutines()
    {
        foreach (Coroutine routine in elevatorSpawnRoutines)
        {
            if (routine != null)
                StopCoroutine(routine);
        }

        elevatorSpawnRoutines.Clear();

        foreach (Coroutine routine in elevatorLiftRoutines)
        {
            if (routine != null)
                StopCoroutine(routine);
        }

        elevatorLiftRoutines.Clear();
    }

    private void ShowMessageTimed(string text)
    {
        if (messageCoroutine != null)
            StopCoroutine(messageCoroutine);

        messageCoroutine = StartCoroutine(ShowMessageTimedRoutine(text));
    }

    private IEnumerator ShowMessageTimedRoutine(string text)
    {
        ShowMessage(text, messageDuration);

        yield return new WaitForSecondsRealtime(messageDuration);

        if (roundMessageUI == null)
            HideMessage();
    }

    private void ShowMessage(string text)
    {
        ShowMessage(text, messageDuration);
    }

    private void ShowMessage(string text, float visibleTime)
    {
        if (roundMessageUI != null)
        {
            roundMessageUI.ShowMessage(text, visibleTime);
            return;
        }

        if (messageText == null)
            return;

        messageText.gameObject.SetActive(true);
        messageText.text = text;
    }

    private void HideMessage()
    {
        if (roundMessageUI != null)
            roundMessageUI.HideInstant();

        if (messageText != null)
            messageText.gameObject.SetActive(false);
    }

    private void UpdateUI()
    {
        if (roundText != null)
            roundText.text = "Ronda: " + CurrentRound;

        if (fameText != null)
            fameText.text = "Fama: " + FameTitle;
    }

    private void OnPlayerDamaged(int damage, string hitZone)
    {
        playerTookDamageThisRound = true;
    }

    private int CalculateRoundBonus(RoundData data)
    {
        int bonus = GetNoDamageBonus(data);

        if (data == null || playerTookDamageThisRound || bonus <= 0)
            return 0;

        flawlessRounds++;
        return bonus;
    }

    private void ApplyRoundRecovery(RoundData data)
    {
        if (data == null)
            return;

        if (data.fullRestoreAfterRound)
        {
            if (playerHealth != null)
                playerHealth.RestoreToFull();

            if (playerStamina != null)
                playerStamina.RestoreToFull();

            return;
        }

        int healthReward = GetHealthReward(data);
        float staminaReward = GetStaminaReward(data);

        if (playerHealth != null)
            playerHealth.Heal(healthReward);

        if (playerStamina != null)
            playerStamina.Restore(staminaReward);
    }

    private string BuildRoundRewardMessage(RoundData data, int totalFameReward, int bonusFame)
    {
        string message = "RONDA SUPERADA\n" + FameTitle + "\nFama +" + totalFameReward;

        if (bonusFame > 0)
            message += "\nBonus +" + bonusFame;

        return message;
    }

    private int GetNoDamageBonus(RoundData data)
    {
        if (data == null)
            return 0;

        return data.noDamageFameBonus > 0 ? data.noDamageFameBonus : defaultNoDamageFameBonus;
    }

    private int GetHealthReward(RoundData data)
    {
        if (data == null)
            return 0;

        return data.healthReward > 0 ? data.healthReward : defaultHealthReward;
    }

    private float GetStaminaReward(RoundData data)
    {
        if (data == null)
            return 0f;

        float reward = data.staminaReward > 0f ? data.staminaReward : defaultStaminaReward;
        return maxStaminaRewardPerRound > 0f ? Mathf.Min(reward, maxStaminaRewardPerRound) : reward;
    }

    private string GetFameTitle()
    {
        if (fame >= fameGoal)
            return "Gladiador libre";

        if (fame >= championFame)
            return "Campeon de la arena";

        if (fame >= contenderFame)
            return "Aspirante del pueblo";

        return "Esclavo de la arena";
    }

    private void DisablePlayerControl()
    {
        if (player == null)
            return;

        PlayerMove move = player.GetComponent<PlayerMove>();
        if (move != null)
            move.enabled = false;

        PlayerCombat combat = player.GetComponent<PlayerCombat>();
        if (combat != null)
            combat.enabled = false;

        PlayerDodge dodge = player.GetComponent<PlayerDodge>();
        if (dodge != null)
            dodge.enabled = false;

        PlayerBlock block = player.GetComponent<PlayerBlock>();
        if (block != null)
            block.enabled = false;

        Debug.Log("Jugador desactivado: fin del juego");
    }

    private static float GetPlayableDuration(PlayableDirector director, float fallbackDuration)
    {
        if (director == null || director.duration <= 0d || double.IsInfinity(director.duration))
            return Mathf.Max(0f, fallbackDuration);

        return Mathf.Max(0f, (float)director.duration);
    }
}
