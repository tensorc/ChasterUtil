using System.ComponentModel;
using ChasterSharp;

namespace ChasterUtil;

public class WheelOfFortuneTextSegment() : WheelOfFortuneSegment
{
    public override WheelOfFortuneSegmentType SegmentType => WheelOfFortuneSegmentType.Text;

    private string _text = string.Empty;
    public string Text
    {
        get => _text;
        set
        {
            if (_text == value) return;
            _text = value;

            OnPropertyChanged(this, new PropertyChangedEventArgs(nameof(Text)));
        }
    }

    internal WheelOfFortuneTextSegment(WheelOfFortuneSegmentModel? segment) : this()
    {
        if (segment is null) return;

        _text = segment.Text ?? string.Empty;
    }

    internal override WheelOfFortuneSegmentModel GetWheelOfFortuneSegmentModel()
    {
        return new WheelOfFortuneSegmentModel
        {
            Type = SegmentType,
            Duration = 3600,
            Text = Text
        };
    }
}