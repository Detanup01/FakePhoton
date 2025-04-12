namespace FakePhotonLib.BinaryData;

public enum OperationCodeEnum : byte
{
    ExchangeKeys = 0,
    GetGameList = 217,
    Settings = 218,
    Rpc = 219,
    LobbyStats = 221,
    GetRegionList = 220,
    FindFriends = 222,
    DebugGame = 223,
    JoinRandomGame = 225,
    JoinGame = 226,
    CreateGame = 227,
    LeaveLobby = 228,
    JoinLobby = 229,
    Authenticate = 230,
    AuthOnce = 231,
    ChangeGroups = 248,
    Ping = 249,
    GetProperties = 251,
    SetProperties = 252,
    RaiseEvent = 253,
    Leave = 254,
    Join = 255
}
