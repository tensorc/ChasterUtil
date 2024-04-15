using System.Text.Json;
using ChasterSharp;

namespace ChasterUtil;

public sealed class ShareLinkExtension : ChasterExtension
{
    public override ExtensionSlug ExtensionSlug => ExtensionSlug.ShareLink;

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

    private TimeSpan _timeToRemove = TimeSpan.FromSeconds(3600);
    public TimeSpan TimeToRemove
    {
        get => _timeToRemove;
        set
        {
            _timeToRemove = value;
            if(IsEnabled) IsModified = true;
        }
    }

    private bool _enableRandom = true;
    public bool EnableRandom
    {
        get => _enableRandom;
        set
        {
            _enableRandom = value;
            if(IsEnabled) IsModified = true;
        }
    }

    private int _requiredVisits;
    public int RequiredVisits
    {
        get => _requiredVisits;
        set
        {
            _requiredVisits = value;
            if(IsEnabled) IsModified = true;
        }
    }

    private bool _limitToLoggedUsers;
    public bool LimitToLoggedUsers
    {
        get => _limitToLoggedUsers;
        set
        {
            _limitToLoggedUsers = value;
            if(IsEnabled) IsModified = true;
        }
    }

    public ShareLinkExtension(ExtensionPartyForPublic? extension, LockInstance lockInstance) : base(extension, lockInstance)
    {
        if (extension == null) return;
        var config = extension.Config.Deserialize<ShareLinkConfig>()!;

        _timeToAdd = TimeSpan.FromSeconds(config.TimeToAdd);
        _timeToRemove = TimeSpan.FromSeconds(config.TimeToRemove);
        _enableRandom = config.EnableRandom;
        _requiredVisits = config.RequiredVisits;
        _limitToLoggedUsers = config.LimitToLoggedUsers;
    }

    internal override LockExtensionConfigDto GetLockExtensionConfig()
    {
        var config = new ShareLinkConfig
        {
            TimeToAdd = (int)TimeToAdd.TotalSeconds,
            TimeToRemove = (int)TimeToRemove.TotalSeconds,
            EnableRandom = EnableRandom,
            RequiredVisits = RequiredVisits,
            LimitToLoggedUsers = LimitToLoggedUsers
        };

        return new LockExtensionConfigDto
        {
            Mode = LockExtensionMode.Unlimited,
            Regularity = 3600,
            Config = JsonSerializer.SerializeToElement(config),
            Slug = EnumStringConverter.GetMemberValueFromEnum(ExtensionSlug)!
        };
    }

    public async Task<ApiResult<string?>?> GetShareLinkAsync(bool bypassCache = false)
    {
        if (Extension is null)
            return null;

        if (!bypassCache)
        {
            var shareLink = Instance.Processor.ChasterRepository.GetShareLink(Instance.LockId);

            if (!string.IsNullOrEmpty(shareLink))
                return new ApiResult<string?>(shareLink, null, null);
        }

        var result = await Instance.Processor.Client.GetShareLinkAsync(Instance.LockId, Extension.Id, Instance.Token);

        if(result.HttpResponse is not null && result.HttpResponse.IsSuccessStatusCode && !string.IsNullOrEmpty(result.Value))
             Instance.Processor.ChasterRepository.UpsertShareLink(new CachedShareLink {Id = Instance.LockId, ShareLink = result.Value });

        return result;
    }

    public async Task<ApiResult<LinkInfoActionRepDto?>?> GetShareLinkInfoAsync()
    {
        if (Extension is null)
            return null;

        var result = await Instance.Processor.Client.GetShareLinkInfoAsync(Instance.LockId, Extension.Id, Instance.Token);

        //TODO: Cache result

        return result;
    }

}