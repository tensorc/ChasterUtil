namespace ChasterUtil;

public sealed class LockTokenIdPair(string lockId, string tokenId)
{

    public string LockId => lockId;

    public string TokenId => tokenId;

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is LockTokenIdPair other && Equals(other);
    }

    private bool Equals(LockTokenIdPair other)
    {
        return LockId == other.LockId && TokenId == other.TokenId;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(LockId, TokenId);
    }

    public static bool operator ==(LockTokenIdPair? left, LockTokenIdPair? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(LockTokenIdPair? left, LockTokenIdPair? right)
    {
        return !Equals(left, right);
    }
}