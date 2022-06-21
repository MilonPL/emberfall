using Content.Shared.Input;
using Content.Shared.Movement.Components;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Players;

namespace Content.Shared.Movement.EntitySystems;

public abstract partial class SharedMoverController
{
    /*
     * Handles all movement related inputs and subtick support.
     */

    public bool DiagonalMovementEnabled = true;

    private void InitializeInput()
    {
        var moveUpCmdHandler = new MoverDirInputCmdHandler(Direction.North);
        var moveLeftCmdHandler = new MoverDirInputCmdHandler(Direction.West);
        var moveRightCmdHandler = new MoverDirInputCmdHandler(Direction.East);
        var moveDownCmdHandler = new MoverDirInputCmdHandler(Direction.South);

        CommandBinds.Builder
            // Mob movement
            .Bind(EngineKeyFunctions.MoveUp, moveUpCmdHandler)
            .Bind(EngineKeyFunctions.MoveLeft, moveLeftCmdHandler)
            .Bind(EngineKeyFunctions.MoveRight, moveRightCmdHandler)
            .Bind(EngineKeyFunctions.MoveDown, moveDownCmdHandler)
            .Bind(EngineKeyFunctions.Walk, new WalkInputCmdHandler())
            // Shuttle
            .Bind(ContentKeyFunctions.ShuttleBrake, new ShuttleBrakeInputCmdHandler())

            .Register<SharedMoverController>();

        // TODO: Space brakes
    }

    private void ShutdownInput()
    {
        CommandBinds.Unregister<SharedMoverController>();
    }

    /// <summary>
    /// Handles subtick inputs with a bool field.
    /// </summary>
    private void SetBoolInput(MoverComponent component, ref bool fieldValue, bool value, ushort subTick)
    {
        ResetSubtickInput(component);

        // TODO: Okay so what we need to do is store the actual inpuy
        // somewhere for a whole tick value and adjust it
        if (TryGetSubtick(component, subTick, out var fraction))
        {
            var ev = new SubtickInputEvent(fraction);
            RaiseLocalEvent(component.Owner, ref ev);

            component._lastInputSubTick = subTick;
        }

        fieldValue = value;
    }

    /// <summary>
    /// Handles subtick inputs with a vector2 field.
    /// </summary>
    public void SetVectorInput(MoverComponent component, ref Vector2 value, ushort subTick)
    {
        ResetSubtickInput(component);

        // TODO: Okay so what we need to do is store the actual inpuy
        // somewhere for a whole tick value and adjust it
        if (subTick >= component._lastInputSubTick)
        {
            var fraction = (subTick - component._lastInputSubTick) / (float) ushort.MaxValue;

            var ev = new SubtickInputEvent(fraction);
            RaiseLocalEvent(component.Owner, ref ev);
        }
    }

    private void ResetSubtickInput(MoverComponent component)
    {
        // Reset the input if its last input was on a previous tick
        if (_gameTiming.CurTick <= component._lastInputTick) return;

        component._lastInputTick = _gameTiming.CurTick;
        component._lastInputSubTick = 0;
        // TODO: MobMover needs to reset walk / sprint vectors.
        var ev = new ResetSubtickInputEvent();
        RaiseLocalEvent(component.Owner, ref ev);
    }

    private bool TryGetSubtick(MoverComponent component, ushort subTick, out float fraction)
    {
        fraction = 0f;

        if (subTick < component._lastInputSubTick) return false;

        fraction = (subTick - component._lastInputSubTick) / (float) ushort.MaxValue;
        component._lastInputSubTick = subTick;
        return true;
    }

    private void HandleDirChange(ICommonSession? session, Direction dir, ushort subTick, bool state)
    {
        if (!TryGetAttachedComponent<IMoverComponent>(session, out var moverComp))
            return;

        SetVelocityDirection(dir, subTick, state);
    }

    /// <summary>
    ///     Toggles one of the four cardinal directions. Each of the four directions are
    ///     composed into a single direction vector, <see cref="VelocityDir"/>. Enabling
    ///     opposite directions will cancel each other out, resulting in no direction.
    /// </summary>
    /// <param name="direction">Direction to toggle.</param>
    /// <param name="subTick"></param>
    /// <param name="enabled">If the direction is active.</param>
    public void SetVelocityDirection(Direction direction, ushort subTick, bool enabled)
    {
        // Logger.Info($"[{_gameTiming.CurTick}/{subTick}] {direction}: {enabled}");

        var bit = direction switch
        {
            Direction.East => MoveButtons.Right,
            Direction.North => MoveButtons.Up,
            Direction.West => MoveButtons.Left,
            Direction.South => MoveButtons.Down,
            _ => throw new ArgumentException(nameof(direction))
        };

        SetMoveInput(subTick, enabled, bit);
    }

