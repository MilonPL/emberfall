using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Utility;

namespace Content.Shared.Weapons.Ranged.Systems;

public abstract partial class SharedGunSystem
{
    private void OnGunVerbExamine(Entity<GunComponent> ent, ref GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var examineMarkup = GetGunExamine(ent);

        var ev = new GunExamineEvent(examineMarkup);
        RaiseLocalEvent(ent, ref ev);

        Examine.AddDetailedExamineVerb(args,
            ent.Comp,
            examineMarkup,
            Loc.GetString("gun-examinable-verb-text"),
            "/Textures/Interface/VerbIcons/dot.svg.192dpi.png",
            Loc.GetString("gun-examinable-verb-message"));
    }

    private FormattedMessage GetGunExamine(Entity<GunComponent> ent)
    {
        var msg = new FormattedMessage();
        msg.AddMarkupOrThrow(Loc.GetString("gun-examine"));

        // Recoil (AngleIncrease)
        msg.PushNewline();
        msg.AddMarkupOrThrow(Loc.GetString("gun-examine-recoil",
            ("color", FireRateExamineColor),
            ("value", MathF.Round((float)ent.Comp.AngleIncreaseModified.Degrees, 2))
        ));

        // Stability (AngleDecay)
        msg.PushNewline();
        msg.AddMarkupOrThrow(Loc.GetString("gun-examine-stability",
            ("color", FireRateExamineColor),
            ("value", MathF.Round((float)ent.Comp.AngleDecayModified.Degrees, 2))
        ));

        // Max Angle
        msg.PushNewline();
        msg.AddMarkupOrThrow(Loc.GetString("gun-examine-max-angle",
            ("color", FireRateExamineColor),
            ("value", MathF.Round((float)ent.Comp.MaxAngleModified.Degrees, 2))
        ));

        // Min Angle
        msg.PushNewline();
        msg.AddMarkupOrThrow(Loc.GetString("gun-examine-min-angle",
            ("color", FireRateExamineColor),
            ("value", MathF.Round((float)ent.Comp.MinAngleModified.Degrees, 2))
        ));

        // Fire Rate (converted from RPS to RPM)
        msg.PushNewline();
        msg.AddMarkupOrThrow(Loc.GetString("gun-examine-fire-rate",
            ("color", FireRateExamineColor),
            ("value", MathF.Round(ent.Comp.FireRateModified, 1))
        ));

        // Muzzle Velocity (ProjectileSpeed * 10)
        msg.PushNewline();
        msg.AddMarkupOrThrow(Loc.GetString("gun-examine-muzzle-velocity",
            ("color", FireRateExamineColor),
            ("value", MathF.Round(ent.Comp.ProjectileSpeedModified * 10f, 0))
        ));

        return msg;
    }

    private bool TryGetGunCaliber(EntityUid uid, [NotNullWhen(true)] out string? caliber)
    {
        caliber = null;

        // Try standard gun with ItemSlots first
        if (HasComp<ItemSlotsComponent>(uid) &&
            _slots.TryGetSlot(uid, "gun_chamber", out var chamberSlot) &&
            chamberSlot.Whitelist?.Tags is { Count: > 0 })
        {
            caliber = GetCaliberFromTag(chamberSlot.Whitelist.Tags.First());
            return true;
        }

        // Try revolver
        if (TryComp<RevolverAmmoProviderComponent>(uid, out var revolver) &&
            revolver.Whitelist?.Tags is { Count: > 0 })
        {
            caliber = GetCaliberFromTag(revolver.Whitelist.Tags.First());
            return true;
        }

        return false;
    }

    private static string GetCaliberFromTag(string tag)
    {
        return tag switch
        {
            "CartridgeMagnum" => ".44 Magnum",
            "CartridgePistol" => "9x19mm",
            "CartridgeHeavyPistol" => "10mm auto",
            "CartridgeRifle" => "7.62x51mm",
            "CartridgeLightRifle" => "5.56x45mm",
            "CartridgeCaselessRifle" => "4.73x33mm caseless",
            _ => tag,
        };
    }

    private void InitializeGunExamine()
    {
        SubscribeLocalEvent<GunComponent, GetVerbsEvent<ExamineVerb>>(OnGunVerbExamine);
    }
}

/// <summary>
/// Event raised on a gun entity to get additional examine text relating to its specifications.
/// </summary>
[ByRefEvent]
public readonly record struct GunExamineEvent(FormattedMessage Msg);
