using FakePhotonLib.BinaryData;
using FakePhotonLib.Datas;
using Serilog;
using System.Net;

namespace FakePhotonLib.Managers;

public static class OperationRequestManager
{
    public static OperationResponse? Parse(ClientPeer peer, OperationRequest opReq)
    {
        if (opReq.OperationCode == (byte)OperationCodeEnum.ExchangeKeys)
        {
            return InitEncryption(peer, opReq);
        }
        if (opReq.OperationCode == (byte)OperationCodeEnum.GetRegionList)
        {
            return GetRegionList(peer, opReq);
        }
        if (opReq.OperationCode == (byte)OperationCodeEnum.Authenticate)
        {
            return Authenticate(peer, opReq);
        }
        if (opReq.OperationCode == (byte)OperationCodeEnum.JoinGame)
        {
            return JoinGame(peer, opReq);
        }

        Console.WriteLine("Request not found: " + opReq.OperationCode);
        return null;
    }

    internal static OperationResponse InitEncryption(ClientPeer peer, OperationRequest opReq)
    {
        var data = (byte[])opReq.Parameters[1]!;
        if (data == null || data.Length == 0)
        {
            Log.Error("Establishing encryption keys failed. Server's public key is null or empty");
            return new() { OperationCode = 0, ReturnCode = -1, DebugMessage = "Encryption key is not present or invalid" };
        }
       
        var responseKey = EncryptionManager.ExchangeKeys(peer, data);
        OperationResponse response = new()
        { 
            OperationCode = opReq.OperationCode, 
            ReturnCode = 0, 
            Parameters = new()
            {
                { 1, responseKey },
            },
            DebugMessage = null,
        };
        if (responseKey == null)
        {
            response.ReturnCode = -1;
        }
        return response;
    }

    internal static OperationResponse GetRegionList(ClientPeer peer, OperationRequest opReq)
    {
        string[] region = ["eu"];
        string[] endpoints = ["127.0.0.1:5505"]; // todo make a config var
        return new()
        {
            OperationCode = opReq.OperationCode,
            ReturnCode = 0,
            Parameters = new()
            {
                { (byte)ParameterCodesEnum.Region_GetRegionListResponse, region },
                { (byte)ParameterCodesEnum.Endpoints_GetRegionListResponse, endpoints },
            },
            DebugMessage = null,
        };
    }

    internal static OperationResponse Authenticate(ClientPeer peer, OperationRequest opReq)
    {
        if (opReq.Parameters.ContainsKey((byte)ParameterCodesEnum.ApplicationVersion_AuthenticateRequest))
        {
            var Version = (string)opReq.Parameters[(byte)ParameterCodesEnum.ApplicationVersion_AuthenticateRequest]!;
            var AppId = opReq.Parameters[(byte)ParameterCodesEnum.ApplicationId_AuthenticateRequest];
            var Region = opReq.Parameters[(byte)ParameterCodesEnum.Region_AuthenticateRequest];
            var UserId = opReq.Parameters[(byte)ParameterCodesEnum.UserId_AuthenticateRequest];
            var AuthType = opReq.Parameters[(byte)ParameterCodesEnum.ClientAuthenticationType_AuthenticateRequest];
            var AuthParams = opReq.Parameters[(byte)ParameterCodesEnum.ClientAuthenticationParams_AuthenticateRequest];
            Log.Information("Authenticate! Version {Version} AppId {AppId} Region {Region}, UserId {UserId} AuthType {AuthType} Params {AuthParams}", Version, AppId, Region, UserId, AuthType, AuthParams);
            peer.UserId = (string)UserId!;
            if (Version == string.Empty)
            {
                return new()
                {
                    OperationCode = opReq.OperationCode,
                    ReturnCode = 0,
                    Parameters = new()
                    {
                        { (byte)ParameterCodesEnum.QueuePosition_AuthenticateResponse, 0 },
                        { (byte)ParameterCodesEnum.AuthenticationToken_AuthenticateResponse, $"{AppId}_{UserId}" },
                    },
                    DebugMessage = null,
                };
            }
            return new()
            {
                OperationCode = opReq.OperationCode,
                ReturnCode = 0,
                Parameters = new()
                {
                    { (byte)ParameterCodesEnum.CurrentCluster_AuthenticateResponse, "default"},
                    { (byte)ParameterCodesEnum.MasterEndpoint_AuthenticateResponse, "127.0.0.1:5055" },
                    { (byte)ParameterCodesEnum.AuthenticationToken_AuthenticateResponse, $"{AppId}_{UserId}" },
                    { (byte)ParameterCodesEnum.UserId_AuthenticateResponse, UserId },
                },
                DebugMessage = null,
            };
        }
        var token = opReq.Parameters[(byte)ParameterCodesEnum.Token_AuthenticateRequest];
        return new()
        {
            OperationCode = opReq.OperationCode,
            ReturnCode = 0,
            Parameters = new()
            {
                { (byte)ParameterCodesEnum.QueuePosition_AuthenticateResponse, 0 },
                { (byte)ParameterCodesEnum.MasterEndpoint_AuthenticateResponse, "127.0.0.1:5055" },
                { (byte)ParameterCodesEnum.AuthenticationToken_AuthenticateResponse, token },
                { (byte)ParameterCodesEnum.UserId_AuthenticateResponse, ((string)token!).Split("_")[1] },
            },
            DebugMessage = null,
        };
    }

