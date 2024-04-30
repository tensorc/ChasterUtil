using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text.Json;
using ChasterSharp;

namespace ChasterUtil;

public sealed class TasksExtension : ChasterExtension
{
    public override ExtensionSlug ExtensionSlug => ExtensionSlug.Tasks;

    private LockExtensionMode _extensionMode = LockExtensionMode.NonCumulative;
    public LockExtensionMode ExtensionMode
    {
        get => _extensionMode;
        set
        {
            _extensionMode = value;
            if(IsEnabled) IsModified = true;
        }
    }

    private TimeSpan _regularity = TimeSpan.FromSeconds(3600);
    public TimeSpan Regularity
    {
        get => _regularity;
        set
        {
            _regularity = value;
            if(IsEnabled) IsModified = true;
        }
    }
     
    private bool _enableTaskPoints;
    public bool EnableTaskPoints
    {
        get => _enableTaskPoints;
        set
        {
            _enableTaskPoints = value;
            if(IsEnabled) IsModified = true;
        }
    }

    private int _pointsRequired;
    public int PointsRequired
    {
        get => _pointsRequired;
        set
        {
            _pointsRequired = value;
            if(IsEnabled) IsModified = true;
        }
    }

    internal bool TasksModified { get; private set; }

    public ObservableCollection<LockTask> UserTasks { get; }

    private bool _allowWearerToEdit;
    public bool AllowWearerToEdit
    {
        get => _allowWearerToEdit;
        set
        {
            _allowWearerToEdit = value;
            if(IsEnabled) IsModified = true;
        }
    }

    private bool _allowWearerToAssign = true;
    public bool AllowWearerToAssign
    {
        get => _allowWearerToAssign;
        set
        {
            _allowWearerToAssign = value;
            if(IsEnabled) IsModified = true;
        }
    }

    private bool _allowWearerToChoose;
    public bool AllowWearerToChoose
    {
        get => _allowWearerToChoose;
        set
        {
            _allowWearerToChoose = value;
            if(IsEnabled) IsModified = true;
        }
    }

    private bool _allowWearerToConfigure;
    public bool AllowWearerToConfigure
    {
        get => _allowWearerToConfigure;
        set
        {
            _allowWearerToConfigure = value;
            if (IsEnabled) IsModified = true;
        }
    }

    public PenaltyActions AbandonedPenalty { get; }

    public TasksUserData UserData { get; } = new();

    internal TasksExtension(ExtensionPartyForPublic? extension, LockInstance lockInstance) : base(extension, lockInstance)
    {
        if (extension == null)
        {
            UserTasks = new ObservableCollection<LockTask>();
            UserTasks.CollectionChanged += UserTasksCollectionChanged;

            AbandonedPenalty = new PenaltyActions();
            AbandonedPenalty.PropertyChanged += AbandonedPenaltyPropertyChanged;
            return;
        }

        var config = extension.Config.Deserialize<TasksConfig>()!;
        UserData = extension.UserData.Deserialize<TasksUserData>()!;

        _extensionMode = extension.Mode;
        _regularity = TimeSpan.FromSeconds(extension.Regularity);
        _enableTaskPoints = config.EnablePoints;
        _pointsRequired = config.PointsRequired;
        _allowWearerToEdit = config.AllowWearerToEditTasks;
        _allowWearerToAssign = !config.PreventWearerFromAssigningTasks;
        _allowWearerToConfigure = config.AllowWearerToConfigureTasks;

        var lockTasks = UserData.UserTasks.Select(x => new LockTask(x.Points, x.Task!)).ToList();
        lockTasks.ForEach(x => x.PropertyChanged += TaskPropertyChanged);
           
        UserTasks = new ObservableCollection<LockTask>(lockTasks);
        UserTasks.CollectionChanged += UserTasksCollectionChanged;

        AbandonedPenalty = PunishmentsToPenaltyActions(config.PunishmentsOnAbandonedTask);
        AbandonedPenalty.PropertyChanged += AbandonedPenaltyPropertyChanged;
    }

    private void UserTasksCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        TasksModified = true;
    }

    private void TaskPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        TasksModified = true;
    }

    private void AbandonedPenaltyPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if(IsEnabled) IsModified = true;
    }

    internal override LockExtensionConfigDto GetLockExtensionConfig()
    {
        var config = new TasksConfig
        {
            Tasks = [new TaskPayload { Task = string.Empty }],
            EnablePoints = EnableTaskPoints,
            PointsRequired = PointsRequired,
            AllowWearerToEditTasks = AllowWearerToEdit,
            PreventWearerFromAssigningTasks = !AllowWearerToAssign,
            AllowWearerToChooseTasks = AllowWearerToChoose,
            AllowWearerToConfigureTasks = AllowWearerToConfigure,
            StartVoteAfterLastVote = false, //NOTE: Always false?
            VoteEnabled = false, //NOTE: Always false?
            VoteDuration = 43200,
            PunishmentsOnAbandonedTask = PenaltyActionsToPunishments(AbandonedPenalty)
        };

        return new LockExtensionConfigDto
        {
            Mode = ExtensionMode,
            Regularity = (int)Regularity.TotalSeconds,
            Config = JsonSerializer.SerializeToElement(config),
            Slug = EnumStringConverter.GetMemberValueFromEnum(ExtensionSlug)!
        };
    }

    public void ResolveTask(bool isCompleted)
    {
       Instance.Processor.LogResolveTaskAction(Instance, isCompleted);
    }

    public void AssignTask(LockTask task)
    {
        Instance.Processor.LogAssignTaskAction(Instance, new LockAssignTaskUpdate { SpecificTask = task } );
    }

    public void AssignRandomTask()
    {
        Instance.Processor.LogAssignTaskAction(Instance, new LockAssignTaskUpdate { IsRandomTask = true });
    }

    public void AssignVoteTask(TimeSpan duration)
    {
        Instance.Processor.LogAssignTaskAction(Instance, new LockAssignTaskUpdate { IsVoteTask = true, VoteTaskDuration = duration });
    }

}