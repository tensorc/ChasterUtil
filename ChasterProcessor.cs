using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ChasterSharp;

namespace ChasterUtil;

//TODO: Add option to automatically archive Unlocked / Deserted locks
//TODO: Add method to Deactivate archived locks (requires passing an Id to update methods, then pruning after)

public sealed class ChasterProcessor
{

    #region  Properties

    public ChasterClient Client { get; }

    internal IChasterRepository ChasterRepository { get; }

    #endregion

    #region Members

    private readonly PollyHttpClient _pollyHttpClient;

    private readonly string? _defaultBearerToken;
    private readonly Dictionary<string, string> _bearerTokenIds;

    private readonly Dictionary<LockTokenIdPair, LockHandler> _lockHandlers;
    private readonly Dictionary<string, LockHandler> _sharedLockHandlers;

    #endregion

    public List<LockHandler> GetSharedLockHandlers()
    {
        return _sharedLockHandlers.Select(x => x.Value).Distinct().ToList();
    }

    #region Constructor

    public ChasterProcessor(IChasterRepository chasterRepository, string? defaultBearerToken = null)
    {
        _bearerTokenIds = [];
        _lockHandlers = [];
        _sharedLockHandlers = [];

        _pollyHttpClient = new PollyHttpClient();
        _defaultBearerToken = defaultBearerToken;

        ChasterRepository = chasterRepository;
        Client = new ChasterClient(_pollyHttpClient, _defaultBearerToken);
    }

    #endregion

    #region Registration

    public void RegisterLockHandler(string lockId, LockHandler lockHandler)
    {
        if (string.IsNullOrEmpty(_defaultBearerToken))
            throw new InvalidOperationException("This method requires a default bearer token.");

        RegisterLockHandler(new LockTokenIdPair(lockId, GetBearerTokenId()), lockHandler);
    }

    public void RegisterLockHandler(LockTokenIdPair lockTokenPair, LockHandler lockHandler)
    {
        ArgumentNullException.ThrowIfNull(lockTokenPair);
        ArgumentNullException.ThrowIfNull(lockHandler);

        _lockHandlers[lockTokenPair] = lockHandler;

        lockHandler.Processor = this;
        lockHandler.Invalidate();
    }

    public void RegisterSharedLockHandler(string sharedLockId, LockHandler lockHandler)
    {
        ArgumentException.ThrowIfNullOrEmpty(sharedLockId);
        ArgumentNullException.ThrowIfNull(lockHandler);

        _sharedLockHandlers[sharedLockId] = lockHandler;

        lockHandler.Processor = this;
        lockHandler.Invalidate();
    }

    #endregion

    #region Tokens

    public string GetBearerTokenId(string? bearerToken = null)
    {
        var token = GetBearerToken(bearerToken);

        if (_bearerTokenIds.TryGetValue(token, out var value))
            return value;

        var hash = MD5.HashData(Encoding.UTF8.GetBytes(token));

        var tokenId = Convert.ToBase64String(hash)[..^2];
        _bearerTokenIds.Add(token, tokenId);

        return tokenId;
    }

    public string GetBearerToken(string? bearerToken = null)
    {
        var result = string.IsNullOrEmpty(bearerToken) ? _defaultBearerToken : bearerToken;

        ArgumentException.ThrowIfNullOrEmpty(result);

        return result;
    }

    #endregion

    #region Lock Snapshots

    public async Task BulkUpdateUserLockSnapshots(UserLockStatus? lockStatus = null, string? bearerToken = null, Guid? updateGuid = null)
    {
        var token = GetBearerToken(bearerToken);
        var tokenId = GetBearerTokenId(bearerToken);

        var result = await Client.GetLocksAsync(lockStatus, token);

        result.Value?.ForEach(x => UpsertLockSnapshot(x, tokenId, updateGuid));
        result.Value?.Select(x => GetLockHandler(x, tokenId)).Where(x => x is not null).Distinct().ToList().ForEach(x => x?.Invalidate());
    }

    public async Task BulkUpdateKeyholderLockSnapshots(KeyholderSearchLocksDtoStatus lockStatus, string? bearerToken = null, Guid? updateGuid = null)
    {
        var token = GetBearerToken(bearerToken);
        var tokenId = GetBearerTokenId(bearerToken);

        var affectedLockHandlers = new List<LockHandler>();

        var dto = new KeyholderSearchLocksDto
        {
            Limit = 50,
            Status = lockStatus
        };

        var pageNumber = 0;

        while (true)
        {
            dto.Page = pageNumber++;
            var result = await Client.SearchLockedUsersAsync(dto, token);

            if (result.Value is null)
                break;

            result.Value.Locks.ForEach(x => UpsertLockSnapshot(x, tokenId, updateGuid));
            var lockHandlers = result.Value.Locks.Select(x => GetLockHandler(x, tokenId)).Where(x => x is not null).ToList();

            foreach (var handler in lockHandlers)
            {
                if (!affectedLockHandlers.Contains(handler!))
                    affectedLockHandlers.Add(handler!);
            }

            if (pageNumber >= result.Value.Pages)
                break;
        }

        foreach (var handler in affectedLockHandlers)
        {
            handler.Invalidate();
        }
    }

