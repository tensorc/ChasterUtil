using ChasterSharp;

namespace ChasterUtil;

public class WheelOfFortuneFreezeSegment : WheelOfFortuneSegment
{
    public override WheelOfFortuneSegmentType SegmentType => WheelOfFortuneSegmentType.Freeze;

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