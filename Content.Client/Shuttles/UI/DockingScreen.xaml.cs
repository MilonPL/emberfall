using System.Text;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Systems;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Map;

namespace Content.Client.Shuttles.UI;

[GenerateTypedNameReferences]
public sealed partial class DockingScreen : BoxContainer
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    private readonly SharedShuttleSystem _shuttles = default!;

    /// <summary>
    /// Stored by GridID then by docks
    /// </summary>
    public Dictionary<NetEntity, List<DockingPortState>> Docks = new();

    public DockingScreen()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);
        _shuttles = _entManager.System<SharedShuttleSystem>();
    }

    public void UpdateState(EntityUid? shuttle, DockingInterfaceState state)
    {
        Docks = state.Docks;
        DockingControl.DockState = state;
        DockingControl.GridEntity = shuttle;
        BuildDocks(shuttle);
    }

    private void BuildDocks(EntityUid? shuttle)
    {
        var currentDock = DockingControl.ViewedDock;

        DockPorts.DisposeAllChildren();

        if (shuttle == null)
        {
            DockingControl.ViewedDock = null;
            return;
        }

        var shuttleNent = _entManager.GetNetEntity(shuttle.Value);

        if (Docks.TryGetValue(shuttleNent, out var shuttleDocks))
        {
            var buttonGroup = new ButtonGroup();
            var idx = 0;

            foreach (var dock in shuttleDocks)
            {
                idx++;
                var dockText = new StringBuilder();
                dockText.Append(Loc.GetString("shuttle-console-dock") + $" {idx}");
                var dockGrid = _entManager.GetEntity(dock.GridDockedWith);

                if (_entManager.EntityExists(dockGrid))
                {
                    var iffLabel = _shuttles.GetIFFLabel(dockGrid.Value) ?? Loc.GetString("shuttle-console-unknown");
                    dockText.AppendLine(iffLabel);

                    dockText.AppendLine(dockText.ToString());
                }

                var button = new Button()
                {
                    Text = dockText.ToString(),
                    ToggleMode = true,
                    Group = buttonGroup,
                    Margin = new Thickness(3f),
                };

                button.OnMouseEntered += args =>
                {
                    DockingControl.HighlightedDock = dock.Entity;
                };

                button.Label.Margin = new Thickness(3f);

                if (dock.Connected)
                {
                    button.Text += "\n";
                }

                if (currentDock == dock.Entity)
                {
                    button.Pressed = true;
                }

                button.OnPressed += args =>
                {
                    OnDockPress(args, dock.Entity, dock.Coordinates, dock.Angle);
                };

                DockPorts.AddChild(button);
            }
        }
    }

    private void OnDockPress(BaseButton.ButtonEventArgs args, NetEntity entity, NetCoordinates? coordinates, Angle angle)
    {
        DockingControl.ViewedDock = entity;
        DockingControl.Coordinates = _entManager.GetCoordinates(coordinates);
        DockingControl.Angle = angle;
    }
}
