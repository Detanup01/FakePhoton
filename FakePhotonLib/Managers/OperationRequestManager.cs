using FakePhotonLib.BinaryData;
using Serilog;

namespace FakePhotonLib.Managers;

public static class OperationRequestManager
{
    public static OperationResponse? Parse(int challenge, OperationRequest operationResponse)
    {
        if (operationResponse.OperationCode == 0) // InitEncryption
        {
            return InitEncryption(challenge, operationResponse);
        }
        Console.WriteLine("Request not found: " + operationResponse.OperationCode);
        return null;
    }

    internal static OperationResponse InitEncryption(int challenge, OperationRequest operationResponse)
    {
        var data = (byte[])operationResponse.Parameters[1]!;
        if (data == null || data.Length == 0)
        {
            Log.Error("Establishing encryption keys failed. Server's public key is null or empty");
            return new() { OperationCode = 0, ReturnCode = -1, DebugMessage = "Encryption key is not present or invalid" };
        }
        var responseKey = EncryptionManager.ExchangeKeys(challenge, data);

        OperationResponse response = new()
        { 
            OperationCode = operationResponse.OperationCode, 
            ReturnCode = 0, 
            Parameters = new()
            {
                { 1, responseKey },
            }
        };
        if (responseKey == null)
        {
            response.ReturnCode = -1;
        }
        return response;
    }
}
