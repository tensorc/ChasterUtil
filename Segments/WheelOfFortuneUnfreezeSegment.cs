using ChasterSharp;

namespace ChasterUtil;

public class WheelOfFortuneUnfreezeSegment : WheelOfFortuneSegment
{
    public override WheelOfFortuneSegmentType SegmentType => WheelOfFortuneSegmentType.Unfreeze;

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