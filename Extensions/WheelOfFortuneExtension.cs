using System.Collections.ObjectModel;
using System.Collections.Specialized;
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

    public WheelOfFortuneExtension(ExtensionPartyForPublic? extension, LockInstance lockInstance) : base(extension, lockInstance)
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

        Segments = new ObservableCollection<WheelOfFortuneSegment>(config.Segments);
        Segments.CollectionChanged += Segments_CollectionChanged;
    }

    private void Segments_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if(IsEnabled) IsModified = true;
    }

    internal override LockExtensionConfigDto GetLockExtensionConfig()
    {
        var config = new WheelOfFortuneConfig
        {
            Segments = Segments.ToList()
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

        return await Instance.Util.Client.SpinWheelOfFortuneAsync(Instance.LockId, Extension.Id, Instance.Token);
    }
}