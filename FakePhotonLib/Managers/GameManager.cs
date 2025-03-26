using FakePhotonLib.BinaryData;
using Serilog;
using System.Collections;

namespace FakePhotonLib.Managers;

public static class GameManager
{
    static List<GameData> Games = [];

    public static void Create(string Id)
    {
        Games.Add(new GameData()
        { 
            Id = Id,
            CreatedAt = DateTime.Now,
        });
    }

    public static int GetGameCount() => Games.Count;

    public static bool IsGameExist(string id) => Games.Any(x => x.Id == id);

    public static void ChangeGame(string id, Dictionary<byte, object?> props)
    {
        int idx = Games.FindIndex(x=>x.Id == id);
        if (idx != -1)
            return;
        var game = Games[idx];

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
            if (table.ContainsKey((byte)255))
            {
                game.MaxPlayer = (byte)table[(byte)255]!;
            }
            if (table.ContainsKey((byte)243)) //?
            {
                //game.MaxPlayer = (byte)table[(byte)255]!;
            }
            if (table.ContainsKey((byte)250)) // Properties
            {
                Log.Information("{info}", string.Join(", ", (string[])table[(byte)250]!));
            }
            if (table.ContainsKey((byte)254)) // IsVisible
            {
                game.IsVisible = (bool)table[(byte)254]!;
            }
            if (table.ContainsKey((byte)253)) // IsVisible
            {
                game.IsOpen = (bool)table[(byte)253]!;
            }
        }

        if (props.ContainsKey((byte)ParameterCodesEnum.RoomFlags_JoinGameRequest))
        {
            game.RoomFlags = (byte)props[(byte)ParameterCodesEnum.RoomFlags_JoinGameRequest]!;
        }
    }
}
