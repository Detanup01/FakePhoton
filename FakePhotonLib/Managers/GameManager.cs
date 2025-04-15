using FakePhotonLib.BinaryData;
using FakePhotonLib.Datas;
using Serilog;
using System.Collections;

namespace FakePhotonLib.Managers;

public static class GameManager
{
    static List<GameData> Games = [];

    public static void Create(string id)
    {
        Games.Add(new GameData()
        { 
            Id = id,
            CreatedAt = DateTime.Now,
        });
    }

    public static int GetGameCount() => Games.Count;

    public static bool IsGameExist(string id) => Games.Any(x => x.Id == id);

    public static void ChangeGame(ClientPeer peer, string id, Dictionary<byte, object?> props)
    {
        var game = GetGame(id);

        if (props.ContainsKey((byte)ParameterCodesEnum.ActorProperties_Common))
        {
            Hashtable table = (Hashtable)props[(byte)ParameterCodesEnum.ActorProperties_Common]!;
            if (table.ContainsKey((byte)255))
            {
                game.ActorsProperties.Nicknames.Add((string)table[(byte)255]!);
            }
        }
        if (props.ContainsKey((byte)ParameterCodesEnum.GameProperties_JoinGameRequest))
        {
            Hashtable table = (Hashtable)props[(byte)ParameterCodesEnum.GameProperties_JoinGameRequest]!;
            if (table.ContainsKey(GameParameters.MaxPlayers))
            {
                game.MaxPlayer = (byte)table[GameParameters.MaxPlayers]!;
            }
            if (table.ContainsKey(GameParameters.ExpectedMaxPlayers))
            {
                game.ExpectedMaxPlayer = (byte)(int)table[GameParameters.ExpectedMaxPlayers]!;
            }
            if (table.ContainsKey(GameParameters.LobbyProperties))
            {
                game.GameProperties.AddRange((string[])table[GameParameters.LobbyProperties]!);
            }
            if (table.ContainsKey(GameParameters.IsVisible))
            {
                game.IsVisible = (bool)table[GameParameters.IsVisible]!;
            }
            if (table.ContainsKey(GameParameters.IsOpen))
            {
                game.IsOpen = (bool)table[GameParameters.IsOpen]!;
            }
        }

        if (props.ContainsKey((byte)ParameterCodesEnum.RoomFlags_JoinGameRequest))
        {
            game.RoomFlags = (byte)(int)props[(byte)ParameterCodesEnum.RoomFlags_JoinGameRequest]!;
        }

        // 250 = OnGameServer (bool)
        // 241 = CleanupAfterLeave (bool)
        // 232 = CheckUserOnJoin (bool)

        game.ExpectedUserIds.Add(peer.UserId == null ? peer.Challenge.ToString() : peer.UserId);
        Log.Information("ChangeGame!!!");
    }

    public static void JoinGamePeer(string id, ClientPeer peer)
    {
        GetGame(id).Peers.Add(peer);
        /*
        foreach (var item in GetGame(id).Peers)
        {
            Header header = new()
            {
                CrcOrEncrypted = 0,
                PeerId = 0,
                Commands = [],
                ServerTime = Environment.TickCount,
                Challenge = item.Challenge,
            };
            header.Commands.Add(new()
            { 
                commandType = CommandType.SendReliable,
                ChannelID = 255,
                ReliableSequenceNumber = 1,
            });
        }
        */
    }

    public static void LeaveGamePeer(string id, ClientPeer peer)
    {
        GetGame(id).Peers.Remove(peer);
    }

    public static Hashtable GetHashtableFromGame(string id)
    {
        return GetGame(id).ToFullHashTable();
    }


    public static GameData GetGame(string id)
    {
        int idx = Games.FindIndex(x => x.Id == id);
        if (idx == -1)
            return new()
            {
                Id = id,
                CreatedAt = DateTime.Now,
            };
        return Games[idx];
    }

    public static GameData? GetGame(ClientPeer peer)
    {
        int idx = Games.FindIndex(x => x.ExpectedUserIds.Contains(peer.UserId == null ? peer.Challenge.ToString() : peer.UserId));
        if (idx == -1)
            return null;
        return Games[idx];
    }
}
