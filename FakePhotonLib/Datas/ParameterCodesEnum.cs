﻿namespace FakePhotonLib.BinaryData;

/// <summary>
/// Name _ OpCodeName
/// </summary>
public enum ParameterCodesEnum
{
    EncryptionMode_EncryptionData = 0,
    EncryptionSecret_EncryptionData = 1,
    AuthSecret_EncryptionData = 2,
    ClientTimeStamp_PingRequest = 1,
    ClientTimeStamp_PingResponse = 1,
    ServerTimeStamp_PingResponse = 2,
    Key_ExchangeKey = 1,
    Mode_ExchangeKeyRequest = 2,
    Region_GetRegionListResponse = 210,
    Endpoints_GetRegionListResponse = 230,
    ApplicationId_GetRegionListRequest = 224,
    Protocol_AuthOnceRequest = 195,
    EncyrptionMode_AuthOnceRequest = 193,
    MasterEndpoint_AuthenticateResponse = 230,
    AuthenticationToken_AuthenticateResponse = 221,
    Data_AuthenticateResponse = 245,
    Nickname_AuthenticateResponse = 202,
    UserId_AuthenticateResponse = 225,
    EncryptionData_AuthenticateResponse = 192,
    ApplicationId_AuthenticateRequest = 224,
    ApplicationVersion_AuthenticateRequest = 220,
    Token_AuthenticateRequest = 221,
    UserId_AuthenticateRequest = 225,
    ClientAuthenticationType_AuthenticateRequest = 217,
    ClientAuthenticationParams_AuthenticateRequest = 216,
    ClientAuthenticationData_AuthenticateRequest = 214,
    Region_AuthenticateRequest = 210,
    Flags_AuthenticateRequest = 199,
    ExpireAtTicks_AuthenticationToken = 1,
    ApplicationId_AuthenticationToken = 2,
    ApplicationVersion_AuthenticationToken = 3,
    UserId_AuthenticationToken = 4,
    AuthCookie_AuthenticationToken = 8,
    SessionId_AuthenticationToken = 10,
    Flags_AuthenticationToken = 11,
    EncryptionData_AuthenticationToken = 13,
    FinalExpireAtTicks_AuthenticationToken = 14,
    TokenIssuer_AuthenticationToken = 15,
    CustomAuthProvider_AuthenticationToken = 16,
    NoTokenAuthOnMaster_AuthenticationToken = 115,
    ExpectedGS_AuthenticationToken = 116,
    ExpectedGameId_AuthenticationToken = 117,
    CustomAuthUserIdUsed_AuthenticationToken = 118,
    QueuePosition_AuthenticateResponse = 223,
    CurrentCluster_AuthenticateResponse = 196,
    BroadcastActorProperties_JoinGameRequest = 250,
    GameId_JoinGameRequest = 255,
    GameProperties_JoinGameRequest = 248,
    DeleteCacheOnLeave_JoinGameRequest = 241,
    SuppressRoomEvents_JoinGameRequest = 237,
    ActorNr_JoinGameRequest = 254,
    EmptyRoomLiveTime_JoinGameRequest = 236,
    PlayerTTL_JoinGameRequest = 235,
    CheckUserOnJoin_JoinGameRequest = 232,
    CacheSlice_JoinGameRequest = 205,
    LobbyName_JoinGameRequest = 213,
    LobbyType_JoinGameRequest = 212,
    InternalJoinMode_JoinGameRequest = 215,
    Plugins_JoinGameRequest = 204,
    WebFlags_JoinGameRequest = 234,
    AddUsers_JoinGameRequest = 238,
    PublishUserId_JoinGameRequest = 239,
    ForceRejoin_JoinGameRequest = 229,
    RoomFlags_JoinGameRequest = 191,
    AuthenticationToken_JoinGameResponse = 221,
    Address_JoinGameResponse = 230,
    RoomFlags_JoinGameResponse = 191,
    GameProperties_JoinGameResponse = 248,
    ActorProperties_Common = 249,
    Actors_Common = 252,
    Broadcast_Common = 250,
    Properties_Common = 251,
}
