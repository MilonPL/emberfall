using Content.Client.Examine;
using Content.Client.Hands.Systems;
using Content.Client.Interaction;
using Content.Client.Storage.Systems;
using Content.Client.UserInterface.Systems.Hotbar.Widgets;
using Content.Client.UserInterface.Systems.Storage.Controls;
using Content.Client.Verbs.UI;
using Content.Shared.CCVar;
using Content.Shared.Input;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Storage;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Input;
using Robust.Shared.Timing;

namespace Content.Client.UserInterface.Systems.Storage;

public sealed class StorageUIController : UIController, IOnSystemChanged<StorageSystem>
{
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IInputManager _input = default!;
    [UISystemDependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;

    private readonly DragDropHelper<ItemGridPiece> _menuDragHelper;

    public ItemGridPiece? DraggingGhost;
    public Angle DraggingRotation = Angle.Zero;
    public bool StaticStorageUIEnabled;
    public bool OpaqueStorageWindow;

    public bool IsDragging => _menuDragHelper.IsDragging;
    public ItemGridPiece? CurrentlyDragging => _menuDragHelper.Dragged;

    /*
     * TODO:
     * - Fix the BRRT when opening or w/e it was
     * - Fix the back button so it goes up nested, nested should open at the same spot.
     * - Add cvar to allow nesting or not
     * - Don't forget static attach to hotbargui or whatever, also dear god fix it getting bumped PLEASE.
     */

    public StorageUIController()
    {
        _menuDragHelper = new DragDropHelper<ItemGridPiece>(OnMenuBeginDrag, OnMenuContinueDrag, OnMenuEndDrag);
    }

    public override void Initialize()
    {
        base.Initialize();

        _configuration.OnValueChanged(CCVars.StaticStorageUI, OnStaticStorageChanged, true);
        //_configuration.OnValueChanged(CCVars.OpaqueStorageWindow, OnOpaqueWindowChanged, true);
    }

    private void OnStaticStorageChanged(bool obj)
    {
        StaticStorageUIEnabled = obj;
    }

    public StorageWindow CreateStorageWindow()
    {
        var window = new StorageWindow();
        window.MouseFilter = Control.MouseFilterMode.Pass;

        window.OnPiecePressed += (args, piece) =>
        {
            OnPiecePressed(args, window, piece);
        };
        window.OnPieceUnpressed += (args, piece) =>
        {
            OnPieceUnpressed(args, window, piece);
        };

        if (StaticStorageUIEnabled)
        {
            UIManager.GetActiveUIWidgetOrNull<HotbarGui>()?.StorageContainer.AddChild(window);
        }
        else
        {
            window.OpenCenteredLeft();
        }

        return window;
    }

    public void OnSystemLoaded(StorageSystem system)
    {
        _input.FirstChanceOnKeyEvent += OnMiddleMouse;
    }

    public void OnSystemUnloaded(StorageSystem system)
    {
        _input.FirstChanceOnKeyEvent -= OnMiddleMouse;
    }

    /// One might ask, Hey Emo, why are you parsing raw keyboard input just to rotate a rectangle?
    /// The answer is, that input bindings regarding mouse inputs are always intercepted by the UI,
    /// thus, if i want to be able to rotate my damn piece anywhere on the screen,
    /// I have to side-step all of the input handling. Cheers.
    private void OnMiddleMouse(KeyEventArgs keyEvent, KeyEventType type)
    {
        if (keyEvent.Handled)
            return;

        if (type != KeyEventType.Down)
            return;

        //todo there's gotta be a method for this in InputManager just expose it to content I BEG.
        if (!_input.TryGetKeyBinding(ContentKeyFunctions.RotateStoredItem, out var binding))
            return;
        if (binding.BaseKey != keyEvent.Key)
            return;

        if (keyEvent.Shift &&
            !(binding.Mod1 == Keyboard.Key.Shift ||
              binding.Mod2 == Keyboard.Key.Shift ||
              binding.Mod3 == Keyboard.Key.Shift))
            return;

        if (keyEvent.Alt &&
            !(binding.Mod1 == Keyboard.Key.Alt ||
              binding.Mod2 == Keyboard.Key.Alt ||
              binding.Mod3 == Keyboard.Key.Alt))
            return;

        if (keyEvent.Control &&
            !(binding.Mod1 == Keyboard.Key.Control ||
              binding.Mod2 == Keyboard.Key.Control ||
              binding.Mod3 == Keyboard.Key.Control))
            return;

        if (!IsDragging && EntityManager.System<HandsSystem>().GetActiveHandEntity() == null)
            return;

        //clamp it to a cardinal.
        DraggingRotation = (DraggingRotation + Math.PI / 2f).GetCardinalDir().ToAngle();
        if (DraggingGhost != null)
            DraggingGhost.Location.Rotation = DraggingRotation;

        if (IsDragging || UIManager.CurrentlyHovered is StorageWindow)
            keyEvent.Handle();
    }

    private void OnPiecePressed(GUIBoundKeyEventArgs args, StorageWindow window, ItemGridPiece control)
    {
        if (IsDragging || !window.IsOpen)
            return;

        if (args.Function == ContentKeyFunctions.MoveStoredItem)
        {
            DraggingRotation = control.Location.Rotation;
            _menuDragHelper.MouseDown(control);
            _menuDragHelper.Update(0f);

            args.Handle();
        }
        else if (args.Function == ContentKeyFunctions.SaveItemLocation)
        {
            if (window.StorageEntity is not {} storage)
                return;

            EntityManager.RaisePredictiveEvent(new StorageSaveItemLocationEvent(
                EntityManager.GetNetEntity(control.Entity),
                EntityManager.GetNetEntity(storage)));
            args.Handle();
        }
        else if (args.Function == ContentKeyFunctions.ExamineEntity)
        {
            EntityManager.System<ExamineSystem>().DoExamine(control.Entity);
            args.Handle();
        }
        else if (args.Function == EngineKeyFunctions.UseSecondary)
        {
            UIManager.GetUIController<VerbMenuUIController>().OpenVerbMenu(control.Entity);
            args.Handle();
        }
        else if (args.Function == ContentKeyFunctions.ActivateItemInWorld)
        {
            EntityManager.RaisePredictiveEvent(
                new InteractInventorySlotEvent(EntityManager.GetNetEntity(control.Entity), altInteract: false));
            args.Handle();
        }
        else if (args.Function == ContentKeyFunctions.AltActivateItemInWorld)
        {
            EntityManager.RaisePredictiveEvent(new InteractInventorySlotEvent(EntityManager.GetNetEntity(control.Entity), altInteract: true));
            args.Handle();
        }
    }

    private void OnPieceUnpressed(GUIBoundKeyEventArgs args, StorageWindow window, ItemGridPiece control)
    {
        if (args.Function != ContentKeyFunctions.MoveStoredItem)
            return;

        if (window.StorageEntity is not { } sourceStorage ||
            !EntityManager.TryGetComponent<StorageComponent>(sourceStorage, out var storageComp))
        {
            _menuDragHelper.EndDrag();
            return;
        }

        var targetStorage = UIManager.CurrentlyHovered as StorageWindow;

        if (DraggingGhost is { } draggingGhost)
        {
            var dragEnt = draggingGhost.Entity;
            var dragLoc = draggingGhost.Location;

            var position = window.GetMouseGridPieceLocation(dragEnt, dragLoc);

            // Dragging in the same storage
            // The existing ItemGridPiece just stops rendering but still exists so check if it's hovered.
            if (targetStorage == window || UIManager.CurrentlyHovered == control)
            {
                EntityManager.RaisePredictiveEvent(new StorageSetItemLocationEvent(
                    EntityManager.GetNetEntity(draggingGhost.Entity),
                    EntityManager.GetNetEntity(sourceStorage),
                    new ItemStorageLocation(DraggingRotation, position)));
            }
            // Dragging to new storage
            else if (targetStorage?.StorageEntity != null && targetStorage != window)
            {
                /*
                EntityManager.RaisePredictiveEvent(new StorageSetItemLocationEvent(
                    EntityManager.GetNetEntity(draggingGhost.Entity),
                    EntityManager.GetNetEntity(targetStorage.StorageEntity.Value),
                    new ItemStorageLocation(DraggingRotation, position)));
                    */
            }
            else
            {
                EntityManager.RaisePredictiveEvent(new StorageRemoveItemEvent(
                    EntityManager.GetNetEntity(draggingGhost.Entity),
                    EntityManager.GetNetEntity(sourceStorage)));
            }

            _menuDragHelper.EndDrag();
            window.BuildItemPieces();
        }
        else //if we just clicked, then take it out of the bag.
        {
            _menuDragHelper.EndDrag();
            EntityManager.RaisePredictiveEvent(new StorageInteractWithItemEvent(
                EntityManager.GetNetEntity(control.Entity),
                EntityManager.GetNetEntity(sourceStorage)));
        }

        args.Handle();
    }

    private bool OnMenuBeginDrag()
    {
        if (_menuDragHelper.Dragged is not { } dragged)
            return false;

        DraggingGhost = new ItemGridPiece(
            (dragged.Entity, EntityManager.GetComponent<ItemComponent>(dragged.Entity)),
            dragged.Location,
            EntityManager);
        DraggingGhost.MouseFilter = Control.MouseFilterMode.Ignore;
        DraggingGhost.Visible = true;
        DraggingRotation = dragged.Location.Rotation;

        UIManager.PopupRoot.AddChild(DraggingGhost);
        SetDraggingRotation();
        return true;
    }

    private bool OnMenuContinueDrag(float frameTime)
    {
        if (DraggingGhost == null)
            return false;

        SetDraggingRotation();
        return true;
    }

    private void SetDraggingRotation()
    {
        if (DraggingGhost == null)
            return;

        var offset = ItemGridPiece.GetCenterOffset(
            (DraggingGhost.Entity, null),
            new ItemStorageLocation(DraggingRotation, Vector2i.Zero),
            EntityManager);

        // I don't know why it divides the position by 2. Hope this helps! -emo
        LayoutContainer.SetPosition(DraggingGhost, UIManager.MousePositionScaled.Position / 2 - offset );
    }

    private void OnMenuEndDrag()
    {
        if (DraggingGhost == null)
            return;

        DraggingGhost.Orphan();
        DraggingGhost = null;
        DraggingRotation = Angle.Zero;
    }

    public override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        _menuDragHelper.Update(args.DeltaSeconds);
    }
}
