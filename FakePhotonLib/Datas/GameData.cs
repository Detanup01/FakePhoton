using System.Collections;

namespace FakePhotonLib.BinaryData;

public class GameData
{
    public string Id { get; set; } = string.Empty;
    public byte MaxPlayer { get; set; }
    public bool IsOpen { get; set; }
    public bool IsVisible { get; set; }
    public DateTime CreatedAt { get; set; }
    public Dictionary<object, object> Properties { get; set; } = [];
    public List<string> ActiveUserIds { get; set; } = [];
    public List<string> ExpectedUserIds { get; set; } = [];
    public List<string> InactiveUserIds { get; set; } = [];
    public List<ExcludedUser> ExcludedUsers { get; set; } = [];
    public byte PlayerCount => (byte)(ActiveUserIds.Count + ExpectedUserIds.Count + InactiveUserIds.Count);

    public ActorsProperties ActorsProperties { get; set; } = new();

    public byte RoomFlags { get; set; }

    public Hashtable ToHashTable()
    {
        Hashtable h = new();
        foreach (var keyValue in Properties)
        {
            h.Add(keyValue.Key, keyValue.Value);
        }
        h[252] = PlayerCount;
        h[byte.MaxValue] = MaxPlayer;
        h[253] = IsOpen;
        h.Remove(254);
        return h;
    }
}

[Serializable]
public class ExcludedUser
{
    public string UserId { get; set; } = string.Empty;
    public byte Reason { get; set; }
}

public class ActorsProperties
{
    public List<string> Nicknames { get; set; } = [];
}