using System.ComponentModel;
using ChasterSharp;

namespace ChasterUtil;

public sealed class TasksPenaltyConfig
{
    internal event PropertyChangedEventHandler? PropertyChanged;

    private int _completionsRequired;
    public int CompletionsRequired
    {
        get => _completionsRequired;
        set
        {
            _completionsRequired = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CompletionsRequired)));
        }
    }

    private PenaltyTimeLimit _timeToCompleteTasks;
    public PenaltyTimeLimit TimeToCompleteTasks
    {
        get => _timeToCompleteTasks;
        set
        {
            _timeToCompleteTasks = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TimeToCompleteTasks)));
        }
    }

    public PenaltyActions NumTasksPenalty { get; } = new();

    private TimeSpan _timeLimitPerTask;
    public TimeSpan TimeLimitPerTask
    {
        get => _timeLimitPerTask;
        set
        {
            _timeLimitPerTask = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TimeLimitPerTask)));
        }
    }

    public PenaltyActions TimeLimitPenalty { get; } = new();

    public TasksPenaltyConfig(TasksFrequencyPenalty? tasksFrequency, TasksTimeLimitPenalty? tasksTimeLimit)
    {
        if (tasksFrequency is not null)
        {
            _completionsRequired = tasksFrequency.Params!.Actions;
            _timeToCompleteTasks = ChasterExtension.GetPenaltyTimeLimit(tasksFrequency.Params!.Frequency);
            NumTasksPenalty = ChasterExtension.PunishmentsToPenaltyActions(tasksFrequency.Punishments);
        }

        if (tasksTimeLimit is not null)
        {
            _timeLimitPerTask = TimeSpan.FromSeconds(tasksTimeLimit.Params!.TimeLimit);
            TimeLimitPenalty = ChasterExtension.PunishmentsToPenaltyActions(tasksTimeLimit.Punishments);
        }

        NumTasksPenalty.PropertyChanged += NumTasksPenaltyPropertyChanged;
        TimeLimitPenalty.PropertyChanged += TimeLimitPenaltyPropertyChanged;
    }

    private void NumTasksPenaltyPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NumTasksPenalty)));
    }

    private void TimeLimitPenaltyPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TimeLimitPenalty)));
    }

    internal TasksFrequencyPenalty? GetTasksFrequencyPenalty()
    {
        if (CompletionsRequired <= 0 || TimeToCompleteTasks == PenaltyTimeLimit.Unknown)
            return null;

        return new TasksFrequencyPenalty
        {
            Params = new PenaltyFrequencyParams { Actions = CompletionsRequired, Frequency = (int)TimeToCompleteTasks },
            Punishments = ChasterExtension.PenaltyActionsToPunishments(NumTasksPenalty)
        };
    }

    internal TasksTimeLimitPenalty? GetTasksTimeLimitPenalty()
    {
        if ((int)TimeLimitPerTask.TotalSeconds <= 0)
            return null;

        return new TasksTimeLimitPenalty
        {
            Params = new PenaltyTimeLimitParams { TimeLimit = (int)TimeLimitPerTask.TotalSeconds },
            Punishments = ChasterExtension.PenaltyActionsToPunishments(TimeLimitPenalty)
        };
    }
}