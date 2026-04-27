using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public class RoundData
{
    [Header("Round Info")]
    public string roundName = "Ronda";
    [TextArea] public string startMessage;

    [Header("Enemies")]
    public GameObject enemyPrefab;
    public int enemyCount = 1;
    public int enemyHealth = 3;

    [Header("Rewards")]
    public int fameReward = 30;

    [Header("Unlocked Weapons")]
    public GameObject[] unlockedWeaponPickups;

    [Header("Audio")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip victorySound;
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
    [SerializeField] private Transform playerSpawnPoint;

    [Header("Enemy Spawns")]
    [SerializeField] private Transform[] enemySpawnPoints;

    [Header("Door")]
    [SerializeField] private PortonArena arenaDoor;

    [Header("Rounds")]
    [SerializeField] private List<RoundData> rounds = new List<RoundData>();

    [Header("Fame")]
    [SerializeField] private int fame = 0;
    [SerializeField] private int fameGoal = 100;

    [Header("UI")]
    [SerializeField] private TMP_Text roundText;
    [SerializeField] private TMP_Text fameText;
    [SerializeField] private TMP_Text messageText;

    [Header("Timing")]
    [SerializeField] private float messageDuration = 3f;
    [SerializeField] private float nextRoundDelay = 1f;

    [SerializeField] private FadeController fadeController;
    [SerializeField] private float timeBeforeFade = 3f;
    [SerializeField] private float timeBeforeLoadMenu = 2f;
    

    private int currentRoundIndex = 0;
    private bool roundEnded;

    private Coroutine messageCoroutine;

    private readonly List<Health> aliveEnemies = new List<Health>();
    private readonly List<GameObject> spawnedEnemies = new List<GameObject>();

    public int CurrentRound => currentRoundIndex + 1;
    public int Fame => fame;

    private void Start()
    {
        if (playerController == null && player != null)
            playerController = player.GetComponent<CharacterController>();

        if (playerCombat == null && player != null)
            playerCombat = player.GetComponent<PlayerCombat>();

        if (messageText != null)
            messageText.gameObject.SetActive(false);

        ResetPlayerToSpawn();
        StartRound();
        UpdateUI();
    }

    private void StartRound()
    {
        if (currentRoundIndex < 0 || currentRoundIndex >= rounds.Count)
        {
            FinalVictory();
            return;
        }

        roundEnded = false;

        if (arenaDoor != null)
            arenaDoor.CloseDoor();

        ResetPlayerToSpawn();
        ResetPlayerWeapon();
        SetupWeaponUnlocks();
        SpawnEnemiesForRound();
        UpdateUI();

        RoundData data = rounds[currentRoundIndex];

        if (!string.IsNullOrWhiteSpace(data.startMessage))
            ShowMessageTimed(data.startMessage);
    }

    private void ResetPlayerWeapon()
    {
        if (playerCombat != null)
            playerCombat.UnequipWeapon();
    }

    private void SetupWeaponUnlocks()
    {
        // Primero apagamos todas las armas que estén configuradas en cualquier ronda
        foreach (RoundData round in rounds)
        {
            foreach (GameObject weapon in round.unlockedWeaponPickups)
            {
                if (weapon != null)
                    weapon.SetActive(false);
            }
        }

        // Luego encendemos las armas desbloqueadas hasta la ronda actual
        for (int i = 0; i <= currentRoundIndex; i++)
        {
            foreach (GameObject weapon in rounds[i].unlockedWeaponPickups)
            {
                if (weapon != null)
                    weapon.SetActive(true);
            }
        }
    }

    private void SpawnEnemiesForRound()
    {
        if (enemySpawnPoints == null || enemySpawnPoints.Length == 0)
        {
            Debug.LogError("RoundManager: faltan EnemySpawnPoints.");
            return;
        }

        RoundData data = rounds[currentRoundIndex];

        if (data.enemyPrefab == null)
        {
            Debug.LogError("RoundManager: falta EnemyPrefab en " + data.roundName);
            return;
        }

        ClearSpawnedEnemies();

        for (int i = 0; i < data.enemyCount; i++)
        {
            Transform spawnPoint = enemySpawnPoints[i % enemySpawnPoints.Length];

            GameObject enemy = Instantiate(data.enemyPrefab, spawnPoint.position, spawnPoint.rotation);
            spawnedEnemies.Add(enemy);

            Health health = enemy.GetComponent<Health>();
            if (health == null)
                health = enemy.GetComponentInChildren<Health>();

            if (health == null)
            {
                Debug.LogError("El enemigo instanciado no tiene Health.");
                continue;
            }

            health.SetMaxHealth(data.enemyHealth, true);
            health.OnDied += OnEnemyDied;
            aliveEnemies.Add(health);

            BotAI bot = enemy.GetComponent<BotAI>();
            if (bot == null)
                bot = enemy.GetComponentInChildren<BotAI>();

            if (bot != null)
                bot.SetPlayer(player);
            else
                Debug.LogWarning("El enemigo instanciado no tiene BotAI.");
        }

        Debug.Log(data.roundName + " creada con " + data.enemyCount + " enemigo(s).");
    }

    private void OnEnemyDied()
    {
        if (roundEnded)
            return;

        aliveEnemies.RemoveAll(enemy => enemy == null || enemy.IsDead);

        if (aliveEnemies.Count <= 0)
            EndRound();
    }

    private void EndRound()
    {
        if (roundEnded)
            return;

        roundEnded = true;

        foreach (Health enemy in aliveEnemies)
        {
            if (enemy != null)
                enemy.OnDied -= OnEnemyDied;
        }

            
        RoundData data = rounds[currentRoundIndex];

        fame += data.fameReward;
        UpdateUI();

        if (fame >= fameGoal || currentRoundIndex >= rounds.Count - 1)
        {
            StartCoroutine(FinalVictoryRoutine());
        }
        else
        {
            StartCoroutine(NextRoundRoutine(data.fameReward));
        }

        if (sfxSource != null && victorySound != null)
            sfxSource.PlayOneShot(victorySound);
    }

    private IEnumerator NextRoundRoutine(int fameReward)
    {
        ShowMessage("¡Ronda superada!\nFama +" + fameReward);

        yield return new WaitForSeconds(messageDuration);

        HideMessage();

        yield return new WaitForSeconds(nextRoundDelay);

        currentRoundIndex++;
        StartRound();
    }

    private IEnumerator FinalVictoryRoutine()
    {
        ShowMessage("¡Has alcanzado la fama necesaria!\nEl Lanista te entrega el Rudis.\n¡Eres libre!");

        if (arenaDoor != null)
            arenaDoor.OpenDoor();

        DisablePlayerControl();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        yield return new WaitForSeconds(timeBeforeFade);

        if (fadeController != null)
            yield return StartCoroutine(fadeController.FadeOut());

        yield return new WaitForSeconds(timeBeforeLoadMenu);

        Time.timeScale = 1f; // MUY IMPORTANTE
        SceneManager.LoadScene("MainMenu");

    }

    private void FinalVictory()
    {
        ShowMessage("¡Has ganado tu libertad!");
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
    }

    private void ClearSpawnedEnemies()
    {
        foreach (GameObject enemy in spawnedEnemies)
        {
            if (enemy != null)
                Destroy(enemy);
        }

        spawnedEnemies.Clear();
        aliveEnemies.Clear();
    }

    private void ShowMessageTimed(string text)
    {
        if (messageCoroutine != null)
            StopCoroutine(messageCoroutine);

        messageCoroutine = StartCoroutine(ShowMessageTimedRoutine(text));
    }

    private IEnumerator ShowMessageTimedRoutine(string text)
    {
        ShowMessage(text);
        yield return new WaitForSeconds(messageDuration);
        HideMessage();
    }

    private void ShowMessage(string text)
    {
        if (messageText == null)
            return;

        messageText.gameObject.SetActive(true);
        messageText.text = text;
    }

    private void HideMessage()
    {
        if (messageText == null)
            return;

        messageText.gameObject.SetActive(false);
    }

    private void UpdateUI()
    {
        if (roundText != null)
            roundText.text = "Ronda: " + CurrentRound;

        if (fameText != null)
            fameText.text = "Fama: " + fame + " / " + fameGoal;
    }

    private void DisablePlayerControl()
    {
        if (player == null) return;

        PlayerMove move = player.GetComponent<PlayerMove>();
        if (move != null) move.enabled = false;

        PlayerCombat combat = player.GetComponent<PlayerCombat>();
        if (combat != null) combat.enabled = false;

        PlayerDodge dodge = player.GetComponent<PlayerDodge>();
        if (dodge != null) dodge.enabled = false;

        PlayerBlock block = player.GetComponent<PlayerBlock>();
        if (block != null) block.enabled = false;

        Debug.Log("Jugador desactivado → fin del juego");
    }

}