    private void UpsertLockSnapshot(Lock @lock, string tokenId, Guid? updateGuid)
    {
        var snapshot = ChasterRepository.GetLockSnapshot(@lock.Id, tokenId) ?? LockSnapshot.Create(@lock, tokenId);

        if (!snapshot.IsActive)
            return;

        snapshot.Lock = @lock;
        snapshot.UpdateGuid = updateGuid ?? Guid.Empty;
        ChasterRepository.UpsertLockSnapshot(snapshot);
    }

    public void DeactivateSnapshotsWithoutUpdateGuid(Guid updateGuid, string? bearerToken = null)
    {
        var tokenId = GetBearerTokenId(bearerToken);

        var snapshots = ChasterRepository.GetActiveLockSnapshots(tokenId);

        foreach (var snapshot in snapshots)
        {
            if (snapshot.UpdateGuid != updateGuid)
                ChasterRepository.MarkLockSnapshotAsInactive(snapshot);
        }
    }

    #endregion

    #region Lock History

    internal List<LockInstance> GetLockHandlerInstances(LockHandler handler, string? bearerToken = null)
    {
        var token = GetBearerToken(bearerToken);
        var tokenId = GetBearerTokenId(bearerToken);

        var snapshots = ChasterRepository.GetActiveLockSnapshots(tokenId);

        return snapshots.Where(x => x.TokenId == tokenId && GetLockHandler(x.Lock, tokenId) == handler).Select(x => new LockInstance(this, x.Lock, token)).ToList();
    }

    public List<LockInstance> GetAllInstances(string? bearerToken = null)
    {
        var token = GetBearerToken(bearerToken);
        var tokenId = GetBearerTokenId(bearerToken);

        var snapshots = ChasterRepository.GetActiveLockSnapshots(tokenId);

        return snapshots.Where(x => x.TokenId == tokenId).Select(x => new LockInstance(this, x.Lock, token)).ToList();
    }

    public async Task ProcessLockHistory(string? bearerToken = null)
    {
        var tokenId = GetBearerTokenId(bearerToken);
        var instances = GetAllInstances(bearerToken);

        var history = ChasterRepository.GetUnprocessedLockHistory(tokenId).OrderBy(x => x.Log.CreatedAt).ToList();

        List<LockHandler> processedHandlers = [];
        List<LockInstance> processedInstances = [];

        foreach (var log in history)
        {
            var instance = instances.Find(x => x.LockId == log.Log.LockId);

            if (instance is null)
                continue;

            var handler = GetLockHandler(instance.Lock, tokenId);

            if (handler is null)
                continue;

            if (!processedHandlers.Contains(handler))
            {
                processedHandlers.Add(handler);
                await handler.OnHandlerEnter();
            }

            if (!processedInstances.Contains(instance))
            {
                processedInstances.Add(instance);
                await handler.OnProcessingStarted(instance);
            }

            await HandleHistoryLog(handler, instance, log);
        }

        foreach (var instance in processedInstances)
        {
            var handler = GetLockHandler(instance.Lock, tokenId)!;
            await handler.OnProcessingCompleted(instance);

            instance.CommitUpdates();

            if (instance.Lock.Status == LockStatus.Locked)
                continue;

            var snapshot = ChasterRepository.GetLockSnapshot(instance.Lock.Id, tokenId);

            if (snapshot is null)
                continue;

            ChasterRepository.MarkLockSnapshotAsInactive(snapshot);
        }

        foreach (var handler in processedHandlers)
        {
            await handler.OnHandlerExit();
        }
    }

