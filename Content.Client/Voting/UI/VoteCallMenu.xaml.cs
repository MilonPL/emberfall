using System;
using System.Numerics;
using Content.Client.Stylesheets;
using Content.Shared.Administration;
using Content.Shared.Voting;
using JetBrains.Annotations;
using Robust.Client.AutoGenerated;
using Robust.Client.Console;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Client.Voting.UI
{
    [GenerateTypedNameReferences]
    public sealed partial class VoteCallMenu : BaseWindow
    {
        [Dependency] private readonly IClientConsoleHost _consoleHost = default!;
        [Dependency] private readonly IVoteManager _voteManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IClientNetManager _netManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly VotingSystem _votingSystem = default!;

        public (string name, StandardVoteType type, List<(string name, string id)>? secondaries, List<(string name, string id)>? tertiaries)[]
            AvailableVoteTypes =
            {
                ("ui-vote-type-restart", StandardVoteType.Restart, null, null),
                ("ui-vote-type-gamemode", StandardVoteType.Preset, null, null),
                ("ui-vote-type-map", StandardVoteType.Map, null, null),
                ("Votekick (set loc)", StandardVoteType.Votekick, null, null)
            };

        public List<(string name, string id)>? secondaries = [("test", "test2"), ("test3", "test4")];

        public List<(string name, string id)>? tertiaries = [("test5", "test6"), ("test7", "test8")];

        public VoteCallMenu()
        {
            IoCManager.InjectDependencies(this);
            RobustXamlLoader.Load(this);

            Stylesheet = IoCManager.Resolve<IStylesheetManager>().SheetSpace;
            CloseButton.OnPressed += _ => Close();

            for (var i = 0; i < AvailableVoteTypes.Length; i++)
            {
                var (text, _, _, _) = AvailableVoteTypes[i];
                VoteTypeButton.AddItem(Loc.GetString(text), i);
            }

            VoteTypeButton.OnItemSelected += VoteTypeSelected;
            VoteSecondButton.OnItemSelected += VoteSecondSelected;
            VoteThirdButton.OnItemSelected += VoteThirdSelected;
            CreateButton.OnPressed += CreatePressed;
        }

        protected override void Opened()
        {
            base.Opened();

            _netManager.ClientSendMessage(new MsgVoteMenu());

            _voteManager.CanCallVoteChanged += CanCallVoteChanged;
            _votingSystem.VotePlayerListResponse += UpdateVotePlayerList;
            _votingSystem.RequestVotePlayerList();

            AvailableVoteTypes[3] = ("Votekick (Loc required)", StandardVoteType.Votekick, secondaries, tertiaries); //TEMPORARY!!
        }

        public override void Close()
        {
            base.Close();

            _voteManager.CanCallVoteChanged -= CanCallVoteChanged;
            _votingSystem.VotePlayerListResponse -= UpdateVotePlayerList;
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);

            UpdateVoteTimeout();
        }

        private void CanCallVoteChanged(bool obj)
        {
            if (!obj)
                Close();
        }

        private void UpdateVotePlayerList(VotePlayerListResponseEvent msg)
        {
            List<(string name, string id)>? list = new();
            foreach ((NetUserId, string) player in msg.Players)
            {
                list.Add((player.Item2, player.Item1.ToString()));
            }
            secondaries = list;
        }

        private void CreatePressed(BaseButton.ButtonEventArgs obj)
        {
            var typeId = VoteTypeButton.SelectedId;
            var (_, typeKey, secondaries, tertiaries) = AvailableVoteTypes[typeId];

            if (tertiaries != null && secondaries != null)
            {
                var secondaryId = VoteSecondButton.SelectedId;
                var (_, secondKey) = secondaries[secondaryId];

                var tertiaryId = VoteThirdButton.SelectedId;
                var (_, thirdKey) = tertiaries[tertiaryId];

                _consoleHost.LocalShell.RemoteExecuteCommand($"createvote {typeKey} {secondKey} {thirdKey}");
            }
            else if (secondaries != null)
            {
                var secondaryId = VoteSecondButton.SelectedId;
                var (_, secondKey) = secondaries[secondaryId];

                _consoleHost.LocalShell.RemoteExecuteCommand($"createvote {typeKey} {secondKey}");
            }
            else
            {
                _consoleHost.LocalShell.RemoteExecuteCommand($"createvote {typeKey}");
            }

            Close();
        }

        private void UpdateVoteTimeout()
        {
            var (_, typeKey, _, _) = AvailableVoteTypes[VoteTypeButton.SelectedId];
            var isAvailable = _voteManager.CanCallStandardVote(typeKey, out var timeout);
            CreateButton.Disabled = !isAvailable;
            VoteTypeTimeoutLabel.Visible = !isAvailable;

            if (!isAvailable)
            {
                if (timeout == TimeSpan.Zero)
                {
                    VoteTypeTimeoutLabel.Text = Loc.GetString("ui-vote-type-not-available");
                }
                else
                {
                    var remaining = timeout - _gameTiming.RealTime;
                    VoteTypeTimeoutLabel.Text = Loc.GetString("ui-vote-type-timeout", ("remaining", remaining.ToString("mm\\:ss")));
                }
            }
        }

        private static void VoteSecondSelected(OptionButton.ItemSelectedEventArgs obj)
        {
            obj.Button.SelectId(obj.Id);
        }

        private static void VoteThirdSelected(OptionButton.ItemSelectedEventArgs obj)
        {
            obj.Button.SelectId(obj.Id);
        }

        private void VoteTypeSelected(OptionButton.ItemSelectedEventArgs obj)
        {
            VoteTypeButton.SelectId(obj.Id);

            var (_, _, options, options2) = AvailableVoteTypes[obj.Id];
            if (options == null)
            {
                VoteSecondButton.Visible = false;
            }
            else
            {
                VoteSecondButton.Visible = true;
                VoteSecondButton.Clear();

                for (var i = 0; i < options.Count; i++)
                {
                    var (text, _) = options[i];
                    VoteSecondButton.AddItem(Loc.GetString(text), i);
                }
            }

            if (options2 == null)
            {
                VoteThirdButton.Visible = false;
            }
            else
            {
                VoteThirdButton.Visible = true;
                VoteThirdButton.Clear();

                for (var i = 0; i < options2.Count; i++)
                {
                    var (text, _) = options2[i];
                    VoteThirdButton.AddItem(Loc.GetString(text), i);
                }
            }
        }

        protected override DragMode GetDragModeFor(Vector2 relativeMousePos)
        {
            return DragMode.Move;
        }
    }

    [UsedImplicitly, AnyCommand]
    public sealed class VoteMenuCommand : IConsoleCommand
    {
        public string Command => "votemenu";
        public string Description => Loc.GetString("ui-vote-menu-command-description");
        public string Help => Loc.GetString("ui-vote-menu-command-help-text");

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            new VoteCallMenu().OpenCentered();
        }
    }
}
