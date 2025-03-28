using FakePhotonLib.BinaryData;
using FakePhotonLib.Datas;
using FakePhotonLib.Encryptions;
using Serilog;

namespace FakePhotonLib.Managers;

public static class OperationResponseManager
{
    public static void Parse(ClientPeer peer, OperationResponse operationResponse)
    {
        if (operationResponse.OperationCode == 0) // InitEncryption
        {
            InitEncryption(peer, operationResponse);
        }
    }

    internal static void InitEncryption(ClientPeer peer, OperationResponse operationResponse)
    {
        if (operationResponse.ReturnCode != 0)
        {
            Log.Error("Establishing encryption keys failed");
            return;
        }

        var data = (byte[])operationResponse.Parameters[1]!;
        if (data == null || data.Length == 0)
        {
            Log.Error("Establishing encryption keys failed. Server's public key is null or empty");
            return;
        }
        Log.Information("Key: " + Convert.ToHexString(data));
        EncryptionManager.EncryptionByChallenge.Remove(peer, out _);
        DiffieHellmanCryptoProvider encryption = new();
        encryption.DeriveSharedKeyAsClient(data);
        EncryptionManager.EncryptionByChallenge.Add(peer, encryption);
    }
}
