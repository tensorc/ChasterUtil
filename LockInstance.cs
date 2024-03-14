using ChasterSharp;

namespace ChasterUtil;

public sealed class LockInstance
{

    #region Properties

    public bool IsFrozen { get; set; }

    public bool DisplayRemainingTime { get; set; }

    public bool HideTimeLogs { get; set; }

    public bool IsLocked { get; private set; }

    public bool IsArchived { get; private set; }

    public bool IsTrusted { get; private set; } 

    public bool IsKeyholderLock => _lock.Role == LockRole.Keyholder;

    public bool HasTimeLimit => MaxLimitDateTime.HasValue;

    public TimeSpan TimeLocked => ServerDateTime - _lock.StartDate;

    public TimeSpan? TimeFrozen => ServerDateTime - _lock.FrozenAt;

    public TimeSpan? TimeRemaining => GetTimeRemaining();

    public DateTimeOffset ServerDateTime => new(DateTime.UtcNow, _lock.StartDate.Offset);

    public DateTimeOffset? MaxLimitDateTime { get; private set; }

    public DateTimeOffset? ArchivedDateTime => IsKeyholderLock ? _lock.KeyholderArchivedAt : _lock.ArchivedAt;

    public Lock Lock => _lock;

    public string LockId => _lock.Id;

    internal string Token { get; }

    internal string TokenId { get; }

    internal ChasterUtil Util { get; }

    #endregion

    #region Extensions

    public DiceExtension Dice { get; }

    public GuessTheTimerExtension GuessTheTimer { get; }

    public HygieneOpeningExtension HygieneOpening { get; }

    public PilloryExtension Pillory { get; }

    public RandomEventsExtension RandomEvents { get; }

    public ShareLinkExtension ShareLink { get; }

    public WheelOfFortuneExtension WheelOfFortune { get; }

    public VerificationPictureExtension VerificationPicture { get; }

    public TasksExtension Tasks { get; }

    public PenaltiesExtension Penalties { get; }

    #endregion

    #region Members

    private TimeSpan _timeAdjustment;
 
    private readonly Lock _lock;

    #endregion

    public LockInstance(ChasterUtil chasterUtil, Lock @lock, string token)
    {
        _lock = @lock;

        Util = chasterUtil;
        Token = token;
        TokenId = Util.GetBearerTokenId(Token);
        IsLocked = @lock.Status == LockStatus.Locked;
        IsArchived = ArchivedDateTime.HasValue;
        IsFrozen = @lock.IsFrozen;
        DisplayRemainingTime = @lock.DisplayRemainingTime;
        HideTimeLogs = @lock.HideTimeLogs;
        IsTrusted = @lock.Trusted;
        MaxLimitDateTime = @lock.MaxLimitDate;

        Dice = new DiceExtension(@lock.Extensions.FirstOrDefault(x => x.GetExtensionSlug() == ExtensionSlug.Dice), this);
        GuessTheTimer = new GuessTheTimerExtension(@lock.Extensions.FirstOrDefault(x => x.GetExtensionSlug() == ExtensionSlug.GuessTheTimer), this);
        HygieneOpening = new HygieneOpeningExtension(@lock.Extensions.FirstOrDefault(x => x.GetExtensionSlug() == ExtensionSlug.HygieneOpening), this);
        Pillory = new PilloryExtension(@lock.Extensions.FirstOrDefault(x => x.GetExtensionSlug() == ExtensionSlug.Pillory), this);
        RandomEvents = new RandomEventsExtension(@lock.Extensions.FirstOrDefault(x => x.GetExtensionSlug() == ExtensionSlug.RandomEvents), this);
        ShareLink = new ShareLinkExtension(@lock.Extensions.FirstOrDefault(x => x.GetExtensionSlug() == ExtensionSlug.ShareLink), this);
        WheelOfFortune = new WheelOfFortuneExtension(@lock.Extensions.FirstOrDefault(x => x.GetExtensionSlug() == ExtensionSlug.WheelOfFortune), this);
        VerificationPicture = new VerificationPictureExtension(@lock.Extensions.FirstOrDefault(x => x.GetExtensionSlug() == ExtensionSlug.VerificationPicture), this);
        Tasks = new TasksExtension(@lock.Extensions.FirstOrDefault(x => x.GetExtensionSlug() == ExtensionSlug.Tasks), this);
        Penalties = new PenaltiesExtension(@lock.Extensions.FirstOrDefault(x => x.GetExtensionSlug() == ExtensionSlug.Penalties), this);
    }