    private async Task HandleHistoryLog(LockHandler handler, LockInstance lockInstance, LockHistory log)
    {
        var payload = log.Log.Payload;

        switch (log.Log.GetLogType())
        {
            case LogType.Locked:
                await handler.OnLocked(lockInstance, new LogData(log));
                break;
            case LogType.Unlocked:
                await handler.OnUnlocked(lockInstance, new LogData(log));
                break;
            case LogType.Deserted:
                await handler.OnDeserted(lockInstance, new LogData(log));
                break;
            case LogType.KeyholderTrusted:
                await handler.OnKeyholderTrusted(lockInstance, new LogData(log));
                break;
            case LogType.SessionOfferAccepted:
                await handler.OnSessionOfferAccepted(lockInstance, new LogData(log));
                break;
            case LogType.MaxLimitDateRemoved:
                await handler.OnMaxLimitDateRemoved(lockInstance, new LogData(log));
                break;
            case LogType.MaxLimitDateIncreased:
                await handler.OnMaxLimitDateIncreased(lockInstance, new LogData(log), payload.Deserialize<LogMaxLimitDateIncreasedPayload>()!);
                break;
            case LogType.LockFrozen:
                await handler.OnLockFrozen(lockInstance, new LogData(log));
                break;
            case LogType.LockUnfrozen:
                await handler.OnLockUnfrozen(lockInstance, new LogData(log));
                break;
            case LogType.TimerHidden:
                await handler.OnTimerHidden(lockInstance, new LogData(log));
                break;
            case LogType.TimerRevealed:
                await handler.OnTimerRevealed(lockInstance, new LogData(log));
                break;
            case LogType.TimeLogsHidden:
                await handler.OnTimeLogsHidden(lockInstance, new LogData(log));
                break;
            case LogType.TimeLogsRevealed:
                await handler.OnTimeLogsRevealed(lockInstance, new LogData(log));
                break;
            case LogType.ExtensionUpdated:
                await handler.OnExtensionUpdated(lockInstance, new LogData(log), payload.Deserialize<LogExtensionUpdatedPayload>()!);
                break;
            case LogType.ExtensionEnabled:
                await handler.OnExtensionEnabled(lockInstance, new LogData(log), payload.Deserialize<LogExtensionEnabledPayload>()!);
                break;
            case LogType.ExtensionDisabled:
                await handler.OnExtensionDisabled(lockInstance, new LogData(log), payload.Deserialize<LogExtensionDisabledPayload>()!);
                break;
            case LogType.TimeChanged:
                await handler.OnTimeChanged(lockInstance, new LogData(log), payload.Deserialize<LogTimeChangedPayload>()!);
                break;
            case LogType.LinkTimeChanged:
                await handler.OnLinkTimeChanged(lockInstance, new LogData(log), payload.Deserialize<LogTimeChangedPayload>()!);
                break;
            case LogType.DiceRolled:
                await handler.OnDiceRolled(lockInstance, new LogData(log), payload.Deserialize<LogDiceRolledPayload>()!);
                break;
            case LogType.TaskAssigned:
                await handler.OnTaskAssigned(lockInstance, new LogData(log), payload.Deserialize<AssignTaskActionModel>()!);
                break;
            case LogType.TaskCompleted:
                await handler.OnTaskCompleted(lockInstance, new LogData(log), payload.Deserialize<LogTaskResultPayload>()!);
                break;
            case LogType.TaskFailed:
                await handler.OnTaskFailed(lockInstance, new LogData(log), payload.Deserialize<LogTaskResultPayload>()!);
                break;
            case LogType.TasksVoteEnded:
                await handler.OnTasksVoteEnded(lockInstance, new LogData(log), payload.Deserialize<LogTaskVoteEndedPayload>()!);
                break;
            case LogType.TemporaryOpeningOpened:
                await handler.OnTemporaryOpeningOpened(lockInstance, new LogData(log), payload.Deserialize<LogTemporaryOpeningOpenedPayload>()!);
                break;
            case LogType.TemporaryOpeningLocked:
                await handler.OnTemporaryOpeningLocked(lockInstance, new LogData(log), payload.Deserialize<LogTemporaryOpeningLockedPayload>()!);
                break;
            case LogType.TemporaryOpeningLockedLate:
                await handler.OnTemporaryOpeningLockedLate(lockInstance, new LogData(log), payload.Deserialize<LogTemporaryOpeningLockedPayload>()!);
                break;
            case LogType.VerificationPictureSubmitted:
                await handler.OnVerificationPictureSubmitted(lockInstance, new LogData(log), payload.Deserialize<LogVerificationPictureSubmittedPayload>()!);
                break;
            case LogType.PilloryStarted:
                await handler.OnPilloryStarted(lockInstance, new LogData(log), payload.Deserialize<LogPilloryStartedPayload>()!);
                break;
            case LogType.PilloryEnded:
                await handler.OnPilloryEnded(lockInstance, new LogData(log), payload.Deserialize<LogPilloryEndedPayload>()!);
                break;
            case LogType.WheelOfFortuneTurned:
                var segmentPayload = WheelOfFortuneExtension.GetWheelOfFortuneSegment(payload.Deserialize<LogWheelOfFortuneTurnedPayload>()!.Segment);

                if(segmentPayload is not null)
                    await handler.OnWheelOfFortuneTurned(lockInstance, new LogData(log), segmentPayload);
                break;
            case LogType.TimerGuessed:
                await handler.OnTimerGuessed(lockInstance, new LogData(log));
                break;
        }

        ChasterRepository.MarkLockHistoryAsProcessed(log);
    }

    public async Task BulkUpdateLockHistory(List<string>? sharedLockIds = null, string? bearerToken = null)
    {
        var token = GetBearerToken(bearerToken);
        var tokenId = GetBearerTokenId(bearerToken);

        var snapshots = ChasterRepository.GetActiveLockSnapshots(tokenId, sharedLockIds);

        foreach (var snapshot in snapshots)
        {
            LockInstance instance = new(this, snapshot.Lock, token);
            var handler = GetLockHandler(instance.Lock, tokenId);

            if (handler is not null)
                await UpdateLockHistory(snapshot, token);
        }
    }

