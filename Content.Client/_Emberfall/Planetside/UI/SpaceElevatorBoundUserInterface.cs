using Content.Shared._Emberfall.Planetside;

namespace Content.Client._Emberfall.Planetside.UI;

public sealed class SpaceElevatorBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private SpaceElevatorConsoleWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = new SpaceElevatorConsoleWindow(Owner);
        _window.OpenCentered();
        _window.OnClose += Close;
        _window.OnFTL += index => SendMessage(new DockingConsoleFTLMessage(index));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not SpaceElevatorConsoleState cast)
            return;

        _window?.UpdateState(cast);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
            _window?.Orphan();
    }
}
