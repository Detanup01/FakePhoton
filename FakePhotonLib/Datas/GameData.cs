using FakePhotonLib.Datas;
using Serilog;
using System.Collections;

namespace FakePhotonLib.BinaryData;

public class GameData
{
    public string Id { get; set; } = string.Empty;
    public byte MaxPlayer { get; set; }
    public byte ExpectedMaxPlayer { get; set; } = 20;
    public bool IsOpen { get; set; } = true;
    public bool IsVisible { get; set; }
    public DateTime CreatedAt { get; set; }
    public Dictionary<object, object> Properties { get; set; } = [];
    public List<string> ActiveUserIds { get; set; } = [];
    public List<string> ExpectedUserIds { get; set; } = [];
    public List<string> InactiveUserIds { get; set; } = [];
    public List<ExcludedUser> ExcludedUsers { get; set; } = [];
    public byte PlayerCount => (byte)(ActiveUserIds.Count + ExpectedUserIds.Count + InactiveUserIds.Count);
    public string? Password { get; set; } = null;
    public ActorsProperties ActorsProperties { get; set; } = new();
    public List<ClientPeer> Peers { get; set; } = [];
    public byte RoomFlags { get; set; }
    public List<string> GameProperties { get; set; } = new();

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

    public Hashtable ToFullHashTable()
    {
        Hashtable h = [];
        foreach (var keyValue in Properties)
        {
            Log.Information("{Key} = {Value}", keyValue.Key, keyValue.Value);
            h.Add(keyValue.Key, keyValue.Value);
        }
        h[GameParameters.MaxPlayers] = MaxPlayer;
        h[GameParameters.MasterClientId] = 1;
        h[GameParameters.PlayerTTL] = 0;
        h[GameParameters.EmptyRoomTTL] = 0;
        h[GameParameters.ExpectedMaxPlayers] = (int)ExpectedMaxPlayer;
        h[GameParameters.IsVisible] = IsVisible;
        h[GameParameters.IsOpen] = IsOpen;
        h[GameParameters.LobbyProperties] = GameProperties.ToArray();
        // PASSWORD
        // curScn
        return h;
    }

    public Hashtable GetUserHashTable()
    {
        Hashtable h = new();
        for (int i = ActorsProperties.Nicknames.Count; i > 0; i--)
        {
            h[(byte)i] = new Hashtable()
            {
                { 255, ActorsProperties.Nicknames[i] } 
            };
        }
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
public static class GameParameters
{
    public const byte MaxPlayers = 255;
    public const byte IsVisible = 254;
    public const byte IsOpen = 253;
    public const byte LobbyProperties = 250;
    public const byte MasterClientId = 248;
    public const byte ExpectedUsers = 247;
    public const byte PlayerTTL = 246;
    public const byte EmptyRoomTTL = 245;
    public const byte ExpectedMaxPlayers = 243;
    public const byte PlayerCount = 252;
}