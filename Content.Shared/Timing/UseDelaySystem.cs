using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Timing;

namespace Content.Shared.Timing;

public sealed class UseDelaySystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly MetaDataSystem _metadata = default!;

    private const string DefaultId = "default";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UseDelayComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<UseDelayComponent, EntityUnpausedEvent>(OnUnpaused);
    }

    private void OnMapInit(Entity<UseDelayComponent> ent, ref MapInitEvent args)
    {
        // Set default delay length from the prototype
        // This makes it easier for simple use cases that only need a single delay
        TryRegisterDelay(ent, DefaultId, ent.Comp.Delay);
    }

    private void OnUnpaused(Entity<UseDelayComponent> ent, ref EntityUnpausedEvent args)
    {
        // We have to do this manually, since it's not just a single field.
        foreach (var entry in ent.Comp.Delays.Values)
        {
            entry.EndTime += args.PausedTime;
        }
    }

    /// <summary>
    /// Adds an additional delay to by tracked by this component
    /// </summary>
    /// <param name="id">identifier used to refer to the delay in other methods</param>
    /// <param name="length">duration of the delay</param>
    /// <returns></returns>
    public bool TryRegisterDelay(Entity<UseDelayComponent> ent, string id, TimeSpan length)
    {
        // Make sure there's not already a delay registered with this ID
        if (ent.Comp.Delays.ContainsKey(id))
            return false;

        ent.Comp.Delays.Add(id, new UseDelayInfo(length));
        Dirty(ent);
        return true;
    }

    /// <summary>
    /// Sets the length of the delay with the specified ID.
    /// </summary>
    public bool SetLength(Entity<UseDelayComponent> ent, TimeSpan length, string id = DefaultId)
    {
        if (!ent.Comp.Delays.TryGetValue(id, out var entry))
            return false;

        if (entry.Length == length)
            return true;

        entry.Length = length;
        Dirty(ent);
        return true;
    }

    /// <summary>
    /// Returns true if the entity has a currently active UseDelay with the specified ID.
    /// </summary>
    public bool IsDelayed(Entity<UseDelayComponent> ent, string id = DefaultId)
    {
        if (!ent.Comp.Delays.TryGetValue(id, out var entry))
            return false;

        return entry.EndTime >= _gameTiming.CurTime;
    }

    /// <summary>
    /// Cancels the delay with the specified ID.
    /// </summary>
    public void CancelDelay(Entity<UseDelayComponent> ent, string id = DefaultId)
    {
        if (!ent.Comp.Delays.TryGetValue(id, out var entry))
            return;

        entry.EndTime = _gameTiming.CurTime;
        Dirty(ent);
    }

    /// <summary>
    /// Tries to get info about the delay with the specified ID. See <see cref="UseDelayInfo"/>.
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="info"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    public bool TryGetDelayInfo(Entity<UseDelayComponent> ent, [NotNullWhen(true)] out UseDelayInfo? info, string id = DefaultId)
    {
        return ent.Comp.Delays.TryGetValue(id, out info);
    }

    /// <summary>
    /// Returns info for the delay that will end farthest in the future.
    /// </summary>
    public UseDelayInfo GetLastEndingDelay(Entity<UseDelayComponent> ent)
    {
        var last = ent.Comp.Delays[DefaultId];
        foreach (var entry in ent.Comp.Delays)
        {
            if (entry.Value.EndTime > last.EndTime)
                last = entry.Value;
        }
        return last;
    }

    /// <summary>
    /// Resets the delay with the specified ID for this entity if possible.
    /// </summary>
    /// <param name="checkDelayed">Check if the entity has an ongoing delay with the specified ID.
    /// If it does, return false and don't reset it.
    /// Otherwise reset it and return true.</param>
    public bool TryResetDelay(Entity<UseDelayComponent> ent, bool checkDelayed = false, string id = DefaultId)
    {
        if (checkDelayed && IsDelayed(ent, id))
            return false;

        if (!ent.Comp.Delays.TryGetValue(id, out var entry))
            return false;

        var curTime = _gameTiming.CurTime;
        entry.StartTime = curTime;
        entry.EndTime = curTime - _metadata.GetPauseTime(ent) + entry.Length;
        Dirty(ent);
        return true;
    }

    public bool TryResetDelay(EntityUid uid, bool checkDelayed = false, UseDelayComponent? component = null, string id = DefaultId)
    {
        if (!Resolve(uid, ref component, false))
            return false;

        return TryResetDelay((uid, component), checkDelayed, id);
    }

    /// <summary>
    /// Resets all delays on the entity.
    /// </summary>
    public void ResetAllDelays(Entity<UseDelayComponent> ent)
    {
        var curTime = _gameTiming.CurTime;
        foreach (var entry in ent.Comp.Delays.Values)
        {
            entry.StartTime = curTime;
            entry.EndTime = curTime - _metadata.GetPauseTime(ent) + entry.Length;
        }
        Dirty(ent);
    }
}
