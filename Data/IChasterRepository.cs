using System.Security.Cryptography.X509Certificates;

namespace ChasterUtil;

public interface IChasterRepository
{

    #region Lock History

    public string? GetMostRecentLockHistoryId(string lockId, string tokenId);

    public List<LockHistory> GetUnprocessedLockHistory(string tokenId);

    public bool HasUnprocessedLockHistory(string lockId, string tokenId);

    public void InsertLockHistory(List<LockHistory> lockHistory);

    public bool ContainsLockHistoryId(string id);

    public void MarkLockHistoryAsProcessed(LockHistory lockHistory);

    #endregion

    #region Lock Snapshots

    public LockSnapshot? GetLockSnapshot(string lockId, string tokenId);

    public List<LockSnapshot> GetActiveLockSnapshots(string tokenId, List<string>? sharedLockIds = null);

    public void UpsertLockSnapshot(LockSnapshot lockSnapshot);

    public void MarkLockSnapshotAsInactive(LockSnapshot lockSnapshot);

    #endregion

    #region Lock Updates

    public LockUpdate? GetLockUpdate(string lockId, string tokenId, LockUpdateType lockUpdateType);

    public List<LockUpdate> GetPendingLockUpdates(string tokenId);

    public void DeleteLockUpdate(string lockUpdateId);

    public void DeleteLockUpdate(string lockId, string tokenId, LockUpdateType lockUpdateType);

    public void DeleteAllLockUpdates(string lockId, string tokenId);

    public void InsertLockUpdate(LockUpdate lockUpdate);

    public void UpsertLockUpdate(LockUpdate lockUpdate);

    #endregion

    #region Caching

    public string? GetShareLink(string lockId);

    public void DeleteShareLink(string lockId);

    public void UpsertShareLink(CachedShareLink cachedShareLink);

    #endregion

}