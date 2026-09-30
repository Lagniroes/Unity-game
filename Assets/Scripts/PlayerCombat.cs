using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sword attack: press attack to swing, and mobs in front of Finn get hit partway through the swing.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerCombat : MonoBehaviour
{
    public int damage = 1;
    public float range = 2.4f;
    public float attackDuration = 0.35f;
    public float cooldown = 0.45f;
    [Range(0f, 1f)] public float hitMoment = 0.45f; // point in the swing where damage is dealt

    float attackStart = -10f;
    bool hitApplied = true;

    public bool IsAttacking => Time.time - attackStart < attackDuration;

    /// <summary>0..1 through the current swing, or -1 when not attacking.</summary>
    public float AttackProgress => IsAttacking ? (Time.time - attackStart) / attackDuration : -1f;

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

        if (GameInput.AttackPressed && Time.time - attackStart >= cooldown)
        {
            attackStart = Time.time;
            hitApplied = false;
        }

        if (!hitApplied && Time.time - attackStart >= attackDuration * hitMoment)
        {
            hitApplied = true;
            HitEnemiesInFront();
        }
    }

    void HitEnemiesInFront()
    {
        // Copy, because killing an enemy removes it from Enemy.All
        foreach (Enemy enemy in new List<Enemy>(Enemy.All))
        {
            Vector3 toEnemy = enemy.Center - transform.position;
            if (Mathf.Abs(toEnemy.y) > 1.5f) continue;
            toEnemy.y = 0f;

            float distance = toEnemy.magnitude;
            if (distance > range + enemy.Radius) continue;
            if (distance > 1f && Vector3.Dot(transform.forward, toEnemy / distance) < 0.3f) continue;

            enemy.TakeHit(damage, transform.position);
        }
    }
}
