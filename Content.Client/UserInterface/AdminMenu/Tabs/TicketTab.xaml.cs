﻿#nullable enable

using System;
using System.Collections.Generic;
using Content.Shared.Administration.AdminMenu;
using Content.Shared.Administration.Tickets;
using Robust.Client.AutoGenerated;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Content.Client.UserInterface.AdminMenu.Tabs
{
    [GenerateTypedNameReferences]
    public partial class TicketTab : Control
    {
        public delegate void TicketListRefresh();

        public event TicketListRefresh? OnTicketListRefresh;

        public TicketTab()
        {
            IoCManager.InjectDependencies(this);
            RobustXamlLoader.Load(this);
            RefreshButton.OnPressed += (_) => OnTicketListRefresh?.Invoke();
        }

        protected override void EnteredTree()
        {
            OnTicketListRefresh?.Invoke();
        }

        public void RefreshTicketList(IEnumerable<AdminMenuTicketListMessage.TicketInfo> tickets)
        {
            TicketList.RemoveAllChildren();

            var altColor = Color.FromHex("#292B38");
            var defaultColor = Color.FromHex("#2F2F3B");

            var header = new HBoxContainer
            {
                HorizontalExpand = true,
                SeparationOverride = 4,
                Children =
                {
                    new Label
                    {
                        Text = " Id", //Space for padding
                        SizeFlagsStretchRatio = 2f,
                        //HorizontalExpand = true,
                        MinWidth = 32
                    },
                    new TicketTab.VSeparator(),
                    new Label
                    {
                        Text = "Status",
                        SizeFlagsStretchRatio = 2f,
                        //HorizontalExpand = true,
                        MinWidth = 128
                    },
                    new TicketTab.VSeparator(),
                    new Label()
                    {
                        Text = "Action",
                        SizeFlagsStretchRatio = 2f,
                        //HorizontalExpand = true,
                        MinWidth = 64
                    },
                    new TicketTab.VSeparator(),
                    new Label()
                    {
                        Text = "Name",
                        SizeFlagsStretchRatio = 2f,
                        //HorizontalExpand = true,
                        MinWidth = 192
                    },
                    new TicketTab.VSeparator(),
                    new Label()
                    {
                        Text = "Message",
                        SizeFlagsStretchRatio = 2f,
                        HorizontalExpand = true,
                    }
                }
            };
            TicketList.AddChild(new PanelContainer
            {
                PanelOverride = new StyleBoxFlat
                {
                    BackgroundColor = altColor,
                },
                Children =
                {
                    header
                }
            });
            TicketList.AddChild(new TicketTab.HSeparator());

            var useAltColor = false;
            foreach (var ticket in tickets)
            {
                var hBox = new HBoxContainer
                {
                    HorizontalExpand = true,
                    SeparationOverride = 4,
                };
                var idLabel = new RichTextLabel
                {
                    MaxWidth = 32,
                    MinWidth = 32,
                };
                idLabel.SetMessage($" {ticket.Id.ToString()}"); //This is weird because the left edge needs a wee bit of padding
                hBox.AddChild(idLabel);
                hBox.AddChild(new VSeparator());
                var statusLabel = new RichTextLabel
                {
                    MaxWidth = 128,
                    MinWidth = 128
                };
                statusLabel.SetMessage(GetReadableStatus(ticket.Status));
                hBox.AddChild(statusLabel);
                hBox.AddChild(new VSeparator());
                var actionButton = new Button
                {
                    Text = "View",
                    MaxWidth = 64
                };
                hBox.AddChild(actionButton);
                hBox.AddChild(new VSeparator());
                var nameLabel = new RichTextLabel
                {
                    MaxWidth = 192,
                    MinWidth = 192
                };
                nameLabel.SetMessage(ticket.Name);
                hBox.AddChild(nameLabel);
                hBox.AddChild(new VSeparator());
                var messageLabel = new RichTextLabel();
                messageLabel.SetMessage(ticket.Message);
                hBox.AddChild(messageLabel);

                TicketList.AddChild(new PanelContainer
                {
                    PanelOverride = new StyleBoxFlat
                    {
                        BackgroundColor = useAltColor ? altColor : defaultColor,
                    },
                    Children =
                    {
                        hBox
                    }
                });
                useAltColor ^= true;
            }
        }

        private string GetReadableStatus(TicketStatus status)
        {
            switch (status)
            {
                case TicketStatus.Claimed:
                    return "Claimed";
                case TicketStatus.Unclaimed:
                    return "Unclaimed";
                case TicketStatus.Resolved:
                    return "Resolved";
                case TicketStatus.Closed:
                    return "Closed";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static readonly Color SeparatorColor = Color.FromHex("#3D4059");

        private class VSeparator : PanelContainer
        {
            public VSeparator()
            {
                MinSize = (2, 5);
                AddChild(new PanelContainer
                {
                    PanelOverride = new StyleBoxFlat
                    {
                        BackgroundColor = SeparatorColor
                    }
                });
            }
        }

        private class HSeparator : Control
        {
            public HSeparator()
            {
                AddChild(new PanelContainer
                {
                    PanelOverride = new StyleBoxFlat
                    {
                        BackgroundColor = SeparatorColor,
                        ContentMarginBottomOverride = 2, ContentMarginLeftOverride = 2
                    }
                });
            }
        }
    }
}
