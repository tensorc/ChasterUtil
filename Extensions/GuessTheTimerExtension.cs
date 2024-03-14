using System.Text.Json;
using ChasterSharp;

namespace ChasterUtil;

public sealed class GuessTheTimerExtension : ChasterExtension
{
    public override ExtensionSlug ExtensionSlug => ExtensionSlug.GuessTheTimer;

    private TimeSpan _minRandomTime = TimeSpan.FromSeconds(10800);
    public TimeSpan MinRandomTime
    {
        get => _minRandomTime;
        set
        {
            _minRandomTime = value;
            if(IsEnabled) IsModified = true;
        }
    }

    private TimeSpan _maxRandomTime = TimeSpan.FromSeconds(21600);
    public TimeSpan MaxRandomTime
    {
        get => _maxRandomTime;
        set
        {
            _maxRandomTime = value;
            if(IsEnabled) IsModified = true;
        }
    }

    public GuessTheTimerExtension(ExtensionPartyForPublic? extension, LockInstance lockInstance) : base(extension, lockInstance)
    {
        if (extension == null) return;
        var config = extension.Config.Deserialize<GuessTheTimerConfig>()!;

        _minRandomTime = TimeSpan.FromSeconds(config.MinRandomTime);
        _maxRandomTime = TimeSpan.FromSeconds(config.MaxRandomTime);
    }

    internal override LockExtensionConfigDto GetLockExtensionConfig()
    {
        var config = new GuessTheTimerConfig
        {
            MinRandomTime = (int)MinRandomTime.TotalSeconds,
            MaxRandomTime = (int)MaxRandomTime.TotalSeconds
        };

        return new LockExtensionConfigDto
        {
            Mode = LockExtensionMode.Unlimited,
            Regularity = 3600,
            Config = JsonSerializer.SerializeToElement(config),
            Slug = EnumStringConverter.GetMemberValueFromEnum(ExtensionSlug)!
        };
    }

    public async Task<ApiResult<GuessTheTimerActionRepDto?>?> SubmitGuessAsync()
    {
        if (Extension is null)
            return null;

        return await Instance.Util.Client.SubmitGuessToGuessTheTimerAsync(Instance.LockId, Extension.Id, Instance.Token);
    }
}