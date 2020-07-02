﻿using Content.Server.GameObjects;
using Content.Server.GameObjects.Components.Observer;
using Content.Server.Interfaces.Chat;
using Content.Server.Interfaces.GameObjects;
using Content.Server.Players;
using Content.Shared.GameObjects;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Enums;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using System.Linq;
using Content.Server.GameObjects.Components.GUI;

namespace Content.Server.Chat
{
    internal class SayCommand : IClientCommand
    {
        public string Command => "say";
        public string Description => "Send chat messages to the local channel or a specified radio channel.";
        public string Help => "say <text>";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            if (player.Status != SessionStatus.InGame || !player.AttachedEntityUid.HasValue)
                return;

            if (args.Length < 1)
                return;

            var chat = IoCManager.Resolve<IChatManager>();

            var message = string.Join(" ", args);

            if (player.AttachedEntity.HasComponent<GhostComponent>())
                chat.SendDeadChat(player, message);
            else
            {
                var mindComponent = player.ContentData().Mind;
                chat.EntitySay(mindComponent.OwnedEntity, message);
            }

        }
    }

    internal class MeCommand : IClientCommand
    {
        public string Command => "me";
        public string Description => "Perform an action.";
        public string Help => "me <text>";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            if (player.Status != SessionStatus.InGame || !player.AttachedEntityUid.HasValue)
                return;

            if (args.Length < 1)
                return;

            var chat = IoCManager.Resolve<IChatManager>();

            var action = string.Join(" ", args);

            var mindComponent = player.ContentData().Mind;
            chat.EntityMe(mindComponent.OwnedEntity, action);
        }
    }

    internal class OOCCommand : IClientCommand
    {
        public string Command => "ooc";
        public string Description => "Send Out Of Character chat messages.";
        public string Help => "ooc <text>";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            var chat = IoCManager.Resolve<IChatManager>();
            chat.SendOOC(player, string.Join(" ", args));
        }
    }

    internal class SuicideCommand : IClientCommand
    {
        public string Command => "suicide";

        public string Description => "Commits suicide";

        public string Help => "The suicide command gives you a quick way out of a round while remaining in-character.\n" +
            "The method varies, first it will attempt to use the held item in your active hand.\n" +
            "If that fails, it will attempt to use an object in the environment.\n" +
            "Finally, if neither of the above worked, you will die by biting your tongue.";

        private void DealDamage(ISuicideAct suicide, IChatManager chat, DamageableComponent damageableComponent, IEntity source, IEntity target)
        {
            SuicideKind kind = suicide.Suicide(target, chat);
            if (kind != SuicideKind.Special)
            {
                damageableComponent.TakeDamage(kind switch
                {
                    SuicideKind.Brute    => DamageType.Brute,
                    SuicideKind.Heat     => DamageType.Heat,
                    SuicideKind.Cold     => DamageType.Cold,
                    SuicideKind.Acid     => DamageType.Acid,
                    SuicideKind.Toxic    => DamageType.Toxic,
                    SuicideKind.Electric => DamageType.Electric,
                                       _ => DamageType.Brute
                },
                500, //TODO: needs to be a max damage of some sorts
                source,
                target);
            }
        }

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            if (player.Status != SessionStatus.InGame)
                return;

            var chat = IoCManager.Resolve<IChatManager>();
            var owner = player.ContentData().Mind.OwnedMob.Owner;
            var dmgComponent = owner.GetComponent<DamageableComponent>();
            //TODO: needs to check if the mob is actually alive
            //TODO: maybe set a suicided flag to prevent ressurection?

            // Held item suicide
            var handsComponent = owner.GetComponent<HandsComponent>();
            var itemComponent = handsComponent.GetActiveHand;
            if (itemComponent != null)
            {
                ISuicideAct suicide = itemComponent.Owner.GetAllComponents<ISuicideAct>().FirstOrDefault();
                if (suicide != null)
                {
                    DealDamage(suicide, chat, dmgComponent, itemComponent.Owner, owner);
                    return;
                }
            }
            // Get all entities in range of the suicider
            var entities = owner.EntityManager.GetEntitiesInRange(owner, 1, true);
            if (entities.Count() > 0)
            {
                foreach (var entity in entities)
                {
                    if (entity.HasComponent<ItemComponent>())
                        continue;
                    var suicide = entity.GetAllComponents<ISuicideAct>().FirstOrDefault();
                    if (suicide != null)
                    {
                        DealDamage(suicide, chat, dmgComponent, entity, owner);
                        return;
                    }
                }
            }
            // Default suicide, bite your tongue
            chat.EntityMe(owner, Loc.GetString("is attempting to bite {0:their} own tongue, looks like {0:theyre} trying to commit suicide!", owner)); //TODO: theyre macro
            dmgComponent.TakeDamage(DamageType.Brute, 500, owner, owner); //TODO: dmg value needs to be a max damage of some sorts
        }
    }
}
