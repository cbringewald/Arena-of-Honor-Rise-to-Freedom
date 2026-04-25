using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RoundManager : MonoBehaviour
{
    [Header("Round Settings")]
    [SerializeField] private int currentRound = 1;
    [SerializeField] private int fame = 0;
    [SerializeField] private int fameGoal = 100;
    [SerializeField] private int famePerRound = 35;

    [Header("Enemies")]
    [SerializeField] private List<Health> enemies = new List<Health>();

    [Header("UI")]
    [SerializeField] private TMP_Text roundText;
    [SerializeField] private TMP_Text fameText;
    [SerializeField] private TMP_Text messageText;

    [Header("Timing")]
    [SerializeField] private float messageDuration = 3f;

    private bool roundEnded;

    private void Start()
    {
        roundEnded = false;

        if (messageText != null)
            messageText.gameObject.SetActive(false);

        RegisterEnemies();
        UpdateUI();
    }

    private void RegisterEnemies()
    {
        foreach (Health enemy in enemies)
        {
            if (enemy != null)
                enemy.OnDied += CheckRoundEnd;
        }
    }

    private void OnDestroy()
    {
        foreach (Health enemy in enemies)
        {
            if (enemy != null)
                enemy.OnDied -= CheckRoundEnd;
        }
    }

    private void CheckRoundEnd()
    {
        if (roundEnded)
            return;

        foreach (Health enemy in enemies)
        {
            if (enemy != null && !enemy.IsDead)
                return;
        }

        EndRound();
    }

    private void EndRound()
    {
        roundEnded = true;

        fame += famePerRound;
        UpdateUI();

        if (fame >= fameGoal)
        {
            StartCoroutine(ShowFinalVictory());
        }
        else
        {
            StartCoroutine(ShowRoundVictory());
        }
    }

    private IEnumerator ShowRoundVictory()
    {
        if (messageText != null)
        {
            messageText.gameObject.SetActive(true);
            messageText.text = "¡Victoria!\nFama +" + famePerRound;
        }

        yield return new WaitForSeconds(messageDuration);

        if (messageText != null)
            messageText.gameObject.SetActive(false);

        currentRound++;
        roundEnded = false;

        UpdateUI();

        Debug.Log("Preparada siguiente ronda: " + currentRound);
    }

    private IEnumerator ShowFinalVictory()
    {
        if (messageText != null)
        {
            messageText.gameObject.SetActive(true);
            messageText.text = "¡Has alcanzado la fama necesaria!\n¡Has ganado tu libertad!";
        }

        Debug.Log("Victoria final: libertad conseguida.");

        yield return null;
    }

    private void UpdateUI()
    {
        if (roundText != null)
            roundText.text = "Ronda: " + currentRound;

        if (fameText != null)
            fameText.text = "Fama: " + fame + " / " + fameGoal;
    }
}