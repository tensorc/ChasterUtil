using System.Text.Json;
using ChasterSharp;

namespace ChasterUtil;

public abstract class ChasterExtension(ExtensionPartyForPublic? extension, LockInstance lockInstance)
{
    private bool _isEnabled = extension is not null;
    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (_isEnabled == value) return;
            _isEnabled = value;
            IsModified = true;
        }
    }

    internal bool IsModified { get; set; }

    protected LockInstance Instance => lockInstance;

    public ExtensionPartyForPublic? Extension => extension;

    public abstract ExtensionSlug ExtensionSlug { get; }

    internal abstract LockExtensionConfigDto GetLockExtensionConfig();

    internal static PenaltyActions PunishmentsToPenaltyActions(List<JsonElement>? elements)
    {
        var actions = new PenaltyActions();

        if (elements is null)
            return actions;

        foreach (var element in elements)
        {
            if (!element.TryGetProperty("name", out var nameElement))
                continue;

            var punishmentName = (PunishmentName)EnumStringConverter.GetEnumFromMemberValue(typeof(PunishmentName), nameElement.GetString());

            switch (punishmentName)
            {
                case PunishmentName.Freeze:
                    actions.FreezeLock = true;
                    break;
                case PunishmentName.Pillory:
                    actions.PilloryDuration = TimeSpan.FromSeconds(element.Deserialize<PilloryPunishment>()!.Params!.Duration);
                    break;
                case PunishmentName.AddTime:
                    actions.TimeAdded = TimeSpan.FromSeconds(element.Deserialize<TimePunishment>()!.Duration);
                    break;
            }
        }

        return actions;
    }

    internal static List<JsonElement> PenaltyActionsToPunishments(PenaltyActions actions)
    {
        var punishments = new List<JsonElement>();

        if(actions.FreezeLock)
            punishments.Add(JsonSerializer.SerializeToElement(new FreezePunishment()));

        if ((int)actions.PilloryDuration.TotalSeconds > 0)
            punishments.Add(JsonSerializer.SerializeToElement(new PilloryPunishment {Params = new PilloryPunishmentParams { Duration = (int)actions.PilloryDuration.TotalSeconds } }));

        if ((int)actions.TimeAdded.TotalSeconds > 0)
            punishments.Add(JsonSerializer.SerializeToElement(new TimePunishment {Duration = (int)actions.TimeAdded.TotalSeconds }));

        return punishments;
    }

    internal static PenaltyTimeLimit GetPenaltyTimeLimit(int value)
    {
        switch (value)
        {
            case 86400:
                return PenaltyTimeLimit.OneDay;
            case 172800:
                return PenaltyTimeLimit.TwoDays;
            case 604800:
                return PenaltyTimeLimit.OneWeek;
            case 2592000:
                return PenaltyTimeLimit.OneMonth;
            default:
                return PenaltyTimeLimit.Unknown;
        }
    }

}