    public async Task UpdateLockHistory(LockSnapshot snapshot, string? bearerToken = null)
    {
        var token = GetBearerToken(bearerToken);
        var tokenId = GetBearerTokenId(bearerToken);

        var logs = new List<LockHistory>();
        var latestLogId = ChasterRepository.GetMostRecentLockHistoryId(snapshot.Lock.Id, tokenId);

        var dto = new LockHistoryDto
        {
            Limit = 100
        };

        while (true)
        {
            var result = await Client.GetLockHistoryAsync(snapshot.Lock.Id, dto, token);

            if (result.HttpResponse is null || !result.HttpResponse.IsSuccessStatusCode || result.Value is null)
                return;

            var indexOfKnownRecord = result.Value.Results.FindIndex(x => x.Id == latestLogId);
            var stopIndex = indexOfKnownRecord == -1 ? result.Value.Results.Count : indexOfKnownRecord;

            for (var i = 0; i < stopIndex; i++)
            {
                logs.Add(new LockHistory { Id = result.Value.Results[i].Id, TokenId = tokenId, Log = result.Value.Results[i] });
            }

            if (indexOfKnownRecord != -1 || !result.Value.HasMore)
                break;

            dto.LastId = result.Value.Results[^1].Id;
        }

        ChasterRepository.InsertLockHistory(logs);

        if (snapshot.Lock.Status != LockStatus.Locked && !ChasterRepository.HasUnprocessedLockHistory(snapshot.Lock.Id, tokenId))
            ChasterRepository.MarkLockSnapshotAsInactive(snapshot);
    }

    #endregion

    #region Lock Updates

