using FakePhotonLib.Encryptions;
using Serilog;

namespace FakePhotonLib.Managers;

public static class EncryptionManager
{
    public static Dictionary<int, DiffieHellmanCryptoProvider> EncryptionByChallenge = [];

    public static byte[] ExchangeKeys(int challenge, byte[] key)
    {
        Log.Information("Client Key: " + Convert.ToHexString(key));
        EncryptionByChallenge.Remove(challenge);
        DiffieHellmanCryptoProvider ServerEncryption = new();
        ServerEncryption.DeriveSharedKeyAsServer(key);
        EncryptionByChallenge.Add(challenge, ServerEncryption);
        var server_key = ServerEncryption.PublicKeyAsServer;
        Log.Information("Server: " + ServerEncryption.ToString());
        return server_key;
    }
}
