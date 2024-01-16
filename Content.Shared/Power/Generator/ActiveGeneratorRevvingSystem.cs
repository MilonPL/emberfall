namespace Content.Shared.Power.Generator;

public sealed class ActiveGeneratorRevvingSystem: EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ActiveGeneratorRevvingComponent, AnchorStateChangedEvent>(OnAnchorStateChanged);
    }

    /// <summary>
    /// Handles the AnchorStateChangedEvent to stop auto-revving when unanchored.
    /// </summary>
    private void OnAnchorStateChanged(EntityUid uid, ActiveGeneratorRevvingComponent component, AnchorStateChangedEvent args)
    {
        if (!args.Anchored)
            StopAutoRevving(uid);
    }

    /// <summary>
    /// Start revving a generator entity automatically, without another entity doing a do-after.
    /// Used for remotely activating a generator.
    /// </summary>
    /// <param name="uid">Uid of the generator entity.</param>
    public void StartAutoRevving(EntityUid uid)
    {
        if (HasComp<ActiveGeneratorRevvingComponent>(uid))
            return;

        AddComp(uid, new ActiveGeneratorRevvingComponent());
    }

    /// <summary>
    /// Stop revving a generator entity.
    /// </summary>
    /// <param name="uid">Uid of the generator entity.</param>
    /// <returns>True if the auto-revving was cancelled, false if it was never revving in the first place.</returns>
    public bool StopAutoRevving(EntityUid uid)
    {
        return RemComp<ActiveGeneratorRevvingComponent>(uid);
    }

    /// <summary>
    /// Raise an event on a generator entity to start it.
    /// </summary>
    /// <remarks>This is not the same as revving it, when this is called the generator will start producing power.</remarks>
    /// <param name="uid">Uid of the generator entity.</param>
    private void StartGenerator(EntityUid uid)
    {
        RaiseLocalEvent(uid, new GeneratorStartedEvent());
    }

    /// <summary>
    /// Updates the timers on ActiveGeneratorRevvingComponent(s), and stops them when they are finished.
    /// </summary>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<ActiveGeneratorRevvingComponent, PortableGeneratorComponent>();

        while (query.MoveNext(out var uid, out var activeGeneratorRevvingComponent, out var portableGeneratorComponent))
        {
            activeGeneratorRevvingComponent.CurrentTime += TimeSpan.FromSeconds(frameTime);
            Dirty(uid, activeGeneratorRevvingComponent);

            if (activeGeneratorRevvingComponent.CurrentTime >= portableGeneratorComponent.StartTime)
            {
                StopAutoRevving(uid);
                StartGenerator(uid);
            }
        }
    }
}
