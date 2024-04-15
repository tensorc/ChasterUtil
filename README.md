Purpose
--------
While ChasterSharp enables you to utilize the Chaster.app API, ChasterUtil makes that process practical. ChasterUtil is intended to abstract away the HTTP handling, (de)serialization, and general burden of producing something coherent with all of the API calls and overly verbose objects.

Important Notice
--------
This project is currently under active development, and breaking changes to the API may occur frequently. Please be aware that any code relying on this API may need to be updated accordingly.

Getting Started
--------
In general, the only leg work you'll need to do is to integrate an IChasterRepository type which will be used to read / write cached data, such as lock states, history logs, and pending updates. I prefer to use LiteDB for this but I wanted to keep it flexible in case someone wanted to use a cloud service such as Amazon RDS, Azure SQL, etc. If you're cool with using LiteDB then feel free to use my implementation below.

[LiteDbChasterRepository](https://gist.github.com/tensorc/bc1ae1133544165da2ae01284e2c33c3)

For the examples below, I will assume you are using the LiteDbChasterRepository class I provided above. For most use cases, you'll want to create a LockHandler class and override whichever events you need, like so:

```cs
public sealed class MyTrapLockHandler : LockHandler
{
    public override Task OnLocked(LockInstance lockInstance, LogData logData)
    {
        lockInstance.IsFrozen = true;
        lockInstance.AddTime(TimeSpan.FromHours(12));

        return Task.CompletedTask;
    }

    public override Task OnKeyholderTrusted(LockInstance lockInstance, LogData logData)
    {
        lockInstance.GuessTheTimer.IsEnabled = true;
        lockInstance.GuessTheTimer.MinRandomTime = TimeSpan.FromHours(8);
        lockInstance.GuessTheTimer.MaxRandomTime = TimeSpan.FromHours(16);

        return Task.CompletedTask;
    }
}
```

Then, you'll want to create an instance of ChasterProcessor and register your lock handler:

```cs
LiteDatabase database = new LiteDatabase("data.db");
LiteDbChasterRepository chasterRepository = new(database);
ChasterProcessor processor = new ChasterProcessor(chasterRepository, BEARER_TOKEN);

processor.RegisterSharedLockHandler("{my_shared_lock_id}", new MyTrapLockHandler());
```

Finally, there are a few operations you'll want to run at whatever interval you see fit:

```cs
//Update local cache of locks
await processor.BulkUpdateKeyholderLockSnapshots(KeyholderSearchLocksDtoStatus.Locked);

//Get latest history of our cached locks
await processor.BulkUpdateLockHistory();

//Instruct processor to enumerate through the new logs and raise LockHandler events where appropriate
await processor.ProcessLockHistory();

//Finally, execute any pending changes we've made (e.g. Freeze, add/remove time, update extensions, etc.)
await processor.ProcessLockUpdates();
```

Lock Updates
--------
The LockInstance type has a CommitUpdates method that checks for modified settings, extensions, etc. and writes these changes to the "Update Log". This grants the benefit of batching calls together and introduces some level of fault tolerance since we can maintain those logs until the action has been processed. CommitUpdates is called automatically when updating locks within your LockHandler events, but in situations where you may be manually creating LockInstance types outside of that system, like so:

```cs
var snapshot = database.GetCollection<LockSnapshot>().FindOne(x => x.Lock.Status == LockStatus.Locked);
var instance = new LockInstance(processor, snapshot.Lock, BEARER_TOKEN);
```

You must be mindful to call CommitUpdates after you've made changes:

```cs
instance.CommitUpdates();
```

It is totally fine to call CommitUpdates multiple times, so don't be afraid to call it often!
