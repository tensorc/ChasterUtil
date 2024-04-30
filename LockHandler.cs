using ChasterSharp;

namespace ChasterUtil;

public abstract class LockHandler(string? bearerToken = null) : IEquatable<LockHandler>
{
    public abstract string Name { get; }

    public ChasterProcessor Processor { get; internal set; }

    public string BearerToken => bearerToken ?? string.Empty;

    public IReadOnlyList<LockInstance> Instances { get; private set; } = [];

    internal void Invalidate()
    {
        Instances = Processor.GetLockHandlerInstances(this, BearerToken);
    }

    public virtual Task OnHandlerEnter()
    {
        return Task.CompletedTask;
    }

    public virtual Task OnHandlerExit()
    {
        return Task.CompletedTask;
    }

    public virtual Task OnHandlerUpdate()
    {
        return Task.CompletedTask;
    }

    public virtual Task OnProcessingStarted(LockInstance lockInstance)
    {
        return Task.CompletedTask;
    }

    public virtual Task OnProcessingCompleted(LockInstance lockInstance)
    {
        return Task.CompletedTask;
    }

    #region Lock Actions

    public virtual Task OnLocked(LockInstance lockInstance, LogData logData)
    {
        return Task.CompletedTask;
    }

    public virtual Task OnUnlocked(LockInstance lockInstance, LogData logData)
    {
        return Task.CompletedTask;
    }

    public virtual Task OnDeserted(LockInstance lockInstance, LogData logData)
    {
        return Task.CompletedTask;
    }

    public virtual Task OnKeyholderTrusted(LockInstance lockInstance, LogData logData)
    {
        return Task.CompletedTask;
    }

    public virtual Task OnSessionOfferAccepted(LockInstance lockInstance, LogData logData)
    {
        return Task.CompletedTask;
    }

    public virtual Task OnMaxLimitDateRemoved(LockInstance lockInstance, LogData logData)
    {
        return Task.CompletedTask;
    }

    public virtual Task OnMaxLimitDateIncreased(LockInstance lockInstance, LogData logData, LogMaxLimitDateIncreasedPayload payload)
    {
        return Task.CompletedTask;
    }

    public virtual Task OnLockFrozen(LockInstance lockInstance, LogData logData)
    {
        return Task.CompletedTask;
    }

    public virtual Task OnLockUnfrozen(LockInstance lockInstance, LogData logData)
    {
        return Task.CompletedTask;
    }

    public virtual Task OnTimerHidden(LockInstance lockInstance, LogData logData)
    {
        return Task.CompletedTask;
    }

    public virtual Task OnTimerRevealed(LockInstance lockInstance, LogData logData)
    {
        return Task.CompletedTask;
    }

    public virtual Task OnTimeLogsHidden(LockInstance lockInstance, LogData logData)
    {
        return Task.CompletedTask;
    }

    public virtual Task OnTimeLogsRevealed(LockInstance lockInstance, LogData logData)
    {
        return Task.CompletedTask;
    }

    public virtual Task OnExtensionUpdated(LockInstance lockInstance, LogData logData, LogExtensionUpdatedPayload payload)
    {
        return Task.CompletedTask;
    }

    public virtual Task OnExtensionEnabled(LockInstance lockInstance, LogData logData, LogExtensionEnabledPayload payload)
    {
        return Task.CompletedTask;
    }

    public virtual Task OnExtensionDisabled(LockInstance lockInstance, LogData logData, LogExtensionDisabledPayload payload)
    {
        return Task.CompletedTask;
    }

    public virtual Task OnTimeChanged(LockInstance lockInstance, LogData logData, LogTimeChangedPayload payload)
    {
        return Task.CompletedTask;
    }

    public virtual Task OnLinkTimeChanged(LockInstance lockInstance, LogData logData, LogTimeChangedPayload payload)
    {
        return Task.CompletedTask;
    }

    public virtual Task OnDiceRolled(LockInstance lockInstance, LogData logData, LogDiceRolledPayload payload)
    {
        return Task.CompletedTask;
    }

    public virtual Task OnTaskAssigned(LockInstance lockInstance, LogData logData, AssignTaskActionModel payload)
    {
        return Task.CompletedTask;
    }

    public virtual Task OnTaskCompleted(LockInstance lockInstance, LogData logData, LogTaskResultPayload payload)
    {
        return Task.CompletedTask;
    }

    public virtual Task OnTaskFailed(LockInstance lockInstance, LogData logData, LogTaskResultPayload payload)
    {
        return Task.CompletedTask;
    }

    public virtual Task OnTasksVoteEnded(LockInstance lockInstance, LogData logData, LogTaskVoteEndedPayload payload)
    {
        return Task.CompletedTask;
    }

    public virtual Task OnTemporaryOpeningOpened(LockInstance lockInstance, LogData logData, LogTemporaryOpeningOpenedPayload payload)
    {
        return Task.CompletedTask;
    }

    public virtual Task OnTemporaryOpeningLocked(LockInstance lockInstance, LogData logData, LogTemporaryOpeningLockedPayload payload)
    {
        return Task.CompletedTask;
    }

    public virtual Task OnTemporaryOpeningLockedLate(LockInstance lockInstance, LogData logData, LogTemporaryOpeningLockedPayload payload)
    {
        return Task.CompletedTask;
    }

    public virtual Task OnVerificationPictureSubmitted(LockInstance lockInstance, LogData logData, LogVerificationPictureSubmittedPayload payload)
    {
        return Task.CompletedTask;
    }

    public virtual Task OnPilloryStarted(LockInstance lockInstance, LogData logData, LogPilloryStartedPayload payload)
    {
        return Task.CompletedTask;
    }

    public virtual Task OnPilloryEnded(LockInstance lockInstance, LogData logData, LogPilloryEndedPayload payload)
    {
        return Task.CompletedTask;
    }

    public virtual Task OnWheelOfFortuneTurned(LockInstance lockInstance, LogData logData, WheelOfFortuneSegment payload)
    {
        return Task.CompletedTask;
    }

    public virtual Task OnTimerGuessed(LockInstance lockInstance, LogData logData)
    {
        return Task.CompletedTask;
    }

    #endregion

    #region Equality

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is LockHandler other && Name == other.Name && BearerToken == other.BearerToken;
    }

    public bool Equals(LockHandler? other)
    {
        return ReferenceEquals(this, other) || other is not null && Name == other.Name && BearerToken == other.BearerToken;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name, BearerToken);
    }

    public static bool operator ==(LockHandler? obj1, LockHandler? obj2)
    {
        if (ReferenceEquals(obj1, obj2))
            return true;

        if (obj1 is null || obj2 is null)
            return false;

        return obj1.Equals(obj2);
    }

    public static bool operator !=(LockHandler? obj1, LockHandler? obj2)
    {
        return !(obj1 == obj2);
    }

    #endregion
}