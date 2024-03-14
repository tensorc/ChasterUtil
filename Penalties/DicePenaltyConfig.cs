using System.ComponentModel;
using ChasterSharp;

namespace ChasterUtil;

public sealed class DicePenaltyConfig
{
    internal event PropertyChangedEventHandler? PropertyChanged;

    private int _rollsRequired;
    public int RollsRequired
    {
        get => _rollsRequired;
        set
        {
            _rollsRequired = value; 
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RollsRequired)));
        }
    }

    private PenaltyTimeLimit _timeToCompleteRolls;
    public PenaltyTimeLimit TimeToCompleteRolls
    {
        get => _timeToCompleteRolls;
        set
        {
            _timeToCompleteRolls = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TimeToCompleteRolls)));
        }
    }

    public PenaltyActions NumRollsPenalty { get; } = new();

    public DicePenaltyConfig(DiceFrequencyPenalty? diceFrequency)
    {
        if (diceFrequency is not null)
        {
            _rollsRequired = diceFrequency.Params!.Actions;
            _timeToCompleteRolls = ChasterExtension.GetPenaltyTimeLimit(diceFrequency.Params!.Frequency);
            NumRollsPenalty = ChasterExtension.PunishmentsToPenaltyActions(diceFrequency.Punishments);
        }

        NumRollsPenalty.PropertyChanged += NumRollsPenaltyPropertyChanged;
    }

    private void NumRollsPenaltyPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NumRollsPenalty)));
    }

    internal DiceFrequencyPenalty? GetDiceFrequencyPenalty()
    {
        if (RollsRequired <= 0 || TimeToCompleteRolls == PenaltyTimeLimit.Unknown)
            return null;

        return new DiceFrequencyPenalty
        {
            Params = new PenaltyFrequencyParams { Actions = RollsRequired, Frequency = (int)TimeToCompleteRolls},
            Punishments = ChasterExtension.PenaltyActionsToPunishments(NumRollsPenalty)
        };
    }

}