    public void Unlock()
    {
        if (!IsKeyholderLock)
            return;

        IsLocked = false;
    }

    public void Archive()
    {
        if(IsKeyholderLock)
            IsLocked = false;

        IsArchived = true;
    }

    public void AddTime(TimeSpan timeToAdd)
    {
        _timeAdjustment += timeToAdd;
    }

    public void RemoveTime(TimeSpan timeToRemove)
    {
        if (!IsKeyholderLock)
            return;

        _timeAdjustment -= timeToRemove;
    }

    public void TrustKeyholder()
    {
        if (IsKeyholderLock)
            return;

        IsTrusted = true;
    }

    public void IncreaseMaxLimitDate(TimeSpan duration)
    {
        if (IsKeyholderLock || !HasTimeLimit)
            return;

        MaxLimitDateTime += duration;
    }

    public void RemoveMaxLimitDate()
    {
        if (IsKeyholderLock)
            return;

        MaxLimitDateTime = null;
    }

    private TimeSpan? GetTimeRemaining()
    {
       var timeRemaining = _lock.EndDate - (_lock.IsFrozen ? _lock.FrozenAt : ServerDateTime) + _timeAdjustment;

       if (MaxLimitDateTime.HasValue)
       {
           var maxTimeRemaining = MaxLimitDateTime - ServerDateTime;

           if(maxTimeRemaining < timeRemaining)
               timeRemaining = maxTimeRemaining;
       }

       return timeRemaining is { TotalSeconds: < 0 } ? TimeSpan.Zero : timeRemaining;
    }

    private ChasterExtension[] GetExtensions()
    {
        return [Dice, GuessTheTimer, HygieneOpening, Pillory, RandomEvents, ShareLink, WheelOfFortune, VerificationPicture, Tasks, Penalties];
    }

    public void CommitUpdates()
    {
        if (_lock.Status == LockStatus.Locked && !IsLocked)
            Util.LogUnlockAction(this);

        if (IsKeyholderLock)
        {
            if (_lock.KeyholderArchivedAt.HasValue != IsArchived && !IsLocked)
                Util.LogArchiveAction(this);
        }
        else
        {
            if (_lock.ArchivedAt.HasValue != IsArchived)
                Util.LogArchiveAction(this);
        }
        
        if (!IsLocked)
            return;

        CommitLockUpdates();
        CommitExtensionUpdates(); 
        CommitTaskUpdates();
    }

    private void CommitLockUpdates()
    {
        if (IsKeyholderLock)
        {
            if (IsFrozen != _lock.IsFrozen)
                Util.LogUpdateFreezeAction(this, IsFrozen);

            if (_lock.DisplayRemainingTime != DisplayRemainingTime || _lock.HideTimeLogs != HideTimeLogs)
                Util.LogUpdateSettingsAction(this, DisplayRemainingTime, HideTimeLogs);
        }
        else
        {
            if(IsTrusted != _lock.Trusted)
                Util.LogTrustKeyholderAction(this);

            if (MaxLimitDateTime != _lock.MaxLimitDate)
                Util.LogUpdateMaxTimeLimitAction(this, MaxLimitDateTime);
        }

        if ((int)_timeAdjustment.TotalSeconds != 0)
            Util.LogAddRemoveTimeAction(this, _timeAdjustment);
    }

    private void CommitExtensionUpdates()
    {
        if (!IsTrusted || !IsKeyholderLock)
            return;

        var extensions = GetExtensions().ToList();

        if (!extensions.Any(x => x.IsModified))
            return;

        var dto = new EditLockExtensionsDto
        {
            Extensions = extensions.Where(x => x.IsEnabled).Select(x => x.GetLockExtensionConfig()).ToList()
        };

        Util.LogUpdateExtensionsAction(this, dto);
    }

    private void CommitTaskUpdates()
    {
        //TODO: I'm not sure how wearer interaction works here... I think it's fine if they have ConfigureTasks enabled?

        if (!Tasks.IsEnabled || !Tasks.TasksModified)
            return;

        var taskParams = Tasks.UserTasks.Select(x => new TaskActionParamsModel { Points = x.Points, Task = x.Task }).ToList();

        Util.LogUpdateTasksAction(this, taskParams);
    }

}