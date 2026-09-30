using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Anything that can fight and be hit: health, hit-stun, knockback, dodge invulnerability,
/// and "armor" that kicks in after being stun-locked too many times in a row.
/// </summary>
public class Combatant : MonoBehaviour
{
    public static readonly List<Combatant> All = new List<Combatant>();

    public string displayName = "Rat";
    public int team;
    public float maxHealth = 100f;
    [Tooltip("After this many stuns in quick succession, gain brief armor so you can't be stun-locked.")]
    public int maxChainStuns = 99;
    public float knockbackDecay = 18f;

    public float Health { get; private set; }
    public bool IsDead => Health <= 0f;
    public bool IsStunned => Time.time < stunnedUntil;
    public bool IsInvulnerable => Time.time < invulnerableUntil;
    public bool IsDodging => Time.time < dodgingUntil;
    public bool IsArmored => Time.time < armorUntil;
    public Vector3 Knockback { get; private set; }

    float stunnedUntil;
    float invulnerableUntil;
    float dodgingUntil;
    float armorUntil;
    float lastStunTime = -10f;
    int stunChain;
    float pendingLaunch;
    RatRig rig;

    public void Init(string name, int teamId, float health)
    {
        displayName = name;
        team = teamId;
        maxHealth = health;
        Health = health;
    }

    void Awake() => Health = maxHealth;
    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);

    void Update()
    {
        Knockback = Vector3.MoveTowards(Knockback, Vector3.zero, knockbackDecay * Time.deltaTime);
    }

    public void StartDodge(float duration)
    {
        dodgingUntil = Time.time + duration;
        invulnerableUntil = Mathf.Max(invulnerableUntil, Time.time + duration + 0.05f);
    }

    /// <summary>Upward velocity from the last hit; returns it once and clears it.</summary>
    public float ConsumeLaunch()
    {
        float launch = pendingLaunch;
        pendingLaunch = 0f;
        return launch;
    }

    public bool TakeHit(float damage, Vector3 source, float knockback, float launch, float stun)
    {
        if (IsDead || IsInvulnerable) return false;

        Health = Mathf.Max(0f, Health - damage);

        Vector3 away = transform.position - source;
        away.y = 0f;
        away = away.sqrMagnitude > 0.0001f ? away.normalized : -transform.forward;

        if (IsArmored)
        {
            Knockback = away * knockback * 0.3f;
        }
        else
        {
            Knockback = away * knockback;
            pendingLaunch = launch;
            stunnedUntil = Time.time + stun;

            stunChain = Time.time - lastStunTime < 1.5f ? stunChain + 1 : 1;
            lastStunTime = Time.time;
            if (stunChain >= maxChainStuns)
            {
                armorUntil = stunnedUntil + 1.5f;
                stunChain = 0;
            }
        }

        if (!rig) rig = GetComponentInChildren<RatRig>();
        if (rig) rig.Flash(0.1f);

        if (GameManager.Instance) GameManager.Instance.OnHit(this, damage);
        if (IsDead)
        {
            stunnedUntil = 0f;
            if (GameManager.Instance) GameManager.Instance.OnCombatantDied(this);
        }
        return true;
    }
}
