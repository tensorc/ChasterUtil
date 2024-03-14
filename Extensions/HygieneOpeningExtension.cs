using System.Text.Json;
using ChasterSharp;

namespace ChasterUtil;

public sealed class HygieneOpeningExtension : ChasterExtension
{
    public override ExtensionSlug ExtensionSlug => ExtensionSlug.HygieneOpening;

    private TimeSpan _regularity = TimeSpan.FromSeconds(172800);
    public TimeSpan Regularity
    {
        get => _regularity;
        set
        {
            _regularity = value;
            if(IsEnabled) IsModified = true;
        }
    }

    private TimeSpan _openingTime = TimeSpan.FromSeconds(900);
    public TimeSpan OpeningTime
    {
        get => _openingTime;
        set
        {
            _openingTime = value;
            if(IsEnabled) IsModified = true;
        }
    }

    private TimeSpan _penaltyTime = TimeSpan.FromSeconds(43200);
    public TimeSpan PenaltyTime
    {
        get => _penaltyTime;
        set
        {
            _penaltyTime = value;
            if(IsEnabled) IsModified = true;
        }
    }

    private bool _allowOnlyKeyholderToOpen;
    public bool AllowOnlyKeyholderToOpen
    {
        get => _allowOnlyKeyholderToOpen;
        set
        {
            _allowOnlyKeyholderToOpen = value;
            if(IsEnabled) IsModified = true;
        }
    }

    public HygieneOpeningUserData UserData { get; } = new();

    public HygieneOpeningExtension(ExtensionPartyForPublic? extension, LockInstance lockInstance) : base(extension, lockInstance)
    {
        if (extension == null) return;
        
        var config = extension.Config.Deserialize<HygieneOpeningConfig>()!;
        UserData = extension.UserData.Deserialize<HygieneOpeningUserData>()!;

        _regularity = TimeSpan.FromSeconds(extension.Regularity);
        _openingTime = TimeSpan.FromSeconds(config.OpeningTime);
        _penaltyTime = TimeSpan.FromSeconds(config.PenaltyTime);
        _allowOnlyKeyholderToOpen = config.AllowOnlyKeyholderToOpen;
    }

    internal override LockExtensionConfigDto GetLockExtensionConfig()
    {
        var config = new HygieneOpeningConfig
        {
            OpeningTime = (int)OpeningTime.TotalSeconds,
            PenaltyTime = (int)PenaltyTime.TotalSeconds,
            AllowOnlyKeyholderToOpen = AllowOnlyKeyholderToOpen
        };

        return new LockExtensionConfigDto
        {
            Mode = LockExtensionMode.NonCumulative,
            Regularity = (int)Regularity.TotalSeconds,
            Config = JsonSerializer.SerializeToElement(config),
            Slug = EnumStringConverter.GetMemberValueFromEnum(ExtensionSlug)!
        };
    }

    public async Task<ApiResult<CombinationForPublic?>?> GetLockCombinationAsync()
    {
        if (Extension is null)
            return null;

        var result = await Instance.Processor.Client.GetTemporaryOpeningLockCombinationAsync(Instance.LockId, Instance.Token);

        //TODO: Cache result

        return result;
    }

    public async Task<ApiResult<CombinationForPublic?>?> GetLockCombinationFromActionLogAsync(string actionLogId)
    {
        if (Extension is null)
            return null;

        var result = await Instance.Processor.Client.GetTemporaryOpeningLockCombinationFromActionLogAsync(Instance.LockId, actionLogId, Instance.Token);
        
        //TODO: Cache result

        return result;
    }

    public void SetLockCombination(string combinationId)
    {
        Instance.Processor.LogSetTemporaryCombinationAction(Instance, combinationId);
    }

    public void TemporarilyUnlock()
    {
        Instance.Processor.LogTemporarilyUnlockAction(Instance);
    }

}