    public async Task ProcessLockUpdates(string? bearerToken = null)
    {
        var token = GetBearerToken(bearerToken);
        var tokenId = GetBearerTokenId(bearerToken);
        var ignoredLocks = new List<string>();

        var actions = ChasterRepository.GetPendingLockUpdates(tokenId).OrderBy(x => x.CreatedTime);

        foreach (var action in actions)
        {
            var @lock = ChasterRepository.GetLockSnapshot(action.LockId, tokenId)!.Lock;

            if (action.UpdateType == LockUpdateType.Archive)
            {
                if (@lock is { Role: LockRole.Keyholder, KeyholderArchivedAt: null })
                    _ = await Client.ArchiveKeyholderLockAsync(action.LockId, token);
                else if (@lock is { Role: LockRole.Wearer, ArchivedAt: null })
                    _ = await Client.ArchiveLockAsync(action.LockId, token);
            }
            else if (@lock.Status == LockStatus.Locked)
            {
                if (action.UpdateType == LockUpdateType.Unlock)
                {
                    //TODO: Make sure the lock can be unlocked (for example, wearer can't have ExtensionsDisallowingUnlock)?
                    _ = await Client.UnlockLockAsync(action.LockId, token);
                }
                else if (!@lock.User.IsSuspendedOrDisabled && !ignoredLocks.Contains(@lock.Id))
                {
                    switch (action.UpdateType)
                    {
                        case LockUpdateType.UpdateFreeze:
                            var lockFreezeUpdate = action.Payload!.Value.Deserialize<LockFreezeUpdate>()!;
                            var setLockFreezeResult = await Client.SetLockFreezeAsync(action.LockId, lockFreezeUpdate.IsFrozen, token);

                            if (setLockFreezeResult.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or HttpStatusCode.NotFound)
                                ignoredLocks.Add(@lock.Id);

                            break;
                        case LockUpdateType.TrustKeyholder:
                            var trustLockKeyholderResult = await Client.TrustLockKeyholderAsync(action.LockId, token);

                            if (trustLockKeyholderResult.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or HttpStatusCode.NotFound)
                                ignoredLocks.Add(@lock.Id);

                            break;
                        case LockUpdateType.UpdateMaxTimeLimit:
                            var lockMaxLimitDateUpdate = action.Payload!.Value.Deserialize<LockMaxLimitDateUpdate>()!;
                            var removeLimit = !lockMaxLimitDateUpdate.NewMaxLimitDate.HasValue;
                            var setLockMaxLimitDate = await Client.SetLockMaxLimitDateAsync(action.LockId, removeLimit ? @lock.MaxLimitDate!.Value : lockMaxLimitDateUpdate.NewMaxLimitDate!.Value, removeLimit, token);

                            if (setLockMaxLimitDate.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or HttpStatusCode.NotFound)
                                ignoredLocks.Add(@lock.Id);
                            break;
                        case LockUpdateType.AddRemoveTime:
                            var lockAddRemoveTimeUpdate = action.Payload!.Value.Deserialize<LockAddRemoveTimeUpdate>()!;
                            var addLockTimeResult = await Client.AddLockTimeAsync(action.LockId, (int)lockAddRemoveTimeUpdate.TimeToAddOrRemove.TotalSeconds,
                                token);

                            if (addLockTimeResult.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or HttpStatusCode.NotFound)
                                ignoredLocks.Add(@lock.Id);

                            break;
                        case LockUpdateType.UpdateSettings:
                            var lockSettingsUpdate = action.Payload!.Value.Deserialize<LockSettingsUpdate>()!;
                            var setLockTimeInfoResult = await Client.SetLockTimeInfoAsync(action.LockId, lockSettingsUpdate.DisplayRemainingTime,
                                lockSettingsUpdate.HideTimeLogs, token);

                            if (setLockTimeInfoResult.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or HttpStatusCode.NotFound)
                                ignoredLocks.Add(@lock.Id);

                            break;
                        case LockUpdateType.UpdateExtensions:
                            var lockExtensionsUpdate = action.Payload!.Value.Deserialize<LockExtensionsUpdate>()!;
                            var updateLockExtensionsResult = await Client.UpdateLockExtensionsAsync(action.LockId, lockExtensionsUpdate.ExtensionData, token);

                            if (!lockExtensionsUpdate.ExtensionData.Extensions.Exists(x => x.GetExtensionSlug() == ExtensionSlug.ShareLink))
                                ChasterRepository.DeleteShareLink(@lock.Id);

                            if (updateLockExtensionsResult.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or HttpStatusCode.NotFound)
                                ignoredLocks.Add(@lock.Id);

                            break;
                        case LockUpdateType.UpdateTasks:
                            var tasksExtensionId = @lock.Extensions
                                .FirstOrDefault(x => x.GetExtensionSlug() == ExtensionSlug.Tasks)?.Id;

                            if (tasksExtensionId is null)
                                continue;

                            var lockTasksUpdate = action.Payload!.Value.Deserialize<LockTasksUpdate>()!;
                            var updateLockTasksResult = await Client.UpdateLockTasksAsync(action.LockId, tasksExtensionId, lockTasksUpdate.Tasks, token);

                            if (updateLockTasksResult.StatusCode is HttpStatusCode.BadRequest)
                                ignoredLocks.Add(@lock.Id);

                            break;
                        case LockUpdateType.ResolveTask:
                            var tasksExtensionId2 = @lock.Extensions
                                .FirstOrDefault(x => x.GetExtensionSlug() == ExtensionSlug.Tasks)?.Id;

                            if (tasksExtensionId2 is null)
                                continue;

                            var lockResolveTaskUpdate = action.Payload!.Value.Deserialize<LockResolveTaskUpdate>()!;
                            var resolveTaskResult = await Client.ResolveTaskAsync(action.LockId, tasksExtensionId2, lockResolveTaskUpdate.IsCompleted, token);

                            if (resolveTaskResult.StatusCode is HttpStatusCode.BadRequest)
                                ignoredLocks.Add(@lock.Id);

                            break;
                        case LockUpdateType.AssignTask:
                            var tasksExtensionId3 = @lock.Extensions
                                 .FirstOrDefault(x => x.GetExtensionSlug() == ExtensionSlug.Tasks)?.Id;

                            if (tasksExtensionId3 is null)
                                continue;

                            var lockAssignTaskUpdate = action.Payload!.Value.Deserialize<LockAssignTaskUpdate>()!;
                            ApiResult assignTaskResult;

                            if (lockAssignTaskUpdate.IsRandomTask)
                                assignTaskResult = await Client.AssignRandomTaskAsync(action.LockId, tasksExtensionId3, token);
                            else if (lockAssignTaskUpdate.IsVoteTask)
                                assignTaskResult = await Client.AssignVoteTaskAsync(action.LockId, tasksExtensionId3, (int)lockAssignTaskUpdate.VoteTaskDuration!.Value.TotalSeconds, token);
                            else
                                assignTaskResult = await Client.AssignTaskAsync(action.LockId, tasksExtensionId3,
                                    new TaskActionParamsModel
                                    {
                                        Task = lockAssignTaskUpdate.SpecificTask!.Task,
                                        Points = lockAssignTaskUpdate.SpecificTask!.Points
                                    });

                            if (assignTaskResult.StatusCode is HttpStatusCode.BadRequest)
                                ignoredLocks.Add(@lock.Id);

                            break;
                        case LockUpdateType.Pillory:
                            var pilloryExtensionId = @lock.Extensions
                                .FirstOrDefault(x => x.GetExtensionSlug() == ExtensionSlug.Pillory)?.Id;

                            if (pilloryExtensionId is null)
                                continue;

                            var lockPilloryUpdate = action.Payload!.Value.Deserialize<LockPilloryUpdate>()!;
                            var pilloryLockResult = await Client.PilloryLockAsync(action.LockId, pilloryExtensionId, lockPilloryUpdate.Reason, (int)lockPilloryUpdate.Duration.TotalSeconds, token);

                            if (pilloryLockResult.StatusCode is HttpStatusCode.BadRequest)
                                ignoredLocks.Add(@lock.Id);

                            break;
                        case LockUpdateType.SetTemporaryCombination:
                            var hygieneOpeningExtensionId = @lock.Extensions
                                .FirstOrDefault(x => x.GetExtensionSlug() == ExtensionSlug.HygieneOpening)?.Id;

                            if (hygieneOpeningExtensionId is null)
                                continue;

                            var setTemporaryCombinationUpdate = action.Payload!.Value.Deserialize<LockSetTemporaryCombinationUpdate>()!;
                            var setTemporaryOpeningCombinationResult = await Client.SetTemporaryOpeningCombinationAsync(action.LockId, setTemporaryCombinationUpdate.CombinationId, token);

                            if (setTemporaryOpeningCombinationResult.StatusCode is HttpStatusCode.BadRequest)
                                ignoredLocks.Add(@lock.Id);

                            break;
                        case LockUpdateType.TemporarilyUnlock:
                            var hygieneOpeningExtensionId2 = @lock.Extensions
                                .FirstOrDefault(x => x.GetExtensionSlug() == ExtensionSlug.HygieneOpening)?.Id;

                            if (hygieneOpeningExtensionId2 is null)
                                continue;

                            var temporarilyUnlockResult = @lock.Role == LockRole.Keyholder
                                ? await Client.KeyholderTemporarilyUnlockAsync(action.LockId, hygieneOpeningExtensionId2, token)
                                : await Client.WearerTemporarilyUnlockAsync(action.LockId, hygieneOpeningExtensionId2, token);

                            if (temporarilyUnlockResult.StatusCode is HttpStatusCode.BadRequest)
                                ignoredLocks.Add(@lock.Id);

                            break;
                        case LockUpdateType.CreateVerificationRequest:
                            var verificationPictureExtensionId = @lock.Extensions
                                .FirstOrDefault(x => x.GetExtensionSlug() == ExtensionSlug.VerificationPicture)?.Id;

                            if (verificationPictureExtensionId is null)
                                continue;

                            var createVerificationPictureRequestResult = await Client.CreateVerificationPictureRequestAsync(action.LockId, verificationPictureExtensionId, token);

                            if (createVerificationPictureRequestResult.StatusCode is HttpStatusCode.BadRequest)
                                ignoredLocks.Add(@lock.Id);

                            break;
                        case LockUpdateType.UploadVerifictionPicture:
                            var verificationPictureExtensionId2 = @lock.Extensions
                                .FirstOrDefault(x => x.GetExtensionSlug() == ExtensionSlug.VerificationPicture)?.Id;

                            if (verificationPictureExtensionId2 is null)
                                continue;

                            var uploadVerificationPictureUpdate = action.Payload!.Value.Deserialize<LockUploadVerificationPictureUpdate>()!;
                            var uploadVerificationPictureResult = await Client.UploadVerificationPictureAsync(action.LockId, uploadVerificationPictureUpdate.Data, uploadVerificationPictureUpdate.ContentType, token);

                            if (uploadVerificationPictureResult.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or HttpStatusCode.NotFound)
                                ignoredLocks.Add(@lock.Id);

                            break;
                    }
                }
            }

            ChasterRepository.DeleteLockUpdate(action.Id);
        }
    }

