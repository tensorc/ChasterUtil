using System.ComponentModel;
using ChasterSharp;

namespace ChasterUtil;

public sealed class WheelOfFortunePenaltyConfig
{
    internal event PropertyChangedEventHandler? PropertyChanged;

    private int _spinsRequired;
    public int SpinsRequired
    {
        get => _spinsRequired;
        set
        {
            _spinsRequired = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SpinsRequired)));
        }
    }

    private PenaltyTimeLimit _timeToCompleteSpins;
    public PenaltyTimeLimit TimeToCompleteSpins
    {
        get => _timeToCompleteSpins;
        set
        {
            _timeToCompleteSpins = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TimeToCompleteSpins)));
        }
    }

    public PenaltyActions NumSpinsPenalty { get; } = new();

    public WheelOfFortunePenaltyConfig(WheelOfFortuneFrequencyPenalty? wheelOfFortuneFrequency)
    {
        if (wheelOfFortuneFrequency is not null)
        {
            _spinsRequired = wheelOfFortuneFrequency.Params!.Actions;
            _timeToCompleteSpins = ChasterExtension.GetPenaltyTimeLimit(wheelOfFortuneFrequency.Params!.Frequency);
            NumSpinsPenalty = ChasterExtension.PunishmentsToPenaltyActions(wheelOfFortuneFrequency.Punishments);
        }

        NumSpinsPenalty.PropertyChanged += NumSpinsPenaltyPropertyChanged;
    }

    private void NumSpinsPenaltyPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NumSpinsPenalty)));
    }

    internal WheelOfFortuneFrequencyPenalty? GetWheelOfFortuneFrequencyPenalty()
    {
        if (SpinsRequired <= 0 || TimeToCompleteSpins == PenaltyTimeLimit.Unknown)
            return null;

        return new WheelOfFortuneFrequencyPenalty
        {
            Params = new PenaltyFrequencyParams { Actions = SpinsRequired, Frequency = (int)TimeToCompleteSpins },
            Punishments = ChasterExtension.PenaltyActionsToPunishments(NumSpinsPenalty)
        };
    }
}