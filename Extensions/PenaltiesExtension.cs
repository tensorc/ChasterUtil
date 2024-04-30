using System.ComponentModel;
using System.Text.Json;
using ChasterSharp;

namespace ChasterUtil;

public sealed class PenaltiesExtension : ChasterExtension
{
    public override ExtensionSlug ExtensionSlug => ExtensionSlug.Penalties;

    public DicePenaltyConfig Dice { get; }

    public HygieneOpeningPenaltyConfig HygieneOpening { get; }

    public TasksPenaltyConfig Tasks { get; }

    public VerificationPicturePenaltyConfig VerificationPicture { get; }

    public WheelOfFortunePenaltyConfig WheelOfFortune { get; }

    public PenaltiesUserData UserData { get; } = new();

    internal PenaltiesExtension(ExtensionPartyForPublic? extension, LockInstance lockInstance) : base(extension, lockInstance)
    {
        DiceFrequencyPenalty? diceFrequency = null;
        HygieneOpeningFrequencyPenalty? hygieneOpeningFrequency = null;
        HygieneOpeningTimeLimitPenalty? hygieneOpeningTimeLimit = null;
        TasksFrequencyPenalty? tasksFrequency = null;
        TasksTimeLimitPenalty? tasksTimeLimit = null;
        VerificationPictureFrequencyPenalty? verificationPictureFrequency = null;
        WheelOfFortuneFrequencyPenalty? wheelOfFortuneFrequency = null;

        if (extension is not null)
        {
            var config = extension.Config.Deserialize<PenaltiesConfig>()!;
            UserData = extension.UserData.Deserialize<PenaltiesUserData>()!;

             diceFrequency = GetPenalty<DiceFrequencyPenalty>(config, PenaltyName.DiceFrequency);
             hygieneOpeningFrequency = GetPenalty<HygieneOpeningFrequencyPenalty>(config, PenaltyName.HygieneOpeningFrequency);
             hygieneOpeningTimeLimit = GetPenalty<HygieneOpeningTimeLimitPenalty>(config, PenaltyName.HygieneOpeningTimeLimit);
             tasksFrequency = GetPenalty<TasksFrequencyPenalty>(config, PenaltyName.TasksFrequency);
             tasksTimeLimit = GetPenalty<TasksTimeLimitPenalty>(config, PenaltyName.TasksTimeLimit);
             verificationPictureFrequency = GetPenalty<VerificationPictureFrequencyPenalty>(config, PenaltyName.VerificationPictureFrequency);
             wheelOfFortuneFrequency = GetPenalty<WheelOfFortuneFrequencyPenalty>(config, PenaltyName.WheelOfFortuneFrequency);
        }

        Dice = new DicePenaltyConfig(diceFrequency);
        Dice.PropertyChanged += PenaltyConfigPropertyChanged;

        HygieneOpening = new HygieneOpeningPenaltyConfig(hygieneOpeningFrequency, hygieneOpeningTimeLimit);
        HygieneOpening.PropertyChanged += PenaltyConfigPropertyChanged;

        Tasks = new TasksPenaltyConfig(tasksFrequency, tasksTimeLimit);
        Tasks.PropertyChanged += PenaltyConfigPropertyChanged;

        VerificationPicture = new VerificationPicturePenaltyConfig(verificationPictureFrequency);
        VerificationPicture.PropertyChanged += PenaltyConfigPropertyChanged;

        WheelOfFortune = new WheelOfFortunePenaltyConfig(wheelOfFortuneFrequency);
        WheelOfFortune.PropertyChanged += PenaltyConfigPropertyChanged;
    }

    private void PenaltyConfigPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (IsEnabled) IsModified = true;
    }

    private T? GetPenalty<T>(PenaltiesConfig config, PenaltyName penaltyName)
    {
        if (config.Penalties is null)
            return default;

        foreach (var element in config.Penalties)
        {
            if (!element.TryGetProperty("name", out var nameElement))
                continue;

            if ((PenaltyName)EnumStringConverter.GetEnumFromMemberValue(typeof(PenaltyName), nameElement.GetString()) == penaltyName)
                return element.Deserialize<T>();
        }

        return default;
    }

    private List<JsonElement> GetPenalties()
    {
        var penalties = new List<JsonElement>();

        var diceFrequency = Dice.GetDiceFrequencyPenalty();
        var hygieneOpeningFrequency = HygieneOpening.GetHygieneOpeningFrequencyPenalty();
        var hygieneOpeningTimeLimit = HygieneOpening.GetHygieneOpeningTimeLimitPenalty();
        var tasksFrequency = Tasks.GetTasksFrequencyPenalty();
        var tasksTimeLimit = Tasks.GetTasksTimeLimitPenalty();
        var verificationPictureFrequency = VerificationPicture.GetVerificationPictureFrequencyPenalty();
        var wheelOfFortuneFrequency = WheelOfFortune.GetWheelOfFortuneFrequencyPenalty();

        if (diceFrequency is not null)
            penalties.Add(JsonSerializer.SerializeToElement(diceFrequency));

        if (hygieneOpeningFrequency is not null)
            penalties.Add(JsonSerializer.SerializeToElement(hygieneOpeningFrequency));

        if (hygieneOpeningTimeLimit is not null)
            penalties.Add(JsonSerializer.SerializeToElement(hygieneOpeningTimeLimit));

        if (tasksFrequency is not null)
            penalties.Add(JsonSerializer.SerializeToElement(tasksFrequency));

        if (tasksTimeLimit is not null)
            penalties.Add(JsonSerializer.SerializeToElement(tasksTimeLimit));

        if (verificationPictureFrequency is not null)
            penalties.Add(JsonSerializer.SerializeToElement(verificationPictureFrequency));

        if (wheelOfFortuneFrequency is not null)
            penalties.Add(JsonSerializer.SerializeToElement(wheelOfFortuneFrequency));

        return penalties;
    }

    internal override LockExtensionConfigDto GetLockExtensionConfig()
    {
        var config = new PenaltiesConfig
        {
            Penalties = GetPenalties()
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