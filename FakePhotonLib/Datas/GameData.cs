using FakePhotonLib.Datas;
using Serilog;
using System.Collections;

namespace FakePhotonLib.BinaryData;

/*
 TODO: Make thing to get the Id of the peer
 Make custom class for ActorProperties, would be good idea.
 Search how the UserIds things work.
 */
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
    public List<ActorsProperty> ActorsProperties { get; set; } = [];
    public List<ClientPeer> Peers { get; set; } = [];
    public byte RoomFlags { get; set; }
    public List<string> GameProperties { get; set; } = new();
    public bool Broadcast { get; set; }

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
        h[GameParameters.LobbyProperties] = GameProperties.ToArray();
        h[GameParameters.MasterClientId] = 1;
        h[GameParameters.PlayerTTL] = 0;
        h[GameParameters.EmptyRoomTTL] = 0;
        h[GameParameters.ExpectedMaxPlayers] = (int)ExpectedMaxPlayer;
        h[GameParameters.MaxPlayers] = MaxPlayer;
        h[GameParameters.IsVisible] = IsVisible;
        h[GameParameters.IsOpen] = IsOpen;
        return h;
    }

    public Hashtable GetActorProperties()
    {
        Hashtable h = new();
        for (int i = 0; i < ActorsProperties.Count; i++)
        {
            h[(byte)i+1] = new Hashtable()
            {
                { 255, ActorsProperties[i].PlayerName } 
            };
            if (((RoomFlag)RoomFlags).HasFlag(RoomFlag.PublishUserId))
            {
                ((Hashtable)h[(byte)i + 1]!).Add(253, ActorsProperties[i].UserId);
            }
        }
        return h;
    }

    public int[] GetPeers()
    {
        List<int> peer = [];
        for (int i = 0; i < Peers.Count; i++)
        {
            peer.Add(i+1);
        }
        return peer.ToArray();
    }

    public int GetPeerNumber(ClientPeer peer)
    {
        var indx = Peers.FindIndex(0, x => x.Challenge == peer.Challenge);
        if (indx == -1)
            return -1;
        return indx + 1;
    }
}

[Serializable]
public class ExcludedUser
{
    public string UserId { get; set; } = string.Empty;
    public byte Reason { get; set; }
}

public class ActorsProperty
{
    public string PlayerName { get; set; } = string.Empty; // 255
    public bool IsInactive { get; set; } = false; // 254
    public string UserId { get; set; } = string.Empty; // 253 | Sent when room gets created with RoomOptions.PublishUserId = true.
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