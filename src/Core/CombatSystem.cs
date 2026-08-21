namespace Luminfield.Core;

public sealed record CombatDamageResult(
    bool Succeeded,
    string MessageKey,
    int DamageTaken = 0,
    int RemainingHealth = 0,
    bool PlayerDefeated = false
);

public sealed record CombatDodgeResult(
    bool Succeeded,
    string MessageKey,
    float DistancePixels = 0,
    float InvulnerabilitySeconds = 0
);

public sealed record StarfallTrialAttackResult(
    bool Succeeded,
    string MessageKey,
    string EnemyInstanceId = "",
    string EnemyId = "",
    int DamageDealt = 0,
    int RemainingHealth = 0,
    bool EnemyDefeated = false,
    string ClearedRoomId = ""
);

public sealed record StarfallTrialDefeatResolution(
    bool Succeeded,
    string MessageKey,
    ShippingSettlement? Settlement = null
);

public sealed class CombatSystem
{
    public const int MaxHealth = 100;
    public const float HurtInvulnerabilitySeconds = 0.75f;
    public const float DodgeDistancePixels = 32f;
    public const float DodgeInvulnerabilitySeconds = 0.35f;
    public const float DodgeCooldownSeconds = 1.5f;

    private float _attackCooldownRemaining;
    private float _hurtInvulnerabilityRemaining;
    private float _dodgeCooldownRemaining;

    public int CurrentHealth { get; private set; } = MaxHealth;
    public float AttackCooldownRemaining => _attackCooldownRemaining;
    public float HurtInvulnerabilityRemaining =>
        _hurtInvulnerabilityRemaining;
    public float DodgeCooldownRemaining => _dodgeCooldownRemaining;
    public bool IsDefeated => CurrentHealth <= 0;
    public bool IsInvulnerable => _hurtInvulnerabilityRemaining > 0;

    public event Action? Changed;

    public void Reset()
    {
        CurrentHealth = MaxHealth;
        ResetTransientState();
        Changed?.Invoke();
    }

    public void Restore(CombatSave? save)
    {
        var normalized = NormalizeSave(save);
        CurrentHealth = normalized.CurrentHealth;
        ResetTransientState();
        Changed?.Invoke();
    }

    public void Advance(float deltaSeconds)
    {
        if (deltaSeconds <= 0)
        {
            return;
        }

        _attackCooldownRemaining = Math.Max(
            0,
            _attackCooldownRemaining - deltaSeconds
        );
        _hurtInvulnerabilityRemaining = Math.Max(
            0,
            _hurtInvulnerabilityRemaining - deltaSeconds
        );
        _dodgeCooldownRemaining = Math.Max(
            0,
            _dodgeCooldownRemaining - deltaSeconds
        );
    }

    public ActionResult CheckAttack(string weaponItemId)
    {
        if (IsDefeated)
        {
            return ActionResult.Fail("combat.player_defeated");
        }

        if (!StarfallRuinsTrialCatalog.TryWeapon(weaponItemId, out _))
        {
            return ActionResult.Fail("combat.requires_weapon");
        }

        return _attackCooldownRemaining <= 0
            ? ActionResult.Success(messageKey: "combat.attack.ready")
            : ActionResult.Fail("combat.attack.cooldown");
    }

    public void BeginCheckedAttack(string weaponItemId)
    {
        if (!CheckAttack(weaponItemId).Succeeded)
        {
            throw new InvalidOperationException(
                "Combat attack must be checked before it begins."
            );
        }

        _attackCooldownRemaining = StarfallRuinsTrialCatalog
            .Weapon(weaponItemId)
            .CooldownSeconds;
        Changed?.Invoke();
    }

    public CombatDamageResult ReceiveHit(int damage)
    {
        if (damage <= 0 || IsDefeated)
        {
            return new CombatDamageResult(
                false,
                "combat.hit.invalid",
                RemainingHealth: CurrentHealth,
                PlayerDefeated: IsDefeated
            );
        }

        if (IsInvulnerable)
        {
            return new CombatDamageResult(
                false,
                "combat.player_invulnerable",
                RemainingHealth: CurrentHealth
            );
        }

        var applied = Math.Min(CurrentHealth, damage);
        CurrentHealth -= applied;
        _hurtInvulnerabilityRemaining = HurtInvulnerabilitySeconds;
        Changed?.Invoke();
        return new CombatDamageResult(
            true,
            CurrentHealth == 0
                ? "combat.player_defeated"
                : "combat.player_hit",
            applied,
            CurrentHealth,
            CurrentHealth == 0
        );
    }

    public CombatDodgeResult CheckDodge()
    {
        if (IsDefeated)
        {
            return new CombatDodgeResult(
                false,
                "combat.player_defeated"
            );
        }

        return _dodgeCooldownRemaining <= 0
            ? new CombatDodgeResult(
                true,
                "combat.dodge.ready",
                DodgeDistancePixels,
                DodgeInvulnerabilitySeconds
            )
            : new CombatDodgeResult(false, "combat.dodge.cooldown");
    }

    public CombatDodgeResult BeginDodge()
    {
        var check = CheckDodge();
        if (!check.Succeeded)
        {
            return check;
        }

        _dodgeCooldownRemaining = DodgeCooldownSeconds;
        _hurtInvulnerabilityRemaining = Math.Max(
            _hurtInvulnerabilityRemaining,
            DodgeInvulnerabilitySeconds
        );
        Changed?.Invoke();
        return check with { MessageKey = "combat.dodge.started" };
    }

    public void RestoreFullHealth()
    {
        CurrentHealth = MaxHealth;
        ResetTransientState();
        Changed?.Invoke();
    }

    public CombatSave Capture() => new()
    {
        CurrentHealth = CurrentHealth
    };

    public static CombatSave NormalizeSave(CombatSave? save) => new()
    {
        CurrentHealth = save?.CurrentHealth is > 0 and <= MaxHealth
            ? save.CurrentHealth
            : MaxHealth
    };

    private void ResetTransientState()
    {
        _attackCooldownRemaining = 0;
        _hurtInvulnerabilityRemaining = 0;
        _dodgeCooldownRemaining = 0;
    }
}
