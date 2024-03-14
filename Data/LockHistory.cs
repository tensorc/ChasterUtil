using ChasterSharp;

namespace ChasterUtil;

public sealed class LockHistory
{
    public string Id { get; set; } = string.Empty;

    public string TokenId { get; set; } = string.Empty;

    public bool Processed { get; set; }

    public LogForPublic Log { get; set; } = new();
}