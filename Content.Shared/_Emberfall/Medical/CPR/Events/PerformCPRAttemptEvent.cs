namespace Content.Shared._Emberfall.Medical.CPR.Events;

// ReSharper disable InconsistentNaming
[ByRefEvent]
public record struct PerformCPRAttemptEvent(EntityUid Target, bool Cancelled = false);
