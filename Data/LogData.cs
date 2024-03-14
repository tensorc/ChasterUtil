using ChasterSharp;

namespace ChasterUtil;

public sealed class LogData(LockHistory lockHistory)
{
    public string TokenId => lockHistory.TokenId;

    public LogForPublic Log => lockHistory.Log;

}