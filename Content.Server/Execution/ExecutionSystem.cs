using Content.Server.Kitchen.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Execution;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Player;

namespace Content.Server.Execution;

/// <summary>
///     Verb for violently murdering cuffed creatures.
/// </summary>
public sealed class ExecutionSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly SharedMeleeWeaponSystem _meleeSystem = default!;

    private const float MeleeExecutionTimeModifier = 5.0f;
    private const float GunExecutionTime = 10.0f;
    private const float DamageModifier = 9.0f;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<SharpComponent, GetVerbsEvent<UtilityVerb>>(OnGetInteractionVerbsMelee);
        SubscribeLocalEvent<GunComponent, GetVerbsEvent<UtilityVerb>>(OnGetInteractionVerbsGun);
        
        SubscribeLocalEvent<SharpComponent, ExecutionDoAfterEvent>(OnDoafterMelee);
        SubscribeLocalEvent<GunComponent, ExecutionDoAfterEvent>(OnDoafterGun);
    }

    private void OnGetInteractionVerbsMelee(
        EntityUid uid, 
        SharpComponent component,
        GetVerbsEvent<UtilityVerb> args)
    {
        if (args.Hands == null || args.Using == null || !args.CanAccess || !args.CanInteract)
            return;
        
        var attacker = args.User;
        var weapon = args.Using!.Value;
        var victim = args.Target;

        if (!CanExecuteWithMelee(weapon, victim, victim))
            return;
        
        UtilityVerb verb = new()
        {
            Act = () =>
            {
                TryStartMeleeExecutionDoafter(weapon, victim, attacker);
            },
            Impact = LogImpact.High,
            Text = Loc.GetString("execution-verb-name"),
            Message = Loc.GetString("execution-verb-message"),
        };

        args.Verbs.Add(verb);
    }

    private void OnGetInteractionVerbsGun(
        EntityUid uid, 
        GunComponent component,
        GetVerbsEvent<UtilityVerb> args)
    {
        if (args.Hands == null || args.Using == null || !args.CanAccess || !args.CanInteract)
            return;

        var attacker = args.User;
        var weapon = args.Using!.Value;
        var victim = args.Target;

        if (!CanExecuteWithGun(weapon, victim, victim))
            return;
        
        UtilityVerb verb = new()
        {
            Act = () =>
            {
                TryStartGunExecutionDoafter(weapon, victim, attacker);
            },
            Impact = LogImpact.High,
            Text = Loc.GetString("execution-verb-name"),
            Message = Loc.GetString("execution-verb-message"),
        };

        args.Verbs.Add(verb);
    }

    private bool CanExecuteWithAny(EntityUid weapon, EntityUid victim, EntityUid user)
    {
        // No point executing someone if they can't take damage
        if (!TryComp<DamageableComponent>(victim, out var damage))
            return false;
        
        // You can't execute something that cannot die
        if (!TryComp<MobStateComponent>(victim, out var mobState))
            return false;
        
        // You're not allowed to execute dead people (no fun allowed)
        if (_mobStateSystem.IsDead(victim, mobState))
            return false;

        // You must be incapacitated to execute someone else
        if (victim != user && _actionBlockerSystem.CanInteract(victim, null))
            return false;
        
        // You must be not incapacitated to execute yourself
        if (victim == user && !_actionBlockerSystem.CanInteract(victim, null))
            return false;

        // All checks passed
        return true;
    }

    private bool CanExecuteWithMelee(EntityUid weapon, EntityUid victim, EntityUid user)
    {
        if (!CanExecuteWithAny(weapon, victim, user)) return false;
        
        // We must be able to actually hurt people with the weapon
        if (!TryComp<MeleeWeaponComponent>(weapon, out var melee) && melee!.Damage.GetTotal() > 0.0f)
            return false;

        return true;
    }
    
    private bool CanExecuteWithGun(EntityUid weapon, EntityUid victim, EntityUid user)
    {
        if (!CanExecuteWithAny(weapon, victim, user)) return false;
        
        // We must be able to actually fire the gun and have it do damage
        if (!TryComp<GunComponent>(weapon, out var gun))
            return false;

        return true;
    }
    
    private void TryStartMeleeExecutionDoafter(EntityUid weapon, EntityUid victim, EntityUid attacker)
    {
        if (!CanExecuteWithMelee(weapon, victim, attacker))
            return;

        var executionTime = (1.0f / Comp<MeleeWeaponComponent>(weapon).AttackRate) * MeleeExecutionTimeModifier;

        if (attacker == victim)
        {
            ShowExecutionPopup("suicide-popup-melee-initial-internal", Filter.Entities(attacker), PopupType.Medium, attacker, victim, weapon);
            ShowExecutionPopup("suicide-popup-melee-initial-external", Filter.PvsExcept(attacker), PopupType.Medium, attacker, victim, weapon);
        }
        else
        {
            ShowExecutionPopup("execution-popup-melee-initial-internal", Filter.Entities(attacker), PopupType.Medium, attacker, victim, weapon);
            ShowExecutionPopup("execution-popup-melee-initial-external", Filter.PvsExcept(attacker), PopupType.Medium, attacker, victim, weapon);
        }
        
        var doAfter =
            new DoAfterArgs(EntityManager, attacker, executionTime, new ExecutionDoAfterEvent(), weapon, target: victim, used: weapon)
            {
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnDamage = true,
                NeedHand = true
            };

        _doAfterSystem.TryStartDoAfter(doAfter);
    }
    
    private void TryStartGunExecutionDoafter(EntityUid weapon, EntityUid victim, EntityUid attacker)
    {
        if (!CanExecuteWithGun(weapon, victim, attacker))
            return;
        
        if (attacker == victim)
        {
            ShowExecutionPopup("suicide-popup-gun-initial-internal", Filter.Entities(attacker), PopupType.Medium, attacker, victim, weapon);
            ShowExecutionPopup("suicide-popup-gun-initial-external", Filter.PvsExcept(attacker), PopupType.Medium, attacker, victim, weapon);
        }
        else
        {
            ShowExecutionPopup("execution-popup-gun-initial-internal", Filter.Entities(attacker), PopupType.Medium, attacker, victim, weapon);
            ShowExecutionPopup("execution-popup-gun-initial-external", Filter.PvsExcept(attacker), PopupType.Medium, attacker, victim, weapon);
        }

        var doAfter =
            new DoAfterArgs(EntityManager, attacker, GunExecutionTime, new ExecutionDoAfterEvent(), weapon, target: victim, used: weapon)
            {
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnDamage = true,
                NeedHand = true
            };

        _doAfterSystem.TryStartDoAfter(doAfter);
    }

    private bool OnDoafterChecks(EntityUid uid, DoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Used == null || args.Target == null)
            return false;
        
        if (!CanExecuteWithAny(args.Used.Value, args.Target.Value, uid))
            return false;
        
        // All checks passed
        return true;
    }

    private void OnDoafterMelee(EntityUid uid, SharpComponent component, DoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Used == null || args.Target == null)
            return;
        
        var attacker = args.User;
        var victim = args.Target!.Value;
        var weapon = args.Used!.Value;

        if (!CanExecuteWithMelee(weapon, victim, attacker)) return;

        if (!TryComp<MeleeWeaponComponent>(weapon, out var melee) && melee!.Damage.GetTotal() > 0.0f)
            return;
        
        _damageableSystem.TryChangeDamage(victim, melee.Damage * DamageModifier, true);
        _meleeSystem.PlayHitSound(victim, weapon, null, null, null);

        if (attacker == victim)
        {
            ShowExecutionPopup("suicide-popup-melee-complete-internal", Filter.Entities(attacker), PopupType.Medium, attacker, victim, weapon);
            ShowExecutionPopup("suicide-popup-melee-complete-external", Filter.PvsExcept(attacker), PopupType.Medium, attacker, victim, weapon);
        }
        else
        {
            ShowExecutionPopup("execution-popup-melee-complete-internal", Filter.Entities(attacker), PopupType.Medium, attacker, victim, weapon);
            ShowExecutionPopup("execution-popup-melee-complete-external", Filter.PvsExcept(attacker), PopupType.Medium, attacker, victim, weapon);
        }
    }
    
    private void OnDoafterGun(EntityUid uid, GunComponent component, DoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Used == null || args.Target == null)
            return;
        
        var attacker = args.User;
        var weapon = args.Used!.Value;
        var victim = args.Target!.Value;

        if (!CanExecuteWithGun(weapon, victim, attacker)) return;
    }

    private void ShowExecutionPopup(string locString, Filter filter, PopupType type,
        EntityUid attacker, EntityUid victim, EntityUid weapon)
    {
        _popupSystem.PopupEntity(Loc.GetString(
                locString, ("attacker", attacker), ("victim", victim), ("weapon", weapon)),
            attacker, filter, true, type);
    }
}