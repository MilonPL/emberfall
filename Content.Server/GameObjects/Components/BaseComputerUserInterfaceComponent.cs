using Content.Server.GameObjects.Components.Construction;
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Content.Server.Utility;
using Content.Shared.GameObjects.Components;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;
using Robust.Shared.Localization;

namespace Content.Server.GameObjects.Components
{
    /// <summary>
    /// This component is used as a base class for classes like SolarControlConsoleComponent.
    /// These components operate the server-side logic for the "primary UI" of a computer.
    /// That means showing the UI when a user activates it, for example.
    /// </summary>
    public abstract class BaseComputerUserInterfaceComponent : Component
    {
        // { get; private set; } doesn't really express this properly.
        protected readonly object UserInterfaceKey;

        [ViewVariables] protected BoundUserInterface? UserInterface => Owner.GetUIOrNull(UserInterfaceKey);
        [ViewVariables] public bool Powered => !Owner.TryGetComponent(out PowerReceiverComponent? receiver) || receiver.Powered;

        public BaseComputerUserInterfaceComponent(object key)
        {
            UserInterfaceKey = key;
        }

        public override void Initialize()
        {
            base.Initialize();

            if (UserInterface != null)
                UserInterface.OnReceiveMessage += OnReceiveUIMessageCallback;

            Owner.EnsureComponent<PowerReceiverComponent>();
        }

        /// <summary>
        /// Internal callback used to grab session and session attached entity before any more work is done.
        /// This is so that sessionEntity is always available to checks up and down the line.
        /// </summary>
        private void OnReceiveUIMessageCallback(ServerBoundUserInterfaceMessage obj)
        {
            var session = obj.Session;
            var sessionEntity = session.AttachedEntity;
            if (sessionEntity == null)
                return; // No session entity, so we're probably not able to touch this.
            OnReceiveUnfilteredUserInterfaceMessage(obj, sessionEntity);
        }

        /// <summary>
        /// Override this to handle messages from the UI before filtering them.
        /// Calling base is necessary if you want this class to have any meaning.
        /// </summary>
        protected void OnReceiveUnfilteredUserInterfaceMessage(ServerBoundUserInterfaceMessage obj, IEntity sessionEntity)
        {
            // "Across all computers" "anti-cheats" ought to be put here or at some parent level (BaseDeviceUserInterfaceComponent?)
            // Determine some facts about the session.
            // Powered?
            if (!Powered)
            {
                sessionEntity.PopupMessageCursor(Loc.GetString("base-computer-ui-component-not-powered"));
                return; // Not powered, so this computer should probably do nothing.
            }
            // Can we interact?
            if (!ActionBlockerSystem.CanInteract(sessionEntity))
            {
                sessionEntity.PopupMessageCursor(Loc.GetString("base-computer-ui-component-cannot-interact"));
                return;
            }
            // Good to go!
            OnReceiveUserInterfaceMessage(obj);
        }

        /// <summary>
        /// Override this to handle messages from the UI.
        /// Calling base is unnecessary.
        /// These messages will automatically be blocked if the user shouldn't be able to access this computer, or if the computer has lost power.
        /// </summary>
        protected virtual void OnReceiveUserInterfaceMessage(ServerBoundUserInterfaceMessage obj)
        {
            // Nothing!
        }

        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
            base.HandleMessage(message, component);
            switch (message)
            {
                case PowerChangedMessage powerChanged:
                    PowerReceiverOnOnPowerStateChanged(powerChanged);
                    break;
            }
        }

        private void PowerReceiverOnOnPowerStateChanged(PowerChangedMessage e)
        {
            if (!e.Powered)
            {
                // We need to kick off users who are using it when it loses power.
                UserInterface?.CloseAll();
                // Now alert subclass.
                ComputerLostPower();
            }
        }

        /// <summary>
        /// Override this if you want the computer to do something when it loses power (i.e. reset state)
        /// All UIs should have been closed by the time this is called.
        /// Calling base is unnecessary.
        /// </summary>
        public virtual void ComputerLostPower()
        {
        }

        /// <summary>
        /// This is called from ComputerUIActivatorSystem.
        /// Override this to add additional activation conditions of some sort.
        /// Calling base runs standard activation logic.
        /// *This remains inside the component for overridability.*
        /// </summary>
        public virtual void ActivateThunk(ActivateInWorldEvent eventArgs)
        {
            if (!eventArgs.User.TryGetComponent(out ActorComponent? actor))
            {
                return;
            }

            if (!Powered)
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("base-computer-ui-component-not-powered"));
                return;
            }

            UserInterface?.Open(actor.PlayerSession);
        }
    }
}
