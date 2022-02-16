﻿using System;
using System.Collections.Generic;
using Content.Shared.CCVar;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Timing;

namespace Content.Server.Afk
{
    /// <summary>
    /// Tracks AFK (away from keyboard) status for players.
    /// </summary>
    /// <seealso cref="CCVars.AfkTime"/>
    public interface IAfkManager
    {
        /// <summary>
        /// Check whether this player is currently AFK.
        /// </summary>
        /// <param name="player">The player to check.</param>
        /// <returns>True if the player is AFK, false otherwise.</returns>
        bool IsAfk(IPlayerSession player);

        /// <summary>
        /// Resets AFK status for the player as if they just did an action and are definitely not AFK.
        /// </summary>
        /// <param name="player">The player to set AFK status for.</param>
        void PlayerDidAction(IPlayerSession player);

        void Initialize();
    }

    [UsedImplicitly]
    public sealed class AfkManager : IAfkManager, IEntityEventSubscriber
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IConsoleHost _consoleHost = default!;

        private readonly Dictionary<IPlayerSession, TimeSpan> _lastActionTimes = new();

        public void Initialize()
        {
            // Connecting, console commands and input commands all reset AFK status.

            _playerManager.PlayerStatusChanged += PlayerStatusChanged;
            _consoleHost.AnyCommandExecuted += ConsoleHostOnAnyCommandExecuted;

            _entityManager.EventBus.SubscribeSessionEvent<FullInputCmdMessage>(
                EventSource.Network,
                this,
                HandleInputCmd);
        }

        public void PlayerDidAction(IPlayerSession player)
        {
            if (player.Status == SessionStatus.Disconnected)
                // Make sure we don't re-add to the dictionary if the player is disconnected now.
                return;

            _lastActionTimes[player] = _gameTiming.RealTime;
        }

        public bool IsAfk(IPlayerSession player)
        {
            if (!_lastActionTimes.TryGetValue(player, out var time))
                // Some weird edge case like disconnected clients. Just say true I guess.
                return true;

            var timeOut = TimeSpan.FromSeconds(_cfg.GetCVar(CCVars.AfkTime));
            return _gameTiming.RealTime - time > timeOut;
        }

        private void PlayerStatusChanged(object? sender, SessionStatusEventArgs e)
        {
            if (e.NewStatus == SessionStatus.Disconnected)
            {
                _lastActionTimes.Remove(e.Session);
                return;
            }

            PlayerDidAction(e.Session);
        }

        private void ConsoleHostOnAnyCommandExecuted(IConsoleShell shell, string commandname, string argstr, string[] args)
        {
            if (shell.Player is IPlayerSession player)
                PlayerDidAction(player);
        }

        private void HandleInputCmd(FullInputCmdMessage msg, EntitySessionEventArgs args)
        {
            PlayerDidAction((IPlayerSession) args.SenderSession);
        }
    }
}