    internal void LogUnlockAction(LockInstance instance)
    {
        if (ChasterRepository.GetLockUpdate(instance.LockId, instance.TokenId, LockUpdateType.Unlock) is not null)
            return;

        ChasterRepository.DeleteAllLockUpdates(instance.LockId, instance.TokenId);

        var action = LockUpdate.Create(instance, LockUpdateType.Unlock);
        ChasterRepository.InsertLockUpdate(action);
    }

    internal void LogArchiveAction(LockInstance instance)
    {
        if (ChasterRepository.GetLockUpdate(instance.LockId, instance.TokenId, LockUpdateType.Archive) is not null)
            return;

        var action = LockUpdate.Create(instance, LockUpdateType.Archive);
        ChasterRepository.InsertLockUpdate(action);
    }

    internal void LogUpdateFreezeAction(LockInstance instance, bool isFrozen)
    {
        var action = ChasterRepository.GetLockUpdate(instance.LockId, instance.TokenId, LockUpdateType.UpdateFreeze) ??
                      LockUpdate.Create(instance, LockUpdateType.UpdateFreeze);

        action.Payload = JsonSerializer.SerializeToElement(new LockFreezeUpdate { IsFrozen = isFrozen });
        ChasterRepository.UpsertLockUpdate(action);
    }

