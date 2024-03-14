using ChasterSharp;

namespace ChasterUtil;

public sealed class LockSnapshot
{
    public string Id { get; set; } = string.Empty;

    public string TokenId { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public Lock Lock { get; set; } = new();

    public static LockSnapshot Create(Lock @lock, string tokenId)
    {
        return new LockSnapshot
        {
            Id = Guid.NewGuid().ToString(),
            TokenId = tokenId,
            IsActive = true,
            Lock = @lock
        };
    }

}