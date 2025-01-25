namespace Content.Shared._RMC.Medical.CPR.Events;

// ReSharper disable InconsistentNaming
[ByRefEvent]
public record struct PerformCPRAttemptEvent(EntityUid Target, bool Cancelled = false);