    internal void LogTrustKeyholderAction(LockInstance instance)
    {
        var action = ChasterRepository.GetLockUpdate(instance.LockId, instance.TokenId, LockUpdateType.TrustKeyholder);

        if (action is not null)
            return;

        ChasterRepository.UpsertLockUpdate(LockUpdate.Create(instance, LockUpdateType.TrustKeyholder));
    }

    internal void LogUpdateMaxTimeLimitAction(LockInstance instance, DateTimeOffset? maxTimeLimit)
    {
        var action = ChasterRepository.GetLockUpdate(instance.LockId, instance.TokenId, LockUpdateType.UpdateMaxTimeLimit) ??
                     LockUpdate.Create(instance, LockUpdateType.UpdateMaxTimeLimit);

        action.Payload = JsonSerializer.SerializeToElement(new LockMaxLimitDateUpdate { NewMaxLimitDate = maxTimeLimit });
        ChasterRepository.UpsertLockUpdate(action);
    }

    internal void LogAddRemoveTimeAction(LockInstance instance, TimeSpan timeToAddOrRemove)
    {
        var action = ChasterRepository.GetLockUpdate(instance.LockId, instance.TokenId, LockUpdateType.AddRemoveTime) ??
                     LockUpdate.Create(instance, LockUpdateType.AddRemoveTime);

        var newValue = timeToAddOrRemove;

        if (action.Payload.HasValue)
            newValue += action.Payload.Value.Deserialize<LockAddRemoveTimeUpdate>()!.TimeToAddOrRemove;

        if ((int)newValue.TotalSeconds == 0)
        {
            if (action.Payload.HasValue)
                ChasterRepository.DeleteLockUpdate(instance.LockId, instance.TokenId, LockUpdateType.AddRemoveTime);
        }
        else
        {
            action.Payload = JsonSerializer.SerializeToElement(new LockAddRemoveTimeUpdate { TimeToAddOrRemove = newValue });
            ChasterRepository.UpsertLockUpdate(action);
        }
    }

    internal void LogUpdateSettingsAction(LockInstance instance, bool displayRemainingTime, bool hideTimeLogs)
    {
        var action = ChasterRepository.GetLockUpdate(instance.LockId, instance.TokenId, LockUpdateType.UpdateSettings) ??
                     LockUpdate.Create(instance, LockUpdateType.UpdateSettings);

        action.Payload = JsonSerializer.SerializeToElement(new LockSettingsUpdate { DisplayRemainingTime = displayRemainingTime, HideTimeLogs = hideTimeLogs });
        ChasterRepository.UpsertLockUpdate(action);
    }

    internal void LogUpdateExtensionsAction(LockInstance instance, EditLockExtensionsDto extensionData)
    {
        var action = ChasterRepository.GetLockUpdate(instance.LockId, instance.TokenId, LockUpdateType.UpdateExtensions) ??
                     LockUpdate.Create(instance, LockUpdateType.UpdateExtensions);

        if (!extensionData.Extensions.Exists(x => x.GetExtensionSlug() == ExtensionSlug.Tasks))
        {
            ChasterRepository.DeleteLockUpdate(instance.LockId, instance.TokenId, LockUpdateType.UpdateTasks);
            ChasterRepository.DeleteLockUpdate(instance.LockId, instance.TokenId, LockUpdateType.ResolveTask);
            ChasterRepository.DeleteLockUpdate(instance.LockId, instance.TokenId, LockUpdateType.AssignTask);
        }

        if (!extensionData.Extensions.Exists(x => x.GetExtensionSlug() == ExtensionSlug.Pillory))
            ChasterRepository.DeleteLockUpdate(instance.LockId, instance.TokenId, LockUpdateType.Pillory);

        if (!extensionData.Extensions.Exists(x => x.GetExtensionSlug() == ExtensionSlug.HygieneOpening))
        {
            ChasterRepository.DeleteLockUpdate(instance.LockId, instance.TokenId, LockUpdateType.TemporarilyUnlock);
            ChasterRepository.DeleteLockUpdate(instance.LockId, instance.TokenId, LockUpdateType.SetTemporaryCombination);
        }

        if (!extensionData.Extensions.Exists(x => x.GetExtensionSlug() == ExtensionSlug.VerificationPicture))
        {
            ChasterRepository.DeleteLockUpdate(instance.LockId, instance.TokenId, LockUpdateType.UploadVerifictionPicture);
            ChasterRepository.DeleteLockUpdate(instance.LockId, instance.TokenId, LockUpdateType.CreateVerificationRequest);
        }

        action.Payload = JsonSerializer.SerializeToElement(new LockExtensionsUpdate { ExtensionData = extensionData });
        ChasterRepository.UpsertLockUpdate(action);
    }

