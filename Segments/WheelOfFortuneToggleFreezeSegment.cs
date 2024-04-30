using ChasterSharp;

namespace ChasterUtil;

public class WheelOfFortuneToggleFreezeSegment : WheelOfFortuneSegment
{
    public override WheelOfFortuneSegmentType SegmentType => WheelOfFortuneSegmentType.ToggleFreeze;

    internal override WheelOfFortuneSegmentModel GetWheelOfFortuneSegmentModel()
    {
        return new WheelOfFortuneSegmentModel
        {
            Type = SegmentType,
            Duration = 3600,
            Text = string.Empty
        };
    }
}