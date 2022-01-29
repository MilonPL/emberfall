﻿using Content.Shared.Administration;
using JetBrains.Annotations;
using Robust.Client.AutoGenerated;
using Robust.Client.Console;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;
using Robust.Shared.Players;

namespace Content.Client.Administration.UI.Tabs.AdminTab
{
    [GenerateTypedNameReferences]
    [UsedImplicitly]
    public partial class TeleportWindow : DefaultWindow
    {
        private PlayerInfo? _selectedPlayer;

        protected override void EnteredTree()
        {
            SubmitButton.OnPressed += SubmitButtonOnOnPressed;
            PlayerList.OnSelectionChanged += OnListOnOnSelectionChanged;
        }

        private void OnListOnOnSelectionChanged(PlayerInfo? obj)
        {
            _selectedPlayer = obj;
            SubmitButton.Disabled = _selectedPlayer == null;
        }

        private void SubmitButtonOnOnPressed(BaseButton.ButtonEventArgs obj)
        {
            if (_selectedPlayer == null)
                return;
            // Execute command
            IoCManager.Resolve<IClientConsoleHost>().ExecuteCommand(
                $"tpto \"{_selectedPlayer.Username}\"");
        }
    }
}