    internal void LogUpdateTasksAction(LockInstance instance, List<TaskActionParamsModel> tasks)
    {
        var action = ChasterRepository.GetLockUpdate(instance.LockId, instance.TokenId, LockUpdateType.UpdateTasks) ??
                     LockUpdate.Create(instance, LockUpdateType.UpdateTasks);

        action.Payload = JsonSerializer.SerializeToElement(new LockTasksUpdate { Tasks = tasks });
        ChasterRepository.UpsertLockUpdate(action);
    }

    internal void LogUpdatePilloryAction(LockInstance instance, string reason, TimeSpan duration)
    {
        var action = ChasterRepository.GetLockUpdate(instance.LockId, instance.TokenId, LockUpdateType.Pillory);
        var durationToAssign = duration;

        if (action is null)
        {
            action = LockUpdate.Create(instance, LockUpdateType.Pillory);
        }
        else
        {
            var payloadDuration = action.Payload!.Value.Deserialize<LockPilloryUpdate>()!.Duration;

            if (durationToAssign + payloadDuration > TimeSpan.FromDays(1))
                action = LockUpdate.Create(instance, LockUpdateType.Pillory);
            else
                durationToAssign += payloadDuration;
        }

        action.Payload = JsonSerializer.SerializeToElement(new LockPilloryUpdate { Duration = durationToAssign, Reason = reason });
        ChasterRepository.UpsertLockUpdate(action);
    }

    internal void LogSetTemporaryCombinationAction(LockInstance instance, string combinationId)
    {
        var action = ChasterRepository.GetLockUpdate(instance.LockId, instance.TokenId, LockUpdateType.SetTemporaryCombination) ??
                     LockUpdate.Create(instance, LockUpdateType.SetTemporaryCombination);

        action.Payload = JsonSerializer.SerializeToElement(new LockSetTemporaryCombinationUpdate { CombinationId = combinationId });

        ChasterRepository.UpsertLockUpdate(action);
    }

    internal void LogTemporarilyUnlockAction(LockInstance instance)
    {
        var action = ChasterRepository.GetLockUpdate(instance.LockId, instance.TokenId, LockUpdateType.TemporarilyUnlock);

        if (action is not null)
            return;

        ChasterRepository.UpsertLockUpdate(LockUpdate.Create(instance, LockUpdateType.TemporarilyUnlock));
    }

    internal void LogAssignTaskAction(LockInstance instance, LockAssignTaskUpdate assignment)
    {
        var action = ChasterRepository.GetLockUpdate(instance.LockId, instance.TokenId, LockUpdateType.AssignTask) ??
                     LockUpdate.Create(instance, LockUpdateType.AssignTask);

        action.Payload = JsonSerializer.SerializeToElement(assignment);
        ChasterRepository.UpsertLockUpdate(action);
    }

    internal void LogResolveTaskAction(LockInstance instance, bool isCompleted)
    {
        var action = ChasterRepository.GetLockUpdate(instance.LockId, instance.TokenId, LockUpdateType.ResolveTask) ??
                     LockUpdate.Create(instance, LockUpdateType.ResolveTask);

        action.Payload = JsonSerializer.SerializeToElement(new LockResolveTaskUpdate { IsCompleted = isCompleted });
        ChasterRepository.UpsertLockUpdate(action);
    }

    //TODO: Can we call this multiple times? Does it support content type?
    internal void LogUploadVerificationPictureAction(LockInstance instance, byte[] data, string? contentType = null)
    {
        var action = ChasterRepository.GetLockUpdate(instance.LockId, instance.TokenId, LockUpdateType.UploadVerifictionPicture) ??
                     LockUpdate.Create(instance, LockUpdateType.UploadVerifictionPicture);

        action.Payload = JsonSerializer.SerializeToElement(new LockUploadVerificationPictureUpdate { Data = data, ContentType = contentType });
        ChasterRepository.UpsertLockUpdate(action);
    }

    internal void LogCreateVerificationRequestAction(LockInstance instance)
    {
        var action = ChasterRepository.GetLockUpdate(instance.LockId, instance.TokenId, LockUpdateType.CreateVerificationRequest);

        if (action is not null)
            return;

        ChasterRepository.UpsertLockUpdate(LockUpdate.Create(instance, LockUpdateType.CreateVerificationRequest));
    }

    #endregion

    #region Handler Update

    public async Task UpdateLockHandlers()
    {
        foreach (var handler in _sharedLockHandlers)
        {
            await handler.Value.OnHandlerUpdate();
        }

        foreach (var handler in _lockHandlers)
        {
            await handler.Value.OnHandlerUpdate();
        }
    }

    #endregion

    private LockHandler? GetLockHandler(Lock @lock, string tokenId)
    {
        LockHandler? handler;

        if (@lock is { Role: LockRole.Keyholder, SharedLock: not null })
        {
            _sharedLockHandlers.TryGetValue(@lock.SharedLock.Id, out handler);
        }
        else
        {
            _lockHandlers.TryGetValue(new LockTokenIdPair(@lock.Id, tokenId), out handler);
        }

        return handler;
    }

}