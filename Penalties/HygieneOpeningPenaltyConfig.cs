using System.ComponentModel;
using ChasterSharp;

namespace ChasterUtil;

public sealed class HygieneOpeningPenaltyConfig
{
    internal event PropertyChangedEventHandler? PropertyChanged;

    private int _unlocksRequired;
    public int UnlocksRequired
    {
        get => _unlocksRequired;
        set
        {
            _unlocksRequired = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UnlocksRequired)));
        }
    }

    private PenaltyTimeLimit _timeToCompleteUnlocks;
    public PenaltyTimeLimit TimeToCompleteUnlocks
    {
        get => _timeToCompleteUnlocks;
        set
        {
            _timeToCompleteUnlocks = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TimeToCompleteUnlocks)));
        }
    }

    public PenaltyActions NumUnlocksPenalty { get; } = new();

    private TimeSpan _maximumUnlockTime;
    public TimeSpan MaximumUnlockTime
    {
        get => _maximumUnlockTime;
        set
        {
            _maximumUnlockTime = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MaximumUnlockTime)));
        }
    }

    public PenaltyActions MaxUnlockTimePenalty { get; } = new();

    public HygieneOpeningPenaltyConfig(HygieneOpeningFrequencyPenalty? hygieneOpeningFrequency, HygieneOpeningTimeLimitPenalty? hygieneOpeningTimeLimit)
    {
        if (hygieneOpeningFrequency is not null)
        {
            _unlocksRequired = hygieneOpeningFrequency.Params!.Actions;
            _timeToCompleteUnlocks = ChasterExtension.GetPenaltyTimeLimit(hygieneOpeningFrequency.Params!.Frequency);
            NumUnlocksPenalty = ChasterExtension.PunishmentsToPenaltyActions(hygieneOpeningFrequency.Punishments);
        }

        if (hygieneOpeningTimeLimit is not null)
        {
            _maximumUnlockTime = TimeSpan.FromSeconds(hygieneOpeningTimeLimit.Params!.TimeLimit);
            MaxUnlockTimePenalty = ChasterExtension.PunishmentsToPenaltyActions(hygieneOpeningTimeLimit.Punishments);
        }

        NumUnlocksPenalty.PropertyChanged += NumUnlocksPenaltyPropertyChanged;
        MaxUnlockTimePenalty.PropertyChanged += MaxUnlockTimePenaltyPropertyChanged;
    }

    private void NumUnlocksPenaltyPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NumUnlocksPenalty)));
    }

    private void MaxUnlockTimePenaltyPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MaxUnlockTimePenalty)));
    }

    internal HygieneOpeningFrequencyPenalty? GetHygieneOpeningFrequencyPenalty()
    {
        if (UnlocksRequired <= 0 || TimeToCompleteUnlocks == PenaltyTimeLimit.Unknown)
            return null;

        return new HygieneOpeningFrequencyPenalty
        {
            Params = new PenaltyFrequencyParams { Actions = UnlocksRequired, Frequency = (int)TimeToCompleteUnlocks },
            Punishments = ChasterExtension.PenaltyActionsToPunishments(NumUnlocksPenalty)
        };
    }

    internal HygieneOpeningTimeLimitPenalty? GetHygieneOpeningTimeLimitPenalty()
    {
        if ((int)MaximumUnlockTime.TotalSeconds <= 0)
            return null;

        return new HygieneOpeningTimeLimitPenalty
        {
            Params = new PenaltyTimeLimitParams { TimeLimit = (int)MaximumUnlockTime.TotalSeconds },
            Punishments = ChasterExtension.PenaltyActionsToPunishments(MaxUnlockTimePenalty)
        };
    }
}