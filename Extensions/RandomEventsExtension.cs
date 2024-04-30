using System.Text.Json;
using ChasterSharp;

namespace ChasterUtil;

public sealed class RandomEventsExtension : ChasterExtension
{
    public override ExtensionSlug ExtensionSlug => ExtensionSlug.RandomEvents;

    private RandomEventsDifficulty _difficulty = RandomEventsDifficulty.Normal;
    public RandomEventsDifficulty Difficulty
    {
        get => _difficulty;
        set
        {
            _difficulty = value;
            if(IsEnabled) IsModified = true;
        }
    }

    internal RandomEventsExtension(ExtensionPartyForPublic? extension, LockInstance lockInstance) : base(extension, lockInstance)
    {
        if (extension == null) return;
        var config = extension.Config.Deserialize<RandomEventsConfig>()!;

        _difficulty = config.Difficulty;
    }

    internal override LockExtensionConfigDto GetLockExtensionConfig()
    {
        var config = new RandomEventsConfig
        {
            Difficulty = Difficulty
        };

        return new LockExtensionConfigDto
        {
            Mode = LockExtensionMode.Unlimited,
            Regularity = 3600,
            Config = JsonSerializer.SerializeToElement(config),
            Slug = EnumStringConverter.GetMemberValueFromEnum(ExtensionSlug)!
        };
    }
}