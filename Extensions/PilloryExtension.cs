using System.Text.Json;
using ChasterSharp;

namespace ChasterUtil;

public sealed class PilloryExtension : ChasterExtension
{
    public override ExtensionSlug ExtensionSlug => ExtensionSlug.Pillory;

    private TimeSpan _timeToAdd = TimeSpan.FromSeconds(3600);
    public TimeSpan TimeToAdd
    {
        get => _timeToAdd;
        set
        {
            _timeToAdd = value;
            if(IsEnabled) IsModified = true;
        }
    }

    public PilloryExtension(ExtensionPartyForPublic? extension, LockInstance lockInstance) : base(extension, lockInstance)
    {
        if (extension == null) return;
        var config = extension.Config.Deserialize<PilloryConfig>()!;

        _timeToAdd = TimeSpan.FromSeconds(config.TimeToAdd);
    }

    internal override LockExtensionConfigDto GetLockExtensionConfig()
    {
        var config = new PilloryConfig
        {
            LimitToLoggedUsers = true, //NOTE: Always true?
            TimeToAdd = (int)TimeToAdd.TotalSeconds
        };

        return new LockExtensionConfigDto
        {
            Mode = LockExtensionMode.Unlimited,
            Regularity = 3600,
            Config = JsonSerializer.SerializeToElement(config),
            Slug = EnumStringConverter.GetMemberValueFromEnum(ExtensionSlug)!
        };
    }

    public void SendToPillory(TimeSpan duration, string? reason = null)
    {
        if (duration.TotalMinutes < 15)
            throw new ArgumentOutOfRangeException(nameof(duration));

        Instance.Processor.LogUpdatePilloryAction(Instance, reason, duration);
    }

    public async Task<ApiResult<List<PilloryVoteInfoActionRepDto>?>?> GetVoteInfoAsync()
    {
        if (Extension is null)
            return null;

        return await Instance.Processor.Client.GetPilloryVoteInfoAsync(Instance.LockId, Extension.Id, Instance.Token);
    }

}