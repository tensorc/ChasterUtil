using ChasterSharp;
using System.ComponentModel;

namespace ChasterUtil;

public abstract class WheelOfFortuneSegment
{

    internal event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(object sender, PropertyChangedEventArgs eventArgs)
    {
        PropertyChanged?.Invoke(sender, eventArgs);
    }

    public abstract WheelOfFortuneSegmentType SegmentType { get; }

    internal abstract WheelOfFortuneSegmentModel GetWheelOfFortuneSegmentModel();

}