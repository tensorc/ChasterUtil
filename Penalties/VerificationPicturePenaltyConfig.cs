using System.ComponentModel;
using ChasterSharp;

namespace ChasterUtil;

public sealed class VerificationPicturePenaltyConfig
{
    internal event PropertyChangedEventHandler? PropertyChanged;

    private int _verificationsRequired;
    public int VerificationsRequired
    {
        get => _verificationsRequired;
        set
        {
            _verificationsRequired = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(VerificationsRequired)));
        }
    }

    private PenaltyTimeLimit _timeToCompleteVerifications;
    public PenaltyTimeLimit TimeToCompleteVerifications
    {
        get => _timeToCompleteVerifications;
        set
        {
            _timeToCompleteVerifications = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TimeToCompleteVerifications)));
        }
    }

    public PenaltyActions NumVerificationsPenalty { get; } = new();

    public VerificationPicturePenaltyConfig(VerificationPictureFrequencyPenalty? verificationPictureFrequency)
    {
        if (verificationPictureFrequency is not null)
        {
            VerificationsRequired = verificationPictureFrequency.Params!.Actions;
            TimeToCompleteVerifications = ChasterExtension.GetPenaltyTimeLimit(verificationPictureFrequency.Params!.Frequency);
            NumVerificationsPenalty = ChasterExtension.PunishmentsToPenaltyActions(verificationPictureFrequency.Punishments);
        }

        NumVerificationsPenalty.PropertyChanged += NumVerificationsPenaltyPropertyChanged;
    }

    private void NumVerificationsPenaltyPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NumVerificationsPenalty)));
    }

    internal VerificationPictureFrequencyPenalty? GetVerificationPictureFrequencyPenalty()
    {
        if (VerificationsRequired <= 0 || TimeToCompleteVerifications == PenaltyTimeLimit.Unknown)
            return null;

        return new VerificationPictureFrequencyPenalty
        {
            Params = new PenaltyFrequencyParams { Actions = VerificationsRequired, Frequency = (int)TimeToCompleteVerifications },
            Punishments = ChasterExtension.PenaltyActionsToPunishments(NumVerificationsPenalty)
        };
    }
}