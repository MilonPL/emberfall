﻿using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.Atmos;
using Content.Server.GameObjects.Components.Buckle;
using Content.Server.GameObjects.Components.Movement;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.Components.Pulling;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces;
using Content.Shared.Alert;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Players;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Mobs
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedAlertsComponent))]
    public sealed class ServerAlertsComponent : SharedAlertsComponent
    {

        private Dictionary<AlertKey, OnClickAlert> _alertClickCallbacks = new Dictionary<AlertKey, OnClickAlert>();

        protected override void Startup()
        {
            base.Startup();

            EntitySystem.Get<WeightlessSystem>().AddAlert(this);
        }

        public override void OnRemove()
        {
            EntitySystem.Get<WeightlessSystem>().RemoveAlert(this);

            base.OnRemove();
        }

        public override ComponentState GetComponentState()
        {
            return new AlertsComponentState(CreateAlertStatesArray());
        }

        public override void HandleNetworkMessage(ComponentMessage message, INetChannel netChannel, ICommonSession session = null)
        {
            base.HandleNetworkMessage(message, netChannel, session);

            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            switch (message)
            {
                case ClickAlertMessage msg:
                {
                    var player = session.AttachedEntity;

                    if (player != Owner)
                    {
                        break;
                    }

                    // TODO: Implement clicking other status effects in the HUD
                    if (AlertManager.TryDecode(msg.EncodedAlert, out var alert))
                    {
                        PerformAlertClickCallback(alert, player);
                    }
                    else
                    {
                        Logger.WarningS("alert", "unrecognized encoded alert {0}", msg.EncodedAlert);
                    }


                    break;
                }
            }
        }
    }
}
