using System.ComponentModel;

namespace ChasterUtil;

public sealed class LockTask(int points, string task)
{
    internal event PropertyChangedEventHandler? PropertyChanged;

    private int _points = points;
    public int Points
    {
        get => _points;
        set
        {
            if (_points == value) return;
            _points = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Points)));
        }
    }

    private string _task = task;
    public string Task
    {
        get => _task;
        set
        {
            if (_task == value) return;
            _task = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Task)));
        }
    }

        
}