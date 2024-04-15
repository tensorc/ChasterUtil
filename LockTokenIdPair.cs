namespace ChasterUtil;

public sealed class LockTokenIdPair(string lockId, string tokenId) : IEquatable<LockTokenIdPair>
{

    public string LockId => lockId;

    public string TokenId => tokenId;

    #region Equality

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is LockTokenIdPair other && LockId == other.LockId && TokenId == other.TokenId;
    }

    public bool Equals(LockTokenIdPair? other)
    {
        return ReferenceEquals(this, other) || other is not null && LockId == other.LockId && TokenId == other.TokenId;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(LockId, TokenId);
    }

    public static bool operator ==(LockTokenIdPair? obj1, LockTokenIdPair? obj2)
    {
        if (ReferenceEquals(obj1, obj2))
            return true;

        if (obj1 is null || obj2 is null)
            return false;

        return obj1.Equals(obj2);
    }

    public static bool operator !=(LockTokenIdPair? obj1, LockTokenIdPair? obj2)
    {
        return !(obj1 == obj2);
    }

    #endregion
}