using System.ComponentModel;

namespace ChasterUtil;

public sealed class PenaltyActions
{

    internal event PropertyChangedEventHandler? PropertyChanged;

    private bool _freezeLock;
    public bool FreezeLock
    {
        get => _freezeLock;
        set
        {
            _freezeLock = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FreezeLock)));
        }
    }

    private TimeSpan _timeAdded;
    public TimeSpan TimeAdded
    {
        get => _timeAdded;
        set
        {
            _timeAdded = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TimeAdded)));
        }
    }

    private TimeSpan _pilloryDuration;
    public TimeSpan PilloryDuration
    {
        get => _pilloryDuration;
        set
        {
            _pilloryDuration = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PilloryDuration)));
        }
    }

}