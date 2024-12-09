using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Shared.Actions;
using Content.Shared.Armor;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Devour;
using Content.Shared.Devour.Components;
using Content.Shared.DoAfter;
using Content.Shared.Forensics;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NameModifier.Components;
using Content.Shared.Popups;
using Content.Shared.Speech.Components;
using Content.Shared.Wagging;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Changeling.Devour;

public abstract partial class SharedChangelingDevourSystem : EntitySystem
{

    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedBodySystem _bodySystem = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedChangelingIdentitySystem _changelingIdentitySystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ChangelingDevourComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<ChangelingDevourComponent, ChangelingDevourActionEvent>(OnDevourAction);
        SubscribeLocalEvent<ChangelingDevourComponent, ChangelingDevourWindupDoAfterEvent>(OnDevourWindup);
        SubscribeLocalEvent<ChangelingDevourComponent, ChangelingDevourConsumeDoAfterEvent>(OnDevourConsume);
        SubscribeLocalEvent<ChangelingDevourComponent, DoAfterAttemptEvent<ChangelingDevourConsumeDoAfterEvent>>(OnConsumeAttemptTick);
    }
    private void OnInit(EntityUid uid, ChangelingDevourComponent component, MapInitEvent args)
    {
        _actionsSystem.AddAction(uid, ref component.ChangelingDevourActionEntity, component.ChangelingDevourAction);
        var identityStorage = EnsureComp<ChangelingIdentityComponent>(uid);
        if (identityStorage.ConsumedIdentities.Count > 0)
            return;
        _changelingIdentitySystem.CloneToNullspace(uid, identityStorage, uid); // Clone yourself so you can transform back.
    }
    private void OnConsumeAttemptTick(EntityUid uid,
        ChangelingDevourComponent component,
        DoAfterAttemptEvent<ChangelingDevourConsumeDoAfterEvent> eventData)
    {
        var curTime = _timing.CurTime;
        if (curTime < component.NextTick)
            return;
        ConsumeDamageTick(eventData.Event.Target, component, eventData.Event.User);
        component.NextTick = curTime + TimeSpan.FromSeconds(1f);
    }

    private void ConsumeDamageTick(EntityUid? target, ChangelingDevourComponent comp, EntityUid? user)
    {
        if (target == null)
            return;

        if (!TryComp<DamageableComponent>(target, out var damage))
            return;
        if (damage.DamagePerGroup.TryGetValue("Brute", out var val) && val < comp.DevourConsumeDamageCap)
        {
            _damageable.TryChangeDamage(target, comp.DamagePerTick, true, true, damage, user);
        }
    }


    private void OnDevourAction(EntityUid uid, ChangelingDevourComponent component, ChangelingDevourActionEvent args)
    {
        Dirty(args.Performer, component);

        if (args.Handled || _whitelistSystem.IsWhitelistFailOrNull(component.Whitelist, args.Target))
            return;
        if (!HasComp<DamageableComponent>(args.Target))
            return;
        if(!TryComp<ContainerManagerComponent>(args.Target, out var containerManager))
            return;
        var curTime = _timing.CurTime;
        component.NextTick = curTime + TimeSpan.FromSeconds(15); // Prevent someone from spamming and canceling the Action repeatedly and doing absurd damage due to DoAfter's continuing onwards on cancel
        args.Handled = true;

        var target = args.Target;

        if (containerManager.Containers.TryGetValue("outerClothing", out var outerClothing)
            && outerClothing.Count > 0) // Somehow they don't have an outerClothing slot... somehow, so like... we'll just eat the target anyways
        {
            foreach (var item in outerClothing.ContainedEntities)
            {
                if (!TryComp<ArmorComponent>(item, out var armor)
                    || armor.Modifiers.Coefficients["Slash"] < component.DevourPreventionPercentageThreshold
                    || armor.Modifiers.Coefficients["Blunt"] < component.DevourPreventionPercentageThreshold
                    || armor.Modifiers.Coefficients["Piercing"] < component.DevourPreventionPercentageThreshold)
                    continue;
                _popupSystem.PopupClient(Loc.GetString("changeling-attempt-failed-protected"), args.Performer, args.Performer, PopupType.Medium);
                return;
            }
        }
        if (HasComp<ChangelingHuskedCorpseComponent>(target))
        {
            _popupSystem.PopupClient(Loc.GetString("changeling-devour-failed-husk"), args.Performer, args.Performer);
            return;
        }
        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, uid, component.DevourWindupTime, new ChangelingDevourWindupDoAfterEvent(), uid, target: target, used: uid)
        {
            BreakOnMove = true,
            BlockDuplicate = true,
            DuplicateCondition = DuplicateConditions.None,
        });

        _popupSystem.PopupPredicted(Loc.GetString("changeling-devour-begin-windup"), args.Performer, null, PopupType.MediumCaution);

    }
    private void OnDevourWindup(EntityUid uid, ChangelingDevourComponent component, ChangelingDevourWindupDoAfterEvent args)
    {
        var curTime = _timing.CurTime;
        args.Handled = true;


        if (args.Cancelled)
        {
            return;
        }
        Dirty(args.User, component);
        _popupSystem.PopupPredicted(Loc.GetString("changeling-devour-begin-consume"),
            args.User,
            null,
            PopupType.LargeCaution);
        StartSound(uid, component);
        component.NextTick = curTime + TimeSpan.FromSeconds(1);

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager,
            uid,
            component.DevourConsumeTime,
            new ChangelingDevourConsumeDoAfterEvent(),
            uid,
            target: args.Target,
            used: uid)
        {
            AttemptFrequency = AttemptFrequency.EveryTick,
            BreakOnMove = true,
            BlockDuplicate = true,
            DuplicateCondition = DuplicateConditions.None,
        });

    }
    private void OnDevourConsume(EntityUid uid, ChangelingDevourComponent component, ChangelingDevourConsumeDoAfterEvent args)
    {
        args.Handled = true;
        var target = args.Target;
        if (target == null)
            return;

        if (args.Cancelled)
        {
            if (component.CurrentDevourSound != null)
            {
                StopSound(uid, component);
            }
            return;
        }

        if (!_mobState.IsDead((EntityUid)target))
        {
            _popupSystem.PopupClient(Loc.GetString("changeling-devour-consume-failed-not-dead"), args.User,  args.User, PopupType.Medium);
            return;
        }
        _popupSystem.PopupPredicted(Loc.GetString("changeling-devour-consume-complete"), args.User, null, PopupType.LargeCaution);
        if (_mobState.IsDead((EntityUid)target)
            && TryComp<BodyComponent>(target, out var body)
            && TryComp<HumanoidAppearanceComponent>(target, out _)
            && TryComp<ChangelingIdentityComponent>(args.User, out var identityStorage))
        {
            _changelingIdentitySystem.CloneToNullspace(uid, identityStorage, (EntityUid)target);
            EnsureComp<ChangelingHuskedCorpseComponent>((EntityUid)target);

            foreach (var organ in _bodySystem.GetBodyOrgans(target, body))
            {
                _entityManager.QueueDeleteEntity(organ.Id);
            }
        }
        Dirty(uid, component);
    }
    public virtual void StartSound(EntityUid uid, ChangelingDevourComponent component){ }
    public virtual void StopSound(EntityUid uid, ChangelingDevourComponent component) { }
}


public sealed partial class ChangelingDevourActionEvent : EntityTargetActionEvent { }

[Serializable, NetSerializable]
public sealed partial class ChangelingDevourWindupDoAfterEvent : SimpleDoAfterEvent { }

[Serializable, NetSerializable]
public sealed partial class ChangelingDevourConsumeDoAfterEvent : SimpleDoAfterEvent { }

public struct ChangelingKillSoundEvent
{
    public bool Handled { get; set; }
    public EntityUid sound { get; }

    public ChangelingKillSoundEvent(EntityUid Sound)
    {
        sound = Sound;
        Handled = false;
    }
}

[ByRefEvent]
public struct ChangelingDevourFailedOnHuskEvent
{
    public bool Handled;
    public ChangelingDevourFailedOnHuskEvent()
    {
        Handled = false;
    }
}
