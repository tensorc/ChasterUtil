using System.ComponentModel;
using ChasterSharp;

namespace ChasterUtil;

public class WheelOfFortunePillorySegment() : WheelOfFortuneSegment
{
    public override WheelOfFortuneSegmentType SegmentType => WheelOfFortuneSegmentType.Pillory;

    private TimeSpan _duration = TimeSpan.FromSeconds(3600);
    public TimeSpan Duration
    {
        get => _duration;
        set
        {
            if (_duration == value) return;
            _duration = value;

            OnPropertyChanged(this, new PropertyChangedEventArgs(nameof(Duration)));
        }
    }

    internal WheelOfFortunePillorySegment(WheelOfFortuneSegmentModel? segment) : this()
    {
        if (segment is null) return;

        _duration = TimeSpan.FromSeconds(segment.Duration);
    }

    internal override WheelOfFortuneSegmentModel GetWheelOfFortuneSegmentModel()
    {
        return new WheelOfFortuneSegmentModel
        {
            Type = SegmentType,
            Duration = (int)Duration.TotalSeconds,
            Text = string.Empty
        };
    }
}