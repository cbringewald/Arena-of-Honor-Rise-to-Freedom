using System.Collections.Generic;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }

    private readonly List<Health> aliveEnemies = new List<Health>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void RegisterEnemy(Health enemyHealth)
    {
        if (enemyHealth == null) return;
        if (!aliveEnemies.Contains(enemyHealth))
            aliveEnemies.Add(enemyHealth);
    }

    public void UnregisterEnemy(Health enemyHealth)
    {
        if (enemyHealth == null) return;
        aliveEnemies.Remove(enemyHealth);
    }

    public bool IsLastEnemy(Health enemyHealth)
    {
        int aliveCount = 0;

        foreach (var enemy in aliveEnemies)
        {
            if (enemy != null && !enemy.IsDead)
                aliveCount++;
        }

        return aliveCount == 1 && enemyHealth != null && !enemyHealth.IsDead;
    }

    public int GetAliveEnemyCount()
    {
        int aliveCount = 0;

        foreach (var enemy in aliveEnemies)
        {
            if (enemy != null && !enemy.IsDead)
                aliveCount++;
        }

        return aliveCount;
    }
}