    internal static OperationResponse JoinGame(ClientPeer peer, OperationRequest opReq)
    {
        object? obj = null;

        string GameId = (string)opReq.Parameters[(byte)ParameterCodesEnum.GameId_JoinGameRequest]!;
        byte JoinMode = (byte)opReq.Parameters[(byte)ParameterCodesEnum.InternalJoinMode_JoinGameRequest]!;
        bool IsGameExisted = GameManager.IsGameExist(GameId);
        if (JoinMode == 1 && !IsGameExisted)
            GameManager.Create(GameId);
        else
        {
            GameManager.ChangeGame(GameId, opReq.Parameters);
        }
        #region x
        if (opReq.Parameters.TryGetValue((byte)ParameterCodesEnum.ActorNr_JoinGameRequest,out obj))
        {
            Log.Information("JoinGame: {ByteCode} = {Obj}", ParameterCodesEnum.ActorNr_JoinGameRequest, obj);
        }
        if (opReq.Parameters.TryGetValue((byte)ParameterCodesEnum.ActorProperties_Common, out obj))
        {
            Log.Information("JoinGame: {ByteCode} = {Obj}", ParameterCodesEnum.ActorProperties_Common, obj);
        }
        if (opReq.Parameters.TryGetValue((byte)ParameterCodesEnum.AddUsers_JoinGameRequest, out obj))
        {
            Log.Information("JoinGame: {ByteCode} = {Obj}", ParameterCodesEnum.AddUsers_JoinGameRequest, obj);
        }
        if (opReq.Parameters.TryGetValue((byte)ParameterCodesEnum.BroadcastActorProperties_JoinGameRequest, out obj))
        {
            Log.Information("JoinGame: {ByteCode} = {Obj}", ParameterCodesEnum.BroadcastActorProperties_JoinGameRequest, obj);
        }
        if (opReq.Parameters.TryGetValue((byte)ParameterCodesEnum.CacheSlice_JoinGameRequest, out obj))
        {
            Log.Information("JoinGame: {ByteCode} = {Obj}", ParameterCodesEnum.CacheSlice_JoinGameRequest, obj);
        }
        if (opReq.Parameters.TryGetValue((byte)ParameterCodesEnum.CheckUserOnJoin_JoinGameRequest, out obj))
        {
            Log.Information("JoinGame: {ByteCode} = {Obj}", ParameterCodesEnum.CheckUserOnJoin_JoinGameRequest, obj);
        }
        if (opReq.Parameters.TryGetValue((byte)ParameterCodesEnum.DeleteCacheOnLeave_JoinGameRequest, out obj))
        {
            Log.Information("JoinGame: {ByteCode} = {Obj}", ParameterCodesEnum.DeleteCacheOnLeave_JoinGameRequest, obj);
        }
        if (opReq.Parameters.TryGetValue((byte)ParameterCodesEnum.EmptyRoomLiveTime_JoinGameRequest, out obj))
        {
            Log.Information("JoinGame: {ByteCode} = {Obj}", ParameterCodesEnum.EmptyRoomLiveTime_JoinGameRequest, obj);
        }
        if (opReq.Parameters.TryGetValue((byte)ParameterCodesEnum.ForceRejoin_JoinGameRequest, out obj))
        {
            Log.Information("JoinGame: {ByteCode} = {Obj}", ParameterCodesEnum.ForceRejoin_JoinGameRequest, obj);
        }
        if (opReq.Parameters.TryGetValue((byte)ParameterCodesEnum.GameId_JoinGameRequest, out obj))
        {
            Log.Information("JoinGame: {ByteCode} = {Obj}", ParameterCodesEnum.GameId_JoinGameRequest, obj);
        }
        if (opReq.Parameters.TryGetValue((byte)ParameterCodesEnum.GameProperties_JoinGameRequest, out obj))
        {
            Log.Information("JoinGame: {ByteCode} = {Obj}", ParameterCodesEnum.GameProperties_JoinGameRequest, obj);
        }
        if (opReq.Parameters.TryGetValue((byte)ParameterCodesEnum.InternalJoinMode_JoinGameRequest, out obj))
        {
            Log.Information("JoinGame: {ByteCode} = {Obj}", ParameterCodesEnum.InternalJoinMode_JoinGameRequest, obj);
        }
        if (opReq.Parameters.TryGetValue((byte)ParameterCodesEnum.LobbyName_JoinGameRequest, out obj))
        {
            Log.Information("JoinGame: {ByteCode} = {Obj}", ParameterCodesEnum.LobbyName_JoinGameRequest, obj);
        }
        if (opReq.Parameters.TryGetValue((byte)ParameterCodesEnum.LobbyType_JoinGameRequest, out obj))
        {
            Log.Information("JoinGame: {ByteCode} = {Obj}", ParameterCodesEnum.LobbyType_JoinGameRequest, obj);
        }
        if (opReq.Parameters.TryGetValue((byte)ParameterCodesEnum.PlayerTTL_JoinGameRequest, out obj))
        {
            Log.Information("JoinGame: {ByteCode} = {Obj}", ParameterCodesEnum.PlayerTTL_JoinGameRequest, obj);
        }
        if (opReq.Parameters.TryGetValue((byte)ParameterCodesEnum.Plugins_JoinGameRequest, out obj))
        {
            Log.Information("JoinGame: {ByteCode} = {Obj}", ParameterCodesEnum.Plugins_JoinGameRequest, obj);
        }
        if (opReq.Parameters.TryGetValue((byte)ParameterCodesEnum.PublishUserId_JoinGameRequest, out obj))
        {
            Log.Information("JoinGame: {ByteCode} = {Obj}", ParameterCodesEnum.PublishUserId_JoinGameRequest, obj);
        }
        if (opReq.Parameters.TryGetValue((byte)ParameterCodesEnum.RoomFlags_JoinGameRequest, out obj))
        {
            Log.Information("JoinGame: {ByteCode} = {Obj}", ParameterCodesEnum.RoomFlags_JoinGameRequest, obj);
        }
        if (opReq.Parameters.TryGetValue((byte)ParameterCodesEnum.WebFlags_JoinGameRequest, out obj))
        {
            Log.Information("JoinGame: {ByteCode} = {Obj}", ParameterCodesEnum.WebFlags_JoinGameRequest, obj);
        }
        #endregion


        if (!IsGameExisted)
        {
            return new()
            {
                OperationCode = opReq.OperationCode,
                ReturnCode = 0,
                Parameters = new()
                {
                    { (byte)ParameterCodesEnum.ActorNr_JoinGameRequest, 1 },
                    { (byte)ParameterCodesEnum.GameProperties_JoinGameResponse, GameManager.GetHashtableFromGame(GameId) },
                    { (byte)ParameterCodesEnum.Actors_Common, (int[])[1] },
                    { (byte)ParameterCodesEnum.RoomFlags_JoinGameResponse, 35 },
                }
            };
        }
        return new()
        {
            OperationCode = opReq.OperationCode,
            ReturnCode = 0,
            Parameters = new()
            {
                { (byte)ParameterCodesEnum.Address_JoinGameResponse, "127.0.0.1:5000" },
                { (byte)ParameterCodesEnum.AuthenticationToken_JoinGameResponse, $"TokenForGameId_{GameId}_{GameManager.GetGameCount()}" }
            }
        };
    }
}