    private void SetMoveInput(ushort subTick, bool enabled, MoveButtons bit)
    {
        ResetSubtickInput();

        // TODO: Okay
        if (TryGetSubtick(, subTick, out var fraction))
        {
            ref var lastMoveAmount = ref Sprinting ? ref _curTickSprintMovement : ref _curTickWalkMovement;

            lastMoveAmount += DirVecForButtons(_heldMoveButtons) * fraction;

            _lastInputSubTick = subTick;
        }

        if (enabled)
        {
            _heldMoveButtons |= bit;
        }
        else
        {
            _heldMoveButtons &= ~bit;
        }

        // TODO: Dirty MobMover
        Dirty();
    }

    public void SetSprinting(ushort subTick, bool walking)
    {
        // Logger.Info($"[{_gameTiming.CurTick}/{subTick}] Sprint: {enabled}");

        SetMoveInput(subTick, walking, MoveButtons.Walk);
    }

    /// <summary>
    ///     Retrieves the normalized direction vector for a specified combination of movement keys.
    /// </summary>
    private Vector2 DirVecForButtons(MoveButtons buttons)
    {
        // key directions are in screen coordinates
        // _moveDir is in world coordinates
        // if the camera is moved, this needs to be changed

        var x = 0;
        x -= HasFlag(buttons, MoveButtons.Left) ? 1 : 0;
        x += HasFlag(buttons, MoveButtons.Right) ? 1 : 0;

        var y = 0;

        if (DiagonalMovementEnabled || x == 0)
        {
            y -= HasFlag(buttons, MoveButtons.Down) ? 1 : 0;
            y += HasFlag(buttons, MoveButtons.Up) ? 1 : 0;
        }

        var vec = new Vector2(x, y);

        // can't normalize zero length vector
        if (vec.LengthSquared > 1.0e-6)
        {
            // Normalize so that diagonals aren't faster or something.
            vec = vec.Normalized;
        }

        return vec;
    }


    private static bool HasFlag(MoveButtons buttons, MoveButtons flag)
    {
        return (buttons & flag) == flag;
    }

    private sealed class MoverDirInputCmdHandler : InputCmdHandler
    {
        private IEntityManager _entManager;

        private readonly Direction _dir;
        private readonly Vector2 _direction;

        public MoverDirInputCmdHandler(Direction dir)
        {
            _dir = dir;
        }

        public override bool HandleCmdMessage(ICommonSession? session, InputCmdMessage message)
        {
            if (message is not FullInputCmdMessage full ||
                !_entManager.TryGetComponent<MoverComponent>(session?.AttachedEntity, out var mover))
                return false;

            Get<SharedMoverController>().SetVectorInput(mover, _direction, message.SubTick);

            Get<SharedMoverController>().HandleDirChange(session, _dir, message.SubTick, full.State == BoundKeyState.Down);
            return false;
        }
    }

    private sealed class WalkInputCmdHandler : InputCmdHandler
    {
        private IEntityManager _entManager;

        public WalkInputCmdHandler(IEntityManager entManager)
        {
            _entManager = entManager;
        }

        public override bool HandleCmdMessage(ICommonSession? session, InputCmdMessage message)
        {
            if (message is not FullInputCmdMessage full ||
                !_entManager.TryGetComponent<MobMoverComponent>(session?.AttachedEntity, out var mobMover))
            {
                return false;
            }

            var sprinting = mobMover.Sprinting;

            Get<SharedMoverController>().SetBoolInput(mobMover, ref sprinting, full.State == BoundKeyState.Down, message.SubTick);
            return false;
        }
    }

    private sealed class ShuttleBrakeInputCmdHandler : InputCmdHandler
    {
        public override bool HandleCmdMessage(ICommonSession? session, InputCmdMessage message)
        {
            if (message is not FullInputCmdMessage full) return false;

            // TODO: Set shuttle stuffsies.


            return false;
        }
    }

    /// <summary>
    /// Raised on an entity whenever a new subtick input comes in.
    /// </summary>
    public readonly struct SubtickInputEvent
    {
        public readonly float Fraction;

        public SubtickInputEvent(float fraction)
        {
            Fraction = fraction;
        }
    }

    /// <summary>
    /// Raised on an entity when its subtick inputs get reset.
    /// </summary>
    [ByRefEvent]
    public readonly struct ResetSubtickInputEvent {}
}
