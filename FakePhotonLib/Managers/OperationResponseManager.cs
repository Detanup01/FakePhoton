using FakePhotonLib.BinaryData;
using FakePhotonLib.Encryptions;
using Serilog;

namespace FakePhotonLib.Managers;

public static class OperationResponseManager
{
    public static void Parse(int challenge, OperationResponse operationResponse)
    {
        if (operationResponse.Code == 0) // InitEncryption
        {
            InitEncryption(challenge, operationResponse);
        }
    }

    internal static void InitEncryption(int challenge, OperationResponse operationResponse)
    {
        if (operationResponse.ReturnCode != 0)
        {
            Log.Error("Establishing encryption keys failed");
            return;
        }

        var data = (byte[])operationResponse.Parameter[1];
        if (data == null || data.Length == 0)
        {
            Log.Error("Establishing encryption keys failed. Server's public key is null or empty");
            return;
        }
        Log.Information("Key: " + BitConverter.ToString(data).Replace("-", string.Empty));
        EncryptionManager.EncryptionByChallenge.Remove(challenge, out _);
        DiffieHellmanCryptoProvider encryption = new();
        encryption.DeriveSharedKey(data);
        EncryptionManager.EncryptionByChallenge.Add(challenge, encryption);
    }
}
