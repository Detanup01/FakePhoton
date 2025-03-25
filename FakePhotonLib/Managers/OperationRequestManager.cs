using FakePhotonLib.BinaryData;
using Serilog;

namespace FakePhotonLib.Managers;

public static class OperationRequestManager
{
    public static OperationResponse? Parse(int challenge, OperationRequest opReq)
    {
        if (opReq.OperationCode == (byte)OperationCodeEnum.ExchangeKeys)
        {
            return InitEncryption(challenge, opReq);
        }
        if (opReq.OperationCode == (byte)OperationCodeEnum.GetRegionList)
        {
            return GetRegionList(challenge, opReq);
        }
        if (opReq.OperationCode == (byte)OperationCodeEnum.Authenticate)
        {
            return Authenticate(challenge, opReq);
        }

        Console.WriteLine("Request not found: " + opReq.OperationCode);
        return null;
    }

    internal static OperationResponse InitEncryption(int challenge, OperationRequest opReq)
    {
        var data = (byte[])opReq.Parameters[1]!;
        if (data == null || data.Length == 0)
        {
            Log.Error("Establishing encryption keys failed. Server's public key is null or empty");
            return new() { OperationCode = 0, ReturnCode = -1, DebugMessage = "Encryption key is not present or invalid" };
        }
       
        var responseKey = EncryptionManager.ExchangeKeys(challenge, data);
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

    internal static OperationResponse GetRegionList(int challenge, OperationRequest opReq)
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

    internal static OperationResponse Authenticate(int challenge, OperationRequest opReq)
    {
        if (opReq.Parameters.ContainsKey((byte)ParameterCodesEnum.ApplicationVersion_AuthenticateRequest))
        {
            var Version = opReq.Parameters[(byte)ParameterCodesEnum.ApplicationVersion_AuthenticateRequest];
            var AppId = opReq.Parameters[(byte)ParameterCodesEnum.ApplicationId_AuthenticateRequest];
            var Region = opReq.Parameters[(byte)ParameterCodesEnum.Region_AuthenticateRequest];
            var UserId = opReq.Parameters[(byte)ParameterCodesEnum.UserId_AuthenticateRequest];
            var AuthType = opReq.Parameters[(byte)ParameterCodesEnum.ClientAuthenticationType_AuthenticateRequest];
            var AuthParams = opReq.Parameters[(byte)ParameterCodesEnum.ClientAuthenticationParams_AuthenticateRequest];
            Log.Information("Authenticate! Version {Version} AppId {AppId} Region {Region}, UserId {UserId} AuthType {AuthType} Params {AuthParams}", Version, AppId, Region, UserId, AuthType, AuthParams);

            return new()
            {
                OperationCode = opReq.OperationCode,
                ReturnCode = 0,
                Parameters = new()
            {
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
                { (byte)ParameterCodesEnum.MasterEndpoint_AuthenticateResponse, "127.0.0.1:5055" },
                { (byte)ParameterCodesEnum.AuthenticationToken_AuthenticateResponse, token },
                { (byte)ParameterCodesEnum.UserId_AuthenticateResponse, ((string)token!).Split("_")[1] },
            },
            DebugMessage = null,
        };
    }
}
