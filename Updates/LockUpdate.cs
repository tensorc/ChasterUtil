using System.Text.Json;

namespace ChasterUtil;

public sealed class LockUpdate
{
    public string Id { get; set; } = string.Empty;

    public DateTime CreatedTime { get; set; } 

    public string LockId { get; set; } = string.Empty;

    public string TokenId { get; set; } = string.Empty;

    public LockUpdateType UpdateType { get; set; }

    public JsonElement? Payload { get; set; }

    public static LockUpdate Create(LockInstance instance, LockUpdateType updateType, JsonElement? payload = null)
    {
        return new LockUpdate
        {
            Id = Guid.NewGuid().ToString(),
            CreatedTime = DateTime.Now,
            LockId = instance.LockId,
            TokenId = instance.TokenId,
            UpdateType = updateType,
            Payload = payload
        };
    }

}