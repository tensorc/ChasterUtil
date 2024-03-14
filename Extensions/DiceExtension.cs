using System.Text.Json;
using ChasterSharp;

namespace ChasterUtil;

public sealed class DiceExtension : ChasterExtension
{
    public override ExtensionSlug ExtensionSlug => ExtensionSlug.Dice;

    private LockExtensionMode _extensionMode = LockExtensionMode.NonCumulative;
    public LockExtensionMode ExtensionMode
    {
        get => _extensionMode;
        set { 
            _extensionMode = value;
            if(IsEnabled) IsModified = true;
        }
    }

    private TimeSpan _regularity = TimeSpan.FromSeconds(3600);
    public TimeSpan Regularity
    {
        get => _regularity;
        set
        {
            _regularity = value;
            if(IsEnabled) IsModified = true;
        }
    }

    private TimeSpan _multiplier = TimeSpan.FromSeconds(3600);
    public TimeSpan Multiplier
    {
        get => _multiplier;
        set
        {
            _multiplier = value;
            if(IsEnabled) IsModified = true;
        }
    }

    public DiceExtension(ExtensionPartyForPublic? extension, LockInstance lockInstance) : base(extension, lockInstance)
    {
        if(extension == null) return;
        var config = extension.Config.Deserialize<DiceConfig>()!;

        _extensionMode = extension.Mode;
        _regularity = TimeSpan.FromSeconds(extension.Regularity);
        _multiplier = TimeSpan.FromSeconds(config.Multiplier);
    }

    internal override LockExtensionConfigDto GetLockExtensionConfig()
    {
        var config = new DiceConfig
        {
            Multiplier = (int)Multiplier.TotalSeconds
        };

        return new LockExtensionConfigDto
        {
            Mode = ExtensionMode,
            Regularity = (int)Regularity.TotalSeconds,
            Config = JsonSerializer.SerializeToElement(config),
            Slug = EnumStringConverter.GetMemberValueFromEnum(ExtensionSlug)!
        };
    }

    public async Task<ApiResult<RollDiceActionModel?>?> RollDiceAsync()
    {
        if (Extension is null)
            return null;

        return await Instance.Util.Client.RollDiceAsync(Instance.LockId, Extension.Id, Instance.Token);
    }
}