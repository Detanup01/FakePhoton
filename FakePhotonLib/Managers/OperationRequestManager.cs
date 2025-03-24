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

        Console.WriteLine("Request not found: " + opReq.OperationCode);
        return null;
    }

    internal static OperationResponse InitEncryption(int challenge, OperationRequest opReq)
    {

        Console.WriteLine($"{opReq.Parameters[1]!.GetType()}");

        var data = (byte[])opReq.Parameters[1]!;
        if (data == null || data.Length == 0)
        {
            Log.Error("Establishing encryption keys failed. Server's public key is null or empty");
            return new() { OperationCode = 0, ReturnCode = -1, DebugMessage = "Encryption key is not present or invalid" };
        }
       
        var responseKey = EncryptionManager.ExchangeKeys(challenge, data);
        Log.Information("Exchaned keys!");
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
}
