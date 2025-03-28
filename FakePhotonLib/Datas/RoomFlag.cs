namespace FakePhotonLib.Datas;

[Flags]
public enum RoomFlag
{
    None = 0,
    CheckUserOnJoin = 1,
    DeleteCacheOnLeave = 2,
    SuppressRoomEvents = 4,
    PublishUserId = 8,
    DeleteNullProperties = 16,
    BroadcastPropertiesChangeToAll = 32,
    SuppressPlayerInfo = 64,
}
