namespace ChasterUtil;

internal sealed class LockAssignTaskUpdate
{
    public LockTask? SpecificTask { get; set; }

    public bool IsRandomTask { get; set; }

    public bool IsVoteTask { get; set; }

    public TimeSpan? VoteTaskDuration { get; set; }

}