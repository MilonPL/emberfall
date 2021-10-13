﻿using System;
using Content.Shared.EffectBlocker;

namespace Content.Shared.ActionBlocker
{
    /// <summary>
    /// This interface gives components the ability to block certain actions from
    /// being done by the owning entity. For effects see <see cref="IEffectBlocker"/>
    /// </summary>
    [Obsolete("Use events instead")]
    public interface IActionBlocker
    {
        [Obsolete("Use SpeakAttemptEvent instead")]
        bool CanSpeak() => true;

        [Obsolete("Use DropAttemptEvent instead")]
        bool CanDrop() => true;

        [Obsolete("Use PickupAttemptEvent instead")]
        bool CanPickup() => true;

        [Obsolete("Use EmoteAttemptEvent instead")]
        bool CanEmote() => true;

        [Obsolete("Use AttackAttemptEvent instead")]
        bool CanAttack() => true;

        [Obsolete("Use EquipAttemptEvent instead")]
        bool CanEquip() => true;

        [Obsolete("Use UnequipAttemptEvent instead")]
        bool CanUnequip() => true;
    }
}
