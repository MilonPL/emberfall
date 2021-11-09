using System;
using System.Linq;
using Content.Client.Stylesheets;
using Content.Client.UserInterface;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using static Content.Shared.Chemistry.Components.SharedChemMasterComponent;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Chemistry.UI;

/// <summary>
/// Client-side UI used to control a <see cref="SharedChemMasterComponent"/>
/// </summary>
[GenerateTypedNameReferences]
public partial class ChemMasterWindow : SS14Window
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    public event Action<BaseButton.ButtonEventArgs, ChemButton>? OnChemButtonPressed;

    private static bool IsSpinValid(int n)
    {
        return n is > 0 and <= 10;
    }

    /// <summary>
    /// Create and initialize the chem master UI client-side. Creates the basic layout,
    /// actual data isn't filled in until the server sends data about the chem master.
    /// </summary>
    public ChemMasterWindow()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);

        PillAmount.IsValid = IsSpinValid;
        BottleAmount.IsValid = IsSpinValid;
        PillAmount.InitDefaultButtons();
        BottleAmount.InitDefaultButtons();
    }

    private ChemButton MakeChemButton(string text, FixedPoint2 amount, string id, bool isBuffer, string styleClass)
    {
        var button = new ChemButton(text, amount, id, isBuffer, styleClass);
        button.OnPressed += args
            => OnChemButtonPressed?.Invoke(args, button);
        return button;
    }

    /// <summary>
    /// Update the UI state when new state data is received from the server.
    /// </summary>
    /// <param name="state">State data sent by the server.</param>
    public void UpdateState(BoundUserInterfaceState state)
    {
        var castState = (ChemMasterBoundUserInterfaceState) state;
        Title = castState.DispenserName;
        UpdatePanelInfo(castState);
        if (Contents.Children != null)
        {
            ButtonHelpers.SetButtonDisabledRecursive(Contents, !castState.HasPower);
            EjectButton.Disabled = !castState.HasBeaker;
        }
    }

    /// <summary>
    /// Update the container, buffer, and packaging panels.
    /// </summary>
    /// <param name="state">State data for the dispenser.</param>
    private void UpdatePanelInfo(ChemMasterBoundUserInterfaceState state)
    {
        var bufferModeTransfer = state.BufferModeTransfer;
        BufferTransferButton.Pressed = bufferModeTransfer;
        BufferDiscardButton.Pressed = !bufferModeTransfer;

        ContainerInfo.Children.Clear();

        if (!state.HasBeaker)
        {
            ContainerInfo.Children.Add(new Label {Text = Loc.GetString("chem-master-window-no-container-loaded-text") });
        }
        else
        {
            ContainerInfo.Children.Add(new BoxContainer // Name of the container and its fill status (Ex: 44/100u)
            {
                Orientation = LayoutOrientation.Horizontal,
                Children =
                {
                    new Label {Text = $"{state.ContainerName}: "},
                    new Label
                    {
                        Text = $"{state.BeakerCurrentVolume}/{state.BeakerMaxVolume}",
                        StyleClasses = {StyleNano.StyleClassLabelSecondaryColor}
                    }
                }
            });
        }

        foreach (var reagent in state.ContainerReagents)
        {
            var name = Loc.GetString("chem-master-window-unknown-reagent-text");
            //Try to the prototype for the given reagent. This gives us it's name.
            if (_prototypeManager.TryIndex(reagent.ReagentId, out ReagentPrototype? proto))
            {
                name = proto.Name;
            }

            if (proto != null)
            {
                ContainerInfo.Children.Add(new BoxContainer
                {
                    Orientation = LayoutOrientation.Horizontal,
                    Children =
                    {
                        new Label {Text = $"{name}: "},
                        new Label
                        {
                            Text = $"{reagent.Quantity}u",
                            StyleClasses = {StyleNano.StyleClassLabelSecondaryColor}
                        },

                        //Padding
                        new Control {HorizontalExpand = true},

                        MakeChemButton("1", FixedPoint2.New(1), reagent.ReagentId, false, StyleBase.ButtonOpenRight),
                        MakeChemButton("5", FixedPoint2.New(5), reagent.ReagentId, false, StyleBase.ButtonOpenBoth),
                        MakeChemButton("10", FixedPoint2.New(10), reagent.ReagentId, false, StyleBase.ButtonOpenBoth),
                        MakeChemButton("25", FixedPoint2.New(25), reagent.ReagentId, false, StyleBase.ButtonOpenBoth),
                        MakeChemButton(Loc.GetString("chem-master-window-buffer-all-amount"), FixedPoint2.New(-1), reagent.ReagentId, false, StyleBase.ButtonOpenLeft),
                    }
                });
            }
        }

        BufferInfo.Children.Clear();

        if (!state.BufferReagents.Any())
        {
            BufferInfo.Children.Add(new Label {Text = Loc.GetString("chem-master-window-buffer-empty-text") });
            return;
        }

        var bufferHBox = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal
        };
        BufferInfo.AddChild(bufferHBox);

        var bufferLabel = new Label { Text = $"{Loc.GetString("chem-master-window-buffer-label")} " };
        bufferHBox.AddChild(bufferLabel);
        var bufferVol = new Label
        {
            Text = $"{state.BufferCurrentVolume}",
            StyleClasses = {StyleNano.StyleClassLabelSecondaryColor}
        };
        bufferHBox.AddChild(bufferVol);

        foreach (var reagent in state.BufferReagents)
        {
            var name = Loc.GetString("chem-master-window-unknown-reagent-text");
            //Try to the prototype for the given reagent. This gives us it's name.
            if (_prototypeManager.TryIndex(reagent.ReagentId, out ReagentPrototype? proto))
            {
                name = proto.Name;
            }

            if (proto != null)
            {
                BufferInfo.Children.Add(new BoxContainer
                {
                    Orientation = LayoutOrientation.Horizontal,
                    //SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
                    Children =
                    {
                        new Label {Text = $"{name}: "},
                        new Label
                        {
                            Text = $"{reagent.Quantity}u",
                            StyleClasses = {StyleNano.StyleClassLabelSecondaryColor}
                        },

                        //Padding
                        new Control {HorizontalExpand = true},

                        MakeChemButton("1", FixedPoint2.New(1), reagent.ReagentId, true, StyleBase.ButtonOpenRight),
                        MakeChemButton("5", FixedPoint2.New(5), reagent.ReagentId, true, StyleBase.ButtonOpenBoth),
                        MakeChemButton("10", FixedPoint2.New(10), reagent.ReagentId, true, StyleBase.ButtonOpenBoth),
                        MakeChemButton("25", FixedPoint2.New(25), reagent.ReagentId, true, StyleBase.ButtonOpenBoth),
                        MakeChemButton(Loc.GetString("chem-master-window-buffer-all-amount"), FixedPoint2.New(-1), reagent.ReagentId, true, StyleBase.ButtonOpenLeft),
                    }
                });
            }
        }
    }
}

public class ChemButton : Button
{
    public FixedPoint2 Amount { get; set; }
    public bool isBuffer = true;
    public string Id { get; set; }
    public ChemButton(string _text, FixedPoint2 _amount, string _id, bool _isBuffer, string _styleClass)
    {
        AddStyleClass(_styleClass);
        Text = _text;
        Amount = _amount;
        Id = _id;
        isBuffer = _isBuffer;
    }

}