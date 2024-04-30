using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text.Json;
using ChasterSharp;

namespace ChasterUtil;

public sealed class WheelOfFortuneExtension : ChasterExtension
{
    public override ExtensionSlug ExtensionSlug => ExtensionSlug.WheelOfFortune;

    private LockExtensionMode _extensionMode;
    public LockExtensionMode ExtensionMode
    {
        get => _extensionMode;
        set
        {
            _extensionMode = value;
            if(IsEnabled) IsModified = true;
        }
    }

    private TimeSpan _regularity;
    public TimeSpan Regularity
    {
        get => _regularity;
        set
        {
            _regularity = value;
            if(IsEnabled) IsModified = true;
        }
    }

    public ObservableCollection<WheelOfFortuneSegment> Segments { get; }

    internal WheelOfFortuneExtension(ExtensionPartyForPublic? extension, LockInstance lockInstance) : base(extension, lockInstance)
    {
        if (extension == null)
        {
            Segments = new ObservableCollection<WheelOfFortuneSegment>();
            Segments.CollectionChanged += Segments_CollectionChanged;
            return;
        }

        var config = extension.Config.Deserialize<WheelOfFortuneConfig>()!;

        _extensionMode = extension.Mode;
        _regularity = TimeSpan.FromSeconds(extension.Regularity);

        var segments = config.Segments.Select(GetWheelOfFortuneSegment).Where(x => x != null).Select(x => x!).ToList();
        segments.ForEach(x => x.PropertyChanged += SegmentPropertyChanged);

        Segments = new ObservableCollection<WheelOfFortuneSegment>(segments);
        Segments.CollectionChanged += Segments_CollectionChanged;
    }

    internal static WheelOfFortuneSegment? GetWheelOfFortuneSegment(WheelOfFortuneSegmentModel? segment)
    {
        if(segment is null)
            return null;

        return segment.Type switch
        {
            WheelOfFortuneSegmentType.Freeze => new WheelOfFortuneFreezeSegment(),
            WheelOfFortuneSegmentType.Unfreeze => new WheelOfFortuneUnfreezeSegment(),
            WheelOfFortuneSegmentType.ToggleFreeze => new WheelOfFortuneToggleFreezeSegment(),
            WheelOfFortuneSegmentType.AddTime => new WheelOfFortuneAddTimeSegment(segment),
            WheelOfFortuneSegmentType.RemoveTime => new WheelOfFortuneRemoveTimeSegment(segment),
            WheelOfFortuneSegmentType.AddRemoveTime => new WheelOfFortuneAddRemoveTimeSegment(segment),
            WheelOfFortuneSegmentType.Text => new WheelOfFortuneTextSegment(segment),
            WheelOfFortuneSegmentType.Pillory => new WheelOfFortunePillorySegment(segment),
            _ => null
        };
    }

    private void Segments_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if(IsEnabled) IsModified = true;
    }

    private void SegmentPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (IsEnabled) IsModified = true;
    }

    internal override LockExtensionConfigDto GetLockExtensionConfig()
    {
        var config = new WheelOfFortuneConfig
        {
            Segments = Segments.Select(x => x.GetWheelOfFortuneSegmentModel()).ToList()
        };

        return new LockExtensionConfigDto
        {
            Mode = ExtensionMode,
            Regularity = (int)Regularity.TotalSeconds,
            Config = JsonSerializer.SerializeToElement(config),
            Slug = EnumStringConverter.GetMemberValueFromEnum(ExtensionSlug)!
        };
    }

    public async Task<ApiResult<SpinWheelActionModel?>?> SpinWheelAsync()
    {
        if (Extension is null)
            return null;

        return await Instance.Processor.Client.SpinWheelOfFortuneAsync(Instance.LockId, Extension.Id, Instance.Token);
    }
}