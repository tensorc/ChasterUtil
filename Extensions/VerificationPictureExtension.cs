using System.ComponentModel;
using System.Text.Json;
using ChasterSharp;

namespace ChasterUtil;

public sealed class VerificationPictureExtension : ChasterExtension
{
    public override ExtensionSlug ExtensionSlug => ExtensionSlug.VerificationPicture;

    private LockExtensionMode _extensionMode = LockExtensionMode.NonCumulative;
    public LockExtensionMode ExtensionMode
    {
        get => _extensionMode;
        set
        {
            _extensionMode = value;
            if(IsEnabled) IsModified = true;
        }
    }

    private TimeSpan _regularity = TimeSpan.FromSeconds(86400);
    public TimeSpan Regularity
    {
        get => _regularity;
        set
        {
            _regularity = value;
            if(IsEnabled) IsModified = true;
        }
    }

    private VerificationPictureVisibility _visibility = VerificationPictureVisibility.All;
    public VerificationPictureVisibility Visibility
    {
        get => _visibility;
        set
        {
            _visibility = value;
            if(IsEnabled) IsModified = true;
        }
    }

    private bool _peerVerification;
    public bool PeerVerification
    {
        get => _peerVerification;
        set
        {
            _peerVerification = value;
            if(IsEnabled) IsModified = true;
        }
    }

    public PenaltyActions RejectedPenalty { get; }

    public VerificationPictureUserData UserData { get; } = new();

    public VerificationPictureExtension(ExtensionPartyForPublic? extension, LockInstance lockInstance) : base(extension, lockInstance)
    {
        if (extension == null)
        {
            RejectedPenalty = new PenaltyActions();
            RejectedPenalty.PropertyChanged += RejectedPenaltyPropertyChanged;
            return;
        }

        var config = extension.Config.Deserialize<VerificationPictureConfig>()!;
        UserData = extension.UserData.Deserialize<VerificationPictureUserData>()!;

        _extensionMode = extension.Mode;
        _regularity = TimeSpan.FromSeconds(extension.Regularity);
        _visibility = config.Visibility;
        _peerVerification = config.PeerVerification?.Enabled ?? false;

        RejectedPenalty = PunishmentsToPenaltyActions(config.PeerVerification?.Punishments);
        RejectedPenalty.PropertyChanged += RejectedPenaltyPropertyChanged;
    }

    private void RejectedPenaltyPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if(IsEnabled) IsModified = true;
    }

    internal override LockExtensionConfigDto GetLockExtensionConfig()
    {
        var config = new VerificationPictureConfig
        {
            Visibility = Visibility,
            PeerVerification = new VerificationPictureConfigParams { Enabled = PeerVerification, Punishments = PenaltyActionsToPunishments(RejectedPenalty) }
        };

        return new LockExtensionConfigDto
        {
            Mode = ExtensionMode,
            Regularity = (int)Regularity.TotalSeconds,
            Config = JsonSerializer.SerializeToElement(config),
            Slug = EnumStringConverter.GetMemberValueFromEnum(ExtensionSlug)!
        };
    }

    public async Task<ApiResult<List<VerificationPictureHistoryEntry>?>?> GetVerificationPicturesAsync()
    {
        if (Extension is null)
            return null;

        var result = await Instance.Util.Client.GetVerificationPicturesAsync(Instance.LockId, Instance.Token);

        //TODO: Cache result

        return result;
    }

    public void UploadVerificationPicture(byte[] data, string? contentType = null)
    {
        Instance.Util.LogUploadVerificationPictureAction(Instance, data, contentType);
    }

    public void CreateVerificationRequest()
    {
        Instance.Util.LogCreateVerificationRequestAction(Instance);
    }

}